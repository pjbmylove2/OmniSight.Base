using System;
using System.Security.Principal;
using System.Threading;
using System.Windows.Forms;
using OmniSight.Base.Application;
using OmniSight.Base.Configuration;
using OmniSight.Base.UI;
using OmniSight.Base.UI.Utils;
using OmniSight.Base.Localization;
using WinForms = System.Windows.Forms.Application;

namespace OmniSight.Base
{
    internal static class Program
    {
        [STAThread]
        private static int Main()
        {
            WinForms.EnableVisualStyles();
            WinForms.SetCompatibleTextRenderingDefault(false);
            WinForms.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);

            try
            {
                LocalizationManager.Initialize();
                FontManager.Initialize();
                var paths = new PathManager(BuildInfo.AppId);
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                using (var instance = new Mutex(false, $@"Global\{identity.User.Value}.{BuildInfo.AppId.ToUpperInvariant()}"))
                {
                    if (!TryAcquire(instance))
                    {
                        return 0;
                    }

                    try
                    {
                        return Run(paths);
                    }
                    finally
                    {
                        instance.ReleaseMutex();
                    }
                }
            }
            catch (Exception error)
            {
                MessageDialog.Show(error.Message,
                    LocalText.Get("Text.Program.FatalErrorTitle", "프로그램 오류"), MessageSeverity.Error);
                return 1;
            }
            finally
            {
                FontManager.Release();
            }
        }

        private static int Run(PathManager paths)
        {
            using (var bootstrapper = new ApplicationBootstrapper(paths))
            {
                try
                {
                    using (var splash = new StartupForm(bootstrapper))
                    {
                        DialogResult result = splash.ShowDialog();
                        if (result != DialogResult.OK)
                        {
                            return result == DialogResult.Cancel ? 0 : 1;
                        }
                    }

                    using (var mainForm = new MainForm(bootstrapper.Machine, bootstrapper.SystemConfiguration, bootstrapper.HealthMonitor))
                    {
                        WinForms.Run(mainForm);
                    }

                    return 0;
                }
                catch (Exception error)
                {
                    bootstrapper.ReportApplicationError(error);
                    throw;
                }
            }
        }

        private static bool TryAcquire(Mutex instance)
        {
            try
            {
                return instance.WaitOne(0);
            }
            catch (AbandonedMutexException)
            {
                return true;
            }
        }
    }
}
