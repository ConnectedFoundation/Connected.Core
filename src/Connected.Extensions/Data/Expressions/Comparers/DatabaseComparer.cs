using Connected.Data.Expressions.Collections;
using Connected.Data.Expressions.Expressions;
using Connected.Data.Expressions.Translation;
using Connected.Data.Expressions.Translation.Resolvers;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using BlockExpression = Connected.Data.Expressions.Expressions.BlockExpression;

namespace Connected.Data.Expressions.Comparers;

internal sealed class DatabaseComparer : ExpressionComparer
{
	private DatabaseComparer(ScopedDictionary<ParameterExpression, ParameterExpression>? parameterScope, Func<object?, object?, bool>? comparer,
		 ScopedDictionary<Alias, Alias>? aliasScope)
		 : base(parameterScope, comparer)
	{
		AliasScope = aliasScope;
	}

	private ScopedDictionary<Alias, Alias>? AliasScope { get; set; }
	public new static bool AreEqual(Expression? a, Expression? b)
	{
		return AreEqual(null, null, a, b, null);
	}

	public new static bool AreEqual(Expression? a, Expression? b, Func<object?, object?, bool>? fnCompare)
	{
		return AreEqual(null, null, a, b, fnCompare);
	}

	public static bool AreEqual(ScopedDictionary<ParameterExpression, ParameterExpression>? parameterScope, ScopedDictionary<Alias, Alias>? aliasScope, Expression? a, Expression? b)
	{
		return new DatabaseComparer(parameterScope, null, aliasScope).Compare(a, b);
	}

	public static bool AreEqual(ScopedDictionary<ParameterExpression, ParameterExpression>? parameterScope, ScopedDictionary<Alias, Alias>? aliasScope, Expression? a, Expression? b, Func<object?, object?, bool>? fnCompare)
	{
		return new DatabaseComparer(parameterScope, fnCompare, aliasScope).Compare(a, b);
	}

	protected override bool Compare(Expression? a, Expression? b)
	{
		if (a == b)
			return true;

		if (a is null || b is null)
			return false;

		if (a.NodeType != b.NodeType)
			return false;

		if (a.Type != b.Type)
			return false;

		return (DatabaseExpressionType)a.NodeType switch
		{
			DatabaseExpressionType.Table => CompareTable((TableExpression)a, (TableExpression)b),
			DatabaseExpressionType.Column => CompareColumn((ColumnExpression)a, (ColumnExpression)b),
			DatabaseExpressionType.Select => CompareSelect((SelectExpression)a, (SelectExpression)b),
			DatabaseExpressionType.Join => CompareJoin((JoinExpression)a, (JoinExpression)b),
			DatabaseExpressionType.Aggregate => CompareAggregate((AggregateExpression)a, (AggregateExpression)b),
			DatabaseExpressionType.Scalar or DatabaseExpressionType.Exists or DatabaseExpressionType.In => CompareSubquery((SubqueryExpression)a, (SubqueryExpression)b),
			DatabaseExpressionType.AggregateSubquery => CompareAggregateSubquery((AggregateSubqueryExpression)a, (AggregateSubqueryExpression)b),
			DatabaseExpressionType.IsNull => CompareIsNull((IsNullExpression)a, (IsNullExpression)b),
			DatabaseExpressionType.Between => CompareBetween((BetweenExpression)a, (BetweenExpression)b),
			DatabaseExpressionType.RowCount => CompareRowNumber((RowNumberExpression)a, (RowNumberExpression)b),
			DatabaseExpressionType.Projection => CompareProjection((ProjectionExpression)a, (ProjectionExpression)b),
			DatabaseExpressionType.NamedValue => CompareNamedValue((NamedValueExpression)a, (NamedValueExpression)b),
			DatabaseExpressionType.Batch => CompareBatch((BatchExpression)a, (BatchExpression)b),
			DatabaseExpressionType.Function => CompareFunction((FunctionExpression)a, (FunctionExpression)b),
			DatabaseExpressionType.Entity => CompareEntity((EntityExpression)a, (EntityExpression)b),
			DatabaseExpressionType.If => CompareIf((IfCommandExpression)a, (IfCommandExpression)b),
			DatabaseExpressionType.Block => CompareBlock((BlockExpression)a, (BlockExpression)b),
			_ => base.Compare(a, b),
		};
	}

	private static bool CompareTable(TableExpression a, TableExpression b)
	{
		return a.Name == b.Name;
	}

	private bool CompareColumn(ColumnExpression a, ColumnExpression b)
	{
		return CompareAlias(a.Alias, b.Alias) && a.Name == b.Name;
	}

	private bool CompareAlias(Alias a, Alias b)
	{
		if (AliasScope is not null)
		{
			if (AliasScope.TryGetValue(a, out Alias? mapped))
				return mapped == b;
		}

		return a == b;
	}

	private bool CompareSelect(SelectExpression a, SelectExpression b)
	{
		var save = AliasScope;

		try
		{
			if (!Compare(a.From, b.From))
				return false;

			AliasScope = new ScopedDictionary<Alias, Alias>(save);

			if (a.From is not null && b.From is not null)
				MapAliases(a.From, b.From);

			return Compare(a.Where, b.Where)
				 && CompareOrderList(a.OrderBy, b.OrderBy)
				 && CompareExpressionList(a.GroupBy, b.GroupBy)
				 && Compare(a.Skip, b.Skip)
				 && Compare(a.Take, b.Take)
				 && a.IsDistinct == b.IsDistinct
				 && a.IsReverse == b.IsReverse
				 && CompareColumnDeclarations(a.Columns, b.Columns);
		}
		finally
		{
			AliasScope = save;
		}
	}

	private void MapAliases(Expression a, Expression b)
	{
		if (AliasScope is null)
			throw new NullReferenceException(nameof(AliasScope));

		var prodA = DeclaredAliasesResolver.Resolve(a).ToArray();
		var prodB = DeclaredAliasesResolver.Resolve(b).ToArray();

		for (int i = 0, n = prodA.Length; i < n; i++)
			AliasScope.Add(prodA[i], prodB[i]);
	}

	private bool CompareOrderList(ReadOnlyCollection<OrderExpression>? a, ReadOnlyCollection<OrderExpression>? b)
	{
		if (a == b)
			return true;

		if (a is null || b is null)
			return false;

		if (a.Count != b.Count)
			return false;

		for (var i = 0; i < a.Count; i++)
		{
			var left = a[i];
			var right = b[i];

			if (left.OrderType != right.OrderType || !Compare(left.Expression, right.Expression))
				return false;
		}

		return true;
	}

	private bool CompareColumnDeclarations(ReadOnlyCollection<ColumnDeclaration>? a, ReadOnlyCollection<ColumnDeclaration>? b)
	{
		if (a == b)
			return true;

		if (a is null || b is null)
			return false;

		if (a.Count != b.Count)
			return false;

		for (var i = 0; i < a.Count; i++)
		{
			if (!CompareColumnDeclaration(a[i], b[i]))
				return false;
		}

		return true;
	}

	private bool CompareColumnDeclaration(ColumnDeclaration a, ColumnDeclaration b)
	{
		return string.Equals(a.Name, b.Name, StringComparison.OrdinalIgnoreCase) && Compare(a.Expression, b.Expression);
	}

	private bool CompareJoin(JoinExpression a, JoinExpression b)
	{
		if (a.Join != b.Join || !Compare(a.Left, b.Left))
			return false;

		if (a.Join == JoinType.CrossApply || a.Join == JoinType.OuterApply)
		{
			var save = AliasScope;

			try
			{
				AliasScope = new ScopedDictionary<Alias, Alias>(AliasScope);
				MapAliases(a.Left, b.Left);

				return Compare(a.Right, b.Right) && Compare(a.Condition, b.Condition);
			}
			finally
			{
				AliasScope = save;
			}
		}
		else
			return Compare(a.Right, b.Right) && Compare(a.Condition, b.Condition);
	}

	private bool CompareAggregate(AggregateExpression a, AggregateExpression b)
	{
		return string.Equals(a.AggregateName, b.AggregateName, StringComparison.OrdinalIgnoreCase) && Compare(a.Argument, b.Argument);
	}

	private bool CompareIsNull(IsNullExpression a, IsNullExpression b)
	{
		return Compare(a.Expression, b.Expression);
	}

	private bool CompareBetween(BetweenExpression a, BetweenExpression b)
	{
		return Compare(a.Expression, b.Expression) && Compare(a.Lower, b.Lower) && Compare(a.Upper, b.Upper);
	}

	private bool CompareRowNumber(RowNumberExpression a, RowNumberExpression b)
	{
		return CompareOrderList(a.OrderBy, b.OrderBy);
	}

	private bool CompareNamedValue(NamedValueExpression a, NamedValueExpression b)
	{
		return string.Equals(a.Name, b.Name, StringComparison.OrdinalIgnoreCase) && Compare(a.Value, b.Value);
	}

	private bool CompareSubquery(SubqueryExpression a, SubqueryExpression b)
	{
		if (a.NodeType != b.NodeType)
			return false;

		return (DatabaseExpressionType)a.NodeType switch
		{
			DatabaseExpressionType.Scalar => CompareScalar((ScalarExpression)a, (ScalarExpression)b),
			DatabaseExpressionType.Exists => CompareExists((ExistsExpression)a, (ExistsExpression)b),
			DatabaseExpressionType.In => CompareIn((InExpression)a, (InExpression)b),
			_ => false,
		};
	}

	private bool CompareScalar(ScalarExpression a, ScalarExpression b)
	{
		return Compare(a.Select, b.Select);
	}

	private bool CompareExists(ExistsExpression a, ExistsExpression b)
	{
		return Compare(a.Select, b.Select);
	}

	private bool CompareIn(InExpression a, InExpression b)
	{
		return Compare(a.Expression, b.Expression) && Compare(a.Select, b.Select) && CompareExpressionList(a.Values, b.Values);
	}

	private bool CompareAggregateSubquery(AggregateSubqueryExpression a, AggregateSubqueryExpression b)
	{
		return Compare(a.AggregateAsSubquery, b.AggregateAsSubquery) && Compare(a.AggregateInGroupSelect, b.AggregateInGroupSelect) && a.GroupByAlias == b.GroupByAlias;
	}

	private bool CompareProjection(ProjectionExpression a, ProjectionExpression b)
	{
		if (!Compare(a.Select, b.Select))
			return false;

		var save = AliasScope;

		try
		{
			AliasScope = new ScopedDictionary<Alias, Alias>(AliasScope);
			AliasScope.Add(a.Select.Alias, b.Select.Alias);

			return Compare(a.Projector, b.Projector)
				 && Compare(a.Aggregator, b.Aggregator)
				 && a.IsSingleton == b.IsSingleton;
		}
		finally
		{
			AliasScope = save;
		}
	}

	private bool CompareBatch(BatchExpression x, BatchExpression y)
	{
		return Compare(x.Input, y.Input) && Compare(x.Operation, y.Operation) && Compare(x.BatchSize, y.BatchSize) && Compare(x.Stream, y.Stream);
	}

	private bool CompareIf(IfCommandExpression x, IfCommandExpression y)
	{
		return Compare(x.Check, y.Check) && Compare(x.IfTrue, y.IfTrue) && Compare(x.IfFalse, y.IfFalse);
	}

	private bool CompareBlock(BlockExpression x, BlockExpression y)
	{
		if (x.Commands.Count != y.Commands.Count)
			return false;

		for (var i = 0; i < x.Commands.Count; i++)
		{
			if (!Compare(x.Commands[i], y.Commands[i]))
				return false;
		}

		return true;
	}

	private bool CompareFunction(FunctionExpression x, FunctionExpression y)
	{
		return string.Equals(x.Name, y.Name, StringComparison.OrdinalIgnoreCase) && CompareExpressionList(x.Arguments, y.Arguments);
	}

	private bool CompareEntity(EntityExpression x, EntityExpression y)
	{
		return x.EntityType == y.EntityType && Compare(x.Expression, y.Expression);
	}
}