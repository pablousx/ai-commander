# AI Commander Architecture and Conventions

## Project Overview
AI Commander is a lightweight C# WPF daemon that intercepts global hotkeys and translates them into specific actions for AI agents (Antigravity, VS Code, Claude, etc.). It acts as a bridge between physical macro keyboards and AI applications.

## Architecture Structure
- **AICommander.App**: WPF application. Contains `TrayIcon` and `MainWindow` (configuration UI). Acts as the executable host.
- **AICommander.Core**: Business logic, YAML parsing, hotkey management via P/Invoke `RegisterHotKey`, and provider implementations.
- **AICommander.Tests**: Unit tests using xUnit.

## Critical Rules & Guidelines
1. **Never use `PostMessage` directly** for interacting with Electron windows (VSCodium, Antigravity) because it is highly unreliable. Instead, use `SetForegroundWindow` followed by `SendInput` (or an equivalent simulation library).
2. **Keep Core logic independent of WPF**. Do not add WPF UI dependencies to the `AICommander.Core` project. If you need Windows Interop in Core, ensure the csproj has `<UseWPF>true</UseWPF>` but keep the logic decoupled from `App.xaml` or visual elements.
3. Providers are located in `src/AICommander.Core/Providers`. All new providers must inherit from `BaseProvider`.
4. The configuration file is `config/ai-commander.yaml` and the order in `provider_priority` dictates which process receives the keys.
5. For adding a new provider, follow the `add-provider` skill.

## How Hotkeys Work
1. User presses a global hotkey (e.g., `Ctrl+Alt+Win+O`).
2. `GlobalHotkeyManager` intercepts this via Win32 `WM_HOTKEY` messages.
3. The hotkey is mapped to a logical action string (e.g., `"accept"`).
4. `ActionDispatcher` asks `ProviderRegistry` for the highest priority provider that is currently running and visible.
5. The chosen provider reads its `ActionConfig` for `"accept"` to get a specific `KeySequence` (e.g., `["y", "Enter"]`).
6. The provider brings its window to the front briefly, sends the keys via `SendInput`, and restores focus.

## Common Commands
- **Build**: `dotnet build AICommander.sln`
- **Run**: `dotnet run --project src/AICommander.App`
- **Test**: `dotnet test AICommander.sln`
- **Format verify**: `dotnet format AICommander.sln --verify-no-changes --severity error`
- **Tooling setup**: `dotnet tool restore` then `dotnet husky install`

## Code Quality
- Shared style lives in `.editorconfig`; SDK analyzers and `EnforceCodeStyleInBuild` are enabled in `Directory.Build.props`.
- Pre-commit runs `dotnet format --verify-no-changes --severity error`; commit-msg runs CommitLint.Net (`commit-message-config.json`).
- CI also verifies formatting and runs the full solution build/test suite.
- Before committing, agents should run format verify, build, and tests (see Common Commands).

## Known Gotchas
- **Config file location**: During development, the config file in `bin/Debug/net8.0-windows` might not reflect the one in the project root. The `ConfigLoader` has a fallback logic traversing parent directories to find `config/ai-commander.yaml`.
- **Focus stealing / SendInput timing**: `SendInput` requires the target window to be active. If the OS blocks focus stealing (e.g. `ForegroundLockTimeout`), the keys might be sent to the wrong window. That's why `SetForegroundWindow` is used along with a brief `Task.Delay(50)`.
