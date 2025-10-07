using System.Linq.Expressions;
using Connected.Data.Expressions.Translation.Rewriters;
using Connected.Data.Expressions.Translation.Optimization;
using Connected.Data.Expressions.Formatters;
using Connected.Data.Expressions.Translation;

namespace Connected.Data.Expressions.Languages;

public class Linguist
{
	/// <summary>
	/// Construct a <see cref="Linguist"/>
	/// </summary>
	public Linguist(ExpressionCompilationContext context, QueryLanguage language, Translator translator)
	{
		Context = context;
		Language = language;
		Translator = translator;
	}

	protected ExpressionCompilationContext Context { get; }
	public QueryLanguage Language { get; }
	public Translator Translator { get; }
	/// <summary>
	/// Provides language specific query translation.  Use this to apply language specific rewrites or
	/// to make assertions/validations about the query.
	/// </summary>
	public virtual Expression Translate(Expression expression)
	{
		/*
	  * remove redundant layers again before cross apply rewrite
	  */
		expression = UnusedColumns.Remove(expression);
		expression = RedundantColumns.Remove(expression);
		expression = RedundantSubqueries.Remove(expression);
		/*
	  * convert cross-apply and outer-apply joins into inner & left-outer-joins if possible
	  */
		var rewritten = CrossApplyRewriter.Rewrite(Language, expression);
		/*
	  * convert cross joins into inner joins
	  */
		rewritten = CrossJoinRewriter.Rewrite(rewritten);

		if (rewritten != expression)
		{
			expression = rewritten;
			/*
		 * do final reduction
		 */
			expression = UnusedColumns.Remove(expression);
			expression = RedundantSubqueries.Remove(expression);
			expression = RedundantJoins.Remove(expression);
			expression = RedundantColumns.Remove(expression);
		}

		return expression;
	}
	/// <summary>
	/// Converts the query expression into text of this query language
	/// </summary>
	public virtual string Format(Expression expression)
	{
		/*
	  * use common SQL formatter by default
	  */
		return SqlFormatter.Format(expression);
	}
	/// <summary>
	/// Determine which sub-expressions must be parameters
	/// </summary>
	public virtual Expression Parameterize(Expression expression)
	{
		return Parameterizer.Parameterize(Language, expression);
	}
}