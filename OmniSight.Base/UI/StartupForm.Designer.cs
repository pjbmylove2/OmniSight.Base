namespace OmniSight.Base.UI
{
    partial class StartupForm
    {
        private System.ComponentModel.IContainer components = null;
        private StartupBrandPanel pnlHero;
        private System.Windows.Forms.Panel pnlProgress;
        private System.Windows.Forms.PictureBox picLogo;
        private System.Windows.Forms.Label lblSiteName;
        private System.Windows.Forms.Label lblCompanyName;
        private System.Windows.Forms.Label lblProductName;
        private System.Windows.Forms.Label lblSubtitle;
        private System.Windows.Forms.Label lblEquipmentName;
        private System.Windows.Forms.Label lblVersion;
        private System.Windows.Forms.Label lblStatusCaption;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label lblProgressHint;
        private System.Windows.Forms.Label lblFooter;
        private StartupProgressBar progressBoot;
        private System.Windows.Forms.Button btnCancel;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ReleaseSplashLogo();
                if (components != null) components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lblVersion = new System.Windows.Forms.Label();
            this.pnlProgress = new System.Windows.Forms.Panel();
            this.lblStatusCaption = new System.Windows.Forms.Label();
            this.btnCancel = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.progressBoot = new OmniSight.Base.UI.StartupProgressBar();
            this.lblProgressHint = new System.Windows.Forms.Label();
            this.lblFooter = new System.Windows.Forms.Label();
            this.pnlHero = new OmniSight.Base.UI.StartupBrandPanel();
            this.picLogo = new System.Windows.Forms.PictureBox();
            this.lblSiteName = new System.Windows.Forms.Label();
            this.lblCompanyName = new System.Windows.Forms.Label();
            this.lblProductName = new System.Windows.Forms.Label();
            this.lblSubtitle = new System.Windows.Forms.Label();
            this.lblEquipmentName = new System.Windows.Forms.Label();
            this.pnlProgress.SuspendLayout();
            this.pnlHero.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picLogo)).BeginInit();
            this.SuspendLayout();
            //
            // lblVersion
            //
            this.lblVersion.AutoEllipsis = true;
            this.lblVersion.BackColor = System.Drawing.Color.Transparent;
            this.lblVersion.Font = new System.Drawing.Font("Segoe UI", 7F);
            this.lblVersion.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(236)))), ((int)(((byte)(236)))), ((int)(((byte)(236)))));
            this.lblVersion.Location = new System.Drawing.Point(12, 3);
            this.lblVersion.Name = "lblVersion";
            this.lblVersion.Size = new System.Drawing.Size(488, 18);
            this.lblVersion.TabIndex = 6;
            //
            // pnlProgress
            //
            this.pnlProgress.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(47)))), ((int)(((byte)(47)))), ((int)(((byte)(47)))));
            this.pnlProgress.Controls.Add(this.lblVersion);
            this.pnlProgress.Controls.Add(this.lblStatusCaption);
            this.pnlProgress.Controls.Add(this.btnCancel);
            this.pnlProgress.Controls.Add(this.lblStatus);
            this.pnlProgress.Controls.Add(this.progressBoot);
            this.pnlProgress.Controls.Add(this.lblProgressHint);
            this.pnlProgress.Controls.Add(this.lblFooter);
            this.pnlProgress.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlProgress.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(236)))), ((int)(((byte)(236)))), ((int)(((byte)(236)))));
            this.pnlProgress.Location = new System.Drawing.Point(0, 294);
            this.pnlProgress.Name = "pnlProgress";
            this.pnlProgress.Size = new System.Drawing.Size(650, 126);
            this.pnlProgress.TabIndex = 0;
            //
            // lblStatusCaption
            //
            this.lblStatusCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(47)))), ((int)(((byte)(47)))), ((int)(((byte)(47)))));
            this.lblStatusCaption.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblStatusCaption.ForeColor = System.Drawing.Color.White;
            this.lblStatusCaption.Location = new System.Drawing.Point(25, 30);
            this.lblStatusCaption.Name = "lblStatusCaption";
            this.lblStatusCaption.Size = new System.Drawing.Size(475, 22);
            this.lblStatusCaption.TabIndex = 0;
            this.lblStatusCaption.Text = "초기화 중 (0%)";
            //
            // btnCancel
            //
            this.btnCancel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(47)))), ((int)(((byte)(47)))), ((int)(((byte)(47)))));
            this.btnCancel.FlatAppearance.BorderSize = 0;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.Font = new System.Drawing.Font("맑은 고딕", 8.5F);
            this.btnCancel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(71)))), ((int)(((byte)(140)))), ((int)(((byte)(226)))));
            this.btnCancel.Location = new System.Drawing.Point(568, 27);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(56, 25);
            this.btnCancel.TabIndex = 0;
            this.btnCancel.Text = "취소";
            this.btnCancel.UseVisualStyleBackColor = false;
            this.btnCancel.Click += new System.EventHandler(this.BtnCancel_Click);
            //
            // lblStatus
            //
            this.lblStatus.AccessibleName = "초기화 진행 상태";
            this.lblStatus.AutoEllipsis = true;
            this.lblStatus.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(47)))), ((int)(((byte)(47)))), ((int)(((byte)(47)))));
            this.lblStatus.Font = new System.Drawing.Font("맑은 고딕", 8F);
            this.lblStatus.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(96)))), ((int)(((byte)(107)))), ((int)(((byte)(126)))));
            this.lblStatus.Location = new System.Drawing.Point(25, 54);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(598, 20);
            this.lblStatus.TabIndex = 1;
            this.lblStatus.Text = "시스템 시작을 준비하고 있습니다...";
            //
            // progressBoot
            //
            this.progressBoot.AccessibleName = "초기화 진행률";
            this.progressBoot.IsMarquee = true;
            this.progressBoot.Location = new System.Drawing.Point(25, 82);
            this.progressBoot.Name = "progressBoot";
            this.progressBoot.Size = new System.Drawing.Size(599, 9);
            this.progressBoot.TabIndex = 2;
            this.progressBoot.TabStop = false;
            this.progressBoot.Value = 0;
            //
            // lblProgressHint
            //
            this.lblProgressHint.AutoEllipsis = true;
            this.lblProgressHint.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(47)))), ((int)(((byte)(47)))), ((int)(((byte)(47)))));
            this.lblProgressHint.Font = new System.Drawing.Font("맑은 고딕", 7.5F);
            this.lblProgressHint.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(131)))), ((int)(((byte)(141)))), ((int)(((byte)(160)))));
            this.lblProgressHint.Location = new System.Drawing.Point(25, 97);
            this.lblProgressHint.Name = "lblProgressHint";
            this.lblProgressHint.Size = new System.Drawing.Size(390, 19);
            this.lblProgressHint.TabIndex = 3;
            this.lblProgressHint.Text = "초기화가 끝나면 메인 화면으로 이동합니다.";
            //
            // lblFooter
            //
            this.lblFooter.AutoEllipsis = true;
            this.lblFooter.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(47)))), ((int)(((byte)(47)))), ((int)(((byte)(47)))));
            this.lblFooter.Font = new System.Drawing.Font("Segoe UI", 7.5F);
            this.lblFooter.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(148)))), ((int)(((byte)(157)))), ((int)(((byte)(174)))));
            this.lblFooter.Location = new System.Drawing.Point(414, 97);
            this.lblFooter.Name = "lblFooter";
            this.lblFooter.Size = new System.Drawing.Size(210, 19);
            this.lblFooter.TabIndex = 4;
            this.lblFooter.Text = "산업용 비전 시스템";
            this.lblFooter.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // pnlHero
            //
            this.pnlHero.Controls.Add(this.picLogo);
            this.pnlHero.Controls.Add(this.lblSiteName);
            this.pnlHero.Controls.Add(this.lblCompanyName);
            this.pnlHero.Controls.Add(this.lblProductName);
            this.pnlHero.Controls.Add(this.lblSubtitle);
            this.pnlHero.Controls.Add(this.lblEquipmentName);
            this.pnlHero.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlHero.Location = new System.Drawing.Point(0, 0);
            this.pnlHero.Name = "pnlHero";
            this.pnlHero.PrimaryColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(0)))));
            this.pnlHero.SecondaryColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(0)))));
            this.pnlHero.Size = new System.Drawing.Size(650, 294);
            this.pnlHero.TabIndex = 1;
            //
            // picLogo
            //
            this.picLogo.BackColor = System.Drawing.Color.Transparent;
            this.picLogo.Enabled = false;
            this.picLogo.Location = new System.Drawing.Point(269, 49);
            this.picLogo.Name = "picLogo";
            this.picLogo.Size = new System.Drawing.Size(112, 112);
            this.picLogo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picLogo.TabIndex = 0;
            this.picLogo.TabStop = false;
            this.picLogo.Paint += new System.Windows.Forms.PaintEventHandler(this.PicLogo_Paint);
            //
            // lblSiteName
            //
            this.lblSiteName.AutoEllipsis = true;
            this.lblSiteName.BackColor = System.Drawing.Color.Transparent;
            this.lblSiteName.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.lblSiteName.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(231)))), ((int)(((byte)(255)))));
            this.lblSiteName.Location = new System.Drawing.Point(24, 18);
            this.lblSiteName.Name = "lblSiteName";
            this.lblSiteName.Size = new System.Drawing.Size(410, 22);
            this.lblSiteName.TabIndex = 1;
            this.lblSiteName.Text = "비전 검사 시스템";
            //
            // lblCompanyName
            //
            this.lblCompanyName.AutoEllipsis = true;
            this.lblCompanyName.BackColor = System.Drawing.Color.Transparent;
            this.lblCompanyName.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.lblCompanyName.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(231)))), ((int)(((byte)(255)))));
            this.lblCompanyName.Location = new System.Drawing.Point(465, 18);
            this.lblCompanyName.Name = "lblCompanyName";
            this.lblCompanyName.Size = new System.Drawing.Size(160, 22);
            this.lblCompanyName.TabIndex = 2;
            this.lblCompanyName.Text = "OmniSight.Base";
            this.lblCompanyName.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // lblProductName
            //
            this.lblProductName.AutoEllipsis = true;
            this.lblProductName.BackColor = System.Drawing.Color.Transparent;
            this.lblProductName.Font = new System.Drawing.Font("Segoe UI", 25F, System.Drawing.FontStyle.Bold);
            this.lblProductName.ForeColor = System.Drawing.Color.White;
            this.lblProductName.Location = new System.Drawing.Point(28, 176);
            this.lblProductName.Name = "lblProductName";
            this.lblProductName.Size = new System.Drawing.Size(594, 54);
            this.lblProductName.TabIndex = 3;
            this.lblProductName.Text = "OmniSight.Base";
            this.lblProductName.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // lblSubtitle
            //
            this.lblSubtitle.AutoEllipsis = true;
            this.lblSubtitle.BackColor = System.Drawing.Color.Transparent;
            this.lblSubtitle.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblSubtitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(211)))), ((int)(((byte)(226)))), ((int)(((byte)(255)))));
            this.lblSubtitle.Location = new System.Drawing.Point(38, 232);
            this.lblSubtitle.Name = "lblSubtitle";
            this.lblSubtitle.Size = new System.Drawing.Size(574, 25);
            this.lblSubtitle.TabIndex = 4;
            this.lblSubtitle.Text = "FA 설비 시작 템플릿";
            this.lblSubtitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // lblEquipmentName
            //
            this.lblEquipmentName.AutoEllipsis = true;
            this.lblEquipmentName.BackColor = System.Drawing.Color.Transparent;
            this.lblEquipmentName.Font = new System.Drawing.Font("맑은 고딕", 7.5F);
            this.lblEquipmentName.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(193)))), ((int)(((byte)(214)))), ((int)(((byte)(255)))));
            this.lblEquipmentName.Location = new System.Drawing.Point(100, 261);
            this.lblEquipmentName.Name = "lblEquipmentName";
            this.lblEquipmentName.Size = new System.Drawing.Size(450, 18);
            this.lblEquipmentName.TabIndex = 5;
            this.lblEquipmentName.Text = "공장 자동화";
            this.lblEquipmentName.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // StartupForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(650, 420);
            this.ControlBox = false;
            this.Controls.Add(this.pnlProgress);
            this.Controls.Add(this.pnlHero);
            this.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(236)))), ((int)(((byte)(236)))), ((int)(((byte)(236)))));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "StartupForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "OmniSight.Base";
            this.TopMost = true;
            this.pnlProgress.ResumeLayout(false);
            this.pnlHero.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.picLogo)).EndInit();
            this.ResumeLayout(false);

        }
    }
}
