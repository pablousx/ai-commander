# AI Commander Design

## Purpose

AI Commander is a tray-resident desktop utility that maps global shortcuts to
application-specific key sequences. A logical action such as `accept` can target
the first eligible running application, while a scoped action such as
`vscode.accept` targets one application directly.

The design prioritizes:

- predictable shortcut routing;
- safe focus and keyboard automation;
- transactional configuration changes;
- a small privileged boundary;
- low idle resource usage; and
- explicit platform limitations.

AI Commander does not automate arbitrary shell commands, expose general
filesystem access to the webview, or promise compositor-independent Wayland
automation.

## System shape

```text
Framework-free TypeScript UI
  ├─ edits an in-memory configuration projection
  ├─ validates and normalizes hotkey input
  └─ invokes three typed Tauri commands
       │
       ▼
Tauri command boundary
  ├─ get_app_info
  ├─ save_config
  └─ dispatch_action
       │
       ▼
Rust runtime
  ├─ config.rs       schema, migration, validation, backup, persistence
  ├─ shortcuts.rs    canonicalization and global shortcut parsing
  ├─ runtime.rs      registration, routing, serialization, notifications
  └─ automation.rs   process lookup, focus, key injection, focus restoration
```

The TypeScript frontend owns presentation and form state. Rust owns persistent
state, native plugins, process discovery, global shortcut registration, and all
OS automation. `src-tauri/src/commands.rs` is the only webview-to-native
application boundary.

## Domain model

The YAML configuration is the durable source of truth.

| Concept           | Meaning                                                         |
| ----------------- | --------------------------------------------------------------- |
| Provider          | An application that can receive actions                         |
| Process names     | Platform-specific executable or application names               |
| Action            | A logical name and ordered key sequence                         |
| Provider priority | Search order for unscoped actions                               |
| Hotkey            | A global gesture mapped to a scoped or unscoped action ID       |
| Settings          | Autostart, tray visibility, and action notification preferences |

Configuration version 2 stores platform-specific process names. Version 1 is
migrated on load by copying the legacy process name into missing platform
fields. Provider and action lookup is case-insensitive, while configured
spelling and insertion order are preserved.

An action ID has one of two forms:

- `action` searches enabled providers in `provider_priority`;
- `provider.action` bypasses priority and resolves that provider directly.

## Core workflows

### Startup

1. Tauri resolves the application configuration path.
2. A missing file is initialized from `config/ai-commander.yaml`.
3. The YAML is parsed, migrated, and validated.
4. Runtime state and the tray are initialized.
5. Global shortcuts and system settings are applied.
6. The UI loads configuration and platform information through `get_app_info`.

Development builds use the repository configuration when it exists. Packaged
builds use the operating system's application configuration directory.

### Save and reload

1. The UI converts editable form state into version 2 configuration.
2. Rust validates the complete replacement before changing live state.
3. Shortcut registrations are replaced as a unit.
4. Autostart and tray settings are applied.
5. The existing YAML is copied to `.yaml.bak`.
6. A temporary file is written and atomically renamed into place.
7. Runtime configuration is replaced only after every required step succeeds.

If registration, system settings, or persistence fails, the previous shortcuts
and system settings are restored and the previous runtime configuration remains
active.

### Action dispatch

1. A pressed global shortcut resolves to an action ID.
2. A dispatch mutex prevents key sequences from interleaving.
3. The runtime selects an enabled provider with a visible matching process.
4. The target window is activated.
5. Existing modifiers are released and the configured sequence is sent.
6. Held modifiers are released in reverse order.
7. The previously focused application is restored.
8. The runtime emits an event and optionally shows a notification.

Manual “test action” requests use the same dispatch path with a scoped action
ID.

## Safety invariants

- Replacement configuration is validated before live state is mutated.
- Invalid or duplicate shortcuts never partially replace active registrations.
- Dispatches are serialized.
- Modifier keys are released after success or input failure.
- Focus restoration is attempted after every key-send result.
- UI errors are structured native command failures, not panics.
- The webview receives only `core:default`; it has no broad shell or filesystem
  capability.
- Pure Wayland fails with an actionable limitation instead of pretending to
  provide portable global automation.

## Platform strategy

| Platform           | Activation and restoration                          | Input       |
| ------------------ | --------------------------------------------------- | ----------- |
| Windows            | Win32 window APIs                                   | Enigo       |
| macOS              | AppleScript/System Events                           | Enigo       |
| Linux X11/XWayland | `xdotool`                                           | Enigo       |
| Linux Wayland-only | Unsupported without compositor-specific integration | Unsupported |

Platform-specific automation stays behind `automation.rs`. New integrations
must preserve visible-window selection, modifier cleanup, and focus restoration,
and must document permissions or compositor requirements.

## UI design

The UI is deliberately framework-free. `src/main.ts` owns a single typed state
object and renders the settings surface from that state. Provider templates are
creation conveniences only; saved YAML remains authoritative.

The UI:

- presents providers in routing-priority order;
- captures and canonicalizes global shortcuts;
- edits per-platform process names and action key sequences;
- keeps changes local until Save;
- reports native validation and dispatch failures without discarding edits; and
- listens for successful native dispatch events.

The frontend must not duplicate native persistence or routing behavior. Client
validation improves feedback, but Rust remains authoritative.

## Extension rules

Add a provider or action through configuration and templates when no new native
capability is required. Add a Tauri command only when the UI needs a new narrow,
privileged operation.

Changes belong in:

- `src/` for rendering, form state, hotkey capture, and typed command calls;
- `commands.rs` for serializable request/response boundaries;
- `config.rs` for schema, migration, validation, backup, and persistence;
- `runtime.rs` for routing, registration, dispatch serialization, and reload;
- `automation.rs` for process, focus, and input behavior; and
- `src-tauri/capabilities/default.json` for the smallest required permission.

Detailed runtime behavior and platform notes live in
`docs/architecture.md`. Test coverage expectations live in `docs/testing.md`.
