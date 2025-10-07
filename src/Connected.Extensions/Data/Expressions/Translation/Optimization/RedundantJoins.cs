using Connected.Data.Expressions.Collections;
using Connected.Data.Expressions.Comparers;
using Connected.Data.Expressions.Expressions;
using Connected.Data.Expressions.Visitors;
using System.Linq.Expressions;

namespace Connected.Data.Expressions.Translation.Optimization;

internal sealed class RedundantJoins : DatabaseVisitor
{
	private RedundantJoins()
	{
		Map = new Dictionary<Alias, Alias>();
	}
	private Dictionary<Alias, Alias> Map { get; }

	public static Expression Remove(Expression expression)
	{
		if (new RedundantJoins().Visit(expression) is not Expression redundantJoinExpression)
			throw new NullReferenceException(nameof(redundantJoinExpression));

		return redundantJoinExpression;
	}

	protected override Expression VisitJoin(JoinExpression expression)
	{
		var result = base.VisitJoin(expression);

		var joinExpression = result as JoinExpression;

		if (joinExpression is not null)
		{
			var right = joinExpression.Right as AliasedExpression;

			if (right is not null)
			{
				var similarRight = FindSimilarRight(expression.Left as JoinExpression, joinExpression) as AliasedExpression;

				if (similarRight is not null)
				{
					Map.Add(right.Alias, similarRight.Alias);

					return joinExpression.Left;
				}
			}
		}

		return result;
	}

	private Expression? FindSimilarRight(JoinExpression? join, JoinExpression compareTo)
	{
		if (join is null)
			return null;

		if (join.Join == compareTo.Join)
		{
			if (join.Right.NodeType == compareTo.Right.NodeType && DatabaseComparer.AreEqual(join.Right, compareTo.Right))
			{
				if (join.Condition == compareTo.Condition)
					return join.Right;

				var scope = new ScopedDictionary<Alias, Alias>(null);

				scope.Add(((AliasedExpression)join.Right).Alias, ((AliasedExpression)compareTo.Right).Alias);

				if (DatabaseComparer.AreEqual(null, scope, join.Condition, compareTo.Condition))
					return join.Right;
			}
		}

		var result = FindSimilarRight(join.Left as JoinExpression, compareTo);

		result ??= FindSimilarRight(join.Right as JoinExpression, compareTo);

		return result;
	}

	protected override Expression VisitColumn(ColumnExpression column)
	{
		if (Map.TryGetValue(column.Alias, out Alias? mapped))
			return new ColumnExpression(column.Type, column.QueryType, mapped, column.Name);

		return column;
	}
}
