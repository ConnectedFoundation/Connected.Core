using Connected.Data.Expressions;
using Connected.Data.Expressions.Languages;
using Connected.Data.Expressions.Translation;
using Connected.Data.Expressions.TypeSystem;

namespace Connected.Storage.PostgreSql.Query;

/// <summary>
/// Represents the PostgreSQL query language implementation.
/// </summary>
/// <remarks>
/// This sealed class provides PostgreSQL specific query language capabilities including type system configuration,
/// identifier quoting rules, and linguist creation for query translation. It extends <see cref="QueryLanguage"/>
/// to support PostgreSQL dialect-specific features and syntax requirements. PostgreSQL uses double quotes for
/// identifier escaping and supports advanced features like LIMIT/OFFSET for pagination, CTE with recursive
/// support, and extensive JSON operations.
/// </remarks>
internal sealed class PostgreSqlLanguage
	: QueryLanguage
{
	private static PostgreSqlLanguage? _default;

	/// <summary>
	/// Initializes the static members of the <see cref="PostgreSqlLanguage"/> class.
	/// </summary>
	static PostgreSqlLanguage()
	{
		SplitChars = ['.'];
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="PostgreSqlLanguage"/> class.
	/// </summary>
	public PostgreSqlLanguage()
	{
		TypeSystem = new PostgreSqlTypeSystem();
	}

	/// <summary>
	/// Gets the type system for PostgreSQL data type management and conversion.
	/// </summary>
	public override QueryTypeSystem TypeSystem { get; }

	/// <summary>
	/// Gets the characters used to split multi-part identifiers.
	/// </summary>
	/// <remarks>
	/// Contains the period (.) character used to separate schema and object names in PostgreSQL.
	/// </remarks>
	private static char[] SplitChars { get; }

	/// <summary>
	/// Gets a value indicating whether the language supports multiple commands in a single batch.
	/// </summary>
	/// <remarks>
	/// Returns <c>true</c> for PostgreSQL as it supports executing multiple statements in a single batch.
	/// </remarks>
	public override bool AllowsMultipleCommands => true;

	/// <summary>
	/// Gets a value indicating whether the language allows subqueries in SELECT clause without FROM clause.
	/// </summary>
	/// <remarks>
	/// Returns <c>true</c> for PostgreSQL as it allows scalar subqueries in SELECT without FROM.
	/// </remarks>
	public override bool AllowSubqueryInSelectWithoutFrom => true;

	/// <summary>
	/// Gets a value indicating whether the language allows DISTINCT keyword in aggregate functions.
	/// </summary>
	/// <remarks>
	/// Returns <c>true</c> for PostgreSQL as it supports DISTINCT in aggregates like COUNT(DISTINCT column).
	/// </remarks>
	public override bool AllowDistinctInAggregates => true;

	/// <summary>
	/// Gets the singleton default instance of the <see cref="PostgreSqlLanguage"/> class.
	/// </summary>
	/// <remarks>
	/// Provides a thread-safe lazy-initialized singleton instance for reuse across the application.
	/// </remarks>
	public static PostgreSqlLanguage Default
	{
		get
		{
			if (_default is null)
				Interlocked.CompareExchange(ref _default, new PostgreSqlLanguage(), null);

			return _default;
		}
	}

	/// <summary>
	/// Quotes an identifier name using PostgreSQL double quote notation.
	/// </summary>
	/// <param name="name">The identifier name to quote.</param>
	/// <returns>The quoted identifier with double quotes.</returns>
	/// <remarks>
	/// Handles multi-part identifiers (e.g., schema.table) by quoting each part separately.
	/// If the name is already quoted with double quotes, it returns the name unchanged.
	/// PostgreSQL identifiers are case-sensitive when quoted and fold to lowercase when unquoted.
	/// </remarks>
	public override string Quote(string name)
	{
		if (name.StartsWith('"') && name.EndsWith('"'))
			return name;
		else if (name.Contains('.'))
		{
			/*
			 * Split multi-part identifier and quote each part separately
			 */
			return $"\"{string.Join("\".\"", name.Split(SplitChars, StringSplitOptions.RemoveEmptyEntries))}\"";
		}
		else
			return $"\"{name}\"";
	}

	/// <summary>
	/// Creates a linguist instance for PostgreSQL query translation.
	/// </summary>
	/// <param name="context">The expression compilation context.</param>
	/// <param name="translator">The expression translator.</param>
	/// <returns>A <see cref="PostgreSqlLinguist"/> instance configured for PostgreSQL query generation.</returns>
	public override Linguist CreateLinguist(ExpressionCompilationContext context, Translator translator)
	{
		return new PostgreSqlLinguist(context, this, translator);
	}
}
