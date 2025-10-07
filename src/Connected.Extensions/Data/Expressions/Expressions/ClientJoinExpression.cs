using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq.Expressions;
using Connected.Data.Expressions.Collections;

namespace Connected.Data.Expressions.Expressions;

public sealed class ClientJoinExpression : DatabaseExpression
{
	public ClientJoinExpression(ProjectionExpression projection, IEnumerable<Expression> outerKey, IEnumerable<Expression> innerKey)
		  : base(DatabaseExpressionType.ClientJoin, projection.Type)
	{
		OuterKey = outerKey.ToReadOnly();
		InnerKey = innerKey.ToReadOnly();
		Projection = projection;
	}

	public ReadOnlyCollection<Expression> OuterKey { get; }
	public ReadOnlyCollection<Expression> InnerKey { get; }
	public ProjectionExpression Projection { get; }
}