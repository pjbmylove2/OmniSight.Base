using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using OmniSight.Base.Devices;
using OmniSight.Base.Logging;

namespace OmniSight.Base.Application
{
    public enum MachineState { Created, Initializing, Ready, Faulted, Disposed }
    public enum MachineMode { Manual, Auto }
    public enum MachineRunStatus { Stopped, Running, Unknown }

    public sealed class MachineSnapshot
    {
        internal MachineSnapshot(MachineState state, MachineMode mode, MachineRunStatus runStatus)
        {
            State = state;
            Mode = mode;
            RunStatus = runStatus;
        }

        public MachineState State { get; }
        public MachineMode Mode { get; }
        public MachineRunStatus RunStatus { get; }
    }

    public sealed class Machine : IDisposable
    {
        private readonly object _sync = new object();
        private readonly LogManager _log;
        private MachineSnapshot _current = new MachineSnapshot(MachineState.Created, MachineMode.Manual, MachineRunStatus.Stopped);
        private bool _devicesReleased;

        // The machine owns the supplied devices. The site supplies the devices required for its sequence.
        public Machine(IEnumerable<IDevice> requiredDevices, LogManager log)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
            RequiredDevices = (requiredDevices ?? throw new ArgumentNullException(nameof(requiredDevices))).ToList().AsReadOnly();
        }

        public IReadOnlyList<IDevice> RequiredDevices { get; }
        public MachineSnapshot Current => Volatile.Read(ref _current);
        public event EventHandler StateChanged;

        public void Initialize(CancellationToken cancellationToken)
        {
            try
            {
                lock (_sync)
                {
                    RequireState(MachineState.Created);
                    SetState(MachineState.Initializing, Current.RunStatus);
                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        foreach (IDevice device in RequiredDevices)
                        {
                            device.Faulted += OnDeviceFaulted;
                            device.Initialize(cancellationToken);
                            if (!device.IsReady)
                            {
                                throw new InvalidOperationException($"{device.Name} is not ready.");
                            }
                        }

                        cancellationToken.ThrowIfCancellationRequested();
                        SetState(MachineState.Ready, MachineRunStatus.Stopped);
                    }
                    catch (Exception error)
                    {
                        SetState(MachineState.Faulted, MachineRunStatus.Stopped);
                        _log.Alarm("Machine initialization failed.", error);
                        ReleaseDevices();
                        throw;
                    }
                }
            }
            finally
            {
                NotifyStateChanged();
            }
        }

        public bool Start(CancellationToken cancellationToken)
        {
            try
            {
                lock (_sync)
                {
                    RequireState(MachineState.Ready);
                    if (Current.RunStatus == MachineRunStatus.Running)
                    {
                        return false;
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                    int attempted = 0;
                    try
                    {
                        foreach (IDevice device in RequiredDevices)
                        {
                            if (!device.IsReady)
                            {
                                throw new InvalidOperationException($"{device.Name} is not ready.");
                            }
                        }

                        foreach (IDevice device in RequiredDevices)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            attempted++;
                            device.Start(cancellationToken);
                        }

                        cancellationToken.ThrowIfCancellationRequested();
                        SetState(MachineState.Ready, MachineRunStatus.Running);
                        return true;
                    }
                    catch (Exception error)
                    {
                        bool stopped = StopDevices(attempted);
                        bool canceled = error is OperationCanceledException && cancellationToken.IsCancellationRequested;
                        SetState(canceled && stopped ? MachineState.Ready : MachineState.Faulted,
                                 stopped ? MachineRunStatus.Stopped : MachineRunStatus.Unknown);
                        if (!canceled)
                        {
                            _log.Alarm("Machine start failed.", error);
                        }
                        throw;
                    }
                }
            }
            finally
            {
                NotifyStateChanged();
            }
        }

        public bool Stop()
        {
            try
            {
                lock (_sync)
                {
                    if (_devicesReleased || Current.RunStatus == MachineRunStatus.Stopped)
                    {
                        return false;
                    }

                    bool stopped = StopDevices(RequiredDevices.Count);
                    SetState(stopped ? Current.State : MachineState.Faulted,
                             stopped ? MachineRunStatus.Stopped : MachineRunStatus.Unknown);
                    return stopped;
                }
            }
            finally
            {
                NotifyStateChanged();
            }
        }

        public void ChangeMode(MachineMode mode)
        {
            if (mode != MachineMode.Manual && mode != MachineMode.Auto)
            {
                throw new ArgumentOutOfRangeException(nameof(mode));
            }

            lock (_sync)
            {
                RequireState(MachineState.Ready);
                if (Current.RunStatus != MachineRunStatus.Stopped)
                {
                    throw new InvalidOperationException("Stop the machine before changing mode.");
                }

                Volatile.Write(ref _current, new MachineSnapshot(Current.State, mode, Current.RunStatus));
                _log.System($"Machine mode: {mode}.");
            }
            NotifyStateChanged();
        }

        private void OnDeviceFaulted(IDevice device, Exception error)
        {
            lock (_sync)
            {
                if (Current.State != MachineState.Ready)
                {
                    return;
                }

                _log.Alarm($"{device.Name} reported a fault.", error);
                bool stopped = Current.RunStatus == MachineRunStatus.Stopped || StopDevices(RequiredDevices.Count);
                SetState(MachineState.Faulted, stopped ? MachineRunStatus.Stopped : MachineRunStatus.Unknown);
            }
            NotifyStateChanged();
        }

        private bool StopDevices(int count)
        {
            bool stopped = true;
            for (int index = count - 1; index >= 0; index--)
            {
                IDevice device = RequiredDevices[index];
                try
                {
                    device.Stop();
                }
                catch (Exception error)
                {
                    stopped = false;
                    _log.Alarm($"Could not stop {device.Name}.", error);
                }
            }
            return stopped;
        }

        private void ReleaseDevices()
        {
            if (_devicesReleased)
            {
                return;
            }

            _devicesReleased = true;
            for (int index = RequiredDevices.Count - 1; index >= 0; index--)
            {
                IDevice device = RequiredDevices[index];
                try
                {
                    device.Faulted -= OnDeviceFaulted;
                    device.Dispose();
                }
                catch (Exception error)
                {
                    _log.Alarm($"Could not release {device.Name}.", error);
                }
            }
        }

        private void RequireState(MachineState state)
        {
            if (Current.State != state)
            {
                throw new InvalidOperationException($"Expected {state}. Current machine state is {Current.State}.");
            }
        }

        private void SetState(MachineState state, MachineRunStatus runStatus)
        {
            Volatile.Write(ref _current, new MachineSnapshot(state, Current.Mode, runStatus));
            _log.System($"Machine: {state} / {Current.Mode} / {runStatus}.");
        }

        // Subscribers read Current and marshal to the UI thread when necessary.
        private void NotifyStateChanged()
        {
            EventHandler handlers = StateChanged;
            if (handlers == null)
            {
                return;
            }

            foreach (EventHandler handler in handlers.GetInvocationList())
            {
                try
                {
                    handler(this, EventArgs.Empty);
                }
                catch (Exception error)
                {
                    _log.Alarm("Machine state subscriber failed.", error);
                }
            }
        }

        public void Dispose()
        {
            lock (_sync)
            {
                if (Current.State == MachineState.Disposed)
                {
                    return;
                }

                bool stopped = Current.RunStatus == MachineRunStatus.Stopped || StopDevices(RequiredDevices.Count);
                ReleaseDevices();
                SetState(MachineState.Disposed, stopped ? MachineRunStatus.Stopped : MachineRunStatus.Unknown);
            }
            NotifyStateChanged();
        }
    }
}
