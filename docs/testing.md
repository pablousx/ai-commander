# Testing Strategy

AI Commander uses **pragmatic TDD** for Core behavior: write a failing test first, implement until green, then refactor. The merge gate is a green suite plus new or updated tests for the change—not pedantic red-phase enforcement in CI.

## What we test

Focus on pure Core logic that decides product behavior:

| Area | Project types |
|------|----------------|
| `KeyParser` | VK / modifier parsing |
| `ConfigLoader` / `ConfigManager` | YAML load, save, reload |
| `ProviderRegistry` | Priority, enabled/running/visible selection |
| `ActionDispatcher` | Global vs provider-scoped routing |

Use hand-written `IProvider` fakes (see `src/AICommander.Tests/Fakes/`). Prefer xUnit `[Theory]` for tables of inputs. Follow Arrange–Act–Assert.

## What we do not automate (yet)

- Win32 `RegisterHotKey` / `WM_HOTKEY` (OS-owned; use the `debug-hotkey` skill)
- `SendInput` / `SetForegroundWindow` focus timing
- WPF UI, tray icon, and ViewModels
- End-to-end launches of real Antigravity / VS Code / Claude processes

Thin interop wrappers stay untested unless you extract pure logic into Core helpers.

## Commands

```bash
dotnet test AICommander.sln
dotnet test AICommander.sln --filter "FullyQualifiedName~ProviderRegistryTests"
```

CI runs the full suite on Windows (`windows-latest`). Pre-push runs tests when C#/project files change; pre-commit only verifies formatting.

## When TDD applies

**Required** for behavior changes in `AICommander.Core` (config parsing, dispatch, registry, key parsing, provider selection rules).

**Not required** for pure XAML, docs-only edits, or commit/chore changes that do not alter Core behavior.

New providers that only inherit `BaseProvider` without custom logic usually need no new unit tests; if you change registry/dispatch/config rules, add or update tests first.

## Conventions

- Framework: **xUnit** only (`[Fact]` / `[Theory]`)
- Assertions: built-in `Assert.*`
- Fakes: hand-written doubles implementing `IProvider` — no Moq unless a clear need appears
- Logging in tests: `NullLogger<T>.Instance` from `Microsoft.Extensions.Logging.Abstractions`
- Tests live in `src/AICommander.Tests/`

See also [`.agents/AGENTS.md`](../.agents/AGENTS.md) (Critical Rules) and [CONTRIBUTING.md](../CONTRIBUTING.md).
