using PdfOwnershipMarker.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PdfOwnershipMarker.Services
{
    internal sealed class ExifToolService
    {
        private const string ExifToolVersion = "13.59";
        private const string ExifToolUrl = "https://exiftool.org/exiftool-13.59_64.zip";
        private const string ConfigFileName = "exiftool.config";

        private readonly string appRoot;
        private readonly string exifExe;
        private readonly string configPath;

        public string ExifExePath { get { return exifExe; } }
        public string ConfigPath { get { return configPath; } }

        public ExifToolService()
        {
            appRoot = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            exifExe = Path.Combine(appRoot, "exiftool.exe");
            configPath = Path.Combine(appRoot, ConfigFileName);
            EnsureConfigFile();
        }

        public bool EnsureInstalled(IWin32Window owner)
        {
            if (File.Exists(exifExe)) return true;

            using (ExifToolDownloadForm form = new ExifToolDownloadForm(this))
            {
                DialogResult result = form.ShowDialog(owner);
                if (result == DialogResult.OK && File.Exists(exifExe)) return true;

                if (result == DialogResult.Abort)
                {
                    MessageBox.Show(owner,
                        "ExifTool 初始化失败。\r\n\r\n" +
                        "你也可以手工把 exiftool.exe 和 exiftool_files 文件夹放到软件运行根目录：\r\n" + appRoot,
                        "PDF归属标记器", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return false;
            }
        }

        public async Task DownloadAndInstallAsync(IProgress<InstallProgress> progress, CancellationToken token)
        {
            if (File.Exists(exifExe)) return;

            string zip = Path.Combine(Path.GetTempPath(), "exiftool_" + Guid.NewGuid().ToString("N") + ".zip");
            string extract = Path.Combine(Path.GetTempPath(), "exiftool_" + Guid.NewGuid().ToString("N"));

            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                if (progress != null)
                    progress.Report(new InstallProgress { Stage = "正在连接 ExifTool 官方下载地址…", IsIndeterminate = true });

                using (WebClient wc = new WebClient())
                using (token.Register(delegate { try { wc.CancelAsync(); } catch { } }))
                {
                    wc.Headers.Add("User-Agent", "PDF-Ownership-Marker/1.0");
                    wc.DownloadProgressChanged += delegate(object sender, DownloadProgressChangedEventArgs e)
                    {
                        if (progress == null) return;
                        progress.Report(new InstallProgress
                        {
                            Stage = "正在下载 ExifTool…",
                            Percentage = e.ProgressPercentage,
                            BytesReceived = e.BytesReceived,
                            TotalBytes = e.TotalBytesToReceive,
                            IsIndeterminate = e.TotalBytesToReceive <= 0
                        });
                    };

                    try
                    {
                        await wc.DownloadFileTaskAsync(new Uri(ExifToolUrl), zip);
                    }
                    catch (WebException ex)
                    {
                        if (token.IsCancellationRequested || ex.Status == WebExceptionStatus.RequestCanceled)
                            throw new OperationCanceledException(token);
                        throw;
                    }
                }

                token.ThrowIfCancellationRequested();
                if (progress != null)
                    progress.Report(new InstallProgress { Stage = "下载完成，正在解压…", Percentage = 100, IsIndeterminate = true });

                await Task.Run(delegate
                {
                    token.ThrowIfCancellationRequested();
                    Directory.CreateDirectory(extract);
                    ZipFile.ExtractToDirectory(zip, extract);

                    token.ThrowIfCancellationRequested();
                    string exe = Directory.GetFiles(extract, "*.exe", SearchOption.AllDirectories)
                        .FirstOrDefault(x => Path.GetFileName(x).StartsWith("exiftool", StringComparison.OrdinalIgnoreCase));
                    if (exe == null) throw new InvalidOperationException("下载包中未找到 ExifTool 可执行文件。");

                    string filesFolder = Path.Combine(Path.GetDirectoryName(exe), "exiftool_files");
                    if (File.Exists(exifExe)) File.Delete(exifExe);
                    File.Copy(exe, exifExe, true);

                    if (Directory.Exists(filesFolder))
                    {
                        string targetFolder = Path.Combine(appRoot, "exiftool_files");
                        if (Directory.Exists(targetFolder)) Directory.Delete(targetFolder, true);
                        CopyDirectory(filesFolder, targetFolder);
                    }
                }, token);

                if (progress != null)
                    progress.Report(new InstallProgress { Stage = "正在验证组件…", Percentage = 100, IsIndeterminate = true });

                token.ThrowIfCancellationRequested();
                if (!File.Exists(exifExe)) throw new InvalidOperationException("ExifTool 安装后未找到 exiftool.exe。");
            }
            finally
            {
                try { if (File.Exists(zip)) File.Delete(zip); } catch { }
                try { if (Directory.Exists(extract)) Directory.Delete(extract, true); } catch { }
            }
        }

        /// <summary>
        /// 通过 UTF-8 参数文件调用 ExifTool。
        /// Windows 命令行会按当前代码页重编码 Unicode 参数，中文/日文等路径可能因此损坏。
        /// ExifTool 官方推荐对 Unicode 文件名使用 UTF-8 编码的 -@ 参数文件，并指定
        /// -charset filename=utf8。这里把所有可变参数（文件路径、归属人等）都放进参数文件，
        /// 命令行本身只保留 ASCII 参数。
        /// </summary>
        public string Run(params string[] args)
        {
            if (!File.Exists(exifExe))
                throw new FileNotFoundException("ExifTool 不可用。", exifExe);

            string argFileName = "et_args_" + Guid.NewGuid().ToString("N") + ".txt";
            string argFilePath = Path.Combine(appRoot, argFileName);

            try
            {
                using (StreamWriter writer = new StreamWriter(argFilePath, false, new UTF8Encoding(false)))
                {
                    foreach (string arg in args ?? new string[0])
                    {
                        string value = arg ?? string.Empty;
                        if (value.IndexOf('\r') >= 0 || value.IndexOf('\n') >= 0)
                            throw new ArgumentException("ExifTool 参数不能包含换行符。", "args");

                        // ExifTool 的 -@ 参数文件是一行一个参数；不要添加 shell 引号。
                        writer.WriteLine(value);
                    }
                }

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = exifExe,
                    WorkingDirectory = appRoot,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8,
                    // -config 不能放进 -@ 参数文件，并且必须使用系统字符集；
                    // 因此使用工作目录中的 ASCII 相对文件名。
                    // 参数文件名同样只使用 ASCII，避免 Windows 命令行字符集问题。
                    Arguments = "-config " + ConfigFileName +
                                " -charset exiftool=utf8" +
                                " -charset filename=utf8" +
                                " -@ " + argFileName
                };

                using (Process p = new Process())
                {
                    p.StartInfo = psi;
                    p.Start();
                    string stdout = p.StandardOutput.ReadToEnd();
                    string stderr = p.StandardError.ReadToEnd();
                    p.WaitForExit();
                    if (p.ExitCode != 0)
                        throw new InvalidOperationException(string.IsNullOrWhiteSpace(stderr) ? stdout.Trim() : stderr.Trim());
                    return stdout;
                }
            }
            finally
            {
                try { if (File.Exists(argFilePath)) File.Delete(argFilePath); } catch { }
            }
        }

        private void EnsureConfigFile()
        {
            string config = @"%Image::ExifTool::UserDefined = (
    'Image::ExifTool::XMP::Main' => {
        zyprov => {
            SubDirectory => { TagTable => 'Image::ExifTool::UserDefined::zyprov' },
        },
    },
);
%Image::ExifTool::UserDefined::zyprov = (
    GROUPS    => { 0 => 'XMP', 1 => 'XMP-zyprov', 2 => 'Document' },
    NAMESPACE => { 'zyprov' => 'https://local.pdf-provenance/zy/1.0/' },
    WRITABLE  => 'string',
    Owner          => { Groups => { 2 => 'Author' } },
    ProvenanceID   => { },
    MarkedAt       => { Groups => { 2 => 'Time' } },
    OriginalSHA256 => { },
    MarkerVersion  => { },
);
1;
";
            if (!File.Exists(configPath) || File.ReadAllText(configPath, Encoding.UTF8) != config)
                File.WriteAllText(configPath, config, new UTF8Encoding(false));
        }

        private static void CopyDirectory(string source, string target)
        {
            Directory.CreateDirectory(target);
            foreach (string file in Directory.GetFiles(source))
                File.Copy(file, Path.Combine(target, Path.GetFileName(file)), true);
            foreach (string dir in Directory.GetDirectories(source))
                CopyDirectory(dir, Path.Combine(target, Path.GetFileName(dir)));
        }
    }
}
