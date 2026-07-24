---
name: add-action
description: Add a logical AI Commander action and its global or scoped shortcut.
---

# Add an action

1. Add the action's `key_sequence` to each supporting provider in
   `config/ai-commander.yaml`.
2. Map a shortcut to `action` for priority routing or `provider.action` for a
   specific target.
3. If a key is unsupported, add its mapping in
   `src-tauri/src/automation.rs` and write a Rust parser test first.
4. If hotkey normalization changes, update `src/hotkeys.ts` and its Vitest
   cases.
5. Run `pnpm check`, then manually dispatch the action on each affected OS.

Sequences contain individual keys, for example `[Ctrl, Shift, Enter]`, not a
combined string.
