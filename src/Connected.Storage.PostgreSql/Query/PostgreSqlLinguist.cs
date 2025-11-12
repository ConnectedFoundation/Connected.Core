using Connected.Data.Expressions;
using Connected.Data.Expressions.Languages;
using Connected.Data.Expressions.Translation;
using Connected.Data.Expressions.Translation.Rewriters;
using System.Linq.Expressions;

namespace Connected.Storage.PostgreSql.Query;

/// <summary>
/// Provides PostgreSQL specific query translation and expression formatting capabilities.
/// </summary>
/// <remarks>
/// This sealed class extends <see cref="Linguist"/> to handle PostgreSQL-specific query translation patterns,
/// including ORDER BY optimization, skip/take conversion to LIMIT/OFFSET patterns, and PostgreSQL formatting.
/// It orchestrates the translation pipeline for converting LINQ expressions to executable PostgreSQL queries.
/// Unlike SQL Server which uses ROW_NUMBER() for pagination, PostgreSQL natively supports LIMIT and OFFSET
/// clauses which provide more efficient query execution.
/// </remarks>
internal sealed class PostgreSqlLinguist(ExpressionCompilationContext context, PostgreSqlLanguage language, Translator translator)
	: Linguist(context, language, translator)
{
	/// <summary>
	/// Translates a LINQ expression tree into a PostgreSQL compatible expression.
	/// </summary>
	/// <param name="expression">The expression tree to translate.</param>
	/// <returns>A translated expression optimized for PostgreSQL execution.</returns>
	/// <remarks>
	/// The translation process applies multiple optimization passes:
	/// 1. Rewrites ORDER BY clauses for PostgreSQL compatibility
	/// 2. Applies base translation logic
	/// 3. Converts Skip/Take operations to LIMIT/OFFSET patterns (PostgreSQL native)
	/// 4. Re-optimizes ORDER BY clauses after transformation
	/// This ensures efficient pagination and sorting in PostgreSQL using native constructs.
	/// </remarks>
	public override Expression Translate(Expression expression)
	{
		/*
		 * First pass: optimize any ORDER BY clauses for PostgreSQL
		 */
		expression = OrderByRewriter.Rewrite(Language, expression);

		expression = base.Translate(expression);

		/*
		 * Convert Skip/Take patterns to LIMIT/OFFSET for pagination
		 * PostgreSQL natively supports these clauses without ROW_NUMBER()
		 */
		expression = SkipToRowNumberRewriter.Rewrite(Language, expression);

		/*
		 * Final pass: re-optimize ORDER BY clauses after transformation
		 */
		expression = OrderByRewriter.Rewrite(Language, expression);

		return expression;
	}

	/// <summary>
	/// Formats the translated expression into a PostgreSQL query string.
	/// </summary>
	/// <param name="expression">The expression to format.</param>
	/// <returns>A PostgreSQL query string representation of the expression.</returns>
	public override string Format(Expression expression)
	{
		return PostgreSqlFormatter.Format(Context, expression, Language);
	}
}
