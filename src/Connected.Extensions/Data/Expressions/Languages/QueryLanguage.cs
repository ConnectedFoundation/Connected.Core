using Connected.Data.Expressions.Expressions;
using Connected.Data.Expressions.Translation;
using Connected.Data.Expressions.Translation.Resolvers;
using Connected.Data.Expressions.TypeSystem;
using Connected.Reflection;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace Connected.Data.Expressions.Languages;

/// <summary>
/// Abstract base class that encapsulates rules and behavior for a target query language.
/// </summary>
/// <remarks>
/// A <see cref="QueryLanguage"/> provides the language-specific type system,
/// quoting rules and translation helpers required by the expression translator.
/// Concrete implementations supply a <see cref="QueryTypeSystem"/> and may override
/// behaviour such as quoting, aggregate handling and formatting.
/// </remarks>
public abstract class QueryLanguage
{
	private const string AggregateCount = "Count";
	private const string AggregateLongCount = "LongCount";
	private const string AggregateSum = "Sum";
	private const string AggregateMin = "Min";
	private const string AggregateMax = "Max";
	private const string AggregateAverage = "Average";

	/// <summary>
	/// Gets a value that indicates whether DISTINCT is allowed inside aggregate functions for this language.
	/// </summary>
	public virtual bool AllowDistinctInAggregates => false;

	/// <summary>
	/// Gets the type system used to map CLR types to the language's data types.
	/// </summary>
	public abstract QueryTypeSystem TypeSystem { get; }

	/// <summary>
	/// Gets a value that indicates if the language allows multiple commands in a single batch.
	/// </summary>
	public virtual bool AllowsMultipleCommands => false;

	/// <summary>
	/// Gets a value that indicates if a SELECT may be emitted without a FROM clause in this language.
	/// </summary>
	public virtual bool AllowSubqueryInSelectWithoutFrom => false;

	/// <summary>
	/// Returns an expression that represents the number of rows affected by a preceding command.
	/// </summary>
	/// <param name="command">The command expression to inspect.</param>
	/// <returns>An expression that yields rows affected (typically @@ROWCOUNT or equivalent).</returns>
	public virtual Expression GetRowsAffectedExpression(Expression command)
	{
		return new FunctionExpression(typeof(int), "@@ROWCOUNT", null);
	}

	/// <summary>
	/// Determines whether the specified expression represents a rows-affected value.
	/// </summary>
	/// <param name="expression">The expression to test.</param>
	/// <returns><c>true</c> if the expression is the rows-affected sentinel; otherwise <c>false</c>.</returns>
	public virtual bool IsRowsAffectedExpressions(Expression expression)
	{
		var fex = expression as FunctionExpression;

		return fex is not null && string.Equals(fex.Name, "@@ROWCOUNT", StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Quote an identifier according to the target language rules.
	/// </summary>
	/// <param name="name">The identifier to quote.</param>
	/// <returns>The quoted identifier.</returns>
	public virtual string Quote(string name)
	{
		return name;
	}

	/// <summary>
	/// Determines whether the specified member represents an aggregate operation.
	/// </summary>
	/// <param name="member">The member to evaluate.</param>
	/// <returns><c>true</c> when the member is an aggregate method or property; otherwise <c>false</c>.</returns>
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

	/// <summary>
	/// Determines whether the aggregate with the specified name accepts a predicate
	/// argument that acts as a filter (for example Count(predicate)).
	/// </summary>
	/// <param name="aggregateName">The aggregate method name.</param>
	/// <returns><c>true</c> for aggregates that accept a predicate; otherwise <c>false</c>.</returns>
	public virtual bool IsAggregateArgumentPredicate(string aggregateName)
	{
		return string.Equals(aggregateName, AggregateCount, StringComparison.Ordinal) || string.Equals(aggregateName, AggregateLongCount, StringComparison.Ordinal);
	}

	/// <summary>
	/// Selects a column expression that can be used to test whether an outer-joined row exists.
	/// </summary>
	/// <param name="select">The select expression to inspect.</param>
	/// <returns>An expression that can be evaluated to determine if the outer join produced a row.</returns>
	public virtual Expression GetOuterJoinTest(SelectExpression select)
	{
		/*
		 *
		 * Find a column that participates in equality tests for the join condition.
		 * If such a column is found and it appears in the projection use it; otherwise
		 * return the first join column. If no suitable column is available introduce
		 * a constant that can act as a safe fallback test value.
		 *
		 */
		if (select.From is null)
			throw new NullReferenceException(SR.ErrExpectedExpression);

		var aliases = DeclaredAliasesResolver.Resolve(select.From);
		var columns = JoinColumnResolver.Resolve(aliases, select).ToList();

		if (columns.Count != 0)
		{
			/*
			 *
			 * Prefer a column that is already present in the projection list so that
			 * the test does not require adding an additional projected column.
			 *
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
		 *
		 * Fall back to introducing a constant expression (nullable int) which can
		 * be used as a safe test when no suitable join column exists.
		 *
		 */
		return Expression.Constant(1, typeof(int?));
	}

	/// <summary>
	/// Adds an outer-join existence test to the projection if necessary.
	/// </summary>
	/// <param name="expression">The projection expression to augment.</param>
	/// <returns>The modified projection containing an outer join test column and an OuterJoined projector.</returns>
	public virtual ProjectionExpression AddOuterJoinTest(ProjectionExpression expression)
	{
		var test = GetOuterJoinTest(expression.Select);
		var select = expression.Select;
		ColumnExpression? testCol = null;
		/*
		 *
		 * Look to see if the test expression already exists as a projected column.
		 * If so wrap it into a ColumnExpression that refers to the current select alias.
		 *
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
			 *
			 * If the test expression is not part of the projection add it as a new
			 * column using an available column name. This ensures the projector can
			 * reference the test via a ColumnExpression.
			 *
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

	/// <summary>
	/// Determines whether the given CLR type is considered a scalar for the language.
	/// </summary>
	/// <param name="type">The CLR type to evaluate.</param>
	/// <returns><c>true</c> when the type maps to a scalar value; otherwise <c>false</c>.</returns>
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
	/// Determines whether the given expression can be represented as a column in a select expression.
	/// </summary>
	/// <param name="expression">The expression to evaluate.</param>
	/// <returns><c>true</c> if the expression can be projected as a column; otherwise <c>false</c>.</returns>
	public virtual bool CanBeColumn(Expression expression)
	{
		return MustBeColumn(expression) || IsScalar(expression.Type);
	}

	/// <summary>
	/// Determines whether the given expression must be represented as a column in a SELECT column list.
	/// </summary>
	/// <param name="expression">The expression to evaluate.</param>
	/// <returns><c>true</c> when the expression's node type corresponds to a column-like database expression.</returns>
	public virtual bool MustBeColumn(Expression expression)
	{
		return expression.NodeType switch
		{
			(ExpressionType)DatabaseExpressionType.Column or (ExpressionType)DatabaseExpressionType.Scalar or (ExpressionType)DatabaseExpressionType.Exists or
			(ExpressionType)DatabaseExpressionType.AggregateSubquery or (ExpressionType)DatabaseExpressionType.Aggregate => true,
			_ => false,
		};
	}

	/// <summary>
	/// Create a linguist responsible for language-specific rewrites and formatting.
	/// </summary>
	/// <param name="context">Compilation context for the expression translation.</param>
	/// <param name="translator">The translator that owns this linguist.</param>
	/// <returns>A new <see cref="Linguist"/> instance.</returns>
	public virtual Linguist CreateLinguist(ExpressionCompilationContext context, Translator translator)
	{
		return new Linguist(context, this, translator);
	}
}
