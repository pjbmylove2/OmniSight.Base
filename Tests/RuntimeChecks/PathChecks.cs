using System;
using System.IO;
using System.Reflection;
using OmniSight.Base.UI;

internal static class PathChecks
{
    public static void Run(string root)
    {
        const string appId = "Starter.Project";
        const string company = "Example Company";
        string storage = Path.Combine(root, "storage");
        string previous = Path.Combine(root, "previous");
        Assembly assembly = typeof(StartupForm).Assembly;
        Type pathsType = assembly.GetType("OmniSight.Base.Configuration.PathManager", throwOnError: true);
        object paths = CreatePaths(pathsType, appId, company, storage, previous);
        string appRoot = Path.Combine(storage, company, appId);
        Check((string)pathsType.GetProperty("RootPath").GetValue(paths) == appRoot, "company and assembly path");

        MethodInfo create = pathsType.GetMethod("CreateDirectories");
        create.Invoke(paths, null);
        foreach (string folder in new[]
        {
            "SYSTEM", "MODEL", "LOG", "DATA", @"DEVICE\CAMERA", @"DEVICE\PROCESS", @"DEVICE\ILLUMINATION",
            @"DEVICE\COMMUNICATION\SHAREFOLDER", @"DEVICE\COMMUNICATION\SOCKET", @"DEVICE\COMMUNICATION\PLC"
        })
        {
            Check(Directory.Exists(Path.Combine(appRoot, folder)), "missing " + folder);
        }

        string previousConfig = (string)pathsType.GetProperty("PreviousConfigPath").GetValue(paths);
        Check(previousConfig == Path.Combine(previous, appId, "Config"), "previous settings path");
        Directory.CreateDirectory(previousConfig);
        string original = Path.Combine(previousConfig, "SystemGeneral.json");
        string destination = Path.Combine(appRoot, "SYSTEM", "SystemGeneral.json");
        const string originalJson = "{\"WriteDebugLog\":false}";
        const string editedJson = "{\"WriteDebugLog\":true}";
        File.WriteAllText(original, originalJson);

        Type config = assembly.GetType("OmniSight.Base.Configuration.JsonConfigProvider", throwOnError: true);
        Type settings = assembly.GetType("OmniSight.Base.Configuration.SystemSettings", throwOnError: true);
        MethodInfo load = config.GetMethod("LoadOrCreate").MakeGenericMethod(settings);
        var arguments = new[] { destination, Activator.CreateInstance(settings), previousConfig };
        load.Invoke(null, arguments);
        Check(File.ReadAllText(destination) == originalJson, "first run copies the existing settings");

        File.WriteAllText(destination, editedJson);
        create.Invoke(paths, null);
        load.Invoke(null, arguments);
        Check(File.ReadAllText(destination) == editedJson && File.ReadAllText(original) == originalJson,
            "restart keeps both settings files");

        RejectFolderName(pathsType, appId, @"..\escape", storage, previous);
        RejectFolderName(pathsType, "NUL.txt", company, storage, previous);
    }

    private static object CreatePaths(Type type, string appId, string company, string storage, string previous)
    {
        return Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.NonPublic, null,
            new object[] { appId, company, storage, previous }, null);
    }

    private static void RejectFolderName(Type type, string appId, string company, string storage, string previous)
    {
        try
        {
            CreatePaths(type, appId, company, storage, previous);
        }
        catch (TargetInvocationException error) when (error.InnerException is ArgumentException)
        {
            return;
        }

        throw new InvalidOperationException("Invalid folder name was accepted.");
    }

    private static void Check(bool condition, string name)
    {
        if (!condition)
        {
            throw new InvalidOperationException("Failed: " + name);
        }
    }
}
