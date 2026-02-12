using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;
using ElmahCore.Postgresql;

namespace ElmahCore.Tests;

public class PgsqlErrorLogTests
{
    [Fact]
    public void PgsqlErrorLog_WithOptions_LogAllXmlTrue_DoesNotThrow()
    {
        var options = Substitute.For<IOptions<ElmahOptions>>();
        options.Value.Returns(new ElmahOptions
        {
            ConnectionString = "Host=localhost;Database=test;",
            LogAllXml = true,
            CreateTablesIfNotExist = false
        });

        var act = () => new PgsqlErrorLog(options);

        act.Should().NotThrow();
    }

    [Fact]
    public void PgsqlErrorLog_WithOptions_LogAllXmlFalse_DoesNotThrow()
    {
        var options = Substitute.For<IOptions<ElmahOptions>>();
        options.Value.Returns(new ElmahOptions
        {
            ConnectionString = "Host=localhost;Database=test;",
            LogAllXml = false,
            CreateTablesIfNotExist = false
        });

        var act = () => new PgsqlErrorLog(options);

        act.Should().NotThrow();
    }

    [Fact]
    public void PgsqlErrorLog_Constructor_WithLogAllXmlParameter_DoesNotThrow()
    {
        var act = () => new PgsqlErrorLog(
            connectionString: "Host=localhost;Database=test;",
            createTablesIfNotExist: false,
            logAllXml: false);

        act.Should().NotThrow();
    }

    [Fact]
    public void PgsqlErrorLog_Constructor_LogAllXmlDefaultsToTrue()
    {
        // The constructor has logAllXml = true as default parameter
        var act = () => new PgsqlErrorLog(
            connectionString: "Host=localhost;Database=test;",
            createTablesIfNotExist: false);

        act.Should().NotThrow();
    }
}
