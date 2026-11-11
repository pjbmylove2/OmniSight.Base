using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace OmniSight.Base.Logging
{
    public sealed class LogManager : IDisposable
    {
        private readonly object _sync = new object();
        private readonly string _directory;
        private StreamWriter _writer;
        private string _logDate;
        private bool _disposed;

        public LogManager(string directory)
        {
            _directory = directory ?? throw new ArgumentNullException(nameof(directory));
        }

        public bool WriteDebugLog { get; set; } = true;

        public void System(string message) => Write("SYSTEM", message);
        public void Alarm(string message, Exception error = null) => Write("ALARM", message, error);

        public void Debug(string message)
        {
            if (WriteDebugLog)
            {
                Write("DEBUG", message);
            }
        }

        private void Write(string level, string message, Exception error = null)
        {
            lock (_sync)
            {
                if (_disposed)
                {
                    return;
                }

                DateTime now = DateTime.UtcNow;
                string date = now.ToString("yyyy-MM-dd", global::System.Globalization.CultureInfo.InvariantCulture);
                string entry = $"{now:O} [{level}] {message}";
                try
                {
                    if (_writer == null || date != _logDate)
                    {
                        CloseWriter();
                        _writer = new StreamWriter(Path.Combine(_directory, date + ".log"), true, new UTF8Encoding(false));
                        _logDate = date;
                    }

                    _writer.WriteLine(entry);
                    if (error != null)
                    {
                        _writer.WriteLine(error);
                    }

                    // This logger records lifecycle and faults. Keep frame-by-frame data out of this path.
                    _writer.Flush();
                }
                catch (Exception failure) when (failure is IOException || failure is UnauthorizedAccessException)
                {
                    Trace.TraceError("{0}\n{1}\n{2}", entry, error, failure);
                    CloseWriter();
                }
            }
        }

        private void CloseWriter()
        {
            try
            {
                _writer?.Dispose();
            }
            catch (IOException error)
            {
                Trace.TraceError("Log close failed: {0}", error);
            }
            finally
            {
                _writer = null;
            }
        }

        public void Dispose()
        {
            lock (_sync)
            {
                _disposed = true;
                CloseWriter();
            }
        }
    }
}
