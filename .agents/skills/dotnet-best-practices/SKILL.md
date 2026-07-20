---
name: dotnet-best-practices
description: 'Ensure .NET/C# code meets best practices for the solution/project.'
---

# .NET/C# Best Practices

> AI Commander is a .NET 8 WPF/hotkey app. Prefer patterns in `.agents/AGENTS.md`, `wpf-best-practices`, and [docs/testing.md](../../../docs/testing.md) when they conflict with generic advice below.

Your task is to ensure .NET/C# code in ${selection} meets the best practices specific to this solution/project. This includes:

## Documentation & Structure

- Create XML documentation comments for public classes, interfaces, methods, and properties where the project already does so
- Follow the established namespace structure: `AICommander.{App|Core|Tests}.{Feature}`

## Design Patterns & Architecture

- Prefer primary constructors for dependency injection when it matches surrounding code
- Use interface segregation with clear naming conventions (prefix interfaces with `I`)
- Keep `AICommander.Core` free of WPF UI dependencies (see AGENTS.md)

## Dependency Injection & Services

- Use constructor dependency injection with null checks via `ArgumentNullException`
- Register services with appropriate lifetimes (Singleton, Scoped, Transient)
- Use Microsoft.Extensions.DependencyInjection patterns
- Implement service interfaces for testability

## Async/Await Patterns

- Use async/await for I/O operations and long-running tasks
- Return `Task` or `Task<T>` from async methods
- Use `ConfigureAwait(false)` in Core library code where appropriate
- Handle async exceptions properly

## Testing Standards

- Use **xUnit** (`[Fact]` / `[Theory]`) — not MSTest or NUnit
- Use built-in `Assert.*` (no FluentAssertions required)
- Follow AAA (Arrange, Act, Assert)
- Prefer hand-written fakes for `IProvider` over Moq
- Test success and failure / early-return paths for Core behavior
- Follow pragmatic TDD for Core changes: failing test → implement → refactor (see [docs/testing.md](../../../docs/testing.md))

## Configuration & Settings

- Use strongly-typed configuration classes (`AICommanderConfig` and related models)
- Prefer YAML via `ConfigLoader` / `ConfigManager` over ad-hoc parsing
- Keep naming aligned with underscored YAML conventions

## Error Handling & Logging

- Use structured logging with Microsoft.Extensions.Logging
- Include meaningful context in log messages
- Throw specific exceptions with descriptive messages
- Use try-catch for expected failure scenarios

## Performance & Security

- Use C# 12+ features and .NET 8 optimizations where applicable
- Validate external inputs (config paths, key strings)
- Follow secure coding practices for process/window interaction

## Code Quality

- Follow SOLID principles
- Avoid duplication through base classes and utilities (`BaseProvider`, shared parsers)
- Use meaningful names that reflect domain concepts
- Keep methods focused and cohesive
- Implement proper disposal patterns for resources (`IDisposable` for hotkey managers, etc.)
