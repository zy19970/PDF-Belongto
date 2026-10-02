using System.Drawing;
using System.Windows.Forms;

namespace PdfOwnershipMarker
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private Panel topPanel;
        private Label ownerLabel;
        private TextBox ownerTextBox;
        private Button addFilesButton;
        private Button addFolderButton;
        private Label dropArea;
        private Panel actionPanel;
        private Button readButton;
        private Button startButton;
        private Button pauseButton;
        private Button stopButton;
        private Button removeSelectedButton;
        private Button clearButton;
        private Label selectionLabel;
        private ProgressBar progressBar;
        private Label statusLabel;
        private DataGridView resultGrid;
        private Label footerLabel;
        private ContextMenuStrip rowContextMenu;
        private ToolStripMenuItem contextReadItem;
        private ToolStripMenuItem contextRemoveItem;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.topPanel = new Panel();
            this.ownerLabel = new Label();
            this.ownerTextBox = new TextBox();
            this.addFilesButton = new Button();
            this.addFolderButton = new Button();
            this.dropArea = new Label();
            this.actionPanel = new Panel();
            this.readButton = new Button();
            this.startButton = new Button();
            this.pauseButton = new Button();
            this.stopButton = new Button();
            this.removeSelectedButton = new Button();
            this.clearButton = new Button();
            this.selectionLabel = new Label();
            this.progressBar = new ProgressBar();
            this.statusLabel = new Label();
            this.resultGrid = new DataGridView();
            this.footerLabel = new Label();
            this.rowContextMenu = new ContextMenuStrip(this.components);
            this.contextReadItem = new ToolStripMenuItem();
            this.contextRemoveItem = new ToolStripMenuItem();
            this.topPanel.SuspendLayout();
            this.actionPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.resultGrid)).BeginInit();
            this.rowContextMenu.SuspendLayout();
            this.SuspendLayout();

            // topPanel
            this.topPanel.Dock = DockStyle.Top;
            this.topPanel.Height = 60;
            this.topPanel.Padding = new Padding(14, 10, 14, 8);
            this.topPanel.BackColor = Color.White;

            // ownerLabel
            this.ownerLabel.AutoSize = true;
            this.ownerLabel.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            this.ownerLabel.Location = new Point(16, 21);
            this.ownerLabel.Text = "归属人";

            // ownerTextBox
            this.ownerTextBox.Location = new Point(72, 17);
            this.ownerTextBox.Size = new Size(250, 23);
            this.ownerTextBox.TabIndex = 0;

            // addFilesButton
            this.addFilesButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.addFilesButton.Location = new Point(742, 14);
            this.addFilesButton.Size = new Size(92, 32);
            this.addFilesButton.Text = "添加文件";
            this.addFilesButton.UseVisualStyleBackColor = true;
            this.addFilesButton.Click += new System.EventHandler(this.AddFilesButton_Click);

            // addFolderButton
            this.addFolderButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.addFolderButton.Location = new Point(842, 14);
            this.addFolderButton.Size = new Size(100, 32);
            this.addFolderButton.Text = "添加文件夹";
            this.addFolderButton.UseVisualStyleBackColor = true;
            this.addFolderButton.Click += new System.EventHandler(this.AddFolderButton_Click);

            this.topPanel.Controls.Add(this.ownerLabel);
            this.topPanel.Controls.Add(this.ownerTextBox);
            this.topPanel.Controls.Add(this.addFilesButton);
            this.topPanel.Controls.Add(this.addFolderButton);

            // dropArea
            this.dropArea.AllowDrop = true;
            this.dropArea.BackColor = Color.FromArgb(248, 249, 250);
            this.dropArea.BorderStyle = BorderStyle.FixedSingle;
            this.dropArea.Dock = DockStyle.Top;
            this.dropArea.Font = new Font("Microsoft YaHei UI", 10.5F);
            this.dropArea.Height = 82;
            this.dropArea.Text = "把 PDF 文件或文件夹拖到这里\r\n加入列表后，可直接点“读取标记”查看；需要写入时再点“开始写入”";
            this.dropArea.TextAlign = ContentAlignment.MiddleCenter;
            this.dropArea.DragEnter += new DragEventHandler(this.DragEnter_Handler);
            this.dropArea.DragDrop += new DragEventHandler(this.DragDrop_Handler);

            // actionPanel
            this.actionPanel.Dock = DockStyle.Top;
            this.actionPanel.Height = 56;
            this.actionPanel.Padding = new Padding(12, 10, 12, 8);
            this.actionPanel.BackColor = Color.White;

            // readButton
            this.readButton.Location = new Point(14, 10);
            this.readButton.Size = new Size(98, 34);
            this.readButton.Text = "读取标记";
            this.readButton.UseVisualStyleBackColor = true;
            this.readButton.Click += new System.EventHandler(this.ReadButton_Click);

            // startButton
            this.startButton.Location = new Point(120, 10);
            this.startButton.Size = new Size(98, 34);
            this.startButton.Text = "开始写入";
            this.startButton.UseVisualStyleBackColor = true;
            this.startButton.Click += new System.EventHandler(this.StartButton_Click);

            // pauseButton
            this.pauseButton.Location = new Point(226, 10);
            this.pauseButton.Size = new Size(82, 34);
            this.pauseButton.Text = "暂停";
            this.pauseButton.UseVisualStyleBackColor = true;
            this.pauseButton.Click += new System.EventHandler(this.PauseButton_Click);

            // stopButton
            this.stopButton.Location = new Point(316, 10);
            this.stopButton.Size = new Size(82, 34);
            this.stopButton.Text = "停止";
            this.stopButton.UseVisualStyleBackColor = true;
            this.stopButton.Click += new System.EventHandler(this.StopButton_Click);

            // selectionLabel
            this.selectionLabel.AutoSize = false;
            this.selectionLabel.Location = new Point(410, 10);
            this.selectionLabel.Size = new Size(240, 34);
            this.selectionLabel.Text = "共 0 个，已选 0 个";
            this.selectionLabel.TextAlign = ContentAlignment.MiddleLeft;
            this.selectionLabel.ForeColor = Color.DimGray;

            // removeSelectedButton
            this.removeSelectedButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.removeSelectedButton.Location = new Point(742, 10);
            this.removeSelectedButton.Size = new Size(92, 34);
            this.removeSelectedButton.Text = "移出列表";
            this.removeSelectedButton.UseVisualStyleBackColor = true;
            this.removeSelectedButton.Click += new System.EventHandler(this.RemoveSelectedButton_Click);

            // clearButton
            this.clearButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.clearButton.Location = new Point(842, 10);
            this.clearButton.Size = new Size(100, 34);
            this.clearButton.Text = "清空列表";
            this.clearButton.UseVisualStyleBackColor = true;
            this.clearButton.Click += new System.EventHandler(this.ClearButton_Click);

            this.actionPanel.Controls.Add(this.readButton);
            this.actionPanel.Controls.Add(this.startButton);
            this.actionPanel.Controls.Add(this.pauseButton);
            this.actionPanel.Controls.Add(this.stopButton);
            this.actionPanel.Controls.Add(this.selectionLabel);
            this.actionPanel.Controls.Add(this.removeSelectedButton);
            this.actionPanel.Controls.Add(this.clearButton);

            // progressBar
            this.progressBar.Dock = DockStyle.Top;
            this.progressBar.Height = 18;

            // statusLabel
            this.statusLabel.Dock = DockStyle.Top;
            this.statusLabel.Height = 36;
            this.statusLabel.Padding = new Padding(10, 0, 0, 0);
            this.statusLabel.Text = "等待添加 PDF 文件或文件夹…";
            this.statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            this.statusLabel.BackColor = Color.White;

            // resultGrid
            this.resultGrid.AllowDrop = true;
            this.resultGrid.AllowUserToAddRows = false;
            this.resultGrid.AllowUserToDeleteRows = false;
            this.resultGrid.AllowUserToResizeRows = false;
            this.resultGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            this.resultGrid.BackgroundColor = Color.White;
            this.resultGrid.BorderStyle = BorderStyle.Fixed3D;
            this.resultGrid.ContextMenuStrip = this.rowContextMenu;
            this.resultGrid.Dock = DockStyle.Fill;
            this.resultGrid.ReadOnly = true;
            this.resultGrid.RowHeadersVisible = false;
            this.resultGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            this.resultGrid.MultiSelect = true;
            this.resultGrid.Columns.Add("File", "PDF 文件");
            this.resultGrid.Columns.Add("Result", "状态");
            this.resultGrid.Columns.Add("Owner", "归属人");
            this.resultGrid.Columns.Add("Provenance", "归属编号");
            this.resultGrid.Columns.Add("MarkedAt", "写入时间");
            this.resultGrid.Columns[0].FillWeight = 50F;
            this.resultGrid.Columns[1].FillWeight = 19F;
            this.resultGrid.Columns[2].FillWeight = 10F;
            this.resultGrid.Columns[3].FillWeight = 14F;
            this.resultGrid.Columns[4].FillWeight = 15F;
            this.resultGrid.CellDoubleClick += new DataGridViewCellEventHandler(this.ResultGrid_CellDoubleClick);
            this.resultGrid.CellMouseDown += new DataGridViewCellMouseEventHandler(this.ResultGrid_CellMouseDown);
            this.resultGrid.KeyDown += new KeyEventHandler(this.ResultGrid_KeyDown);
            this.resultGrid.SelectionChanged += new System.EventHandler(this.ResultGrid_SelectionChanged);
            this.resultGrid.DragEnter += new DragEventHandler(this.DragEnter_Handler);
            this.resultGrid.DragDrop += new DragEventHandler(this.DragDrop_Handler);

            // context menu
            this.contextReadItem.Text = "读取标记";
            this.contextReadItem.Click += new System.EventHandler(this.ContextReadItem_Click);
            this.contextRemoveItem.Text = "移出列表";
            this.contextRemoveItem.Click += new System.EventHandler(this.ContextRemoveItem_Click);
            this.rowContextMenu.Items.AddRange(new ToolStripItem[] { this.contextReadItem, this.contextRemoveItem });
            this.rowContextMenu.Opening += new System.ComponentModel.CancelEventHandler(this.RowContextMenu_Opening);

            // footerLabel
            this.footerLabel.Dock = DockStyle.Bottom;
            this.footerLabel.ForeColor = Color.DimGray;
            this.footerLabel.Height = 38;
            this.footerLabel.Padding = new Padding(10, 0, 0, 0);
            this.footerLabel.Text = "读取与写入互相独立：选中条目后点“读取标记”只读取，不修改 PDF；未选中时读取全部。Delete 键或“移出列表”只从列表删除，不删除磁盘文件。";
            this.footerLabel.TextAlign = ContentAlignment.MiddleLeft;
            this.footerLabel.BackColor = Color.White;

            // MainForm
            this.AllowDrop = true;
            this.AutoScaleDimensions = new SizeF(7F, 17F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(970, 650);
            this.Controls.Add(this.resultGrid);
            this.Controls.Add(this.statusLabel);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.actionPanel);
            this.Controls.Add(this.dropArea);
            this.Controls.Add(this.topPanel);
            this.Controls.Add(this.footerLabel);
            this.Font = new Font("Microsoft YaHei UI", 9F);
            this.MinimumSize = new Size(860, 560);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "PDF归属标记器";
            this.FormClosing += new FormClosingEventHandler(this.MainForm_FormClosing);
            this.DragEnter += new DragEventHandler(this.DragEnter_Handler);
            this.DragDrop += new DragEventHandler(this.DragDrop_Handler);

            this.topPanel.ResumeLayout(false);
            this.topPanel.PerformLayout();
            this.actionPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.resultGrid)).EndInit();
            this.rowContextMenu.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
