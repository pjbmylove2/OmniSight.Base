using System.Drawing;
using System.Windows.Forms;

namespace OmniSight.Base.UI
{
    partial class StartupForm
    {
        private Label lblTitle;
        private Label lblSubtitle;
        private Label lblVersion;
        private Label lblStatus;
        private ProgressBar progressStartup;
        private Button btnClose;

        private void InitializeComponent()
        {
            lblTitle = new Label();
            lblSubtitle = new Label();
            lblVersion = new Label();
            lblStatus = new Label();
            progressStartup = new ProgressBar();
            btnClose = new Button();
            SuspendLayout();

            lblTitle.Font = new Font("Segoe UI", 23F, FontStyle.Bold);
            lblTitle.Location = new Point(28, 24);
            lblTitle.Size = new Size(504, 49);
            lblTitle.Text = "OmniSight.Base";

            lblSubtitle.Location = new Point(32, 81);
            lblSubtitle.Size = new Size(496, 21);
            lblSubtitle.Text = "FA application starter";

            lblVersion.ForeColor = Color.Silver;
            lblVersion.Location = new Point(32, 112);
            lblVersion.Size = new Size(496, 21);

            lblStatus.Location = new Point(32, 148);
            lblStatus.Size = new Size(496, 49);
            lblStatus.Text = "Preparing application…";

            progressStartup.Location = new Point(32, 205);
            progressStartup.Size = new Size(496, 12);
            progressStartup.Style = ProgressBarStyle.Marquee;

            btnClose.Location = new Point(436, 232);
            btnClose.Size = new Size(92, 30);
            btnClose.FlatStyle = FlatStyle.Flat;
            btnClose.Text = "Cancel";
            btnClose.Click += CloseRequested;

            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.FromArgb(24, 27, 33);
            ForeColor = Color.WhiteSmoke;
            ClientSize = new Size(560, 282);
            Controls.AddRange(new Control[] { lblTitle, lblSubtitle, lblVersion, lblStatus, progressStartup, btnClose });
            FormBorderStyle = FormBorderStyle.None;
            Name = "StartupForm";
            StartPosition = FormStartPosition.CenterScreen;
            ResumeLayout(false);
        }
    }
}
