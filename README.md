# AI Commander

<div align="center">
  <img src="assets/logo.png" alt="AI Commander Logo" width="200"/>
</div>

A lightweight C# WPF Windows daemon designed to intercept global hotkeys and translate them into specific actions for AI agents like Antigravity, VS Code, and Claude.

This project was conceived to work alongside physical macro keyboards (e.g., CH57x) to map physical keys to universal actions (e.g., "Accept", "Deny", "Next"), regardless of which application currently has focus.

## Table of Contents
- [Usage](#usage)
- [Configuration](#configuration)
- [Development](#development)
- [Documentation](#documentation)
- [Extending the Project](#extending-the-project)

## Usage

Simply run `AICommander.App.exe`. An icon will appear in the System Tray, from which you can access the graphical configuration.

## Configuration

Edit the `config/ai-commander.yaml` file to:
- Change hotkeys
- Modify the priority of providers (`provider_priority`)
- Adjust which keys are sent to each application

*Note: During development, the config file in `bin/Debug/net8.0-windows` might not reflect the one in the project root. The `ConfigLoader` has a fallback logic traversing parent directories to find `config/ai-commander.yaml`.*

## Development

### Common Development Commands
- **Setup tooling**: `dotnet tool restore` then `dotnet husky install`
- **Build**: `dotnet build AICommander.sln`
- **Run**: `dotnet run --project src/AICommander.App`
- **Test**: `dotnet test AICommander.sln`
- **Format verify**: `dotnet format AICommander.sln --verify-no-changes`

See [CONTRIBUTING.md](CONTRIBUTING.md) for code quality gates and [`.agents/AGENTS.md`](.agents/AGENTS.md) for architecture rules.

## Documentation

For more detailed technical information, please refer to the documentation:

- [Architecture & Hotkey Flow](docs/architecture.md)
- [Providers](docs/providers.md)
- [Testing & TDD](docs/testing.md)
- [Contributing](CONTRIBUTING.md)

## Extending the Project (AI Agent Skills)

This repository is equipped with custom AI agent skills (located in `.agents/skills`) to streamline development and troubleshooting:
- **`add-provider`**: Guide to adding support for a new AI agent provider. Providers inherit from `BaseProvider` in `src/AICommander.Core/Providers`.
- **`add-action`**: Guide to adding new logical actions across providers.
- **`debug-hotkey`**: Diagnostic guide and checklist for troubleshooting unresponsive global hotkeys.
- **`wpf-best-practices` & `dotnet-best-practices`**: Best practices for maintaining clean, decoupled MVVM architecture and C# standards.
