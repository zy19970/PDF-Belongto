using System.Drawing;
using System.Windows.Forms;

namespace PdfOwnershipMarker
{
    partial class ExifToolDownloadForm
    {
        private System.ComponentModel.IContainer components = null;
        private Label titleLabel;
        private Label statusLabel;
        private ProgressBar progressBar;
        private Label detailLabel;
        private Button cancelButton;
        private Button closeButton;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null) components.Dispose();
                cancellation.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.titleLabel = new Label();
            this.statusLabel = new Label();
            this.progressBar = new ProgressBar();
            this.detailLabel = new Label();
            this.cancelButton = new Button();
            this.closeButton = new Button();
            this.SuspendLayout();

            this.titleLabel.AutoSize = true;
            this.titleLabel.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
            this.titleLabel.Location = new Point(20, 18);
            this.titleLabel.Text = "首次运行：初始化 ExifTool 13.59";

            this.statusLabel.AutoEllipsis = true;
            this.statusLabel.Location = new Point(21, 54);
            this.statusLabel.Size = new Size(428, 22);
            this.statusLabel.Text = "正在准备下载…";

            this.progressBar.Location = new Point(24, 83);
            this.progressBar.Size = new Size(425, 22);
            this.progressBar.Style = ProgressBarStyle.Continuous;

            this.detailLabel.AutoEllipsis = true;
            this.detailLabel.ForeColor = Color.DimGray;
            this.detailLabel.Location = new Point(21, 114);
            this.detailLabel.Size = new Size(428, 38);
            this.detailLabel.Text = "下载完成后会自动解压到软件根目录。";

            this.cancelButton.Location = new Point(284, 160);
            this.cancelButton.Size = new Size(78, 30);
            this.cancelButton.Text = "取消";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.CancelButton_Click);

            this.closeButton.Enabled = false;
            this.closeButton.Location = new Point(371, 160);
            this.closeButton.Size = new Size(78, 30);
            this.closeButton.Text = "关闭";
            this.closeButton.UseVisualStyleBackColor = true;
            this.closeButton.Click += new System.EventHandler(this.CloseButton_Click);

            this.AcceptButton = this.closeButton;
            this.AutoScaleDimensions = new SizeF(7F, 17F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(474, 208);
            this.Controls.Add(this.titleLabel);
            this.Controls.Add(this.statusLabel);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.detailLabel);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.closeButton);
            this.Font = new Font("Microsoft YaHei UI", 9F);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ExifToolDownloadForm";
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "初始化组件";
            this.FormClosing += new FormClosingEventHandler(this.ExifToolDownloadForm_FormClosing);
            this.Shown += new System.EventHandler(this.ExifToolDownloadForm_Shown);
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
