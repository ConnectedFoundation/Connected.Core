using Connected.Data.Expressions.Collections;
using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace Connected.Data.Expressions.Mappings;

internal class ConstructorBindResult(NewExpression expression, IEnumerable<EntityAssignment> remaining)
{
	public NewExpression Expression { get; } = expression;
	public ReadOnlyCollection<EntityAssignment> Remaining { get; } = remaining.ToReadOnly();
}
