using Connected.Data.Expressions;
using Connected.Data.Expressions.Languages;
using Connected.Data.Expressions.Translation;
using Connected.Data.Expressions.TypeSystem;

namespace Connected.Storage.Oracle.Query;

/// <summary>
/// Represents the Oracle SQL query language implementation.
/// </summary>
/// <remarks>
/// This sealed class provides Oracle specific query language capabilities including type system configuration,
/// identifier quoting rules, and linguist creation for query translation. It extends <see cref="QueryLanguage"/>
/// to support Oracle dialect-specific features and syntax requirements. Oracle uses double quotes for
/// case-sensitive identifier escaping, while unquoted identifiers are case-insensitive and fold to uppercase.
/// The class supports Oracle-specific features including bind variables with colon prefix, ROWNUM for pagination
/// (pre-12c), and FETCH FIRST syntax (12c+). Oracle allows DISTINCT in aggregate functions and supports
/// subqueries in SELECT without FROM using the DUAL table.
/// </remarks>
internal sealed class OracleLanguage
	: QueryLanguage
{
	private static OracleLanguage? _default;

	/// <summary>
	/// Initializes the static members of the <see cref="OracleLanguage"/> class.
	/// </summary>
	static OracleLanguage()
	{
		SplitChars = ['.'];
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="OracleLanguage"/> class.
	/// </summary>
	public OracleLanguage()
	{
		TypeSystem = new OracleTypeSystem();
	}

	/// <summary>
	/// Gets the type system for Oracle data type management and conversion.
	/// </summary>
	public override QueryTypeSystem TypeSystem { get; }

	/// <summary>
	/// Gets the characters used to split multi-part identifiers.
	/// </summary>
	/// <remarks>
	/// Contains the period (.) character used to separate schema and object names in Oracle.
	/// </remarks>
	private static char[] SplitChars { get; }

	/// <summary>
	/// Gets a value indicating whether the language supports multiple commands in a single batch.
	/// </summary>
	/// <remarks>
	/// Returns <c>true</c> for Oracle as it supports executing multiple statements in a single batch
	/// when separated by semicolons or using PL/SQL blocks.
	/// </remarks>
	public override bool AllowsMultipleCommands => true;

	/// <summary>
	/// Gets a value indicating whether the language allows subqueries in SELECT clause without FROM clause.
	/// </summary>
	/// <remarks>
	/// Returns <c>true</c> for Oracle as it allows scalar subqueries in SELECT without FROM, though
	/// the DUAL table is traditionally used for such queries (e.g., SELECT SYSDATE FROM DUAL).
	/// </remarks>
	public override bool AllowSubqueryInSelectWithoutFrom => true;

	/// <summary>
	/// Gets a value indicating whether the language allows DISTINCT keyword in aggregate functions.
	/// </summary>
	/// <remarks>
	/// Returns <c>true</c> for Oracle as it supports DISTINCT in aggregates like COUNT(DISTINCT column).
	/// </remarks>
	public override bool AllowDistinctInAggregates => true;

	/// <summary>
	/// Gets the singleton default instance of the <see cref="OracleLanguage"/> class.
	/// </summary>
	/// <remarks>
	/// Provides a thread-safe lazy-initialized singleton instance for reuse across the application.
	/// </remarks>
	public static OracleLanguage Default
	{
		get
		{
			if (_default is null)
				Interlocked.CompareExchange(ref _default, new OracleLanguage(), null);

			return _default;
		}
	}

	/// <summary>
	/// Quotes an identifier name using Oracle double quote notation.
	/// </summary>
	/// <param name="name">The identifier name to quote.</param>
	/// <returns>The quoted identifier with double quotes.</returns>
	/// <remarks>
	/// Handles multi-part identifiers (e.g., schema.table) by quoting each part separately.
	/// If the name is already quoted with double quotes, it returns the name unchanged.
	/// Oracle identifiers are case-sensitive when quoted and fold to uppercase when unquoted.
	/// Quoting is necessary for case-sensitive identifiers, reserved keywords, or special characters.
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
	/// Creates a linguist instance for Oracle query translation.
	/// </summary>
	/// <param name="context">The expression compilation context.</param>
	/// <param name="translator">The expression translator.</param>
	/// <returns>A <see cref="OracleLinguist"/> instance configured for Oracle query generation.</returns>
	public override Linguist CreateLinguist(ExpressionCompilationContext context, Translator translator)
	{
		return new OracleLinguist(context, this, translator);
	}
}
