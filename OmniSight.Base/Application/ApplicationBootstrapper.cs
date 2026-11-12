using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OmniSight.Base.Configuration;
using OmniSight.Base.Devices;
using OmniSight.Base.Logging;

namespace OmniSight.Base.Application
{
    internal sealed class ApplicationBootstrapper : IDisposable
    {
        private readonly AppPaths _paths;
        private LogManager _log;

        public ApplicationBootstrapper(AppPaths paths)
        {
            _paths = paths;
        }

        public Machine Machine { get; private set; }

        public Task InitializeAsync(IProgress<(int Percent, string Status)> progress, IProgress<SplashBranding> branding, CancellationToken token)
        {
            return Task.Run(() => Initialize(progress, branding, token), token);
        }

        private void Initialize(IProgress<(int Percent, string Status)> progress, IProgress<SplashBranding> branding, CancellationToken token)
        {
            try
            {
                progress.Report((33, "폴더와 로그를 준비하고 있습니다..."));
                _log = new LogManager(_paths.Logs, Path.Combine(Path.GetTempPath(), BuildInfo.AppId, "LogFallback"));
                _paths.CreateDirectories();
                _log.System($"{BuildInfo.AppId} started. {BuildInfo.DisplayText}");
                token.ThrowIfCancellationRequested();

                progress.Report((66, "설정을 읽고 있습니다..."));
                var display = JsonConfigProvider.LoadOrCreate(Path.Combine(_paths.Config, "SplashBranding.json"), new SplashBranding());
                branding.Report(display);
                var system = JsonConfigProvider.LoadOrCreate(Path.Combine(_paths.Config, "SystemGeneral.json"), new SystemSettings());
                _log.WriteDebugLog = system.WriteDebugLog;
                var camera = JsonConfigProvider.LoadOrCreate(Path.Combine(_paths.Config, "Camera.json"), new CameraSettings());
                camera.Validate();
                _log.Debug("Configuration loaded.");
                _log.System($"Camera mode: {camera.Mode}.");
                token.ThrowIfCancellationRequested();

                progress.Report((95, "장비를 초기화하고 있습니다..."));
                Machine = new Machine(DeviceFactory.Create(camera), _log);
                Machine.Initialize(token);
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

        public void ReportApplicationError(Exception error)
        {
            _log?.Alarm("Application error.", error);
        }

        public void Dispose()
        {
            try
            {
                Machine?.Dispose();
            }
            finally
            {
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
