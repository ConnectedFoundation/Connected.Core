using System.Collections.Generic;
using System.Linq.Expressions;
using Connected.Data.Expressions.Serialization;
using Connected.Data.Expressions.Formatters;

namespace Connected.Data.Expressions.Expressions;

public sealed class ProjectionExpression : DatabaseExpression
{
	public ProjectionExpression(SelectExpression source, Expression projector)
		 : this(source, projector, null)
	{
	}

	public ProjectionExpression(SelectExpression source, Expression projector, LambdaExpression? aggregator)
		 : base(DatabaseExpressionType.Projection, aggregator is not null ? aggregator.Body.Type : typeof(IEnumerable<>).MakeGenericType(projector.Type))
	{
		Select = source;
		Projector = projector;
		Aggregator = aggregator;
	}

	public SelectExpression Select { get; }
	public Expression Projector { get; }
	public LambdaExpression? Aggregator { get; }
	public bool IsSingleton => Aggregator?.Body.Type == Projector.Type;
	public string QueryText => SqlFormatter.Format(Select);

	public override string ToString()
	{
		return DatabaseSerializer.Serialize(this);
	}
}