using Connected.Data.Expressions.Reflection;
using Connected.Reflection;
using System.Linq.Expressions;
using System.Reflection;
using ExpressionVisitor = Connected.Data.Expressions.Visitors.ExpressionVisitor;

namespace Connected.Data.Expressions.Evaluation;

internal sealed class SubtreeEvaluator
	: ExpressionVisitor
{
	private SubtreeEvaluator(ExpressionCompilationContext context, HashSet<Expression> candidates, Func<ConstantExpression, Expression>? onEval)
	{
		Candidates = candidates;
		OnEval = onEval;
		Context = context;
	}

	public ExpressionCompilationContext Context { get; }
	private HashSet<Expression> Candidates { get; set; }
	private Func<ConstantExpression, Expression>? OnEval { get; set; }

	internal static Expression Eval(ExpressionCompilationContext context, HashSet<Expression> candidates, Func<ConstantExpression, Expression>? onEval, Expression exp)
	{
		if (new SubtreeEvaluator(context, candidates, onEval).Visit(exp) is not Expression subtreeExpression)
			throw new NullReferenceException(nameof(subtreeExpression));

		return subtreeExpression;
	}

	protected override Expression Visit(Expression exp)
	{
		if (Candidates.Contains(exp))
			return Evaluate(exp);

		return base.Visit(exp);
	}

	protected override Expression VisitConditional(ConditionalExpression c)
	{
		if (Candidates.Contains(c.Test))
		{
			var test = Evaluate(c.Test);

			if (test is ConstantExpression expression && expression.Type == typeof(bool))
			{
				if ((bool)(expression.Value ?? false))
					return Visit(c.IfTrue);
				else
					return Visit(c.IfFalse);
			}
		}

		return base.VisitConditional(c);
	}

	private Expression PostEval(ConstantExpression e)
	{
		if (OnEval is not null)
			return OnEval(e);

		return e;
	}

	private Expression Evaluate(Expression e)
	{
		var type = e.Type;

		if (e.NodeType == ExpressionType.Convert)
		{
			var u = (UnaryExpression)e;

			if (Nullables.GetNonNullableType(u.Operand.Type) == Nullables.GetNonNullableType(type))
				e = ((UnaryExpression)e).Operand;
		}

		if (e.NodeType == ExpressionType.Constant)
		{
			if (e.Type == type)
				return e;
			else if (Nullables.GetNonNullableType(e.Type) == Nullables.GetNonNullableType(type))
				return Expression.Constant(((ConstantExpression)e).Value, type);
		}

		if (e is MemberExpression me)
		{
			if (me.Expression is ConstantExpression ce)
			{
				var value = ce.Value is null ? null : me.Member.GetValue(ce.Value);
				var constant = Expression.Constant(value, type);

				Context.Parameters.Add(me.Member.Name, constant);

				return PostEval(constant);
			}
		}

		if (type.GetTypeInfo().IsValueType)
			e = Expression.Convert(e, typeof(object));

		var lambda = Expression.Lambda<Func<object>>(e);
		var fn = lambda.Compile();

		return PostEval(Expression.Constant(fn(), type));
	}
}