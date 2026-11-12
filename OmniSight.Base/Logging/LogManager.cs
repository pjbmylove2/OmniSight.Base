using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OmniSight.Base.Configuration;

namespace OmniSight.Base.Logging
{
    /// <summary>검사 호출부에서는 디스크를 기다리지 않는 유한 큐, 단일 기록 작업자입니다.</summary>
    public sealed class LogManager : IDisposable
    {
        private sealed class Pending
        {
            public LogEntry Entry;
            public long Bytes;
            public bool Priority;
            public TaskCompletionSource<bool> Barrier;
        }
        private sealed class CachedStream
        {
            public StreamWriter Writer;
            public string Path;
            public bool Fallback;
            public long LastUsed;
            public readonly List<Pending> Pending = new List<Pending>();
        }
        private readonly BlockingCollection<Pending> _logQueue;
        private readonly BlockingCollection<Pending> _priorityQueue;
        private readonly AutoResetEvent _wake = new AutoResetEvent(false);
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly Task _writeTask;
        private readonly Dictionary<string, CachedStream> _writerCache = new Dictionary<string, CachedStream>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, long> _retryAfter = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, long> _throttleTimestamps = new ConcurrentDictionary<string, long>(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, byte> _activeAlarms = new ConcurrentDictionary<string, byte>(StringComparer.Ordinal);
        private readonly string _directory;
        private readonly int[] _enabled = { 1, 0, 1, 1, 1, 0, 0 };
        private readonly string _session = Guid.NewGuid().ToString("N");
        private readonly string _fallbackRoot;
        private long _accepted, _written, _dropped, _failed, _fallbackWritten;
        private long _normalBytes, _priorityBytes, _droppedNotice, _lastDropNotice, _lastCleanup, _lastProbe, _lastFailure;
        private long _shutdownDeadline = long.MaxValue;
        private long _lastFailureTrace;
        private string _lastError = string.Empty;
        private string _lastFailedPath;
        private int _storageDegraded, _disposeStarted;
        private volatile bool _isDisposed;
        private bool _overflowIncidentActive;
        private const long NormalBudget = 32L * 1024 * 1024;
        private const long PriorityBudget = 8L * 1024 * 1024;
        public string FallbackRoot => _fallbackRoot;
        public LogManagerHealth Health => new LogManagerHealth
        {
            SessionId = _session, FallbackRoot = _fallbackRoot, LastError = Volatile.Read(ref _lastError),
            Accepted = Interlocked.Read(ref _accepted), Written = Interlocked.Read(ref _written),
            Dropped = Interlocked.Read(ref _dropped), Failed = Interlocked.Read(ref _failed),
            FallbackWritten = Interlocked.Read(ref _fallbackWritten),
            QueuedBytes = Interlocked.Read(ref _normalBytes) + Interlocked.Read(ref _priorityBytes),
            QueueDepth = SafeCount(_logQueue) + SafeCount(_priorityQueue),
            IsStorageDegraded = Volatile.Read(ref _storageDegraded) != 0
        };
        private static int SafeCount(BlockingCollection<Pending> queue)
        {
            try { return queue.Count; } catch (ObjectDisposedException) { return 0; }
        }
        public LogManager(string directory) : this(directory, Path.Combine(Path.GetTempPath(), "OmniSight.Base", "LogFallback"))
        {
        }

        public LogManager(string directory, string fallbackDirectory) : this(directory, fallbackDirectory, 32768, 4096)
        {
        }

        internal LogManager(string directory, string fallbackDirectory, int capacity, int priorityCapacity)
        {
            _directory = Path.GetFullPath(directory ?? throw new ArgumentNullException(nameof(directory)));
            _fallbackRoot = Path.Combine(Path.GetFullPath(fallbackDirectory ?? throw new ArgumentNullException(nameof(fallbackDirectory))), _session);
            _logQueue = new BlockingCollection<Pending>(capacity);
            _priorityQueue = new BlockingCollection<Pending>(priorityCapacity);
            _lastCleanup = Stopwatch.GetTimestamp();
            _writeTask = Task.Factory.StartNew(ProcessLogQueue, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public bool WriteDebugLog
        {
            get => IsEnabled(LogCategory.DEBUG);
            set => SetEnabled(LogCategory.DEBUG, value);
        }

        public bool IsEnabled(LogCategory category)
        {
            return category >= 0 && category < LogCategory.MAX && Volatile.Read(ref _enabled[(int)category]) != 0;
        }

        public void SetEnabled(LogCategory category, bool enabled)
        {
            if (category < 0 || category >= LogCategory.MAX)
            {
                throw new ArgumentOutOfRangeException(nameof(category));
            }

            Volatile.Write(ref _enabled[(int)category], enabled ? 1 : 0);
        }

        public void System(string message) => Write(LogCategory.SYSTEM, message);
        public void Alarm(string message, Exception error = null) => Write(LogCategory.ALARM, message, error: error);
        public void Debug(string message) => Write(LogCategory.DEBUG, message);

        public void Write(LogCategory category, string message, string lotId = "", int camId = -1,
            string eventCode = null, Exception error = null)
        {
            if (IsEnabled(category))
            {
                EnqueueLog(new LogEntry(category, message, lotId, camId, eventCode, error));
            }
        }

        public void WriteSystemThrottled(string key, string message, TimeSpan minimumInterval, string lotId = "")
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("A throttle key is required.", nameof(key));
            }

            EnqueueThrottled(key, new LogEntry(LogCategory.SYSTEM, message, lotId, -1, key), minimumInterval);
        }

        /// <summary>Records an active alarm once. Clear it on recovery to allow the next occurrence.</summary>
        public void SetAlarmActive(string key, string message, string lotId = "")
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("An alarm key is required.", nameof(key));
            }

            if (!_isDisposed && IsEnabled(LogCategory.ALARM) && _activeAlarms.Count < 4096 && _activeAlarms.TryAdd(key, 0))
            {
                Write(LogCategory.ALARM, message, lotId, eventCode: key);
            }
        }

        public void ClearAlarm(string key)
        {
            if (key != null)
            {
                _activeAlarms.TryRemove(key, out _);
            }
        }

        private void EnqueueThrottled(string key, LogEntry entry, TimeSpan interval)
        {
            if (interval < TimeSpan.Zero) { throw new ArgumentOutOfRangeException(nameof(interval)); }
            if (_isDisposed || !IsEnabled(entry.Category)) { return; }
            long now = Stopwatch.GetTimestamp();
            while (true)
            {
                long previous;
                if (_throttleTimestamps.TryGetValue(key, out previous))
                {
                    if (now - previous < interval.TotalSeconds * Stopwatch.Frequency) { return; }
                    if (!_throttleTimestamps.TryUpdate(key, now, previous)) { continue; }
                }
                else
                {
                    // 동적 프레임/파일명을 key로 잘못 사용해도 사전이 무한 증가하지 않습니다.
                    if (_throttleTimestamps.Count >= 4096) { return; }
                    if (!_throttleTimestamps.TryAdd(key, now)) { continue; }
                }
                EnqueueLog(entry);
                return;
            }
        }
        private void EnqueueLog(LogEntry entry)
        {
            if (_isDisposed || !IsEnabled(entry.Category)) { return; }
            bool priority = entry.Category == LogCategory.RESULT || entry.Category == LogCategory.ALARM;
            var item = new Pending
            {
                Entry = entry, Priority = priority,
                Bytes = 256L + (entry.Message.Length + entry.ExceptionDetails.Length + entry.LotId.Length) * 2L
            };
            bool reserved = Reserve(item);
            try
            {
                if (reserved && (priority ? _priorityQueue : _logQueue).TryAdd(item))
                {
                    Interlocked.Increment(ref _accepted);
                    try { _wake.Set(); } catch (ObjectDisposedException) { }
                    return;
                }
            }
            catch (InvalidOperationException) { }
            if (reserved) { Release(item); }
            Interlocked.Increment(ref _dropped);
            Interlocked.Increment(ref _droppedNotice);
        }
        private bool Reserve(Pending item)
        {
            ref long bytes = ref (item.Priority ? ref _priorityBytes : ref _normalBytes);
            long maximum = item.Priority ? PriorityBudget : NormalBudget;
            while (true)
            {
                long current = Interlocked.Read(ref bytes);
                if (item.Bytes > maximum - current) { return false; }
                if (Interlocked.CompareExchange(ref bytes, current + item.Bytes, current) == current) { return true; }
            }
        }
        private void Release(Pending item)
        {
            if (item.Priority) { Interlocked.Add(ref _priorityBytes, -item.Bytes); }
            else { Interlocked.Add(ref _normalBytes, -item.Bytes); }
        }
        /// <summary>이 호출 이전에 접수된 양쪽 큐의 기록을 flush합니다. 검사 루프에서는 호출하지 않습니다.</summary>
        public bool Flush(TimeSpan timeout)
        {
            if (_isDisposed) { return false; }
            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            try
            {
                if (!_logQueue.TryAdd(new Pending { Barrier = completion })) { return false; }
                _wake.Set();
                return completion.Task.Wait(timeout) && completion.Task.Result;
            }
            catch (InvalidOperationException) { return false; }
        }
        private void ProcessLogQueue()
        {
            try
            {
                try { Thread.CurrentThread.Priority = ThreadPriority.BelowNormal; } catch (Exception) { }
                long lastFlush = Stopwatch.GetTimestamp();
                int buffered = 0;
                while (!_cts.IsCancellationRequested)
                {
                    int count = Drain(_priorityQueue, 64) + Drain(_logQueue, 512);
                    buffered += count;
                    long now = Stopwatch.GetTimestamp();
                    // 저속 입력에서도 행마다 flush하지 않습니다. 최대 100ms 또는 512건을 모읍니다.
                    if (buffered >= 512 || now - lastFlush >= Stopwatch.Frequency / 10)
                    {
                        FlushAll();
                        buffered = 0;
                        lastFlush = now;
                    }
                    Maintenance();
                    if (count == 0) { _wake.WaitOne(20); }
                }
                while (Stopwatch.GetTimestamp() < Interlocked.Read(ref _shutdownDeadline) &&
                    (SafeCount(_priorityQueue) > 0 || SafeCount(_logQueue) > 0))
                {
                    Drain(_priorityQueue, 64); Drain(_logQueue, 512); FlushAll();
                }
                DiscardRemaining(_priorityQueue); DiscardRemaining(_logQueue);
                Internal(LogCategory.SYSTEM, "Logger.SessionStopped", "accepted=" + Interlocked.Read(ref _accepted) +
                    "; written=" + Interlocked.Read(ref _written) + "; dropped=" + Interlocked.Read(ref _dropped) +
                    "; failed=" + Interlocked.Read(ref _failed) + "; fallback=" + Interlocked.Read(ref _fallbackWritten));
            }
            catch (Exception ex)
            {
                _isDisposed = true;
                _logQueue.CompleteAdding();
                _priorityQueue.CompleteAdding();
                RecordFailure(ex);
                Trace.WriteLine("[LogManager] 기록 작업자 종료 오류: " + ex);
            }
            finally
            {
                DiscardRemaining(_priorityQueue); DiscardRemaining(_logQueue);
                FlushAll();
                foreach (string path in _writerCache.Keys.ToArray()) { CloseWriter(path); }
            }
        }
        private int Drain(BlockingCollection<Pending> queue, int maximum)
        {
            int count = 0;
            while (count < maximum && queue.TryTake(out Pending item))
            {
                count++;
                if (item.Barrier != null)
                {
                    Drain(_priorityQueue, SafeCount(_priorityQueue)); FlushAll();
                    item.Barrier.TrySetResult(true);
                }
                else { Write(item); }
            }
            return count;
        }
        private void DiscardRemaining(BlockingCollection<Pending> queue)
        {
            while (queue.TryTake(out Pending item))
            {
                if (item.Barrier != null) { item.Barrier.TrySetResult(false); }
                else { Release(item); Interlocked.Increment(ref _dropped); }
            }
        }
        private static string SafeName(string value)
        {
            return SafePathSegment.Sanitize(value, string.Empty);
        }
        private static string RelativePath(LogEntry entry)
        {
            DateTime at = entry.Timestamp;
            string day = at.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            string directory = Path.Combine(day.Substring(0, 4), day.Substring(5, 2), day.Substring(8, 2));
            string lot = SafeName(entry.LotId);
            if (entry.Category == LogCategory.DEBUG || entry.Category == LogCategory.RESULT || entry.Category == LogCategory.DIO)
            {
                directory = Path.Combine(directory, "CAM" + entry.CamId.ToString(CultureInfo.InvariantCulture), lot);
            }
            string file;
            switch (entry.Category)
            {
                case LogCategory.COMMUNICATION: file = day + "-" + at.Hour.ToString("00", CultureInfo.InvariantCulture) + "_Communication.log"; break;
                case LogCategory.RESULT: file = day + (lot.Length > 0 ? "_" + lot : "") + "_Result.log"; break;
                case LogCategory.DEBUG: file = day + (lot.Length > 0 ? "_" + lot : "") + "_Debug.log"; break;
                case LogCategory.DIO: file = day + (lot.Length > 0 ? "_" + lot : "") + "_DIO.log"; break;
                case LogCategory.ALARM: file = day + "_Alarm.log"; break;
                case LogCategory.ETHERNET: file = day + "_Ethernet.log"; break;
                default: file = day + "_System.log"; break;
            }
            return Path.Combine(directory, file);
        }
        private void Write(Pending item)
        {
            string path = Path.Combine(_directory, RelativePath(item.Entry));
            if (_retryAfter.TryGetValue(path, out long retry) && Stopwatch.GetTimestamp() < retry)
            {
                WriteFallback(item); return;
            }
            try { Buffer(item, path, false); }
            catch (Exception ex) { FailPrimary(path, item, ex); }
        }
        private void Buffer(Pending item, string path, bool fallback)
        {
            CachedStream cached;
            if (!_writerCache.TryGetValue(path, out cached))
            {
                while (_writerCache.Count >= 128)
                {
                    string oldest = _writerCache.OrderBy(pair => pair.Value.LastUsed).First().Key;
                    CachedStream evicted = _writerCache[oldest];
                    _writerCache.Remove(oldest);
                    try { FlushWriter(evicted); }
                    finally { DisposeStream(evicted); }
                }
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                string archived = UnicodeLogFile.PrepareForAppend(path, trustUtf8Bom: true);
                bool header = !File.Exists(path) || new FileInfo(path).Length == 0;
                var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete, 65536);
                StreamWriter writer = null;
                try
                {
                    writer = new StreamWriter(stream, UnicodeLogFile.Utf8WithBom, 65536) { AutoFlush = false };
                    if (header)
                    {
                        writer.WriteLine("OmniSight " + item.Entry.Category);
                    }
                    cached = new CachedStream { Writer = writer, Path = path, Fallback = fallback };
                    _writerCache.Add(path, cached);
                }
                catch
                {
                    try { writer?.Dispose(); } catch (Exception) { }
                    stream.Dispose();
                    throw;
                }
                if (archived != null) { Trace.WriteLine("[LogManager] 기존 인코딩 파일 보존: " + archived); }
            }
            cached.LastUsed = Stopwatch.GetTimestamp();
            cached.Pending.Add(item);
            cached.Writer.WriteLine(LogRecordFormatter.Format(item.Entry));
        }
        private void FlushWriter(CachedStream cached)
        {
            if (cached.Pending.Count == 0) { return; }
            try
            {
                cached.Writer.Flush();
                int count = cached.Pending.Count;
                foreach (Pending item in cached.Pending) { Release(item); }
                cached.Pending.Clear();
                Interlocked.Add(ref _written, count);
                if (cached.Fallback) { Interlocked.Add(ref _fallbackWritten, count); }
                else
                {
                    _retryAfter.Remove(cached.Path);
                    if (string.Equals(cached.Path, Volatile.Read(ref _lastFailedPath), StringComparison.OrdinalIgnoreCase) &&
                        Stopwatch.GetTimestamp() - Interlocked.Read(ref _lastFailure) > Stopwatch.Frequency)
                    { Interlocked.Exchange(ref _storageDegraded, 0); }
                }
            }
            catch (Exception ex)
            {
                Pending[] pending = cached.Pending.ToArray(); cached.Pending.Clear();
                CloseWriter(cached.Path);
                if (cached.Fallback)
                {
                    RecordFailure(ex);
                    foreach (Pending item in pending) { Release(item); Interlocked.Increment(ref _failed); }
                }
                else
                {
                    Backoff(cached.Path, ex);
                    foreach (Pending item in pending) { WriteFallback(item); }
                }
            }
        }
        private void FailPrimary(string path, Pending current, Exception ex)
        {
            Backoff(path, ex);
            if (_writerCache.TryGetValue(path, out CachedStream cached))
            {
                Pending[] items = cached.Pending.ToArray(); cached.Pending.Clear(); CloseWriter(path);
                foreach (Pending item in items) { WriteFallback(item); }
                if (!items.Contains(current)) { WriteFallback(current); }
            }
            else { WriteFallback(current); }
        }
        private void Backoff(string path, Exception ex)
        {
            Volatile.Write(ref _lastFailedPath, path);
            RecordFailure(ex);
            if (_retryAfter.Count >= 128) { _retryAfter.Remove(_retryAfter.Keys.First()); }
            _retryAfter[path] = Stopwatch.GetTimestamp() + Stopwatch.Frequency;
        }
        private void WriteFallback(Pending item)
        {
            Interlocked.Exchange(ref _lastFailure, Stopwatch.GetTimestamp());
            try { Buffer(item, Path.Combine(_fallbackRoot, RelativePath(item.Entry)), true); }
            catch (Exception ex)
            {
                RecordFailure(ex);
                string path = Path.Combine(_fallbackRoot, RelativePath(item.Entry));
                if (_writerCache.TryGetValue(path, out CachedStream cached))
                {
                    Pending[] items = cached.Pending.ToArray(); cached.Pending.Clear(); CloseWriter(path);
                    foreach (Pending failed in items) { Release(failed); Interlocked.Increment(ref _failed); }
                    if (items.Contains(item)) { return; }
                }
                Release(item); Interlocked.Increment(ref _failed);
            }
        }
        private void RecordFailure(Exception ex)
        {
            Interlocked.Exchange(ref _storageDegraded, 1);
            Interlocked.Exchange(ref _lastFailure, Stopwatch.GetTimestamp());
            Volatile.Write(ref _lastError, ex.ToString());
            long now = Stopwatch.GetTimestamp();
            if (_lastFailureTrace == 0 || now - _lastFailureTrace >= Stopwatch.Frequency)
            {
                _lastFailureTrace = now;
                Trace.TraceError("[LogManager] 로그 기록 장애; fallback={0} / {1}", _fallbackRoot, ex);
            }
        }
        private void FlushAll()
        {
            // 원본 flush 실패로 생성된 우회 버퍼도 두 번째 패스에서 기록합니다.
            for (int pass = 0; pass < 2; pass++)
            {
                foreach (CachedStream cached in _writerCache.Values.ToArray()) { FlushWriter(cached); }
            }
        }
        private void CloseWriter(string path)
        {
            if (!_writerCache.TryGetValue(path, out CachedStream cached)) { return; }
            _writerCache.Remove(path);
            DisposeStream(cached);
        }
        private static void DisposeStream(CachedStream cached)
        {
            Stream stream = cached.Writer.BaseStream;
            try { cached.Writer.Dispose(); }
            catch (Exception ex) { Trace.WriteLine("[LogManager] 스트림 종료 오류: " + ex); }
            finally
            {
                try { stream?.Dispose(); } catch (Exception ex) { Trace.WriteLine("[LogManager] 핸들 종료 오류: " + ex); }
            }
        }
        private void Internal(LogCategory category, string code, string message)
        {
            Interlocked.Increment(ref _accepted);
            Write(new Pending { Entry = new LogEntry(category, message, "", -1, code) });
        }
        private void Maintenance()
        {
            long now = Stopwatch.GetTimestamp();
            if (now - _lastDropNotice >= Stopwatch.Frequency && SafeCount(_logQueue) <= _logQueue.BoundedCapacity / 2)
            {
                long count = Interlocked.Exchange(ref _droppedNotice, 0);
                if (count > 0 && !_overflowIncidentActive)
                {
                    Internal(LogCategory.ALARM, "Logger.QueueOverflow", "[로그] 저장 지연으로 로그 큐가 가득 찼습니다. 누락=" + Interlocked.Read(ref _dropped));
                    _overflowIncidentActive = true;
                }
                else if (count == 0) { _overflowIncidentActive = false; }
                _lastDropNotice = now;
            }
            if (Volatile.Read(ref _storageDegraded) != 0 && now - _lastProbe >= Stopwatch.Frequency * 5L)
            {
                _lastProbe = now;
                string path = Volatile.Read(ref _lastFailedPath);
                if (path != null)
                {
                    try
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(path));
                        using (var probe = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete))
                        { probe.Flush(); }
                        _retryAfter.Remove(path);
                        Interlocked.Exchange(ref _storageDegraded, 0);
                        Internal(LogCategory.SYSTEM, "Logger.StorageRecovered", "path=" + path + "; fallback=" + _fallbackRoot);
                    }
                    catch (Exception ex) { RecordFailure(ex); }
                }
            }
            if (now - _lastCleanup < Stopwatch.Frequency * 60L) { return; }
            _lastCleanup = now;
            foreach (string path in _writerCache.Where(pair => now - pair.Value.LastUsed > Stopwatch.Frequency * 300L).Select(pair => pair.Key).ToArray())
            {
                if (_writerCache.TryGetValue(path, out CachedStream cached))
                {
                    FlushWriter(cached);
                    CloseWriter(path);
                }
            }
            foreach (string key in _throttleTimestamps.Where(pair => now - pair.Value > Stopwatch.Frequency * 3600L).Select(pair => pair.Key).ToArray())
            { _throttleTimestamps.TryRemove(key, out long ignored); }
        }
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposeStarted, 1) != 0) { return; }
            _isDisposed = true;
            Interlocked.Exchange(ref _shutdownDeadline, Stopwatch.GetTimestamp() + Stopwatch.Frequency * 3L);
            _logQueue.CompleteAdding(); _priorityQueue.CompleteAdding(); _cts.Cancel(); _wake.Set();
            try { _writeTask.Wait(3500); }
            catch (AggregateException ex) { Trace.WriteLine("[LogManager] 종료 오류: " + ex); }
            _writeTask.ContinueWith(task =>
            {
                var ignored = task.Exception;
                _cts.Dispose(); _wake.Dispose(); _logQueue.Dispose(); _priorityQueue.Dispose();
            }, TaskScheduler.Default);
        }
    }
}
