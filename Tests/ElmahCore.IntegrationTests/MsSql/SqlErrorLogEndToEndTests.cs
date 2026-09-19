using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AwesomeAssertions;
using ElmahCore.Mvc;
using ElmahCore.Sql;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Xunit;

namespace ElmahCore.IntegrationTests.MsSql;

/// <summary>
/// Runs an app configured the way the README shows, so the options → DI → middleware → SQL Server path is covered.
/// </summary>
[Collection(MsSqlCollection.Name)]
public sealed class SqlErrorLogEndToEndTests(MsSqlFixture fixture)
{
    [Fact]
    public async Task SqlErrorLog_UnhandledExceptionInRequest_IsStoredAndListedByElmahApi()
    {
        var applicationName = $"it-{Guid.NewGuid():N}";
        await using var app = await StartAppAsync(fixture.ConnectionString, applicationName);
        var client = app.GetTestClient();

        var request = () => client.GetAsync("/boom");
        await request.Should().ThrowAsync<InvalidOperationException>().WithMessage("Boom from the app");

        var rows = await MsSqlFixture.ScalarAsync<int>(fixture.ConnectionString,
            "SELECT COUNT(*) FROM [dbo].[ELMAH_Error] WHERE Application = @app AND Type = @type",
            new SqlParameter("@app", applicationName),
            new SqlParameter("@type", typeof(InvalidOperationException).FullName));
        rows.Should().Be(1);

        using var json = JsonDocument.Parse(await client.GetStringAsync("/elmah/api/errors"));
        json.RootElement.GetProperty("totalCount").GetInt32().Should().Be(1);
        var error = json.RootElement.GetProperty("errors").EnumerateArray().Single().GetProperty("error");
        error.GetProperty("message").GetString().Should().Be("Boom from the app");
        error.GetProperty("type").GetString().Should().Be(typeof(InvalidOperationException).FullName);
    }

    private static async Task<WebApplication> StartAppAsync(string connectionString, string applicationName)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddElmah<SqlErrorLog>(options =>
        {
            options.ConnectionString = connectionString;
            options.ApplicationName = applicationName;
        });

        var app = builder.Build();
        app.UseElmah();
        app.MapGet("/boom", (HttpContext _) => throw new InvalidOperationException("Boom from the app"));

        await app.StartAsync();
        return app;
    }
}
