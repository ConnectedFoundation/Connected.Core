using System.Linq.Expressions;
using System.Collections.Generic;
using Connected.Data.Expressions.Expressions;
using Connected.Data.Expressions.Visitors;

namespace Connected.Data.Expressions.Translation.Resolvers;

internal sealed class AggregateResolver : DatabaseVisitor
{
	private AggregateResolver()
	{
		Aggregates = new();
	}

	private List<AggregateSubqueryExpression> Aggregates { get; }

	internal static List<AggregateSubqueryExpression> Resolve(Expression expression)
	{
		var resolver = new AggregateResolver();

		resolver.Visit(expression);

		return resolver.Aggregates;
	}

	protected override Expression VisitAggregateSubquery(AggregateSubqueryExpression aggregate)
	{
		Aggregates.Add(aggregate);

		return base.VisitAggregateSubquery(aggregate);
	}
}