---
name: add-provider
description: "Step-by-step guide to add support for a new AI agent provider."
---

# Add a New Provider

To add support for a new AI agent (e.g., a new IDE or tool), you need to create a class that inherits from `BaseProvider`.

## 1. Create the Provider Class
Create a new file in `src/AICommander.Core/Providers/[Name]/[Name]Provider.cs`.

```csharp
namespace AICommander.Core.Providers.NewAgent
{
    public class NewAgentProvider : BaseProvider
    {
        // The name used in the YAML config (e.g., "newagent")
        public override string Name => "newagent";
        
        // Optional: override ProcessName if it differs from Name.ToLower()
        // Optional: override IsRunning() or IsVisible() if custom logic is needed
    }
}
```

## 2. Register the Provider
Open `src/AICommander.App/App.xaml.cs` and add your provider to the initialization list in `Application_Startup`:

```csharp
var providers = new List<IProvider>
{
    new AntigravityProvider(),
    new VSCodeProvider(),
    new ClaudeProvider(),
    new NewAgentProvider() // <-- Add here
};
```

## 3. Add Default Configuration
Update `config/ai-commander.yaml` to include the new provider, its process name, and default actions. Also, make sure to add it to the `provider_priority` list if you want it to be considered during action dispatching.

```yaml
provider_priority:
  - antigravity
  - vscode
  - claude
  - newagent # <-- Add here

providers:
  newagent:
    enabled: true
    process_name: "new_agent_process"
    actions:
      accept:
        key_sequence: ["Ctrl+Enter"]
      reject:
        key_sequence: ["Escape"]
```

`BaseProvider` takes care of putting the window in focus and sending the keys, so typically no further logic is required.

## 4. Tests (pragmatic TDD)

A provider that only inherits `BaseProvider` with no custom `IsRunning` / `IsVisible` / action logic usually needs **no new unit tests**.

If you change Core behavior (registry priority, dispatch routing, config shape/parsing, or `KeyParser`), write or update failing tests in `AICommander.Tests` **first**, then implement. See [docs/testing.md](../../../docs/testing.md).

```bash
dotnet test AICommander.sln
```
