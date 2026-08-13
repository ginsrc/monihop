# MoniHop（跃屏）

MoniHop 是面向 Windows 11 多显示器环境的本地窗口调度工具。

## 当前状态

当前仓库处于 `0.1.0-alpha1`。正式 WPF 设置界面已迁移六页信息架构，已接入本机显示器枚举、鼠标快捷切屏和当前窗口上一/下一屏切换；窗口顶部投放层、应用长期路由、显示器历史/命名和托盘运行态仍在后续功能切片中。

正式界面不会把未实现能力伪装成可用功能：尚未接入的设置会显示“待开发”并禁用操作。原型只保留在内部文档中作为历史设计证据，不参与正式构建。

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
