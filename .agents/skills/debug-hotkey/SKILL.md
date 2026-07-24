---
name: debug-hotkey
description: Diagnose unresponsive AI Commander global hotkeys on Windows, macOS, or Linux.
---

# Debug a hotkey

1. Check the platform app log directory for shortcut registration errors. A
   conflicting OS/application shortcut is the most common cause.
2. Confirm the saved target exists as either `action` or `provider.action`.
3. Verify the configured platform process name against Task Manager, Activity
   Monitor, or `ps`.
4. Trigger the action from the UI. If this works, focus on shortcut
   registration; if it fails, focus on discovery/activation/input.
5. On macOS, confirm Accessibility permission for AI Commander.
6. On Linux, verify `xdotool` exists and the session is X11 or XWayland. Pure
   Wayland global automation is unsupported.
7. Confirm the sequence uses supported individual keys and inspect errors for
   failed focus restoration or input injection.

After changing parsing, registration, or dispatch logic, add a focused unit
test and run `pnpm check`.
