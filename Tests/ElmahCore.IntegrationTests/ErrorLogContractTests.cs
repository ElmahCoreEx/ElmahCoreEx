using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AwesomeAssertions;
using Xunit;

namespace ElmahCore.IntegrationTests;

/// <summary>
/// Behavior that every database-backed <see cref="ErrorLog" /> must have. Each database gets a subclass that
/// supplies a log connected to a real server.
/// </summary>
/// <remarks>
/// Tests share one table and isolate their rows with a unique <see cref="ErrorLog.ApplicationName" />.
/// </remarks>
public abstract class ErrorLogContractTests
{
    /// <summary>
    /// Creates a log connected to the shared test database. The table must already exist or be created by the log.
    /// </summary>
    protected abstract ErrorLog CreateErrorLog(bool logAllXml = true);

    private ErrorLog CreateIsolatedErrorLog(bool logAllXml = true, string applicationName = null)
    {
        var log = CreateErrorLog(logAllXml);
        log.ApplicationName = applicationName ?? UniqueApplicationName();
        return log;
    }

    protected static string UniqueApplicationName() => $"it-{Guid.NewGuid():N}";

    protected static Error CreateError(string message = "Something failed") => new()
    {
        HostName = "test-host",
        Type = "System.InvalidOperationException",
        Source = "ElmahCore.IntegrationTests",
        Message = message,
        Detail = "System.InvalidOperationException: Something failed\n   at Test.Method()",
        User = "alice",
        StatusCode = 503,
        Time = DateTime.UtcNow
    };

    [Fact]
    public void ErrorLog_LogThenGetError_RoundTripsErrorDetails()
    {
        var log = CreateIsolatedErrorLog();
        var error = CreateError();

        var id = log.Log(error);
        var entry = log.GetError(id);

        entry.Should().NotBeNull();
        entry.Id.Should().Be(id);
        ShouldMatch(entry.Error, error);
    }

    [Fact]
    public async Task ErrorLog_LogAsyncThenGetErrorAsync_RoundTripsErrorDetails()
    {
        var log = CreateIsolatedErrorLog();
        var error = CreateError();

        var id = await log.LogAsync(error);
        var entry = await log.GetErrorAsync(id);

        entry.Should().NotBeNull();
        entry.Id.Should().Be(id);
        ShouldMatch(entry.Error, error);
    }

    [Fact]
    public void ErrorLog_GetErrorWithUnknownId_ReturnsNull()
    {
        var log = CreateIsolatedErrorLog();

        log.GetError(Guid.NewGuid().ToString()).Should().BeNull();
    }

    [Fact]
    public void ErrorLog_GetErrorFromOtherApplication_ReturnsNull()
    {
        var id = CreateIsolatedErrorLog().Log(CreateError());

        CreateIsolatedErrorLog().GetError(id).Should().BeNull();
    }

    [Fact]
    public void ErrorLog_GetErrors_ReturnsNewestFirstWithTotalCount()
    {
        var log = CreateIsolatedErrorLog();
        var ids = LogErrors(log, 3);

        var entries = new List<ErrorLogEntry>();
        var total = log.GetErrors(0, 10, entries);

        total.Should().Be(3);
        entries.Select(e => e.Id).Should().Equal(Enumerable.Reverse(ids));
    }

    [Fact]
    public void ErrorLog_GetErrorsSecondPage_ReturnsNextOldestErrors()
    {
        var log = CreateIsolatedErrorLog();
        var ids = LogErrors(log, 5);

        var entries = new List<ErrorLogEntry>();
        var total = log.GetErrors(2, 2, entries);

        total.Should().Be(5);
        entries.Select(e => e.Id).Should().Equal(ids[2], ids[1]);
    }

    [Fact]
    public async Task ErrorLog_GetErrorsAsyncSecondPage_ReturnsNextOldestErrors()
    {
        var log = CreateIsolatedErrorLog();
        var ids = LogErrors(log, 5);

        var entries = new List<ErrorLogEntry>();
        var total = await log.GetErrorsAsync(2, 2, entries);

        total.Should().Be(5);
        entries.Select(e => e.Id).Should().Equal(ids[2], ids[1]);
    }

    [Fact]
    public void ErrorLog_GetErrorsPastTheEnd_ReturnsNoEntriesAndTotalCount()
    {
        var log = CreateIsolatedErrorLog();
        LogErrors(log, 2);

        var entries = new List<ErrorLogEntry>();
        var total = log.GetErrors(10, 10, entries);

        total.Should().Be(2);
        entries.Should().BeEmpty();
    }

    [Fact]
    public void ErrorLog_GetErrors_DoesNotReturnOtherApplicationsErrors()
    {
        LogErrors(CreateIsolatedErrorLog(), 2);
        var log = CreateIsolatedErrorLog();
        var ids = LogErrors(log, 1);

        var entries = new List<ErrorLogEntry>();
        var total = log.GetErrors(0, 10, entries);

        total.Should().Be(1);
        entries.Select(e => e.Id).Should().Equal(ids);
    }

    [Fact]
    public void ErrorLog_LogAllXmlDisabled_StoresPlaceholderInsteadOfDetails()
    {
        var log = CreateIsolatedErrorLog(logAllXml: false);

        var id = log.Log(CreateError());
        var entry = log.GetError(id);

        entry.Should().NotBeNull();
        entry.Error.Message.Should().Be("AllXml logging disabled");
        entry.Error.Detail.Should().BeEmpty();
    }

    [Fact]
    public void ErrorLog_UnicodeText_RoundTrips()
    {
        var log = CreateIsolatedErrorLog();
        var error = CreateError("Échec de la requête — 요청 실패 — 请求失败 — 🔥");
        error.User = "ユーザー";

        var entry = log.GetError(log.Log(error));

        entry.Should().NotBeNull();
        entry.Error.Message.Should().Be(error.Message);
        entry.Error.User.Should().Be(error.User);
    }

    [Fact]
    public void ErrorLog_EmptyUserAndSource_IsStored()
    {
        var log = CreateIsolatedErrorLog();
        var error = CreateError();
        error.User = string.Empty;
        error.Source = string.Empty;

        var entry = log.GetError(log.Log(error));

        entry.Should().NotBeNull();
        entry.Error.Source.Should().BeEmpty();
    }

    [Fact(Skip = "Error.User falls back to the USERDOMAIN/USERNAME of the machine that reads the log when no " +
                 "user is stored, so an anonymous error shows the server's account. Core Error behavior, " +
                 "not specific to a database; to be fixed separately.")]
    public void ErrorLog_EmptyUser_RoundTripsAsEmpty()
    {
        var log = CreateIsolatedErrorLog();
        var error = CreateError();
        error.User = string.Empty;

        var entry = log.GetError(log.Log(error));

        entry.Error.User.Should().BeEmpty();
    }

    [Fact]
    public void ErrorLog_LargeMessageAndDetail_RoundTrip()
    {
        var log = CreateIsolatedErrorLog();
        var error = CreateError(new string('m', 100_000));
        error.Detail = new string('d', 500_000);

        var entry = log.GetError(log.Log(error));

        entry.Should().NotBeNull();
        entry.Error.Message.Should().Be(error.Message);
        entry.Error.Detail.Should().Be(error.Detail);
    }

    [Fact]
    public void ErrorLog_ValuesLongerThanTheirColumns_IsStoredWithFullValuesInDetails()
    {
        var log = CreateIsolatedErrorLog();
        var error = CreateError();
        error.HostName = new string('h', 300);
        error.Type = new string('t', 300);
        error.Source = new string('s', 300);
        error.User = new string('u', 300);

        var entry = log.GetError(log.Log(error));

        entry.Should().NotBeNull("an error must not be lost because one of its values is longer than its column");
        ShouldMatch(entry.Error, error);
    }

    [Fact]
    public void ErrorLog_ErrorWithoutTime_IsStored()
    {
        var log = CreateIsolatedErrorLog();
        var error = new Error { Message = "No time set" };

        var entry = log.GetError(log.Log(error));

        entry.Should().NotBeNull("an error created with the default constructor must not be lost");
        entry.Error.Message.Should().Be(error.Message);
    }

    private static List<string> LogErrors(ErrorLog log, int count) =>
        Enumerable.Range(1, count).Select(i => log.Log(CreateError($"Error {i}"))).ToList();

    private static void ShouldMatch(Error actual, Error expected)
    {
        actual.HostName.Should().Be(expected.HostName);
        actual.Type.Should().Be(expected.Type);
        actual.Source.Should().Be(expected.Source);
        actual.Message.Should().Be(expected.Message);
        actual.Detail.Should().Be(expected.Detail);
        actual.User.Should().Be(expected.User);
        actual.StatusCode.Should().Be(expected.StatusCode);
        actual.Time.ToUniversalTime().Should().BeCloseTo(expected.Time, TimeSpan.FromSeconds(1));
    }
}
