using System.Linq.Expressions;

namespace Connected.Data.Expressions.Translation;

internal sealed class GroupByDescriptor
{
	public GroupByDescriptor(Alias alias, Expression element)
	{
		Alias = alias;
		Element = element;
	}

	public Alias Alias { get; }
	public Expression Element { get; }
}
