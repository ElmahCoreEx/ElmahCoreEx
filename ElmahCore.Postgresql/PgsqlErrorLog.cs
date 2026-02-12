using System;
using System.Data.Common;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;

namespace ElmahCore.Postgresql;

/// <summary>
///     An <see cref="ErrorLog" /> implementation that uses PostgreSQL
///     as its backing store.
/// </summary>
[UsedImplicitly]
public class PgsqlErrorLog : RelationalErrorLog
{
    private const int MaxAppNameLength = 60;
    private readonly bool _logAllXml;
    private readonly string _connectionString;

    /// <summary>
    ///     Initializes a new instance of the <see cref="PgsqlErrorLog" /> class
    ///     using a dictionary of configured settings.
    /// </summary>
    public PgsqlErrorLog(IOptions<ElmahOptions> option) : this(option.Value.ConnectionString,
        option.Value.CreateTablesIfNotExist, option.Value.LogAllXml)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="PgsqlErrorLog" /> class
    ///     to use a specific connection string for connecting to the database.
    /// </summary>
    public PgsqlErrorLog(string connectionString, bool createTablesIfNotExist = true, bool logAllXml = true)
    {
        if (string.IsNullOrEmpty(connectionString))
            throw new ArgumentNullException(nameof(connectionString));

        _connectionString = connectionString;
        _logAllXml = logAllXml;

        if (createTablesIfNotExist)
            CreateTableIfNotExists();
    }

    /// <inheritdoc />
    public override string Name => "PostgreSQL Error Log";

    /// <inheritdoc />
    protected override string ConnectionString => _connectionString;

    /// <inheritdoc />
    protected override bool LogAllXml => _logAllXml;

    /// <inheritdoc />
    protected override DbConnection CreateConnection() => new NpgsqlConnection(ConnectionString);

    /// <inheritdoc />
    protected override DbCommand CreateLogErrorCommand(Guid id, string appName, string hostName, string typeName,
        string source, string message, string user, int statusCode, DateTime time, string xml)
    {
        var command = new NpgsqlCommand
        {
            CommandText = @"
/* elmah */
INSERT INTO Elmah_Error (ErrorId, Application, Host, Type, Source, Message, ""User"", StatusCode, TimeUtc, AllXml)
VALUES (@ErrorId, @Application, @Host, @Type, @Source, @Message, @User, @StatusCode, @TimeUtc, @AllXml)
"
        };

        command.Parameters.Add(new NpgsqlParameter("ErrorId", id));
        command.Parameters.Add(new NpgsqlParameter("Application", appName));
        command.Parameters.Add(new NpgsqlParameter("Host", hostName));
        command.Parameters.Add(new NpgsqlParameter("Type", typeName));
        command.Parameters.Add(new NpgsqlParameter("Source", source));
        command.Parameters.Add(new NpgsqlParameter("Message", message));
        command.Parameters.Add(new NpgsqlParameter("User", user));
        command.Parameters.Add(new NpgsqlParameter("StatusCode", statusCode));
        command.Parameters.Add(new NpgsqlParameter("TimeUtc", time.ToUniversalTime()));
        command.Parameters.Add(new NpgsqlParameter("AllXml", xml));

        return command;
    }

    /// <inheritdoc />
    protected override DbCommand CreateGetErrorXmlCommand(string appName, Guid errorId)
    {
        var command = new NpgsqlCommand
        {
            CommandText = @"
SELECT AllXml
FROM Elmah_Error
WHERE
    Application = @Application
    AND ErrorId = @ErrorId
"
        };

        command.Parameters.Add(new NpgsqlParameter("Application", appName));
        command.Parameters.Add(new NpgsqlParameter("ErrorId", errorId));

        return command;
    }

    /// <inheritdoc />
    protected override DbCommand CreateGetErrorsXmlCommand(string appName, int errorIndex, int pageSize)
    {
        var command = new NpgsqlCommand
        {
            CommandText = @"
SELECT ErrorId, AllXml FROM Elmah_Error
WHERE
    Application = @Application
ORDER BY Sequence DESC
OFFSET @offset
LIMIT @limit
"
        };

        command.Parameters.Add("@Application", NpgsqlDbType.Text, MaxAppNameLength).Value = appName;
        command.Parameters.Add("@offset", NpgsqlDbType.Integer).Value = errorIndex;
        command.Parameters.Add("@limit", NpgsqlDbType.Integer).Value = pageSize;

        return command;
    }

    /// <inheritdoc />
    protected override DbCommand CreateGetErrorsCountCommand(string appName)
    {
        var command = new NpgsqlCommand
        {
            CommandText = "SELECT COUNT(*) FROM Elmah_Error WHERE Application = @Application"
        };
        command.Parameters.Add("@Application", NpgsqlDbType.Text, MaxAppNameLength).Value = appName;
        return command;
    }

    /// <inheritdoc />
    protected override void CreateTableIfNotExists()
    {
        using var connection = new NpgsqlConnection(ConnectionString);
        connection.Open();

        using var cmdCheck = new NpgsqlCommand
        {
            Connection = connection,
            CommandText = @"
SELECT EXISTS (
   SELECT 1
   FROM   information_schema.tables
   WHERE  table_schema = 'public'
   AND    table_name = 'elmah_error'
   )
"
        };

        var exists = (bool)cmdCheck.ExecuteScalar()!;

        if (!exists)
        {
            using var cmdCreate = new NpgsqlCommand
            {
                Connection = connection,
                CommandText = @"
CREATE SEQUENCE ELMAH_Error_SEQUENCE;
CREATE TABLE ELMAH_Error
(
    ErrorId		UUID NOT NULL,
    Application	VARCHAR(60) NOT NULL,
    Host 		VARCHAR(50) NOT NULL,
    Type		VARCHAR(100) NOT NULL,
    Source		VARCHAR(60)  NOT NULL,
    Message		VARCHAR(500) NOT NULL,
    ""User""		VARCHAR(50)  NOT NULL,
    StatusCode	INT NOT NULL,
    TimeUtc		TIMESTAMP NOT NULL,
    Sequence	INT NOT NULL DEFAULT NEXTVAL('ELMAH_Error_SEQUENCE'),
    AllXml		TEXT NOT NULL
);

ALTER TABLE ELMAH_Error ADD CONSTRAINT PK_ELMAH_Error PRIMARY KEY (ErrorId);

CREATE INDEX IX_ELMAH_Error_App_Time_Seq ON ELMAH_Error USING BTREE
(
    Application   ASC,
    TimeUtc       DESC,
    Sequence      DESC
);
"
            };
            cmdCreate.ExecuteNonQuery();
        }
    }
}
