using Connected.Data.Expressions;
using Connected.Data.Expressions.Languages;
using Connected.Data.Expressions.Translation;
using Connected.Data.Expressions.Translation.Rewriters;
using System.Linq.Expressions;

namespace Connected.Storage.Sql.Query;

/// <summary>
/// Provides T-SQL specific query translation and expression formatting capabilities.
/// </summary>
/// <remarks>
/// This sealed class extends <see cref="Linguist"/> to handle SQL Server-specific query translation patterns,
/// including ORDER BY optimization, skip/take conversion to ROW_NUMBER patterns, and T-SQL formatting.
/// It orchestrates the translation pipeline for converting LINQ expressions to executable T-SQL queries.
/// </remarks>
internal sealed class TSqlLinguist(ExpressionCompilationContext context, TSqlLanguage language, Translator translator)
	: Linguist(context, language, translator)
{
	/// <summary>
	/// Translates a LINQ expression tree into a T-SQL compatible expression.
	/// </summary>
	/// <param name="expression">The expression tree to translate.</param>
	/// <returns>A translated expression optimized for T-SQL execution.</returns>
	/// <remarks>
	/// The translation process applies multiple optimization passes:
	/// 1. Rewrites ORDER BY clauses for T-SQL compatibility
	/// 2. Applies base translation logic
	/// 3. Converts Skip/Take operations to ROW_NUMBER patterns
	/// 4. Re-optimizes ORDER BY clauses after transformation
	/// This ensures efficient pagination and sorting in SQL Server.
	/// </remarks>
	public override Expression Translate(Expression expression)
	{
		/*
		 * First pass: optimize any ORDER BY clauses for T-SQL
		 */
		expression = OrderByRewriter.Rewrite(Language, expression);

		expression = base.Translate(expression);

		/*
		 * Convert Skip/Take patterns to ROW_NUMBER() OVER (ORDER BY ...) for pagination
		 */
		expression = SkipToRowNumberRewriter.Rewrite(Language, expression);

		/*
		 * Final pass: re-optimize ORDER BY clauses after ROW_NUMBER transformation
		 */
		expression = OrderByRewriter.Rewrite(Language, expression);

		return expression;
	}

	/// <summary>
	/// Formats the translated expression into a T-SQL query string.
	/// </summary>
	/// <param name="expression">The expression to format.</param>
	/// <returns>A T-SQL query string representation of the expression.</returns>
	public override string Format(Expression expression)
	{
		return TSqlFormatter.Format(Context, expression, Language);
	}
}