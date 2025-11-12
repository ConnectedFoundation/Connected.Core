namespace Connected.Storage.Sql.Schemas;

/// <summary>
/// Base class for schema synchronization commands providing SQL identifier escaping utilities.
/// </summary>
/// <remarks>
/// This abstract class provides common functionality for all schema synchronization operations
/// including methods for properly escaping and unescaping SQL identifiers. SQL Server requires
/// identifiers containing special characters, spaces, or reserved keywords to be enclosed in
/// square brackets. The class provides static utility methods for consistent identifier handling
/// across all schema operations, ensuring generated DDL statements are syntactically correct
/// and safe from SQL injection when using dynamic identifiers.
/// </remarks>
internal abstract class SynchronizationCommand
{
	/// <summary>
	/// Escapes a SQL identifier by enclosing it in square brackets.
	/// </summary>
	/// <param name="value">The identifier to escape.</param>
	/// <returns>The escaped identifier enclosed in square brackets.</returns>
	/// <remarks>
	/// This method ensures the identifier is properly formatted for use in SQL statements,
	/// removing any existing brackets before adding new ones to prevent double-bracketing.
	/// </remarks>
	public static string Escape(string value)
	{
		return $"[{Unescape(value)}]";
	}

	/// <summary>
	/// Removes square bracket escaping from a SQL identifier.
	/// </summary>
	/// <param name="value">The identifier to unescape.</param>
	/// <returns>The identifier without square brackets.</returns>
	/// <remarks>
	/// This method extracts the raw identifier name by removing leading and trailing brackets,
	/// useful for identifier comparison and manipulation operations.
	/// </remarks>
	public static string Unescape(string value)
	{
		return value.TrimStart('[').TrimEnd(']');
	}

	/// <summary>
	/// Escapes a schema-qualified identifier by enclosing both parts in square brackets.
	/// </summary>
	/// <param name="schema">The schema name.</param>
	/// <param name="name">The object name.</param>
	/// <returns>The fully qualified and escaped identifier in the format [schema].[name].</returns>
	/// <remarks>
	/// This method creates a fully qualified identifier suitable for use in DDL statements,
	/// ensuring both the schema and object name are properly escaped.
	/// </remarks>
	public static string Escape(string schema, string name)
	{
		return $"{Escape(schema)}.{Escape(name)}";
	}
}
