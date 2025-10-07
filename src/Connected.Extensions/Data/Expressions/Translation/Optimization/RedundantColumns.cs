using System.Collections;
using System.Linq.Expressions;
using System.Collections.Generic;
using System;
using System.Linq;
using Connected.Data.Expressions.Expressions;
using Connected.Data.Expressions.Visitors;

namespace Connected.Data.Expressions.Translation.Optimization;

internal sealed class RedundantColumns : DatabaseVisitor
{
	private RedundantColumns()
	{
		Map = new();
	}

	private Dictionary<ColumnExpression, ColumnExpression> Map { get; set; }

	public static Expression Remove(Expression expression)
	{
		if (new RedundantColumns().Visit(expression) is not Expression redundantColumnsExpression)
			throw new NullReferenceException(nameof(redundantColumnsExpression));

		return redundantColumnsExpression;
	}

	protected override Expression VisitColumn(ColumnExpression column)
	{
		if (Map.TryGetValue(column, out ColumnExpression? mapped))
			return mapped;

		return column;
	}

	protected override Expression VisitSelect(SelectExpression select)
	{
		select = (SelectExpression)base.VisitSelect(select);

		var cols = select.Columns.OrderBy(c => c.Name).ToList();
		var removed = new BitArray(select.Columns.Count);
		var anyRemoved = false;

		for (var i = 0; i < cols.Count - 1; i++)
		{
			var ci = cols[i];
			var cix = ci.Expression as ColumnExpression;
			var qt = cix is not null ? cix.QueryType : ci.DataType;
			var cxi = new ColumnExpression(ci.Expression.Type, qt, select.Alias, ci.Name);

			for (var j = i + 1; j < cols.Count; j++)
			{
				if (!removed.Get(j))
				{
					var cj = cols[j];

					if (SameExpression(ci.Expression, cj.Expression))
					{
						var cxj = new ColumnExpression(cj.Expression.Type, qt, select.Alias, cj.Name);

						Map.Add(cxj, cxi);

						removed.Set(j, true);
						anyRemoved = true;
					}
				}
			}
		}

		if (anyRemoved)
		{
			var newDecls = new List<ColumnDeclaration>();

			for (var i = 0; i < cols.Count; i++)
			{
				if (!removed.Get(i))
					newDecls.Add(cols[i]);
			}

			select = select.SetColumns(newDecls);
		}

		return select;
	}

	private static bool SameExpression(Expression a, Expression b)
	{
		if (a == b)
			return true;

		var ca = a as ColumnExpression;
		var cb = b as ColumnExpression;

		return ca is not null && cb is not null && ca.Alias == cb.Alias && ca.Name == cb.Name;
	}
}