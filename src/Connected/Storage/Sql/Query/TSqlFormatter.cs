using Connected.Data.Expressions;
using Connected.Data.Expressions.Expressions;
using Connected.Data.Expressions.Formatters;
using Connected.Data.Expressions.Languages;
using Connected.Reflection;
using System.Collections;
using System.Linq.Expressions;

namespace Connected.Storage.Sql.Query;

/// <summary>
/// Provides T-SQL specific formatting capabilities for converting expression trees into SQL Server query strings.
/// </summary>
/// <remarks>
/// This sealed class extends <see cref="SqlFormatter"/> to handle T-SQL dialect-specific syntax and functions,
/// including string manipulation, date/time operations, mathematical functions, and aggregate functions.
/// It translates LINQ expression trees into executable T-SQL statements with proper parameter handling.
/// </remarks>
internal sealed class TSqlFormatter(ExpressionCompilationContext context, QueryLanguage? language)
		: SqlFormatter(language)
{
	/// <summary>
	/// Gets the expression compilation context containing parameter information and metadata.
	/// </summary>
	public ExpressionCompilationContext Context { get; } = context;

	/// <summary>
	/// Formats an expression tree into a T-SQL query string using the default T-SQL language.
	/// </summary>
	/// <param name="context">The expression compilation context.</param>
	/// <param name="expression">The expression tree to format.</param>
	/// <returns>A T-SQL query string representation of the expression.</returns>
	public static string Format(ExpressionCompilationContext context, Expression expression)
	{
		return Format(context, expression, new TSqlLanguage());
	}

	/// <summary>
	/// Formats an expression tree into a T-SQL query string using the specified query language.
	/// </summary>
	/// <param name="context">The expression compilation context.</param>
	/// <param name="expression">The expression tree to format.</param>
	/// <param name="language">The query language configuration to use.</param>
	/// <returns>A T-SQL query string representation of the expression.</returns>
	public static string Format(ExpressionCompilationContext context, Expression expression, QueryLanguage language)
	{
		var formatter = new TSqlFormatter(context, language);

		formatter.Visit(expression);

		return formatter.ToString();
	}

	/// <summary>
	/// Writes the T-SQL specific aggregate function name.
	/// </summary>
	/// <param name="aggregateName">The name of the aggregate function.</param>
	/// <remarks>
	/// Translates LINQ aggregate functions to their T-SQL equivalents, such as converting
	/// "LongCount" to "COUNT_BIG" for large result sets.
	/// </remarks>
	protected override void WriteAggregateName(string aggregateName)
	{
		if (string.Equals(aggregateName, "LongCount", StringComparison.Ordinal))
			Write("COUNT_BIG");
		else
			base.WriteAggregateName(aggregateName);
	}

	/// <summary>
	/// Visits a member access expression and translates it to T-SQL syntax.
	/// </summary>
	/// <param name="m">The member access expression to visit.</param>
	/// <returns>The visited expression.</returns>
	/// <remarks>
	/// Handles member access for string properties (e.g., Length) and DateTime/DateTimeOffset properties
	/// (e.g., Day, Month, Year, Hour) by converting them to appropriate T-SQL functions.
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
					 * Convert string.Length to LEN() function
					 */
					Write("LEN(");

					if (m.Expression is null)
						throw new NullReferenceException(SR.ErrExpectedExpression);

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
					 * Convert DateTime.Day to DAY() function
					 */
					Write("DAY(");

					if (m.Expression is null)
						throw new NullReferenceException(SR.ErrExpectedExpression);

					Visit(m.Expression);

					Write(")");

					return m;
				case "Month":
					/*
					 * Convert DateTime.Month to MONTH() function
					 */
					Write("MONTH(");

					if (m.Expression is null)
						throw new NullReferenceException(SR.ErrExpectedExpression);

					Visit(m.Expression);

					Write(")");

					return m;
				case "Year":
					/*
					 * Convert DateTime.Year to YEAR() function
					 */
					Write("YEAR(");

					if (m.Expression is null)
						throw new NullReferenceException(SR.ErrExpectedExpression);

					Visit(m.Expression);

					Write(")");

					return m;
				case "Hour":
					/*
					 * Convert DateTime.Hour to DATEPART(hour, ...) function
					 */
					Write("DATEPART(hour, ");

					if (m.Expression is null)
						throw new NullReferenceException(SR.ErrExpectedExpression);

					Visit(m.Expression);

					Write(")");

					return m;
				case "Minute":
					/*
					 * Convert DateTime.Minute to DATEPART(minute, ...) function
					 */
					Write("DATEPART(minute, ");

					if (m.Expression is null)
						throw new NullReferenceException(SR.ErrExpectedExpression);

					Visit(m.Expression);

					Write(")");

					return m;
				case "Second":
					/*
					 * Convert DateTime.Second to DATEPART(second, ...) function
					 */
					Write("DATEPART(second, ");

					if (m.Expression is null)
						throw new NullReferenceException(SR.ErrExpectedExpression);

					Visit(m.Expression);

					Write(")");

					return m;
				case "Millisecond":
					/*
					 * Convert DateTime.Millisecond to DATEPART(millisecond, ...) function
					 */
					Write("DATEPART(millisecond, ");

					if (m.Expression is null)
						throw new NullReferenceException(SR.ErrExpectedExpression);

					Visit(m.Expression);

					Write(")");

					return m;
				case "DayOfWeek":
					/*
					 * Convert DateTime.DayOfWeek to DATEPART(weekday, ...) - 1 (zero-based)
					 */
					Write("(DATEPART(weekday, ");

					if (m.Expression is null)
						throw new NullReferenceException(SR.ErrExpectedExpression);

					Visit(m.Expression);

					Write(") - 1)");

					return m;
				case "DayOfYear":
					/*
					 * Convert DateTime.DayOfYear to DATEPART(dayofyear, ...) - 1 (zero-based)
					 */
					Write("(DATEPART(dayofyear, ");

					if (m.Expression is null)
						throw new NullReferenceException(SR.ErrExpectedExpression);

					Visit(m.Expression);

					Write(") - 1)");

					return m;
			}
		}

		return base.VisitMemberAccess(m);
	}

	/// <summary>
	/// Visits a method call expression and translates it to T-SQL syntax.
	/// </summary>
	/// <param name="m">The method call expression to visit.</param>
	/// <returns>The visited expression.</returns>
	/// <remarks>
	/// Translates method calls from various types (String, DateTime, Math, Decimal) to their T-SQL equivalents.
	/// Handles string operations (Contains, StartsWith, EndsWith, Substring, etc.), date/time arithmetic,
	/// mathematical functions, and type comparisons.
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
						throw new NullReferenceException(SR.ErrExpectedExpression);

					Visit(m.Object);

					Write(" LIKE ");
					Visit(m.Arguments[0]);
					Write(" + '%')");

					return m;
				case "EndsWith":
					/*
					 * Convert string.EndsWith to LIKE pattern
					 */
					Write("(");

					if (m.Object is null)
						throw new NullReferenceException(SR.ErrExpectedExpression);

					Visit(m.Object);

					Write(" LIKE '%' + ");
					Visit(m.Arguments[0]);
					Write(")");

					return m;
				case "Contains":
					/*
					 * Convert string.Contains to LIKE pattern
					 */
					Write("(");

					if (m.Object is null)
						throw new NullReferenceException(SR.ErrExpectedExpression);

					Visit(m.Object);
					Write(" LIKE '%' + ");
					Visit(m.Arguments[0]);
					Write(" + '%')");

					return m;
				case "Concat":
					/*
					 * Convert string.Concat to + operator
					 */
					var args = m.Arguments;

					if (args.Count == 1 && args[0].NodeType == ExpressionType.NewArrayInit)
						args = ((NewArrayExpression)args[0]).Expressions;

					for (var i = 0; i < args.Count; i++)
					{
						if (i > 0)
							Write(" + ");

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
						throw new NullReferenceException(SR.ErrExpectedExpression);

					Visit(m.Object);
					Write(")");

					return m;
				case "ToLower":
					/*
					 * Convert string.ToLower to LOWER() function
					 */
					Write("LOWER(");

					if (m.Object is null)
						throw new NullReferenceException(SR.ErrExpectedExpression);

					Visit(m.Object);
					Write(")");

					return m;
				case "Replace":
					/*
					 * Convert string.Replace to REPLACE() function
					 */
					Write("REPLACE(");

					if (m.Object is null)
						throw new NullReferenceException(SR.ErrExpectedExpression);

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
					 * Note: T-SQL SUBSTRING is 1-based, so we add 1 to the start index
					 */
					Write("SUBSTRING(");

					if (m.Object is null)
						throw new NullReferenceException(SR.ErrExpectedExpression);

					Visit(m.Object);

					Write(", ");
					Visit(m.Arguments[0]);
					Write(" + 1, ");

					if (m.Arguments.Count == 2)
						Visit(m.Arguments[1]);
					else
						Write("8000");

					Write(")");

					return m;
				case "Remove":
					/*
					 * Convert string.Remove to STUFF() function
					 */
					Write("STUFF(");

					if (m.Object is null)
						throw new NullReferenceException(SR.ErrExpectedExpression);

					Visit(m.Object);

					Write(", ");
					Visit(m.Arguments[0]);
					Write(" + 1, ");

					if (m.Arguments.Count == 2)
						Visit(m.Arguments[1]);
					else
						Write("8000");

					Write(", '')");

					return m;
				case "IndexOf":
					/*
					 * Convert string.IndexOf to CHARINDEX() function
					 * Subtract 1 to convert from 1-based to 0-based indexing
					 */
					Write("(CHARINDEX(");
					Visit(m.Arguments[0]);
					Write(", ");

					if (m.Object is null)
						throw new NullReferenceException(SR.ErrExpectedExpression);

					Visit(m.Object);

					if (m.Arguments.Count == 2 && m.Arguments[1].Type == typeof(int))
					{
						Write(", ");
						Visit(m.Arguments[1]);
						Write(" + 1");
					}

					Write(") - 1)");

					return m;
				case "Trim":
					/*
					 * Convert string.Trim to RTRIM(LTRIM()) functions
					 */
					Write("RTRIM(LTRIM(");

					if (m.Object is null)
						throw new NullReferenceException(SR.ErrExpectedExpression);

					Visit(m.Object);
					Write("))");

					return m;
			}
		}
		else if (m.Method.DeclaringType == typeof(DateTime))
		{
			switch (m.Method.Name)
			{
				case "op_Subtract":
					/*
					 * Convert DateTime subtraction to DATEDIFF() function
					 */
					if (m.Arguments[1].Type == typeof(DateTime))
					{
						Write("DATEDIFF(");
						Visit(m.Arguments[0]);
						Write(", ");
						Visit(m.Arguments[1]);
						Write(")");
						return m;
					}
					break;
				case "AddYears":
					/*
					 * Convert DateTime.AddYears to DATEADD(YYYY, ...) function
					 */
					Write("DATEADD(YYYY,");
					Visit(m.Arguments[0]);
					Write(",");

					if (m.Object is null)
						throw new NullReferenceException(SR.ErrExpectedExpression);

					Visit(m.Object);
					Write(")");

					return m;
				case "AddMonths":
					/*
					 * Convert DateTime.AddMonths to DATEADD(MM, ...) function
					 */
					Write("DATEADD(MM,");
					Visit(m.Arguments[0]);
					Write(",");

					if (m.Object is null)
						throw new NullReferenceException(SR.ErrExpectedExpression);

					Visit(m.Object);
					Write(")");

					return m;
				case "AddDays":
					/*
					 * Convert DateTime.AddDays to DATEADD(DAY, ...) function
					 */
					Write("DATEADD(DAY,");
					Visit(m.Arguments[0]);
					Write(",");

					if (m.Object is null)
						throw new NullReferenceException(SR.ErrExpectedExpression);

					Visit(m.Object);
					Write(")");

					return m;
				case "AddHours":
					/*
					 * Convert DateTime.AddHours to DATEADD(HH, ...) function
					 */
					Write("DATEADD(HH,");
					Visit(m.Arguments[0]);
					Write(",");

					if (m.Object is null)
						throw new NullReferenceException(SR.ErrExpectedExpression);

					Visit(m.Object);
					Write(")");

					return m;
				case "AddMinutes":
					/*
					 * Convert DateTime.AddMinutes to DATEADD(MI, ...) function
					 */
					Write("DATEADD(MI,");
					Visit(m.Arguments[0]);
					Write(",");

					if (m.Object is null)
						throw new NullReferenceException(SR.ErrExpectedExpression);

					Visit(m.Object);
					Write(")");

					return m;
				case "AddSeconds":
					/*
					 * Convert DateTime.AddSeconds to DATEADD(SS, ...) function
					 */
					Write("DATEADD(SS,");
					Visit(m.Arguments[0]);
					Write(",");

					if (m.Object is null)
						throw new NullReferenceException(SR.ErrExpectedExpression);

					Visit(m.Object);
					Write(")");

					return m;
				case "AddMilliseconds":
					/*
					 * Convert DateTime.AddMilliseconds to DATEADD(MS, ...) function
					 */
					Write("DATEADD(MS,");
					Visit(m.Arguments[0]);
					Write(",");

					if (m.Object is null)
						throw new NullReferenceException(SR.ErrExpectedExpression);

					Visit(m.Object);
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
					 * Convert decimal.Ceiling/Floor to T-SQL CEILING/FLOOR functions
					 */
					Write(m.Method.Name.ToUpper());
					Write("(");
					Visit(m.Arguments[0]);
					Write(")");
					return m;
				case "Round":
					/*
					 * Convert decimal.Round to T-SQL ROUND function
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
					 * Convert decimal.Truncate to ROUND with truncation mode
					 */
					Write("ROUND(");
					Visit(m.Arguments[0]);
					Write(", 0, 1)");
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
				case "Log10":
				case "Sin":
				case "Tan":
				case "Sqrt":
				case "Sign":
				case "Ceiling":
				case "Floor":
					/*
					 * Convert Math functions to their T-SQL equivalents
					 */
					Write(m.Method.Name.ToUpper());
					Write("(");
					Visit(m.Arguments[0]);
					Write(")");
					return m;
				case "Atan2":
					/*
					 * Convert Math.Atan2 to ATN2 function
					 */
					Write("ATN2(");
					Visit(m.Arguments[0]);
					Write(", ");
					Visit(m.Arguments[1]);
					Write(")");
					return m;
				case "Log":
					/*
					 * Convert Math.Log to LOG10 if single argument
					 */
					if (m.Arguments.Count == 1)
						goto case "Log10";

					break;
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
					 * Convert Math.Round to T-SQL ROUND function
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
					 * Convert Math.Truncate to ROUND with truncation mode
					 */
					Write("ROUND(");
					Visit(m.Arguments[0]);
					Write(", 0, 1)");
					return m;
			}
		}

		if (m.Method.Name == "ToString")
		{
			/*
			 * Convert ToString() calls to CONVERT(NVARCHAR, ...) for non-string types
			 */
			if (m.Object?.Type != typeof(string))
			{
				Write("CONVERT(NVARCHAR, ");

				if (m.Object is null)
					throw new NullReferenceException(SR.ErrExpectedExpression);

				Visit(m.Object);

				Write(")");
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
				throw new NullReferenceException(SR.ErrExpectedExpression);

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
			 * Convert IList<T>.Contains to IN operator
			 */
			if (string.Equals(m.Method.Name, "Contains", StringComparison.Ordinal))
			{
				Visit(m.Arguments[0]);
				Write(" IN (");

				if (m.Object is null)
					throw new NullReferenceException(SR.ErrExpectedExpression);

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
	/// Visits a new object expression and translates it to T-SQL syntax.
	/// </summary>
	/// <param name="nex">The new expression to visit.</param>
	/// <returns>The visited expression.</returns>
	/// <remarks>
	/// Handles DateTime constructor calls by converting them to T-SQL CONVERT expressions with string concatenation.
	/// </remarks>
	protected override NewExpression VisitNew(NewExpression nex)
	{
		if (nex.Constructor?.DeclaringType == typeof(DateTime))
		{
			if (nex.Arguments.Count == 3)
			{
				/*
				 * Convert DateTime constructor with date components to CONVERT expression
				 */
				Write("Convert(DateTime, ");
				Write("Convert(nvarchar, ");
				Visit(nex.Arguments[0]);
				Write(") + '/' + ");
				Write("Convert(nvarchar, ");
				Visit(nex.Arguments[1]);
				Write(") + '/' + ");
				Write("Convert(nvarchar, ");
				Visit(nex.Arguments[2]);
				Write("))");
				return nex;
			}
			else if (nex.Arguments.Count == 6)
			{
				/*
				 * Convert DateTime constructor with date and time components to CONVERT expression
				 */
				Write("Convert(DateTime, ");
				Write("Convert(nvarchar, ");
				Visit(nex.Arguments[0]);
				Write(") + '/' + ");
				Write("Convert(nvarchar, ");
				Visit(nex.Arguments[1]);
				Write(") + '/' + ");
				Write("Convert(nvarchar, ");
				Visit(nex.Arguments[2]);
				Write(") + ' ' + ");
				Write("Convert(nvarchar, ");
				Visit(nex.Arguments[3]);
				Write(") + ':' + ");
				Write("Convert(nvarchar, ");
				Visit(nex.Arguments[4]);
				Write(") + ':' + ");
				Write("Convert(nvarchar, ");
				Visit(nex.Arguments[5]);
				Write("))");
				return nex;
			}
		}

		return base.VisitNew(nex);
	}

	/// <summary>
	/// Visits a binary expression and translates it to T-SQL syntax.
	/// </summary>
	/// <param name="b">The binary expression to visit.</param>
	/// <returns>The visited expression.</returns>
	/// <remarks>
	/// Handles special binary operations like Power, Coalesce, LeftShift, and RightShift by converting
	/// them to T-SQL POWER, COALESCE, and bitwise shift operations using multiplication/division.
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
			 * Convert left shift to multiplication by power of 2
			 */
			Write("(");
			VisitValue(b.Left);
			Write(" * POWER(2, ");
			VisitValue(b.Right);
			Write("))");
			return b;
		}
		else if (b.NodeType == ExpressionType.RightShift)
		{
			/*
			 * Convert right shift to division by power of 2
			 */
			Write("(");
			VisitValue(b.Left);
			Write(" / POWER(2, ");
			VisitValue(b.Right);
			Write("))");
			return b;
		}

		return base.VisitBinary(b);
	}

	/// <summary>
	/// Visits a constant expression and translates it to T-SQL syntax.
	/// </summary>
	/// <param name="c">The constant expression to visit.</param>
	/// <returns>The visited expression.</returns>
	/// <remarks>
	/// Handles enumerable constants by expanding them inline, and parameter references by writing
	/// parameter names prefixed with @.
	/// </remarks>
	protected override Expression VisitConstant(ConstantExpression c)
	{
		if (c.Value is not null && c.Value.GetType().IsEnumerable())
		{
			/*
			 * Expand enumerable constants inline
			 */
			var en = ((IEnumerable)c.Value).GetEnumerator();
			var first = true;

			while (en.MoveNext())
			{
				var value = en.Current?.ToString();

				if (value is null)
					continue;

				if (!first)
					Write(", ");
				else
					first = false;

				if (NeedsEscaping(en.Current))
					Write($"'{value}'");
				else
					Write(value);
			}

			return c;
		}
		else
		{
			/*
			 * Check if constant is a registered parameter and write parameter reference
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
	/// Visits a conditional expression and translates it to T-SQL CASE expression.
	/// </summary>
	/// <param name="c">The conditional expression to visit.</param>
	/// <returns>The visited expression.</returns>
	/// <remarks>
	/// Converts conditional expressions to T-SQL CASE WHEN THEN ELSE END syntax, handling
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
			 * Handle value-based conditional (test value 0 = false, else true)
			 */
			Write("(CASE ");
			VisitValue(c.Test);
			Write(" WHEN 0 THEN ");
			VisitValue(c.IfFalse);
			Write(" ELSE ");
			VisitValue(c.IfTrue);
			Write(" END)");
		}

		return c;
	}

	/// <summary>
	/// Visits a row number expression and translates it to T-SQL ROW_NUMBER() function.
	/// </summary>
	/// <param name="rowNumber">The row number expression to visit.</param>
	/// <returns>The visited expression.</returns>
	/// <remarks>
	/// Generates T-SQL ROW_NUMBER() OVER(ORDER BY ...) clause for ranking and pagination.
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
	/// Visits an IF command expression and translates it to T-SQL IF...BEGIN...END syntax.
	/// </summary>
	/// <param name="ifx">The IF command expression to visit.</param>
	/// <returns>The visited expression.</returns>
	/// <remarks>
	/// Generates T-SQL IF statement with BEGIN/END blocks for true and optional ELSE blocks for false conditions.
	/// Only used when the language allows multiple commands.
	/// </remarks>
	protected override Expression VisitIf(IfCommandExpression ifx)
	{
		if (Language is not null && !Language.AllowsMultipleCommands)
			return base.VisitIf(ifx);

		Write("IF ");
		Visit(ifx.Check);
		WriteLine(Indentation.Same);
		Write("BEGIN");
		WriteLine(Indentation.Inner);
		VisitStatement(ifx.IfTrue);
		WriteLine(Indentation.Outer);

		if (ifx.IfFalse is not null)
		{
			Write("END ELSE BEGIN");
			WriteLine(Indentation.Inner);
			VisitStatement(ifx.IfFalse);
			WriteLine(Indentation.Outer);
		}

		Write("END");

		return ifx;
	}

	/// <summary>
	/// Visits a block expression containing multiple commands.
	/// </summary>
	/// <param name="block">The block expression to visit.</param>
	/// <returns>The visited expression.</returns>
	/// <remarks>
	/// Processes multiple statements in sequence with proper spacing.
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
				WriteLine(Indentation.Same);
				WriteLine(Indentation.Same);
			}

			VisitStatement(block.Commands[i]);
		}

		return block;
	}

	/// <summary>
	/// Visits a variable declaration expression and translates it to T-SQL DECLARE and SELECT/SET statements.
	/// </summary>
	/// <param name="decl">The declaration expression to visit.</param>
	/// <returns>The visited expression.</returns>
	/// <remarks>
	/// Generates T-SQL variable declarations with DECLARE statements and initializes them using either
	/// SELECT statements (for query-based initialization) or SET statements (for expression-based initialization).
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
			 * Declare each variable with its data type
			 */
			Write("DECLARE @");
			Write(v.Name);
			Write(" ");

			if (Language is not null)
				Write(Language.TypeSystem.Format(v.DataType, false));
		}

		if (decl.Source is not null)
		{
			/*
			 * Initialize variables from a SELECT query
			 */
			WriteLine(Indentation.Same);
			Write("SELECT ");

			for (var i = 0; i < decl.Variables.Count; i++)
			{
				if (i > 0)
					Write(", ");

				Write("@");
				Write(decl.Variables[i].Name);
				Write(" = ");
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
		}
		else
		{
			/*
			 * Initialize variables with SET statements
			 */
			for (var i = 0; i < decl.Variables.Count; i++)
			{
				var v = decl.Variables[i];

				if (v.Expression is not null)
				{
					WriteLine(Indentation.Same);
					Write("SET @");
					Write(v.Name);
					Write(" = ");
					Visit(v.Expression);
				}
			}
		}

		return decl;
	}

	private static bool NeedsEscaping(object? value)
	{
		if (value is null)
			return false;

		var type = value.GetType().ToDbType();

		switch (type)
		{
			case System.Data.DbType.AnsiString:
			case System.Data.DbType.Date:
			case System.Data.DbType.DateTime:
			case System.Data.DbType.Guid:
			case System.Data.DbType.String:
			case System.Data.DbType.Time:
			case System.Data.DbType.AnsiStringFixedLength:
			case System.Data.DbType.StringFixedLength:
			case System.Data.DbType.Xml:
			case System.Data.DbType.DateTime2:
			case System.Data.DbType.DateTimeOffset:
				return true;
			default:
				return false;
		}
	}
}
