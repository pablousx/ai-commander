# Contributing to AI Commander

## Prerequisites

- Node.js 22.18 or newer
- pnpm 11.15.1
- Rust stable with `rustfmt` and `clippy`
- Native dependencies listed in the
  [Tauri prerequisites](https://v2.tauri.app/start/prerequisites/)
- Linux: `xdotool` plus the WebKitGTK development packages

Install dependencies and Git hooks:

```bash
pnpm install
```

## Development workflow

Run the complete application with hot reload:

```bash
pnpm desktop:dev
```

Before opening a pull request, run:

```bash
pnpm check
pnpm desktop:build
```

`pnpm check` verifies Oxfmt, rustfmt, Oxlint, Clippy, TypeScript and Rust tests,
and the production frontend build. Lefthook runs formatting/linting before
commits, tests before pushes, and Commitlint on Conventional Commit messages.

## Testing

Use pragmatic TDD for routing, migration, validation, shortcut parsing, and
platform-neutral automation behavior. Put frontend tests beside the relevant
TypeScript module and Rust unit tests inside the owning module.

OS integrations still require a manual smoke test on the target OS:

1. Start `pnpm desktop:dev`.
2. Grant Accessibility permission on macOS when prompted.
3. Save a non-conflicting shortcut.
4. Open the target application.
5. Trigger the shortcut and verify focus is restored.
6. Quit from the tray and verify all registrations are released.

## Pull requests and releases

Use Conventional Commits with headers no longer than 90 characters. Branch
from `develop` and target `develop`; `main` is release-ready.

CI checks Windows, macOS, and Linux. Pushing a semantic tag such as `v0.2.0`
creates a draft GitHub release with native bundles for all three platforms.

See [docs/architecture.md](docs/architecture.md) before changing the native
boundary and [docs/testing.md](docs/testing.md) for the test matrix.
