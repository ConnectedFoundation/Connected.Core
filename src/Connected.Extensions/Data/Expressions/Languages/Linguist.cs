using Connected.Data.Expressions.Formatters;
using Connected.Data.Expressions.Translation;
using Connected.Data.Expressions.Translation.Optimization;
using Connected.Data.Expressions.Translation.Rewriters;
using System.Linq.Expressions;

namespace Connected.Data.Expressions.Languages;

/// <summary>
/// Provides language-specific translation and formatting behavior for query expressions.
/// </summary>
/// <remarks>
/// A <see cref="Linguist"/> is responsible for performing language-aware rewrites,
/// optimizations and formatting of expression trees. Concrete query languages may
/// subclass this type to apply additional transformations or to change the
/// formatting behavior used by the translator.
/// </remarks>
public class Linguist(ExpressionCompilationContext context, QueryLanguage language, Translator translator)
{
	/// <summary>
	/// Gets the compilation context which contains parameters and variables used
	/// during translation.
	/// </summary>
	protected ExpressionCompilationContext Context { get; } = context;

	/// <summary>
	/// Gets the query language instance that provides type system and language rules.
	/// </summary>
	public QueryLanguage Language { get; } = language;

	/// <summary>
	/// Gets the owning translator which coordinates mapping, policing and other
	/// translation phases.
	/// </summary>
	public Translator Translator { get; } = translator;

	/// <summary>
	/// Provides language specific query translation. Use this to apply language
	/// specific rewrites or to make assertions/validations about the query.
	/// </summary>
	/// <param name="expression">The input expression tree to translate.</param>
	/// <returns>The translated expression tree.</returns>
	public virtual Expression Translate(Expression expression)
	{
		/*
		 * Remove redundant column and subquery layers before performing cross-apply
		 * and join rewrites. These passes reduce complexity of the tree and make
		 * subsequent rewrites more effective.
		 */
		expression = UnusedColumns.Remove(expression);
		expression = RedundantColumns.Remove(expression);
		expression = RedundantSubqueries.Remove(expression);

		/*
		 * Convert cross-apply and outer-apply joins into inner & left-outer joins
		 * where the target language supports them. This may simplify execution
		 * models and improve translation quality.
		 */
		var rewritten = CrossApplyRewriter.Rewrite(Language, expression);

		/*
		 * Convert cross joins into inner joins where possible to simplify the plan.
		 */
		rewritten = CrossJoinRewriter.Rewrite(rewritten);

		if (rewritten != expression)
		{
			expression = rewritten;
			/*
			 * After rewriting, do final reduction passes to remove any redundant
			 * constructs introduced or revealed by the rewrites.
			 */
			expression = UnusedColumns.Remove(expression);
			expression = RedundantSubqueries.Remove(expression);
			expression = RedundantJoins.Remove(expression);
			expression = RedundantColumns.Remove(expression);
		}

		return expression;
	}

	/// <summary>
	/// Converts the query expression into the textual representation for this language.
	/// </summary>
	/// <param name="expression">The expression to format.</param>
	/// <returns>A language-specific textual representation (for example SQL).</returns>
	public virtual string Format(Expression expression)
	{
		/*
		 * Use the default SQL formatter unless a language overrides this method.
		 * Formatters are responsible for turning expression trees into readable
		 * and, where applicable, executable query text.
		 */
		return SqlFormatter.Format(expression);
	}

	/// <summary>
	/// Determine which sub-expressions must be converted into parameters for the
	/// target language and execution environment.
	/// </summary>
	/// <param name="expression">The expression to analyze and parameterize.</param>
	/// <returns>The parameterized expression tree.</returns>
	public virtual Expression Parameterize(Expression expression)
	{
		/*
		 * Delegate parameterization to a shared utility so languages can reuse
		 * consistent rules for deciding what becomes a runtime parameter.
		 */
		return Parameterizer.Parameterize(Language, expression);
	}
}