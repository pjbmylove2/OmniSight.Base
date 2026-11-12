using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using OmniSight.Base.Application;
using OmniSight.Base.Configuration;

namespace OmniSight.Base.UI
{
    public partial class StartupForm : Form
    {
        private readonly ApplicationBootstrapper _bootstrapper;
        private CancellationTokenSource _startupCancellation;
        private bool _startupFailed;

        public StartupForm()
        {
            InitializeComponent();
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
        }

        internal StartupForm(ApplicationBootstrapper bootstrapper) : this()
        {
            _bootstrapper = bootstrapper ?? throw new ArgumentNullException(nameof(bootstrapper));
            Text = BuildInfo.AppId + " Startup";
            lblProductName.Text = BuildInfo.AppId;
            lblVersion.Text = BuildInfo.DisplayText;
        }

        protected override async void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (_bootstrapper == null)
            {
                return;
            }

            using (var cancellation = new CancellationTokenSource())
            {
                _startupCancellation = cancellation;
                DialogResult outcome;
                try
                {
                    await _bootstrapper.InitializeAsync(new Progress<(int Percent, string Status)>(UpdateStatus),
                        new Progress<SplashBranding>(ApplyBranding), cancellation.Token);
                    if (IsDisposed || Disposing)
                    {
                        return;
                    }

                    cancellation.Token.ThrowIfCancellationRequested();
                    progressBoot.Value = 100;
                    progressBoot.IsMarquee = false;
                    lblStatusCaption.Text = "초기화 완료 (100%)";
                    lblStatus.Text = "프로그램 초기화가 완료되었습니다.";
                    await Task.Delay(500, cancellation.Token);
                    outcome = DialogResult.OK;
                }
                catch (Exception) when (cancellation.IsCancellationRequested)
                {
                    outcome = DialogResult.Cancel;
                }
                catch (Exception error)
                {
                    if (!IsDisposed && !Disposing)
                    {
                        ShowStartupFailure(error);
                    }
                    return;
                }
                finally
                {
                    _startupCancellation = null;
                }

                if (!IsDisposed && !Disposing)
                {
                    DialogResult = cancellation.IsCancellationRequested ? DialogResult.Cancel : outcome;
                }
            }
        }

        private bool CanUpdateProgress()
        {
            return !IsDisposed && !Disposing && _startupCancellation != null && !_startupCancellation.IsCancellationRequested;
        }

        private void UpdateStatus((int Percent, string Status) progress)
        {
            if (CanUpdateProgress() && progressBoot.Value < 100)
            {
                progressBoot.Value = progress.Percent;
                lblStatusCaption.Text = $"초기화 중 ({progress.Percent}%)";
                lblStatus.Text = progress.Status;
            }
        }

        private void ApplyBranding(SplashBranding branding)
        {
            if (CanUpdateProgress())
            {
                lblProductName.Text = string.IsNullOrWhiteSpace(branding.Title) ? BuildInfo.AppId : branding.Title;
                lblSubtitle.Text = branding.Subtitle;
            }
        }

        private void ShowStartupFailure(Exception error)
        {
            _startupFailed = true;
            Text = BuildInfo.AppId + " Startup failed";
            lblStatusCaption.Text = "초기화 실패";
            lblStatus.Text = error.Message;
            lblStatus.ForeColor = Color.Salmon;
            progressBoot.IsMarquee = false;
            progressBoot.Visible = false;
            lblProgressHint.Text = "오류를 확인한 뒤 창을 닫으십시오.";
            btnCancel.Text = "닫기";
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (_startupCancellation != null)
            {
                e.Cancel = true;
                _startupCancellation.Cancel();
                btnCancel.Enabled = false;
                progressBoot.IsMarquee = false;
                lblStatusCaption.Text = "초기화 취소 중";
                lblStatus.Text = "현재 초기화 단계가 끝나면 창이 닫힙니다.";
                lblProgressHint.Text = string.Empty;
            }
            else if (_startupFailed)
            {
                DialogResult = DialogResult.Abort;
            }
        }
    }
}
