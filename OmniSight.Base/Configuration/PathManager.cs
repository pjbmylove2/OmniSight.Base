using System;
using System.IO;

namespace OmniSight.Base.Configuration
{
    /// <summary>앱의 저장 경로를 계산하고 첫 실행에 필요한 폴더를 생성합니다.</summary>
    internal sealed class PathManager
    {
        private const string CompanyFolderVariable = "OMNISIGHT_COMPANY_FOLDER";

        public PathManager(string appId)
            : this(appId, ReadCompanyFolder(), @"D:\",
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData))
        {
        }

        internal PathManager(string appId, string companyFolder, string storageRoot, string previousAppData)
        {
            ValidateFolderName(appId, "AssemblyName");
            ValidateFolderName(companyFolder, CompanyFolderVariable);

            RootPath = Path.Combine(storageRoot, companyFolder, appId);
            SystemPath = Path.Combine(RootPath, "SYSTEM");
            ModelPath = Path.Combine(RootPath, "MODEL");
            LogPath = Path.Combine(RootPath, "LOG");
            DataPath = Path.Combine(RootPath, "DATA");

            string devicePath = Path.Combine(RootPath, "DEVICE");
            CameraDevicePath = Path.Combine(devicePath, "CAMERA");
            ProcessDevicePath = Path.Combine(devicePath, "PROCESS");
            IlluminationDevicePath = Path.Combine(devicePath, "ILLUMINATION");

            string communicationPath = Path.Combine(devicePath, "COMMUNICATION");
            ShareFolderCommPath = Path.Combine(communicationPath, "SHAREFOLDER");
            SocketCommPath = Path.Combine(communicationPath, "SOCKET");
            PlcCommPath = Path.Combine(communicationPath, "PLC");

            PreviousConfigPath = Path.Combine(previousAppData, appId, "Config");
            FallbackLogPath = Path.Combine(Path.GetTempPath(), appId, "LogFallback");
        }

        public string RootPath { get; }
        public string SystemPath { get; }
        public string ModelPath { get; }
        public string LogPath { get; }
        public string DataPath { get; }
        public string CameraDevicePath { get; }
        public string ProcessDevicePath { get; }
        public string IlluminationDevicePath { get; }
        public string ShareFolderCommPath { get; }
        public string SocketCommPath { get; }
        public string PlcCommPath { get; }
        public string PreviousConfigPath { get; }
        public string FallbackLogPath { get; }

        public void CreateDirectories()
        {
            string[] directories =
            {
                SystemPath, ModelPath, LogPath, DataPath,
                CameraDevicePath, ProcessDevicePath, IlluminationDevicePath,
                ShareFolderCommPath, SocketCommPath, PlcCommPath
            };

            foreach (string directory in directories)
            {
                Directory.CreateDirectory(directory);
            }
        }

        private static string ReadCompanyFolder()
        {
            string company = Environment.GetEnvironmentVariable(CompanyFolderVariable);
            if (string.IsNullOrWhiteSpace(company))
            {
                company = Environment.GetEnvironmentVariable(CompanyFolderVariable, EnvironmentVariableTarget.User);
            }

            return string.IsNullOrWhiteSpace(company) ? "Company" : company;
        }

        private static void ValidateFolderName(string name, string source)
        {
            if (string.IsNullOrEmpty(name) || !string.Equals(name, SafePathSegment.Sanitize(name, string.Empty), StringComparison.Ordinal))
            {
                throw new ArgumentException(source + " must be a valid Windows folder name.", source);
            }
        }
    }
}
