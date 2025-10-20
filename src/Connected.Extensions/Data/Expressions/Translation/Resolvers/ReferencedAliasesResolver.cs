using Connected.Data.Expressions.Expressions;
using Connected.Data.Expressions.Visitors;
using System.Linq.Expressions;

namespace Connected.Data.Expressions.Translation.Resolvers;

internal sealed class ReferencedAliasesResolver
	: DatabaseVisitor
{
	private ReferencedAliasesResolver()
	{
		Aliases = [];
	}

	private HashSet<Alias> Aliases { get; }

	public static HashSet<Alias> Resolve(Expression source)
	{
		var resolver = new ReferencedAliasesResolver();

		resolver.Visit(source);

		return resolver.Aliases;
	}

	protected override Expression VisitColumn(ColumnExpression column)
	{
		Aliases.Add(column.Alias);

		return column;
	}
}
