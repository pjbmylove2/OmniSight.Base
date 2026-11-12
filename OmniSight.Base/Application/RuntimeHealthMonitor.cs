using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using OmniSight.Base.Logging;

namespace OmniSight.Base.Application
{
    public sealed class RuntimeHealthSnapshot
    {
        internal RuntimeHealthSnapshot(string driveRoot, long? driveFreeBytes, long? driveTotalBytes, string driveError,
            long workingSetBytes, long privateBytes, long managedBytes, LogManagerHealth log)
        {
            CollectedAtUtc = DateTime.UtcNow;
            DriveRoot = driveRoot;
            DriveFreeBytes = driveFreeBytes;
            DriveTotalBytes = driveTotalBytes;
            DriveError = driveError;
            WorkingSetBytes = workingSetBytes;
            PrivateBytes = privateBytes;
            ManagedBytes = managedBytes;
            Log = log;
        }

        public DateTime CollectedAtUtc { get; }
        public string DriveRoot { get; }
        public long? DriveFreeBytes { get; }
        public long? DriveTotalBytes { get; }
        public string DriveError { get; }
        public long WorkingSetBytes { get; }
        public long PrivateBytes { get; }
        public long ManagedBytes { get; }
        public LogManagerHealth Log { get; }
    }

    /// <summary>Samples local storage, process memory and logger health off the UI thread.</summary>
    public sealed class RuntimeHealthMonitor : IDisposable
    {
        private readonly string _driveRoot;
        private readonly LogManager _log;
        private readonly Timer _timer;
        private RuntimeHealthSnapshot _latest;
        private int _sampling;
        private int _disposed;
        private long? _lastReport;

        public RuntimeHealthMonitor(string dataPath, LogManager log, TimeSpan? samplePeriod = null)
        {
            _driveRoot = Path.GetPathRoot(Path.GetFullPath(dataPath));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            TimeSpan period = samplePeriod ?? TimeSpan.FromSeconds(30);
            if (period.TotalMilliseconds < 1 || period.TotalMilliseconds > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(samplePeriod));
            }

            _timer = new Timer(Sample, null, TimeSpan.Zero, period);
        }

        /// <summary>Null until the first sample completes.</summary>
        public RuntimeHealthSnapshot Latest => Volatile.Read(ref _latest);

        private void Sample(object state)
        {
            if (Volatile.Read(ref _disposed) != 0 || Interlocked.Exchange(ref _sampling, 1) != 0)
            {
                return;
            }

            try
            {
                long? free = null;
                long? total = null;
                string driveError = null;
                try
                {
                    var drive = new DriveInfo(_driveRoot);
                    if (drive.IsReady)
                    {
                        long available = drive.AvailableFreeSpace;
                        total = drive.TotalSize;
                        free = available;
                    }
                    else
                    {
                        driveError = "The data drive is not ready.";
                    }
                }
                catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is ArgumentException)
                {
                    driveError = error.Message;
                }

                using (Process process = Process.GetCurrentProcess())
                {
                    var snapshot = new RuntimeHealthSnapshot(_driveRoot, free, total, driveError, process.WorkingSet64,
                        process.PrivateMemorySize64, GC.GetTotalMemory(false), _log.Health);
                    Volatile.Write(ref _latest, snapshot);

                    long now = Stopwatch.GetTimestamp();
                    if (!_lastReport.HasValue || now - _lastReport.Value >= Stopwatch.Frequency * 600L)
                    {
                        _lastReport = now;
                        _log.System($"[Health] driveFreeBytes={free?.ToString() ?? "unavailable"}, " +
                            $"workingSetBytes={snapshot.WorkingSetBytes}, privateBytes={snapshot.PrivateBytes}, " +
                            $"managedBytes={snapshot.ManagedBytes}, logDegraded={snapshot.Log.IsStorageDegraded}, " +
                            $"logDropped={snapshot.Log.Dropped}, logFailed={snapshot.Log.Failed}, " +
                            $"logFallbackWritten={snapshot.Log.FallbackWritten}");
                    }
                }
                _log.ClearAlarm("Health.SampleFailed");
            }
            catch (Exception error)
            {
                _log.SetAlarmActive("Health.SampleFailed", "[Health] Runtime sample failed: " + error.Message);
            }
            finally
            {
                Volatile.Write(ref _sampling, 0);
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            using (var stopped = new ManualResetEvent(false))
            {
                if (_timer.Dispose(stopped))
                {
                    stopped.WaitOne();
                }
            }
        }
    }
}
