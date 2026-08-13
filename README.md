# MoniHop（跃屏）

MoniHop 是面向 Windows 11 多显示器环境的本地窗口调度工具。

## 当前状态

当前仓库处于 `0.1.0-alpha1`。正式 WPF 设置界面已迁移六页信息架构，已接入本机显示器枚举、真实显示参数、插拔与显示设置变化自动刷新、显示器历史记录和自定义名称，以及鼠标快捷切屏、当前窗口上一/下一屏切换和新窗口自动投放；窗口顶部投放层和托盘运行态仍在后续功能切片中。

正式界面不会把未实现能力伪装成可用功能：尚未接入的设置会显示“待开发”并禁用操作。原型只保留在内部文档中作为历史设计证据，不参与正式构建。

应用投放默认关闭。开启后可跟随 Windows 主显示器或使用固定显示器，并从 Windows 已安装应用列表或运行中的应用创建保持尺寸、最大化、左半屏或右半屏规则。MoniHop 只处理具备正常标题栏的普通顶层应用窗口，菜单、截图遮罩、工具窗口和子窗口不会独立投放；目标显示器缺失时本次回退到 Windows 主显示器，原配置保持不变。Windows 不提供外部工具在第三方窗口创建前指定目标显示器的通用接口，MoniHop 会在窗口首次可识别时立即移动，以尽量减少先出现在原屏幕上的时间。

默认快捷键：

- 鼠标切到下一屏：`Ctrl + Alt + M`
- 当前窗口移到上一屏：`Ctrl + Alt + Shift + Left`
- 当前窗口移到下一屏：`Ctrl + Alt + Shift + Right`

## 环境

- Windows 11
- .NET SDK 10.0.302 或兼容的 10.0 最新补丁版本

## 构建

```powershell
& 'C:\Program Files\dotnet\dotnet.exe' build MoniHop.sln
```

## 测试

```powershell
& 'C:\Program Files\dotnet\dotnet.exe' test MoniHop.sln
```

## 运行

```powershell
& 'C:\Program Files\dotnet\dotnet.exe' run --project src/MoniHop.Desktop/MoniHop.Desktop.csproj
```

## 文档

公开文档位于 [`docs/public/`](docs/public/README.md)。

## 隐私边界

MoniHop 采用本地优先设计。第一版不依赖云服务，不读取窗口内容、键盘输入、剪贴板或用户文件。

应用投放设置保存在 `%LOCALAPPDATA%\MoniHop\config\application-projection.json`。普通权限无法移动更高权限窗口时，MoniHop 保留窗口原状态并显示失败提示，不要求管理员常驻。
