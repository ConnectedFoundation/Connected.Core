using System.Linq.Expressions;
using System.Collections.Generic;
using Connected.Data.Expressions.Visitors;
using Connected.Data.Expressions.Expressions;

namespace Connected.Data.Expressions.Translation.Resolvers;

internal sealed class ReferencedAliasesResolver : DatabaseVisitor
{
	private ReferencedAliasesResolver()
	{
		Aliases = new();
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
