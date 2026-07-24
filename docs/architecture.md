# Architecture

AI Commander uses a Tauri 2 process with a framework-free TypeScript frontend.
The split keeps startup and resident memory low while preserving a productive,
hot-reloaded settings UI.

```text
TypeScript UI (src/)
  └─ Tauri invoke/events
      └─ commands.rs
          ├─ config.rs       YAML schema, validation, migration, atomic save
          ├─ shortcuts.rs    global shortcut parsing and registration
          ├─ runtime.rs      transactional reload and serialized dispatch
          └─ automation.rs   process discovery, focus, input, focus restore
```

## Dispatch lifecycle

1. Tauri's global-shortcut plugin receives a registered shortcut.
2. `RuntimeState` maps it to either `action` or `provider.action`.
3. For an unscoped action, enabled providers are checked in
   `provider_priority` order; scoped actions bypass priority selection.
4. `automation` finds a matching process and activates its visible window.
5. Enigo sends the configured key sequence with modifiers held safely.
6. The previously active application is restored.
7. Dispatches are serialized so overlapping shortcuts cannot interleave input.

Saving is transactional. The new YAML is migrated and validated first,
shortcut registrations are replaced as a unit, and failures roll back to the
previous configuration and registrations. The on-disk file is backed up before
replacement.

## Platform boundary

- Windows uses `EnumWindows`, `ShowWindow`, and `SetForegroundWindow`.
- macOS uses `osascript`/System Events for process activation and restoration.
- Linux uses `xdotool` on X11 or XWayland. Pure Wayland deliberately returns an
  actionable error because compositor-wide input and activation are not
  portable or generally permitted.

The UI never receives filesystem or shell permissions. Its only privileged
operations are the narrow commands declared in `commands.rs`.

## Runtime behavior

The app is single-instance and tray-resident. Closing the settings window hides
it; the tray menu can reopen settings or quit. Autostart and notifications are
Tauri plugins, while logs use the platform app log directory.
