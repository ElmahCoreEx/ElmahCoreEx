using System;
using System.Data.Common;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;

namespace ElmahCore.MySql;

/// <summary>
/// An <see cref="ErrorLog" /> implementation that uses MySQL
/// as its backing store.
/// </summary>
[UsedImplicitly]
public class MySqlErrorLog : RelationalErrorLog
{
    private readonly string _connectionString;
    private readonly bool _logAllXml;

    /// <summary>
    /// Initializes a new instance of the <see cref="MySqlErrorLog" /> class
    /// using a dictionary of configured settings.
    /// </summary>
    public MySqlErrorLog(IOptions<ElmahOptions> option) : this(option.Value.ConnectionString,
        option.Value.CreateTablesIfNotExist, option.Value.LogAllXml)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MySqlErrorLog" /> class
    /// to use a specific connection string for connecting to the database.
    /// </summary>
    public MySqlErrorLog(string connectionString, bool createTablesIfNotExist = true, bool logAllXml = true)
    {
        if (string.IsNullOrEmpty(connectionString))
            throw new ArgumentNullException(nameof(connectionString));

        _connectionString = connectionString;
        _logAllXml = logAllXml;

        if (createTablesIfNotExist)
            CreateTableIfNotExists();
    }

    /// <inheritdoc />
    public override string Name => "MySQL Error Log";

    /// <inheritdoc />
    protected override string ConnectionString => _connectionString;

    /// <inheritdoc />
    protected override bool LogAllXml => _logAllXml;

    /// <inheritdoc />
    protected override DbConnection CreateConnection() => new MySqlConnection(ConnectionString);

    /// <inheritdoc />
    protected override DbCommand CreateLogErrorCommand(Guid id, string appName, string hostName, string typeName,
        string source, string message, string user, int statusCode, DateTime time, string xml)
    {
        var command = new MySqlCommand
        {
            CommandText = @"
/* elmah */
INSERT INTO ELMAH_Error (ErrorId, Application, Host, Type, Source, Message, User, StatusCode, TimeUtc, AllXml)
VALUES (@ErrorId, @Application, @Host, @Type, @Source, @Message, @User, @StatusCode, @TimeUtc, @AllXml)
"
        };

        command.Parameters.Add(new MySqlParameter("ErrorId", id));
        command.Parameters.Add(new MySqlParameter("Application", appName));
        command.Parameters.Add(new MySqlParameter("Host", hostName));
        command.Parameters.Add(new MySqlParameter("Type", typeName));
        command.Parameters.Add(new MySqlParameter("Source", source));
        command.Parameters.Add(new MySqlParameter("Message", message));
        command.Parameters.Add(new MySqlParameter("User", user));
        command.Parameters.Add(new MySqlParameter("StatusCode", statusCode));
        command.Parameters.Add(new MySqlParameter("TimeUtc", time.ToUniversalTime()));
        command.Parameters.Add(new MySqlParameter("AllXml", xml));

        return command;
    }

    /// <inheritdoc />
    protected override DbCommand CreateGetErrorXmlCommand(string appName, Guid errorId)
    {
        var command = new MySqlCommand
        {
            CommandText = @"
SELECT AllXml FROM ELMAH_Error
WHERE
    Application = @Application
    AND ErrorId = @ErrorId
"
        };

        command.Parameters.Add(new MySqlParameter("Application", appName));
        command.Parameters.Add(new MySqlParameter("ErrorId", errorId));

        return command;
    }

    /// <inheritdoc />
    protected override DbCommand CreateGetErrorsXmlCommand(string appName, int errorIndex, int pageSize)
    {
        var command = new MySqlCommand
        {
            CommandText = @"
SELECT ErrorId, AllXml FROM ELMAH_Error
WHERE
    Application = @Application
ORDER BY Sequence DESC
    LIMIT @limit
    OFFSET @offset
"
        };

        command.Parameters.Add("@Application", MySqlDbType.String).Value = appName;
        command.Parameters.Add("@offset", MySqlDbType.Int32).Value = errorIndex;
        command.Parameters.Add("@limit", MySqlDbType.Int32).Value = pageSize;

        return command;
    }

    /// <inheritdoc />
    protected override DbCommand CreateGetErrorsCountCommand(string appName)
    {
        var command = new MySqlCommand
        {
            CommandText = "SELECT COUNT(*) FROM ELMAH_Error WHERE Application = @Application"
        };
        command.Parameters.Add(new MySqlParameter("@Application", appName));
        return command;
    }

    /// <inheritdoc />
    protected override void CreateTableIfNotExists()
    {
        using var connection = new MySqlConnection(ConnectionString);
        connection.Open();
        var databaseName = connection.Database;

        using var commandCheck = new MySqlCommand
        {
            Connection = connection,
            CommandText = @"
SELECT EXISTS (
   SELECT 1
   FROM   information_schema.tables
   WHERE  table_schema = @DatabaseName
   AND    table_name = 'ELMAH_Error'
   );
"
        };
        commandCheck.Parameters.Add(new MySqlParameter("@DatabaseName", databaseName));

        var exists = Convert.ToBoolean(commandCheck.ExecuteScalar());

        if (!exists)
        {
            using var commandCreate = new MySqlCommand
            {
                Connection = connection,
                CommandText = @"
CREATE TABLE ELMAH_Error
(
    ErrorId		VARCHAR(64) NOT NULL,
    Application	VARCHAR(60) NOT NULL,
    Host 		VARCHAR(50) NOT NULL,
    Type		VARCHAR(100) NOT NULL,
    Source		VARCHAR(60)  NOT NULL,
    Message		TEXT NOT NULL,
    User		VARCHAR(50)  NOT NULL,
    StatusCode	INT NOT NULL,
    TimeUtc		TIMESTAMP NOT NULL,
    Sequence	INT NOT NULL AUTO_INCREMENT,
    AllXml		MEDIUMTEXT NOT NULL,
    KEY(Sequence)
);

ALTER TABLE ELMAH_Error ADD CONSTRAINT PK_ELMAH_Error PRIMARY KEY (ErrorId);

CREATE INDEX IX_ELMAH_Error_App_Time_Seq ON ELMAH_Error
(
    Application   ASC,
    TimeUtc       DESC,
    Sequence      DESC
) USING BTREE;
"
            };
            commandCreate.ExecuteNonQuery();
        }
    }
}