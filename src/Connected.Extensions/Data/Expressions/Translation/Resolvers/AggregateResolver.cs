using Connected.Data.Expressions.Expressions;
using Connected.Data.Expressions.Visitors;
using System.Linq.Expressions;

namespace Connected.Data.Expressions.Translation.Resolvers;

internal sealed class AggregateResolver
	: DatabaseVisitor
{
	private AggregateResolver()
	{
		Aggregates = [];
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