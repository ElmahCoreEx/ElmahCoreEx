using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;

namespace ElmahCore.MySql;

/// <summary>
///     An <see cref="ErrorLog" /> implementation that uses MySQL
///     as its backing store.
/// </summary>
[UsedImplicitly]
public class MySqlErrorLog : ErrorLog
{
    private readonly bool _logAllXml;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MySqlErrorLog" /> class
    ///     using a dictionary of configured settings.
    /// </summary>
    public MySqlErrorLog(IOptions<ElmahOptions> option) : this(option.Value.ConnectionString,
        option.Value.CreateTablesIfNotExist, option.Value.LogAllXml)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="MySqlErrorLog" /> class
    ///     to use a specific connection string for connecting to the database.
    /// </summary>
    public MySqlErrorLog(string connectionString, bool createTablesIfNotExist = true, bool logAllXml = true)
    {
        if (string.IsNullOrEmpty(connectionString))
            throw new ArgumentNullException(nameof(connectionString));

        ConnectionString = connectionString;
        _logAllXml = logAllXml;

        if (createTablesIfNotExist)
            CreateTableIfNotExist();
    }

    /// <summary>
    ///     Gets the name of this error log implementation.
    /// </summary>
    public override string Name => "MySQL Error Log";

    /// <summary>
    ///     Gets the connection string used by the log to connect to the database.
    /// </summary>
    protected virtual string ConnectionString { get; }

    public override string Log(Error error)
    {
        var id = Guid.NewGuid();

        Log(id, error);

        return id.ToString();
    }

    public override void Log(Guid id, Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        var errorXml = _logAllXml
            ? ErrorXml.EncodeString(error)
            : "<error message=\"AllXml logging disabled\" />";

        using var connection = new MySqlConnection(ConnectionString);
        using var command = CommandExtension.LogError(id, ApplicationName, error.HostName, error.Type,
            error.Source, error.Message, error.User, error.StatusCode, error.Time, errorXml);
        connection.Open();
        command.Connection = connection;
        command.ExecuteNonQuery();
    }

    public override ErrorLogEntry GetError(string id)
    {
        ArgumentNullException.ThrowIfNull(id);
        if (id.Length == 0) throw new ArgumentException(null, nameof(id));

        Guid errorGuid;

        try
        {
            errorGuid = new Guid(id);
        }
        catch (FormatException e)
        {
            throw new ArgumentException(e.Message, nameof(id), e);
        }

        string errorXml;

        using (var connection = new MySqlConnection(ConnectionString))
        using (var command = CommandExtension.GetErrorXml(ApplicationName, errorGuid))
        {
            command.Connection = connection;
            connection.Open();
            errorXml = (string)command.ExecuteScalar();
        }

        if (errorXml == null)
            return null;

        var error = ErrorXml.DecodeString(errorXml);
        return new ErrorLogEntry(this, id, error);
    }

    public override int GetErrors(int errorIndex, int pageSize, ICollection<ErrorLogEntry> errorEntryList)
    {
        if (errorIndex < 0) throw new ArgumentOutOfRangeException(nameof(errorIndex), errorIndex, null);
        if (pageSize < 0) throw new ArgumentOutOfRangeException(nameof(pageSize), pageSize, null);

        using var connection = new MySqlConnection(ConnectionString);
        connection.Open();

        using (var command = CommandExtension.GetErrorsXml(ApplicationName, errorIndex, pageSize))
        {
            command.Connection = connection;

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var id = reader.GetGuid(0);
                    var xml = reader.GetString(1);
                    var error = ErrorXml.DecodeString(xml);
                    errorEntryList.Add(new ErrorLogEntry(this, id.ToString(), error));
                }
            }
        }

        return GetTotalErrorsXml(connection);
    }

    /// <inheritdoc />
    public override async Task<string> LogAsync(Error error, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(error);

        var id = Guid.NewGuid();

        var errorXml = _logAllXml
            ? ErrorXml.EncodeString(error)
            : "<error message=\"AllXml logging disabled\" />";

        await using var connection = new MySqlConnection(ConnectionString);
        await using var command = CommandExtension.LogError(id, ApplicationName, error.HostName, error.Type,
            error.Source, error.Message, error.User, error.StatusCode, error.Time, errorXml);
        await connection.OpenAsync(cancellationToken);
        command.Connection = connection;
        await command.ExecuteNonQueryAsync(cancellationToken);

        return id.ToString();
    }

    /// <inheritdoc />
    public override async Task<ErrorLogEntry> GetErrorAsync(string id, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(id);
        if (id.Length == 0) throw new ArgumentException(null, nameof(id));

        Guid errorGuid;

        try
        {
            errorGuid = new Guid(id);
        }
        catch (FormatException e)
        {
            throw new ArgumentException(e.Message, nameof(id), e);
        }

        string errorXml;

        await using (var connection = new MySqlConnection(ConnectionString))
        await using (var command = CommandExtension.GetErrorXml(ApplicationName, errorGuid))
        {
            command.Connection = connection;
            await connection.OpenAsync(cancellationToken);
            errorXml = (string)await command.ExecuteScalarAsync(cancellationToken);
        }

        if (errorXml == null)
            return null;

        var error = ErrorXml.DecodeString(errorXml);
        return new ErrorLogEntry(this, id, error);
    }

    /// <inheritdoc />
    public override async Task<int> GetErrorsAsync(int errorIndex, int pageSize, ICollection<ErrorLogEntry> errorEntryList,
        CancellationToken cancellationToken)
    {
        if (errorIndex < 0) throw new ArgumentOutOfRangeException(nameof(errorIndex), errorIndex, null);
        if (pageSize < 0) throw new ArgumentOutOfRangeException(nameof(pageSize), pageSize, null);

        await using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using (var command = CommandExtension.GetErrorsXml(ApplicationName, errorIndex, pageSize))
        {
            command.Connection = connection;

            await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    var id = reader.GetGuid(0);
                    var xml = reader.GetString(1);
                    var error = ErrorXml.DecodeString(xml);
                    errorEntryList.Add(new ErrorLogEntry(this, id.ToString(), error));
                }
            }
        }

        return await GetTotalErrorsXmlAsync(connection, cancellationToken);
    }

    private async Task<int> GetTotalErrorsXmlAsync(MySqlConnection connection, CancellationToken cancellationToken)
    {
        await using var command = CommandExtension.GetTotalErrorsXml(ApplicationName);
        command.Connection = connection;
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    /// <summary>
    ///     Creates the necessary tables used by this implementation
    /// </summary>
    private void CreateTableIfNotExist()
    {
        using var connection = new MySqlConnection(ConnectionString);
        connection.Open();
        var databaseName = connection.Database;

        using var commandCheck = CommandExtension.CheckTable(databaseName);
        commandCheck.Connection = connection;
        var exists = Convert.ToBoolean(commandCheck.ExecuteScalar());

        if (!exists)
        {
            using var commandCreate = CommandExtension.CreateTable();
            commandCreate.Connection = connection;
            commandCreate.ExecuteNonQuery();
        }
    }

    private int GetTotalErrorsXml(MySqlConnection connection)
    {
        using var command = CommandExtension.GetTotalErrorsXml(ApplicationName);
        command.Connection = connection;
        return Convert.ToInt32(command.ExecuteScalar());
    }
}