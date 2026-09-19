using System;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;
using Xunit;

namespace ElmahCore.IntegrationTests.MsSql;

/// <summary>
/// Starts one SQL Server container for every test in the <see cref="MsSqlCollection" />.
/// </summary>
public sealed class MsSqlFixture : IAsyncLifetime
{
    private const string Image = "mcr.microsoft.com/mssql/server:2022-latest";

    private readonly MsSqlContainer _container = new MsSqlBuilder(Image).Build();

    /// <summary>
    /// Connection string for the shared "Elmah" database, where tests isolate their rows by application name.
    /// </summary>
    public string ConnectionString { get; private set; }

    public async Task InitializeAsync()
    {
        try
        {
            await _container.StartAsync();
        }
        catch (DockerUnavailableException e)
        {
            throw new InvalidOperationException(
                "The SQL Server integration tests need Docker. Start Docker and run the tests again.", e);
        }

        ConnectionString = await CreateDatabaseAsync("Elmah");
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    /// <summary>
    /// Creates an empty database and returns a connection string for it.
    /// </summary>
    public async Task<string> CreateDatabaseAsync(string name)
    {
        await ExecuteAsync(_container.GetConnectionString(), $"CREATE DATABASE [{name}]");

        return new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = name
        }.ConnectionString;
    }

    /// <summary>
    /// Creates an empty database with a unique name and returns a connection string for it.
    /// </summary>
    public Task<string> CreateUniqueDatabaseAsync() => CreateDatabaseAsync($"Elmah_{Guid.NewGuid():N}");

    public static async Task ExecuteAsync(string connectionString, string sql)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    public static async Task<T> ScalarAsync<T>(string connectionString, string sql, params SqlParameter[] parameters)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddRange(parameters);
        return (T)await command.ExecuteScalarAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class MsSqlCollection : ICollectionFixture<MsSqlFixture>
{
    public const string Name = "MsSql";
}
