using System;
using System.Threading;
using System.Windows.Forms;
using OmniSight.Base.Application;
using OmniSight.Base.Configuration;

namespace OmniSight.Base.UI
{
    public partial class StartupForm : Form
    {
        private readonly ApplicationBootstrapper _bootstrapper;
        private CancellationTokenSource _startupCancellation;
        private bool _initializing;
        private bool _startupFailed;

        public StartupForm()
        {
            InitializeComponent();
        }

        internal StartupForm(ApplicationBootstrapper bootstrapper) : this()
        {
            _bootstrapper = bootstrapper ?? throw new ArgumentNullException(nameof(bootstrapper));
            Text = BuildInfo.AppId + " Startup";
            lblTitle.Text = BuildInfo.AppId;
            lblVersion.Text = BuildInfo.DisplayText;
        }

        protected override async void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (_bootstrapper == null)
            {
                return;
            }

            _initializing = true;
            _startupCancellation = new CancellationTokenSource();
            DialogResult outcome = DialogResult.None;
            try
            {
                await _bootstrapper.InitializeAsync(new Progress<string>(UpdateStatus),
                    new Progress<SplashBranding>(ApplyBranding), _startupCancellation.Token);
                outcome = _startupCancellation.IsCancellationRequested ? DialogResult.Cancel : DialogResult.OK;
            }
            catch (Exception) when (_startupCancellation.IsCancellationRequested)
            {
                outcome = DialogResult.Cancel;
            }
            catch (Exception error)
            {
                _startupFailed = true;
                Text = BuildInfo.AppId + " Startup failed";
                lblStatus.Text = "Startup failed. " + error.Message;
                lblStatus.ForeColor = System.Drawing.Color.Salmon;
                progressStartup.Visible = false;
                btnClose.Text = "Close";
                btnClose.Enabled = true;
            }
            finally
            {
                _initializing = false;
                _startupCancellation.Dispose();
                _startupCancellation = null;
            }

            if (outcome != DialogResult.None)
            {
                DialogResult = outcome;
            }
        }

        private void UpdateStatus(string status)
        {
            if (_initializing && !_startupCancellation.IsCancellationRequested)
            {
                lblStatus.Text = status;
            }
        }

        private void ApplyBranding(SplashBranding branding)
        {
            if (_initializing)
            {
                lblTitle.Text = string.IsNullOrWhiteSpace(branding.Title) ? BuildInfo.AppId : branding.Title;
                lblSubtitle.Text = branding.Subtitle;
            }
        }

        private void CloseRequested(object sender, EventArgs e)
        {
            if (_initializing)
            {
                CancelStartup();
                return;
            }

            DialogResult = DialogResult.Abort;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (_initializing)
            {
                e.Cancel = true;
                CancelStartup();
            }
            else if (_startupFailed)
            {
                DialogResult = DialogResult.Abort;
            }
        }

        private void CancelStartup()
        {
            _startupCancellation.Cancel();
            btnClose.Enabled = false;
            lblStatus.Text = "Canceling startup…";
        }
    }
}
