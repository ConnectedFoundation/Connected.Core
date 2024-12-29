using Connected.Data.Expressions;
using Connected.Data.Expressions.Languages;
using Connected.Data.Expressions.Translation;
using Connected.Data.Expressions.Translation.Rewriters;
using System.Linq.Expressions;

namespace Connected.Storage.Sql.Query;

internal sealed class TSqlLinguist(ExpressionCompilationContext context, TSqlLanguage language, Translator translator)
	: Linguist(context, language, translator)
{
	public override Expression Translate(Expression expression)
	{
		/*
	  * fix up any order-by's
	  */
		expression = OrderByRewriter.Rewrite(Language, expression);

		expression = base.Translate(expression);
		/*
	  * convert skip/take info into RowNumber pattern
	  */
		expression = SkipToRowNumberRewriter.Rewrite(Language, expression);
		/*
	  * fix up any order-by's we may have changed
	  */
		expression = OrderByRewriter.Rewrite(Language, expression);

		return expression;
	}

	public override string Format(Expression expression)
	{
		return TSqlFormatter.Format(Context, expression, Language);
	}
}