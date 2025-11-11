using Connected.Data.Expressions.Formatters;
using Connected.Data.Expressions.Serialization;
using System.Linq.Expressions;

namespace Connected.Data.Expressions.Expressions;

public sealed class ProjectionExpression(SelectExpression source, Expression projector, LambdaExpression? aggregator)
		: DatabaseExpression(DatabaseExpressionType.Projection, aggregator is not null ? aggregator.Body.Type : typeof(IEnumerable<>).MakeGenericType(projector.Type))
{
	public ProjectionExpression(SelectExpression source, Expression projector)
		 : this(source, projector, null)
	{
	}

	public SelectExpression Select { get; } = source;
	public Expression Projector { get; } = projector;
	public LambdaExpression? Aggregator { get; } = aggregator;
	public bool IsSingleton => Aggregator?.Body.Type == Projector.Type;
	public string QueryText => SqlFormatter.Format(Select);

	public override string ToString()
	{
		return DatabaseSerializer.Serialize(this);
	}
}