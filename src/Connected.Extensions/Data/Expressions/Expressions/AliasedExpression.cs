using Connected.Data.Expressions.Translation;
using System;

namespace Connected.Data.Expressions.Expressions;

public abstract class AliasedExpression : DatabaseExpression
{
	protected AliasedExpression(DatabaseExpressionType nodeType, Type type, Alias alias)
		 : base(nodeType, type)
	{
		Alias = alias;
	}

	public Alias Alias { get; }
}
