using Connected.Data.Expressions.Expressions;
using Connected.Data.Expressions.Visitors;
using System.Linq.Expressions;

namespace Connected.Data.Expressions.Translation.Rewriters;

public class ParameterRewriter : DatabaseVisitor
{
	private ParameterRewriter(ExpressionCompilationContext context)
	{
		Context = context;
	}

	private ExpressionCompilationContext Context { get; }

	public static Expression Rewrite(ExpressionCompilationContext context, Expression expression)
	{
		return new ParameterRewriter(context).Visit(expression);
	}

	protected override Expression VisitBinary(BinaryExpression expression)
	{
		if (expression.Left is ColumnExpression column && expression.Right is ConstantExpression constant)
		{
			if (Context.Variables.TryGetValue(column.Name, out List<object?>? existing) && existing is not null)
				existing.Add(constant.Value);
			else
				Context.Variables.Add(column.Name, [constant.Value]);
		}

		return base.VisitBinary(expression);
	}
	// protected override Expression VisitConstant(ConstantExpression expression)
	// {
	// 	var parameter = Context.Parameters.FirstOrDefault(f => f.Value == expression);

	// 	if (parameter.Value is not null)
	// 		return Expression.Constant($"@{parameter.Key}");

	// 	return base.VisitConstant(expression);
	// }
}
