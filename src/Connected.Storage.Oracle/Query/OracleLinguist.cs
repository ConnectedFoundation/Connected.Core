using Connected.Data.Expressions;
using Connected.Data.Expressions.Languages;
using Connected.Data.Expressions.Translation;
using Connected.Data.Expressions.Translation.Rewriters;
using System.Linq.Expressions;

namespace Connected.Storage.Oracle.Query;

/// <summary>
/// Provides Oracle specific query translation and expression formatting capabilities.
/// </summary>
/// <remarks>
/// This sealed class extends <see cref="Linguist"/> to handle Oracle-specific query translation patterns,
/// including ORDER BY optimization, skip/take conversion to ROWNUM or OFFSET/FETCH patterns, and Oracle formatting.
/// It orchestrates the translation pipeline for converting LINQ expressions to executable Oracle queries.
/// Oracle supports multiple pagination strategies: traditional ROWNUM with nested queries (all versions) and
/// modern OFFSET/FETCH FIRST syntax (12c+). The linguist can be configured to use either approach based on
/// database version. Oracle also supports ROW_NUMBER() OVER for more complex pagination scenarios.
/// </remarks>
internal sealed class OracleLinguist(ExpressionCompilationContext context, OracleLanguage language, Translator translator)
	: Linguist(context, language, translator)
{
	/// <summary>
	/// Translates a LINQ expression tree into an Oracle compatible expression.
	/// </summary>
	/// <param name="expression">The expression tree to translate.</param>
	/// <returns>A translated expression optimized for Oracle execution.</returns>
	/// <remarks>
	/// The translation process applies multiple optimization passes:
	/// 1. Rewrites ORDER BY clauses for Oracle compatibility
	/// 2. Applies base translation logic
	/// 3. Converts Skip/Take operations to ROWNUM or OFFSET/FETCH patterns (Oracle version-dependent)
	/// 4. Re-optimizes ORDER BY clauses after transformation
	/// This ensures efficient pagination and sorting in Oracle using native constructs. For Oracle 12c+,
	/// OFFSET/FETCH is preferred; for earlier versions, ROWNUM with nested queries is used.
	/// </remarks>
	public override Expression Translate(Expression expression)
	{
		/*
		 * First pass: optimize any ORDER BY clauses for Oracle
		 */
		expression = OrderByRewriter.Rewrite(Language, expression);

		expression = base.Translate(expression);

		/*
		 * Convert Skip/Take patterns to ROWNUM or OFFSET/FETCH for pagination
		 * Oracle 12c+ supports OFFSET/FETCH natively
		 * Earlier versions use ROWNUM with nested queries
		 */
		expression = SkipToRowNumberRewriter.Rewrite(Language, expression);

		/*
		 * Final pass: re-optimize ORDER BY clauses after transformation
		 */
		expression = OrderByRewriter.Rewrite(Language, expression);

		return expression;
	}

	/// <summary>
	/// Formats the translated expression into an Oracle query string.
	/// </summary>
	/// <param name="expression">The expression to format.</param>
	/// <returns>An Oracle query string representation of the expression.</returns>
	public override string Format(Expression expression)
	{
		return OracleFormatter.Format(Context, expression, (OracleLanguage)Language);
	}
}
