using System.Linq.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using Connected.Data.Expressions.Expressions;
using Connected.Data.Expressions.Visitors;
using Connected.Data.Expressions.Translation;

namespace Connected.Data.Expressions.Translation.Optimization;

internal sealed class Subqueries : DatabaseVisitor
{
	private Subqueries(IEnumerable<SelectExpression> selectsToRemove)
	{
		SelectsToRemove = new HashSet<SelectExpression>(selectsToRemove);
		Map = SelectsToRemove.ToDictionary(d => d.Alias, d => d.Columns.ToDictionary(d2 => d2.Name, d2 => d2.Expression));
	}

	private HashSet<SelectExpression> SelectsToRemove { get; set; }
	private Dictionary<Alias, Dictionary<string, Expression>> Map { get; set; }

	public static SelectExpression Remove(SelectExpression outerSelect, params SelectExpression[] selectsToRemove)
	{
		return Remove(outerSelect, (IEnumerable<SelectExpression>)selectsToRemove);
	}

	public static SelectExpression Remove(SelectExpression outerSelect, IEnumerable<SelectExpression> selectsToRemove)
	{
		if (new Subqueries(selectsToRemove).Visit(outerSelect) is not SelectExpression selectRemoveExpression)
			throw new NullReferenceException(nameof(selectRemoveExpression));

		return selectRemoveExpression;
	}

	public static ProjectionExpression Remove(ProjectionExpression projection, params SelectExpression[] selectsToRemove)
	{
		return Remove(projection, (IEnumerable<SelectExpression>)selectsToRemove);
	}

	public static ProjectionExpression Remove(ProjectionExpression projection, IEnumerable<SelectExpression> selectsToRemove)
	{
		if (new Subqueries(selectsToRemove).Visit(projection) is not ProjectionExpression projectionRemoveExpression)
			throw new NullReferenceException(nameof(projectionRemoveExpression));

		return projectionRemoveExpression;
	}

	protected override Expression VisitSelect(SelectExpression expression)
	{
		if (SelectsToRemove.Contains(expression))
		{
			if (Visit(expression.From) is not Expression fromExpression)
				throw new NullReferenceException(nameof(fromExpression));

			return fromExpression;
		}
		else
			return base.VisitSelect(expression);
	}

	protected override Expression VisitColumn(ColumnExpression expression)
	{
		if (Map.TryGetValue(expression.Alias, out Dictionary<string, Expression>? nameMap))
		{
			if (nameMap.TryGetValue(expression.Name, out Expression? expr))
			{
				if (Visit(expr) is not Expression columnExpression)
					throw new NullReferenceException(nameof(columnExpression));

				return columnExpression;
			}

			throw new NullReferenceException($"Reference to undefined column ({expression.Name})");
		}

		return expression;
	}
}