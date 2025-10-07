using Connected.Data.Expressions.Expressions;
using Connected.Data.Expressions.Translation;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using BlockExpression = Connected.Data.Expressions.Expressions.BlockExpression;

namespace Connected.Data.Expressions.Visitors;

public abstract class DatabaseVisitor : ExpressionVisitor
{
	protected override Expression? Visit(Expression? expression)
	{
		if (expression is null)
			return default;

		return (DatabaseExpressionType)expression.NodeType switch
		{
			DatabaseExpressionType.Table => VisitTable((TableExpression)expression),
			DatabaseExpressionType.Column => VisitColumn((ColumnExpression)expression),
			DatabaseExpressionType.Select => VisitSelect((SelectExpression)expression),
			DatabaseExpressionType.Join => VisitJoin((JoinExpression)expression),
			DatabaseExpressionType.OuterJoined => VisitOuterJoined((OuterJoinedExpression)expression),
			DatabaseExpressionType.Aggregate => VisitAggregate((AggregateExpression)expression),
			DatabaseExpressionType.Scalar or DatabaseExpressionType.Exists or DatabaseExpressionType.In => VisitSubquery((SubqueryExpression)expression),
			DatabaseExpressionType.AggregateSubquery => VisitAggregateSubquery((AggregateSubqueryExpression)expression),
			DatabaseExpressionType.IsNull => VisitIsNull((IsNullExpression)expression),
			DatabaseExpressionType.Between => VisitBetween((BetweenExpression)expression),
			DatabaseExpressionType.RowCount => VisitRowNumber((RowNumberExpression)expression),
			DatabaseExpressionType.Projection => VisitProjection((ProjectionExpression)expression),
			DatabaseExpressionType.NamedValue => VisitNamedValue((NamedValueExpression)expression),
			DatabaseExpressionType.ClientJoin => VisitClientJoin((ClientJoinExpression)expression),
			DatabaseExpressionType.If or DatabaseExpressionType.Block or DatabaseExpressionType.Declaration => VisitCommand((CommandExpression)expression),
			DatabaseExpressionType.Batch => VisitBatch((BatchExpression)expression),
			DatabaseExpressionType.Variable => VisitVariable((VariableExpression)expression),
			DatabaseExpressionType.Function => VisitFunction((FunctionExpression)expression),
			DatabaseExpressionType.Entity => VisitEntity((EntityExpression)expression),
			_ => base.Visit(expression),
		};
	}

	protected virtual Expression VisitEntity(EntityExpression entity)
	{
		if (Visit(entity.Expression) is not Expression entityExpression)
			throw new NullReferenceException(nameof(entityExpression));

		return UpdateEntity(entity, entityExpression);
	}

	protected static EntityExpression UpdateEntity(EntityExpression entity, Expression expression)
	{
		if (expression != entity.Expression)
			return new EntityExpression(entity.EntityType, expression);

		return entity;
	}

	protected virtual Expression VisitTable(TableExpression expression)
	{
		return expression;
	}

	protected virtual Expression VisitColumn(ColumnExpression expression)
	{
		return expression;
	}

	protected virtual Expression VisitSelect(SelectExpression expression)
	{
		var from = VisitSource(expression.From);
		var where = VisitWhere(expression.Where);
		var groupBy = VisitExpressionList(expression.GroupBy);
		var skip = Visit(expression.Skip);
		var take = Visit(expression.Take);
		var columns = VisitColumnDeclarations(expression.Columns);
		var orderBy = VisitOrderBy(expression.OrderBy);

		return UpdateSelect(expression, from, where, orderBy, groupBy, skip, take, expression.IsDistinct, expression.IsReverse, columns);
	}

	protected virtual Expression VisitWhere(Expression whereExpression)
	{
		return whereExpression;
	}
	protected static SelectExpression UpdateSelect(SelectExpression expression, Expression from, Expression? where,
		 IEnumerable<OrderExpression>? orderBy, IEnumerable<Expression> groupBy, Expression? skip, Expression? take,
		 bool isDistinct, bool isReverse, IEnumerable<ColumnDeclaration> columns)
	{
		if (from != expression.From || where != expression.Where || orderBy != expression.OrderBy || groupBy != expression.GroupBy
			 || take != expression.Take || skip != expression.Skip || isDistinct != expression.IsDistinct
			 || columns != expression.Columns || isReverse != expression.IsReverse)
		{
			return new SelectExpression(expression.Alias, columns, from, where, orderBy, groupBy, isDistinct, skip, take, isReverse);
		}

		return expression;
	}

	protected virtual Expression VisitJoin(JoinExpression expression)
	{
		if (Visit(expression.Condition) is not Expression condition)
			throw new NullReferenceException(nameof(condition));

		return UpdateJoin(expression, expression.Join, VisitSource(expression.Left), VisitSource(expression.Right), condition);
	}

	protected static JoinExpression UpdateJoin(JoinExpression expression, JoinType joinType, Expression left, Expression right, Expression condition)
	{
		if (joinType != expression.Join || left != expression.Left || right != expression.Right || condition != expression.Condition)
			return new JoinExpression(joinType, left, right, condition);

		return expression;
	}

	protected virtual Expression VisitOuterJoined(OuterJoinedExpression expression)
	{
		if (Visit(expression.Test) is not Expression joinTest)
			throw new NullReferenceException(nameof(joinTest));

		if (Visit(expression.Expression) is not Expression joinExpression)
			throw new NullReferenceException(nameof(JoinExpression));

		return UpdateOuterJoined(expression, joinTest, joinExpression);
	}

	protected static OuterJoinedExpression UpdateOuterJoined(OuterJoinedExpression expression, Expression test, Expression e)
	{
		if (test != expression.Test || e != expression.Expression)
			return new OuterJoinedExpression(test, e);

		return expression;
	}

	protected virtual Expression VisitAggregate(AggregateExpression expression)
	{
		if (Visit(expression.Argument) is not Expression argumentExpression)
			throw new NullReferenceException(nameof(argumentExpression));

		return UpdateAggregate(expression, expression.Type, expression.AggregateName, argumentExpression, expression.IsDistinct);
	}

	protected static AggregateExpression UpdateAggregate(AggregateExpression expression, Type type, string aggType, Expression e, bool isDistinct)
	{
		if (type != expression.Type || aggType != expression.AggregateName || e != expression.Argument || isDistinct != expression.IsDistinct)
			return new AggregateExpression(type, aggType, e, isDistinct);

		return expression;
	}

	protected virtual Expression VisitIsNull(IsNullExpression expression)
	{
		if (Visit(expression.Expression) is not Expression nullExpression)
			throw new NullReferenceException(nameof(nullExpression));

		return UpdateIsNull(expression, nullExpression);
	}

	protected static IsNullExpression UpdateIsNull(IsNullExpression expression, Expression e)
	{
		if (e != expression.Expression)
			return new IsNullExpression(e);

		return expression;
	}

	protected virtual Expression VisitBetween(BetweenExpression expression)
	{
		if (Visit(expression.Expression) is not Expression betweenExpression)
			throw new NullReferenceException(nameof(betweenExpression));

		if (Visit(expression.Lower) is not Expression lowerExpression)
			throw new NullReferenceException(nameof(lowerExpression));

		if (Visit(expression.Upper) is not Expression upperExpression)
			throw new NullReferenceException(nameof(upperExpression));

		return UpdateBetween(expression, betweenExpression, lowerExpression, upperExpression);
	}

	protected static BetweenExpression UpdateBetween(BetweenExpression expression, Expression e, Expression lower, Expression upper)
	{
		if (e != expression.Expression || lower != expression.Lower || upper != expression.Upper)
			return new BetweenExpression(e, lower, upper);

		return expression;
	}

	protected virtual Expression VisitRowNumber(RowNumberExpression expression)
	{
		return UpdateRowNumber(expression, VisitOrderBy(expression.OrderBy));
	}

	protected static RowNumberExpression UpdateRowNumber(RowNumberExpression expression, IEnumerable<OrderExpression>? orderBy)
	{
		if (orderBy != expression.OrderBy)
		{
			if (orderBy is null)
				throw new ArgumentNullException(nameof(orderBy));

			return new RowNumberExpression(orderBy);
		}

		return expression;
	}

	protected virtual Expression VisitNamedValue(NamedValueExpression expression)
	{
		return expression;
	}

	protected virtual Expression VisitSubquery(SubqueryExpression expression)
	{
		return (DatabaseExpressionType)expression.NodeType switch
		{
			DatabaseExpressionType.Scalar => VisitScalar((ScalarExpression)expression),
			DatabaseExpressionType.Exists => VisitExists((ExistsExpression)expression),
			DatabaseExpressionType.In => VisitIn((InExpression)expression),
			_ => expression,
		};
	}

	protected virtual Expression VisitScalar(ScalarExpression expression)
	{
		if (Visit(expression.Select) is not SelectExpression selectExpression)
			throw new NullReferenceException(nameof(selectExpression));

		return UpdateScalar(expression, selectExpression);
	}

	protected static ScalarExpression UpdateScalar(ScalarExpression expression, SelectExpression select)
	{
		if (select != expression.Select)
			return new ScalarExpression(expression.Type, select);

		return expression;
	}

	protected virtual Expression VisitExists(ExistsExpression expression)
	{
		if (Visit(expression.Select) is not SelectExpression selectExpression)
			throw new NullReferenceException(nameof(selectExpression));

		return UpdateExists(expression, selectExpression);
	}

	protected static ExistsExpression UpdateExists(ExistsExpression expression, SelectExpression select)
	{
		if (select != expression.Select)
			return new ExistsExpression(select);

		return expression;
	}

	protected virtual Expression VisitIn(InExpression expression)
	{
		if (Visit(expression.Expression) is not Expression inExpression)
			throw new NullReferenceException(nameof(inExpression));

		if (Visit(expression.Select) is not SelectExpression selectExpression)
			throw new NullReferenceException(nameof(selectExpression));

		return UpdateIn(expression, inExpression, selectExpression, VisitExpressionList(expression.Values));
	}

	protected static InExpression UpdateIn(InExpression expression, Expression e, SelectExpression select, IEnumerable<Expression> values)
	{
		if (e != expression.Expression || select != expression.Select || values != expression.Values)
		{
			if (select is not null)
				return new InExpression(e, select);
			else
				return new InExpression(e, values);
		}

		return expression;
	}

	protected virtual Expression VisitAggregateSubquery(AggregateSubqueryExpression expression)
	{
		if (Visit(expression.AggregateAsSubquery) is not ScalarExpression scalarExpression)
			throw new NullReferenceException(nameof(scalarExpression));

		return UpdateAggregateSubquery(expression, scalarExpression);
	}

	protected static AggregateSubqueryExpression UpdateAggregateSubquery(AggregateSubqueryExpression expression, ScalarExpression subquery)
	{
		if (subquery != expression.AggregateAsSubquery)
			return new AggregateSubqueryExpression(expression.GroupByAlias, expression.AggregateInGroupSelect, subquery);

		return expression;
	}

	protected virtual Expression VisitSource(Expression expression)
	{
		if (Visit(expression) is not Expression sourceExpression)
			throw new NullReferenceException(nameof(sourceExpression));

		return sourceExpression;
	}

	protected virtual Expression VisitProjection(ProjectionExpression expression)
	{
		if (Visit(expression.Select) is not SelectExpression selectExpression)
			throw new NullReferenceException(nameof(selectExpression));

		if (Visit(expression.Projector) is not Expression projectorExpression)
			throw new NullReferenceException(nameof(projectorExpression));

		return UpdateProjection(expression, selectExpression, projectorExpression, expression.Aggregator);
	}

	protected static ProjectionExpression UpdateProjection(ProjectionExpression expression, SelectExpression select, Expression projector, LambdaExpression? aggregator)
	{
		if (select != expression.Select || projector != expression.Projector || aggregator != expression.Aggregator)
			return new ProjectionExpression(select, projector, aggregator);

		return expression;
	}

	protected virtual Expression VisitClientJoin(ClientJoinExpression expression)
	{
		if (Visit(expression.Projection) is not ProjectionExpression projectionExpression)
			throw new NullReferenceException(nameof(projectionExpression));

		return UpdateClientJoin(expression, projectionExpression, VisitExpressionList(expression.OuterKey), VisitExpressionList(expression.InnerKey));
	}

	protected static ClientJoinExpression UpdateClientJoin(ClientJoinExpression expression, ProjectionExpression projection, IEnumerable<Expression> outerKey, IEnumerable<Expression> innerKey)
	{
		if (projection != expression.Projection || outerKey != expression.OuterKey || innerKey != expression.InnerKey)
			return new ClientJoinExpression(projection, outerKey, innerKey);

		return expression;
	}

	protected virtual Expression VisitCommand(CommandExpression expression)
	{
		switch ((DatabaseExpressionType)expression.NodeType)
		{
			case DatabaseExpressionType.If:
				return VisitIf((IfCommandExpression)expression);
			case DatabaseExpressionType.Block:
				return VisitBlock((BlockExpression)expression);
			case DatabaseExpressionType.Declaration:
				return VisitDeclaration((DeclarationExpression)expression);
			default:
				if (VisitUnknown(expression) is not Expression unknownExpression)
					throw new NullReferenceException(nameof(unknownExpression));

				return unknownExpression;
		}
	}

	protected virtual Expression VisitBatch(BatchExpression expression)
	{
		if (Visit(expression.Operation) is not LambdaExpression lambdaExpression)
			throw new NullReferenceException(nameof(lambdaExpression));

		if (Visit(expression.BatchSize) is not Expression batchExpression)
			throw new NullReferenceException(nameof(batchExpression));

		if (Visit(expression.Stream) is not Expression streamExpression)
			throw new NullReferenceException(nameof(streamExpression));

		return UpdateBatch(expression, expression.Input, lambdaExpression, batchExpression, streamExpression);
	}

	protected static BatchExpression UpdateBatch(BatchExpression expression, Expression input, LambdaExpression operation, Expression batchSize, Expression stream)
	{
		if (input != expression.Input || operation != expression.Operation || batchSize != expression.BatchSize || stream != expression.Stream)
			return new BatchExpression(input, operation, batchSize, stream);

		return expression;
	}

	protected virtual Expression VisitIf(IfCommandExpression command)
	{
		if (Visit(command.Check) is not Expression checkExpression)
			throw new NullReferenceException(nameof(checkExpression));

		if (Visit(command.Check) is not Expression ifTrueExpression)
			throw new NullReferenceException(nameof(ifTrueExpression));

		if (Visit(command.Check) is not Expression ifFalseExpression)
			throw new NullReferenceException(nameof(ifFalseExpression));

		return UpdateIf(command, checkExpression, ifTrueExpression, ifFalseExpression);
	}

	protected static IfCommandExpression UpdateIf(IfCommandExpression command, Expression check, Expression ifTrue, Expression ifFalse)
	{
		if (check != command.Check || ifTrue != command.IfTrue || ifFalse != command.IfFalse)
			return new IfCommandExpression(check, ifTrue, ifFalse);

		return command;
	}

	protected virtual Expression VisitBlock(BlockExpression command)
	{
		return UpdateBlock(command, VisitExpressionList(command.Commands));
	}

	protected static BlockExpression UpdateBlock(BlockExpression command, IList<Expression> commands)
	{
		if (command.Commands != commands)
			return new BlockExpression(commands);

		return command;
	}

	protected virtual Expression VisitDeclaration(DeclarationExpression command)
	{
		if (Visit(command.Source) is not SelectExpression sourceExpression)
			throw new NullReferenceException(nameof(sourceExpression));

		return UpdateDeclaration(command, VisitVariableDeclarations(command.Variables), sourceExpression);
	}

	protected static DeclarationExpression UpdateDeclaration(DeclarationExpression command, IEnumerable<VariableDeclaration> variables, SelectExpression source)
	{
		if (variables != command.Variables || source != command.Source)
			return new DeclarationExpression(variables, source);

		return command;
	}

	protected virtual Expression VisitVariable(VariableExpression expression)
	{
		return expression;
	}

	protected virtual Expression VisitFunction(FunctionExpression expression)
	{
		return UpdateFunction(expression, expression.Name, VisitExpressionList(expression.Arguments));
	}

	protected static FunctionExpression UpdateFunction(FunctionExpression expression, string name, IEnumerable<Expression> arguments)
	{
		if (name != expression.Name || arguments != expression.Arguments)
			return new FunctionExpression(expression.Type, name, arguments);

		return expression;
	}

	protected virtual ColumnAssignment VisitColumnAssignment(ColumnAssignment column)
	{
		if (Visit(column.Column) is not ColumnExpression columnExpression)
			throw new NullReferenceException(nameof(columnExpression));

		if (Visit(column.Expression) is not Expression expression)
			throw new NullReferenceException(nameof(expression));

		return UpdateColumnAssignment(column, columnExpression, expression);
	}

	protected static ColumnAssignment UpdateColumnAssignment(ColumnAssignment column, ColumnExpression c, Expression e)
	{
		if (c != column.Column || e != column.Expression)
			return new ColumnAssignment(c, e);

		return column;
	}

	protected virtual ReadOnlyCollection<ColumnAssignment> VisitColumnAssignments(ReadOnlyCollection<ColumnAssignment> assignments)
	{
		List<ColumnAssignment>? alternate = null;

		for (var i = 0; i < assignments.Count; i++)
		{
			var current = assignments[i];
			var assignment = VisitColumnAssignment(current);

			if (alternate is null && assignment != current)
				alternate = assignments.Take(i).ToList();

			alternate?.Add(assignment);
		}

		if (alternate is not null)
			return alternate.AsReadOnly();

		return assignments;
	}

	protected virtual ReadOnlyCollection<ColumnDeclaration> VisitColumnDeclarations(ReadOnlyCollection<ColumnDeclaration> columns)
	{
		List<ColumnDeclaration>? alternate = null;

		for (var i = 0; i < columns.Count; i++)
		{
			var column = columns[i];

			if (Visit(column.Expression) is not Expression columnDeclarationExpression)
				throw new NullReferenceException(nameof(columnDeclarationExpression));

			if (alternate is null && columnDeclarationExpression != column.Expression)
				alternate = columns.Take(i).ToList();

			alternate?.Add(new ColumnDeclaration(column.Name, columnDeclarationExpression, column.DataType));
		}

		if (alternate is not null)
			return alternate.AsReadOnly();

		return columns;
	}

	protected virtual ReadOnlyCollection<VariableDeclaration> VisitVariableDeclarations(ReadOnlyCollection<VariableDeclaration> declarations)
	{
		List<VariableDeclaration>? alternate = null;

		for (var i = 0; i < declarations.Count; i++)
		{
			var decl = declarations[i];

			if (Visit(decl.Expression) is not Expression declarationExpression)
				throw new NullReferenceException(nameof(declarationExpression));

			if (alternate is null && declarationExpression != decl.Expression)
				alternate = declarations.Take(i).ToList();

			alternate?.Add(new VariableDeclaration(decl.Name, decl.DataType, declarationExpression));
		}

		if (alternate is not null)
			return alternate.AsReadOnly();

		return declarations;
	}

	protected virtual ReadOnlyCollection<OrderExpression>? VisitOrderBy(ReadOnlyCollection<OrderExpression>? expressions)
	{
		if (expressions is not null)
		{
			List<OrderExpression>? alternate = null;

			for (var i = 0; i < expressions.Count; i++)
			{
				var expr = expressions[i];

				if (Visit(expr.Expression) is not Expression orderByExpression)
					throw new NullReferenceException(nameof(orderByExpression));

				if (alternate is null && orderByExpression != expr.Expression)
					alternate = expressions.Take(i).ToList();

				alternate?.Add(new OrderExpression(expr.OrderType, orderByExpression));
			}

			if (alternate is not null)
				return alternate.AsReadOnly();
		}

		return expressions;
	}
}
