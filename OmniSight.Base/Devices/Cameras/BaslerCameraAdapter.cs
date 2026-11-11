using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Basler.Pylon;
using OmniSight.Base.Configuration;

namespace OmniSight.Base.Devices.Cameras
{
    internal sealed class BaslerCameraAdapter : ICameraDevice
    {
        private const int PollTimeoutMilliseconds = 100;
        private readonly object _sync = new object();
        private readonly string _serialNumber;
        private readonly CameraTrigger _trigger;
        private Camera _camera;
        private int _ready;
        private int _readerActive;
        private volatile bool _running;
        private bool _disposed;
        private int _captureGeneration;
        private bool _softwareTriggerPending;
        private long _frameId;

        public BaslerCameraAdapter(CameraSettings settings)
        {
            _serialNumber = settings.SerialNumber.Trim();
            _trigger = settings.Trigger;
        }

        public string Name => $"Basler {_serialNumber}";
        public bool IsReady => Volatile.Read(ref _ready) == 1;
        public event Action<IDevice, Exception> Faulted;

        public void Initialize(CancellationToken cancellationToken)
        {
            lock (_sync)
            {
                ThrowIfDisposed();
                cancellationToken.ThrowIfCancellationRequested();

                ICameraInfo info = CameraFinder.Enumerate()
                    .SingleOrDefault(item => item[CameraInfoKey.SerialNumber] == _serialNumber);
                if (info == null)
                {
                    throw new IOException($"Basler camera with serial {_serialNumber} was not found.");
                }

                _camera = new Camera(info);
                _camera.Open();
                _camera.Parameters[PLCamera.AcquisitionMode].SetValue(PLCamera.AcquisitionMode.Continuous);
                _camera.Parameters[PLCamera.TriggerSelector].TrySetValue(PLCamera.TriggerSelector.FrameStart);
                _camera.Parameters[PLCamera.TriggerMode].SetValue(
                    _trigger == CameraTrigger.Continuous ? PLCamera.TriggerMode.Off : PLCamera.TriggerMode.On);
                if (_trigger != CameraTrigger.Continuous)
                {
                    _camera.Parameters[PLCamera.TriggerSource].SetValue(
                        _trigger == CameraTrigger.Software ? PLCamera.TriggerSource.Software : PLCamera.TriggerSource.Line1);
                }

                _camera.Parameters[PLCamera.PixelFormat].SetValue(PLCamera.PixelFormat.Mono8);
                cancellationToken.ThrowIfCancellationRequested();
                Volatile.Write(ref _ready, 1);
                _camera.ConnectionLost += OnConnectionLost;
                if (!_camera.IsConnected)
                {
                    Volatile.Write(ref _ready, 0);
                    throw new IOException($"{Name} disconnected during initialization.");
                }
            }
        }

        public void Start(CancellationToken cancellationToken)
        {
            lock (_sync)
            {
                ThrowIfDisposed();
                cancellationToken.ThrowIfCancellationRequested();
                if (!IsReady)
                {
                    throw new InvalidOperationException($"{Name} is not ready.");
                }

                _camera.StreamGrabber.Start(GrabStrategy.OneByOne, GrabLoop.ProvidedByUser);
                _softwareTriggerPending = false;
                Interlocked.Increment(ref _captureGeneration);
                _running = true;
            }
        }

        public Task<CameraFrame> WaitFrameAsync(CancellationToken cancellationToken)
        {
            if (Interlocked.CompareExchange(ref _readerActive, 1, 0) != 0)
            {
                throw new InvalidOperationException("Only one camera read may be pending.");
            }

            int generation = Volatile.Read(ref _captureGeneration);
            return Task.Run(() =>
            {
                try
                {
                    return ReadFrame(cancellationToken, generation);
                }
                catch (Exception error) when (!(error is OperationCanceledException))
                {
                    if (_running && generation == Volatile.Read(ref _captureGeneration) && !cancellationToken.IsCancellationRequested)
                    {
                        ReportFault(error);
                    }
                    throw;
                }
                finally
                {
                    Interlocked.Exchange(ref _readerActive, 0);
                }
            });
        }

        private CameraFrame ReadFrame(CancellationToken token, int generation)
        {
            lock (_sync)
            {
                RequireCapture(token, generation);
                if (_trigger == CameraTrigger.Software && !_softwareTriggerPending)
                {
                    _camera.WaitForFrameTriggerReady(500, TimeoutHandling.ThrowException);
                    RequireCapture(token, generation);
                    _camera.ExecuteSoftwareTrigger();
                    _softwareTriggerPending = true;
                }

                while (true)
                {
                    RequireCapture(token, generation);
                    using (IGrabResult grab = _camera.StreamGrabber.RetrieveResult(PollTimeoutMilliseconds, TimeoutHandling.Return))
                    {
                        if (grab != null)
                        {
                            _softwareTriggerPending = false;
                            RequireCapture(token, generation);
                            return CopyFrame(grab);
                        }
                    }
                }
            }
        }

        private CameraFrame CopyFrame(IGrabResult grab)
        {
            if (!grab.GrabSucceeded)
            {
                throw new IOException($"{Name} grab failed: {grab.ErrorDescription}");
            }

            DateTime capturedUtc = DateTime.UtcNow;
            var pixels = (byte[])grab.PixelData;
            int? stride = grab.ComputeStride();
            if (!stride.HasValue || grab.Width <= 0 || grab.Height <= 0 ||
                stride.Value < grab.Width || (long)stride.Value * grab.Height > pixels.LongLength)
            {
                throw new InvalidDataException("Invalid Basler Mono8 frame dimensions or stride.");
            }

            // Grab buffers belong to pylon until IGrabResult.Dispose. Return an independent, tightly packed copy.
            var ownedPixels = new byte[checked(grab.Width * grab.Height)];
            if (stride.Value == grab.Width)
            {
                Buffer.BlockCopy(pixels, 0, ownedPixels, 0, ownedPixels.Length);
            }
            else
            {
                for (int row = 0; row < grab.Height; row++)
                {
                    Buffer.BlockCopy(pixels, row * stride.Value, ownedPixels, row * grab.Width, grab.Width);
                }
            }

            return new CameraFrame(grab.Width, grab.Height, ++_frameId, capturedUtc, ownedPixels);
        }

        private void RequireCapture(CancellationToken token, int generation)
        {
            token.ThrowIfCancellationRequested();
            if (!_running || !IsReady || generation != _captureGeneration)
            {
                throw new OperationCanceledException($"{Name} capture is stopped.");
            }
        }

        private void OnConnectionLost(object sender, EventArgs e)
        {
            ReportFault(new IOException($"{Name} connection lost."));
        }

        private void ReportFault(Exception error)
        {
            _running = false;
            if (Interlocked.Exchange(ref _ready, 0) == 1)
            {
                // Leave the SDK callback before notifying the machine, which may call Stop.
                ThreadPool.QueueUserWorkItem(_ => Faulted?.Invoke(this, error));
            }
        }

        public void Stop()
        {
            // Release a pending read before entering the lock. Stop the stream only after the reader leaves it.
            _running = false;
            lock (_sync)
            {
                if (_camera != null && _camera.StreamGrabber.IsGrabbing)
                {
                    _camera.StreamGrabber.Stop();
                }
            }
        }

        public void Dispose()
        {
            _running = false;
            lock (_sync)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                Volatile.Write(ref _ready, 0);
                try
                {
                    Stop();
                }
                finally
                {
                    if (_camera != null)
                    {
                        _camera.ConnectionLost -= OnConnectionLost;
                        _camera.Dispose();
                        _camera = null;
                    }
                }
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(Name);
            }
        }
    }
}
