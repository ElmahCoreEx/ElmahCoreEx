using System;
using System.Threading.Tasks;
using AwesomeAssertions;
using ElmahCore.Sql;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Xunit;

namespace ElmahCore.IntegrationTests.MsSql;

/// <summary>
/// SQL Server specific behavior: table creation, schema and table names, and the columns written.
/// </summary>
[Collection(MsSqlCollection.Name)]
public sealed class SqlErrorLogTests(MsSqlFixture fixture)
{
    private const string TableCountSql = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES";

    [Fact]
    public async Task SqlErrorLog_NewDatabase_CreatesDefaultTable()
    {
        var connectionString = await fixture.CreateUniqueDatabaseAsync();

        _ = new SqlErrorLog(connectionString);

        (await TableExistsAsync(connectionString, "dbo", "ELMAH_Error")).Should().BeTrue();
    }

    [Fact]
    public async Task SqlErrorLog_TableAlreadyExists_KeepsExistingErrors()
    {
        var connectionString = await fixture.CreateUniqueDatabaseAsync();
        var first = new SqlErrorLog(connectionString) { ApplicationName = "app" };
        var id = first.Log(NewError("kept"));

        var second = new SqlErrorLog(connectionString) { ApplicationName = "app" };

        second.GetError(id).Should().NotBeNull();
    }

    [Fact]
    public async Task SqlErrorLog_CreateTablesDisabled_DoesNotCreateTable()
    {
        var connectionString = await fixture.CreateUniqueDatabaseAsync();

        _ = new SqlErrorLog(connectionString, createTablesIfNotExist: false);

        (await MsSqlFixture.ScalarAsync<int>(connectionString, TableCountSql)).Should().Be(0);
    }

    [Fact]
    public async Task SqlErrorLog_CustomSchemaAndTable_LogsToThatTable()
    {
        var connectionString = await fixture.CreateUniqueDatabaseAsync();
        await MsSqlFixture.ExecuteAsync(connectionString, "CREATE SCHEMA [logging]");
        var log = new SqlErrorLog(connectionString, "logging", "AppErrors") { ApplicationName = "app" };

        var id = log.Log(NewError("custom table"));

        log.GetError(id).Should().NotBeNull();
        (await MsSqlFixture.ScalarAsync<int>(connectionString, "SELECT COUNT(*) FROM [logging].[AppErrors]"))
            .Should().Be(1);
        (await TableExistsAsync(connectionString, "dbo", "ELMAH_Error")).Should().BeFalse();
    }

    [Fact]
    public async Task SqlErrorLog_OptionsConstructor_UsesConfiguredSchemaTableAndLogAllXml()
    {
        var connectionString = await fixture.CreateUniqueDatabaseAsync();
        await MsSqlFixture.ExecuteAsync(connectionString, "CREATE SCHEMA [logging]");
        var options = Options.Create(new ElmahOptions
        {
            ConnectionString = connectionString,
            SqlServerDatabaseSchemaName = "logging",
            SqlServerDatabaseTableName = "OptionErrors",
            LogAllXml = false
        });
        var log = new SqlErrorLog(options) { ApplicationName = "app" };

        var id = log.Log(NewError("from options"));

        (await TableExistsAsync(connectionString, "logging", "OptionErrors")).Should().BeTrue();
        log.GetError(id).Error.Message.Should().Be("AllXml logging disabled");
    }

    [Theory]
    [InlineData("dbo", "Errors]; DROP TABLE [dbo].[Other];--")]
    [InlineData("dbo]; DROP TABLE [dbo].[Other];--", "ELMAH_Error")]
    public async Task SqlErrorLog_InjectionInSchemaOrTableName_ThrowsWithoutTouchingDatabase(string schema, string table)
    {
        var connectionString = await fixture.CreateUniqueDatabaseAsync();
        await MsSqlFixture.ExecuteAsync(connectionString, "CREATE TABLE [dbo].[Other] (Id INT)");

        var act = () => new SqlErrorLog(connectionString, schema, table);

        act.Should().Throw<ArgumentException>();
        (await MsSqlFixture.ScalarAsync<int>(connectionString, TableCountSql)).Should().Be(1);
    }

    [Fact]
    public async Task SqlErrorLog_Log_WritesQueryableColumns()
    {
        var log = new SqlErrorLog(fixture.ConnectionString) { ApplicationName = $"it-{Guid.NewGuid():N}" };
        var time = new DateTime(2026, 9, 19, 10, 30, 15, DateTimeKind.Utc);

        var id = log.Log(new Error
        {
            HostName = "web-01",
            Type = "System.TimeoutException",
            Source = "Orders",
            Message = "Timed out",
            User = "bob",
            StatusCode = 504,
            Time = time
        });

        await using var connection = new SqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(
            "SELECT Application, Host, Type, Source, Message, [User], StatusCode, TimeUtc " +
            "FROM [dbo].[ELMAH_Error] WHERE ErrorId = @id", connection);
        command.Parameters.AddWithValue("@id", Guid.Parse(id));
        await using var reader = await command.ExecuteReaderAsync();

        (await reader.ReadAsync()).Should().BeTrue();
        reader.GetString(0).Should().Be(log.ApplicationName);
        reader.GetString(1).Should().Be("web-01");
        reader.GetString(2).Should().Be("System.TimeoutException");
        reader.GetString(3).Should().Be("Orders");
        reader.GetString(4).Should().Be("Timed out");
        reader.GetString(5).Should().Be("bob");
        reader.GetInt32(6).Should().Be(504);
        reader.GetDateTime(7).Should().Be(time);
    }

    private static Error NewError(string message) => new() { Message = message, Time = DateTime.UtcNow };

    private static async Task<bool> TableExistsAsync(string connectionString, string schema, string table) =>
        await MsSqlFixture.ScalarAsync<int>(connectionString,
            TableCountSql + " WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @table",
            new SqlParameter("@schema", schema), new SqlParameter("@table", table)) == 1;
}
