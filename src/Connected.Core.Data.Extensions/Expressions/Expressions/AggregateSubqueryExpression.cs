using System.Linq.Expressions;
using Connected.Data.Expressions.Translation;

namespace Connected.Data.Expressions.Expressions;

public sealed class AggregateSubqueryExpression : DatabaseExpression
{
	public AggregateSubqueryExpression(Alias groupByAlias, Expression aggregateInGroupSelect, ScalarExpression aggregateAsSubquery)
		  : base(DatabaseExpressionType.AggregateSubquery, aggregateAsSubquery.Type)
	{
		AggregateInGroupSelect = aggregateInGroupSelect;
		GroupByAlias = groupByAlias;
		AggregateAsSubquery = aggregateAsSubquery;
	}

	public Alias GroupByAlias { get; }
	public Expression AggregateInGroupSelect { get; }
	public ScalarExpression AggregateAsSubquery { get; }
}
