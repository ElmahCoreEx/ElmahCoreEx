using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ElmahCore;

/// <summary>
///  Elmah Options
/// </summary>
public class ElmahOptions
{
    /// <summary>
    /// ELMAH access url (default = 'elmah')
    /// </summary>
    public string Path { get; set; }

    /// <summary>
    /// ELMAH log files path (default = '"~/errors.xml"'), example: options.LogPath = "~/log"; // OR options.LogPath =
    /// "с:\errors";
    /// </summary>
    public string LogPath { get; set; }

    /// <summary>
    /// Filters a file path, example: options.LogPath = "~/elmah.xml"; // OR options.LogPath = "с:\elmah.xml"
    /// </summary>
    public string FiltersConfig { get; set; }

    /// <summary>
    /// Custom filters
    /// </summary>
    public ICollection<IErrorFilter> Filters { get; set; } = new List<IErrorFilter>();

    /// <summary>
    /// Custom notifiers
    /// </summary>
    public ICollection<IErrorNotifier> Notifiers { get; set; } = new List<IErrorNotifier>();

    /// <summary>
    /// Error log
    /// </summary>
    public ErrorLog EventLog { get; set; }

    /// <summary>
    ///  Database connection string. Used with SqlErrorLog, MySqlErrorLog, PgsqlErrorLog
    /// </summary>
    public string ConnectionString { get; set; }

    /// <summary>
    /// Database table name. Used with SqlErrorLog
    /// Default value if not provided = "ELMAH_Error"
    /// </summary>
    public string SqlServerDatabaseTableName { get; set; }

    /// <summary>
    /// Database schema name. Used with SqlErrorLog
    /// Default value if not provided = "dbo"
    /// </summary>
    public string SqlServerDatabaseSchemaName { get; set; }

    /// <summary>
    /// Indicate if the CreateTables check should be run when initializing the logger.
    /// Defaults to true
    /// </summary>
    public bool CreateTablesIfNotExist { get; set; } = true;

    /// <summary>
    ///  Permission Check callback
    /// </summary>
    public Func<HttpContext, bool> OnPermissionCheck { get; set; } = context => true;

    /// <summary>
    ///  Custom error handler
    /// </summary>
    public Func<HttpContext, Error, Task> OnError { get; set; } = (context, error) => Task.CompletedTask;

    /// <summary>
    ///  Application Name
    /// </summary>
    public string ApplicationName { get; set; }

    /// <summary>
    ///  List of paths to sources
    /// </summary>
    public string[] SourcePaths { get; set; }

    /// <summary>
    ///  Enable/Disable request body logging
    /// </summary>
    public bool LogRequestBody { get; set; } = true;

    /// <summary>
    /// Maximum request body size in bytes to capture for logging.
    /// Bodies larger than this will not be captured.
    /// Default is 1MB (1,048,576 bytes). Set to 0 for unlimited (not recommended).
    /// </summary>
    public int MaxRequestBodySize { get; set; } = 1024 * 1024;

    /// <summary>
    /// Enable or disable the diagnostic observer
    /// Default is true
    /// </summary>
    public bool EnableDiagnosticObserver { get; set; } = true;

    /// <summary>
    /// Enable/Disable storing the full XML representation of errors in the database.
    /// When disabled, a minimal placeholder is stored instead.
    /// Default is true for backward compatibility.
    /// </summary>
    public bool LogAllXml { get; set; } = true;

    public virtual bool PermissionCheck(HttpContext context)
    {
        return OnPermissionCheck(context);
    }

    public virtual Task Error(HttpContext context, Error error)
    {
        return OnError(context, error);
    }
}