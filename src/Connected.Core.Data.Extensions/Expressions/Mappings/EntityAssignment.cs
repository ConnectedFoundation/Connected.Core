using System.Linq.Expressions;

namespace Connected.Data.Expressions.Mappings;

internal sealed class EntityAssignment
{
	public EntityAssignment(MemberMapping mapping, Expression expression)
	{
		Mapping = mapping;
		Expression = expression;
	}

	public MemberMapping Mapping { get; }
	public Expression Expression { get; }
}
