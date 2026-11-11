using System;
using System.Threading;

namespace OmniSight.Base.Devices
{
    public interface IDevice : IDisposable
    {
        string Name { get; }
        bool IsReady { get; }

        // Raise operational faults outside device locks. A subscriber can stop this device.
        event Action<IDevice, Exception> Faulted;

        void Initialize(CancellationToken cancellationToken);
        void Start(CancellationToken cancellationToken);
        void Stop();
    }
}
