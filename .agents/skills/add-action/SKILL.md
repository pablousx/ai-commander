---
name: add-action
description: "Guide on how to add a new logical action across providers."
---

# Add a New Logical Action

To introduce a new global action (e.g., `next`, `previous`, `toggle`), follow these steps.

## 1. Add the Global Hotkey
Define the new action in `config/ai-commander.yaml` under the `hotkeys` section.

```yaml
hotkeys:
  "Ctrl+Alt+Win+N": "next"
  "Ctrl+Alt+Win+P": "previous"
```

## 2. Map the Action in Providers
For every provider that should support this action, add the corresponding `key_sequence` in their configuration in `config/ai-commander.yaml`.

```yaml
providers:
  vscode:
    actions:
      next:
        key_sequence: ["Tab"]
  antigravity:
    actions:
      next:
        key_sequence: ["ArrowDown"]
```

## 3. Ensure the Key is Parsed
If your `key_sequence` uses a key that is not yet supported by `BaseProvider.ParseKey(string keyString)`, you must add it to `src/AICommander.Core/Providers/BaseProvider.cs`.

```csharp
protected virtual ushort ParseKey(string keyString)
{
    // Existing keys...
    if (keyString.Equals("ArrowDown", StringComparison.OrdinalIgnoreCase)) return 0x28; // VK_DOWN
    if (keyString.Equals("ArrowUp", StringComparison.OrdinalIgnoreCase)) return 0x26;   // VK_UP
    
    // ...
}
```
*Note: Refer to Microsoft's Virtual-Key Codes documentation for the correct hex values. New key aliases belong in `KeyParser`, not a copy inside `BaseProvider`.*

## 4. Tests (pragmatic TDD)

Config-only actions (YAML hotkey + per-provider `key_sequence`) usually need **no new unit tests**.

If you add or change `KeyParser` aliases, dispatch/registry rules, or config load/save behavior, write a failing test in `AICommander.Tests` **first**, then implement. See [docs/testing.md](../../../docs/testing.md).

```bash
dotnet test AICommander.sln
```
