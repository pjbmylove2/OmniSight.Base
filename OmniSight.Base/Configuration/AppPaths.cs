using System;
using System.IO;

namespace OmniSight.Base.Configuration
{
    internal sealed class AppPaths
    {
        public AppPaths(string appId)
        {
            if (string.IsNullOrWhiteSpace(appId) || appId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                appId != appId.Trim() || appId.EndsWith(".", StringComparison.Ordinal))
            {
                throw new ArgumentException("AssemblyName must be a valid Windows directory name.", nameof(appId));
            }

            Root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), appId);
            Config = Path.Combine(Root, "Config");
            Logs = Path.Combine(Root, "Logs");
            Data = Path.Combine(Root, "Data");
        }

        public string Root { get; }
        public string Config { get; }
        public string Logs { get; }
        public string Data { get; }

        public void CreateDirectories()
        {
            Directory.CreateDirectory(Config);
            Directory.CreateDirectory(Logs);
            Directory.CreateDirectory(Data);
        }
    }
}
