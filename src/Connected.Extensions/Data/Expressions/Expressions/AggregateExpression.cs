using System.Linq.Expressions;

namespace Connected.Data.Expressions.Expressions;

public sealed class AggregateExpression(Type type, string aggregateName, Expression? argument, bool isDistinct)
		: DatabaseExpression(DatabaseExpressionType.Aggregate, type)
{
	public string AggregateName { get; } = aggregateName;
	public Expression? Argument { get; } = argument;
	public bool IsDistinct { get; } = isDistinct;
}