using System.Linq.Expressions;

namespace Connected.Data.Expressions.Translation;

internal sealed class GroupByDescriptor(Alias alias, Expression element)
{
	public Alias Alias { get; } = alias;
	public Expression Element { get; } = element;
}
