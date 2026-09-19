# Release Notes

## 3.0.0

### Summary

Move the SQL Server, MySQL and PostgreSQL error logs onto a shared `RelationalErrorLog` base class with true async
database calls. Add `IErrorLogger` for logging errors through dependency injection. Fix several bugs in request body
capture and update dependencies.

This is a major release because the database error log packages have breaking API and behavior changes. The
`ElmahCoreEx` and `ElmahCoreEx.Common` packages have no breaking API changes.

### Breaking changes

- `SqlErrorLog`, `MySqlErrorLog` and `PgsqlErrorLog` now inherit from `RelationalErrorLog` instead of `ErrorLog`.
- `ConnectionString` is no longer `virtual` on these three classes. On `SqlErrorLog` and `PgsqlErrorLog` it is also
  no longer `public`; it is now `protected`.
- Remove the public `ElmahCore.MySql.CommandExtension` class.
- `SqlErrorLog` now throws `ArgumentException` if the schema or table name is not a plain identifier (letters, digits
  and `_`, and not starting with a digit). Before, any name was accepted inside `[...]`.
- The error log no longer captures request bodies larger than `ElmahOptions.MaxRequestBodySize` (default 1 MB).
  Set it to `0` to capture bodies of any size, as before.
- Consuming apps now get major version updates of database drivers: Microsoft.Data.SqlClient 7.x (was 6.x) and
  MySql.Data 26.x (was 9.x). Read the release notes of these drivers before you upgrade.
- Remove the NetJSON package dependency. If your app used NetJSON through ElmahCore, add a direct reference to it.

### Features

- Add `IErrorLogger` so you can log errors through dependency injection. `AddElmah` registers it. (#14)
- Add `ElmahOptions.MaxRequestBodySize` to limit the size of request bodies that the error log captures. (#14)
- Database error logs now implement `LogAsync`, `GetErrorAsync` and `GetErrorsAsync` with async ADO.NET calls
  instead of wrapping the sync methods. (#14)
- `PgsqlErrorLog(string connectionString)`: `createTablesIfNotExist` now defaults to `true`, the same as the other
  database error logs. (#14)

### Deprecations

- The static `ElmahExtensions.RaiseError` methods and `ElmahExtensions.LogMiddleware` are obsolete. Use `IErrorLogger`.
- The APM methods on `ErrorLog` (`BeginLog`/`EndLog` and similar) are obsolete. Use the async methods.

### Fixes

- Fix the content type check in `ErrorLogMiddleware`, which compared the value to itself. (#14)
- Fix a `NullReferenceException` when no notifiers are configured. (#14)
- Fix a race condition in the lazy initialization of `Error` collections. (#14)
- `Error.HostName` falls back to `Environment.MachineName`, so it is no longer null on Linux. (#14)
- Pass the exception to the logger when the filters XML cannot be read. (#14)
- Don't crash when the request body is truncated. (#15)
- Don't log an error when the request body read fails because the client disconnected. (#17)

### Security

- Validate the `SqlErrorLog` schema and table names to prevent SQL injection. (#14)
- Update Microsoft.SourceLink.GitHub to 10.0.401 to fix NU1902 (GHSA-23fw-v26w-5fgq). This is a build-time
  dependency only. (#14)

### Maintenance

- Use System.Text.Json for all JSON serialization. (#14)
- Tests: replace FluentAssertions with AwesomeAssertions. (#14)
- Add release drafter and PR auto-label workflows. (#18)
- Update NuGet packages. (#19)

## 2.1.6

### Changes

- Add `LogAllXml` option to disable full XML logging in database error logs (#11)
- Add optional `statusCode` parameter to `RaiseError()` methods for custom HTTP status codes (#13)

## 2.1.5

### Summary

Migrate all projects to .NET 10, update dependencies and adjust solution structure

### Changes

- Migrate to net10
- Update dependencies

## 2.1.4-beta 1

## Summary

### Breaking changes

- Upgrade to net8 dependencies

### Maintenance

- Bump packages to current releases

### Fixes

- Fix minor concurrency bug.


## 2.1.3 -- Changes from ElmahCore 2.1.2

### Summary

Drop in replacement for existing ElmahCore with collection of outstanding PR's merged in.

### Changes

- Fix to improve memory usage with large Serialization payload (#4)

#### 2.1.3-alpha.2

- refactor: Refactoring to statics, parser.
- refactor: Drop Dictionary cache (No LRU)
- perf: Set regex to compiled.
- chore: Bump HtmlAgility to 1.11.48, system.Text.Json 6.0.8
- fix: UserAgent < 3 characters will not throw exception. [#60](https://github.com/ElmahCore/ElmahCore/issues/168)


#### 2.1.3-alpha.1

- Dropped support for net core 3.1, 5.0 (unsupported from Microsoft)
- Tests run under .NET6 [#162](https://github.com/ElmahCore/ElmahCore/pull/162)
- Update other package dependencies

### Fixes

- Increase size of MySQL data length for Message to TEXT and AllXML [#1]
- Updated ELMAHCore to use `Microsoft.Data.SqlClient` [#157](https://github.com/ElmahCore/ElmahCore/pull/163)
- Fix a stack trace high CPU usage bug [#158](https://github.com/ElmahCore/ElmahCore/pull/164)
- Make ErrorTextFormatter public [#165](https://github.com/ElmahCore/ElmahCore/pull/165)