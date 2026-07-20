# Contributing to AI Commander

First off, thank you for considering contributing to AI Commander. It's people like you that make AI Commander such a great tool.

## Where do I go from here?

If you've noticed a bug or have a feature request, make one! It's generally best if you get confirmation of your bug or approval for your feature request this way before starting to code.

## Development Setup

The project uses C# .NET 8 WPF. You will need a Windows environment to run and build the application.

### Prerequisites
- .NET 8.0 SDK
- Windows OS (WPF dependency)

### Local Tooling Setup
Run the following commands to install dev tools and activate git hooks:
```bash
dotnet tool restore
dotnet husky install
```
This installs Husky.Net and CommitLint.Net, then wires the `pre-commit` and `commit-msg` hooks so formatting and commit messages are checked locally.

### Code quality gates
- **Format**: SDK `dotnet format` driven by [`.editorconfig`](.editorconfig). Locally via the pre-commit hook; also `dotnet format AICommander.sln --verify-no-changes --severity error` in CI (whitespace/style; analyzer warnings do not fail the gate yet).
- **Analyzers**: `EnableNETAnalyzers` + `EnforceCodeStyleInBuild` in [`Directory.Build.props`](Directory.Build.props) (warnings, not `TreatWarningsAsErrors`).
- **Commits**: Conventional Commits via CommitLint.Net (`commit-message-config.json`, subject max 90 chars).
- **Tests**: `dotnet test AICommander.sln` before opening a PR.

## Commit Messages & Workflow

We follow [Conventional Commits](https://www.conventionalcommits.org/), enforced by CommitLint.Net (`commit-message-config.json`).

For the full message format, types/scopes, and step-by-step commit process (including agent `/commit`), see [`.agent/workflows/commit.md`](.agent/workflows/commit.md).

## Branch Strategy & Pull Request Process

We use a `develop` & `main` branch strategy:
- `develop` is the integration branch.
- `main` is reserved for releases.

1. Fork the repo and create your feature branch from `develop`.
2. Use Conventional Commits (see [`.agent/workflows/commit.md`](.agent/workflows/commit.md)).
3. If you've added code that should be tested, add tests to `AICommander.Tests`.
4. Ensure the test suite passes (`dotnet test`).
5. Open a Pull Request targeting the `develop` branch.

## Architecture and Guidelines

Please review [`.agents/AGENTS.md`](.agents/AGENTS.md) and the `.agents/skills/` directory for architectural rules.
- Maintain decoupled architecture (No UI dependencies in `AICommander.Core`).
- Follow `wpf-best-practices` and `dotnet-best-practices`.
- If you are adding a new Provider or Action, follow the instructions in the `add-provider` and `add-action` skills respectively.
