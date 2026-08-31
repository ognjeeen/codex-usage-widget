# Codex Usage Widget

[English](README.md) | 简体中文

无需打开浏览器，即可查看账户可用的 Codex 用量限制，包括 5 小时和每周周期。
Codex Usage Widget 是一款在 Windows 本地运行的小组件，通过官方 Codex CLI app server
读取用量信息。

> 这是一个独立工具，并非 OpenAI 官方应用。

![Codex Usage Widget 显示 5 小时和每周用量限制](docs/images/desktop-widget-limits.png)

## 快速开始

本小组件需要 Windows 10 1809 或更高版本，Codex CLI 需要位于 `PATH` 中，并且已在本机完成登录。

1. 下载[最新的 Windows x64 便携版](https://github.com/ognjeeen/codex-usage-widget/releases/latest/download/codex-usage-widget-win-x64.zip)。
2. 解压后启动 `CodexUsageWidget.exe`，也可以直接从 ZIP 中启动。
3. 如果没有显示用量，请确认 PowerShell 中可以运行 `codex --version`，然后执行 `codex login`。

便携版已经包含 .NET 运行时。当前可执行文件尚未进行代码签名，因此 Windows 可能会显示未知发布者警告。
每个 GitHub Release 都附有 SHA-256 校验和，可用于验证文件。

如果 Windows 从 ZIP 临时目录启动可执行文件，小组件会将当前版本复制到
`%LOCALAPPDATA%\CodexUsageWidget\app\<version>`，然后重新启动。从解压后的文件夹启动时，
程序会继续在原位置运行。同一时间只会运行一个小组件实例。

如果 Codex 没有安装在 `PATH` 中，请将环境变量 `CODEX_USAGE_WIDGET_CODEX_PATH` 设置为
`codex.cmd` 或 `codex.exe` 的完整路径。

## 功能

- 显示各个通用 Codex 用量周期的剩余百分比和重置时间
- 主窗口、任务栏标签和托盘图标共用一个可选择的用量限制
- 可在设置或快捷展开按钮中切换紧凑和详情布局；详情布局会显示额度、消费限制、可用重置次数、
  Token 活动以及 Codex 返回的模型专属限制
- 可在设置中选择跟随系统、浅色或深色主题，并提供五种预设强调色
- 自动检测 Windows 显示语言，不支持的语言回退到英语；也可在设置中固定使用英语或简体中文
- 可移动、始终置顶的桌面小组件，以及位于 Windows 通知区域旁的紧凑任务栏标签
- 根据官方 Codex 本地生命周期钩子显示实时运行状态点
- 每两分钟自动刷新，并接收实时用量限制通知
- 支持全屏检测、多显示器 DPI，并可选择开机启动
- 日志仅保存在本地，不包含遥测，也不使用远程后端

## 显示模式与用量限制选择

**桌面小组件。** 顶部显示所选用量限制的摘要，其余通用限制列在下方。紧凑模式重点显示用量限制；
详情模式还会显示账户和 Token 活动信息。

**任务栏标签。** 在 Windows 通知区域旁显示同一用量限制的剩余百分比。托盘图标及其提示信息也会
跟随该选择。

在**设置**的**用量**区域中选择 `5 小时限制`、`每周限制` 或 `剩余额度最少`。默认显示 5 小时周期。
不同 Codex 账户返回的用量周期可能不同；如果所选周期不可用，小组件会自动使用一个可用周期。

![Codex Usage Widget 详情模式预览](docs/images/detailed-widget.png)

Token 活动仅供参考。Token 数量与订阅用量的剩余百分比并不直接对应。

![Codex Usage Widget 任务栏标签预览](docs/images/taskbar-label.png)

点击 `−` 按钮可将小组件移至任务栏。右键单击任务栏标签或托盘图标，可以刷新、切换显示模式、
打开设置、检查更新或退出。也可以通过小组件上的齿轮按钮打开设置。语言、主题、强调色、小组件布局、
显示的用量限制和开机启动设置会在选择后立即生效。

## 运行状态点

运行状态点用于显示本机是否至少有一个 Codex 任务正在运行。状态更新来自官方 Codex 生命周期钩子，
并通过仅限当前 Windows 用户的命名管道传递。小组件不会读取提示词、回复、对话记录路径或模型输出。

启用方法：

1. 打开**设置**，在**功能**区域中找到 **Codex 运行状态**。
2. 选择**安装钩子**，检查即将写入 `~/.codex/hooks.json` 的确切改动。
3. 选择**复制 /hooks 并打开 Codex**，粘贴 `/hooks`，然后信任这三个定义。
4. 返回小组件并选择**重新检查**。

钩子只会在用户明确确认后安装，小组件不会在正常启动时自动安装。有关隐私、命令行设置、移除和恢复行为，
请参阅[运行状态点](docs/ACTIVITY_DOTS.md)。

## 隐私与本地数据

小组件只与本机安装的 Codex CLI 通信。它不会抓取浏览器内容、读取身份验证密钥、发送遥测数据或使用
远程后端。身份验证始终由 Codex 管理。

应用只会在 `%LOCALAPPDATA%\CodexUsageWidget` 中写入以下内容：

- `app\<version>\CodexUsageWidget.exe`：直接从 ZIP 启动时使用的稳定副本
- `display-mode.txt`：桌面小组件或任务栏标签的显示偏好
- `widget-density.txt`：紧凑或详情布局偏好
- `displayed-limit.txt`：摘要区域显示的用量限制
- `theme.txt`：跟随系统、浅色或深色主题偏好
- `accent-palette.txt`：所选的预设强调色
- `language.txt`：跟随系统、英语或简体中文语言偏好
- `logs\codex-usage-widget-YYYYMMDD.log`：诊断日志，保留 14 天

小组件显示 ChatGPT 和 Codex 订阅用量限制，不显示 OpenAI API 账单或 API Key 用量。

## 卸载

1. 如果安装了运行状态钩子，请打开**设置**，在**功能**区域找到 **Codex 运行状态**，然后选择**移除钩子**。
2. 在**设置**的**常规**区域关闭**开机启动**。
3. 退出小组件。
4. 删除解压后的应用文件夹和 `%LOCALAPPDATA%\CodexUsageWidget`。本地数据目录包含稳定副本、
   已保存的偏好和诊断日志。

## 开发

仓库通过 `global.json` 固定 .NET SDK 版本。

```powershell
dotnet restore .\CodexUsageWidget.slnx
dotnet test .\CodexUsageWidget.slnx -c Release
dotnet run --project .\src\CodexUsageWidget\CodexUsageWidget.csproj
```

如需在不读取 Codex 用量的情况下预览两个通用用量周期，请先关闭正在运行的小组件实例，然后启动本地预览版本：

```powershell
dotnet run --project .\src\CodexUsageWidget\CodexUsageWidget.csproj -p:EnableUsagePreview=true -- --preview-usage
```

预览数据中，5 小时限制的剩余用量为 80%，每周限制为 15%。标准 Release 构建不接受预览参数。

每次构建都会将警告视为错误，并运行推荐的 .NET 分析器。

## 构建便携版

```powershell
.\scripts\publish.ps1 -Runtime win-x64
```

脚本会运行完整测试套件，并创建 `artifacts/release/codex-usage-widget-win-x64.zip`。
通过 `-Runtime` 参数也可以构建 `win-arm64` 版本。完整的维护者发布流程请参阅[发布说明](docs/RELEASING.md)。

## 架构

模块职责、运行流程和扩展说明请参阅[架构文档](docs/ARCHITECTURE.md)。

## 许可证

本项目采用 [MIT License](LICENSE)。在保留版权声明和许可证文本的前提下，你可以使用、修改、Fork、
发布、再分发、再授权或销售本软件的副本。
