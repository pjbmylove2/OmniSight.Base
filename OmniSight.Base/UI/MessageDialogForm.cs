using OmniSight.Base.Localization;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Media;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace OmniSight.Base.UI.Forms
{
    using OmniSight.Base.UI.Utils;

    public partial class MessageDialogForm : Form
    {
        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int message, int wParam, int lParam);

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        private const int WmNcLButtonDown = 0xA1;
        private const int HtCaption = 0x2;
        private const int DropShadowClassStyle = 0x00020000;

        private readonly Point? _initialLocation;
        private WeakReference<Form> _centeringMainWindow;
        private MessageSeverity _level = MessageSeverity.Info;
        private Font _criticalFont;
        private Timer _blinkTimer;
        private Timer _autoCloseTimer;
        private int _remainingSeconds;
        private bool _blinkState;
        private bool _resourcesReleased;
        private Color _accentColor = Color.FromArgb(37, 99, 235);
        private Color _softAccentColor = Color.FromArgb(239, 246, 255);
        private DialogResult _timeoutResult = DialogResult.OK;

        /// <summary>Visual Studio 디자이너에서 화면 구조만 표시하기 위한 생성자입니다.</summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public MessageDialogForm()
        {
            InitializeComponent();
            if (LicenseManager.UsageMode != LicenseUsageMode.Designtime)
            {
                FontManager.Apply(this);
                UiLocalization.Apply(this);
            }

            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            UpdateStyles();
        }

        public MessageDialogForm(string text, string caption, MessageSeverity level,
            MessageBoxButtons buttons, int autoCloseSeconds)
            : this(text, caption, level, buttons, autoCloseSeconds, null)
        {
        }

        internal MessageDialogForm(string text, string caption, MessageSeverity level,
            MessageBoxButtons buttons, int autoCloseSeconds, Point? initialLocation)
            : this()
        {
            ValidateArguments(buttons, autoCloseSeconds);
            if (!Enum.IsDefined(typeof(MessageSeverity), level))
            {
                throw new ArgumentOutOfRangeException(nameof(level));
            }

            _initialLocation = initialLocation;
            _level = level;
            _remainingSeconds = autoCloseSeconds;
            SetupUi(text, caption, buttons);
            SetupTimers();
        }

        public void CenterOnMainWindow(Form mainWindow)
        {
            _centeringMainWindow = mainWindow == null ? null : new WeakReference<Form>(mainWindow);
            ApplyInitialPlacement();
        }
        public void RecenterOnMainWindow()
        {
            Form anchor = null;
            _centeringMainWindow?.TryGetTarget(out anchor);
            MainWindowDialogPlacement.Center(this, anchor);
        }
        protected override void OnShown(EventArgs e)
        {
            ApplyInitialPlacement();
            base.OnShown(e);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams parameters = base.CreateParams;
                parameters.ClassStyle |= DropShadowClassStyle;
                return parameters;
            }
        }

        internal static bool IsSupportedButtons(MessageBoxButtons buttons)
        {
            return buttons == MessageBoxButtons.OK ||
                   buttons == MessageBoxButtons.OKCancel ||
                   buttons == MessageBoxButtons.YesNo;
        }

        private void ApplyInitialPlacement()
        {
            if (_initialLocation.HasValue)
            {
                Location = _initialLocation.Value;
            }
            else
            {
                RecenterOnMainWindow();
            }
        }

        private static void ValidateArguments(MessageBoxButtons buttons, int autoCloseSeconds)
        {
            if (!IsSupportedButtons(buttons))
            {
                throw new ArgumentOutOfRangeException(nameof(buttons), buttons,
                                LocalText.Get("Text.MessageDialogForm.UnsupportedButtons", "MessageDialog는 OK, OKCancel, YesNo 버튼 조합을 지원합니다."));
            }

            if (autoCloseSeconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(autoCloseSeconds),
                                LocalText.Get("Text.MessageDialogForm.AutoCloseRange", "자동 닫힘 시간은 0 이상의 초 단위 값이어야 합니다."));
            }
        }

        private void SetupUi(string text, string caption, MessageBoxButtons buttons)
        {
            string safeText = string.IsNullOrWhiteSpace(text) ? LocalText.Get("Text.MessageDialogForm.NoMessage", "표시할 메시지가 없습니다.") : text.Trim();
            string safeCaption = string.IsNullOrWhiteSpace(caption) ? LocalText.Get("Text.MessageDialogForm.NoticeTitle", "알림") : caption.Trim();

            lblCaption.Text = safeCaption;
            lblMessage.Text = safeText;
            lblCountdown.Text = string.Empty;
            Text = safeCaption;
            AccessibleName = safeCaption;
            AccessibleDescription = safeText;

            ConfigureLevel(buttons);
            ConfigureButtons(buttons);
            AdjustDialogSize(safeText);
            ApplySeverityAppearance(buttons);
            PlayLevelSound();
        }

        private void ConfigureLevel(MessageBoxButtons buttons)
        {
            switch (_level)
            {
                case MessageSeverity.Info:
                    lblSeverity.Text = LocalText.Get("Text.MessageDialogForm.SeverityInformation", "정보");
                    _accentColor = Color.FromArgb(37, 99, 235);
                    _softAccentColor = Color.FromArgb(239, 246, 255);
                    break;

                case MessageSeverity.Warning:
                    lblSeverity.Text = buttons == MessageBoxButtons.YesNo ? LocalText.Get("Text.MessageDialogForm.SeverityConfirmation", "확인") : LocalText.Get("Text.MessageDialogForm.SeverityWarning", "경고");
                    _accentColor = Color.FromArgb(202, 138, 4);
                    _softAccentColor = Color.FromArgb(254, 249, 195);
                    break;

                case MessageSeverity.Error:
                    lblSeverity.Text = LocalText.Get("Text.MessageDialogForm.SeverityError", "오류");
                    _accentColor = Color.FromArgb(220, 38, 38);
                    _softAccentColor = Color.FromArgb(254, 242, 242);
                    break;

                case MessageSeverity.CriticalAlarm:
                    lblSeverity.Text = LocalText.Get("Text.MessageDialogForm.SeverityCriticalAlarm", "중대 알람");
                    _accentColor = Color.FromArgb(185, 28, 28);
                    _criticalFont = FontManager.CreateFont(lblMessage.Font.Size, FontStyle.Bold);
                    lblMessage.Font = _criticalFont;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(_level), _level, LocalText.Get("Text.MessageDialogForm.UnsupportedSeverity", "지원하지 않는 메시지 단계입니다."));
            }

            ChangeIconSafely((buttons == MessageBoxButtons.YesNo && _level == MessageSeverity.Warning
                ? SystemIcons.Question : GetSystemIcon(_level)).ToBitmap());
        }

        private void ApplySeverityAppearance(MessageBoxButtons buttons)
        {
            _softAccentColor = Blend(_accentColor, pnlBody.BackColor, 0.20f);
            pnlAccent.BackColor = _accentColor;
            pnlIconSurface.BackColor = _softAccentColor;
            lblSeverity.ForeColor = _accentColor;
            Button primaryButton = buttons == MessageBoxButtons.YesNo ? btnYes : btnOk;
            primaryButton.BackColor = _accentColor;
            primaryButton.ForeColor = Color.White;
            primaryButton.FlatAppearance.BorderColor = _accentColor;
            primaryButton.FlatAppearance.MouseOverBackColor = Darken(_accentColor, 0.88f);
            primaryButton.FlatAppearance.MouseDownBackColor = Darken(_accentColor, 0.76f);
        }

        private void ConfigureButtons(MessageBoxButtons buttons)
        {
            btnOk.Visible = buttons == MessageBoxButtons.OK || buttons == MessageBoxButtons.OKCancel;
            btnYes.Visible = buttons == MessageBoxButtons.YesNo;
            btnNo.Visible = buttons == MessageBoxButtons.YesNo;
            btnCancel.Visible = buttons == MessageBoxButtons.OKCancel;

            switch (buttons)
            {
                case MessageBoxButtons.OK:
                    AcceptButton = btnOk;
                    CancelButton = btnOk;
                    _timeoutResult = DialogResult.OK;
                    break;
                case MessageBoxButtons.OKCancel:
                    AcceptButton = btnOk;
                    CancelButton = btnCancel;
                    _timeoutResult = DialogResult.Cancel;
                    break;
                case MessageBoxButtons.YesNo:
                    AcceptButton = btnYes;
                    CancelButton = btnNo;
                    _timeoutResult = DialogResult.No;
                    break;
            }
        }

        private void AdjustDialogSize(string text)
        {
            const int minimumWidth = 560;
            int maximumWidth = _level == MessageSeverity.CriticalAlarm ? 760 : 700;
            int longestLineWidth = 0;
            foreach (string line in text.Replace("\r", string.Empty).Split('\n'))
            {
                int lineWidth = TextRenderer.MeasureText(line, lblMessage.Font, Size.Empty,
                    TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix).Width;
                longestLineWidth = Math.Max(longestLineWidth, lineWidth);
            }

            int desiredWidth = Math.Max(minimumWidth, Math.Min(maximumWidth, longestLineWidth + 178));
            int textWidth = Math.Max(280, desiredWidth - 164);
            Size measured = TextRenderer.MeasureText(text, lblMessage.Font, new Size(textWidth, 800),
                TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
            int bodyHeight = Math.Max(132, Math.Min(264, measured.Height + 58));
            ClientSize = new Size(desiredWidth, pnlHeader.Height + pnlFooter.Height + pnlAccent.Height + bodyHeight);
        }

        private static Icon GetSystemIcon(MessageSeverity level)
        {
            if (level == MessageSeverity.Info)
            {
                return SystemIcons.Information;
            }

            if (level == MessageSeverity.Warning)
            {
                return SystemIcons.Warning;
            }

            return SystemIcons.Error;
        }

        private void ChangeIconSafely(Image newImage)
        {
            Image oldImage = picIcon.Image;
            picIcon.Image = newImage;
            if (oldImage != null)
            {
                oldImage.Dispose();
            }
        }

        private void PlayLevelSound()
        {
            if (_level == MessageSeverity.Info)
            {
                SystemSounds.Asterisk.Play();
            }
            else if (_level == MessageSeverity.Warning)
            {
                SystemSounds.Exclamation.Play();
            }
            else
            {
                SystemSounds.Hand.Play();
            }
        }

        private void SetupTimers()
        {
            if (_level == MessageSeverity.CriticalAlarm)
            {
                _blinkTimer = new Timer { Interval = 750 };
                _blinkTimer.Tick += BlinkTimer_Tick;
                _blinkTimer.Start();
            }

            if (_remainingSeconds <= 0)
            {
                return;
            }

            UpdateCountdownText();
            _autoCloseTimer = new Timer { Interval = 1000 };
            _autoCloseTimer.Tick += AutoCloseTimer_Tick;
            _autoCloseTimer.Start();
        }

        private void BlinkTimer_Tick(object sender, EventArgs e)
        {
            _blinkState = !_blinkState;
            pnlAccent.BackColor = _blinkState ? Color.FromArgb(127, 29, 29) : _accentColor;
            pnlIconSurface.BackColor = _blinkState ? Color.FromArgb(254, 202, 202) : _softAccentColor;
        }

        private void AutoCloseTimer_Tick(object sender, EventArgs e)
        {
            _remainingSeconds--;
            if (_remainingSeconds <= 0)
            {
                DialogResult = _timeoutResult;
                Close();
                return;
            }
            UpdateCountdownText();
        }

        private void UpdateCountdownText()
        {
            lblCountdown.Text = LocalText.Interpolate("Text.MessageDialogForm.AutoCloseCountdown", $"자동 닫기  {_remainingSeconds}초");
        }

        private void PnlTop_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            ReleaseCapture();
            SendMessage(Handle, WmNcLButtonDown, HtCaption, 0);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.C))
            {
                try
                {
                    Clipboard.SetText(lblCaption.Text + Environment.NewLine + Environment.NewLine + lblMessage.Text);
                }
                catch (ExternalException)
                {
                    // 클립보드가 잠긴 경우에도 메시지박스는 계속 동작합니다.
                }
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            ControlPaint.DrawBorder(e.Graphics, ClientRectangle, Color.FromArgb(203, 213, 225), ButtonBorderStyle.Solid);
        }

        private static Color Darken(Color color, float factor)
        {
            return Color.FromArgb(color.A, (int)(color.R * factor), (int)(color.G * factor), (int)(color.B * factor));
        }

        private static Color Blend(Color foreground, Color background, float amount)
        {
            return Color.FromArgb(
                (int)(foreground.R * amount + background.R * (1f - amount)),
                (int)(foreground.G * amount + background.G * (1f - amount)),
                (int)(foreground.B * amount + background.B * (1f - amount)));
        }

        private void ReleaseRuntimeResources()
        {
            if (_resourcesReleased)
            {
                return;
            }

            _resourcesReleased = true;
            _criticalFont?.Dispose();

            if (_blinkTimer != null)
            {
                _blinkTimer.Stop();
                _blinkTimer.Tick -= BlinkTimer_Tick;
                _blinkTimer.Dispose();
                _blinkTimer = null;
            }
            if (_autoCloseTimer != null)
            {
                _autoCloseTimer.Stop();
                _autoCloseTimer.Tick -= AutoCloseTimer_Tick;
                _autoCloseTimer.Dispose();
                _autoCloseTimer = null;
            }
            if (picIcon != null && picIcon.Image != null)
            {
                Image image = picIcon.Image;
                picIcon.Image = null;
                image.Dispose();
            }
        }
    }
}
