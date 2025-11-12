using Connected.Data.Expressions.Evaluation;
using Connected.Data.Expressions.Expressions;
using Connected.Data.Expressions.Formatters;
using Connected.Data.Expressions.Translation;
using System.Linq.Expressions;

namespace Connected.Data.Expressions.Serialization;

internal sealed class DatabaseSerializer(TextWriter writer)
		: ExpressionSerializer(writer)
{
	private Dictionary<Alias, int> Aliases { get; } = [];

	public static new void Serialize(TextWriter writer, Expression expression)
	{
		new DatabaseSerializer(writer).Visit(expression);
	}

	public static new string Serialize(Expression expression)
	{
		var writer = new StringWriter();

		Serialize(writer, expression);

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

		if (projection.Aggregator is not null)
		{
			Write(',');
			WriteLine(Indentation.Same);
			Visit(projection.Aggregator);
		}

		WriteLine(Indentation.Outer);
		Write(')');

		return projection;
	}

	private ClientJoinExpression VisitClientJoin(ClientJoinExpression join)
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

	private OuterJoinedExpression VisitOuterJoined(OuterJoinedExpression outer)
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

	private SelectExpression VisitSelect(SelectExpression select)
	{
		Write(select.QueryText);

		return select;
	}

	private CommandExpression VisitCommand(CommandExpression command)
	{
		Write(FormatQuery(command));

		return command;
	}

	private static string FormatQuery(Expression query)
	{
		return SqlFormatter.Format(query);
	}

	private BatchExpression VisitBatch(BatchExpression batch)
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

	private FunctionExpression VisitFunction(FunctionExpression function)
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

	private EntityExpression VisitEntity(EntityExpression expression)
	{
		Visit(expression.Expression);

		return expression;
	}

	protected override Expression VisitConstant(ConstantExpression c)
	{
		if (c.Type == typeof(Command))
		{
			if (c.Value is not Command qc)
				return c;

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
