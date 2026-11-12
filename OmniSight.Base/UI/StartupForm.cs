using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using OmniSight.Base.Application;
using OmniSight.Base.Configuration;
using OmniSight.Base.Localization;
using OmniSight.Base.UI.Utils;

namespace OmniSight.Base.UI
{
    public partial class StartupForm : Form
    {
        private readonly ApplicationBootstrapper _bootstrapper;
        private CancellationTokenSource _startupCancellation;
        private bool _startupFailed;
        private string _logoText;

        public StartupForm()
        {
            InitializeComponent();
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
        }

        internal StartupForm(ApplicationBootstrapper bootstrapper) : this()
        {
            _bootstrapper = bootstrapper ?? throw new ArgumentNullException(nameof(bootstrapper));
            LocalizationManager.ChangeLanguage(_bootstrapper.ReadStartupLanguage());
            lblProductName.Text = BuildInfo.AppId;
            RefreshLocalization();
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
                    lblStatusCaption.Text = LocalText.Get("Text.StartupForm.InitCompletedCaption", "초기화 완료 (100%)");
                    lblStatus.Text = LocalText.Get("Text.StartupForm.InitCompletedStatus", "프로그램 초기화 완료. 메인 화면을 시작합니다.");
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
                lblStatusCaption.Text = LocalText.Get("Text.StartupForm.Initializing", "초기화 중") + $" ({progress.Percent}%)";
                lblStatus.Text = progress.Status;
            }
        }

        private void ApplyBranding(SplashBranding branding)
        {
            if (!CanUpdateProgress())
            {
                return;
            }

            RefreshLocalization();
            lblProductName.Text = TextOrDefault(branding.ProductName, TextOrDefault(branding.Title, BuildInfo.AppId));
            if (branding.Subtitle != null && branding.Subtitle != "FA application starter")
            {
                lblSubtitle.Text = branding.Subtitle;
            }
            lblSiteName.Text = TextOrDefault(branding.SiteName, lblSiteName.Text);
            lblEquipmentName.Text = TextOrDefault(branding.EquipmentName, lblEquipmentName.Text);
            lblCompanyName.Text = TextOrDefault(branding.CompanyName, lblCompanyName.Text);
            lblFooter.Text = TextOrDefault(branding.FooterText, lblFooter.Text);
            pnlHero.PrimaryColor = ParseColor(branding.AccentColor, pnlHero.PrimaryColor);
            pnlHero.SecondaryColor = ParseColor(branding.BrandPanelColor, pnlHero.SecondaryColor);
            _logoText = branding.LogoText?.Trim();
            picLogo.Image = _bootstrapper.TakeSplashLogo();
            picLogo.AccessibleName = TextOrDefault(_logoText, lblCompanyName.Text) + LocalText.Get("Text.StartupForm.LogoSuffix", " 로고");
            picLogo.Invalidate();
        }

        private void RefreshLocalization()
        {
            FontManager.Apply(this);
            UiLocalization.Apply(this);
            Text = LocalText.Format("Text.StartupForm.WindowTitle", "{0} 시작", BuildInfo.AppId);
            lblVersion.Text = LocalText.Format("Text.StartupForm.Version", "버전 {0} · {1}", BuildInfo.FileVersion, BuildInfo.Configuration);
            lblStatus.AccessibleName = LocalText.Get("Text.StartupForm.StatusAccessibleName", "초기화 진행 상태");
            progressBoot.AccessibleName = LocalText.Get("Text.StartupVisuals.ProgressAccessibleName", "초기화 진행률");
        }

        private static string TextOrDefault(string text, string fallback)
        {
            return string.IsNullOrWhiteSpace(text) ? fallback : text.Trim();
        }

        private static Color ParseColor(string value, Color fallback)
        {
            value = value?.Trim();
            return value != null && value.Length == 7 && value[0] == '#' &&
                int.TryParse(value.Substring(1), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out int rgb)
                ? Color.FromArgb(unchecked((int)0xFF000000) | rgb)
                : fallback;
        }

        private void PicLogo_Paint(object sender, PaintEventArgs e)
        {
            if (picLogo.Image != null || string.IsNullOrWhiteSpace(_logoText))
            {
                return;
            }

            Graphics graphics = e.Graphics;
            GraphicsState state = graphics.Save();
            try
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.ScaleTransform(picLogo.ClientSize.Width / 112f, picLogo.ClientSize.Height / 112f);
                using (var halo = new SolidBrush(Color.FromArgb(42, Color.White)))
                using (var outline = new Pen(Color.FromArgb(229, Color.White), 3F))
                using (var lens = new SolidBrush(Color.FromArgb(30, 191, 93)))
                using (var font = FontManager.CreateFont(21F, FontStyle.Bold, GraphicsUnit.Pixel))
                using (var format = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center,
                    FormatFlags = StringFormatFlags.NoWrap,
                    Trimming = StringTrimming.EllipsisCharacter
                })
                {
                    graphics.FillEllipse(halo, 9, 8, 94, 94);
                    graphics.DrawEllipse(outline, 18, 17, 76, 76);
                    graphics.FillEllipse(lens, 31, 30, 50, 50);
                    graphics.DrawString(_logoText, font, Brushes.White, new RectangleF(24, 29, 64, 52), format);
                }
            }
            finally
            {
                graphics.Restore(state);
            }
        }

        private void ReleaseSplashLogo()
        {
            if (picLogo?.Image != null)
            {
                Image logo = picLogo.Image;
                picLogo.Image = null;
                logo.Dispose();
            }
        }

        private void ShowStartupFailure(Exception error)
        {
            _startupFailed = true;
            Text = LocalText.Format("Text.StartupForm.FailureTitle", "{0} 시작 실패", BuildInfo.AppId);
            lblStatusCaption.Text = LocalText.Get("Text.StartupForm.InitFailedCaption", "초기화 실패");
            lblStatus.Text = error.Message;
            lblStatus.ForeColor = Color.Salmon;
            progressBoot.IsMarquee = false;
            progressBoot.Visible = false;
            lblProgressHint.Text = LocalText.Get("Text.StartupForm.ErrorCloseHint", "오류를 확인한 뒤 창을 닫으십시오.");
            btnCancel.Text = LocalText.Get("Text.StartupForm.Close", "닫기");
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
                lblStatusCaption.Text = LocalText.Get("Text.StartupForm.CancelingCaption", "초기화 취소 중");
                lblStatus.Text = LocalText.Get("Text.StartupForm.CloseAfterCurrentStep", "현재 초기화 단계가 끝나면 창이 닫힙니다.");
                lblProgressHint.Text = string.Empty;
            }
            else if (_startupFailed)
            {
                DialogResult = DialogResult.Abort;
            }
        }
    }
}
