using Connected.Data.Expressions.Translation;
using System.Linq.Expressions;

namespace Connected.Data.Expressions.Expressions;

public sealed class AggregateSubqueryExpression(Alias groupByAlias, Expression aggregateInGroupSelect, ScalarExpression aggregateAsSubquery)
		: DatabaseExpression(DatabaseExpressionType.AggregateSubquery, aggregateAsSubquery.Type)
{
	public Alias GroupByAlias { get; } = groupByAlias;
	public Expression AggregateInGroupSelect { get; } = aggregateInGroupSelect;
	public ScalarExpression AggregateAsSubquery { get; } = aggregateAsSubquery;
}
