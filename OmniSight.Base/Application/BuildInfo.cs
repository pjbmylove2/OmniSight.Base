using System.Reflection;

namespace OmniSight.Base.Application
{
    internal static class BuildInfo
    {
        private static readonly Assembly AppAssembly = typeof(BuildInfo).Assembly;

        public static string AppId => AppAssembly.GetName().Name;
        public static string FileVersion => AppAssembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version;
        public static string Configuration => AppAssembly.GetCustomAttribute<AssemblyConfigurationAttribute>().Configuration;
        public static string DisplayText => $"Version {FileVersion} · {Configuration}";
    }
}
