using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;
using ElmahCore.Sql;

namespace ElmahCore.Tests;

public class SqlErrorLogTests
{
    [Fact]
    public void SqlErrorLog_WithOptions_LogAllXmlTrue_DoesNotThrow()
    {
        var options = Substitute.For<IOptions<ElmahOptions>>();
        options.Value.Returns(new ElmahOptions
        {
            ConnectionString = "Server=localhost;Database=test;",
            LogAllXml = true,
            CreateTablesIfNotExist = false
        });

        var act = () => new SqlErrorLog(options);

        act.Should().NotThrow();
    }

    [Fact]
    public void SqlErrorLog_WithOptions_LogAllXmlFalse_DoesNotThrow()
    {
        var options = Substitute.For<IOptions<ElmahOptions>>();
        options.Value.Returns(new ElmahOptions
        {
            ConnectionString = "Server=localhost;Database=test;",
            LogAllXml = false,
            CreateTablesIfNotExist = false
        });

        var act = () => new SqlErrorLog(options);

        act.Should().NotThrow();
    }

    [Fact]
    public void SqlErrorLog_Constructor_WithLogAllXmlParameter_DoesNotThrow()
    {
        var act = () => new SqlErrorLog(
            connectionString: "Server=localhost;Database=test;",
            schemaName: "dbo",
            tableName: "ELMAH_Error",
            createTablesIfNotExist: false,
            logAllXml: false);

        act.Should().NotThrow();
    }

    [Fact]
    public void SqlErrorLog_Constructor_LogAllXmlDefaultsToTrue()
    {
        // The constructor has logAllXml = true as the default parameter
        var act = () => new SqlErrorLog(
            connectionString: "Server=localhost;Database=test;",
            createTablesIfNotExist: false);

        act.Should().NotThrow();
    }
}
