# AI Commander

A lightweight C# WPF Windows daemon designed to intercept global hotkeys and translate them into specific actions for AI agents like Antigravity, VS Code, and Claude.

This project was conceived to work alongside physical macro keyboards (e.g., CH57x) to map physical keys to universal actions (e.g., "Accept", "Deny", "Next"), regardless of which application currently has focus.

## Architecture

The solution is structured into three main projects:
- **AICommander.App**: WPF application. Contains `TrayIcon` and `MainWindow` (configuration UI). Acts as the executable host.
- **AICommander.Core**: Core business logic, YAML configuration parsing, hotkey management via P/Invoke `RegisterHotKey`, and provider implementations. It is kept independent of WPF UI dependencies.
- **AICommander.Tests**: Unit tests using xUnit.

### How Hotkeys Work

1. User presses a global hotkey (e.g., `Ctrl+Alt+Win+O`).
2. `GlobalHotkeyManager` intercepts this via Win32 `WM_HOTKEY` messages.
3. The hotkey is mapped to a logical action string (e.g., `"accept"`).
4. `ActionDispatcher` asks `ProviderRegistry` for the highest priority provider that is currently running and visible.
5. The chosen provider reads its `ActionConfig` for `"accept"` to get a specific `KeySequence` (e.g., `["y", "Enter"]`).
6. The provider brings its window to the front briefly using `SetForegroundWindow`, sends the keys via `SendInput`, and restores focus. (Direct `PostMessage` is avoided for reliability).

## Configuration

Edit the `config/ai-commander.yaml` file to:
- Change hotkeys
- Modify the priority of providers (`provider_priority`)
- Adjust which keys are sent to each application

*Note: During development, the config file in `bin/Debug/net8.0-windows` might not reflect the one in the project root. The `ConfigLoader` has a fallback logic traversing parent directories to find `config/ai-commander.yaml`.*

## Usage

Simply run `AICommander.App.exe`. An icon will appear in the System Tray, from which you can access the graphical configuration.

### Common Development Commands
- **Build**: `dotnet build`
- **Run**: `dotnet run --project src/AICommander.App`
- **Test**: `dotnet test`

## Extending the Project (AI Agent Skills)

This repository is equipped with custom AI agent skills (located in `.agents/skills`) to streamline development and troubleshooting:
- **`add-provider`**: Guide to adding support for a new AI agent provider. Providers inherit from `BaseProvider` in `src/AICommander.Core/Providers`.
- **`add-action`**: Guide to adding new logical actions across providers.
- **`debug-hotkey`**: Diagnostic guide and checklist for troubleshooting unresponsive global hotkeys.
- **`wpf-best-practices` & `dotnet-best-practices`**: Best practices for maintaining clean, decoupled MVVM architecture and C# standards.
# ai-commander
