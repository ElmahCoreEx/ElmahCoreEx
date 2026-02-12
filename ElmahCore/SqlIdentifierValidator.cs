using System;
using System.Text.RegularExpressions;

namespace ElmahCore;

/// <summary>
///     Validates SQL identifiers (table names, schema names) to prevent SQL injection.
/// </summary>
public static partial class SqlIdentifierValidator
{
    /// <summary>
    ///     Regular expression pattern for valid SQL identifiers.
    ///     Allows letters, digits, and underscores, must start with letter or underscore.
    /// </summary>
    private const string IdentifierPattern = @"^[a-zA-Z_][a-zA-Z0-9_]*$";

    [GeneratedRegex(IdentifierPattern)]
    private static partial Regex IdentifierRegex();

    /// <summary>
    ///     Validates that a string is a safe SQL identifier.
    /// </summary>
    /// <param name="identifier">The identifier to validate.</param>
    /// <returns>True if the identifier is valid; otherwise, false.</returns>
    public static bool IsValidIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            return false;

        return IdentifierRegex().IsMatch(identifier);
    }

    /// <summary>
    ///     Validates a SQL identifier and throws if invalid.
    /// </summary>
    /// <param name="identifier">The identifier to validate.</param>
    /// <param name="parameterName">The parameter name for the exception.</param>
    /// <exception cref="ArgumentException">Thrown when the identifier is invalid.</exception>
    public static void ValidateIdentifier(string identifier, string parameterName)
    {
        if (!IsValidIdentifier(identifier))
        {
            throw new ArgumentException(
                $"Invalid SQL identifier '{identifier}'. Identifiers must start with a letter or underscore " +
                "and contain only letters, digits, and underscores.",
                parameterName);
        }
    }

    /// <summary>
    ///     Validates a schema name and throws if invalid.
    /// </summary>
    /// <param name="schemaName">The schema name to validate.</param>
    /// <exception cref="ArgumentException">Thrown when the schema name is invalid.</exception>
    public static void ValidateSchemaName(string schemaName)
    {
        ValidateIdentifier(schemaName, nameof(schemaName));
    }

    /// <summary>
    ///     Validates a table name and throws if invalid.
    /// </summary>
    /// <param name="tableName">The table name to validate.</param>
    /// <exception cref="ArgumentException">Thrown when the table name is invalid.</exception>
    public static void ValidateTableName(string tableName)
    {
        ValidateIdentifier(tableName, nameof(tableName));
    }
}
