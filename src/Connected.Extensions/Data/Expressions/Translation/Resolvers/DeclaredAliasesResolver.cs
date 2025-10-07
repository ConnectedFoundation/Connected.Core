using System.Linq.Expressions;
using System.Collections.Generic;
using Connected.Data.Expressions.Expressions;
using Connected.Data.Expressions.Visitors;

namespace Connected.Data.Expressions.Translation.Resolvers;

internal sealed class DeclaredAliasesResolver : DatabaseVisitor
{
	private DeclaredAliasesResolver()
	{
		Aliases = new();
	}

	private HashSet<Alias> Aliases { get; set; }

	public static HashSet<Alias> Resolve(Expression source)
	{
		var resolver = new DeclaredAliasesResolver();

		resolver.Visit(source);

		return resolver.Aliases;
	}

	protected override Expression VisitSelect(SelectExpression select)
	{
		Aliases.Add(select.Alias);

		return select;
	}

	protected override Expression VisitTable(TableExpression table)
	{
		Aliases.Add(table.Alias);

		return table;
	}
}
