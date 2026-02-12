using System;
using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace ElmahCore.Sql;

/// <summary>
///     An <see cref="ErrorLog" /> implementation that uses MSSQL
///     as its backing store.
/// </summary>
// ReSharper disable once UnusedType.Global
public class SqlErrorLog : RelationalErrorLog
{
    private const int MaxAppNameLength = 60;
    private readonly bool _logAllXml;
    private readonly string _connectionString;
    private readonly string _schemaName;
    private readonly string _tableName;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SqlErrorLog" /> class
    ///     using a dictionary of configured settings.
    /// </summary>
    public SqlErrorLog(IOptions<ElmahOptions> option)
        : this(option.Value.ConnectionString, option.Value.SqlServerDatabaseSchemaName,
            option.Value.SqlServerDatabaseTableName, option.Value.CreateTablesIfNotExist,
            option.Value.LogAllXml)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SqlErrorLog" /> class
    ///     to use a specific connection string for connecting to the database and a specific schema and table name.
    /// </summary>
    public SqlErrorLog(string connectionString, string schemaName = null, string tableName = null,
        bool createTablesIfNotExist = true, bool logAllXml = true)
    {
        if (string.IsNullOrEmpty(connectionString))
            throw new ArgumentNullException(nameof(connectionString));

        _connectionString = connectionString;
        _schemaName = !string.IsNullOrWhiteSpace(schemaName) ? schemaName : "dbo";
        _tableName = !string.IsNullOrWhiteSpace(tableName) ? tableName : "ELMAH_Error";
        _logAllXml = logAllXml;

        if (createTablesIfNotExist)
            CreateTableIfNotExists();
    }

    /// <inheritdoc />
    public override string Name => "MSSQL Error Log";

    /// <inheritdoc />
    protected override string ConnectionString => _connectionString;

    /// <inheritdoc />
    protected override bool LogAllXml => _logAllXml;

    /// <summary>
    ///     Gets the schema name to be used for the error table.
    /// </summary>
    protected virtual string DatabaseSchemaName => _schemaName;

    /// <summary>
    ///     Gets the table name to be used for the error table.
    /// </summary>
    protected virtual string DatabaseTableName => _tableName;

    /// <inheritdoc />
    protected override DbConnection CreateConnection() => new SqlConnection(ConnectionString);

    /// <inheritdoc />
    protected override DbCommand CreateLogErrorCommand(Guid id, string appName, string hostName, string typeName,
        string source, string message, string user, int statusCode, DateTime time, string xml)
    {
        var command = new SqlCommand
        {
            CommandText = $@"
/* elmah */
INSERT INTO [{DatabaseSchemaName}].[{DatabaseTableName}] (ErrorId, Application, Host, Type, Source, Message, ""User"", StatusCode, TimeUtc, AllXml)
VALUES (@ErrorId, @Application, @Host, @Type, @Source, @Message, @User, @StatusCode, @TimeUtc, @AllXml)
"
        };
        command.Parameters.Add(new SqlParameter("ErrorId", id));
        command.Parameters.Add(new SqlParameter("Application", appName));
        command.Parameters.Add(new SqlParameter("Host", hostName));
        command.Parameters.Add(new SqlParameter("Type", typeName));
        command.Parameters.Add(new SqlParameter("Source", source));
        command.Parameters.Add(new SqlParameter("Message", message));
        command.Parameters.Add(new SqlParameter("User", user));
        command.Parameters.Add(new SqlParameter("StatusCode", statusCode));
        command.Parameters.Add(new SqlParameter("TimeUtc", time.ToUniversalTime()));
        command.Parameters.Add(new SqlParameter("AllXml", xml));

        return command;
    }

    /// <inheritdoc />
    protected override DbCommand CreateGetErrorXmlCommand(string appName, Guid errorId)
    {
        var command = new SqlCommand
        {
            CommandText = $@"
SELECT AllXml FROM [{DatabaseSchemaName}].[{DatabaseTableName}]
WHERE
    Application = @Application
    AND ErrorId = @ErrorId
"
        };

        command.Parameters.Add(new SqlParameter("Application", appName));
        command.Parameters.Add(new SqlParameter("ErrorId", errorId));

        return command;
    }

    /// <inheritdoc />
    protected override DbCommand CreateGetErrorsXmlCommand(string appName, int errorIndex, int pageSize)
    {
        var command = new SqlCommand
        {
            CommandText = $@"
SELECT ErrorId, AllXml FROM [{DatabaseSchemaName}].[{DatabaseTableName}]
WHERE
    Application = @Application
ORDER BY [Sequence] DESC
OFFSET     @offset ROWS
FETCH NEXT @limit ROWS ONLY;
"
        };

        command.Parameters.Add("@Application", SqlDbType.NVarChar, MaxAppNameLength).Value = appName;
        command.Parameters.Add("@offset", SqlDbType.Int).Value = errorIndex;
        command.Parameters.Add("@limit", SqlDbType.Int).Value = pageSize;

        return command;
    }

    /// <inheritdoc />
    protected override DbCommand CreateGetErrorsCountCommand(string appName)
    {
        var command = new SqlCommand
        {
            CommandText = $"SELECT COUNT(*) FROM [{DatabaseSchemaName}].[{DatabaseTableName}] WHERE Application = @Application"
        };
        command.Parameters.Add("@Application", SqlDbType.NVarChar, MaxAppNameLength).Value = appName;
        return command;
    }

    /// <inheritdoc />
    protected override void CreateTableIfNotExists()
    {
        using var connection = new SqlConnection(ConnectionString);
        connection.Open();

        using var cmdCheck = new SqlCommand
        {
            Connection = connection,
            CommandText = $@"
SELECT 1
WHERE EXISTS (
   SELECT 1
   FROM   INFORMATION_SCHEMA.TABLES
   WHERE  TABLE_SCHEMA = '{DatabaseSchemaName}'
   AND    TABLE_NAME = '{DatabaseTableName}'
   )
"
        };

        var exists = (int?)cmdCheck.ExecuteScalar();

        if (!exists.HasValue)
            ExecuteBatchNonQuery(CreateTableSql(), connection);
    }

    private string CreateTableSql()
    {
        return $@"
CREATE TABLE [{DatabaseSchemaName}].[{DatabaseTableName}]
(
    [ErrorId]     UNIQUEIDENTIFIER NOT NULL,
    [Application] NVARCHAR(60)  NOT NULL,
    [Host]        NVARCHAR(50)  NOT NULL,
    [Type]        NVARCHAR(100) NOT NULL,
    [Source]      NVARCHAR(60)  NOT NULL,
    [Message]     NVARCHAR(MAX) NOT NULL,
    [User]        NVARCHAR(50)  NOT NULL,
    [StatusCode]  INT NOT NULL,
    [TimeUtc]     DATETIME NOT NULL,
    [Sequence]    INT IDENTITY (1, 1) NOT NULL,
    [AllXml]      NVARCHAR(MAX) NOT NULL
)
GO

ALTER TABLE [{DatabaseSchemaName}].[{DatabaseTableName}] WITH NOCHECK ADD
    CONSTRAINT [PK_{DatabaseTableName}] PRIMARY KEY NONCLUSTERED ([ErrorId]) ON [PRIMARY]
GO

ALTER TABLE [{DatabaseSchemaName}].[{DatabaseTableName}] ADD
    CONSTRAINT [DF_{DatabaseTableName}_ErrorId] DEFAULT (NEWID()) FOR [ErrorId]
GO

CREATE NONCLUSTERED INDEX [IX_{DatabaseTableName}_App_Time_Seq] ON [{DatabaseSchemaName}].[{DatabaseTableName}]
(
    [Application]   ASC,
    [TimeUtc]       DESC,
    [Sequence]      DESC
)
ON [PRIMARY]";
    }

    private static void ExecuteBatchNonQuery(string sql, SqlConnection conn)
    {
        var sqlBatch = string.Empty;
        using var cmd = new SqlCommand(string.Empty, conn);
        sql += "\nGO"; // make sure the last batch is executed.
        foreach (var line in sql.Split(["\n", "\r"], StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.ToUpperInvariant().Trim() == "GO")
            {
                cmd.CommandText = sqlBatch;
                cmd.ExecuteNonQuery();
                sqlBatch = string.Empty;
            }
            else
            {
                sqlBatch += line + "\n";
            }
        }
    }
}
