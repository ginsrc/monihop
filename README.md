<p align="center">
  <img src="assets/brand/monihop-mark.svg" width="96" alt="MoniHop logo">
</p>

# MoniHop（跃屏）

MoniHop 是一款面向 Windows 11 多显示器环境的开源窗口调度工具。它在本机运行，用于快速切换鼠标和窗口、按应用分配新窗口的目标显示器，以及通过拖拽投放层移动窗口。

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
![Platform](https://img.shields.io/badge/Platform-Windows%2011-0078D4)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)

## 功能

- 实时识别显示器连接、分辨率、刷新率、缩放、方向和物理尺寸。
- 记录连接过的显示器，并使用稳定设备标识恢复名称、规则和快捷键。
- 使用全局快捷键在显示器间切换鼠标或当前窗口。
- 为不同应用设置新窗口的目标显示器和窗口布局。
- 将窗口拖到顶部投放口，临时选择目标显示器和布局。
- 支持保持尺寸、最大化、左半屏和右半屏四种投放布局。
- 召回完全位于所有显示器工作区之外的普通窗口。
- 支持托盘运行、开机启动、浅色/深色主题和本地诊断。

主动投放功能默认关闭，只有用户启用后才会接管相应窗口行为。

## 系统要求

- Windows 11
- 从源码构建需要 [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

MoniHop 默认以当前用户的普通权限运行。Windows 不允许普通权限进程移动更高权限窗口；遇到这种情况时，MoniHop 会保留窗口原状态。

## 下载与安装

正式版本提供两种 `win-x64` 发行形式，均已包含 .NET 运行时：

- `MoniHop-<版本>-win-x64-setup.exe`：当前用户安装版，不需要管理员权限。
- `MoniHop-<版本>-win-x64-portable.zip`：便携版，解压到可写目录后运行 `MoniHop.exe`。

可使用发布页同时提供的 `SHA256SUMS.txt` 核对文件完整性：

```powershell
Get-FileHash .\MoniHop-1.0.3-win-x64-setup.exe -Algorithm SHA256
```

安装版可在“关于与诊断”中下载新版本，校验发布页提供的 SHA-256 后打开安装程序；便携版会打开对应发布页，由用户选择发行文件。

## 从源码运行

```powershell
git clone https://github.com/ginsrc/monihop.git
cd monihop
dotnet restore MoniHop.sln
dotnet build MoniHop.sln
dotnet run --project src/MoniHop.Desktop/MoniHop.Desktop.csproj
```

运行测试：

```powershell
dotnet test MoniHop.sln
```

生成安装版、便携版和 SHA-256 清单需要 Inno Setup 7：

```powershell
.\packaging\build-release.ps1
```

## 基本使用

1. 打开“显示器”页面，确认系统已识别当前显示器。
2. 在“快捷键”页面查看、修改或清除全局快捷键。
3. 如需拖拽投放，在“窗口投放”页面启用顶部投放层并设置默认目标与布局。
4. 如需自动分配新窗口，在“应用投放”页面启用功能并添加应用规则。
5. 通过“通用设置”配置关闭行为、托盘、开机启动、鼠标落点、主题和语言。

### 默认快捷键

| 操作 | 快捷键 |
| --- | --- |
| 鼠标切到下一屏 | `Ctrl + Alt + M` |
| 当前窗口移到上一屏 | `Ctrl + Alt + Shift + Left` |
| 当前窗口移到下一屏 | `Ctrl + Alt + Shift + Right` |

其他动作默认不分配快捷键，不会占用系统按键组合。

## 行为边界

- 应用投放只处理可识别的普通顶层窗口，不独立处理菜单、工具窗口、截图遮罩或子窗口。
- Windows 没有供第三方工具在任意窗口创建前指定显示器的通用接口。MoniHop 会在窗口可识别后尽快移动，因此个别应用可能短暂出现在原显示器。
- 固定尺寸窗口不会被强制最大化或拉伸；不支持的布局会自动降级为“保持尺寸”。
- 目标显示器断开时会保留原规则，并在设备重新连接后自动匹配。
- 顶部投放层是 MoniHop 的独立界面，不嵌入或修改 Windows Snap Layout。

## 数据与隐私

MoniHop 不依赖云服务，不读取窗口内容、键盘输入、剪贴板或用户文件，也不会自动上传配置或诊断数据。

| 运行方式 | 配置与显示器记录 | 本地诊断 |
| --- | --- | --- |
| 安装版 | `%LOCALAPPDATA%\MoniHop\config` | `%LOCALAPPDATA%\MoniHop\diagnostics` |
| 便携版 | `<程序目录>\data\config` | `<程序目录>\data\diagnostics` |

关键失败类别只保存在本机；详细诊断需要用户主动开启。单个日志文件上限为 1 MB。损坏的 JSON 配置会先保留为带时间戳的 `.corrupt-...json` 文件，再恢复默认设置。

## 项目结构

```text
src/MoniHop.Core/            平台无关的规则与模型
src/MoniHop.Windows/         Windows 与 Win32 适配
src/MoniHop.Desktop/         WPF 桌面应用
tests/                       自动化测试
packaging/                   安装与便携发行脚本
docs/public/                 公开文档
```

## 参与贡献

欢迎提交 Issue 和 Pull Request。提交问题时，请提供 Windows 版本、显示器连接方式、复现步骤和实际表现；请勿附带包含个人路径、窗口标题或其他敏感信息的诊断内容。

- [问题反馈](https://github.com/ginsrc/monihop/issues)
- [版本发布](https://github.com/ginsrc/monihop/releases)

## 许可证

MoniHop 使用 [MIT License](LICENSE)，Copyright (c) 2026 ginsrc。
