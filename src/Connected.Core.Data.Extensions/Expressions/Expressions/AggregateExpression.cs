using System;
using System.Linq.Expressions;

namespace Connected.Data.Expressions.Expressions;

public sealed class AggregateExpression : DatabaseExpression
{
	public AggregateExpression(Type type, string aggregateName, Expression argument, bool isDistinct)
		 : base(DatabaseExpressionType.Aggregate, type)
	{
		AggregateName = aggregateName;
		Argument = argument;
		IsDistinct = isDistinct;
	}

	public string AggregateName { get; }
	public Expression Argument { get; }
	public bool IsDistinct { get; }
}