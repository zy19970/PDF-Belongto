using PdfOwnershipMarker.Models;
using PdfOwnershipMarker.Services;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PdfOwnershipMarker
{
    internal partial class ExifToolDownloadForm : Form
    {
        private readonly ExifToolService service;
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();
        private bool finished;

        public ExifToolDownloadForm(ExifToolService service)
        {
            this.service = service;
            InitializeComponent();
        }

        private async void ExifToolDownloadForm_Shown(object sender, EventArgs e)
        {
            await StartInstallAsync();
        }

        private async Task StartInstallAsync()
        {
            cancelButton.Enabled = true;
            closeButton.Enabled = false;

            Progress<InstallProgress> progress = new Progress<InstallProgress>(UpdateProgress);

            try
            {
                await service.DownloadAndInstallAsync(progress, cancellation.Token);
                finished = true;
                progressBar.Style = ProgressBarStyle.Continuous;
                progressBar.Value = 100;
                statusLabel.Text = "ExifTool 已安装完成";
                detailLabel.Text = "组件已放到软件运行根目录。";
                cancelButton.Enabled = false;
                closeButton.Enabled = true;
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (OperationCanceledException)
            {
                finished = true;
                statusLabel.Text = "下载已取消";
                detailLabel.Text = "下次使用时会重新下载。";
                cancelButton.Enabled = false;
                closeButton.Enabled = true;
                DialogResult = DialogResult.Cancel;
            }
            catch (Exception ex)
            {
                finished = true;
                progressBar.Style = ProgressBarStyle.Continuous;
                progressBar.Value = 0;
                statusLabel.Text = "ExifTool 下载或安装失败";
                detailLabel.Text = ex.Message;
                cancelButton.Enabled = false;
                closeButton.Enabled = true;
                DialogResult = DialogResult.Abort;
            }
        }

        private void UpdateProgress(InstallProgress info)
        {
            statusLabel.Text = string.IsNullOrWhiteSpace(info.Stage) ? "正在初始化…" : info.Stage;

            if (info.IsIndeterminate)
            {
                progressBar.Style = ProgressBarStyle.Marquee;
                detailLabel.Text = info.BytesReceived > 0 ? FormatBytes(info.BytesReceived) : "请稍候…";
                return;
            }

            progressBar.Style = ProgressBarStyle.Continuous;
            int value = Math.Max(0, Math.Min(100, info.Percentage));
            progressBar.Value = value;

            if (info.TotalBytes > 0)
            {
                detailLabel.Text = string.Format("{0} / {1}    {2}%",
                    FormatBytes(info.BytesReceived), FormatBytes(info.TotalBytes), value);
            }
            else if (info.BytesReceived > 0)
            {
                detailLabel.Text = FormatBytes(info.BytesReceived);
            }
            else
            {
                detailLabel.Text = value + "%";
            }
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            cancelButton.Enabled = false;
            statusLabel.Text = "正在取消…";
            cancellation.Cancel();
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void ExifToolDownloadForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!finished)
            {
                cancellation.Cancel();
            }
        }

        private static string FormatBytes(long bytes)
        {
            double value = bytes;
            string[] units = { "B", "KB", "MB", "GB" };
            int unit = 0;
            while (value >= 1024 && unit < units.Length - 1)
            {
                value /= 1024;
                unit++;
            }
            return value.ToString(unit >= 2 ? "0.00" : "0.0") + " " + units[unit];
        }
    }
}
