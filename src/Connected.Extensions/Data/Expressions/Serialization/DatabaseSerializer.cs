using Connected.Data.Expressions.Evaluation;
using Connected.Data.Expressions.Expressions;
using Connected.Data.Expressions.Formatters;
using Connected.Data.Expressions.Languages;
using Connected.Data.Expressions.Translation;
using System.Linq.Expressions;

namespace Connected.Data.Expressions.Serialization;

internal sealed class DatabaseSerializer : ExpressionSerializer
{
	public DatabaseSerializer(TextWriter writer, QueryLanguage? language) : base(writer)
	{
		Aliases = new();
		Language = language;
	}

	private Dictionary<Alias, int> Aliases { get; }
	private QueryLanguage? Language { get; }

	public static new void Serialize(TextWriter writer, Expression expression)
	{
		Serialize(writer, expression, null);
	}

	public static void Serialize(TextWriter writer, Expression expression, QueryLanguage? language)
	{
		new DatabaseSerializer(writer, language).Visit(expression);
	}

	public new static string Serialize(Expression expression)
	{
		return Serialize((QueryLanguage?)null, expression);
	}

	public static string Serialize(QueryLanguage? language, Expression expression)
	{
		var writer = new StringWriter();

		Serialize(writer, expression, language);

		return writer.ToString();
	}

	protected override Expression Visit(Expression expression)
	{
		switch ((DatabaseExpressionType)expression.NodeType)
		{
			case DatabaseExpressionType.Projection:
				return VisitProjection((ProjectionExpression)expression);
			case DatabaseExpressionType.ClientJoin:
				return VisitClientJoin((ClientJoinExpression)expression);
			case DatabaseExpressionType.Select:
				return VisitSelect((SelectExpression)expression);
			case DatabaseExpressionType.OuterJoined:
				return VisitOuterJoined((OuterJoinedExpression)expression);
			case DatabaseExpressionType.Column:
				return VisitColumn((ColumnExpression)expression);
			case DatabaseExpressionType.If:
			case DatabaseExpressionType.Block:
			case DatabaseExpressionType.Declaration:
				return VisitCommand((CommandExpression)expression);
			case DatabaseExpressionType.Batch:
				return VisitBatch((BatchExpression)expression);
			case DatabaseExpressionType.Function:
				return VisitFunction((FunctionExpression)expression);
			case DatabaseExpressionType.Entity:
				return VisitEntity((EntityExpression)expression);
			default:
				if (expression is DatabaseExpression)
				{
					Write(FormatQuery(expression));

					return expression;
				}
				else
					return base.Visit(expression);
		}
	}

	private void AddAlias(Alias alias)
	{
		if (!Aliases.ContainsKey(alias))
			Aliases.Add(alias, Aliases.Count);
	}

	private ProjectionExpression VisitProjection(ProjectionExpression projection)
	{
		AddAlias(projection.Select.Alias);
		Write("Project(");
		WriteLine(Indentation.Inner);
		Write("@\"");
		Visit(projection.Select);
		Write("\",");
		WriteLine(Indentation.Same);
		Visit(projection.Projector);
		Write(',');
		WriteLine(Indentation.Same);
		Visit(projection.Aggregator);
		WriteLine(Indentation.Outer);
		Write(')');

		return projection;
	}

	private Expression VisitClientJoin(ClientJoinExpression join)
	{
		AddAlias(join.Projection.Select.Alias);
		Write("ClientJoin(");
		WriteLine(Indentation.Inner);
		Write("OuterKey(");
		VisitExpressionList(join.OuterKey);
		Write("),");
		WriteLine(Indentation.Same);
		Write("InnerKey(");
		VisitExpressionList(join.InnerKey);
		Write("),");
		WriteLine(Indentation.Same);
		Visit(join.Projection);
		WriteLine(Indentation.Outer);
		Write(')');

		return join;
	}

	private Expression VisitOuterJoined(OuterJoinedExpression outer)
	{
		Write("Outer(");
		WriteLine(Indentation.Inner);
		Visit(outer.Test);
		Write(", ");
		WriteLine(Indentation.Same);
		Visit(outer.Expression);
		WriteLine(Indentation.Outer);
		Write(')');

		return outer;
	}

	private Expression VisitSelect(SelectExpression select)
	{
		Write(select.QueryText);

		return select;
	}

	private Expression VisitCommand(CommandExpression command)
	{
		Write(FormatQuery(command));

		return command;
	}

	private string FormatQuery(Expression query)
	{
		return SqlFormatter.Format(query);
	}

	private Expression VisitBatch(BatchExpression batch)
	{
		Write("Batch(");
		WriteLine(Indentation.Inner);
		Visit(batch.Input);
		Write(",");
		WriteLine(Indentation.Same);
		Visit(batch.Operation);
		Write(",");
		WriteLine(Indentation.Same);
		Visit(batch.BatchSize);
		Write(", ");
		Visit(batch.Stream);
		WriteLine(Indentation.Outer);
		Write(")");

		return batch;
	}

	private Expression VisitVariable(VariableExpression vex)
	{
		Write(FormatQuery(vex));

		return vex;
	}

	private Expression VisitFunction(FunctionExpression function)
	{
		Write("FUNCTION ");
		Write(function.Name);

		if (function.Arguments?.Count > 0)
		{
			Write("(");
			VisitExpressionList(function.Arguments);
			Write(")");
		}

		return function;
	}

	private Expression VisitEntity(EntityExpression expression)
	{
		Visit(expression.Expression);

		return expression;
	}

	protected override Expression VisitConstant(ConstantExpression c)
	{
		if (c.Type == typeof(Command))
		{
			var qc = (Command)c.Value;

			Write("new Connected.Expressions.Evaluation.QueryCommand {");
			WriteLine(Indentation.Inner);
			Write("\"" + qc.CommandText + "\"");
			Write(",");
			WriteLine(Indentation.Same);
			Visit(Expression.Constant(qc.Parameters));
			Write(")");
			WriteLine(Indentation.Outer);

			return c;
		}

		return base.VisitConstant(c);
	}

	private ColumnExpression VisitColumn(ColumnExpression column)
	{
		var aliasName = Aliases.TryGetValue(column.Alias, out int iAlias) ? "A" + iAlias : "A" + (column.Alias is not null ? column.Alias.GetHashCode().ToString() : "") + "?";

		Write(aliasName);
		Write(".");
		Write("Column(\"");
		Write(column.Name);
		Write("\")");

		return column;
	}
}
