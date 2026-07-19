---
name: debug-hotkey
description: "Diagnostic guide and checklist for troubleshooting unresponsive global hotkeys."
---

# Debug Hotkey Flow

When a hotkey is pressed but nothing happens, follow this diagnostic checklist.

## 1. Check Win32 Hotkey Registration
Sometimes the hotkey is already registered by another application (e.g., Windows itself or a tool like PowerToys).
- Check the console logs for warnings like: `Failed to register hotkey with modifiers X and key Y`.
- **Fix**: Try changing the hotkey combination in `config/ai-commander.yaml`.

## 2. Check Action Dispatching
- Verify that `ActionDispatcher` is attempting to handle the action. 
- Are there logs saying `No active provider found to handle action 'xyz'`?
- If so, it means the `ProviderRegistry` couldn't find a provider that is both `IsRunning()` and `IsVisible()`.

## 3. Verify Target Process Visibility
By default, `BaseProvider.IsVisible()` checks if the process has a `MainWindowHandle != IntPtr.Zero`.
- Some Electron apps have multiple helper processes. Make sure `ProcessName` in the config matches the main process that owns the window.
- Check Windows Task Manager to see the exact process name.

## 4. Check Focus Stealing and SendInput
If the action is dispatched but keys aren't sent to the target app:
- Verify the target app actually receives focus. If it doesn't, Windows might be preventing focus stealing (Check `ForegroundLockTimeout` in Registry).
- Ensure the keys mapped in `key_sequence` are supported by `BaseProvider.ParseKey()`. If not, they will be sent as `0` (null key). 

## 5. Review the Logs
The application uses `Microsoft.Extensions.Logging`. Check the console output or `startup_log.txt` for any exceptions during the dispatch flow.
