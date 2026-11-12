using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OmniSight.Base.Configuration;
using OmniSight.Base.Devices;
using OmniSight.Base.Logging;
using OmniSight.Base.Localization;

namespace OmniSight.Base.Application
{
    internal sealed class ApplicationBootstrapper : IDisposable
    {
        private readonly PathManager _paths;
        private LogManager _log;
        private Bitmap _splashLogo;

        public ApplicationBootstrapper(PathManager paths)
        {
            _paths = paths ?? throw new ArgumentNullException(nameof(paths));
            SystemConfiguration = new JsonConfigProvider<SystemSettings>(SystemSettingsPath);
        }

        public Machine Machine { get; private set; }
        public RuntimeHealthMonitor HealthMonitor { get; private set; }
        public JsonConfigProvider<SystemSettings> SystemConfiguration { get; }

        private string SystemSettingsPath => Path.Combine(_paths.SystemPath, "SystemGeneral.json");

        public LanguageType ReadStartupLanguage()
        {
            return JsonConfigProvider.TryReadExisting<SystemSettings>(SystemSettingsPath, _paths.PreviousConfigPath)?.Language
                ?? LanguageType.KOREAN;
        }

        public Task InitializeAsync(IProgress<(int Percent, string Status)> progress, IProgress<SplashBranding> branding, CancellationToken token)
        {
            return Task.Run(() => Initialize(progress, branding, token), token);
        }

        private void Initialize(IProgress<(int Percent, string Status)> progress, IProgress<SplashBranding> branding, CancellationToken token)
        {
            try
            {
                progress.Report((33, LocalText.Get("Text.StartupForm.PrepareFolders", "폴더와 로그를 준비하고 있습니다...")));
                _log = new LogManager(_paths.LogPath, _paths.FallbackLogPath);
                try
                {
                    _paths.CreateDirectories();
                }
                catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
                {
                    throw new IOException(LocalText.Format("Text.StartupForm.StorageFolderError", "저장 폴더를 만들 수 없습니다. {0}", _paths.RootPath) + Environment.NewLine + error.Message, error);
                }
                _log.System($"[Boot] Storage root: {_paths.RootPath}");
                _log.System($"{BuildInfo.AppId} started. {BuildInfo.DisplayText}");
                token.ThrowIfCancellationRequested();

                progress.Report((66, LocalText.Get("Text.StartupForm.LoadSettings", "설정을 읽고 있습니다...")));
                var system = SystemConfiguration.Load(_paths.PreviousConfigPath);
                LocalizationManager.ChangeLanguage(system.Language);
                _log.WriteDebugLog = system.WriteDebugLog;
                var display = new JsonConfigProvider<SplashBranding>(Path.Combine(_paths.SystemPath, "SplashBranding.json")).Load(_paths.PreviousConfigPath);
                _splashLogo = LoadSplashLogo(display.LogoPath);
                branding.Report(display);
                var camera = new JsonConfigProvider<CameraSettings>(Path.Combine(_paths.CameraDevicePath, "Camera.json"),
                    settings => settings.Validate()).Load(_paths.PreviousConfigPath);
                _log.Debug("Configuration loaded.");
                _log.System($"Camera mode: {camera.Mode}.");
                token.ThrowIfCancellationRequested();

                progress.Report((95, LocalText.Get("Text.StartupForm.InitializeDevices", "장비를 초기화하고 있습니다...")));
                Machine = new Machine(DeviceFactory.Create(camera), _log);
                Machine.Initialize(token);
                token.ThrowIfCancellationRequested();
                HealthMonitor = new RuntimeHealthMonitor(_paths.DataPath, _log);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                _log?.System("Startup canceled.");
                throw;
            }
            catch (Exception error)
            {
                _log?.Alarm("Application startup failed.", error);
                throw;
            }
        }

        // The splash owns the returned bitmap. Dispose releases it here if startup is canceled before it is taken.
        public Bitmap TakeSplashLogo()
        {
            return Interlocked.Exchange(ref _splashLogo, null);
        }

        private Bitmap LoadSplashLogo(string configuredPath)
        {
            if (string.IsNullOrWhiteSpace(configuredPath))
            {
                return null;
            }

            try
            {
                string path = configuredPath.Trim();
                if (!Path.IsPathRooted(path))
                {
                    string storagePath = Path.Combine(_paths.RootPath, path);
                    path = File.Exists(storagePath) ? storagePath : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
                }

                using (var stream = File.OpenRead(path))
                using (var image = Image.FromStream(stream))
                {
                    return new Bitmap(image);
                }
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException ||
                                          error is ArgumentException || error is NotSupportedException ||
                                          error is OutOfMemoryException || error is System.Runtime.InteropServices.ExternalException)
            {
                _log.System("[Splash] Logo image could not be loaded: " + error.Message);
                return null;
            }
        }

        public void ReportApplicationError(Exception error)
        {
            _log?.Alarm("Application error.", error);
        }

        public void Dispose()
        {
            try
            {
                try
                {
                    HealthMonitor?.Dispose();
                }
                finally
                {
                    Machine?.Dispose();
                }
            }
            finally
            {
                TakeSplashLogo()?.Dispose();
                if (_log != null)
                {
                    _log.System("Application stopped.");
                    _log.Dispose();
                    _log = null;
                }
            }
        }
    }
}
