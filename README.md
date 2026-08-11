# MoniHop（跃屏）

MoniHop 是面向 Windows 11 多显示器环境的本地窗口调度工具。

## 当前状态

当前仓库处于早期开发阶段。已完成本机显示器枚举、鼠标快捷切屏和当前窗口上一/下一屏纵向切片；窗口投放、应用路由和顶部投放层尚未实现。

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
