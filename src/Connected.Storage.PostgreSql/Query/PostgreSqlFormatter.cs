using Connected.Data.Expressions;
using Connected.Data.Expressions.Expressions;
using Connected.Data.Expressions.Formatters;
using Connected.Data.Expressions.Languages;
using Connected.Reflection;
using System.Collections;
using System.Linq.Expressions;

namespace Connected.Storage.PostgreSql.Query;

/// <summary>
/// Provides PostgreSQL specific formatting capabilities for converting expression trees into PostgreSQL query strings.
/// </summary>
/// <remarks>
/// This sealed class extends <see cref="SqlFormatter"/> to handle PostgreSQL dialect-specific syntax and functions,
/// including string manipulation, date/time operations, mathematical functions, and aggregate functions.
/// It translates LINQ expression trees into executable PostgreSQL statements with proper parameter handling.
/// PostgreSQL-specific features include support for native LIMIT/OFFSET, INTERVAL for time arithmetic, and
/// extensive function support including string functions, date/time extraction, and mathematical operations.
/// </remarks>
internal sealed class PostgreSqlFormatter(ExpressionCompilationContext context, QueryLanguage? language)
		: SqlFormatter(language)
{
	/// <summary>
	/// Gets the expression compilation context containing parameter information and metadata.
	/// </summary>
	public ExpressionCompilationContext Context { get; } = context;

	/// <summary>
	/// Formats an expression tree into a PostgreSQL query string using the default PostgreSQL language.
	/// </summary>
	/// <param name="context">The expression compilation context.</param>
	/// <param name="expression">The expression tree to format.</param>
	/// <returns>A PostgreSQL query string representation of the expression.</returns>
	public static string Format(ExpressionCompilationContext context, Expression expression)
	{
		return Format(context, expression, new PostgreSqlLanguage());
	}

	/// <summary>
	/// Formats an expression tree into a PostgreSQL query string using the specified query language.
	/// </summary>
	/// <param name="context">The expression compilation context.</param>
	/// <param name="expression">The expression tree to format.</param>
	/// <param name="language">The query language configuration to use.</param>
	/// <returns>A PostgreSQL query string representation of the expression.</returns>
	public static string Format(ExpressionCompilationContext context, Expression expression, QueryLanguage language)
	{
		var formatter = new PostgreSqlFormatter(context, language);

		formatter.Visit(expression);

		return formatter.ToString();
	}

	/// <summary>
	/// Writes the PostgreSQL specific aggregate function name.
	/// </summary>
	/// <param name="aggregateName">The name of the aggregate function.</param>
	/// <remarks>
	/// PostgreSQL aggregate functions are mostly standard SQL, so most names are passed through unchanged.
	/// LongCount is handled as COUNT which returns bigint by default in PostgreSQL.
	/// </remarks>
	protected override void WriteAggregateName(string aggregateName)
	{
		if (string.Equals(aggregateName, "LongCount", StringComparison.Ordinal))
			Write("COUNT");
		else
			base.WriteAggregateName(aggregateName);
	}

	/// <summary>
	/// Visits a member access expression and translates it to PostgreSQL syntax.
	/// </summary>
	/// <param name="m">The member access expression to visit.</param>
	/// <returns>The visited expression.</returns>
	/// <remarks>
	/// Handles member access for string properties (e.g., Length) and DateTime/DateTimeOffset properties
	/// (e.g., Day, Month, Year, Hour) by converting them to appropriate PostgreSQL functions using EXTRACT
	/// and LENGTH functions.
	/// </remarks>
	/// <exception cref="NullReferenceException">Thrown when a required expression is null.</exception>
	protected override Expression VisitMemberAccess(MemberExpression m)
	{
		if (m.Member.DeclaringType == typeof(string))
		{
			switch (m.Member.Name)
			{
				case "Length":
					/*
					 * Convert string.Length to LENGTH() function
					 */
					Write("LENGTH(");

					if (m.Expression is null)
						throw new NullReferenceException(Schemas.SR.ErrExpectedExpression);

					Visit(m.Expression);

					Write(")");
					return m;
			}
		}
		else if (m.Member.DeclaringType == typeof(DateTime) || m.Member.DeclaringType == typeof(DateTimeOffset))
		{
			switch (m.Member.Name)
			{
				case "Day":
					/*
					 * Convert DateTime.Day to EXTRACT(DAY FROM ...) function
					 */
					Write("EXTRACT(DAY FROM ");

					if (m.Expression is null)
						throw new NullReferenceException(Schemas.SR.ErrExpectedExpression);

					Visit(m.Expression);

					Write(")");

					return m;
				case "Month":
					/*
					 * Convert DateTime.Month to EXTRACT(MONTH FROM ...) function
					 */
					Write("EXTRACT(MONTH FROM ");

					if (m.Expression is null)
						throw new NullReferenceException(Schemas.SR.ErrExpectedExpression);

					Visit(m.Expression);

					Write(")");

					return m;
				case "Year":
					/*
					 * Convert DateTime.Year to EXTRACT(YEAR FROM ...) function
					 */
					Write("EXTRACT(YEAR FROM ");

					if (m.Expression is null)
						throw new NullReferenceException(Schemas.SR.ErrExpectedExpression);

					Visit(m.Expression);

					Write(")");

					return m;
				case "Hour":
					/*
					 * Convert DateTime.Hour to EXTRACT(HOUR FROM ...) function
					 */
					Write("EXTRACT(HOUR FROM ");

					if (m.Expression is null)
						throw new NullReferenceException(Schemas.SR.ErrExpectedExpression);

					Visit(m.Expression);

					Write(")");

					return m;
				case "Minute":
					/*
					 * Convert DateTime.Minute to EXTRACT(MINUTE FROM ...) function
					 */
					Write("EXTRACT(MINUTE FROM ");

					if (m.Expression is null)
						throw new NullReferenceException(Schemas.SR.ErrExpectedExpression);

					Visit(m.Expression);

					Write(")");

					return m;
				case "Second":
					/*
					 * Convert DateTime.Second to EXTRACT(SECOND FROM ...) function
					 */
					Write("EXTRACT(SECOND FROM ");

					if (m.Expression is null)
						throw new NullReferenceException(Schemas.SR.ErrExpectedExpression);

					Visit(m.Expression);

					Write(")");

					return m;
				case "Millisecond":
					/*
					 * Convert DateTime.Millisecond to EXTRACT(MILLISECONDS FROM ...) % 1000
					 */
					Write("(EXTRACT(MILLISECONDS FROM ");

					if (m.Expression is null)
						throw new NullReferenceException(Schemas.SR.ErrExpectedExpression);

					Visit(m.Expression);

					Write(") % 1000)");

					return m;
				case "DayOfWeek":
					/*
					 * Convert DateTime.DayOfWeek to EXTRACT(DOW FROM ...) (already zero-based in PostgreSQL)
					 */
					Write("EXTRACT(DOW FROM ");

					if (m.Expression is null)
						throw new NullReferenceException(Schemas.SR.ErrExpectedExpression);

					Visit(m.Expression);

					Write(")");

					return m;
				case "DayOfYear":
					/*
					 * Convert DateTime.DayOfYear to EXTRACT(DOY FROM ...) - 1 (zero-based)
					 */
					Write("(EXTRACT(DOY FROM ");

					if (m.Expression is null)
						throw new NullReferenceException(Schemas.SR.ErrExpectedExpression);

					Visit(m.Expression);

					Write(") - 1)");

					return m;
			}
		}

		return base.VisitMemberAccess(m);
	}

	/// <summary>
	/// Visits a method call expression and translates it to PostgreSQL syntax.
	/// </summary>
	/// <param name="m">The method call expression to visit.</param>
	/// <returns>The visited expression.</returns>
	/// <remarks>
	/// Translates method calls from various types (String, DateTime, Math, Decimal) to their PostgreSQL equivalents.
	/// Handles string operations (Contains, StartsWith, EndsWith, Substring, etc.), date/time arithmetic using
	/// INTERVAL notation, mathematical functions, and type comparisons. PostgreSQL uses INTERVAL for time-based
	/// arithmetic and concatenation operator || for strings.
	/// </remarks>
	/// <exception cref="NullReferenceException">Thrown when a required expression object is null.</exception>
	protected override Expression VisitMethodCall(MethodCallExpression m)
	{
		if (m.Method.DeclaringType == typeof(string))
		{
			switch (m.Method.Name)
			{
				case "StartsWith":
					/*
					 * Convert string.StartsWith to LIKE pattern
					 */
					Write("(");

					if (m.Object is null)
						throw new NullReferenceException(Schemas.SR.ErrExpectedExpression);

					Visit(m.Object);

					Write(" LIKE ");
					Visit(m.Arguments[0]);
					Write(" || '%')");

					return m;
				case "EndsWith":
					/*
					 * Convert string.EndsWith to LIKE pattern
					 */
					Write("(");

					if (m.Object is null)
						throw new NullReferenceException(Schemas.SR.ErrExpectedExpression);

					Visit(m.Object);

					Write(" LIKE '%' || ");
					Visit(m.Arguments[0]);
					Write(")");

					return m;
				case "Contains":
					/*
					 * Convert string.Contains to LIKE pattern or POSITION for better performance
					 */
					Write("(POSITION(");
					Visit(m.Arguments[0]);
					Write(" IN ");

					if (m.Object is null)
						throw new NullReferenceException(Schemas.SR.ErrExpectedExpression);

					Visit(m.Object);
					Write(") > 0)");

					return m;
				case "Concat":
					/*
					 * Convert string.Concat to || operator (PostgreSQL concatenation)
					 */
					var args = m.Arguments;

					if (args.Count == 1 && args[0].NodeType == ExpressionType.NewArrayInit)
						args = ((NewArrayExpression)args[0]).Expressions;

					for (var i = 0; i < args.Count; i++)
					{
						if (i > 0)
							Write(" || ");

						Visit(args[i]);
					}
					return m;
				case "IsNullOrEmpty":
					/*
					 * Convert string.IsNullOrEmpty to IS NULL OR = '' check
					 */
					Write("(");
					Visit(m.Arguments[0]);
					Write(" IS NULL OR ");
					Visit(m.Arguments[0]);
					Write(" = '')");
					return m;
				case "ToUpper":
					/*
					 * Convert string.ToUpper to UPPER() function
					 */
					Write("UPPER(");

					if (m.Object is null)
						throw new NullReferenceException(Schemas.SR.ErrExpectedExpression);

					Visit(m.Object);
					Write(")");

					return m;
				case "ToLower":
					/*
					 * Convert string.ToLower to LOWER() function
					 */
					Write("LOWER(");

					if (m.Object is null)
						throw new NullReferenceException(Schemas.SR.ErrExpectedExpression);

					Visit(m.Object);
					Write(")");

					return m;
				case "Replace":
					/*
					 * Convert string.Replace to REPLACE() function
					 */
					Write("REPLACE(");

					if (m.Object is null)
						throw new NullReferenceException(Schemas.SR.ErrExpectedExpression);

					Visit(m.Object);

					Write(", ");
					Visit(m.Arguments[0]);
					Write(", ");
					Visit(m.Arguments[1]);
					Write(")");

					return m;
				case "Substring":
					/*
					 * Convert string.Substring to SUBSTRING() function
					 * Note: PostgreSQL SUBSTRING is 1-based like T-SQL
					 */
					Write("SUBSTRING(");

					if (m.Object is null)
						throw new NullReferenceException(Schemas.SR.ErrExpectedExpression);

					Visit(m.Object);

					Write(" FROM ");
					Visit(m.Arguments[0]);
					Write(" + 1");

					if (m.Arguments.Count == 2)
					{
						Write(" FOR ");
						Visit(m.Arguments[1]);
					}

					Write(")");

					return m;
				case "Remove":
					/*
					 * Convert string.Remove using OVERLAY function
					 */
					Write("OVERLAY(");

					if (m.Object is null)
						throw new NullReferenceException(Schemas.SR.ErrExpectedExpression);

					Visit(m.Object);

					Write(" PLACING '' FROM ");
					Visit(m.Arguments[0]);
					Write(" + 1");

					if (m.Arguments.Count == 2)
					{
						Write(" FOR ");
						Visit(m.Arguments[1]);
					}

					Write(")");

					return m;
				case "IndexOf":
					/*
					 * Convert string.IndexOf to POSITION() function
					 * Subtract 1 to convert from 1-based to 0-based indexing
					 */
					Write("(POSITION(");
					Visit(m.Arguments[0]);
					Write(" IN ");

					if (m.Object is null)
						throw new NullReferenceException(Schemas.SR.ErrExpectedExpression);

					if (m.Arguments.Count == 2 && m.Arguments[1].Type == typeof(int))
					{
						/*
						 * For indexed search, use SUBSTRING to skip initial characters
						 */
						Write("SUBSTRING(");
						Visit(m.Object);
						Write(" FROM ");
						Visit(m.Arguments[1]);
						Write(" + 1)");
					}
					else
					{
						Visit(m.Object);
					}

					Write(") - 1)");

					return m;
				case "Trim":
					/*
					 * Convert string.Trim to TRIM() function
					 */
					Write("TRIM(");

					if (m.Object is null)
						throw new NullReferenceException(Schemas.SR.ErrExpectedExpression);

					Visit(m.Object);
					Write(")");

					return m;
			}
		}
		else if (m.Method.DeclaringType == typeof(DateTime))
		{
			switch (m.Method.Name)
			{
				case "op_Subtract":
					/*
					 * Convert DateTime subtraction to date arithmetic
					 * PostgreSQL returns interval type for date subtraction
					 */
					if (m.Arguments[1].Type == typeof(DateTime))
					{
						Write("(");
						Visit(m.Arguments[0]);
						Write(" - ");
						Visit(m.Arguments[1]);
						Write(")");
						return m;
					}
					break;
				case "AddYears":
					/*
					 * Convert DateTime.AddYears to interval addition
					 */
					Write("(");

					if (m.Object is null)
						throw new NullReferenceException(Schemas.SR.ErrExpectedExpression);

					Visit(m.Object);
					Write(" + INTERVAL '1 year' * ");
					Visit(m.Arguments[0]);
					Write(")");

					return m;
				case "AddMonths":
					/*
					 * Convert DateTime.AddMonths to interval addition
					 */
					Write("(");

					if (m.Object is null)
						throw new NullReferenceException(Schemas.SR.ErrExpectedExpression);

					Visit(m.Object);
					Write(" + INTERVAL '1 month' * ");
					Visit(m.Arguments[0]);
					Write(")");

					return m;
				case "AddDays":
					/*
					 * Convert DateTime.AddDays to interval addition
					 */
					Write("(");

					if (m.Object is null)
						throw new NullReferenceException(Schemas.SR.ErrExpectedExpression);

					Visit(m.Object);
					Write(" + INTERVAL '1 day' * ");
					Visit(m.Arguments[0]);
					Write(")");

					return m;
				case "AddHours":
					/*
					 * Convert DateTime.AddHours to interval addition
					 */
					Write("(");

					if (m.Object is null)
						throw new NullReferenceException(Schemas.SR.ErrExpectedExpression);

					Visit(m.Object);
					Write(" + INTERVAL '1 hour' * ");
					Visit(m.Arguments[0]);
					Write(")");

					return m;
				case "AddMinutes":
					/*
					 * Convert DateTime.AddMinutes to interval addition
					 */
					Write("(");

					if (m.Object is null)
						throw new NullReferenceException(Schemas.SR.ErrExpectedExpression);

					Visit(m.Object);
					Write(" + INTERVAL '1 minute' * ");
					Visit(m.Arguments[0]);
					Write(")");

					return m;
				case "AddSeconds":
					/*
					 * Convert DateTime.AddSeconds to interval addition
					 */
					Write("(");

					if (m.Object is null)
						throw new NullReferenceException(Schemas.SR.ErrExpectedExpression);

					Visit(m.Object);
					Write(" + INTERVAL '1 second' * ");
					Visit(m.Arguments[0]);
					Write(")");

					return m;
				case "AddMilliseconds":
					/*
					 * Convert DateTime.AddMilliseconds to interval addition
					 */
					Write("(");

					if (m.Object is null)
						throw new NullReferenceException(Schemas.SR.ErrExpectedExpression);

					Visit(m.Object);
					Write(" + INTERVAL '1 millisecond' * ");
					Visit(m.Arguments[0]);
					Write(")");

					return m;
			}
		}
		else if (m.Method.DeclaringType == typeof(decimal))
		{
			switch (m.Method.Name)
			{
				case "Add":
				case "Subtract":
				case "Multiply":
				case "Divide":
				case "Remainder":
					/*
					 * Convert decimal arithmetic operations to SQL operators
					 */
					Write("(");
					VisitValue(m.Arguments[0]);
					Write(" ");
					Write(GetOperator(m.Method.Name));
					Write(" ");
					VisitValue(m.Arguments[1]);
					Write(")");
					return m;
				case "Negate":
					/*
					 * Convert decimal.Negate to unary minus operator
					 */
					Write("-");
					Visit(m.Arguments[0]);
					Write("");
					return m;
				case "Ceiling":
				case "Floor":
					/*
					 * Convert decimal.Ceiling/Floor to PostgreSQL CEIL/FLOOR functions
					 */
					Write(m.Method.Name == "Ceiling" ? "CEIL(" : "FLOOR(");
					Visit(m.Arguments[0]);
					Write(")");
					return m;
				case "Round":
					/*
					 * Convert decimal.Round to PostgreSQL ROUND function
					 */
					if (m.Arguments.Count == 1)
					{
						Write("ROUND(");
						Visit(m.Arguments[0]);
						Write(", 0)");
						return m;
					}
					else if (m.Arguments.Count == 2 && m.Arguments[1].Type == typeof(int))
					{
						Write("ROUND(");
						Visit(m.Arguments[0]);
						Write(", ");
						Visit(m.Arguments[1]);
						Write(")");
						return m;
					}
					break;
				case "Truncate":
					/*
					 * Convert decimal.Truncate to TRUNC function
					 */
					Write("TRUNC(");
					Visit(m.Arguments[0]);
					Write(", 0)");
					return m;
			}
		}
		else if (m.Method.DeclaringType == typeof(Math))
		{
			switch (m.Method.Name)
			{
				case "Abs":
				case "Acos":
				case "Asin":
				case "Atan":
				case "Cos":
				case "Exp":
				case "Sin":
				case "Tan":
				case "Sqrt":
				case "Sign":
					/*
					 * Convert Math functions to their PostgreSQL equivalents
					 */
					Write(m.Method.Name.ToUpper());
					Write("(");
					Visit(m.Arguments[0]);
					Write(")");
					return m;
				case "Ceiling":
					/*
					 * Convert Math.Ceiling to CEIL function
					 */
					Write("CEIL(");
					Visit(m.Arguments[0]);
					Write(")");
					return m;
				case "Floor":
					/*
					 * Convert Math.Floor to FLOOR function
					 */
					Write("FLOOR(");
					Visit(m.Arguments[0]);
					Write(")");
					return m;
				case "Atan2":
					/*
					 * Convert Math.Atan2 to ATAN2 function
					 */
					Write("ATAN2(");
					Visit(m.Arguments[0]);
					Write(", ");
					Visit(m.Arguments[1]);
					Write(")");
					return m;
				case "Log":
					/*
					 * Convert Math.Log to LN (natural log) function
					 */
					if (m.Arguments.Count == 1)
					{
						Write("LN(");
						Visit(m.Arguments[0]);
						Write(")");
						return m;
					}
					break;
				case "Log10":
					/*
					 * Convert Math.Log10 to LOG function
					 */
					Write("LOG(");
					Visit(m.Arguments[0]);
					Write(")");
					return m;
				case "Pow":
					/*
					 * Convert Math.Pow to POWER function
					 */
					Write("POWER(");
					Visit(m.Arguments[0]);
					Write(", ");
					Visit(m.Arguments[1]);
					Write(")");
					return m;
				case "Round":
					/*
					 * Convert Math.Round to PostgreSQL ROUND function
					 */
					if (m.Arguments.Count == 1)
					{
						Write("ROUND(");
						Visit(m.Arguments[0]);
						Write(", 0)");
						return m;
					}
					else if (m.Arguments.Count == 2 && m.Arguments[1].Type == typeof(int))
					{
						Write("ROUND(");
						Visit(m.Arguments[0]);
						Write(", ");
						Visit(m.Arguments[1]);
						Write(")");
						return m;
					}
					break;
				case "Truncate":
					/*
					 * Convert Math.Truncate to TRUNC function
					 */
					Write("TRUNC(");
					Visit(m.Arguments[0]);
					Write(", 0)");
					return m;
			}
		}

		if (m.Method.Name == "ToString")
		{
			/*
			 * Convert ToString() calls to CAST(... AS TEXT) for non-string types
			 */
			if (m.Object?.Type != typeof(string))
			{
				Write("CAST(");

				if (m.Object is null)
					throw new NullReferenceException(Schemas.SR.ErrExpectedExpression);

				Visit(m.Object);

				Write(" AS TEXT)");
			}
			else
				Visit(m.Object);

			return m;
		}
		else if (!m.Method.IsStatic && string.Equals(m.Method.Name, "CompareTo", StringComparison.Ordinal) && m.Method.ReturnType == typeof(int) && m.Arguments.Count == 1)
		{
			/*
			 * Convert CompareTo method to CASE WHEN expression
			 */
			Write("(CASE WHEN ");

			if (m.Object is null)
				throw new NullReferenceException(Schemas.SR.ErrExpectedExpression);

			Visit(m.Object);

			Write(" = ");
			Visit(m.Arguments[0]);
			Write(" THEN 0 WHEN ");
			Visit(m.Object);
			Write(" < ");
			Visit(m.Arguments[0]);
			Write(" THEN -1 ELSE 1 END)");
			return m;
		}
		else if (m.Method.IsStatic && string.Equals(m.Method.Name, "Compare", StringComparison.Ordinal) && m.Method.ReturnType == typeof(int) && m.Arguments.Count == 2)
		{
			/*
			 * Convert static Compare method to CASE WHEN expression
			 */
			Write("(CASE WHEN ");
			Visit(m.Arguments[0]);
			Write(" = ");
			Visit(m.Arguments[1]);
			Write(" THEN 0 WHEN ");
			Visit(m.Arguments[0]);
			Write(" < ");
			Visit(m.Arguments[1]);
			Write(" THEN -1 ELSE 1 END)");
			return m;
		}
		else if (typeof(IList<>).FullName is string listFullName && m.Method.DeclaringType?.GetInterface(listFullName) is not null)
		{
			/*
			 * Convert IList<T>.Contains to = ANY(ARRAY[...]) operator
			 */
			if (string.Equals(m.Method.Name, "Contains", StringComparison.Ordinal))
			{
				Visit(m.Arguments[0]);
				Write(" = ANY(");

				if (m.Object is null)
					throw new NullReferenceException(Schemas.SR.ErrExpectedExpression);

				Visit(m.Object);

				Write(")");

				return m;
			}
		}
		else if (m.Method.DeclaringType == typeof(TypeComparer) && m.Method.IsStatic && string.Equals(m.Method.Name, nameof(TypeComparer.Compare), StringComparison.Ordinal) && m.Method.ReturnType == typeof(bool) && m.Arguments.Count == 2)
		{
			/*
			 * Convert TypeComparer.Compare to equality operator
			 */
			Visit(m.Arguments[0]);
			Write(" = ");
			Visit(m.Arguments[1]);
			return m;
		}

		return base.VisitMethodCall(m);
	}

	/// <summary>
	/// Visits a new object expression and translates it to PostgreSQL syntax.
	/// </summary>
	/// <param name="nex">The new expression to visit.</param>
	/// <returns>The visited expression.</returns>
	/// <remarks>
	/// Handles DateTime constructor calls by converting them to PostgreSQL MAKE_DATE and MAKE_TIMESTAMP functions.
	/// </remarks>
	protected override NewExpression VisitNew(NewExpression nex)
	{
		if (nex.Constructor?.DeclaringType == typeof(DateTime))
		{
			if (nex.Arguments.Count == 3)
			{
				/*
				 * Convert DateTime constructor with date components to MAKE_DATE function
				 */
				Write("MAKE_DATE(");
				Visit(nex.Arguments[0]);
				Write(", ");
				Visit(nex.Arguments[1]);
				Write(", ");
				Visit(nex.Arguments[2]);
				Write(")");
				return nex;
			}
			else if (nex.Arguments.Count == 6)
			{
				/*
				 * Convert DateTime constructor with date and time components to MAKE_TIMESTAMP function
				 */
				Write("MAKE_TIMESTAMP(");
				Visit(nex.Arguments[0]);
				Write(", ");
				Visit(nex.Arguments[1]);
				Write(", ");
				Visit(nex.Arguments[2]);
				Write(", ");
				Visit(nex.Arguments[3]);
				Write(", ");
				Visit(nex.Arguments[4]);
				Write(", ");
				Visit(nex.Arguments[5]);
				Write(")");
				return nex;
			}
		}

		return base.VisitNew(nex);
	}

	/// <summary>
	/// Visits a binary expression and translates it to PostgreSQL syntax.
	/// </summary>
	/// <param name="b">The binary expression to visit.</param>
	/// <returns>The visited expression.</returns>
	/// <remarks>
	/// Handles special binary operations like Power, Coalesce, LeftShift, and RightShift by converting
	/// them to PostgreSQL POWER, COALESCE, and bitwise shift operators (<<, >>).
	/// </remarks>
	protected override Expression VisitBinary(BinaryExpression b)
	{
		if (b.NodeType == ExpressionType.Power)
		{
			/*
			 * Convert Power operator to POWER function
			 */
			Write("POWER(");
			VisitValue(b.Left);
			Write(", ");
			VisitValue(b.Right);
			Write(")");
			return b;
		}
		else if (b.NodeType == ExpressionType.Coalesce)
		{
			/*
			 * Convert Coalesce operator to COALESCE function with multiple arguments
			 */
			Write("COALESCE(");
			VisitValue(b.Left);
			Write(", ");

			var right = b.Right;

			while (right.NodeType == ExpressionType.Coalesce)
			{
				var rb = (BinaryExpression)right;

				VisitValue(rb.Left);
				Write(", ");

				right = rb.Right;
			}

			VisitValue(right);
			Write(")");

			return b;
		}
		else if (b.NodeType == ExpressionType.LeftShift)
		{
			/*
			 * PostgreSQL supports native bitwise left shift operator
			 */
			Write("(");
			VisitValue(b.Left);
			Write(" << ");
			VisitValue(b.Right);
			Write(")");
			return b;
		}
		else if (b.NodeType == ExpressionType.RightShift)
		{
			/*
			 * PostgreSQL supports native bitwise right shift operator
			 */
			Write("(");
			VisitValue(b.Left);
			Write(" >> ");
			VisitValue(b.Right);
			Write(")");
			return b;
		}

		return base.VisitBinary(b);
	}

	/// <summary>
	/// Visits a constant expression and translates it to PostgreSQL syntax.
	/// </summary>
	/// <param name="c">The constant expression to visit.</param>
	/// <returns>The visited expression.</returns>
	/// <remarks>
	/// Handles enumerable constants by expanding them inline, and parameter references by writing
	/// parameter names prefixed with $ (PostgreSQL parameter style) or @ for compatibility.
	/// </remarks>
	protected override Expression VisitConstant(ConstantExpression c)
	{
		if (c.Value is not null && c.Value.GetType().IsEnumerable())
		{
			/*
			 * Expand enumerable constants inline or as ARRAY[...] for PostgreSQL
			 */
			var en = ((IEnumerable)c.Value).GetEnumerator();
			var first = true;

			Write("ARRAY[");

			while (en.MoveNext())
			{
				var value = en.Current?.ToString();

				if (value is null)
					continue;

				if (!first)
					Write(", ");
				else
					first = false;

				Write(value);
			}

			Write("]");

			return c;
		}
		else
		{
			/*
			 * Check if constant is a registered parameter and write parameter reference
			 * PostgreSQL supports both $n and @param style parameters
			 */
			var parameter = Context.Parameters.FirstOrDefault(f => f.Value == c);

			if (parameter.Value is not null)
			{
				Write($"@{parameter.Key}");

				return c;
			}
		}
		return base.VisitConstant(c);
	}

	/// <summary>
	/// Visits a value expression and wraps predicates in CASE expressions.
	/// </summary>
	/// <param name="expr">The expression to visit.</param>
	/// <returns>The visited expression.</returns>
	/// <remarks>
	/// Converts boolean predicate expressions to integer values (1 for true, 0 for false) using CASE WHEN.
	/// PostgreSQL has native boolean type but CASE conversion may be needed for certain contexts.
	/// </remarks>
	protected override Expression VisitValue(Expression expr)
	{
		if (IsPredicate(expr))
		{
			/*
			 * Wrap boolean predicates in CASE WHEN to convert to integer values
			 */
			Write("CASE WHEN (");
			Visit(expr);
			Write(") THEN 1 ELSE 0 END");

			return expr;
		}

		return base.VisitValue(expr);
	}

	/// <summary>
	/// Visits a conditional expression and translates it to PostgreSQL CASE expression.
	/// </summary>
	/// <param name="c">The conditional expression to visit.</param>
	/// <returns>The visited expression.</returns>
	/// <remarks>
	/// Converts conditional expressions to PostgreSQL CASE WHEN THEN ELSE END syntax, handling
	/// both predicate-based and value-based conditionals.
	/// </remarks>
	protected override Expression VisitConditional(ConditionalExpression c)
	{
		if (IsPredicate(c.Test))
		{
			/*
			 * Handle predicate-based conditional with CASE WHEN
			 */
			Write("(CASE WHEN ");
			VisitPredicate(c.Test);
			Write(" THEN ");
			VisitValue(c.IfTrue);

			var ifFalse = c.IfFalse;

			while (ifFalse is not null && ifFalse.NodeType == ExpressionType.Conditional)
			{
				var fc = (ConditionalExpression)ifFalse;

				Write(" WHEN ");
				VisitPredicate(fc.Test);
				Write(" THEN ");
				VisitValue(fc.IfTrue);

				ifFalse = fc.IfFalse;
			}

			if (ifFalse is not null)
			{
				Write(" ELSE ");
				VisitValue(ifFalse);
			}

			Write(" END)");
		}
		else
		{
			/*
			 * Handle value-based conditional (PostgreSQL supports boolean type natively)
			 */
			Write("(CASE WHEN ");
			VisitValue(c.Test);
			Write(" THEN ");
			VisitValue(c.IfTrue);
			Write(" ELSE ");
			VisitValue(c.IfFalse);
			Write(" END)");
		}

		return c;
	}

	/// <summary>
	/// Visits a row number expression and translates it to PostgreSQL ROW_NUMBER() function.
	/// </summary>
	/// <param name="rowNumber">The row number expression to visit.</param>
	/// <returns>The visited expression.</returns>
	/// <remarks>
	/// Generates PostgreSQL ROW_NUMBER() OVER(ORDER BY ...) clause for ranking.
	/// Note: PostgreSQL prefers LIMIT/OFFSET for pagination over ROW_NUMBER when possible.
	/// </remarks>
	protected override Expression VisitRowNumber(RowNumberExpression rowNumber)
	{
		Write("ROW_NUMBER() OVER(");

		if (rowNumber.OrderBy is not null && rowNumber.OrderBy.Count != 0)
		{
			/*
			 * Add ORDER BY clause to ROW_NUMBER function
			 */
			Write("ORDER BY ");

			for (var i = 0; i < rowNumber.OrderBy.Count; i++)
			{
				var exp = rowNumber.OrderBy[i];

				if (i > 0)
					Write(", ");

				VisitValue(exp.Expression);

				if (exp.OrderType != OrderType.Ascending)
					Write(" DESC");
			}
		}

		Write(")");

		return rowNumber;
	}

	/// <summary>
	/// Visits an IF command expression and translates it to PostgreSQL IF...THEN...END syntax.
	/// </summary>
	/// <param name="ifx">The IF command expression to visit.</param>
	/// <returns>The visited expression.</returns>
	/// <remarks>
	/// Generates PostgreSQL procedural language IF statement (used in functions/procedures).
	/// Only used when the language allows multiple commands.
	/// </remarks>
	protected override Expression VisitIf(IfCommandExpression ifx)
	{
		if (Language is not null && !Language.AllowsMultipleCommands)
			return base.VisitIf(ifx);

		Write("IF ");
		Visit(ifx.Check);
		Write(" THEN");
		WriteLine(Indentation.Inner);
		VisitStatement(ifx.IfTrue);
		WriteLine(Indentation.Outer);

		if (ifx.IfFalse is not null)
		{
			Write("ELSE");
			WriteLine(Indentation.Inner);
			VisitStatement(ifx.IfFalse);
			WriteLine(Indentation.Outer);
		}

		Write("END IF");

		return ifx;
	}

	/// <summary>
	/// Visits a block expression containing multiple commands.
	/// </summary>
	/// <param name="block">The block expression to visit.</param>
	/// <returns>The visited expression.</returns>
	/// <remarks>
	/// Processes multiple statements in sequence with proper spacing and semicolons.
	/// Only used when the language allows multiple commands.
	/// </remarks>
	protected override Expression VisitBlock(Data.Expressions.Expressions.BlockExpression block)
	{
		if (Language is not null && !Language.AllowsMultipleCommands)
			return base.VisitBlock(block);

		for (var i = 0; i < block.Commands.Count; i++)
		{
			if (i > 0)
			{
				Write(";");
				WriteLine(Indentation.Same);
				WriteLine(Indentation.Same);
			}

			VisitStatement(block.Commands[i]);
		}

		return block;
	}

	/// <summary>
	/// Visits a variable declaration expression and translates it to PostgreSQL DECLARE statements.
	/// </summary>
	/// <param name="decl">The declaration expression to visit.</param>
	/// <returns>The visited expression.</returns>
	/// <remarks>
	/// Generates PostgreSQL variable declarations (used in PL/pgSQL functions/procedures).
	/// PostgreSQL uses DECLARE for variables and := for assignment within procedural blocks.
	/// Only used when the language allows multiple commands.
	/// </remarks>
	protected override Expression VisitDeclaration(DeclarationExpression decl)
	{
		if (Language is not null && !Language.AllowsMultipleCommands)
			return base.VisitDeclaration(decl);

		for (var i = 0; i < decl.Variables.Count; i++)
		{
			var v = decl.Variables[i];

			if (i > 0)
				WriteLine(Indentation.Same);

			/*
			 * Declare each variable with its data type (PL/pgSQL syntax)
			 */
			Write("DECLARE ");
			Write(v.Name);
			Write(" ");

			if (Language is not null)
				Write(Language.TypeSystem.Format(v.DataType, false));
		}

		if (decl.Source is not null)
		{
			/*
			 * Initialize variables from a SELECT query using INTO clause
			 */
			WriteLine(Indentation.Same);
			Write("SELECT ");

			for (var i = 0; i < decl.Source.Columns.Count; i++)
			{
				if (i > 0)
					Write(", ");

				Visit(decl.Source.Columns[i].Expression);
			}

			if (decl.Source.From is not null)
			{
				WriteLine(Indentation.Same);
				Write("FROM ");
				VisitSource(decl.Source.From);
			}

			if (decl.Source.Where is not null)
			{
				WriteLine(Indentation.Same);
				Write("WHERE ");
				Visit(decl.Source.Where);
			}

			WriteLine(Indentation.Same);
			Write("INTO ");

			for (var i = 0; i < decl.Variables.Count; i++)
			{
				if (i > 0)
					Write(", ");

				Write(decl.Variables[i].Name);
			}
		}
		else
		{
			/*
			 * Initialize variables with assignment (:= operator in PL/pgSQL)
			 */
			for (var i = 0; i < decl.Variables.Count; i++)
			{
				var v = decl.Variables[i];

				if (v.Expression is not null)
				{
					WriteLine(Indentation.Same);
					Write(v.Name);
					Write(" := ");
					Visit(v.Expression);
				}
			}
		}

		return decl;
	}
}
