using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
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
        private string _logoText;

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
            if (!CanUpdateProgress())
            {
                return;
            }

            lblProductName.Text = TextOrDefault(branding.ProductName, TextOrDefault(branding.Title, BuildInfo.AppId));
            lblSubtitle.Text = branding.Subtitle;
            lblSiteName.Text = TextOrDefault(branding.SiteName, lblSiteName.Text);
            lblEquipmentName.Text = TextOrDefault(branding.EquipmentName, lblEquipmentName.Text);
            lblCompanyName.Text = TextOrDefault(branding.CompanyName, lblCompanyName.Text);
            lblFooter.Text = TextOrDefault(branding.FooterText, lblFooter.Text);
            pnlHero.PrimaryColor = ParseColor(branding.AccentColor, pnlHero.PrimaryColor);
            pnlHero.SecondaryColor = ParseColor(branding.BrandPanelColor, pnlHero.SecondaryColor);
            _logoText = branding.LogoText?.Trim();
            picLogo.Image = _bootstrapper.TakeSplashLogo();
            picLogo.AccessibleName = TextOrDefault(_logoText, lblCompanyName.Text) + " 로고";
            picLogo.Invalidate();
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
                using (var font = new Font("Segoe UI", 21F, FontStyle.Bold, GraphicsUnit.Pixel))
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
