using Connected.Data.Expressions;
using Connected.Data.Expressions.Visitors;
using System.Linq.Expressions;

namespace Connected.Data.Expressions.Translation.Rewriters;

public class WhereClauseRewriter : DatabaseVisitor
{
	protected WhereClauseRewriter(ExpressionCompilationContext context)
	{
		Context = context;
	}

	public ExpressionCompilationContext Context { get; }

	public static Expression Rewrite(ExpressionCompilationContext context, Expression expression)
	{
		return new WhereClauseRewriter(context).Visit(expression);
	}

	protected override Expression VisitWhere(Expression whereExpression)
	{
		return ParameterRewriter.Rewrite(Context, whereExpression);
	}
}
