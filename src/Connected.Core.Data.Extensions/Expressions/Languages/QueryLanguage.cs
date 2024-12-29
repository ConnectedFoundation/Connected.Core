using Connected.Data.Expressions.Expressions;
using Connected.Data.Expressions.Translation;
using Connected.Data.Expressions.Translation.Resolvers;
using Connected.Data.Expressions.TypeSystem;
using Connected.Reflection;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace Connected.Data.Expressions.Languages;

public abstract class QueryLanguage
{
	private const string AggregateCount = "Count";
	private const string AggregateLongCount = "LongCount";
	private const string AggregateSum = "Sum";
	private const string AggregateMin = "Min";
	private const string AggregateMax = "Max";
	private const string AggregateAverage = "Average";

	public virtual bool AllowDistinctInAggregates => false;
	public abstract QueryTypeSystem TypeSystem { get; }
	public virtual bool AllowsMultipleCommands => false;
	public virtual bool AllowSubqueryInSelectWithoutFrom => false;

	public virtual Expression GetRowsAffectedExpression(Expression command)
	{
		return new FunctionExpression(typeof(int), "@@ROWCOUNT", null);
	}

	public virtual bool IsRowsAffectedExpressions(Expression expression)
	{
		var fex = expression as FunctionExpression;

		return fex is not null && string.Equals(fex.Name, "@@ROWCOUNT", StringComparison.OrdinalIgnoreCase);
	}

	public virtual string Quote(string name)
	{
		return name;
	}

	public virtual bool IsAggregate(MemberInfo member)
	{
		var method = member as MethodInfo;

		if (method is not null)
		{
			if (method.DeclaringType == typeof(Queryable) || method.DeclaringType == typeof(Enumerable))
			{
				switch (method.Name)
				{
					case AggregateCount:
					case AggregateLongCount:
					case AggregateSum:
					case AggregateMin:
					case AggregateMax:
					case AggregateAverage:
						return true;
				}
			}
		}

		var property = member as PropertyInfo;

		if (property is not null && string.Equals(property.Name, AggregateCount, StringComparison.Ordinal) && typeof(IEnumerable).IsAssignableFrom(property.DeclaringType))
			return true;

		return false;
	}

	public virtual bool IsAggregateArgumentPredicate(string aggregateName)
	{
		return string.Equals(aggregateName, AggregateCount, StringComparison.Ordinal) || string.Equals(aggregateName, AggregateLongCount, StringComparison.Ordinal);
	}

	public virtual Expression GetOuterJoinTest(SelectExpression select)
	{
		/*
		 * if the column is used in the join condition (equality test)
		 * if it is null in the database then the join test won't match (null != null) so the row won't appear
		 * we can safely use this existing column as our test to determine if the outer join produced a row
		 *  
		 * find a column that is used in equality test
		 */
		var aliases = DeclaredAliasesResolver.Resolve(select.From);
		var columns = JoinColumnResolver.Resolve(aliases, select).ToList();

		if (columns.Any())
		{
			/*
			 * prefer one that is already in the projection list.
			 */
			foreach (var column in columns)
			{
				foreach (var col in select.Columns)
				{
					if (column.Equals(col.Expression))
						return column;
				}
			}

			return columns[0];
		}
		/*
		 * fall back to introducing a constant
		 */
		return Expression.Constant(1, typeof(int?));
	}
	public virtual ProjectionExpression AddOuterJoinTest(ProjectionExpression expression)
	{
		var test = GetOuterJoinTest(expression.Select);
		var select = expression.Select;
		ColumnExpression? testCol = null;
		/*
		 * look to see if test expression exists in columns already
		 */
		foreach (var column in select.Columns)
		{
			if (test.Equals(column.Expression))
			{
				var colType = TypeSystem.ResolveColumnType(test.Type);

				testCol = new ColumnExpression(test.Type, colType, select.Alias, column.Name);

				break;
			}
		}

		if (testCol is null)
		{
			/*
			 * add expression to projection
			 */
			testCol = test as ColumnExpression;

			var colName = testCol is not null ? testCol.Name : "Test";

			colName = expression.Select.Columns.ResolveAvailableColumnName(colName);

			var colType = TypeSystem.ResolveColumnType(test.Type);

			select = select.AddColumn(new ColumnDeclaration(colName, test, colType));
			testCol = new ColumnExpression(test.Type, colType, select.Alias, colName);
		}

		var newProjector = new OuterJoinedExpression(testCol, expression.Projector);

		return new ProjectionExpression(select, newProjector, expression.Aggregator);
	}

	public virtual bool IsScalar(Type type)
	{
		type = Nullables.GetNonNullableType(type);

		return type.GetTypeCode() switch
		{
			TypeCode.Empty => false,
			TypeCode.Object => type == typeof(DateTimeOffset) ||
							type == typeof(TimeSpan) ||
							type == typeof(Guid) ||
							type == typeof(byte[]),
			_ => true,
		};
	}

	/// <summary>
	/// Determines whether the given expression can be represented as a column in a select expressionss
	/// </summary>
	public virtual bool CanBeColumn(Expression expression)
	{
		return MustBeColumn(expression) || IsScalar(expression.Type);
	}
	/// <summary>
	/// Determines whether the given expression must be represented as a column in a SELECT column list
	/// </summary>
	public virtual bool MustBeColumn(Expression expression)
	{
		return expression.NodeType switch
		{
			(ExpressionType)DatabaseExpressionType.Column or (ExpressionType)DatabaseExpressionType.Scalar or (ExpressionType)DatabaseExpressionType.Exists or
			(ExpressionType)DatabaseExpressionType.AggregateSubquery or (ExpressionType)DatabaseExpressionType.Aggregate => true,
			_ => false,
		};
	}

	public virtual Linguist CreateLinguist(ExpressionCompilationContext context, Translator translator)
	{
		return new Linguist(context, this, translator);
	}
}
