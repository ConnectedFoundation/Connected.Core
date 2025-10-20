using Connected.Data.Expressions.Expressions;
using Connected.Data.Expressions.Translation.Resolvers;
using Connected.Data.Expressions.Visitors;
using System.Linq.Expressions;

namespace Connected.Data.Expressions.Translation.Rewriters;

public sealed class AggregateRewriter
	: DatabaseVisitor
{
	private AggregateRewriter(ExpressionCompilationContext context, Expression expr)
	{
		Context = context;

		Map = [];
		Lookup = AggregateResolver.Resolve(expr).ToLookup(a => a.GroupByAlias);
	}

	private ExpressionCompilationContext Context { get; set; }
	private ILookup<Alias, AggregateSubqueryExpression> Lookup { get; set; }
	private Dictionary<AggregateSubqueryExpression, Expression> Map { get; set; }

	public static Expression Rewrite(ExpressionCompilationContext context, Expression expr)
	{
		if (new AggregateRewriter(context, expr).Visit(expr) is not Expression aggregateExpression)
			throw new NullReferenceException(nameof(aggregateExpression));

		return aggregateExpression;
	}

	protected override Expression VisitSelect(SelectExpression select)
	{
		select = (SelectExpression)base.VisitSelect(select);

		if (Lookup.Contains(select.Alias))
		{
			var aggColumns = new List<ColumnDeclaration>(select.Columns);

			foreach (AggregateSubqueryExpression ae in Lookup[select.Alias])
			{
				var name = $"agg{aggColumns.Count}";
				var colType = Context.Language.TypeSystem.ResolveColumnType(ae.Type);
				var cd = new ColumnDeclaration(name, ae.AggregateInGroupSelect, colType);

				Map.Add(ae, new ColumnExpression(ae.Type, colType, ae.GroupByAlias, name));

				aggColumns.Add(cd);
			}

			return new SelectExpression(select.Alias, aggColumns, select.From, select.Where, select.OrderBy, select.GroupBy, select.IsDistinct, select.Skip, select.Take, select.IsReverse);
		}

		return select;
	}

	protected override Expression VisitAggregateSubquery(AggregateSubqueryExpression expression)
	{
		if (Map.TryGetValue(expression, out Expression? mapped))
			return mapped;

		return Visit(expression.AggregateAsSubquery) ?? throw new NullReferenceException();
	}
}