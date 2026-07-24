---
description: Verify and create a Conventional Commit for the Tauri project
---

# Commit

1. Inspect status, diffs, and recent messages. Preserve unrelated user changes.
2. Run `pnpm check` for code changes.
3. Stage only files that belong to the requested change.
4. Use `<type>(<optional-scope>): <imperative description>`, maximum 90
   characters. Common scopes are `ui`, `native`, `hotkey`, `config`, `build`,
   and `ci`.
5. Commit normally. Lefthook runs formatting/linting before commit, Commitlint on
   the message, and tests before push.
6. Do not push or amend unless explicitly requested.
