# Applications and actions

Applications are configuration-driven; adding one does not require Rust code.

```yaml
version: 2
provider_priority:
  - vscode

providers:
  vscode:
    enabled: true
    process_names:
      windows: [Code.exe]
      macos: [Visual Studio Code]
      linux: [code]
    actions:
      accept:
        key_sequence: [Ctrl, Enter]

hotkeys:
  Ctrl+Alt+Enter: vscode.accept
```

Use the executable name for Windows/Linux and the application process name on
macOS.

Each key sequence item is one key. Modifiers may precede the primary key:
`[Ctrl, Shift, Enter]`. Supported modifiers are `Ctrl`, `Alt`, `Shift`,
`Meta`/`Super`, and `Command`. Common navigation, function, letter, number, and
punctuation keys are supported.

Hotkey targets have two forms:

- `accept` chooses the highest-priority running provider with that action.
- `vscode.accept` only dispatches to `vscode`.

The settings UI can add applications and actions, reorder priority, record
shortcuts, test actions, and save. Validation rejects duplicate shortcuts,
unknown targets, invalid key sequences, missing process names, and duplicate
priority entries before changing the active runtime.
