using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using OmniSight.Base.Application;
using OmniSight.Base.Devices;
using OmniSight.Base.Logging;
using OmniSight.Base.UI;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        string root = Path.Combine(Path.GetTempPath(), "OmniSight.Base.Checks-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            using (var log = new LogManager(root))
            {
                InitializationFailureReleasesInReverseOrder(log);
                StartAndShutdown(log);
                StartFailureRollsBackInReverseOrder(log);
                UnreadyDeviceBlocksStart(log);
                DeviceFaultStopsTheRun(log);
                StopFailureRemainsFaulted(log);
            }
            Check(Directory.EnumerateFiles(root, "*_Alarm.log", SearchOption.AllDirectories).Any(path =>
                File.ReadAllText(path).Contains("Machine initialization failed.")), "alarm written");
            MainFormDefaults();
            StartupFormDefaults();
            LoggingChecks.Run(root);
            PathChecks.Run(root);
            Console.WriteLine("PASS: machine lifecycle (6), logging (3), storage layout and settings migration, and MainForm/StartupForm layout.");
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    private static void InitializationFailureReleasesInReverseOrder(LogManager log)
    {
        var calls = new List<string>();
        using (var machine = new Machine(new[]
        {
            new FakeDevice("one", calls),
            new FakeDevice("two", calls) { FailInitialize = true },
            new FakeDevice("three", calls)
        }, log))
        {
            ExpectInvalidOperation(() => machine.Initialize(CancellationToken.None));
            Check(machine.Current.State == MachineState.Faulted, "initialization fault state");
            Check(calls.SequenceEqual(new[]
            {
                "initialize one", "initialize two", "dispose three", "dispose two", "dispose one"
            }), "initialization rollback order");
        }
    }

    private static void StartAndShutdown(LogManager log)
    {
        var calls = new List<string>();
        using (var machine = new Machine(new[] { new FakeDevice("one", calls) }, log))
        {
            machine.Initialize(CancellationToken.None);
            Check(machine.Current.State == MachineState.Ready && machine.Current.Mode == MachineMode.Manual &&
                  machine.Current.RunStatus == MachineRunStatus.Stopped, "default machine state");
            machine.ChangeMode(MachineMode.Auto);
            Check(machine.Start(CancellationToken.None), "first start");
            Check(!machine.Start(CancellationToken.None), "duplicate start");
            ExpectInvalidOperation(() => machine.ChangeMode(MachineMode.Manual));
        }

        Check(calls.SequenceEqual(new[] { "initialize one", "start one", "stop one", "dispose one" }),
              "shutdown while running");
    }

    private static void StartFailureRollsBackInReverseOrder(LogManager log)
    {
        var calls = new List<string>();
        using (var machine = new Machine(new[]
        {
            new FakeDevice("one", calls),
            new FakeDevice("two", calls) { FailStart = true }
        }, log))
        {
            machine.Initialize(CancellationToken.None);
            ExpectInvalidOperation(() => machine.Start(CancellationToken.None));
            Check(machine.Current.State == MachineState.Faulted, "start fault state");
            Check(calls.Skip(2).SequenceEqual(new[] { "start one", "start two", "stop two", "stop one" }),
                  "partial start rollback order");
        }
    }

    private static void UnreadyDeviceBlocksStart(LogManager log)
    {
        var calls = new List<string>();
        var device = new FakeDevice("one", calls);
        using (var machine = new Machine(new[] { device }, log))
        {
            machine.Initialize(CancellationToken.None);
            device.IsReady = false;
            ExpectInvalidOperation(() => machine.Start(CancellationToken.None));
            Check(!calls.Contains("start one") && machine.Current.State == MachineState.Faulted,
                  "unready required device never starts");
        }
    }

    private static void DeviceFaultStopsTheRun(LogManager log)
    {
        var calls = new List<string>();
        var device = new FakeDevice("one", calls);
        using (var machine = new Machine(new[] { device }, log))
        {
            machine.Initialize(CancellationToken.None);
            machine.Start(CancellationToken.None);
            device.Disconnect();
            Check(calls.Contains("stop one") && machine.Current.State == MachineState.Faulted &&
                  machine.Current.RunStatus == MachineRunStatus.Stopped, "disconnect stops running machine");
        }
    }

    private static void StopFailureRemainsFaulted(LogManager log)
    {
        var calls = new List<string>();
        using (var machine = new Machine(new[] { new FakeDevice("one", calls) { FailStop = true } }, log))
        {
            machine.Initialize(CancellationToken.None);
            machine.Start(CancellationToken.None);
            Check(!machine.Stop(), "stop failure is returned");
            Check(machine.Current.State == MachineState.Faulted && machine.Current.RunStatus == MachineRunStatus.Unknown,
                  "stop failure is not reported as stopped");
        }
        Check(calls.Count(call => call == "stop one") == 2 && calls.Last() == "dispose one",
              "shutdown retries stop and releases the device");
    }

    private static void MainFormDefaults()
    {
        using (var form = new MainForm())
        {
            Check(form.ClientSize == new Size(1920, 1080) && form.BackColor == Color.Black &&
                  form.FormBorderStyle == FormBorderStyle.None && form.Controls.Count == 0, "blank main form");
        }
    }

    private static void StartupFormDefaults()
    {
        using (var form = new StartupForm())
        {
            Control hero = form.Controls["pnlHero"];
            Control panel = form.Controls["pnlProgress"];
            var progress = (StartupProgressBar)panel.Controls["progressBoot"];
            Check(form.ClientSize == new Size(650, 420) && form.FormBorderStyle == FormBorderStyle.None &&
                  hero is StartupBrandPanel && hero.Height == 294 && panel.BackColor == Color.FromArgb(47, 47, 47),
                  "reference splash layout");
            Check(progress.Bounds == new Rectangle(25, 82, 599, 9) && progress.IsMarquee &&
                  panel.Controls["btnCancel"].Text == "취소", "reference progress and cancel controls");
        }
    }

    private static void ExpectInvalidOperation(Action action)
    {
        try
        {
            action();
        }
        catch (InvalidOperationException)
        {
            return;
        }
        throw new Exception("Expected InvalidOperationException.");
    }

    private static void Check(bool condition, string name)
    {
        if (!condition)
        {
            throw new Exception("Failed: " + name);
        }
    }

    private sealed class FakeDevice : IDevice
    {
        private readonly List<string> _calls;
        public FakeDevice(string name, List<string> calls)
        {
            Name = name;
            _calls = calls;
        }

        public string Name { get; }
        public bool IsReady { get; set; }
        public bool FailInitialize { get; set; }
        public bool FailStart { get; set; }
        public bool FailStop { get; set; }
        public event Action<IDevice, Exception> Faulted;

        public void Initialize(CancellationToken token)
        {
            _calls.Add("initialize " + Name);
            if (FailInitialize)
            {
                throw new InvalidOperationException("Initialization failed.");
            }
            IsReady = true;
        }

        public void Start(CancellationToken token)
        {
            _calls.Add("start " + Name);
            if (FailStart)
            {
                throw new InvalidOperationException("Start failed.");
            }
        }

        public void Stop()
        {
            _calls.Add("stop " + Name);
            if (FailStop)
            {
                throw new InvalidOperationException("Stop failed.");
            }
        }

        public void Disconnect()
        {
            IsReady = false;
            Faulted?.Invoke(this, new IOException("Disconnected."));
        }

        public void Dispose()
        {
            _calls.Add("dispose " + Name);
        }
    }
}
