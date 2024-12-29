using System.Linq.Expressions;
using System.Reflection;
using System;
using ExpressionVisitor = Connected.Data.Expressions.Visitors.ExpressionVisitor;

namespace Connected.Data.Expressions.Evaluation;

public sealed class SubtreeResolver : ExpressionVisitor
{
	private SubtreeResolver(Type type)
	{
		Type = type;
	}

	private Type Type { get; }
	private Expression Found { get; set; }

	public static Expression? Resolve(Expression expression, Type type)
	{
		var finder = new SubtreeResolver(type);

		finder.Visit(expression);

		return finder.Found;
	}

	protected override Expression? Visit(Expression? exp)
	{
		var node = base.Visit(exp);

		if (Found is null && node is not null && Type.GetTypeInfo().IsAssignableFrom(node.Type.GetTypeInfo()))
			Found = node;

		return node;
	}
}