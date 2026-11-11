using System;
using System.Threading;
using System.Threading.Tasks;

namespace OmniSight.Base.Devices.Cameras
{
    public interface ICameraDevice : IDevice
    {
        // One read at a time. The caller owns the returned Mono8 pixel array.
        Task<CameraFrame> WaitFrameAsync(CancellationToken cancellationToken);
    }

    public sealed class CameraFrame
    {
        internal CameraFrame(int width, int height, long frameId, DateTime capturedUtc, byte[] pixels)
        {
            Width = width;
            Height = height;
            FrameId = frameId;
            CapturedUtc = capturedUtc;
            Pixels = pixels;
        }

        public int Width { get; }
        public int Height { get; }
        public long FrameId { get; }
        public DateTime CapturedUtc { get; }
        public byte[] Pixels { get; }
    }
}
