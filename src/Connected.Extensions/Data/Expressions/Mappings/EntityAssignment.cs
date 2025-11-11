using System.Linq.Expressions;

namespace Connected.Data.Expressions.Mappings;

internal sealed class EntityAssignment(MemberMapping mapping, Expression expression)
{
	public MemberMapping Mapping { get; } = mapping;
	public Expression Expression { get; } = expression;
}
