using PdfOwnershipMarker.Models;
using PdfOwnershipMarker.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PdfOwnershipMarker
{
    public partial class MainForm : Form
    {
        private const string DefaultOwner = "Joey";

        private readonly ExifToolService exifTool;
        private readonly PdfMarkerService markerService;
        private readonly HashSet<string> queuedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, MarkerInfo> markerCache = new Dictionary<string, MarkerInfo>(StringComparer.OrdinalIgnoreCase);
        private readonly ManualResetEventSlim pauseGate = new ManualResetEventSlim(true);

        private bool isProcessing;
        private bool isReading;
        private bool pauseRequested;
        private bool stopRequested;

        public MainForm()
        {
            InitializeComponent();

            exifTool = new ExifToolService();
            markerService = new PdfMarkerService(exifTool);
            ownerTextBox.Text = DefaultOwner;

            try
            {
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
                if (File.Exists(iconPath))
                    this.Icon = new System.Drawing.Icon(iconPath);
            }
            catch { }
            UpdateSelectionLabel();
            UpdateUiState();
        }

        private void AddFilesButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Title = "添加 PDF 文件";
                dlg.Filter = "PDF 文件 (*.pdf)|*.pdf";
                dlg.Multiselect = true;
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    AddPathsToQueue(dlg.FileNames);
            }
        }

        private void AddFolderButton_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dlg = new FolderBrowserDialog())
            {
                dlg.Description = "选择包含 PDF 的文件夹（会递归扫描子文件夹）";
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    AddPathsToQueue(new[] { dlg.SelectedPath });
            }
        }

        private async void ReadButton_Click(object sender, EventArgs e)
        {
            if (isProcessing || isReading) return;

            if (resultGrid.Rows.Count == 0)
            {
                MessageBox.Show(this,
                    "当前列表为空。\r\n\r\n请先通过“添加文件”“添加文件夹”或拖拽方式把 PDF 加入列表。",
                    "读取归属标记", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            List<DataGridViewRow> rows = GetRowsForRead();
            await ReadRowsAsync(rows, rows.Count == 1);
        }

        private async Task ReadRowsAsync(List<DataGridViewRow> rows, bool showSingleDetails)
        {
            if (rows == null || rows.Count == 0) return;
            if (!exifTool.EnsureInstalled(this)) return;

            isReading = true;
            UpdateUiState();
            progressBar.Minimum = 0;
            progressBar.Maximum = Math.Max(1, rows.Count);
            progressBar.Value = 0;

            int found = 0;
            int none = 0;
            int failed = 0;
            MarkerInfo singleMarker = null;
            string singleFile = null;

            try
            {
                for (int i = 0; i < rows.Count; i++)
                {
                    DataGridViewRow row = rows[i];
                    string file = Convert.ToString(row.Tag);
                    if (string.IsNullOrWhiteSpace(file)) continue;

                    row.Cells[1].Value = "读取中…";
                    statusLabel.Text = string.Format("正在读取 [{0}/{1}] {2}", i + 1, rows.Count, file);
                    ScrollToRow(row.Index);

                    try
                    {
                        MarkerInfo marker = await Task.Run(() => markerService.ReadMarker(file));
                        markerCache[file] = marker ?? new MarkerInfo();
                        ApplyMarkerToRow(row, marker);

                        if (marker != null && !string.IsNullOrWhiteSpace(marker.Owner))
                        {
                            row.Cells[1].Value = "已读取";
                            found++;
                            if (rows.Count == 1)
                            {
                                singleMarker = marker;
                                singleFile = file;
                            }
                        }
                        else
                        {
                            row.Cells[1].Value = "未检测到标记";
                            none++;
                        }
                    }
                    catch (Exception ex)
                    {
                        row.Cells[1].Value = "读取失败：" + ShortMessage(ex.Message);
                        failed++;
                    }

                    progressBar.Value = Math.Min(progressBar.Maximum, i + 1);
                }
            }
            finally
            {
                isReading = false;
                UpdateUiState();
            }

            statusLabel.Text = string.Format("读取结束：有标记 {0}，无标记 {1}，失败 {2}。读取过程不会修改 PDF。", found, none, failed);

            if (showSingleDetails && rows.Count == 1)
            {
                if (singleMarker != null)
                    ShowMarkerDetails(singleFile, singleMarker);
                else if (failed == 0)
                    MessageBox.Show(this, "未检测到本软件写入的归属标记。\r\n\r\n本次操作仅进行了读取，没有修改 PDF。", "读取结果", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private async void StartButton_Click(object sender, EventArgs e)
        {
            if (isProcessing || isReading) return;
            if (resultGrid.Rows.Count == 0)
            {
                MessageBox.Show(this, "请先添加 PDF 文件或文件夹。", "PDF归属标记器", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string owner = (ownerTextBox.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(owner))
            {
                MessageBox.Show(this, "请先填写归属人。", "PDF归属标记器", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ownerTextBox.Focus();
                return;
            }

            if (!exifTool.EnsureInstalled(this)) return;

            DialogResult confirm = MessageBox.Show(this,
                string.Format(
                    "共 {0} 个 PDF。\r\n归属人：{1}\r\n\r\n程序会直接修改并覆盖原文件；文件名和页面内容不变。\r\n你已自行备份原文件。\r\n\r\n开始处理吗？",
                    resultGrid.Rows.Count, owner),
                "确认覆盖原文件", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;

            isProcessing = true;
            pauseRequested = false;
            stopRequested = false;
            pauseGate.Set();
            pauseButton.Text = "暂停";
            UpdateUiState();

            progressBar.Minimum = 0;
            progressBar.Maximum = resultGrid.Rows.Count;
            progressBar.Value = 0;

            int ok = 0;
            int skipped = 0;
            int failed = 0;
            int processed = 0;

            try
            {
                for (int i = 0; i < resultGrid.Rows.Count; i++)
                {
                    if (stopRequested) break;

                    if (pauseRequested)
                    {
                        statusLabel.Text = "已暂停。点击“继续”恢复处理，或点击“停止”结束任务。";
                        await Task.Run(() => pauseGate.Wait());
                    }

                    if (stopRequested) break;

                    DataGridViewRow row = resultGrid.Rows[i];
                    string file = Convert.ToString(row.Tag);
                    if (string.IsNullOrWhiteSpace(file)) continue;

                    row.Cells[1].Value = "处理中…";
                    statusLabel.Text = string.Format("正在处理 [{0}/{1}] {2}", i + 1, resultGrid.Rows.Count, file);
                    ScrollToRow(row.Index);

                    try
                    {
                        WorkResult result = await Task.Run(() => ProcessOne(file, owner));
                        row.Cells[1].Value = result.Status;
                        ApplyMarkerToRow(row, result.Marker);
                        if (result.Marker != null) markerCache[file] = result.Marker;

                        if (result.Kind == WorkResultKind.Completed) ok++;
                        else if (result.Kind == WorkResultKind.Skipped) skipped++;
                        else failed++;
                    }
                    catch (Exception ex)
                    {
                        row.Cells[1].Value = "失败：" + ShortMessage(ex.Message);
                        failed++;
                    }

                    processed++;
                    progressBar.Value = processed;
                }
            }
            finally
            {
                isProcessing = false;
                pauseRequested = false;
                pauseGate.Set();
                pauseButton.Text = "暂停";
                UpdateUiState();
            }

            if (stopRequested)
            {
                statusLabel.Text = string.Format("已停止：完成 {0}，跳过 {1}，失败 {2}，已处理 {3}/{4}。", ok, skipped, failed, processed, resultGrid.Rows.Count);
            }
            else
            {
                statusLabel.Text = string.Format("处理结束：完成 {0}，跳过 {1}，失败 {2}。", ok, skipped, failed);
                MessageBox.Show(this,
                    string.Format("完成：{0}\r\n跳过：{1}\r\n失败：{2}", ok, skipped, failed),
                    "处理结束", MessageBoxButtons.OK, failed == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }

            stopRequested = false;
        }

        private void PauseButton_Click(object sender, EventArgs e)
        {
            if (!isProcessing || stopRequested) return;

            if (!pauseRequested)
            {
                pauseRequested = true;
                pauseGate.Reset();
                pauseButton.Text = "继续";
                statusLabel.Text = "暂停已请求：当前 PDF 完成后暂停。";
            }
            else
            {
                pauseRequested = false;
                pauseGate.Set();
                pauseButton.Text = "暂停";
                statusLabel.Text = "继续处理…";
            }
        }

        private void StopButton_Click(object sender, EventArgs e)
        {
            if (!isProcessing) return;

            stopRequested = true;
            pauseRequested = false;
            pauseGate.Set();
            pauseButton.Text = "暂停";
            pauseButton.Enabled = false;
            stopButton.Enabled = false;
            statusLabel.Text = "停止已请求：为避免损坏 PDF，会在当前文件处理完成后停止。";
        }

        private void ClearButton_Click(object sender, EventArgs e)
        {
            if (isProcessing || isReading) return;
            queuedFiles.Clear();
            markerCache.Clear();
            resultGrid.Rows.Clear();
            progressBar.Value = 0;
            statusLabel.Text = "等待添加 PDF 文件或文件夹…";
            UpdateSelectionLabel();
            UpdateUiState();
        }

        private void RemoveSelectedButton_Click(object sender, EventArgs e)
        {
            RemoveSelectedRows();
        }

        private void RemoveSelectedRows()
        {
            if (isProcessing || isReading) return;

            if (resultGrid.SelectedRows.Count == 0)
            {
                MessageBox.Show(this,
                    "请先在列表中选中要移出的 PDF。\r\n\r\n该操作只会从当前列表移除，不会删除磁盘上的 PDF 文件。",
                    "移出列表", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            List<DataGridViewRow> rows = new List<DataGridViewRow>();
            foreach (DataGridViewRow row in resultGrid.SelectedRows)
                rows.Add(row);
            rows.Sort((a, b) => b.Index.CompareTo(a.Index));

            int removed = 0;
            foreach (DataGridViewRow row in rows)
            {
                string file = Convert.ToString(row.Tag);
                if (!string.IsNullOrWhiteSpace(file))
                {
                    queuedFiles.Remove(file);
                    markerCache.Remove(file);
                }

                if (!row.IsNewRow)
                {
                    resultGrid.Rows.Remove(row);
                    removed++;
                }
            }

            progressBar.Minimum = 0;
            progressBar.Maximum = Math.Max(1, resultGrid.Rows.Count);
            progressBar.Value = 0;
            statusLabel.Text = string.Format("已从列表移出 {0} 个 PDF；磁盘文件未删除。当前列表共 {1} 个。", removed, resultGrid.Rows.Count);
            UpdateSelectionLabel();
            UpdateUiState();
        }

        private void ResultGrid_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete && !isProcessing && !isReading)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                RemoveSelectedRows();
            }
        }

        private void ResultGrid_SelectionChanged(object sender, EventArgs e)
        {
            UpdateSelectionLabel();
            UpdateUiState();
        }

        private void ResultGrid_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;
            if (e.RowIndex < 0 || e.RowIndex >= resultGrid.Rows.Count) return;

            DataGridViewRow row = resultGrid.Rows[e.RowIndex];
            if (!row.Selected)
            {
                resultGrid.ClearSelection();
                row.Selected = true;
            }
        }

        private void RowContextMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            bool idle = !isProcessing && !isReading;
            bool hasRows = resultGrid.Rows.Count > 0;
            bool hasSelection = resultGrid.SelectedRows.Count > 0;
            contextReadItem.Enabled = idle && hasRows;
            contextRemoveItem.Enabled = idle && hasSelection;
        }

        private async void ContextReadItem_Click(object sender, EventArgs e)
        {
            if (isProcessing || isReading || resultGrid.Rows.Count == 0) return;
            List<DataGridViewRow> rows = GetRowsForRead();
            await ReadRowsAsync(rows, rows.Count == 1);
        }

        private void ContextRemoveItem_Click(object sender, EventArgs e)
        {
            RemoveSelectedRows();
        }

        private void UpdateSelectionLabel()
        {
            if (selectionLabel == null || resultGrid == null) return;
            selectionLabel.Text = string.Format("共 {0} 个，已选 {1} 个", resultGrid.Rows.Count, resultGrid.SelectedRows.Count);
        }

        private void DragEnter_Handler(object sender, DragEventArgs e)
        {
            if (isProcessing || isReading)
            {
                e.Effect = DragDropEffects.None;
                return;
            }

            e.Effect = e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop)
                ? DragDropEffects.Copy
                : DragDropEffects.None;
        }

        private void DragDrop_Handler(object sender, DragEventArgs e)
        {
            if (isProcessing || isReading) return;
            string[] paths = e.Data == null ? null : e.Data.GetData(DataFormats.FileDrop) as string[];
            if (paths != null && paths.Length > 0)
                AddPathsToQueue(paths);
        }

        private async void ResultGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (isProcessing || isReading) return;
            if (e.RowIndex < 0 || e.RowIndex >= resultGrid.Rows.Count) return;

            DataGridViewRow row = resultGrid.Rows[e.RowIndex];
            string file = Convert.ToString(row.Tag);
            if (string.IsNullOrWhiteSpace(file)) return;

            MarkerInfo marker;
            if (markerCache.TryGetValue(file, out marker) && marker != null && !string.IsNullOrWhiteSpace(marker.Owner))
            {
                ShowMarkerDetails(file, marker);
                return;
            }

            // 双击一个尚未读取的 PDF 时，直接执行一次只读检查，不需要先“开始”。
            await ReadRowsAsync(new List<DataGridViewRow> { row }, true);
        }

        private void AddPathsToQueue(IEnumerable<string> paths)
        {
            int added = 0;
            int ignored = 0;
            List<string> discovered = new List<string>();

            foreach (string raw in paths ?? Enumerable.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;

                try
                {
                    if (File.Exists(raw))
                    {
                        if (string.Equals(Path.GetExtension(raw), ".pdf", StringComparison.OrdinalIgnoreCase))
                            discovered.Add(Path.GetFullPath(raw));
                        else
                            ignored++;
                    }
                    else if (Directory.Exists(raw))
                    {
                        discovered.AddRange(CollectPdfFiles(raw));
                    }
                    else
                    {
                        ignored++;
                    }
                }
                catch
                {
                    ignored++;
                }
            }

            List<DataGridViewRow> newlyAddedRows = new List<DataGridViewRow>();
            foreach (string file in discovered.OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase))
            {
                if (!queuedFiles.Add(file)) continue;

                int rowIndex = resultGrid.Rows.Add(file, "待操作", string.Empty, string.Empty, string.Empty);
                DataGridViewRow row = resultGrid.Rows[rowIndex];
                row.Tag = file;
                newlyAddedRows.Add(row);
                added++;
            }

            resultGrid.ClearSelection();
            foreach (DataGridViewRow row in newlyAddedRows)
                row.Selected = true;

            if (added > 0)
                statusLabel.Text = string.Format("已添加 {0} 个 PDF，并已选中。可直接点击“读取标记”或“开始写入”。当前列表共 {1} 个。", added, resultGrid.Rows.Count);
            else if (ignored > 0)
                statusLabel.Text = "没有添加新的 PDF 文件。";

            progressBar.Minimum = 0;
            progressBar.Maximum = Math.Max(1, resultGrid.Rows.Count);
            progressBar.Value = 0;
            UpdateSelectionLabel();
            UpdateUiState();
        }

        private IEnumerable<string> CollectPdfFiles(string root)
        {
            List<string> result = new List<string>();
            Stack<string> pending = new Stack<string>();
            pending.Push(root);

            while (pending.Count > 0)
            {
                string dir = pending.Pop();
                try
                {
                    foreach (string file in Directory.GetFiles(dir, "*.pdf", SearchOption.TopDirectoryOnly))
                        result.Add(Path.GetFullPath(file));
                }
                catch { }

                try
                {
                    foreach (string sub in Directory.GetDirectories(dir))
                        pending.Push(sub);
                }
                catch { }
            }

            return result;
        }

        private WorkResult ProcessOne(string file, string owner)
        {
            if (markerService.LooksSigned(file))
                return WorkResult.Skipped("跳过：数字签名", null);

            MarkerInfo current = markerService.ReadMarker(file);
            if (current != null && string.Equals(current.Owner, owner, StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(current.ProvenanceId))
                return WorkResult.Skipped("跳过：已有当前归属", current);

            if (current != null && !string.IsNullOrWhiteSpace(current.Owner) && !string.Equals(current.Owner, owner, StringComparison.Ordinal))
                return WorkResult.Skipped("跳过：已有其他归属", current);

            markerService.WriteMarker(file, owner);
            MarkerInfo written = markerService.ReadMarker(file);
            return WorkResult.Completed("完成", written);
        }

        private List<DataGridViewRow> GetRowsForRead()
        {
            List<DataGridViewRow> rows = new List<DataGridViewRow>();
            if (resultGrid.SelectedRows.Count > 0)
            {
                foreach (DataGridViewRow row in resultGrid.SelectedRows)
                    rows.Add(row);
                rows.Sort((a, b) => a.Index.CompareTo(b.Index));
            }
            else
            {
                foreach (DataGridViewRow row in resultGrid.Rows)
                    rows.Add(row);
            }
            return rows;
        }

        private static void ApplyMarkerToRow(DataGridViewRow row, MarkerInfo marker)
        {
            if (row == null) return;
            if (marker == null || string.IsNullOrWhiteSpace(marker.Owner))
            {
                row.Cells[2].Value = string.Empty;
                row.Cells[3].Value = string.Empty;
                row.Cells[4].Value = string.Empty;
                return;
            }

            row.Cells[2].Value = marker.Owner ?? string.Empty;
            row.Cells[3].Value = marker.ProvenanceId ?? string.Empty;
            row.Cells[4].Value = marker.MarkedAt ?? string.Empty;
        }

        private void ShowMarkerDetails(string file, MarkerInfo marker)
        {
            string message = string.Format(
                "文件：{0}\r\n\r\n归属人：{1}\r\n归属编号：{2}\r\n写入时间：{3}\r\n原始 SHA-256：{4}\r\n标记版本：{5}",
                file,
                marker.Owner ?? string.Empty,
                marker.ProvenanceId ?? string.Empty,
                marker.MarkedAt ?? string.Empty,
                marker.OriginalSha256 ?? string.Empty,
                marker.Version ?? string.Empty);
            MessageBox.Show(this, message, "归属标记详情", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void UpdateUiState()
        {
            bool idle = !isProcessing && !isReading;
            bool hasRows = resultGrid.Rows.Count > 0;
            bool hasSelection = resultGrid.SelectedRows.Count > 0;

            ownerTextBox.Enabled = idle;
            addFilesButton.Enabled = idle;
            addFolderButton.Enabled = idle;
            readButton.Enabled = idle && hasRows;
            clearButton.Enabled = idle && hasRows;
            removeSelectedButton.Enabled = idle && hasSelection;
            startButton.Enabled = idle && hasRows;
            pauseButton.Enabled = isProcessing && !stopRequested;
            stopButton.Enabled = isProcessing && !stopRequested;
            dropArea.Enabled = idle;
            resultGrid.AllowDrop = idle;

            if (!isProcessing)
                pauseButton.Text = "暂停";

            UpdateSelectionLabel();
            UseWaitCursor = false;
        }

        private void ScrollToRow(int rowIndex)
        {
            try
            {
                if (rowIndex >= 0 && rowIndex < resultGrid.Rows.Count)
                    resultGrid.FirstDisplayedScrollingRowIndex = rowIndex;
            }
            catch { }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (isProcessing || isReading)
            {
                DialogResult result = MessageBox.Show(this,
                    isReading
                        ? "当前仍在读取 PDF。直接关闭程序会中断当前读取任务。\r\n\r\n确定要关闭吗？"
                        : "当前仍在处理 PDF。直接关闭程序可能中断当前任务。\r\n\r\n确定要关闭吗？",
                    "确认关闭", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (result != DialogResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }
            }

        }

        private static string ShortMessage(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "未知错误";
            string oneLine = value.Replace("\r", " ").Replace("\n", " ").Trim();
            return oneLine.Length <= 100 ? oneLine : oneLine.Substring(0, 100) + "…";
        }

        private sealed class WorkResult
        {
            public WorkResultKind Kind { get; private set; }
            public string Status { get; private set; }
            public MarkerInfo Marker { get; private set; }

            public static WorkResult Completed(string status, MarkerInfo marker)
            {
                return new WorkResult { Kind = WorkResultKind.Completed, Status = status, Marker = marker };
            }

            public static WorkResult Skipped(string status, MarkerInfo marker)
            {
                return new WorkResult { Kind = WorkResultKind.Skipped, Status = status, Marker = marker };
            }
        }

        private enum WorkResultKind
        {
            Completed,
            Skipped,
            Failed
        }
    }
}
