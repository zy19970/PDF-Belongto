# PDF-Belongto

一个 Windows WinForms PDF 归属标记工具，用于在**不改变 PDF 页面可见内容和文件名**的前提下，为 PDF 写入可读取的 XMP 归属信息。

## 功能

- 支持添加单个 PDF、多个 PDF，以及整个文件夹。
- 文件夹会递归扫描子目录中的 PDF。
- 支持把 PDF 文件或文件夹直接拖入程序。
- 支持**独立读取**归属标记，不需要先执行写入。
- 支持批量写入归属标记，并直接覆盖原文件，文件名保持不变。
- 支持“开始 / 暂停 / 继续 / 停止”。
- 支持从任务列表中移出选中项，或清空整个列表；不会删除磁盘上的原 PDF。
- 支持中文、日文等 Unicode 路径。
- 检测到 PDF 数字签名时会跳过，避免破坏已有签名。
- 写入后自动读取校验。
- 首次需要 ExifTool 时，会显示下载、解压和验证进度。
- ExifTool 安装在程序运行根目录，不写入待处理 PDF 所在目录。
- 默认归属人为 `Joey`，可在界面中随时修改。
- 不使用 `settings.ini`，不会额外保存归属人配置。
- 已配置应用程序图标。

## 写入内容

程序通过自定义 XMP namespace 写入以下字段：

| 字段 | 含义 |
| --- | --- |
| `Owner` | 归属人 |
| `ProvenanceID` | 根据归属人和原文件 SHA-256 生成的归属编号 |
| `MarkedAt` | 写入时间 |
| `OriginalSHA256` | 写入前 PDF 的 SHA-256 |
| `MarkerVersion` | 标记格式版本 |

这些字段不会作为可见水印显示在 PDF 页面中，正常浏览和打印时不会出现在正文里。

> 注意：XMP 元数据可以被专门的工具删除或重写，因此它适合作为文件归属暗记和整理记录，不应被视为不可篡改的数字签名。

## 使用方法

1. 启动程序。
2. 通过“添加文件”“添加文件夹”或拖拽，把 PDF 加入列表。
3. 如需检查已有标记，选中条目后点击“读取标记”；未选中条目时会读取列表中的全部 PDF。
4. 如需写入，确认顶部“归属人”，然后点击“开始写入”。
5. 处理中可以点击“暂停/继续”或“停止”。
6. “移出列表”和键盘 `Delete` 只会把条目从当前任务列表移除，不会删除磁盘文件。

程序会直接覆盖原 PDF。建议先自行备份重要文件。

## ExifTool

项目使用 [ExifTool](https://exiftool.org/) 读写 PDF XMP 信息。

当前代码使用 ExifTool 13.59。首次需要时，程序从 ExifTool 官方地址下载 64 位压缩包，并把以下组件放到程序运行目录：

```text
PDF归属标记器.exe
exiftool.exe
exiftool_files\
exiftool.config
```

为避免 Windows 命令行代码页导致中文路径损坏，程序使用 UTF-8 参数文件配合 ExifTool 的 `-@` 方式传递文件路径和归属信息。

## 编译环境

- Visual Studio 2022
- “使用 .NET 的桌面开发”工作负载
- .NET Framework 4.8 Targeting Pack
- WinForms

打开：

```text
PDF归属标记器.sln
```

然后在 Visual Studio 中执行“生成解决方案”即可。

## 项目结构

```text
PDF归属标记器.sln
PDF归属标记器/
├─ MainForm.cs
├─ MainForm.Designer.cs
├─ ExifToolDownloadForm.cs
├─ ExifToolDownloadForm.Designer.cs
├─ Services/
│  ├─ ExifToolService.cs
│  └─ PdfMarkerService.cs
├─ Models/
│  ├─ MarkerInfo.cs
│  └─ InstallProgress.cs
├─ Properties/
│  └─ AssemblyInfo.cs
├─ Program.cs
├─ App.config
├─ app.ico
├─ app_icon.png
└─ PDF归属标记器.csproj
```

## 当前处理原则

- 不在 PDF 页面添加可见文字、水印或图形。
- 不修改文件名。
- 不自动生成旁车配置文件保存归属人。
- 不覆盖其他归属人的已有标记。
- 不处理检测到数字签名的 PDF。
- 暂停和停止均在当前 PDF 处理完成后生效，以降低中途覆盖导致文件损坏的风险。

## License

当前仓库暂未指定开源许可证。