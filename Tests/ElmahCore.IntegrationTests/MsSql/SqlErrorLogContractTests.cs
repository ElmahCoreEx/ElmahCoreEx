using ElmahCore.Sql;
using Xunit;

namespace ElmahCore.IntegrationTests.MsSql;

[Collection(MsSqlCollection.Name)]
public sealed class SqlErrorLogContractTests(MsSqlFixture fixture) : ErrorLogContractTests
{
    protected override ErrorLog CreateErrorLog(bool logAllXml = true) =>
        new SqlErrorLog(fixture.ConnectionString, logAllXml: logAllXml);
}
