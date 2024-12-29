using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Collections.Generic;
using Connected.Data.Expressions.Collections;

namespace Connected.Data.Expressions.Mappings;

internal class ConstructorBindResult
{
	public ConstructorBindResult(NewExpression expression, IEnumerable<EntityAssignment> remaining)
	{
		Expression = expression;
		Remaining = remaining.ToReadOnly();
	}

	public NewExpression Expression { get; }
	public ReadOnlyCollection<EntityAssignment> Remaining { get; }
}
