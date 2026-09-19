# ElmahCore

Error Logging Modules and Handlers for ASP.NET Core. A .NET Core port of ELMAH providing centralized error logging, a web-based error browser UI, and support for multiple storage backends.

## Build Commands

```bash
dotnet build              # Build all projects
dotnet test               # Run all tests (integration tests need Docker)
dotnet run --project ElmahCore.Demo  # Run demo app (UI at /elmah)
```

## Project Structure

- `ElmahCore/` - Core library: Error model, ErrorLog base class, XML serialization
- `ElmahCore.Mvc/` - ASP.NET Core integration: middleware, handlers, extensions
- `ElmahCore.MsSql/` - SQL Server error log implementation
- `ElmahCore.MySql/` - MySQL error log implementation
- `ElmahCore.Postgresql/` - PostgreSQL error log implementation
- `ElmahCore.Demo/` - Reference application with usage examples
- `Tests/` - xUnit tests with AwesomeAssertions and NSubstitute

## Key Files

| Task | File |
|------|------|
| Middleware pipeline | `ElmahCore.Mvc/ErrorLogMiddleware.cs` |
| DI registration | `ElmahCore.Mvc/BuilderHelper.cs` |
| Raise errors programmatically | `ElmahCore.Mvc/ElmahExtensions.cs` |
| Configuration options | `ElmahCore/ElmahOptions.cs` |
| Error model | `ElmahCore/Error.cs` |
| Custom error log base | `ElmahCore/ErrorLog.cs` |

## Coding Conventions

- Target framework: .NET 10
- Use file-scoped namespaces
- XML documentation on public APIs
- Async methods suffixed with `Async`
- Extension points: inherit `ErrorLog`, implement `IErrorFilter` or `IErrorNotifier`

## Testing

Tests use xUnit, AwesomeAssertions, and NSubstitute:

```bash
dotnet test Tests/ElmahCore.Tests
dotnet test Tests/ElmahCore.Mvc.Tests
dotnet test Tests/ElmahCore.IntegrationTests   # needs Docker
```

Pattern: `[ClassName]_[Scenario]_[ExpectedResult]`

`Tests/ElmahCore.IntegrationTests` runs the database error logs against real servers with Testcontainers
(SQL Server 2022, one container per test run). Shared behavior is in `ErrorLogContractTests`; to cover another
database, subclass it with that database's log. Tests isolate their rows with a unique `ApplicationName`.
