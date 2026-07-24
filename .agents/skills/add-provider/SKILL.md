---
name: add-provider
description: Add a configuration-driven application provider to AI Commander.
---

# Add a provider

Use the UI or edit `config/ai-commander.yaml`. Add the identifier to
`provider_priority`, then define:

- `display_name`
- `enabled`
- optional icon name
- `process_names.windows`, `.macos`, and `.linux`
- one or more actions with `key_sequence`

Do not add Rust provider classes. All providers use the same validated runtime
and platform automation boundary. Native code is appropriate only when the
generic process/focus/input model fundamentally cannot support the application.

Run `pnpm check`, then manually verify process discovery, activation, input,
and focus restoration on every affected OS.
