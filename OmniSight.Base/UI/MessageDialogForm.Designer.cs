namespace OmniSight.Base.UI.Forms
{
    partial class MessageDialogForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ReleaseRuntimeResources();
                if (components != null) components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.pnlAccent = new System.Windows.Forms.Panel();
            this.pnlHeader = new System.Windows.Forms.Panel();
            this.lblCountdown = new System.Windows.Forms.Label();
            this.lblCaption = new System.Windows.Forms.Label();
            this.lblSeverity = new System.Windows.Forms.Label();
            this.pnlBody = new System.Windows.Forms.Panel();
            this.tlpBody = new System.Windows.Forms.TableLayoutPanel();
            this.pnlIconSurface = new System.Windows.Forms.Panel();
            this.picIcon = new System.Windows.Forms.PictureBox();
            this.lblMessage = new System.Windows.Forms.Label();
            this.pnlFooter = new System.Windows.Forms.Panel();
            this.pnlDivider = new System.Windows.Forms.Panel();
            this.flpButtons = new System.Windows.Forms.FlowLayoutPanel();
            this.btnOk = new System.Windows.Forms.Button();
            this.btnYes = new System.Windows.Forms.Button();
            this.btnNo = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.pnlHeader.SuspendLayout();
            this.pnlBody.SuspendLayout();
            this.tlpBody.SuspendLayout();
            this.pnlIconSurface.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picIcon)).BeginInit();
            this.pnlFooter.SuspendLayout();
            this.flpButtons.SuspendLayout();
            this.SuspendLayout();
            //
            // pnlAccent
            //
            this.pnlAccent.BackColor = System.Drawing.Color.FromArgb(37, 99, 235);
            this.pnlAccent.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlAccent.Location = new System.Drawing.Point(0, 0);
            this.pnlAccent.Margin = new System.Windows.Forms.Padding(0);
            this.pnlAccent.ForeColor = System.Drawing.Color.FromArgb(236, 236, 236);
            this.pnlAccent.Name = "pnlAccent";
            this.pnlAccent.Size = new System.Drawing.Size(600, 6);
            this.pnlAccent.TabIndex = 0;
            //
            // pnlHeader
            //
            this.pnlHeader.BackColor = System.Drawing.Color.FromArgb(47, 47, 47);
            this.pnlHeader.Controls.Add(this.lblCountdown);
            this.pnlHeader.Controls.Add(this.lblCaption);
            this.pnlHeader.Controls.Add(this.lblSeverity);
            this.pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlHeader.Location = new System.Drawing.Point(0, 6);
            this.pnlHeader.ForeColor = System.Drawing.Color.FromArgb(236, 236, 236);
            this.pnlHeader.Name = "pnlHeader";
            this.pnlHeader.Size = new System.Drawing.Size(600, 68);
            this.pnlHeader.TabIndex = 1;
            this.pnlHeader.MouseDown += new System.Windows.Forms.MouseEventHandler(this.PnlTop_MouseDown);
            //
            // lblCountdown
            //
            this.lblCountdown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblCountdown.AutoEllipsis = true;
            this.lblCountdown.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblCountdown.ForeColor = System.Drawing.Color.FromArgb(236, 236, 236);
            this.lblCountdown.Location = new System.Drawing.Point(452, 21);
            this.lblCountdown.BackColor = System.Drawing.Color.FromArgb(47, 47, 47);
            this.lblCountdown.Name = "lblCountdown";
            this.lblCountdown.Size = new System.Drawing.Size(124, 28);
            this.lblCountdown.TabIndex = 2;
            this.lblCountdown.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.lblCountdown.MouseDown += new System.Windows.Forms.MouseEventHandler(this.PnlTop_MouseDown);
            //
            // lblCaption
            //
            this.lblCaption.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.lblCaption.AutoEllipsis = true;
            this.lblCaption.Font = new System.Drawing.Font("맑은 고딕", 13F, System.Drawing.FontStyle.Bold);
            this.lblCaption.ForeColor = System.Drawing.Color.FromArgb(236, 236, 236);
            this.lblCaption.Location = new System.Drawing.Point(24, 29);
            this.lblCaption.BackColor = System.Drawing.Color.FromArgb(47, 47, 47);
            this.lblCaption.Name = "lblCaption";
            this.lblCaption.Size = new System.Drawing.Size(416, 30);
            this.lblCaption.TabIndex = 1;
            this.lblCaption.Text = "메시지 제목";
            this.lblCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblCaption.MouseDown += new System.Windows.Forms.MouseEventHandler(this.PnlTop_MouseDown);
            //
            // lblSeverity
            //
            this.lblSeverity.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.lblSeverity.AutoEllipsis = true;
            this.lblSeverity.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.lblSeverity.ForeColor = System.Drawing.Color.FromArgb(37, 99, 235);
            this.lblSeverity.Location = new System.Drawing.Point(24, 9);
            this.lblSeverity.BackColor = System.Drawing.Color.FromArgb(47, 47, 47);
            this.lblSeverity.Name = "lblSeverity";
            this.lblSeverity.Size = new System.Drawing.Size(416, 20);
            this.lblSeverity.TabIndex = 0;
            this.lblSeverity.Text = "INFORMATION";
            this.lblSeverity.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblSeverity.MouseDown += new System.Windows.Forms.MouseEventHandler(this.PnlTop_MouseDown);
            //
            // pnlBody
            //
            this.pnlBody.BackColor = System.Drawing.Color.FromArgb(47, 47, 47);
            this.pnlBody.Controls.Add(this.tlpBody);
            this.pnlBody.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlBody.Location = new System.Drawing.Point(0, 74);
            this.pnlBody.ForeColor = System.Drawing.Color.FromArgb(236, 236, 236);
            this.pnlBody.Name = "pnlBody";
            this.pnlBody.Padding = new System.Windows.Forms.Padding(24, 20, 24, 18);
            this.pnlBody.Size = new System.Drawing.Size(600, 132);
            this.pnlBody.TabIndex = 2;
            //
            // tlpBody
            //
            this.tlpBody.ColumnCount = 2;
            this.tlpBody.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 76F));
            this.tlpBody.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpBody.Controls.Add(this.pnlIconSurface, 0, 0);
            this.tlpBody.Controls.Add(this.lblMessage, 1, 0);
            this.tlpBody.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpBody.Location = new System.Drawing.Point(24, 20);
            this.tlpBody.Margin = new System.Windows.Forms.Padding(0);
            this.tlpBody.BackColor = System.Drawing.Color.FromArgb(47, 47, 47);
            this.tlpBody.ForeColor = System.Drawing.Color.FromArgb(236, 236, 236);
            this.tlpBody.Name = "tlpBody";
            this.tlpBody.RowCount = 1;
            this.tlpBody.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpBody.Size = new System.Drawing.Size(552, 94);
            this.tlpBody.TabIndex = 0;
            //
            // pnlIconSurface
            //
            this.pnlIconSurface.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.pnlIconSurface.BackColor = System.Drawing.Color.FromArgb(239, 246, 255);
            this.pnlIconSurface.Controls.Add(this.picIcon);
            this.pnlIconSurface.Location = new System.Drawing.Point(6, 2);
            this.pnlIconSurface.Margin = new System.Windows.Forms.Padding(0, 2, 0, 0);
            this.pnlIconSurface.ForeColor = System.Drawing.Color.FromArgb(236, 236, 236);
            this.pnlIconSurface.Name = "pnlIconSurface";
            this.pnlIconSurface.Padding = new System.Windows.Forms.Padding(12);
            this.pnlIconSurface.Size = new System.Drawing.Size(64, 64);
            this.pnlIconSurface.TabIndex = 0;
            //
            // picIcon
            //
            this.picIcon.Dock = System.Windows.Forms.DockStyle.Fill;
            this.picIcon.Location = new System.Drawing.Point(12, 12);
            this.picIcon.Margin = new System.Windows.Forms.Padding(0);
            this.picIcon.Name = "picIcon";
            this.picIcon.Size = new System.Drawing.Size(40, 40);
            this.picIcon.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picIcon.TabIndex = 0;
            this.picIcon.TabStop = false;
            //
            // lblMessage
            //
            this.lblMessage.AutoEllipsis = true;
            this.lblMessage.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMessage.Font = new System.Drawing.Font("맑은 고딕", 10.5F);
            this.lblMessage.ForeColor = System.Drawing.Color.FromArgb(236, 236, 236);
            this.lblMessage.Location = new System.Drawing.Point(88, 0);
            this.lblMessage.Margin = new System.Windows.Forms.Padding(12, 0, 0, 0);
            this.lblMessage.BackColor = System.Drawing.Color.FromArgb(47, 47, 47);
            this.lblMessage.Name = "lblMessage";
            this.lblMessage.Size = new System.Drawing.Size(464, 94);
            this.lblMessage.TabIndex = 1;
            this.lblMessage.Text = "작업자가 읽어야 할 메시지가 여기에 표시됩니다.";
            this.lblMessage.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // pnlFooter
            //
            this.pnlFooter.BackColor = System.Drawing.Color.FromArgb(47, 47, 47);
            this.pnlFooter.Controls.Add(this.flpButtons);
            this.pnlFooter.Controls.Add(this.pnlDivider);
            this.pnlFooter.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlFooter.Location = new System.Drawing.Point(0, 206);
            this.pnlFooter.ForeColor = System.Drawing.Color.FromArgb(236, 236, 236);
            this.pnlFooter.Name = "pnlFooter";
            this.pnlFooter.Size = new System.Drawing.Size(600, 74);
            this.pnlFooter.TabIndex = 3;
            //
            // pnlDivider
            //
            this.pnlDivider.BackColor = System.Drawing.Color.FromArgb(226, 232, 240);
            this.pnlDivider.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlDivider.Location = new System.Drawing.Point(0, 0);
            this.pnlDivider.ForeColor = System.Drawing.Color.FromArgb(236, 236, 236);
            this.pnlDivider.Name = "pnlDivider";
            this.pnlDivider.Size = new System.Drawing.Size(600, 1);
            this.pnlDivider.TabIndex = 0;
            //
            // flpButtons
            //
            this.flpButtons.Controls.Add(this.btnOk);
            this.flpButtons.Controls.Add(this.btnYes);
            this.flpButtons.Controls.Add(this.btnNo);
            this.flpButtons.Controls.Add(this.btnCancel);
            this.flpButtons.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpButtons.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.flpButtons.Location = new System.Drawing.Point(0, 1);
            this.flpButtons.BackColor = System.Drawing.Color.FromArgb(47, 47, 47);
            this.flpButtons.ForeColor = System.Drawing.Color.FromArgb(236, 236, 236);
            this.flpButtons.Name = "flpButtons";
            this.flpButtons.Padding = new System.Windows.Forms.Padding(0, 16, 18, 0);
            this.flpButtons.Size = new System.Drawing.Size(600, 73);
            this.flpButtons.TabIndex = 1;
            this.flpButtons.WrapContents = false;
            //
            // btnOk
            //
            this.btnOk.BackColor = System.Drawing.Color.FromArgb(37, 99, 235);
            this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOk.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOk.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnOk.ForeColor = System.Drawing.Color.FromArgb(236, 236, 236);
            this.btnOk.Location = new System.Drawing.Point(462, 16);
            this.btnOk.Margin = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(112, 40);
            this.btnOk.TabIndex = 0;
            this.btnOk.Text = "확인(&O)";
            this.btnOk.UseVisualStyleBackColor = false;
            //
            // btnYes
            //
            this.btnYes.DialogResult = System.Windows.Forms.DialogResult.Yes;
            this.btnYes.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnYes.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnYes.Location = new System.Drawing.Point(342, 16);
            this.btnYes.Margin = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.btnYes.BackColor = System.Drawing.Color.FromArgb(64, 65, 68);
            this.btnYes.ForeColor = System.Drawing.Color.FromArgb(236, 236, 236);
            this.btnYes.Name = "btnYes";
            this.btnYes.Size = new System.Drawing.Size(112, 40);
            this.btnYes.TabIndex = 0;
            this.btnYes.Text = "예(&Y)";
            this.btnYes.UseVisualStyleBackColor = false;
            this.btnYes.Visible = false;
            //
            // btnNo
            //
            this.btnNo.DialogResult = System.Windows.Forms.DialogResult.No;
            this.btnNo.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnNo.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnNo.Location = new System.Drawing.Point(222, 16);
            this.btnNo.Margin = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.btnNo.BackColor = System.Drawing.Color.FromArgb(64, 65, 68);
            this.btnNo.ForeColor = System.Drawing.Color.FromArgb(236, 236, 236);
            this.btnNo.Name = "btnNo";
            this.btnNo.Size = new System.Drawing.Size(112, 40);
            this.btnNo.TabIndex = 1;
            this.btnNo.Text = "아니오(&N)";
            this.btnNo.UseVisualStyleBackColor = false;
            this.btnNo.Visible = false;
            //
            // btnCancel
            //
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnCancel.Location = new System.Drawing.Point(102, 16);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.btnCancel.BackColor = System.Drawing.Color.FromArgb(64, 65, 68);
            this.btnCancel.ForeColor = System.Drawing.Color.FromArgb(236, 236, 236);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(112, 40);
            this.btnCancel.TabIndex = 1;
            this.btnCancel.Text = "취소(&C)";
            this.btnCancel.UseVisualStyleBackColor = false;
            this.btnCancel.Visible = false;
            //
            // MessageDialogForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = System.Drawing.Color.FromArgb(33, 33, 33);
            this.ClientSize = new System.Drawing.Size(600, 280);
            this.Controls.Add(this.pnlBody);
            this.Controls.Add(this.pnlFooter);
            this.Controls.Add(this.pnlHeader);
            this.Controls.Add(this.pnlAccent);
            this.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ForeColor = System.Drawing.Color.FromArgb(236, 236, 236);
            this.Name = "MessageDialogForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "OmniSight Message";
            this.TopMost = false;
            this.pnlHeader.ResumeLayout(false);
            this.pnlBody.ResumeLayout(false);
            this.tlpBody.ResumeLayout(false);
            this.pnlIconSurface.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.picIcon)).EndInit();
            this.pnlFooter.ResumeLayout(false);
            this.flpButtons.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel pnlAccent;
        private System.Windows.Forms.Panel pnlHeader;
        private System.Windows.Forms.Label lblCountdown;
        private System.Windows.Forms.Label lblCaption;
        private System.Windows.Forms.Label lblSeverity;
        private System.Windows.Forms.Panel pnlBody;
        private System.Windows.Forms.TableLayoutPanel tlpBody;
        private System.Windows.Forms.Panel pnlIconSurface;
        private System.Windows.Forms.PictureBox picIcon;
        private System.Windows.Forms.Label lblMessage;
        private System.Windows.Forms.Panel pnlFooter;
        private System.Windows.Forms.Panel pnlDivider;
        private System.Windows.Forms.FlowLayoutPanel flpButtons;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnYes;
        private System.Windows.Forms.Button btnNo;
        private System.Windows.Forms.Button btnCancel;
    }
}
