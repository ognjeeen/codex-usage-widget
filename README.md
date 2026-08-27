# Codex Usage Widget

A local-only Windows widget that shows the remaining Codex subscription allowance.
It talks directly to the official Codex app server through
`account/rateLimits/read` and listens for live `account/rateLimits/updated`
notifications.

> This is an independent utility and is not an official OpenAI application.

![Codex Usage Widget desktop preview](docs/images/desktop-widget.png)

## Features

- Remaining percentage and reset time for every general Codex rate-limit window
- Compact and detailed widget layouts with optional token-activity history
- Credit, spend-control, earned-reset, and model-specific limit details when available
- Compact, movable, always-on-top desktop widget
- Native-looking taskbar label beside the Windows notification area
- Selectable displayed limit shared by the widget, taskbar label, and tray icon,
  with a 5-hour default and automatic fallback
- Event-driven task activity animation through official local Codex lifecycle hooks
- Immediate taskbar-label hiding while another app is fullscreen on the same monitor
- Persistent desktop/taskbar display preference
- Optional per-user start with Windows registration
- Automatic refresh every two minutes and live server notifications
- Single-instance protection to prevent overlapping labels
- Per-monitor DPI support, local diagnostic logs, and graceful CLI reconnects
- Model-specific buckets stay out of the compact view and appear only in details
- No browser automation, token scraping, telemetry, or external backend

## Requirements

- Windows 10 version 1809 or newer
- Codex CLI available on `PATH`
- A completed local sign-in (`codex login`)

The portable release includes the .NET runtime. A separate .NET installation is
therefore not required on the destination computer.

## Install on another computer

1. Download the
   [latest Windows x64 portable release](https://github.com/ognjeeen/codex-usage-widget/releases/latest/download/codex-usage-widget-win-x64.zip).
2. Start `CodexUsageWidget.exe`, either after extraction or directly from the ZIP.
3. Ensure `codex --version` works in PowerShell and run `codex login` if needed.

When Windows starts the executable from a temporary ZIP location, the widget atomically
copies that exact version to `%LOCALAPPDATA%\CodexUsageWidget\app\<version>` and relaunches
the stable per-user copy. An executable already extracted to a normal directory continues
to run in place.

Only one instance can run at a time. Starting the executable again exits quietly.

The executable is not currently code-signed, so Windows may identify the
publisher as unknown. A SHA-256 checksum is attached to every
[GitHub Release](https://github.com/ognjeeen/codex-usage-widget/releases) for
verification before running the application.

If Codex is installed in a non-standard location, set
`CODEX_USAGE_WIDGET_CODEX_PATH` to the full path of `codex.cmd` or `codex.exe`.

## Display modes

- **Desktop widget** keeps the selected widget layout visible and always on top.
- **Taskbar label** shows `Codex 75%` directly to the left of the notification area.

The desktop widget has two persisted layouts:

- **Compact** shows the general Codex windows and highlights the selected displayed limit.
- **Details** adds available credits and spend controls, token activity, earned resets,
  and model-specific limits such as GPT-5.3-Codex-Spark.

The selected **Displayed limit** controls the headline percentage in the widget, the taskbar
label, and the tray icon. Limit rows continue to show every general Codex window.

![Codex Usage Widget detailed preview](docs/images/detailed-widget.png)

Token activity is informational and is intentionally presented separately from quota
consumption because tokens do not map linearly to the remaining subscription percentage.

![Codex Usage Widget taskbar label preview](docs/images/taskbar-label.png)

Use the `−` button to switch to taskbar mode. Right-click the taskbar label or tray
icon to refresh, change display mode, or exit.

Choose **Start with Windows** from either menu to register the running executable for
the signed-in Windows user. The option does not require administrator rights, and turning
it off removes the registration. If an extracted portable folder moves, the path is
refreshed the next time the widget is started manually.

## Live Codex activity dots

Activity dots turn the official local Codex lifecycle hooks into an at-a-glance signal
that work is still running. They are available in both the taskbar label and desktop
widget, without polling Codex or estimating activity from rate-limit changes.

### What activity dots provide

- One quiet dot while Codex is idle, expanding into a three-dot wave during active work
- Immediate, event-driven updates when a Codex turn starts or finishes
- Independent tracking of parallel Codex sessions, so one completed session cannot hide
  another session that is still running
- A completion animation only after the final active turn finishes
- A dedicated setup window for installation status, trust approval, refresh, and removal

### Private and local by design

- No prompts, responses, transcript contents, transcript paths, or model output are
  collected, stored, forwarded, or logged
- No telemetry, analytics, browser automation, remote backend, or credential access is used
- Hook signals stay on the current Windows account through a current-user-only named pipe
- Hook handlers use a small path-independent PowerShell-to-pipe bridge and never start the
  WPF widget executable for each lifecycle event
- Only the lifecycle event type and the Codex-provided session and turn identifiers are
  passed to the in-memory activity monitor
- Activity state is not persisted, so the widget does not build a history of your work
- Authentication remains entirely owned by the locally installed Codex CLI

Hook installation remains an explicit, reviewable action and is never performed during
normal widget startup.

### Setup and removal

Select the three-dot activity button in the desktop widget, or choose **Activity dots...**
from the tray or taskbar-label menu. The setup window reports whether the hooks are missing,
awaiting approval, active, modified, or disabled. Select **Install hooks**, review the exact
proposed `~/.codex/hooks.json` content, and confirm the change.

After installation, select **Copy /hooks and open Codex**. Paste `/hooks` into Codex, then
review and trust the exact new `UserPromptSubmit`, `Stop`, and `SessionEnd` definitions.
New or changed definitions require new trust. Return to the setup window and select
**Check again** to verify that activity reporting is ready.

The setup window can remove handlers generated by the current widget and conservatively
recognized handlers from earlier Codex Usage Widget portable locations. Recognition is limited
to the exact command formats historically generated for `CodexUsageWidget.exe`; similarly named
handlers from other applications, existing hooks, and unknown configuration fields are preserved.

For scripted setup or recovery, the existing command-line flow remains available. From
PowerShell in the directory containing the widget executable, run:

```powershell
.\CodexUsageWidget.exe --install-activity-hooks
```

The command displays the proposed content and writes it only after interactive confirmation.

To perform the equivalent removal from PowerShell, run:

```powershell
.\CodexUsageWidget.exe --uninstall-activity-hooks
```

If the widget is closed, the hook handler exits successfully after a short bounded
connection attempt and Codex continues normally.

Activity state is intentionally in memory only. A task that started before the widget
or hooks were ready is not reconstructed. Each session owns at most one active turn: a
later `UserPromptSubmit` replaces an orphaned turn, and a late `Stop` for the old turn
cannot clear the current one. If Codex terminates without any later lifecycle event, a
widget restart still clears the in-memory state; no arbitrary timeout is used because
legitimate tasks can run for a long time.

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

The preview reports 80% remaining for the 5-hour limit and 15% remaining for the weekly
limit. Standard release builds do not accept the preview flag.

Warnings are treated as errors and the recommended .NET analyzers run during every
build.

## Portable release

```powershell
.\scripts\publish.ps1 -Runtime win-x64
```

The script runs the complete test suite and creates:

```text
artifacts/release/codex-usage-widget-win-x64.zip
```

`win-arm64` is also supported through the script's `-Runtime` parameter.

Maintainer release instructions are documented in
[docs/RELEASING.md](docs/RELEASING.md).

## Local data

The application only writes under `%LOCALAPPDATA%\CodexUsageWidget`:

- `app\<version>\CodexUsageWidget.exe` — stable copy used after a direct ZIP launch
- `display-mode.txt` — the selected display mode
- `widget-density.txt` — the selected compact or detailed widget layout
- `displayed-limit.txt` — the selected limit used by summary usage displays
- `logs\codex-usage-widget-YYYYMMDD.log` — diagnostics, retained for 14 days

No credentials are read or stored by the widget. Authentication remains owned by
the locally installed Codex CLI.

## Architecture

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for component boundaries,
runtime flow, and extension guidance.

## Usage semantics

This displays ChatGPT/Codex subscription rate limits. It does not display OpenAI
API billing or API-key usage, which use a different accounting system.

## License

Released under the [MIT License](LICENSE). You may use, modify, fork, publish,
redistribute, sublicense, or sell copies of the software as long as the copyright
notice and license text are retained.
