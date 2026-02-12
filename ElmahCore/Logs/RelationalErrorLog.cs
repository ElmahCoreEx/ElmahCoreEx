using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace ElmahCore;

/// <summary>
///     Abstract base class for relational database error log implementations.
///     Provides common logic for SQL Server, MySQL, and PostgreSQL backends.
/// </summary>
public abstract class RelationalErrorLog : ErrorLog
{
    /// <summary>
    ///     Gets the connection string used by the log to connect to the database.
    /// </summary>
    protected abstract string ConnectionString { get; }

    /// <summary>
    ///     Gets whether to log the full XML representation of errors.
    /// </summary>
    protected abstract bool LogAllXml { get; }

    /// <summary>
    ///     Creates a new database connection.
    /// </summary>
    protected abstract DbConnection CreateConnection();

    /// <summary>
    ///     Creates a command to insert an error into the database.
    /// </summary>
    protected abstract DbCommand CreateLogErrorCommand(
        Guid id,
        string appName,
        string hostName,
        string typeName,
        string source,
        string message,
        string user,
        int statusCode,
        DateTime time,
        string xml);

    /// <summary>
    ///     Creates a command to retrieve the XML for a single error.
    /// </summary>
    protected abstract DbCommand CreateGetErrorXmlCommand(string appName, Guid errorId);

    /// <summary>
    ///     Creates a command to retrieve a page of errors.
    /// </summary>
    protected abstract DbCommand CreateGetErrorsXmlCommand(string appName, int errorIndex, int pageSize);

    /// <summary>
    ///     Creates a command to get the total count of errors.
    /// </summary>
    protected abstract DbCommand CreateGetErrorsCountCommand(string appName);

    /// <summary>
    ///     Converts the scalar result from the count command to an integer.
    /// </summary>
    protected virtual int ConvertCountResult(object scalarResult)
    {
        return Convert.ToInt32(scalarResult);
    }

    /// <summary>
    ///     Called during construction to create the error table if it doesn't exist.
    /// </summary>
    protected abstract void CreateTableIfNotExists();

    /// <inheritdoc />
    public override string Log(Error error)
    {
        var id = Guid.NewGuid();
        Log(id, error);
        return id.ToString();
    }

    /// <inheritdoc />
    public override void Log(Guid id, Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        try
        {
            var errorXml = LogAllXml
                ? ErrorXml.EncodeString(error)
                : "<error message=\"AllXml logging disabled\" />";

            using var connection = CreateConnection();
            using var command = CreateLogErrorCommand(id, ApplicationName, error.HostName, error.Type,
                error.Source, error.Message, error.User, error.StatusCode, error.Time, errorXml);
            command.Connection = connection;
            connection.Open();
            command.ExecuteNonQuery();
        }
        catch
        {
            // Guard: silently fail to prevent stack overflow from errors attempting to log errors
        }
    }

    /// <inheritdoc />
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

        using (var connection = CreateConnection())
        using (var command = CreateGetErrorXmlCommand(ApplicationName, errorGuid))
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

    /// <inheritdoc />
    public override int GetErrors(int errorIndex, int pageSize, ICollection<ErrorLogEntry> errorEntryList)
    {
        if (errorIndex < 0) throw new ArgumentOutOfRangeException(nameof(errorIndex), errorIndex, null);
        if (pageSize < 0) throw new ArgumentOutOfRangeException(nameof(pageSize), pageSize, null);

        using var connection = CreateConnection();
        connection.Open();

        using (var command = CreateGetErrorsXmlCommand(ApplicationName, errorIndex, pageSize))
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

        using (var command = CreateGetErrorsCountCommand(ApplicationName))
        {
            command.Connection = connection;
            return ConvertCountResult(command.ExecuteScalar());
        }
    }

    /// <inheritdoc />
    public override async Task<string> LogAsync(Error error, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(error);

        var id = Guid.NewGuid();

        try
        {
            var errorXml = LogAllXml
                ? ErrorXml.EncodeString(error)
                : "<error message=\"AllXml logging disabled\" />";

            await using var connection = CreateConnection();
            await using var command = CreateLogErrorCommand(id, ApplicationName, error.HostName, error.Type,
                error.Source, error.Message, error.User, error.StatusCode, error.Time, errorXml);
            command.Connection = connection;
            await connection.OpenAsync(cancellationToken);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch
        {
            // Guard: silently fail to prevent stack overflow from errors attempting to log errors
        }

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

        await using (var connection = CreateConnection())
        await using (var command = CreateGetErrorXmlCommand(ApplicationName, errorGuid))
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

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using (var command = CreateGetErrorsXmlCommand(ApplicationName, errorIndex, pageSize))
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

        await using (var command = CreateGetErrorsCountCommand(ApplicationName))
        {
            command.Connection = connection;
            return ConvertCountResult(await command.ExecuteScalarAsync(cancellationToken));
        }
    }
}
