# Contributing to AI Commander

First off, thank you for considering contributing to AI Commander. It's people like you that make AI Commander such a great tool.

## Where do I go from here?

If you've noticed a bug or have a feature request, make one! It's generally best if you get confirmation of your bug or approval for your feature request this way before starting to code.

## Development Setup

The project uses C# .NET 8 WPF. You will need a Windows environment to run and build the application.

### Prerequisites
- .NET 8.0 SDK
- Windows OS (WPF dependency)

## Pull Request Process

1. Fork the repo and create your branch from `main`.
2. If you've added code that should be tested, add tests to `AICommander.Tests`.
3. Ensure the test suite passes (`dotnet test`).
4. Make sure your code lints and builds correctly (`dotnet build`).

## Architecture and Guidelines

Please review our `AGENTS.md` and the `.agents/skills/` directory for architectural rules.
- Maintain decoupled architecture (No UI dependencies in `AICommander.Core`).
- Follow `wpf-best-practices` and `dotnet-best-practices`.
- If you are adding a new Provider or Action, follow the instructions in the `add-provider` and `add-action` skills respectively.
