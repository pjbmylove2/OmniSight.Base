using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using OmniSight.Base.Application;
using OmniSight.Base.Configuration;
using OmniSight.Base.Logging;
using OmniSight.Base.UI.Utils;

internal static class BaseSystemChecks
{
    public static void Run(string root)
    {
        ConfigurationSave(root);
        InputValidation();
        RuntimeHealth(root);
    }

    private static void ConfigurationSave(string root)
    {
        string directory = Path.Combine(root, "settings");
        string path = Path.Combine(directory, "settings.json");
        var settings = new JsonConfigProvider<SampleSettings>(path, Validate);
        var rejectedDefaults = new JsonConfigProvider<SampleSettings>(Path.Combine(directory, "rejected.json"),
            _ => throw new ArgumentOutOfRangeException("Limit"));
        Expect<ArgumentOutOfRangeException>(() => rejectedDefaults.Load());
        Check(!File.Exists(Path.Combine(directory, "rejected.json")), "invalid defaults never become a configuration file");

        SampleSettings initial = settings.Load();
        initial.Limit = -1;
        Check(settings.GetClone().Limit == 20, "loaded values are detached from the snapshot");

        string original = File.ReadAllText(path);
        SampleSettings edited = settings.GetClone();
        edited.Limit = 55;
        settings.ApplyAndSave(edited);
        edited.Limit = 77;
        string saved = File.ReadAllText(path);
        Check(settings.GetClone().Limit == 55 && File.ReadAllText(path + ".bak") == original,
            "atomic save preserves the previous file and publishes an independent snapshot");
        Check(new JsonConfigProvider<SampleSettings>(path, Validate).Load().Limit == 55, "saved configuration round trip");

        Expect<ArgumentOutOfRangeException>(() => settings.ApplyAndSave(new SampleSettings { Limit = -5 }));
        using (File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            Expect<IOException>(() => settings.ApplyAndSave(new SampleSettings { Limit = 60 }));
        }
        Check(settings.GetClone().Limit == 55 && File.ReadAllText(path) == saved &&
              File.ReadAllText(path + ".bak") == original && Directory.GetFiles(directory, "*.tmp").Length == 0,
            "invalid and failed writes keep the prior file, backup and in-memory settings");

        var stale = new JsonConfigProvider<SampleSettings>(path, Validate);
        stale.Load();
        settings.ApplyAndSave(new SampleSettings { Limit = 60 });
        Expect<IOException>(() => stale.ApplyAndSave(new SampleSettings { Limit = 75 }));
        Check(stale.GetClone().Limit == 55 && stale.Load().Limit == 60 && File.ReadAllText(path + ".bak") == saved,
            "stale editor cannot overwrite a newer configuration");
    }

    private static void InputValidation()
    {
        CultureInfo previousCulture = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("pl-PL");
            using (var box = new TextBox { Text = "-12,5" })
            {
                InputValidator.RevertInvalidNumericPaste(box, allowDecimal: true, allowNegative: true);
                box.TextChanged += (sender, args) =>
                    InputValidator.RevertInvalidNumericPaste(sender, allowDecimal: true, allowNegative: true);
                Check(box.CheckDoubleRange("Pressure", -13, 0).IsValid, "Windows decimal separator is accepted");
                box.SelectionStart = 3;
                box.SelectionLength = 1;
                var replaceSeparator = new KeyPressEventArgs('.');
                InputValidator.EnforceNumericInput(box, replaceSeparator, allowDecimal: true, allowNegative: true);
                Check(!replaceSeparator.Handled, "a selected decimal separator can be replaced");

                box.SelectionStart = box.TextLength;
                var duplicateSeparator = new KeyPressEventArgs('.');
                InputValidator.EnforceNumericInput(box, duplicateSeparator, allowDecimal: true, allowNegative: true);
                Check(duplicateSeparator.Handled, "a second decimal separator is rejected");
                box.Text = "-1-23";
                Check(box.Text == "-12,5", "invalid paste keeps the previous value");
                box.Text = "-12.5";
                Check(box.CheckDoubleRange("Pressure", -13, 0).IsValid &&
                      !box.CheckDoubleRange("Pressure", -12, 0).IsValid, "dot separator and inclusive limits");
            }

            using (var box = new TextBox { Text = "NaN" })
            {
                Check(!box.CheckDoubleRange("Pressure", -13, 0).IsValid, "NaN is rejected");
                box.Text = "1e2";
                Check(!box.CheckDoubleRange("Pressure", 0, 100).IsValid, "exponent notation is rejected");
                box.Text = "2147483648";
                Check(!box.CheckIntRange("Count", int.MinValue, int.MaxValue).IsValid, "overflowing integer is rejected");
                box.Text = "10";
                Check(box.CheckIntRange("Count", 10, 10).IsValid, "integer endpoints are included");
                box.Text = "모델1-A";
                InputValidator.RevertInvalidTextPaste(box, allowNumbers: false, allowSpecialChars: false);
                Check(box.Text == "모델A", "text paste observes the character restrictions");
            }
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = previousCulture;
        }
    }

    private static void RuntimeHealth(string root)
    {
        string primary = Path.Combine(root, "blocked-monitor-log");
        File.WriteAllText(primary, "deliberately blocks directory creation");
        using (var log = new LogManager(primary, Path.Combine(root, "monitor-fallback")))
        {
            log.Alarm("Verify degraded storage");
            Check(log.Flush(TimeSpan.FromSeconds(5)) && log.Health.IsStorageDegraded, "degraded logger test fixture");
            var monitor = new RuntimeHealthMonitor(root, log, TimeSpan.FromMilliseconds(100));
            try
            {
                Check(SpinWait.SpinUntil(() => monitor.Latest != null, 5000), "first health sample");
                RuntimeHealthSnapshot sample = monitor.Latest;
                Check(sample.DriveFreeBytes >= 0 && sample.DriveTotalBytes > 0 && sample.DriveError == null &&
                      sample.WorkingSetBytes > 0 && sample.PrivateBytes > 0 && sample.ManagedBytes > 0,
                    "disk and memory measurements are present");
                Check(sample.Log.IsStorageDegraded && sample.Log.FallbackWritten > 0 && sample.Log.LastError.Length > 0,
                    "the same snapshot includes logger degradation and fallback status");
                Check(SpinWait.SpinUntil(() => monitor.Latest.CollectedAtUtc > sample.CollectedAtUtc, 5000),
                    "health is sampled periodically");
            }
            finally
            {
                monitor.Dispose();
            }

            RuntimeHealthSnapshot stopped = monitor.Latest;
            Thread.Sleep(150);
            Check(ReferenceEquals(monitor.Latest, stopped), "disposed monitor has no later callbacks");
        }
    }

    private static void Validate(SampleSettings settings)
    {
        if (settings.Limit < 0 || settings.Limit > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(settings.Limit));
        }
    }

    private static void Expect<T>(Action action) where T : Exception
    {
        try
        {
            action();
        }
        catch (T)
        {
            return;
        }

        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }

    private static void Check(bool condition, string name)
    {
        if (!condition)
        {
            throw new InvalidOperationException("Failed: " + name);
        }
    }

    public sealed class SampleSettings
    {
        public int Limit { get; set; } = 20;
    }
}
