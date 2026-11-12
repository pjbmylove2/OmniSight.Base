using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using OmniSight.Base.Logging;

internal static class LoggingChecks
{
    private static readonly TimeSpan FlushTimeout = TimeSpan.FromSeconds(10);

    public static void Run(string root)
    {
        ConcurrentWrites(root);
        StorageFailureAndRecovery(root);
        PrioritySurvivesOverflow(root);
    }

    private static void ConcurrentWrites(string root)
    {
        string directory = Path.Combine(root, "bulk");
        using (var log = new LogManager(directory))
        {
            Parallel.For(1, 3, cam =>
            {
                for (int i = 0; i < 3000; i++)
                {
                    log.Write(LogCategory.DEBUG, "한글 frame=" + i, "LOT/" + cam, cam);
                }
            });
            log.WriteDebugLog = false;
            log.Debug("disabled marker");
            log.Write(LogCategory.RESULT, "result marker", "LOT/1", 1);
            log.Alarm("failure marker", new IOException("first line\r\nsecond line"));
            log.SetAlarmActive("Test.Persistent", "active marker");
            log.SetAlarmActive("Test.Persistent", "suppressed alarm");
            log.ClearAlarm("Test.Persistent");
            log.SetAlarmActive("Test.Persistent", "reactivated marker");
            log.WriteSystemThrottled("Test.Throttle", "first system marker", TimeSpan.FromHours(1));
            log.WriteSystemThrottled("Test.Throttle", "suppressed system", TimeSpan.FromHours(1));

            Check(log.Flush(FlushTimeout), "flush both queues");
            LogManagerHealth health = log.Health;
            Check(health.Accepted == health.Written && health.Dropped == 0 &&
                  health.Failed == 0 && health.QueuedBytes == 0, "all accepted rows accounted for");

            string[] debug = Directory.GetFiles(directory, "*_Debug.log", SearchOption.AllDirectories);
            Check(debug.Length == 2 && debug.All(path => path.Contains("LOT_") &&
                  ReadLog(path).Split('\n').Count(line => line.Contains("한글 frame=")) == 3000 &&
                  !ReadLog(path).Contains("disabled marker")), "Unicode, CAM/LOT routing and debug filtering");
            Check(ReadCategory(directory, "Result").Contains("result marker"), "result category routing");
            string alarms = ReadCategory(directory, "Alarm");
            Check(alarms.Contains("first line / second line") && alarms.Contains("active marker") &&
                  alarms.Contains("reactivated marker") && !alarms.Contains("suppressed alarm"), "exception and alarm state");
            string system = ReadCategory(directory, "System");
            Check(system.Contains("first system marker") && !system.Contains("suppressed system"), "system log throttling");

            log.System("dispose marker");
        }

        Check(ReadCategory(directory, "System").Contains("dispose marker"), "dispose drains pending rows");
    }

    private static void StorageFailureAndRecovery(string root)
    {
        string blocked = Path.Combine(root, "blocked-primary");
        string fallback = Path.Combine(root, "fallback");
        File.WriteAllText(blocked, "blocks directory creation");
        using (var log = new LogManager(blocked, fallback))
        {
            log.Alarm("fallback marker");
            Check(log.Flush(FlushTimeout) && log.Health.IsStorageDegraded && log.Health.FallbackWritten == 1 &&
                  log.Health.Failed == 0 && log.Health.QueuedBytes == 0, "fallback accounts for the row");
            Check(ReadCategory(log.FallbackRoot, "Alarm").Contains("fallback marker"), "fallback row is readable");

            File.Delete(blocked);
            Directory.CreateDirectory(blocked);
            Check(SpinWait.SpinUntil(() => !log.Health.IsStorageDegraded, 7000), "primary recovery detected");
            log.Alarm("primary marker");
            Check(log.Flush(FlushTimeout) && ReadCategory(blocked, "Alarm").Contains("primary marker"),
                  "writes resume in the primary directory");
        }

        string blockedFallback = Path.Combine(root, "blocked-fallback");
        File.WriteAllText(blockedFallback, "blocks directory creation");
        using (var log = new LogManager(blockedFallback, blockedFallback))
        {
            log.Alarm("both paths fail");
            Check(log.Flush(FlushTimeout) && log.Health.IsStorageDegraded && log.Health.Failed == 1 &&
                  log.Health.LastError.Length > 0 && log.Health.QueuedBytes == 0, "failure of both directories is counted");
        }
    }

    private static void PrioritySurvivesOverflow(string root)
    {
        string directory = Path.Combine(root, "overflow");
        using (var log = (LogManager)Activator.CreateInstance(typeof(LogManager), BindingFlags.Instance | BindingFlags.NonPublic,
            null, new object[] { directory, Path.Combine(root, "overflow-fallback"), 1, 4 }, null))
        {
            string message = new string('x', 2000);
            Parallel.For(0, 10000, i => log.Debug(message));
            log.Alarm("priority marker");
            Check(SpinWait.SpinUntil(() => log.Health.QueueDepth == 0, 5000) &&
                  log.Flush(FlushTimeout), "saturated queue drains");
            Check(log.Health.Dropped > 0 && log.Health.QueuedBytes == 0 &&
                  ReadCategory(directory, "Alarm").Contains("priority marker"), "alarm has a separate queue");
        }
    }

    private static string ReadCategory(string directory, string category)
    {
        return string.Join("\n", Directory.GetFiles(directory, "*_" + category + ".log", SearchOption.AllDirectories)
            .Select(ReadLog));
    }

    private static string ReadLog(string path)
    {
        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
        using (var reader = new StreamReader(stream))
        {
            Check(stream.ReadByte() == 0xEF && stream.ReadByte() == 0xBB && stream.ReadByte() == 0xBF, "UTF-8 BOM");
            stream.Position = 0;
            return reader.ReadToEnd();
        }
    }

    private static void Check(bool condition, string name)
    {
        if (!condition)
        {
            throw new InvalidOperationException("Failed: " + name);
        }
    }
}
