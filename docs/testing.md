# Testing strategy

AI Commander has two fast test layers and one manual platform layer.

| Layer        | Command              | Coverage                                                                         |
| ------------ | -------------------- | -------------------------------------------------------------------------------- |
| TypeScript   | `pnpm test:frontend` | Hotkey capture and normalization                                                 |
| Rust         | `pnpm test:native`   | Config migration/validation/save, shortcut parsing, routing helpers, key parsing |
| Native smoke | `pnpm desktop:dev`   | OS registration, focus, input, tray, permissions                                 |

Run every automated quality gate with:

```bash
pnpm check
```

Use pragmatic TDD for behavior changes: add the failing case in the module that
owns the behavior, implement it, then refactor. Prefer table-driven Rust tests
and small pure TypeScript functions.

CI runs formatting, linting, tests, and the frontend production build on
Windows, macOS, and Ubuntu. Tagged release builds additionally exercise the
full Tauri bundler on each platform.

OS automation cannot be made trustworthy in a headless unit test. Manually
verify global shortcut conflicts, window activation, input delivery, focus
restoration, tray lifecycle, and macOS Accessibility permissions on the
affected platform. Linux automation must also be checked on X11/XWayland; pure
Wayland is explicitly unsupported.
