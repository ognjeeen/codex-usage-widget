# Codex Usage Widget

English | [简体中文](README.zh-CN.md)

See the Codex limits available to your account, including 5-hour and weekly windows,
without opening a browser. Codex Usage Widget runs locally on Windows and reads usage
through the official Codex CLI app server.

> This is an independent utility and is not an official OpenAI application.

![Codex Usage Widget showing 5-hour and weekly limits](docs/images/desktop-widget-limits.png)

## Quick start

The widget requires Windows 10 version 1809 or newer, the Codex CLI on `PATH`, and a
completed local sign-in.

1. Download the
   [latest Windows x64 portable release](https://github.com/ognjeeen/codex-usage-widget/releases/latest/download/codex-usage-widget-win-x64.zip).
2. Start `CodexUsageWidget.exe` after extraction or directly from the ZIP.
3. If usage does not appear, confirm that `codex --version` works in PowerShell and run
   `codex login`.

The portable release includes the .NET runtime. The executable is not currently
code-signed, so Windows may show an unknown-publisher warning. Each GitHub release includes
a SHA-256 checksum for verification.

When Windows starts the executable from a temporary ZIP location, the widget copies that
version to `%LOCALAPPDATA%\CodexUsageWidget\app\<version>` and relaunches it. An executable
started from an extracted folder continues to run there. Only one widget instance runs at
a time.

If Codex is installed outside `PATH`, set `CODEX_USAGE_WIDGET_CODEX_PATH` to the full path
of `codex.cmd` or `codex.exe`.

## What it shows

- Remaining percentage and reset time for every general Codex usage window
- A selectable displayed limit shared by the widget headline, taskbar label, and tray icon
- Compact and detailed desktop-widget layouts selected from Settings or the quick chevron,
  including credits, spend controls, earned resets, token activity, and model-specific limits
  when Codex returns them
- System, light, and dark themes with five preset accent colors selected from Settings
- Automatic Windows-language selection with English fallback, plus English and Simplified
  Chinese overrides in Settings
- A movable, always-on-top desktop widget and a compact label beside the notification area
- Live task activity dots based on official local Codex lifecycle hooks
- Automatic refresh every two minutes plus live rate-limit notifications
- Fullscreen-aware taskbar behavior, per-monitor DPI support, and optional start with Windows
- Local logs for diagnostics, with no telemetry or remote backend

## Display modes and limit selection

**Desktop widget.** Shows the selected limit as the headline and keeps every general limit
visible below it. Compact mode focuses on limits; Details adds account and token activity.

**Taskbar label.** Shows the same selected percentage beside the Windows notification area.
The tray icon and its tooltip follow that selection too.

Choose `5h limit`, `Weekly limit`, or `Most constrained` in **Settings** under **Usage**.
The 5-hour window is the default. Available windows depend on the Codex account, so the
widget falls back to an available window when Codex does not return the selected one.

![Codex Usage Widget detailed preview](docs/images/detailed-widget.png)

Token activity is informational. Token counts do not map directly to the remaining
subscription percentage.

![Codex Usage Widget taskbar label preview](docs/images/taskbar-label.png)

Select the `−` button to move the widget to the taskbar. Right-click the taskbar label or
tray icon to refresh, change the display mode, open Settings, check for updates, or exit.
The Settings window opens from the gear button on the widget too. Language, theme, accent
color, widget layout, displayed limit, and Start with Windows changes apply as soon as they
are selected.

## Activity dots

Activity dots show whether at least one local Codex turn is running. Updates come from the
official Codex lifecycle hooks through a current-user-only named pipe. The widget does not
read prompt text, responses, transcript paths, or model output.

To enable them:

1. Open **Settings**, then find **Codex activity** under **Features**.
2. Select **Install hooks** and review the exact proposed `~/.codex/hooks.json` change.
3. Select **Copy /hooks and open Codex**, paste `/hooks`, and trust the three definitions.
4. Return to the widget and select **Check again**.

Installation is always explicit. The widget never installs hooks during normal startup.
See [Activity dots](docs/ACTIVITY_DOTS.md) for privacy details, command-line setup, removal,
and recovery behavior.

## Privacy and local data

The widget talks only to the locally installed Codex CLI. It does not scrape a browser,
read authentication secrets, send telemetry, or use a remote backend. Codex remains the
owner of authentication.

The application writes only under `%LOCALAPPDATA%\CodexUsageWidget`:

- `app\<version>\CodexUsageWidget.exe`: stable copy used after a direct ZIP launch
- `display-mode.txt`: desktop or taskbar display preference
- `widget-density.txt`: compact or detailed widget preference
- `displayed-limit.txt`: selected summary limit
- `theme.txt`: system, light, or dark theme preference
- `accent-palette.txt`: selected preset accent color
- `language.txt`: system, English, or Simplified Chinese language preference
- `logs\codex-usage-widget-YYYYMMDD.log`: diagnostic logs retained for 14 days

The widget displays ChatGPT and Codex subscription limits. It does not display OpenAI API
billing or API-key usage.

## Uninstall

1. If activity hooks are installed, open **Settings**, find **Codex activity** under
   **Features**, and select **Remove hooks**.
2. Turn off **Start with Windows** in **Settings** under **General**.
3. Exit the widget.
4. Delete the extracted application folder and `%LOCALAPPDATA%\CodexUsageWidget`. The local
   data folder contains any stable copy, saved preferences, and diagnostic logs.

## Development

The repository pins the .NET SDK in `global.json`.

```powershell
dotnet restore .\CodexUsageWidget.slnx
dotnet test .\CodexUsageWidget.slnx -c Release
dotnet run --project .\src\CodexUsageWidget\CodexUsageWidget.csproj
```

To preview both general rate-limit windows without reading Codex usage, close any running
widget instance and start a local preview build:

```powershell
dotnet run --project .\src\CodexUsageWidget\CodexUsageWidget.csproj -p:EnableUsagePreview=true -- --preview-usage
```

Preview data reports 80% remaining for the 5-hour limit and 15% remaining for the weekly
limit. Standard release builds do not accept the preview flag.

Warnings are treated as errors and the recommended .NET analyzers run during every build.

## Build a portable release

```powershell
.\scripts\publish.ps1 -Runtime win-x64
```

The script runs the complete test suite and creates
`artifacts/release/codex-usage-widget-win-x64.zip`. It also supports `win-arm64` through the
`-Runtime` parameter. See [Releasing](docs/RELEASING.md) for the full maintainer workflow.

## Architecture

See [Architecture](docs/ARCHITECTURE.md) for module responsibilities, runtime flow, and
extension guidance.

## License

Released under the [MIT License](LICENSE). You may use, modify, fork, publish, redistribute,
sublicense, or sell copies of the software as long as you retain the copyright notice and
license text.
