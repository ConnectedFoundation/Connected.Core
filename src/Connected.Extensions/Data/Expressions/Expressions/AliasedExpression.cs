using Connected.Data.Expressions.Translation;

namespace Connected.Data.Expressions.Expressions;

public abstract class AliasedExpression(DatabaseExpressionType nodeType, Type type, Alias alias)
		: DatabaseExpression(nodeType, type)
{
	public Alias Alias { get; } = alias;
}
