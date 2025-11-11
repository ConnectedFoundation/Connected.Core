using Connected.Data.Expressions.Collections;
using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace Connected.Data.Expressions.Expressions;

public sealed class ClientJoinExpression(ProjectionExpression projection, IEnumerable<Expression> outerKey, IEnumerable<Expression> innerKey)
		: DatabaseExpression(DatabaseExpressionType.ClientJoin, projection.Type)
{
	public ReadOnlyCollection<Expression> OuterKey { get; } = outerKey.ToReadOnly();
	public ReadOnlyCollection<Expression> InnerKey { get; } = innerKey.ToReadOnly();
	public ProjectionExpression Projection { get; } = projection;
}