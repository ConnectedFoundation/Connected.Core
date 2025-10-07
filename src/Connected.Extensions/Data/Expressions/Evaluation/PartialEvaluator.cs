using Connected.Data.Expressions;
using System;
using System.Linq.Expressions;

namespace Connected.Data.Expressions.Evaluation;

internal sealed class PartialEvaluator
{
	public static Expression Eval(ExpressionCompilationContext context, Expression expression)
	{
		return Eval(context, expression, null);
	}

	public static Expression Eval(ExpressionCompilationContext context, Expression expression, Func<ConstantExpression, Expression>? fnPostEval)
	{
		return SubtreeEvaluator.Eval(context, ColumnNominator.Nominate(expression), fnPostEval, expression);
	}
}
