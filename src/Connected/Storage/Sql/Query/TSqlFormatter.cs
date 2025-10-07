using Connected.Data.Expressions;
using Connected.Data.Expressions.Expressions;
using Connected.Data.Expressions.Formatters;
using Connected.Data.Expressions.Languages;
using Connected.Reflection;
using System.Collections;
using System.Linq.Expressions;

namespace Connected.Storage.Sql.Query;

internal sealed class TSqlFormatter : SqlFormatter
{

	public TSqlFormatter(ExpressionCompilationContext context, QueryLanguage? language)
		  : base(language)
	{
		Context = context;
	}
	public ExpressionCompilationContext Context { get; }

	public static string Format(ExpressionCompilationContext context, Expression expression)
	{
		return Format(context, expression, new TSqlLanguage());
	}

	public static string Format(ExpressionCompilationContext context, Expression expression, QueryLanguage language)
	{
		var formatter = new TSqlFormatter(context, language);

		formatter.Visit(expression);

		return formatter.ToString();
	}
	protected override void WriteAggregateName(string aggregateName)
	{
		if (string.Equals(aggregateName, "LongCount", StringComparison.Ordinal))
			Write("COUNT_BIG");
		else
			base.WriteAggregateName(aggregateName);
	}
	protected override Expression VisitMemberAccess(MemberExpression m)
	{
		if (m.Member.DeclaringType == typeof(string))
		{
			switch (m.Member.Name)
			{
				case "Length":
					Write("LEN(");
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
					Write("DAY(");
					Visit(m.Expression);
					Write(")");
					return m;
				case "Month":
					Write("MONTH(");
					Visit(m.Expression);
					Write(")");
					return m;
				case "Year":
					Write("YEAR(");
					Visit(m.Expression);
					Write(")");
					return m;
				case "Hour":
					Write("DATEPART(hour, ");
					Visit(m.Expression);
					Write(")");
					return m;
				case "Minute":
					Write("DATEPART(minute, ");
					Visit(m.Expression);
					Write(")");
					return m;
				case "Second":
					Write("DATEPART(second, ");
					Visit(m.Expression);
					Write(")");
					return m;
				case "Millisecond":
					Write("DATEPART(millisecond, ");
					Visit(m.Expression);
					Write(")");
					return m;
				case "DayOfWeek":
					Write("(DATEPART(weekday, ");
					Visit(m.Expression);
					Write(") - 1)");
					return m;
				case "DayOfYear":
					Write("(DATEPART(dayofyear, ");
					Visit(m.Expression);
					Write(") - 1)");
					return m;
			}
		}

		return base.VisitMemberAccess(m);
	}

	protected override Expression VisitMethodCall(MethodCallExpression m)
	{
		if (m.Method.DeclaringType == typeof(string))
		{
			switch (m.Method.Name)
			{
				case "StartsWith":
					Write("(");
					Visit(m.Object);
					Write(" LIKE ");
					Visit(m.Arguments[0]);
					Write(" + '%')");
					return m;
				case "EndsWith":
					Write("(");
					Visit(m.Object);
					Write(" LIKE '%' + ");
					Visit(m.Arguments[0]);
					Write(")");
					return m;
				case "Contains":
					Write("(");
					Visit(m.Object);
					Write(" LIKE '%' + ");
					Visit(m.Arguments[0]);
					Write(" + '%')");
					return m;
				case "Concat":
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
					Write("(");
					Visit(m.Arguments[0]);
					Write(" IS NULL OR ");
					Visit(m.Arguments[0]);
					Write(" = '')");
					return m;
				case "ToUpper":
					Write("UPPER(");
					Visit(m.Object);
					Write(")");
					return m;
				case "ToLower":
					Write("LOWER(");
					Visit(m.Object);
					Write(")");
					return m;
				case "Replace":
					Write("REPLACE(");
					Visit(m.Object);
					Write(", ");
					Visit(m.Arguments[0]);
					Write(", ");
					Visit(m.Arguments[1]);
					Write(")");
					return m;
				case "Substring":
					Write("SUBSTRING(");
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
					Write("STUFF(");
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
					Write("(CHARINDEX(");
					Visit(m.Arguments[0]);
					Write(", ");
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
					Write("RTRIM(LTRIM(");
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
					Write("DATEADD(YYYY,");
					Visit(m.Arguments[0]);
					Write(",");
					Visit(m.Object);
					Write(")");
					return m;
				case "AddMonths":
					Write("DATEADD(MM,");
					Visit(m.Arguments[0]);
					Write(",");
					Visit(m.Object);
					Write(")");
					return m;
				case "AddDays":
					Write("DATEADD(DAY,");
					Visit(m.Arguments[0]);
					Write(",");
					Visit(m.Object);
					Write(")");
					return m;
				case "AddHours":
					Write("DATEADD(HH,");
					Visit(m.Arguments[0]);
					Write(",");
					Visit(m.Object);
					Write(")");
					return m;
				case "AddMinutes":
					Write("DATEADD(MI,");
					Visit(m.Arguments[0]);
					Write(",");
					Visit(m.Object);
					Write(")");
					return m;
				case "AddSeconds":
					Write("DATEADD(SS,");
					Visit(m.Arguments[0]);
					Write(",");
					Visit(m.Object);
					Write(")");
					return m;
				case "AddMilliseconds":
					Write("DATEADD(MS,");
					Visit(m.Arguments[0]);
					Write(",");
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
					Write("(");
					VisitValue(m.Arguments[0]);
					Write(" ");
					Write(GetOperator(m.Method.Name));
					Write(" ");
					VisitValue(m.Arguments[1]);
					Write(")");
					return m;
				case "Negate":
					Write("-");
					Visit(m.Arguments[0]);
					Write("");
					return m;
				case "Ceiling":
				case "Floor":
					Write(m.Method.Name.ToUpper());
					Write("(");
					Visit(m.Arguments[0]);
					Write(")");
					return m;
				case "Round":
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
					Write(m.Method.Name.ToUpper());
					Write("(");
					Visit(m.Arguments[0]);
					Write(")");
					return m;
				case "Atan2":
					Write("ATN2(");
					Visit(m.Arguments[0]);
					Write(", ");
					Visit(m.Arguments[1]);
					Write(")");
					return m;
				case "Log":
					if (m.Arguments.Count == 1)
						goto case "Log10";

					break;
				case "Pow":
					Write("POWER(");
					Visit(m.Arguments[0]);
					Write(", ");
					Visit(m.Arguments[1]);
					Write(")");
					return m;
				case "Round":
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
					Write("ROUND(");
					Visit(m.Arguments[0]);
					Write(", 0, 1)");
					return m;
			}
		}
		if (m.Method.Name == "ToString")
		{
			if (m.Object.Type != typeof(string))
			{
				Write("CONVERT(NVARCHAR, ");
				Visit(m.Object);
				Write(")");
			}
			else
				Visit(m.Object);

			return m;
		}
		else if (!m.Method.IsStatic && string.Equals(m.Method.Name, "CompareTo", StringComparison.Ordinal) && m.Method.ReturnType == typeof(int) && m.Arguments.Count == 1)
		{
			Write("(CASE WHEN ");
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
			if (string.Equals(m.Method.Name, "Contains", StringComparison.Ordinal))
			{
				Visit(m.Arguments[0]);
				Write(" IN (");
				Visit(m.Object);
				Write(")");

				return m;
			}
		}
		else if (m.Method.DeclaringType == typeof(TypeComparer) && m.Method.IsStatic && string.Equals(m.Method.Name, nameof(TypeComparer.Compare), StringComparison.Ordinal) && m.Method.ReturnType == typeof(bool) && m.Arguments.Count == 2)
		{
			Visit(m.Arguments[0]);
			Write(" = ");
			Visit(m.Arguments[1]);
			return m;
		}
		return base.VisitMethodCall(m);
	}

	protected override NewExpression VisitNew(NewExpression nex)
	{
		if (nex.Constructor.DeclaringType == typeof(DateTime))
		{
			if (nex.Arguments.Count == 3)
			{
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

	protected override Expression VisitBinary(BinaryExpression b)
	{
		if (b.NodeType == ExpressionType.Power)
		{
			Write("POWER(");
			VisitValue(b.Left);
			Write(", ");
			VisitValue(b.Right);
			Write(")");
			return b;
		}
		else if (b.NodeType == ExpressionType.Coalesce)
		{
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
			Write("(");
			VisitValue(b.Left);
			Write(" * POWER(2, ");
			VisitValue(b.Right);
			Write("))");
			return b;
		}
		else if (b.NodeType == ExpressionType.RightShift)
		{
			Write("(");
			VisitValue(b.Left);
			Write(" / POWER(2, ");
			VisitValue(b.Right);
			Write("))");
			return b;
		}

		return base.VisitBinary(b);
	}

	protected override Expression VisitConstant(ConstantExpression c)
	{
		if (c.Value is not null && c.Value.GetType().IsEnumerable())
		{
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
			var parameter = Context.Parameters.FirstOrDefault(f => f.Value == c);

			if (parameter.Value is not null)
			{
				Write($"@{parameter.Key}");

				return c;
			}
		}
		return base.VisitConstant(c);
	}

	protected override Expression VisitValue(Expression expr)
	{
		if (IsPredicate(expr))
		{
			Write("CASE WHEN (");
			Visit(expr);
			Write(") THEN 1 ELSE 0 END");

			return expr;
		}

		return base.VisitValue(expr);
	}

	protected override Expression VisitConditional(ConditionalExpression c)
	{
		if (IsPredicate(c.Test))
		{
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

	protected override Expression VisitRowNumber(RowNumberExpression rowNumber)
	{
		Write("ROW_NUMBER() OVER(");

		if (rowNumber.OrderBy is not null && rowNumber.OrderBy.Any())
		{
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

	protected override Expression VisitIf(IfCommandExpression ifx)
	{
		if (!Language.AllowsMultipleCommands)
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

	protected override Expression VisitBlock(Data.Expressions.Expressions.BlockExpression block)
	{
		if (!Language.AllowsMultipleCommands)
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

	protected override Expression VisitDeclaration(DeclarationExpression decl)
	{
		if (!Language.AllowsMultipleCommands)
			return base.VisitDeclaration(decl);

		for (var i = 0; i < decl.Variables.Count; i++)
		{
			var v = decl.Variables[i];

			if (i > 0)
				WriteLine(Indentation.Same);

			Write("DECLARE @");
			Write(v.Name);
			Write(" ");
			Write(Language.TypeSystem.Format(v.DataType, false));
		}

		if (decl.Source is not null)
		{
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
}
