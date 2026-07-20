---
description: Stage changes, draft a Conventional Commit message, and commit using project hooks and commit-message-config.json
---

# Commit

Create a git commit that passes Husky + CommitLint.Net for this repo.

## Prerequisites

- Hooks installed: `dotnet tool restore` then `dotnet husky install`
- Rules live in `commit-message-config.json` (subject max 90 chars)

## Message format

```text
<type>(<optional-scope>): <description>

[optional body]

[optional footer(s)]
```

- Imperative mood, lowercase start, no trailing period
- Blank line between subject and body when a body is present
- Breaking change: `!` after type/scope and/or `BREAKING CHANGE:` footer

### Types

| Type | Use when |
| --- | --- |
| `feat` | User-facing feature |
| `fix` | Bug fix |
| `refactor` | No intended behavior change |
| `perf` | Performance |
| `test` | Tests |
| `docs` | Documentation |
| `style` | Formatting only |
| `build` | Build / project / deps |
| `ci` | CI/CD |
| `chore` | Other maintenance |
| `revert` | Revert a prior commit |

### Recommended scopes

`app`, `ui`, `core`, `hotkey`, `provider`, `config`, `ci`

### Examples

```text
feat(ui): add settings window for app and hotkey configuration
```

```text
fix(hotkey): restore focus after SendInput to target provider
```

```text
ci: validate conventional commits with CommitLint.Net
```

## Steps

1. Check status, staged/unstaged diff, and recent commit style:
   ```bash
   git status
   git diff
   git diff --cached
   git log -10 --oneline
   ```

2. Stage only the files that belong in this commit (do not use `git add .` unless the user asks). Prefer focused commits.

3. Draft a Conventional Commit message from the staged diff:
   - Focus on *why*, not a file list
   - Match types/scopes above
   - Keep the subject ≤ 90 characters

4. Verify before commit when code changed:
   ```bash
   dotnet format AICommander.sln --verify-no-changes --severity error
   dotnet test AICommander.sln
   ```

5. Commit using a HEREDOC (or equivalent) so the message keeps formatting. On Windows PowerShell:
   ```powershell
   git commit -m @"
   type(scope): short description

   Optional body explaining why.
   "@
   ```
   Hooks run automatically:
   - `pre-commit` → `dotnet format --verify-no-changes --severity error`
   - `commit-msg` → `dotnet commit-lint` with `commit-message-config.json`

6. If `pre-commit` fails: run `dotnet format AICommander.sln`, re-stage, create a **new** commit attempt.
   If `commit-msg` fails: fix the message and commit again (do not amend unless the user explicitly asks and amend rules allow it).

7. Confirm success with `git status`. Do not push unless the user asks.
