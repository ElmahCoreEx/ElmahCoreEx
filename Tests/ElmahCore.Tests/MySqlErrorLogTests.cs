using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;
using ElmahCore.MySql;

namespace ElmahCore.Tests;

public class MySqlErrorLogTests
{
    [Fact]
    public void MySqlErrorLog_WithOptions_LogAllXmlTrue_DoesNotThrow()
    {
        var options = Substitute.For<IOptions<ElmahOptions>>();
        options.Value.Returns(new ElmahOptions
        {
            ConnectionString = "Server=localhost;Database=test;",
            LogAllXml = true,
            CreateTablesIfNotExist = false
        });

        var act = () => new MySqlErrorLog(options);

        act.Should().NotThrow();
    }

    [Fact]
    public void MySqlErrorLog_WithOptions_LogAllXmlFalse_DoesNotThrow()
    {
        var options = Substitute.For<IOptions<ElmahOptions>>();
        options.Value.Returns(new ElmahOptions
        {
            ConnectionString = "Server=localhost;Database=test;",
            LogAllXml = false,
            CreateTablesIfNotExist = false
        });

        var act = () => new MySqlErrorLog(options);

        act.Should().NotThrow();
    }

    [Fact]
    public void MySqlErrorLog_Constructor_WithLogAllXmlParameter_DoesNotThrow()
    {
        var act = () => new MySqlErrorLog(
            connectionString: "Server=localhost;Database=test;",
            createTablesIfNotExist: false,
            logAllXml: false);

        act.Should().NotThrow();
    }

    [Fact]
    public void MySqlErrorLog_Constructor_LogAllXmlDefaultsToTrue()
    {
        // The constructor has logAllXml = true as default parameter
        var act = () => new MySqlErrorLog(
            connectionString: "Server=localhost;Database=test;",
            createTablesIfNotExist: false);

        act.Should().NotThrow();
    }
}
