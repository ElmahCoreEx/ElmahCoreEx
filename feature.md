# ElmahCore Code Review & Improvement Plan

> **Review Date:** 2026-02-12
> **Reviewer:** Senior Staff Engineer
> **Scope:** Architecture, Security, Performance, Maintainability

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                        ASP.NET Core Application                      │
└─────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────┐
│                      ErrorLogMiddleware                              │
│  - Intercepts exceptions and HTTP 4xx/5xx errors                     │
│  - Captures request body, logs, SQL queries, parameters              │
│  - Routes ~/elmah/* requests to handlers                             │
└─────────────────────────────────────────────────────────────────────┘
           │                    │                        │
           ▼                    ▼                        ▼
┌──────────────────┐  ┌──────────────────┐  ┌──────────────────────────┐
│  IErrorFilter    │  │  IErrorNotifier  │  │      ErrorLog (Base)     │
│  - Filter errors │  │  - Email/webhook │  │  - MemoryErrorLog        │
│  - XML config    │  │  - Notifications │  │  - SqlErrorLog (MSSQL)   │
└──────────────────┘  └──────────────────┘  │  - MySqlErrorLog         │
                                            │  - PgsqlErrorLog         │
                                            │  - XmlFileErrorLog       │
                                            └──────────────────────────┘
                                                        │
                                                        ▼
┌─────────────────────────────────────────────────────────────────────┐
│                           Web UI (Vue.js SPA)                        │
│  - ErrorApiHandler serves /api/errors, /api/error                    │
│  - ErrorResourceHandler serves embedded static assets                │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Actionable Checklist

### High Priority - Security & Reliability

- [x] **SEC-001: Fix SQL Injection in Table/Schema Names**
  - **Files:** `ElmahCore.MsSql/SqlErrorLog.cs:241-254`
  - **Issue:** Schema and table names interpolated directly into SQL
  - **Action:** Add identifier validation regex or use parameterized approach
  - **Status:** ✓ Completed - Added SqlIdentifierValidator class with regex validation. SqlErrorLog validates schema/table names in constructor.
  - **Example Fix:**
    ```csharp
    private static readonly Regex SafeIdentifier = new(@"^[a-zA-Z_][a-zA-Z0-9_]{0,127}$");

    private static void ValidateIdentifier(string value, string paramName)
    {
        if (!SafeIdentifier.IsMatch(value))
            throw new ArgumentException($"Invalid SQL identifier: {value}", paramName);
    }
    ```

- [ ] **REL-001: Add Logging for Silent Exception Catches**
  - **Files:**
    - `ElmahCore.MsSql/SqlErrorLog.cs:92-97`
    - `ElmahCore.Mvc/ElmahExtensions.cs:122-125`
  - **Issue:** Empty catch blocks hide critical failures
  - **Action:** Add `Trace.TraceError` or fallback logging
  - **Example Fix:**
    ```csharp
    catch (Exception ex)
    {
        Trace.TraceError($"ELMAH: Failed to log error: {ex.Message}");
        Debug.WriteLine($"ELMAH: {ex}");
    }
    ```

- [ ] **SEC-002: Add Sensitive Data Filtering**
  - **Files:** `ElmahCore/Error.cs:104-115`, `ElmahCore/ElmahOptions.cs`
  - **Issue:** Passwords, tokens, session IDs captured in error logs
  - **Action:** Add configurable sensitive field masking
  - **Example Fix:**
    ```csharp
    // In ElmahOptions.cs
    public string[] SensitiveFormFields { get; set; } =
        ["password", "pwd", "secret", "token", "api_key", "credit_card"];

    public string[] SensitiveCookies { get; set; } =
        [".aspnetcore.session", "auth_token"];
    ```

- [ ] **REL-002: Add Data Retention Policy**
  - **Files:** All database implementations, `ElmahCore/ElmahOptions.cs`
  - **Issue:** Errors accumulate indefinitely causing DB/disk bloat
  - **Action:** Add `ErrorRetention` and `MaxErrorCount` options with cleanup job
  - **Example Fix:**
    ```csharp
    // In ElmahOptions.cs
    public TimeSpan? ErrorRetention { get; set; }
    public int? MaxErrorCount { get; set; }
    ```

---

### Medium Priority - Performance & Architecture

- [x] **PERF-001: Add Request Body Size Limits**
  - **File:** `ElmahCore.Mvc/ErrorLogMiddleware.cs:198-210`
  - **Issue:** No limit on body size, potential memory exhaustion
  - **Action:** Add configurable `MaxRequestBodySize` with default 1MB
  - **Status:** ✓ Completed - Added MaxRequestBodySize option (default 1MB) to ElmahOptions and body size checking in GetBody
  - **Example Fix:**
    ```csharp
    private const int MaxBodySize = 1024 * 1024; // 1MB

    if (!request.ContentLength.HasValue || request.ContentLength > MaxBodySize)
        return "[Body too large or unknown size]";
    ```

- [x] **ARCH-001: Replace Static Service Locator Pattern**
  - **Files:**
    - `ElmahCore.Mvc/ElmahExtensions.cs:14`
    - `ElmahCore.Mvc/ErrorLogMiddleware.cs:54`
  - **Issue:** Static `LogMiddleware` field breaks DI, testing, and multi-tenancy
  - **Action:** Create `IErrorLogger` interface for DI-based access
  - **Status:** ✓ Completed - Added IErrorLogger interface and ErrorLoggerService. Static methods deprecated but maintained for backward compatibility.
  - **Example Fix:**
    ```csharp
    public interface IErrorLogger
    {
        Task LogAsync(Exception ex, HttpContext context = null);
        Task LogAsync(Exception ex, int statusCode, HttpContext context = null);
    }
    ```

- [x] **PERF-002: Fix Thread Safety in Error Class**
  - **File:** `ElmahCore/Error.cs:462-465`
  - **Issue:** `FaultIn` method not thread-safe for lazy initialization
  - **Action:** Use `Interlocked.CompareExchange` or `Lazy<T>`
  - **Status:** ✓ Completed - FaultIn now uses Interlocked.CompareExchange for thread-safe lazy initialization
  - **Example Fix:**
    ```csharp
    private static NameValueCollection GetOrCreate(ref NameValueCollection location)
    {
        if (location != null) return location;
        var newCollection = new NameValueCollection();
        return Interlocked.CompareExchange(ref location, newCollection, null) ?? newCollection;
    }
    ```

- [ ] **PERF-003: Add Limit to GetNewErrorsAsync**
  - **File:** `ElmahCore.Mvc/Handlers/ErrorApiHandler.cs:70-83`
  - **Issue:** No limit on returned errors, potential memory exhaustion
  - **Action:** Add configurable limit (default 1000)

- [x] **MAINT-001: Standardize JSON Serializer**
  - **File:** `ElmahCore.Mvc/Handlers/ErrorApiHandler.cs:22,29,36`
  - **Issue:** Mixed use of `System.Text.Json` and `NetJSON`
  - **Action:** Standardize on `System.Text.Json` throughout
  - **Status:** ✓ Completed - Removed NetJSON dependency, standardized on System.Text.Json

---

### Low Priority - Code Quality & Maintainability

- [x] **MAINT-002: Extract Common Database Logic**
  - **Files:**
    - `ElmahCore.MsSql/SqlErrorLog.cs`
    - `ElmahCore.MySql/MySqlErrorLog.cs`
    - `ElmahCore.Postgresql/PgsqlErrorLog.cs`
  - **Issue:** ~80% code duplication across implementations
  - **Action:** Create `RelationalErrorLog` base class
  - **Status:** ✓ Completed - Created RelationalErrorLog base class with common Log, GetError, GetErrors, and async implementations. Database-specific classes now only implement connection creation and command factories.
  - **Example Fix:**
    ```csharp
    public abstract class RelationalErrorLog : ErrorLog
    {
        protected abstract DbConnection CreateConnection();
        protected abstract DbCommand CreateInsertCommand(DbConnection conn, Guid id, Error error, string xml);

        public override void Log(Guid id, Error error)
        {
            var xml = SerializeError(error);
            using var conn = CreateConnection();
            using var cmd = CreateInsertCommand(conn, id, error, xml);
            conn.Open();
            cmd.ExecuteNonQuery();
        }
    }
    ```

- [x] **MAINT-003: Add Proper Async Implementations**
  - **File:** `ElmahCore/ErrorLog.cs` and all implementations
  - **Issue:** Async methods wrap sync calls with `Task.Run`
  - **Action:** Implement true async with `*Async` ADO.NET methods
  - **Status:** ✓ Completed - Added true async implementations to SqlErrorLog, MySqlErrorLog, and PgsqlErrorLog
  - **Example Fix:**
    ```csharp
    public override async Task<string> LogAsync(Error error, CancellationToken ct = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(ct);
        await using var command = CreateInsertCommand(connection, error);
        await command.ExecuteNonQueryAsync(ct);
        return id.ToString();
    }
    ```

- [ ] **TEST-001: Add XML Round-Trip Tests**
  - **File:** `Tests/ElmahCore.Tests/ErrorXmlTests.cs` (new)
  - **Issue:** No tests for ErrorXml encode/decode fidelity
  - **Action:** Add tests for special characters, large payloads, edge cases

- [ ] **TEST-002: Add Error Filter Tests**
  - **File:** `Tests/ElmahCore.Mvc.Tests/ErrorFilterTests.cs` (new)
  - **Issue:** No tests for IErrorFilter implementations
  - **Action:** Add tests for filter chain, XML config parsing, dismissal

- [ ] **TEST-003: Add Thread Safety Tests**
  - **File:** `Tests/ElmahCore.Tests/ConcurrencyTests.cs` (new)
  - **Issue:** No tests for concurrent access scenarios
  - **Action:** Add tests for MemoryErrorLog concurrent writes, Error property access

- [ ] **TEST-004: Add API Handler Integration Tests**
  - **File:** `Tests/ElmahCore.Mvc.Tests/ErrorApiHandlerTests.cs` (new)
  - **Issue:** No tests for `/api/*` endpoints
  - **Action:** Add tests using `WebApplicationFactory`

- [ ] **SEC-003: Audit Vue Components for XSS**
  - **Files:** `ElmahCore.Mvc/wwwroot/js/*.js`
  - **Issue:** Ensure no `v-html` with untrusted error data
  - **Action:** Review and document frontend security

- [ ] **SEC-004: Add Path Traversal Protection for XmlFileErrorLog**
  - **File:** `ElmahCore/Logs/XmlFileErrorLog.cs`
  - **Issue:** Potential path traversal if LogPath is manipulated
  - **Action:** Canonicalize and validate paths against allowed base

---

## Priority Matrix

| ID | Issue | Impact | Effort | Priority |
|----|-------|--------|--------|----------|
| SEC-001 | SQL injection in table names | Critical | Low | **P0** |
| REL-001 | Silent exception swallowing | High | Low | **P0** |
| SEC-002 | Sensitive data exposure | High | Medium | **P1** |
| REL-002 | No data retention | High | Medium | **P1** |
| PERF-001 | Request body limits | Medium | Low | **P1** |
| ARCH-001 | Static service locator | Medium | High | **P2** |
| PERF-002 | Thread safety | Medium | Low | **P2** |
| PERF-003 | Unbounded GetNewErrors | Medium | Low | **P2** |
| MAINT-001 | Mixed JSON serializers | Low | Low | **P2** |
| MAINT-002 | Code duplication | Low | Medium | **P3** |
| MAINT-003 | Async implementations | Low | Medium | **P3** |
| TEST-* | Testing gaps | Low | Medium | **P3** |
| SEC-003 | XSS audit | Low | Low | **P3** |
| SEC-004 | Path traversal | Low | Low | **P3** |

---

## Implementation Notes

### Getting Started

1. **Start with P0 items** - These are quick wins with high impact
2. **Create a feature branch** for each major change
3. **Add tests alongside fixes** - Don't add technical debt
4. **Update CHANGELOG.md** for each fix

### Breaking Changes

The following items may introduce breaking changes:

- **ARCH-001** (Static service locator removal) - Changes public API for `ElmahExtensions.RaiseError()`
- **MAINT-002** (Database base class) - May affect custom ErrorLog implementations

Consider a major version bump if implementing these.

### Dependencies to Add

For proper async database support:
```xml
<!-- Already present, but ensure latest versions -->
<PackageReference Include="Microsoft.Data.SqlClient" Version="5.x" />
<PackageReference Include="Npgsql" Version="8.x" />
<PackageReference Include="MySql.Data" Version="8.x" />
```

---

## Changelog Template

```markdown
## [Unreleased]

### Security
- Fixed SQL injection vulnerability in table/schema name handling (SEC-001)
- Added sensitive data filtering for passwords and tokens (SEC-002)

### Fixed
- Added logging for previously silent exception catches (REL-001)
- Fixed thread safety issue in Error property accessors (PERF-002)

### Added
- Data retention policy with configurable `ErrorRetention` option (REL-002)
- Request body size limit with `MaxRequestBodySize` option (PERF-001)

### Changed
- Standardized on System.Text.Json for all JSON serialization (MAINT-001)
```
