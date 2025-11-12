using Connected.Data.Expressions;
using Connected.Data.Expressions.Expressions;
using Connected.Data.Expressions.Formatters;
using Connected.Data.Expressions.Languages;
using Connected.Reflection;
using System.Collections;
using System.Linq.Expressions;

namespace Connected.Storage.Oracle.Query;

/// <summary>
/// Provides Oracle specific formatting capabilities for converting expression trees into Oracle query strings.
/// </summary>
/// <remarks>
/// This sealed class extends <see cref="SqlFormatter"/> to handle Oracle dialect-specific syntax and functions,
/// including string manipulation, date/time operations, mathematical functions, and aggregate functions.
/// It translates LINQ expression trees into executable Oracle SQL statements with proper parameter handling.
/// Oracle-specific features include bind variables with colon prefix (:param), ROWNUM for pagination,
/// DUAL table for queries without FROM, and Oracle-specific functions like LENGTH, SUBSTR, INSTR, and TO_DATE.
/// </remarks>
internal sealed class OracleFormatter(ExpressionCompilationContext context, QueryLanguage? language)
		: SqlFormatter(language)
{
	/// <summary>
	/// Gets the expression compilation context containing parameter information and metadata.
	/// </summary>
	public ExpressionCompilationContext Context { get; } = context;

	/// <summary>
	/// Formats an expression tree into an Oracle query string using the default Oracle language.
	/// </summary>
	/// <param name="context">The expression compilation context.</param>
	/// <param name="expression">The expression tree to format.</param>
	/// <returns>An Oracle query string representation of the expression.</returns>
	public static string Format(ExpressionCompilationContext context, Expression expression)
	{
		return Format(context, expression, new OracleLanguage());
	}

	/// <summary>
	/// Formats an expression tree into an Oracle query string using the specified query language.
	/// </summary>
	/// <param name="context">The expression compilation context.</param>
	/// <param name="expression">The expression tree to format.</param>
	/// <param name="language">The query language configuration to use.</param>
	/// <returns>An Oracle query string representation of the expression.</returns>
	public static string Format(ExpressionCompilationContext context, Expression expression, QueryLanguage language)
	{
		var formatter = new OracleFormatter(context, language);

		formatter.Visit(expression);

		return formatter.ToString();
	}

	/// <summary>
	/// Writes the Oracle specific aggregate function name.
	/// </summary>
	/// <param name="aggregateName">The name of the aggregate function.</param>
	/// <remarks>
	/// Oracle aggregate functions are mostly standard SQL. LongCount is handled as COUNT which returns
	/// NUMBER type capable of holding large values in Oracle.
	/// </remarks>
	protected override void WriteAggregateName(string aggregateName)
	{
		if (string.Equals(aggregateName, "LongCount", StringComparison.Ordinal))
			Write("COUNT");
		else
			base.WriteAggregateName(aggregateName);
	}

	/// <summary>
	/// Visits a member access expression and translates it to Oracle syntax.
	/// </summary>
	/// <param name="m">The member access expression to visit.</param>
	/// <returns>The visited expression.</returns>
	/// <remarks>
	/// Handles member access for string properties (e.g., Length) and DateTime/DateTimeOffset properties
	/// (e.g., Day, Month, Year, Hour) by converting them to appropriate Oracle functions using LENGTH
	/// and EXTRACT functions.
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
					 * Convert DateTime.Hour to EXTRACT(HOUR FROM CAST(... AS TIMESTAMP))
					 */
					Write("EXTRACT(HOUR FROM CAST(");

					if (m.Expression is null)
						throw new NullReferenceException(Schemas.SR.ErrExpectedExpression);

					Visit(m.Expression);

					Write(" AS TIMESTAMP))");

					return m;
				case "Minute":
					/*
					 * Convert DateTime.Minute to EXTRACT(MINUTE FROM CAST(... AS TIMESTAMP))
					 */
					Write("EXTRACT(MINUTE FROM CAST(");

					if (m.Expression is null)
						throw new NullReferenceException(Schemas.SR.ErrExpectedExpression);

					Visit(m.Expression);

					Write(" AS TIMESTAMP))");

					return m;
				case "Second":
					/*
					 * Convert DateTime.Second to EXTRACT(SECOND FROM CAST(... AS TIMESTAMP))
					 */
					Write("FLOOR(EXTRACT(SECOND FROM CAST(");

					if (m.Expression is null)
						throw new NullReferenceException(Schemas.SR.ErrExpectedExpression);

					Visit(m.Expression);

					Write(" AS TIMESTAMP)))");

					return m;
				case "Millisecond":
					/*
					 * Convert DateTime.Millisecond to EXTRACT milliseconds
					 */
					Write("MOD(EXTRACT(SECOND FROM CAST(");

					if (m.Expression is null)
						throw new NullReferenceException(Schemas.SR.ErrExpectedExpression);

					Visit(m.Expression);

					Write(" AS TIMESTAMP)) * 1000, 1000)");

					return m;
				case "DayOfWeek":
					/*
					 * Convert DateTime.DayOfWeek to TO_CHAR(date, 'D') - 1 (zero-based, Sunday=0)
					 */
					Write("(TO_NUMBER(TO_CHAR(");

					if (m.Expression is null)
						throw new NullReferenceException(Schemas.SR.ErrExpectedExpression);

					Visit(m.Expression);

					Write(", 'D')) - 1)");

					return m;
				case "DayOfYear":
					/*
					 * Convert DateTime.DayOfYear to TO_CHAR(date, 'DDD') - 1 (zero-based)
					 */
					Write("(TO_NUMBER(TO_CHAR(");

					if (m.Expression is null)
						throw new NullReferenceException(Schemas.SR.ErrExpectedExpression);

					Visit(m.Expression);

					Write(", 'DDD')) - 1)");

					return m;
			}
		}

		return base.VisitMemberAccess(m);
	}

	/// <summary>
	/// Visits a method call expression and translates it to Oracle syntax.
	/// </summary>
	/// <param name="m">The method call expression to visit.</param>
	/// <returns>The visited expression.</returns>
	/// <remarks>
	/// Translates method calls from various types (String, DateTime, Math, Decimal) to their Oracle equivalents.
	/// Handles string operations (Contains, StartsWith, EndsWith, Substring using SUBSTR, etc.), date/time arithmetic
	/// using INTERVAL, mathematical functions, and type comparisons. Oracle uses 1-based string indexing unlike
	/// .NET's 0-based indexing, requiring index adjustments.
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
					 * Convert string.Contains to INSTR function (returns position, >0 means found)
					 */
					Write("(INSTR(");

					if (m.Object is null)
						throw new NullReferenceException(Schemas.SR.ErrExpectedExpression);

					Visit(m.Object);
					Write(", ");
					Visit(m.Arguments[0]);
					Write(") > 0)");

					return m;
				case "Concat":
					/*
					 * Convert string.Concat to || operator (Oracle concatenation)
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
					 * Convert string.IsNullOrEmpty to IS NULL check (Oracle treats '' as NULL)
					 */
					Write("(");
					Visit(m.Arguments[0]);
					Write(" IS NULL)");
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
					 * Convert string.Substring to SUBSTR() function
					 * Note: Oracle SUBSTR is 1-based, so add 1 to start index
					 */
					Write("SUBSTR(");

					if (m.Object is null)
						throw new NullReferenceException(Schemas.SR.ErrExpectedExpression);

					Visit(m.Object);

					Write(", ");
					Visit(m.Arguments[0]);
					Write(" + 1");

					if (m.Arguments.Count == 2)
					{
						Write(", ");
						Visit(m.Arguments[1]);
					}

					Write(")");

					return m;
				case "Remove":
					/*
					 * Convert string.Remove using combination of SUBSTR
					 */
					Write("(SUBSTR(");

					if (m.Object is null)
						throw new NullReferenceException(Schemas.SR.ErrExpectedExpression);

					Visit(m.Object);
					Write(", 1, ");
					Visit(m.Arguments[0]);
					Write(")");

					if (m.Arguments.Count == 1)
					{
						/*
						 * Remove to end - just return substring up to index
						 */
						Write(")");
					}
					else
					{
						/*
						 * Remove specific length - concatenate before and after parts
						 */
						Write(" || SUBSTR(");
						Visit(m.Object);
						Write(", ");
						Visit(m.Arguments[0]);
						Write(" + ");
						Visit(m.Arguments[1]);
						Write(" + 1))");
					}

					return m;
				case "IndexOf":
					/*
					 * Convert string.IndexOf to INSTR() function
					 * Subtract 1 to convert from 1-based to 0-based indexing
					 */
					Write("(INSTR(");

					if (m.Object is null)
						throw new NullReferenceException(Schemas.SR.ErrExpectedExpression);

					Visit(m.Object);
					Write(", ");
					Visit(m.Arguments[0]);

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
					 * Oracle returns NUMBER of days for date subtraction
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
					 * Convert DateTime.AddYears to ADD_MONTHS
					 */
					Write("ADD_MONTHS(");

					if (m.Object is null)
						throw new NullReferenceException(Schemas.SR.ErrExpectedExpression);

					Visit(m.Object);
					Write(", ");
					Visit(m.Arguments[0]);
					Write(" * 12)");

					return m;
				case "AddMonths":
					/*
					 * Convert DateTime.AddMonths to ADD_MONTHS
					 */
					Write("ADD_MONTHS(");

					if (m.Object is null)
						throw new NullReferenceException(Schemas.SR.ErrExpectedExpression);

					Visit(m.Object);
					Write(", ");
					Visit(m.Arguments[0]);
					Write(")");

					return m;
				case "AddDays":
					/*
					 * Convert DateTime.AddDays to date arithmetic
					 */
					Write("(");

					if (m.Object is null)
						throw new NullReferenceException(Schemas.SR.ErrExpectedExpression);

					Visit(m.Object);
					Write(" + ");
					Visit(m.Arguments[0]);
					Write(")");

					return m;
				case "AddHours":
					/*
					 * Convert DateTime.AddHours to date arithmetic (divide by 24)
					 */
					Write("(");

					if (m.Object is null)
						throw new NullReferenceException(Schemas.SR.ErrExpectedExpression);

					Visit(m.Object);
					Write(" + ");
					Visit(m.Arguments[0]);
					Write(" / 24)");

					return m;
				case "AddMinutes":
					/*
					 * Convert DateTime.AddMinutes to date arithmetic (divide by 1440)
					 */
					Write("(");

					if (m.Object is null)
						throw new NullReferenceException(Schemas.SR.ErrExpectedExpression);

					Visit(m.Object);
					Write(" + ");
					Visit(m.Arguments[0]);
					Write(" / 1440)");

					return m;
				case "AddSeconds":
					/*
					 * Convert DateTime.AddSeconds to date arithmetic (divide by 86400)
					 */
					Write("(");

					if (m.Object is null)
						throw new NullReferenceException(Schemas.SR.ErrExpectedExpression);

					Visit(m.Object);
					Write(" + ");
					Visit(m.Arguments[0]);
					Write(" / 86400)");

					return m;
				case "AddMilliseconds":
					/*
					 * Convert DateTime.AddMilliseconds to date arithmetic (divide by 86400000)
					 */
					Write("(");

					if (m.Object is null)
						throw new NullReferenceException(Schemas.SR.ErrExpectedExpression);

					Visit(m.Object);
					Write(" + ");
					Visit(m.Arguments[0]);
					Write(" / 86400000)");

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
					 * Convert decimal.Ceiling/Floor to Oracle CEIL/FLOOR functions
					 */
					Write(m.Method.Name == "Ceiling" ? "CEIL(" : "FLOOR(");
					Visit(m.Arguments[0]);
					Write(")");
					return m;
				case "Round":
					/*
					 * Convert decimal.Round to Oracle ROUND function
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
					 * Convert Math functions to their Oracle equivalents
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
					Write("LOG(10, ");
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
					 * Convert Math.Round to Oracle ROUND function
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
			 * Convert ToString() calls to TO_CHAR for non-string types
					 */
			if (m.Object?.Type != typeof(string))
			{
				Write("TO_CHAR(");

				if (m.Object is null)
					throw new NullReferenceException(Schemas.SR.ErrExpectedExpression);

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
			 * Convert IList<T>.Contains to IN operator
			 */
			if (string.Equals(m.Method.Name, "Contains", StringComparison.Ordinal))
			{
				Visit(m.Arguments[0]);
				Write(" IN (");

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
	/// Visits a new object expression and translates it to Oracle syntax.
	/// </summary>
	/// <param name="nex">The new expression to visit.</param>
	/// <returns>The visited expression.</returns>
	/// <remarks>
	/// Handles DateTime constructor calls by converting them to Oracle TO_DATE function with
	/// appropriate format strings.
	/// </remarks>
	protected override NewExpression VisitNew(NewExpression nex)
	{
		if (nex.Constructor?.DeclaringType == typeof(DateTime))
		{
			if (nex.Arguments.Count == 3)
			{
				/*
				 * Convert DateTime constructor with date components to TO_DATE
				 */
				Write("TO_DATE(");
				Visit(nex.Arguments[0]);
				Write(" || '-' || ");
				Visit(nex.Arguments[1]);
				Write(" || '-' || ");
				Visit(nex.Arguments[2]);
				Write(", 'YYYY-MM-DD')");
				return nex;
			}
			else if (nex.Arguments.Count == 6)
			{
				/*
				 * Convert DateTime constructor with date and time components to TO_TIMESTAMP
				 */
				Write("TO_TIMESTAMP(");
				Visit(nex.Arguments[0]);
				Write(" || '-' || ");
				Visit(nex.Arguments[1]);
				Write(" || '-' || ");
				Visit(nex.Arguments[2]);
				Write(" || ' ' || ");
				Visit(nex.Arguments[3]);
				Write(" || ':' || ");
				Visit(nex.Arguments[4]);
				Write(" || ':' || ");
				Visit(nex.Arguments[5]);
				Write(", 'YYYY-MM-DD HH24:MI:SS')");
				return nex;
			}
		}

		return base.VisitNew(nex);
	}

	/// <summary>
	/// Visits a binary expression and translates it to Oracle syntax.
	/// </summary>
	/// <param name="b">The binary expression to visit.</param>
	/// <returns>The visited expression.</returns>
	/// <remarks>
	/// Handles special binary operations like Power, Coalesce by converting them to Oracle
	/// POWER and NVL/COALESCE functions.
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
			 * Convert Coalesce operator to NVL or COALESCE function
			 */
			Write("NVL(");
			VisitValue(b.Left);
			Write(", ");
			VisitValue(b.Right);
			Write(")");

			return b;
		}

		return base.VisitBinary(b);
	}

	/// <summary>
	/// Visits a constant expression and translates it to Oracle syntax.
	/// </summary>
	/// <param name="c">The constant expression to visit.</param>
	/// <returns>The visited expression.</returns>
	/// <remarks>
	/// Handles enumerable constants by expanding them inline, and parameter references by writing
	/// parameter names prefixed with : (Oracle bind variable style).
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

				Write(value);
			}

			return c;
		}
		else
		{
			/*
			 * Check if constant is a registered parameter and write parameter reference
			 * Oracle uses colon prefix for bind variables (:param)
			 */
			var parameter = Context.Parameters.FirstOrDefault(f => f.Value == c);

			if (parameter.Value is not null)
			{
				Write($":{parameter.Key}");

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
	/// Visits a conditional expression and translates it to Oracle CASE expression.
	/// </summary>
	/// <param name="c">The conditional expression to visit.</param>
	/// <returns>The visited expression.</returns>
	/// <remarks>
	/// Converts conditional expressions to Oracle CASE WHEN THEN ELSE END syntax.
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
			 * Handle value-based conditional
			 */
			Write("(CASE WHEN ");
			VisitValue(c.Test);
			Write(" = 0 THEN ");
			VisitValue(c.IfFalse);
			Write(" ELSE ");
			VisitValue(c.IfTrue);
			Write(" END)");
		}

		return c;
	}

	/// <summary>
	/// Visits a row number expression and translates it to Oracle ROW_NUMBER() function.
	/// </summary>
	/// <param name="rowNumber">The row number expression to visit.</param>
	/// <returns>The visited expression.</returns>
	/// <remarks>
	/// Generates Oracle ROW_NUMBER() OVER(ORDER BY ...) clause for ranking.
	/// Oracle also supports traditional ROWNUM for simpler pagination scenarios.
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
}
