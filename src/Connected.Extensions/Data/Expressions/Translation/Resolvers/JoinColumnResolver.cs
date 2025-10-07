using System.Linq.Expressions;
using System.Collections.Generic;
using Connected.Data.Expressions.Expressions;

namespace Connected.Data.Expressions.Translation.Resolvers;

internal sealed class JoinColumnResolver
{
	private JoinColumnResolver(HashSet<Alias> aliases)
	{
		Aliases = aliases;
		Columns = new HashSet<ColumnExpression>();
	}

	private HashSet<Alias> Aliases { get; }
	private HashSet<ColumnExpression> Columns { get; }

	public static HashSet<ColumnExpression> Resolve(HashSet<Alias> aliases, SelectExpression select)
	{
		var resolver = new JoinColumnResolver(aliases);

		resolver.Resolve(select.Where);

		return resolver.Columns;
	}

	private void Resolve(Expression? expression)
	{
		if (expression is BinaryExpression b)
		{
			switch (b.NodeType)
			{
				case ExpressionType.Equal:
				case ExpressionType.NotEqual:

					if (IsExternalColumn(b.Left) && GetColumn(b.Right) is not null)
					{
						if (GetColumn(b.Right) is ColumnExpression right)
							Columns.Add(right);
					}
					else if (IsExternalColumn(b.Right) && GetColumn(b.Left) is not null)
					{
						if (GetColumn(b.Left) is ColumnExpression left)
							Columns.Add(left);
					}

					break;
				case ExpressionType.And:
				case ExpressionType.AndAlso:

					if (b.Type == typeof(bool) || b.Type == typeof(bool?))
					{
						Resolve(b.Left);
						Resolve(b.Right);
					}

					break;
			}
		}
	}

	private static ColumnExpression? GetColumn(Expression exp)
	{
		while (exp.NodeType == ExpressionType.Convert)
			exp = ((UnaryExpression)exp).Operand;

		return exp as ColumnExpression;
	}

	private bool IsExternalColumn(Expression exp)
	{
		var col = GetColumn(exp);

		if (col is not null && !Aliases.Contains(col.Alias))
			return true;

		return false;
	}
}