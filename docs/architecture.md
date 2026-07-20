# Architecture

The solution is structured into three main projects:
- **AICommander.App**: WPF application. Contains `TrayIcon` and `MainWindow` (configuration UI). Acts as the executable host.
- **AICommander.Core**: Core business logic, YAML configuration parsing, hotkey management via P/Invoke `RegisterHotKey`, and provider implementations. It is kept independent of WPF UI dependencies.
- **AICommander.Tests**: Unit tests using xUnit.

## How Hotkeys Work

1. User presses a global hotkey (e.g., `Ctrl+Alt+Win+O`).
2. `GlobalHotkeyManager` intercepts this via Win32 `WM_HOTKEY` messages.
3. The hotkey is mapped to a logical action string (e.g., `"accept"`).
4. `ActionDispatcher` asks `ProviderRegistry` for the highest priority provider that is currently running and visible.
5. The chosen provider reads its `ActionConfig` for `"accept"` to get a specific `KeySequence` (e.g., `["y", "Enter"]`).
6. The provider brings its window to the front briefly using `SetForegroundWindow`, sends the keys via `SendInput`, and restores focus. (Direct `PostMessage` is avoided for reliability).
