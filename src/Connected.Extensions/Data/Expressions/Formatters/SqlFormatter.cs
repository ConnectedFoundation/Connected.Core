using Connected.Data.Expressions.Expressions;
using Connected.Data.Expressions.Languages;
using Connected.Data.Expressions.Translation;
using Connected.Data.Expressions.Visitors;
using Connected.Reflection;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using BlockExpression = Connected.Data.Expressions.Expressions.BlockExpression;

namespace Connected.Data.Expressions.Formatters;

public class SqlFormatter
	: DatabaseVisitor
{
	protected const char Space = ' ';
	protected const char Period = '.';
	protected const char OpenBracket = '(';
	protected const char CloseBracket = ')';
	protected const char SingleQuote = '\'';
	protected enum Indentation
	{
		Same,
		Inner,
		Outer
	}

	protected SqlFormatter(QueryLanguage? language)
	{
		Language = language;
		Text = new StringBuilder();
		Aliases = [];
	}

	private int Depth { get; set; }
	protected virtual QueryLanguage? Language { get; }
	protected bool HideColumnAliases { get; set; } = true;
	protected bool HideTableAliases { get; set; } = true;
	protected bool UseBracketsInWhere { get; set; } = true;
	protected bool IsNested { get; set; }
	public int IndentationWidth { get; set; } = 2;
	private StringBuilder Text { get; }
	private Dictionary<Alias, string> Aliases { get; }

	public static string Format(Expression expression)
	{
		var formatter = new SqlFormatter(null);

		formatter.Visit(expression);

		return formatter.ToString();
	}

	public override string ToString()
	{
		return Text.ToString();
	}

	protected void Write(object value)
	{
		Text.Append(value);
	}

	protected virtual void WriteParameterName(string name)
	{
		Write($"@{name}");
	}

	protected virtual void WriteVariableName(string name)
	{
		WriteParameterName(name);
	}

	protected virtual void WriteAsAliasName(string aliasName)
	{
		Write("AS ");
		WriteAliasName(aliasName);
	}

	protected virtual void WriteAliasName(string aliasName)
	{
		Write(aliasName);
	}

	protected virtual void WriteAsColumnName(string columnName)
	{
		Write("AS ");
		WriteColumnName(columnName);
	}

	protected virtual void WriteColumnName(string columnName)
	{
		var name = Language is not null ? Language.Quote(columnName) : columnName;

		Write(name);
	}

	protected virtual void WriteTableName(string tableSchema, string tableName)
	{
		var name = Language is not null ? Language.Quote(tableName) : tableName;
		var schema = Language is not null ? Language.Quote(tableSchema) : tableName;

		Write($"{schema}.{name}");
	}

	protected void WriteLine(Indentation style)
	{
		Text.AppendLine();
		Indent(style);

		for (var i = 0; i < Depth * IndentationWidth; i++)
			Write(Space);
	}

	protected void Indent(Indentation style)
	{
		if (style == Indentation.Inner)
			Depth++;
		else if (style == Indentation.Outer)
			Depth--;
	}

	protected virtual string GetAliasName(Alias alias)
	{
		if (!Aliases.TryGetValue(alias, out string? name))
		{
			name = $"A{alias.GetHashCode()}?";

			Aliases.Add(alias, name);
		}

		return name;
	}

	protected void AddAlias(Alias alias)
	{
		if (!Aliases.TryGetValue(alias, out _))
		{
			var name = $"t{Aliases.Count}";

			Aliases.Add(alias, name);
		}
	}

	protected virtual void AddAliases(Expression expr)
	{
		if (expr as AliasedExpression is AliasedExpression ax)
			AddAlias(ax.Alias);
		else
		{
			if (expr as JoinExpression is JoinExpression jx)
			{
				AddAliases(jx.Left);
				AddAliases(jx.Right);
			}
		}
	}

	protected override Expression Visit(Expression exp)
	{
		if (exp is null)
			throw new NullReferenceException("Expected expression.");

		return exp.NodeType switch
		{
			ExpressionType.Negate or ExpressionType.NegateChecked or ExpressionType.Not or ExpressionType.Convert or ExpressionType.ConvertChecked
	  or ExpressionType.UnaryPlus or ExpressionType.Add or ExpressionType.AddChecked or ExpressionType.Subtract or ExpressionType.SubtractChecked
	  or ExpressionType.Multiply or ExpressionType.MultiplyChecked or ExpressionType.Divide or ExpressionType.Modulo or ExpressionType.And
	  or ExpressionType.AndAlso or ExpressionType.Or or ExpressionType.OrElse or ExpressionType.LessThan or ExpressionType.LessThanOrEqual
	  or ExpressionType.GreaterThan or ExpressionType.GreaterThanOrEqual or ExpressionType.Equal or ExpressionType.NotEqual
	  or ExpressionType.Coalesce or ExpressionType.RightShift or ExpressionType.LeftShift or ExpressionType.ExclusiveOr
	  or ExpressionType.Power or ExpressionType.Conditional or ExpressionType.Constant or ExpressionType.MemberAccess or ExpressionType.Call
	  or ExpressionType.New or (ExpressionType)DatabaseExpressionType.Table or (ExpressionType)DatabaseExpressionType.Column
	  or (ExpressionType)DatabaseExpressionType.Select or (ExpressionType)DatabaseExpressionType.Join or (ExpressionType)DatabaseExpressionType.Aggregate
	  or (ExpressionType)DatabaseExpressionType.Scalar or (ExpressionType)DatabaseExpressionType.Exists or (ExpressionType)DatabaseExpressionType.In
	  or (ExpressionType)DatabaseExpressionType.AggregateSubquery or (ExpressionType)DatabaseExpressionType.IsNull
	  or (ExpressionType)DatabaseExpressionType.Between or (ExpressionType)DatabaseExpressionType.RowCount
	  or (ExpressionType)DatabaseExpressionType.Projection or (ExpressionType)DatabaseExpressionType.NamedValue
	  or (ExpressionType)DatabaseExpressionType.Block or (ExpressionType)DatabaseExpressionType.If or (ExpressionType)DatabaseExpressionType.Declaration
	  or (ExpressionType)DatabaseExpressionType.Variable or (ExpressionType)DatabaseExpressionType.Function => base.Visit(exp),
			_ => throw new NotSupportedException($"The expression node of type '{exp.ResolveNodeTypeName()}' is not supported."),
		};
	}

	protected override Expression VisitMemberAccess(MemberExpression m)
	{
		throw new NotSupportedException($"The member access '{m.Member}' is not supported.");
	}

	protected override Expression VisitMethodCall(MethodCallExpression m)
	{
		if (m.Method.DeclaringType == typeof(decimal))
		{
			switch (m.Method.Name)
			{
				case "Add":
				case "Subtract":
				case "Multiply":
				case "Divide":
				case "Remainder":
					Write(OpenBracket);
					VisitValue(m.Arguments[0]);
					Write(Space);
					Write(GetOperator(m.Method.Name));
					Write(Space);
					VisitValue(m.Arguments[1]);
					Write(CloseBracket);
					return m;
				case "Negate":
					Write('-');
					Visit(m.Arguments[0]);
					Write(string.Empty);
					return m;
				case "Compare":
					Visit(Expression.Condition(
						  Expression.Equal(m.Arguments[0], m.Arguments[1]),
						  Expression.Constant(0),
						  Expression.Condition(
								 Expression.LessThan(m.Arguments[0], m.Arguments[1]),
								 Expression.Constant(-1),
								 Expression.Constant(1)
								 )));

					return m;
			}
		}
		else if (string.Equals(m.Method.Name, "ToString", StringComparison.Ordinal) && m.Object?.Type == typeof(string))
		{
			/*
		 * no op.
		 */
			return Visit(m.Object);
		}
		else if (string.Equals(m.Method.Name, "Equals", StringComparison.Ordinal))
		{
			if (m.Method.IsStatic && m.Method.DeclaringType == typeof(object) || m.Method.DeclaringType == typeof(string))
			{
				Write(OpenBracket);
				Visit(m.Arguments[0]);
				Write(" = ");
				Visit(m.Arguments[1]);
				Write(CloseBracket);
				return m;
			}
			else if (!m.Method.IsStatic && m.Arguments.Count == 1 && m.Arguments[0].Type == m.Object?.Type)
			{
				Write(OpenBracket);
				Visit(m.Object);
				Write(" = ");
				Visit(m.Arguments[0]);
				Write(CloseBracket);
				return m;
			}
		}

		throw new NotSupportedException($"The method '{m.Method.Name}' is not supported");
	}

	protected virtual bool IsInteger(Type type)
	{
		return type.IsInteger();
	}

	protected override NewExpression VisitNew(NewExpression nex)
	{
		throw new NotSupportedException($"The constructor for '{nex.Constructor?.DeclaringType}' is not supported");
	}

	protected override Expression VisitUnary(UnaryExpression u)
	{
		var op = GetOperator(u);

		switch (u.NodeType)
		{
			case ExpressionType.Not:

				if (u.Operand is IsNullExpression nullExpression)
				{
					Visit(nullExpression.Expression);
					Write(" IS NOT NULL");
				}
				else if (IsBoolean(u.Operand.Type) || op.Length > 1)
				{
					Write(op);
					Write(Space);
					VisitPredicate(u.Operand);
				}
				else
				{
					Write(op);
					VisitValue(u.Operand);
				}

				break;
			case ExpressionType.Negate:
			case ExpressionType.NegateChecked:
				Write(op);
				VisitValue(u.Operand);
				break;
			case ExpressionType.UnaryPlus:
				VisitValue(u.Operand);
				break;
			case ExpressionType.Convert:
				/*
			* ignore conversions for now
			*/
				Visit(u.Operand);
				break;
			default:
				throw new NotSupportedException($"The unary operator '{u.NodeType}' is not supported");
		}

		return u;
	}

	protected override Expression VisitBinary(BinaryExpression b)
	{
		var op = GetOperator(b);
		var left = b.Left;
		var right = b.Right;

		if (UseBracketsInWhere)
			Write(OpenBracket);

		switch (b.NodeType)
		{
			case ExpressionType.And:
			case ExpressionType.AndAlso:
			case ExpressionType.Or:
			case ExpressionType.OrElse:
				if (IsBoolean(left.Type))
				{
					VisitPredicate(left);
					Write(Space);
					Write(op);
					Write(Space);
					VisitPredicate(right);
				}
				else
				{
					VisitValue(left);
					Write(Space);
					Write(op);
					Write(Space);
					VisitValue(right);
				}
				break;
			case ExpressionType.Equal:
				if (right.NodeType == ExpressionType.Constant)
				{
					var ce = (ConstantExpression)right;

					if (ce.Value is null)
					{
						Visit(left);
						Write(" IS NULL");

						break;
					}
				}
				else if (left.NodeType == ExpressionType.Constant)
				{
					var ce = (ConstantExpression)left;

					if (ce.Value is null)
					{
						Visit(right);
						Write(" IS NULL");

						break;
					}
				}
				goto case ExpressionType.LessThan;
			case ExpressionType.NotEqual:
				if (right.NodeType == ExpressionType.Constant)
				{
					var ce = (ConstantExpression)right;

					if (ce.Value is null)
					{
						Visit(left);
						Write(" IS NOT NULL");

						break;
					}
				}
				else if (left.NodeType == ExpressionType.Constant)
				{
					var ce = (ConstantExpression)left;

					if (ce.Value is null)
					{
						Visit(right);
						Write(" IS NOT NULL");

						break;
					}
				}
				goto case ExpressionType.LessThan;
			case ExpressionType.LessThan:
			case ExpressionType.LessThanOrEqual:
			case ExpressionType.GreaterThan:
			case ExpressionType.GreaterThanOrEqual:
				/*
			* check for special x.CompareTo(y) && type.Compare(x,y)
			*/
				if (left.NodeType == ExpressionType.Call && right.NodeType == ExpressionType.Constant)
				{
					var mc = (MethodCallExpression)left;
					var ce = (ConstantExpression)right;

					if (ce.Value is not null && ce.Value.GetType() == typeof(int) && (int)ce.Value == 0)
					{
						if (string.Equals(mc.Method.Name, "CompareTo", StringComparison.Ordinal) && !mc.Method.IsStatic && mc.Arguments.Count == 1)
						{
							left = mc.Object;
							right = mc.Arguments[0];
						}
						else if ((mc.Method.DeclaringType == typeof(string) || mc.Method.DeclaringType == typeof(decimal))
								  && string.Equals(mc.Method.Name, "Compare", StringComparison.Ordinal) && mc.Method.IsStatic && mc.Arguments.Count == 2)
						{
							left = mc.Arguments[0];
							right = mc.Arguments[1];
						}
					}
				}
				goto case ExpressionType.Add;
			case ExpressionType.Add:
			case ExpressionType.AddChecked:
			case ExpressionType.Subtract:
			case ExpressionType.SubtractChecked:
			case ExpressionType.Multiply:
			case ExpressionType.MultiplyChecked:
			case ExpressionType.Divide:
			case ExpressionType.Modulo:
			case ExpressionType.ExclusiveOr:
			case ExpressionType.LeftShift:
			case ExpressionType.RightShift:

				if (left is not null)
					VisitValue(left);

				Write(Space);
				Write(op);
				Write(Space);
				VisitValue(right);
				break;
			default:
				throw new NotSupportedException($"The binary operator '{b.NodeType}' is not supported");
		}

		if (UseBracketsInWhere)
			Write(CloseBracket);

		return b;
	}

	protected virtual string GetOperator(string methodName)
	{
		return methodName switch
		{
			"Add" => "+",
			"Subtract" => "-",
			"Multiply" => "*",
			"Divide" => "/",
			"Negate" => "-",
			"Remainder" => "%",
			_ => string.Empty,
		};
	}

	protected virtual string GetOperator(UnaryExpression u)
	{
		return u.NodeType switch
		{
			ExpressionType.Negate or ExpressionType.NegateChecked => "-",
			ExpressionType.UnaryPlus => "+",
			ExpressionType.Not => IsBoolean(u.Operand.Type) ? "NOT" : "~",
			_ => string.Empty,
		};
	}

	protected virtual string GetOperator(BinaryExpression b)
	{
		return b.NodeType switch
		{
			ExpressionType.And or ExpressionType.AndAlso => IsBoolean(b.Left.Type) ? "AND" : "&",
			ExpressionType.Or or ExpressionType.OrElse => IsBoolean(b.Left.Type) ? "OR" : "|",
			ExpressionType.Equal => "=",
			ExpressionType.NotEqual => "<>",
			ExpressionType.LessThan => "<",
			ExpressionType.LessThanOrEqual => "<=",
			ExpressionType.GreaterThan => ">",
			ExpressionType.GreaterThanOrEqual => ">=",
			ExpressionType.Add or ExpressionType.AddChecked => "+",
			ExpressionType.Subtract or ExpressionType.SubtractChecked => "-",
			ExpressionType.Multiply or ExpressionType.MultiplyChecked => "*",
			ExpressionType.Divide => "/",
			ExpressionType.Modulo => "%",
			ExpressionType.ExclusiveOr => "^",
			ExpressionType.LeftShift => "<<",
			ExpressionType.RightShift => ">>",
			_ => string.Empty,
		};
	}

	protected virtual bool IsBoolean(Type type)
	{
		return type == typeof(bool) || type == typeof(bool?);
	}

	protected virtual bool IsPredicate(Expression expr)
	{
		return expr.NodeType switch
		{
			ExpressionType.And or ExpressionType.AndAlso or ExpressionType.Or or ExpressionType.OrElse => IsBoolean(((BinaryExpression)expr).Type),
			ExpressionType.Not => IsBoolean(((UnaryExpression)expr).Type),
			ExpressionType.Equal or ExpressionType.NotEqual or ExpressionType.LessThan or ExpressionType.LessThanOrEqual or
			ExpressionType.GreaterThan or ExpressionType.GreaterThanOrEqual or (ExpressionType)DatabaseExpressionType.IsNull or
			(ExpressionType)DatabaseExpressionType.Between or (ExpressionType)DatabaseExpressionType.Exists or (ExpressionType)DatabaseExpressionType.In => true,
			ExpressionType.Call => IsBoolean(((MethodCallExpression)expr).Type),
			_ => false,
		};
	}

	protected virtual Expression VisitPredicate(Expression expr)
	{
		Visit(expr);

		if (!IsPredicate(expr))
			Write(" <> 0");

		return expr;
	}

	protected virtual Expression VisitValue(Expression expr)
	{
		return Visit(expr);
	}

	protected override Expression VisitConditional(ConditionalExpression c)
	{
		throw new NotSupportedException("Conditional expressions not supported.");
	}

	protected override Expression VisitConstant(ConstantExpression c)
	{
		WriteValue(c.Value);

		return c;
	}

	protected virtual void WriteValue(object? value)
	{
		if (value is null)
			Write("NULL");
		else if (value.GetType().GetTypeInfo().IsEnum)
			Write(Types.Convert(value, Enum.GetUnderlyingType(value.GetType())) ?? "NULL");
		else
		{
			switch (value.GetType().GetTypeCode())
			{
				case TypeCode.Boolean:
					Write((bool)value ? 1 : 0);
					break;
				case TypeCode.String:
					Write(SingleQuote);
					Write(value);
					Write(SingleQuote);
					break;
				case TypeCode.Object:
					throw new NotSupportedException($"The constant for '{value}' is not supported");
				case TypeCode.Single:
				case TypeCode.Double:
					var str = ((IConvertible)value).ToString(NumberFormatInfo.InvariantInfo);

					if (!str.Contains(Period))
						str = string.Concat(str, $"{Period}0");

					Write(str);
					break;
				case TypeCode.DateTime:
					Write("CONVERT (datetime2(7),");
					Write(SingleQuote);

					if (value is DateTimeOffset offset)
					{
						var dt = new DateTime(offset.Year, offset.Month, offset.Day, offset.Hour, offset.Minute, offset.Second, offset.Millisecond);

						Write(dt.ToString("o", CultureInfo.InvariantCulture) ?? value);
					}
					else
						Write((value as DateTime?)?.ToString("o", CultureInfo.InvariantCulture) ?? value);

					Write(SingleQuote);
					Write(')');
					break;
				default:
					Write((value as IConvertible)?.ToString(CultureInfo.InvariantCulture) ?? value);
					break;
			}
		}
	}
	protected override Expression VisitColumn(ColumnExpression column)
	{
		if (column.Alias is not null && !HideColumnAliases)
		{
			WriteAliasName(GetAliasName(column.Alias));
			Write(Period);
		}

		WriteColumnName(column.Name);

		return column;
	}
	protected override Expression VisitProjection(ProjectionExpression proj)
	{
		// treat these like scalar subqueries
		if (proj.Projector is ColumnExpression)
		{
			Write(OpenBracket);
			WriteLine(Indentation.Inner);
			Visit(proj.Select);
			Write(CloseBracket);
			Indent(Indentation.Outer);
		}
		else
			throw new NotSupportedException("Non-scalar projections cannot be translated to SQL.");

		return proj;
	}

	protected override Expression VisitSelect(SelectExpression select)
	{
		AddAliases(select.From);
		Write("SELECT ");

		if (select.IsDistinct)
			Write("DISTINCT ");

		if (select.Take is not null)
			WriteTopClause(select.Take);

		WriteColumns(select.Columns);

		if (select.From is not null)
		{
			WriteLine(Indentation.Same);
			Write("FROM ");
			VisitSource(select.From);
		}

		if (select.Where is not null)
			WriteWhere(select.Where);

		if (select.GroupBy is not null && select.GroupBy.Count != 0)
		{
			WriteLine(Indentation.Same);
			Write("GROUP BY ");

			for (var i = 0; i < select.GroupBy.Count; i++)
			{
				if (i > 0)
					Write(", ");

				VisitValue(select.GroupBy[i]);
			}
		}

		if (select.OrderBy is not null && select.OrderBy.Count != 0)
		{
			WriteLine(Indentation.Same);
			Write("ORDER BY ");

			for (var i = 0; i < select.OrderBy.Count; i++)
			{
				var exp = select.OrderBy[i];

				if (i > 0)
					Write(", ");

				VisitValue(exp.Expression);

				if (exp.OrderType != OrderType.Ascending)
					Write(" DESC");
			}
		}

		return select;
	}

	protected virtual void WriteWhere(Expression expression)
	{
		WriteLine(Indentation.Same);
		Write("WHERE ");
		VisitPredicate(expression);
	}

	protected virtual void WriteTopClause(Expression expression)
	{
		Write("TOP (");
		Visit(expression);
		Write(") ");
	}

	protected virtual void WriteColumns(ReadOnlyCollection<ColumnDeclaration> columns)
	{
		if (columns.Count != 0)
		{
			for (var i = 0; i < columns.Count; i++)
			{
				var column = columns[i];

				if (i > 0)
					Write(", ");

				var c = VisitValue(column.Expression) as ColumnExpression;

				if (!string.IsNullOrEmpty(column.Name) && (c is null || !string.Equals(c.Name, column.Name, StringComparison.OrdinalIgnoreCase)))
				{
					Write(Space);
					WriteAsColumnName(column.Name);
				}
			}
		}
		else
		{
			Write("NULL ");

			if (IsNested)
			{
				WriteAsColumnName("tmp");
				Write(Space);
			}
		}
	}

	protected override Expression VisitSource(Expression source)
	{
		var saveIsNested = IsNested;

		IsNested = true;

		switch ((DatabaseExpressionType)source.NodeType)
		{
			case DatabaseExpressionType.Table:
				var table = (TableExpression)source;

				WriteTableName(table.Schema, table.Name);

				if (!HideTableAliases)
				{
					Write(Space);
					WriteAsAliasName(GetAliasName(table.Alias));
				}
				break;
			case DatabaseExpressionType.Select:
				var select = (SelectExpression)source;

				Write(OpenBracket);
				WriteLine(Indentation.Inner);
				Visit(select);
				WriteLine(Indentation.Same);
				Write($"{CloseBracket} ");
				WriteAsAliasName(GetAliasName(select.Alias));
				Indent(Indentation.Outer);
				break;
			case DatabaseExpressionType.Join:
				VisitJoin((JoinExpression)source);
				break;
			default:
				throw new InvalidOperationException("Select source is not valid type");
		}

		IsNested = saveIsNested;

		return source;
	}

	protected override Expression VisitJoin(JoinExpression join)
	{
		VisitJoinLeft(join.Left);
		WriteLine(Indentation.Same);

		switch (join.Join)
		{
			case JoinType.CrossJoin:
				Write("CROSS JOIN ");
				break;
			case JoinType.InnerJoin:
				Write("INNER JOIN ");
				break;
			case JoinType.CrossApply:
				Write("CROSS APPLY ");
				break;
			case JoinType.OuterApply:
				Write("OUTER APPLY ");
				break;
			case JoinType.LeftOuter:
			case JoinType.SingletonLeftOuter:
				Write("LEFT OUTER JOIN ");
				break;
		}

		VisitJoinRight(join.Right);

		if (join.Condition is not null)
		{
			WriteLine(Indentation.Inner);
			Write("ON ");
			VisitPredicate(join.Condition);
			Indent(Indentation.Outer);
		}

		return join;
	}

	protected virtual Expression VisitJoinLeft(Expression source)
	{
		return VisitSource(source);
	}

	protected virtual Expression VisitJoinRight(Expression source)
	{
		return VisitSource(source);
	}

	protected virtual void WriteAggregateName(string aggregateName)
	{
		switch (aggregateName)
		{
			case "Average":
				Write("AVG");
				break;
			case "LongCount":
				Write("COUNT");
				break;
			default:
				Write(aggregateName.ToUpper());
				break;
		}
	}

	protected virtual bool RequiresAsteriskWhenNoArgument(string aggregateName)
	{
		return string.Equals(aggregateName, "Count", StringComparison.Ordinal) || string.Equals(aggregateName, "LongCount", StringComparison.Ordinal);
	}

	protected override Expression VisitAggregate(AggregateExpression aggregate)
	{
		WriteAggregateName(aggregate.AggregateName);
		Write(OpenBracket);

		if (aggregate.IsDistinct)
			Write("DISTINCT ");

		if (aggregate.Argument is not null)
			VisitValue(aggregate.Argument);
		else if (RequiresAsteriskWhenNoArgument(aggregate.AggregateName))
			Write("*");

		Write(CloseBracket);

		return aggregate;
	}

	protected override Expression VisitIsNull(IsNullExpression isnull)
	{
		VisitValue(isnull.Expression);
		Write(" IS NULL");

		return isnull;
	}

	protected override Expression VisitBetween(BetweenExpression between)
	{
		VisitValue(between.Expression);
		Write(" BETWEEN ");
		VisitValue(between.Lower);
		Write(" AND ");
		VisitValue(between.Upper);

		return between;
	}

	protected override Expression VisitRowNumber(RowNumberExpression rowNumber)
	{
		throw new NotSupportedException();
	}

	protected override Expression VisitScalar(ScalarExpression subquery)
	{
		Write(OpenBracket);
		WriteLine(Indentation.Inner);

		if (subquery.Select is not null)
			Visit(subquery.Select);

		WriteLine(Indentation.Same);
		Write(CloseBracket);
		Indent(Indentation.Outer);

		return subquery;
	}

	protected override Expression VisitExists(ExistsExpression exists)
	{
		Write($"EXISTS{OpenBracket}");
		WriteLine(Indentation.Inner);

		if (exists.Select is not null)
			Visit(exists.Select);

		WriteLine(Indentation.Same);
		Write(CloseBracket);
		Indent(Indentation.Outer);

		return exists;
	}
	protected override Expression VisitIn(InExpression @in)
	{
		if (@in.Values is not null)
		{
			if (@in.Values.Count == 0)
				Write("0 <> 0");
			else
			{
				VisitValue(@in.Expression);
				Write($" IN {OpenBracket}");

				for (var i = 0; i < @in.Values.Count; i++)
				{
					if (i > 0)
						Write(", ");

					VisitValue(@in.Values[i]);
				}

				Write(CloseBracket);
			}
		}
		else
		{
			VisitValue(@in.Expression);
			Write($" IN {OpenBracket}");
			WriteLine(Indentation.Inner);

			if (@in.Select is not null)
				Visit(@in.Select);

			WriteLine(Indentation.Same);
			Write(CloseBracket);
			Indent(Indentation.Outer);
		}

		return @in;
	}

	protected override Expression VisitNamedValue(NamedValueExpression value)
	{
		WriteParameterName(value.Name);

		return value;
	}

	protected override Expression VisitIf(IfCommandExpression ifx)
	{
		throw new NotSupportedException();
	}

	protected override Expression VisitBlock(BlockExpression block)
	{
		throw new NotSupportedException();
	}

	protected override Expression VisitDeclaration(DeclarationExpression declaration)
	{
		throw new NotSupportedException(nameof(declaration));
	}

	protected override Expression VisitVariable(VariableExpression vex)
	{
		WriteVariableName(vex.Name);

		return vex;
	}

	protected virtual void VisitStatement(Expression expression)
	{
		if (expression is ProjectionExpression p)
			Visit(p.Select);
		else
			Visit(expression);
	}

	protected override Expression VisitFunction(FunctionExpression func)
	{
		Write(func.Name);

		if (func.Arguments is not null && func.Arguments.Count != 0)
		{
			Write(OpenBracket);

			for (var i = 0; i < func.Arguments.Count; i++)
			{
				if (i > 0)
					Write(", ");

				Visit(func.Arguments[i]);
			}

			Write(CloseBracket);
		}

		return func;
	}
}