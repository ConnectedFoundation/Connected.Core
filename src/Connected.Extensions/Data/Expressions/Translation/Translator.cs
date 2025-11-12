using Connected.Data.Expressions.Evaluation;
using Connected.Data.Expressions.Languages;
using Connected.Data.Expressions.Translation.Optimization;
using Connected.Data.Expressions.Translation.Rewriters;
using System.Linq.Expressions;

namespace Connected.Data.Expressions.Translation;

public class Translator
{

	/// <summary>
	/// Constructs a new <see cref="Translator"/>.
	/// </summary>
	public Translator(ExpressionCompilationContext context)
	{
		Context = context;
		Linguist = Context.Language.CreateLinguist(context, this);
	}

	public Linguist Linguist { get; }
	public ExpressionCompilationContext Context { get; }

	/// <summary>
	/// Translates a query expression using rules defined by the <see cref="Linguist"/>, <see cref="Mapping"/> and <see cref="Police"/>.
	/// </summary>
	public Expression Translate(Expression expression)
	{
		var result = expression;
		/*
	  * pre-evaluate local sub-trees
	  */
		result = PartialEvaluator.Eval(Context, result);
		/*
	  * apply mapping (binds LINQ operators too)
	  */
		result = Bind(Context, result);
		/*
	  * any language specific translations or validations
	  */
		return Linguist.Translate(result);
	}

	private static Expression Bind(ExpressionCompilationContext context, Expression expression)
	{
		var bound = Binder.Bind(context, expression) ?? throw new NullReferenceException(SR.ErrExpectedExpression);
		var aggmoved = AggregateRewriter.Rewrite(context, bound);
		var reduced = UnusedColumns.Remove(aggmoved);

		reduced = RedundantColumns.Remove(reduced);
		reduced = RedundantSubqueries.Remove(reduced);
		reduced = RedundantJoins.Remove(reduced);

		var rbound = RelationshipBinder.Bind(context, reduced);

		if (rbound != reduced)
		{
			rbound = RedundantColumns.Remove(rbound);
			rbound = RedundantJoins.Remove(rbound);
		}

		var result = ComparisonRewriter.Rewrite(rbound);

		result = WhereClauseRewriter.Rewrite(context, result);

		return result;
	}
}