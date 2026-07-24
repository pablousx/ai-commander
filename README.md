# AI Commander

<div align="center">
  <img src="assets/logo.png" alt="AI Commander logo" width="180">
</div>

AI Commander is a lightweight Tauri desktop utility for Windows, macOS, and
Linux. It turns global hotkeys into application-specific key sequences so one
macro pad can accept, reject, navigate, or trigger actions in different AI
coding tools.

The configuration window is a small TypeScript/Vite frontend. The resident
runtime, global shortcut handling, process discovery, focus switching, and
input injection are native Rust.

## Platform support

| Platform           | Window activation         | Input   | Notes                                                   |
| ------------------ | ------------------------- | ------- | ------------------------------------------------------- |
| Windows 10/11      | Win32                     | Enigo   | Fully native                                            |
| macOS 12+          | AppleScript/System Events | Enigo   | Accessibility permission required                       |
| Linux X11/XWayland | `xdotool`                 | Enigo   | `xdotool` is installed by the `.deb` package            |
| Linux Wayland-only | Limited                   | Limited | Compositor security prevents portable global automation |

## Run locally

Prerequisites are Node.js 22.18+, pnpm 11.15.1, Rust stable, and the
[Tauri system dependencies](https://v2.tauri.app/start/prerequisites/) for your
operating system.

```bash
pnpm install
pnpm desktop:dev
```

Useful commands:

```bash
pnpm build          # type-check and build the web UI
pnpm test           # TypeScript and Rust tests
pnpm lint           # Oxlint and Clippy
pnpm format:check   # Oxfmt and rustfmt
pnpm check          # complete local quality gate
pnpm desktop:build  # native installer/bundle for this OS
```

## Configuration

The first run copies the bundled defaults to the OS application config
directory:

- Windows: `%APPDATA%\com.pablousx.ai-commander\ai-commander.yaml`
- macOS: `~/Library/Application Support/com.pablousx.ai-commander/ai-commander.yaml`
- Linux: `~/.config/com.pablousx.ai-commander/ai-commander.yaml`

Use the UI to edit applications, process names, priority, hotkeys, key
sequences, startup behavior, and notifications. Saves are validated and create
an `ai-commander.yaml.bak` recovery copy. Version 1 configuration is migrated
to version 2 automatically.

An unscoped action such as `accept` selects the first running application in
`provider_priority`. A scoped action such as `vscode.accept` always targets
that application.

## Documentation

- [Architecture](docs/architecture.md)
- [Applications and actions](docs/providers.md)
- [Testing](docs/testing.md)
- [Contributing](CONTRIBUTING.md)
