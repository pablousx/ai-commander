# Agent Instructions

## Scope and stack

- Use TypeScript for the framework-free Vite UI and all repository tooling.
- Use Rust only for the Tauri host, privileged OS integration, and native tests.
- Use pnpm 11 through the checked-in `packageManager`; never use npm, npx, Yarn, or Bun.
- Treat `pnpm-lock.yaml` and `src-tauri/Cargo.lock` as generated; update them with pnpm or Cargo.

## Design constraints

- Follow the ownership boundaries and invariants in `DESIGN.md`.
- Keep `src-tauri/src/commands.rs` as the narrow webview-to-native boundary.
- Validate replacement configuration before mutating live state.
- Serialize dispatch and restore modifiers, focus, shortcuts, and settings on failure.
- Do not grant broad shell or filesystem access; add the smallest capability required in `src-tauri/capabilities/default.json`.
- Do not claim portable Wayland automation without a compositor-specific integration and consent.

## Commands

| Task                | Command                                    |
| ------------------- | ------------------------------------------ |
| Install             | `pnpm install --frozen-lockfile`           |
| Development app     | `pnpm desktop:dev`                         |
| Frontend test file  | `pnpm vitest run src/path/to/file.test.ts` |
| Native test         | `pnpm test:native -- test_name`            |
| Frontend lint       | `pnpm exec oxlint path/to/file.ts`         |
| Dead-code check     | `pnpm knip`                                |
| Format repository   | `pnpm format`                              |
| Required final gate | `pnpm check`                               |
| Native bundle       | `pnpm desktop:build`                       |

## Change and test policy

- Preserve existing user changes; inspect the worktree before editing.
- Prefer narrow, typed Tauri commands and serializable request/response types.
- Model invalid TypeScript states out of the type system; avoid `any` and unchecked casts.
- Return structured Rust errors; do not panic on user input, configuration, or OS failures.
- Add regression tests for config, migration, routing, shortcut parsing, and pure automation logic.
- Run target-OS smoke tests for focus, global shortcuts, tray, autostart, and input injection changes.
- Run `pnpm check` before handoff; run `pnpm desktop:build` when packaging or Tauri config changes.
- Do not hand-edit generated output in `dist/`, `src-tauri/target/`, or `src-tauri/gen/`.
- Do not commit, push, or publish unless requested; use Conventional Commits when asked.

## References

| Need                      | File                                                        |
| ------------------------- | ----------------------------------------------------------- |
| Product and system design | `DESIGN.md`                                                 |
| Setup and workflow        | `README.md`, `CONTRIBUTING.md`                              |
| Runtime and platforms     | `docs/architecture.md`                                      |
| Test strategy             | `docs/testing.md`                                           |
| CI and releases           | `.github/workflows/ci.yml`, `.github/workflows/release.yml` |
