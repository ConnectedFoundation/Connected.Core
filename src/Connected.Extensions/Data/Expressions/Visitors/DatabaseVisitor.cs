using Connected.Data.Expressions.Expressions;
using Connected.Data.Expressions.Translation;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using BlockExpression = Connected.Data.Expressions.Expressions.BlockExpression;

namespace Connected.Data.Expressions.Visitors;
/// <summary>
/// Provides a visitor base class for walking and updating the database-aware
/// expression model used by the translator. This visitor dispatches on the
/// database-specific expression node types and exposes virtual update methods
/// that derived classes can override to customize rewriting behavior.
/// </summary>
public abstract class DatabaseVisitor
	: ExpressionVisitor
{
	/// <summary>
	/// Entry point for visiting an expression. This override dispatches to
	/// database-specific visit methods based on the expression's database node
	/// type and falls back to the base visitor for non-database nodes.
	/// </summary>
	/// <param name="expression">The expression to visit.</param>
	/// <returns>The visited (and possibly rewritten) expression.</returns>
	protected override Expression Visit(Expression expression)
	{
		/*
		 * Central dispatch for database expression nodes. The database expression
		 * model encodes a set of custom node types (DatabaseExpressionType) which
		 * require specialized handling. Map each known database node type to the
		 * corresponding Visit* method so derived visitors can override those
		 * methods to transform the query tree.
		 */
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
	/// <summary>
	/// Visit an entity wrapper expression. The default behavior visits the
	/// wrapped inner expression and reconstructs the EntityExpression when the
	/// inner expression was changed by the visit.
	/// </summary>
	protected virtual Expression VisitEntity(EntityExpression entity)
	{
		/*
		 * Visit the inner expression first. This allows derived visitors to
		 * rewrite the body's projection or column references and then preserve
		 * the entity wrapper semantics by updating the EntityExpression with the
		 * rewritten inner expression when necessary.
		 */
		if (Visit(entity.Expression) is not Expression entityExpression)
			throw new NullReferenceException(nameof(entityExpression));

		return UpdateEntity(entity, entityExpression);
	}
	/// <summary>
	/// Update an EntityExpression when the wrapped inner expression was rewritten.
	/// </summary>
	/// <param name="entity">Original entity wrapper.</param>
	/// <param name="expression">Visited inner expression.</param>
	/// <returns>Updated or original EntityExpression.</returns>
	protected static EntityExpression UpdateEntity(EntityExpression entity, Expression expression)
	{
		/*
		 * Create a new EntityExpression only when the inner expression was
		 * rewritten; otherwise return the original instance to avoid allocating
		 * unnecessary nodes.
		 */
		if (expression != entity.Expression)
			return new EntityExpression(entity.EntityType, expression);

		return entity;
	}
	/// <summary>
	/// Visit a table expression. Default implementation returns the expression
	/// unchanged; derived visitors can override to rewrite table references.
	/// </summary>
	protected virtual Expression VisitTable(TableExpression expression)
	{
		/*
		 * Table expressions are leaf nodes in the database expression tree. The
		 * base implementation does not change the table; override to apply
		 * provider/model-specific transformations (for example aliasing).
		 */
		return expression;
	}
	/// <summary>
	/// Visit a column expression. Default implementation returns the expression
	/// unchanged; derived visitors can override to rewrite column references.
	/// </summary>
	protected virtual Expression VisitColumn(ColumnExpression expression)
	{
		/*
		 * ColumnExpressions represent projected columns. Derived visitors may
		 * rewrite these to apply type coercions, name resolution or mapping to
		 * different physical representations.
		 */
		return expression;
	}
	/// <summary>
	/// Visit a SelectExpression and its components. This method visits the
	/// source, where clause, grouping, ordering and projected columns and uses
	/// UpdateSelect to return a possibly updated SelectExpression.
	/// </summary>
	protected virtual Expression VisitSelect(SelectExpression expression)
	{
		/*
		 * Walk the components of the SELECT: source (FROM), WHERE predicate,
		 * GROUP BY expressions, pagination (SKIP/TAKE), the column declarations
		 * and ORDER BY entries. Each component is visited so nested database
		 * expressions can be rewritten by derived visitors.
		 */
		var from = VisitSource(expression.From);
		var where = expression.Where is not null ? VisitWhere(expression.Where) : null;
		var groupBy = VisitExpressionList(expression.GroupBy);
		var skip = expression.Skip is not null ? Visit(expression.Skip) : null;
		var take = expression.Take is not null ? Visit(expression.Take) : null;
		var columns = VisitColumnDeclarations(expression.Columns);
		var orderBy = VisitOrderBy(expression.OrderBy);

		return UpdateSelect(expression, from, where, orderBy, groupBy, skip, take, expression.IsDistinct, expression.IsReverse, columns);
	}
	/// <summary>
	/// Visit a WHERE predicate. Default implementation returns the predicate
	/// unchanged; override to apply predicate rewrites.
	/// </summary>
	protected virtual Expression VisitWhere(Expression whereExpression)
	{
		/*
		 * This hook allows derived visitors to rewrite WHERE predicates in a
		 * centralized manner (for example to expand custom functions into SQL
		 * comparators). The base implementation acts as a passthrough.
		 */
		return whereExpression;
	}
	/// <summary>
	/// Reconstruct a SelectExpression when any of its components changed during visiting.
	/// </summary>
	protected static SelectExpression UpdateSelect(SelectExpression expression, Expression from, Expression? where,
		 IEnumerable<OrderExpression>? orderBy, IEnumerable<Expression> groupBy, Expression? skip, Expression? take,
		 bool isDistinct, bool isReverse, IEnumerable<ColumnDeclaration> columns)
	{
		/*
		 * Only allocate a new SelectExpression when one of the visited components
		 * differs from the original; otherwise return the original instance to
		 * avoid unnecessary allocations and to preserve reference equality where
		 * possible.
		 */
		if (from != expression.From || where != expression.Where || orderBy != expression.OrderBy || groupBy != expression.GroupBy
			 || take != expression.Take || skip != expression.Skip || isDistinct != expression.IsDistinct
			 || columns != expression.Columns || isReverse != expression.IsReverse)
		{
			return new SelectExpression(expression.Alias, columns, from, where, orderBy, groupBy, isDistinct, skip, take, isReverse);
		}

		return expression;
	}
	/// <summary>
	/// Visit a JoinExpression. The default implementation visits the left and
	/// right sources as well as the join condition and reconstructs the join
	/// node when necessary.
	/// </summary>
	protected virtual Expression VisitJoin(JoinExpression expression)
	{
		/*
		 * Visit and rewrite the join condition and both sides of the join.
		 * UpdateJoin will create a new JoinExpression only if something changed.
		 */
		if (expression.Condition is null || Visit(expression.Condition) is not Expression condition)
			throw new NullReferenceException(nameof(condition));

		var left = VisitSource(expression.Left);
		var right = VisitSource(expression.Right);

		return UpdateJoin(expression, expression.Join, left, right, condition);
	}
	/// <summary>
	/// Helper that updates a JoinExpression when any component changed.
	/// </summary>
	protected static JoinExpression UpdateJoin(JoinExpression expression, JoinType joinType, Expression left, Expression right, Expression condition)
	{
		/*
		 * Compare the supplied components with the original join and construct a
		 * new JoinExpression only when something changed. This preserves reference
		 * identity when possible and reduces allocations.
		 */
		if (joinType != expression.Join || left != expression.Left || right != expression.Right || condition != expression.Condition)
			return new JoinExpression(joinType, left, right, condition);

		return expression;
	}
	/// <summary>
	/// Visit an OuterJoinedExpression which encapsulates outer join null-test and
	/// the wrapped expression. Both the test and the wrapped expression are
	/// visited and the node is updated when either changes.
	/// </summary>
	protected virtual Expression VisitOuterJoined(OuterJoinedExpression expression)
	{
		/*
		 * The OuterJoined node contains a boolean test expression and an inner
		 * expression which must be visited so any nested database expressions can
		 * be rewritten. Preserve semantics by updating only when necessary.
		 */
		if (Visit(expression.Test) is not Expression joinTest)
			throw new NullReferenceException(nameof(joinTest));

		if (Visit(expression.Expression) is not Expression joinExpression)
			throw new NullReferenceException(nameof(JoinExpression));

		return UpdateOuterJoined(expression, joinTest, joinExpression);
	}
	/// <summary>
	/// Helper that reconstructs an OuterJoinedExpression only when components changed.
	/// </summary>
	protected static OuterJoinedExpression UpdateOuterJoined(OuterJoinedExpression expression, Expression test, Expression e)
	{
		/*
		 * Recreate the OuterJoinedExpression only when the visited test or the
		 * wrapped expression changed so callers can rely on reference equality
		 * otherwise.
		 */
		if (test != expression.Test || e != expression.Expression)
			return new OuterJoinedExpression(test, e);

		return expression;
	}
	/// <summary>
	/// Visit an AggregateExpression and its argument, updating the aggregate
	/// node when the argument was rewritten.
	/// </summary>
	protected virtual Expression VisitAggregate(AggregateExpression expression)
	{
		/*
		 * Visit the aggregate argument so nested expressions are rewritten and
		 * then rebuild the AggregateExpression when appropriate.
		 */
		if (Visit(expression.Argument) is not Expression argumentExpression)
			throw new NullReferenceException(nameof(argumentExpression));

		return UpdateAggregate(expression, expression.Type, expression.AggregateName, argumentExpression, expression.IsDistinct);
	}
	/// <summary>
	/// Helper that updates an AggregateExpression instance only when needed.
	/// </summary>
	protected static AggregateExpression UpdateAggregate(AggregateExpression expression, Type type, string aggType, Expression e, bool isDistinct)
	{
		/*
		 * Recreate the aggregate node only when type, aggregate name, argument
		 * or distinct-ness changed to ensure stable nodes otherwise.
		 */
		if (type != expression.Type || aggType != expression.AggregateName || e != expression.Argument || isDistinct != expression.IsDistinct)
			return new AggregateExpression(type, aggType, e, isDistinct);

		return expression;
	}
	/// <summary>
	/// Visit an IsNullExpression and update it when the underlying expression
	/// changes.
	/// </summary>
	protected virtual Expression VisitIsNull(IsNullExpression expression)
	{
		if (Visit(expression.Expression) is not Expression nullExpression)
			throw new NullReferenceException(nameof(nullExpression));

		return UpdateIsNull(expression, nullExpression);
	}
	/// <summary>
	/// Update an IsNullExpression when its inner expression was rewritten.
	/// </summary>
	/// <param name="expression">Original IsNullExpression.</param>
	/// <param name="e">Visited inner expression.</param>
	/// <returns>Updated or original IsNullExpression.</returns>
	protected static IsNullExpression UpdateIsNull(IsNullExpression expression, Expression e)
	{
		/*
		 * Build a new IsNullExpression only when the wrapped expression changed.
		 */
		if (e != expression.Expression)
			return new IsNullExpression(e);

		return expression;
	}
	/// <summary>
	/// Visit a BetweenExpression and its bounds, updating when any subexpression changes.
	/// </summary>
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
	/// <summary>
	/// Update a BetweenExpression when any component changed during visiting.
	/// </summary>
	protected static BetweenExpression UpdateBetween(BetweenExpression expression, Expression e, Expression lower, Expression upper)
	{
		/*
		 * Recreate the BetweenExpression only when any of its components changed.
		 */
		if (e != expression.Expression || lower != expression.Lower || upper != expression.Upper)
			return new BetweenExpression(e, lower, upper);

		return expression;
	}
	/// <summary>
	/// Visit a RowNumberExpression and its ordering component.
	/// </summary>
	protected virtual Expression VisitRowNumber(RowNumberExpression expression)
	{
		return UpdateRowNumber(expression, VisitOrderBy(expression.OrderBy));
	}
	/// <summary>
	/// Update a RowNumberExpression if ordering changed.
	/// </summary>
	protected static RowNumberExpression UpdateRowNumber(RowNumberExpression expression, IEnumerable<OrderExpression>? orderBy)
	{
		/*
		 * Recreate the row-number expression when the ordering list changed.
		 */
		if (orderBy != expression.OrderBy)
			return orderBy is null ? throw new ArgumentNullException(nameof(orderBy)) : new RowNumberExpression(orderBy);

		return expression;
	}
	/// <summary>
	/// Visit a NamedValueExpression. Default implementation returns the node unchanged.
	/// </summary>
	protected virtual Expression VisitNamedValue(NamedValueExpression expression)
	{
		return expression;
	}
	/// <summary>
	/// Visit a subquery node (scalar/exists/in) and dispatch to the appropriate
	/// handler for the concrete subquery type.
	/// </summary>
	protected virtual Expression VisitSubquery(SubqueryExpression expression)
	{
		/*
		 * Dispatch based on the specific subquery node type. Scalars, EXISTS and
		 * IN clauses are represented as different subtypes of SubqueryExpression
		 * and must be visited accordingly.
		 */
		return (DatabaseExpressionType)expression.NodeType switch
		{
			DatabaseExpressionType.Scalar => VisitScalar((ScalarExpression)expression),
			DatabaseExpressionType.Exists => VisitExists((ExistsExpression)expression),
			DatabaseExpressionType.In => VisitIn((InExpression)expression),
			_ => expression,
		};
	}
	/// <summary>
	/// Visit a scalar subquery and update if the inner select changes.
	/// </summary>
	protected virtual Expression VisitScalar(ScalarExpression expression)
	{
		if (expression.Select is null || Visit(expression.Select) is not SelectExpression selectExpression)
			throw new NullReferenceException(nameof(selectExpression));

		return UpdateScalar(expression, selectExpression);
	}
	/// <summary>
	/// Update a ScalarExpression when its inner Select was rewritten.
	/// </summary>
	protected static ScalarExpression UpdateScalar(ScalarExpression expression, SelectExpression select)
	{
		/*
		 * Recreate the scalar node only when the contained select changed.
		 */
		if (select != expression.Select)
			return new ScalarExpression(expression.Type, select);

		return expression;
	}
	/// <summary>
	/// Visit an EXISTS subquery and update when the inner select changed.
	/// </summary>
	protected virtual Expression VisitExists(ExistsExpression expression)
	{
		if (expression.Select is null || Visit(expression.Select) is not SelectExpression selectExpression)
			throw new NullReferenceException(nameof(selectExpression));

		return UpdateExists(expression, selectExpression);
	}
	/// <summary>
	/// Update an ExistsExpression if its inner select changed.
	/// </summary>
	protected static ExistsExpression UpdateExists(ExistsExpression expression, SelectExpression select)
	{
		/*
		 * Recreate the ExistsExpression only when the contained select changed.
		 */
		if (select != expression.Select)
			return new ExistsExpression(select);

		return expression;
	}
	/// <summary>
	/// Visit an IN expression and update it when the expression or select/values change.
	/// </summary>
	protected virtual Expression VisitIn(InExpression expression)
	{
		if (Visit(expression.Expression) is not Expression inExpression)
			throw new NullReferenceException(nameof(inExpression));

		if (expression.Select is null || Visit(expression.Select) is not SelectExpression selectExpression)
			throw new NullReferenceException(nameof(selectExpression));

		return UpdateIn(expression, inExpression, selectExpression, VisitExpressionList(expression.Values));
	}
	/// <summary>
	/// Update an InExpression when its tested expression, select or values changed.
	/// </summary>
	protected static InExpression UpdateIn(InExpression expression, Expression e, SelectExpression select, IEnumerable<Expression> values)
	{
		/*
		 * Construct a new InExpression when any component changed; preserve the
		 * original instance otherwise.
		 */
		if (e != expression.Expression || select != expression.Select || values != expression.Values)
		{
			if (select is not null)
				return new InExpression(e, select);
			else
				return new InExpression(e, values);
		}

		return expression;
	}
	/// <summary>
	/// Visit an AggregateSubqueryExpression and update when its scalar changes.
	/// </summary>
	protected virtual Expression VisitAggregateSubquery(AggregateSubqueryExpression expression)
	{
		if (Visit(expression.AggregateAsSubquery) is not ScalarExpression scalarExpression)
			throw new NullReferenceException(nameof(scalarExpression));

		return UpdateAggregateSubquery(expression, scalarExpression);
	}
	/// <summary>
	/// Update an AggregateSubqueryExpression when the contained scalar changed.
	/// </summary>
	protected static AggregateSubqueryExpression UpdateAggregateSubquery(AggregateSubqueryExpression expression, ScalarExpression subquery)
	{
		/*
		 * Recreate the wrapper only when the contained scalar subquery changed.
		 */
		if (subquery != expression.AggregateAsSubquery)
			return new AggregateSubqueryExpression(expression.GroupByAlias, expression.AggregateInGroupSelect, subquery);

		return expression;
	}
	/// <summary>
	/// Visit a source expression (FROM clause) and return the visited source.
	/// </summary>
	protected virtual Expression VisitSource(Expression expression)
	{
		/*
		 * Visit the source (FROM) expression and return the visited result.
		 * This delegates to the generic Visit to allow derived visitors to
		 * rewrite the source representation. Throw if the visit unexpectedly
		 * returns null to surface errors early.
		 */
		if (Visit(expression) is not Expression sourceExpression)
			throw new NullReferenceException(nameof(sourceExpression));

		return sourceExpression;
	}
	/// <summary>
	/// Visit a ProjectionExpression and update when select/projector/aggregator change.
	/// </summary>
	protected virtual Expression VisitProjection(ProjectionExpression expression)
	{
		/*
		 * Visit the projection's select and projector so nested database
		 * expressions can be rewritten. Rebuild the ProjectionExpression only
		 * when one of the components changed.
		 */
		if (Visit(expression.Select) is not SelectExpression selectExpression)
			throw new NullReferenceException(nameof(selectExpression));

		if (Visit(expression.Projector) is not Expression projectorExpression)
			throw new NullReferenceException(nameof(projectorExpression));

		return UpdateProjection(expression, selectExpression, projectorExpression, expression.Aggregator);
	}
	/// <summary>
	/// Update a ProjectionExpression when any component changed.
	/// </summary>
	protected static ProjectionExpression UpdateProjection(ProjectionExpression expression, SelectExpression select, Expression projector, LambdaExpression? aggregator)
	{
		/*
		 * Recreate the ProjectionExpression only when select, projector or the
		 * aggregator lambda changed.
		 */
		if (select != expression.Select || projector != expression.Projector || aggregator != expression.Aggregator)
			return new ProjectionExpression(select, projector, aggregator);

		return expression;
	}
	/// <summary>
	/// Visit a client-join node which contains a projection plus outer/inner key lists.
	/// </summary>
	protected virtual Expression VisitClientJoin(ClientJoinExpression expression)
	{
		/*
		 * Visit the projection and both key lists that drive the client-side join.
		 * Rebuild the ClientJoinExpression only when one of the visited components
		 * changed during visiting.
		 */
		if (Visit(expression.Projection) is not ProjectionExpression projectionExpression)
			throw new NullReferenceException(nameof(projectionExpression));

		return UpdateClientJoin(expression, projectionExpression, VisitExpressionList(expression.OuterKey), VisitExpressionList(expression.InnerKey));
	}
	/// <summary>
	/// Update a ClientJoinExpression when its projection or key lists changed.
	/// </summary>
	protected static ClientJoinExpression UpdateClientJoin(ClientJoinExpression expression, ProjectionExpression projection, IEnumerable<Expression> outerKey, IEnumerable<Expression> innerKey)
	{
		/*
		 * Recreate the client-join wrapper only when projection or key lists differ
		 * from their original instances. This preserves identity when no changes
		 * occur and returns the rewritten components otherwise.
		 */
		if (projection != expression.Projection || outerKey != expression.OuterKey || innerKey != expression.InnerKey)
			return new ClientJoinExpression(projection, outerKey, innerKey);

		return expression;
	}
	/// <summary>
	/// Visit command-like expressions (If, Block, Declaration). The dispatch
	/// forwards to the specific command visitor.
	/// </summary>
	protected virtual Expression VisitCommand(CommandExpression expression)
	{
		/*
		 * Dispatch command-like nodes to the specific command visitors. This
		 * keeps command handling centralized and allows different command kinds
		 * (If/Block/Declaration) to be handled by dedicated methods.
		 */
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
	/// <summary>
	/// Visit a BatchExpression used for batched operations and update when parts change.
	/// </summary>
	protected virtual Expression VisitBatch(BatchExpression expression)
	{
		/*
		 * Visit the batch operation lambda, the batch size and the stream
		 * expression, then rebuild the BatchExpression when parts changed.
		 */
		if (Visit(expression.Operation) is not LambdaExpression lambdaExpression)
			throw new NullReferenceException(nameof(lambdaExpression));

		if (Visit(expression.BatchSize) is not Expression batchExpression)
			throw new NullReferenceException(nameof(batchExpression));

		if (Visit(expression.Stream) is not Expression streamExpression)
			throw new NullReferenceException(nameof(streamExpression));

		return UpdateBatch(expression, expression.Input, lambdaExpression, batchExpression, streamExpression);
	}
	/// <summary>
	/// Update a BatchExpression when its parts change.
	/// </summary>
	protected static BatchExpression UpdateBatch(BatchExpression expression, Expression input, LambdaExpression operation, Expression batchSize, Expression stream)
	{
		/*
		 * Only recreate the BatchExpression when any of the visited parts differ
		 * from the original ones to avoid unnecessary allocations.
		 */
		if (input != expression.Input || operation != expression.Operation || batchSize != expression.BatchSize || stream != expression.Stream)
			return new BatchExpression(input, operation, batchSize, stream);

		return expression;
	}
	/// <summary>
	/// Visit an If command expression and return a possibly updated node.
	/// </summary>
	protected virtual Expression VisitIf(IfCommandExpression command)
	{
		/*
		 * Visit the condition and both branches of the if-command. Rebuild the
		 * IfCommandExpression only when one of the parts was rewritten.
		 */
		if (Visit(command.Check) is not Expression checkExpression)
			throw new NullReferenceException(nameof(checkExpression));

		if (Visit(command.IfTrue) is not Expression ifTrueExpression)
			throw new NullReferenceException(nameof(ifTrueExpression));

		if (Visit(command.IfFalse) is not Expression ifFalseExpression)
			throw new NullReferenceException(nameof(ifFalseExpression));

		return UpdateIf(command, checkExpression, ifTrueExpression, ifFalseExpression);
	}
	/// <summary>
	/// Update an IfCommandExpression when its branches or condition change.
	/// </summary>
	protected static IfCommandExpression UpdateIf(IfCommandExpression command, Expression check, Expression ifTrue, Expression ifFalse)
	{
		/*
		 * Recreate the IfCommandExpression when any subcomponent changed.
		 */
		if (check != command.Check || ifTrue != command.IfTrue || ifFalse != command.IfFalse)
			return new IfCommandExpression(check, ifTrue, ifFalse);

		return command;
	}
	/// <summary>
	/// Visit a Block command and update its contained commands when necessary.
	/// </summary>
	protected virtual Expression VisitBlock(BlockExpression command)
	{
		/*
		 * Visit and rewrite the block's commands list then rebuild the block
		 * only when the commands changed.
		 */
		return UpdateBlock(command, VisitExpressionList(command.Commands));
	}
	/// <summary>
	/// Update a BlockExpression when its commands change.
	/// </summary>
	protected static BlockExpression UpdateBlock(BlockExpression command, IList<Expression> commands)
	{
		/*
		 * If the visited command list differs from the original one, construct a
		 * new BlockExpression to reflect the updated commands.
		 */
		if (command.Commands != commands)
			return new BlockExpression(commands);

		return command;
	}
	/// <summary>
	/// Visit a Declaration command which binds variables to a source select.
	/// </summary>
	protected virtual Expression VisitDeclaration(DeclarationExpression command)
	{
		/*
		 * Visit the source select and the variable declarations. Rebuild the
		 * DeclarationExpression only when the variables or source changed.
		 */
		if (Visit(command.Source) is not SelectExpression sourceExpression)
			throw new NullReferenceException(nameof(sourceExpression));

		return UpdateDeclaration(command, VisitVariableDeclarations(command.Variables), sourceExpression);
	}
	/// <summary>
	/// Recreate the DeclarationExpression when variable declarations or the associated
	/// source select were rewritten during visiting.
	/// </summary>
	protected static DeclarationExpression UpdateDeclaration(DeclarationExpression command, IEnumerable<VariableDeclaration> variables, SelectExpression source)
	{
		/*
		 * Recreate the DeclarationExpression when the visited variables or the
		 * visited source differ from the originals.
		 */
		if (variables != command.Variables || source != command.Source)
			return new DeclarationExpression(variables, source);

		return command;
	}
	/// <summary>
	/// Visit a variable expression. Default returns the expression unchanged.
	/// </summary>
	protected virtual Expression VisitVariable(VariableExpression expression)
	{
		/*
		 * Variable expressions are treated as leaves by the database visitor.
		 * The base implementation returns the original expression.
		 */
		return expression;
	}
	/// <summary>
	/// Visit a user-defined function expression and update its argument list.
	/// </summary>
	protected virtual Expression VisitFunction(FunctionExpression expression)
	{
		/*
		 * Visit each function argument and rebuild the FunctionExpression only
		 * when name or arguments changed.
		 */
		return UpdateFunction(expression, expression.Name, VisitExpressionList(expression.Arguments));
	}
	/// <summary>
	/// Update a FunctionExpression when its name or arguments change.
	/// </summary>
	protected static FunctionExpression UpdateFunction(FunctionExpression expression, string name, IEnumerable<Expression> arguments)
	{
		/*
		 * Recreate the FunctionExpression only when the name or the visited
		 * argument list differ from the originals.
		 */
		if (name != expression.Name || arguments != expression.Arguments)
			return new FunctionExpression(expression.Type, name, arguments);

		return expression;
	}
	/// <summary>
	/// Visit a ColumnAssignment and update its column/expression when needed.
	/// </summary>
	protected virtual ColumnAssignment VisitColumnAssignment(ColumnAssignment column)
	{
		/*
		 * Visit the column reference and the assigned expression, then rebuild
		 * the ColumnAssignment when changes are detected.
		 */
		if (Visit(column.Column) is not ColumnExpression columnExpression)
			throw new NullReferenceException(nameof(columnExpression));

		if (Visit(column.Expression) is not Expression expression)
			throw new NullReferenceException(nameof(expression));

		return UpdateColumnAssignment(column, columnExpression, expression);
	}
	/// <summary>
	/// Update a ColumnAssignment when its column or expression changes.
	/// </summary>
	protected static ColumnAssignment UpdateColumnAssignment(ColumnAssignment column, ColumnExpression c, Expression e)
	{
		/*
		 * Recreate the ColumnAssignment only when either the column or the
		 * assigned expression differs from the original.
		 */
		if (c != column.Column || e != column.Expression)
			return new ColumnAssignment(c, e);

		return column;
	}
	/// <summary>
	/// Visit and potentially rewrite a collection of variable declarations.
	/// </summary>
	protected virtual ReadOnlyCollection<VariableDeclaration> VisitVariableDeclarations(ReadOnlyCollection<VariableDeclaration> declarations)
	{
		/*
		 * Iterate through declarations and visit each declaration expression so
		 * nested expressions may be rewritten by derived visitors. Only when a
		 * difference is observed do we allocate an alternate list and populate
		 * it with rewritten entries. This preserves identity when inputs are
		 * unchanged and reduces allocation churn.
		 */
		List<VariableDeclaration>? alternate = null;
		/*
		 * Loop over each variable declaration so that we can visit and possibly
		 * rewrite its associated expression.
		 */
		for (var i = 0; i < declarations.Count; i++)
		{
			var decl = declarations[i];
			/*
			 * Visit the declaration expression to allow nested rewrites. Throw if
			 * the visit unexpectedly returns null to help detect issues early.
			 */
			if (Visit(decl.Expression) is not Expression declarationExpression)
				throw new NullReferenceException(nameof(declarationExpression));
			/*
			 * If this is the first detected change, allocate an alternate list and
			 * copy the unchanged prefix to avoid allocating unnecessarily when no
			 * changes occur.
			 */
			if (alternate is null && declarationExpression != decl.Expression)
				alternate = [.. declarations.Take(i)];
			/*
			 * Add the (possibly rewritten) declaration to the alternate list when
			 * it exists.
			 */
			alternate?.Add(new VariableDeclaration(decl.Name, decl.DataType, declarationExpression));
		}

		/*
		 * If any changes were recorded, return a read-only wrapper of the
		 * alternate list; otherwise return the original collection.
		 */
		if (alternate is not null)
			return alternate.AsReadOnly();

		return declarations;
	}
	/// <summary>
	/// Visit a collection of OrderExpression instances and update if any
	/// individual ordering expression changes.
	/// </summary>
	protected virtual ReadOnlyCollection<OrderExpression>? VisitOrderBy(ReadOnlyCollection<OrderExpression>? expressions)
	{
		/*
		 * Iterate each OrderExpression, visit its inner expression and build an
		 * alternate list only when a difference is observed. This preserves the
		 * original collection identity when no changes occur and minimizes
		 * allocations.
		 */
		if (expressions is not null)
		{
			List<OrderExpression>? alternate = null;
			/*
			 * Loop over the order expressions to allow derived visitors to rewrite
			 * each ordering expression independently.
			 */
			for (var i = 0; i < expressions.Count; i++)
			{
				var expr = expressions[i];
				/*
				 * Visit the ordering expression to apply rewrites; throw if the visit
				 * unexpectedly returns null so the caller can diagnose the issue.
				 */
				if (Visit(expr.Expression) is not Expression orderByExpression)
					throw new NullReferenceException(nameof(orderByExpression));
				/*
				 * On first detected difference, allocate and copy the earlier items
				 * so that the alternate list starts with the unchanged prefix.
				 */
				if (alternate is null && orderByExpression != expr.Expression)
					alternate = [.. expressions.Take(i)];
				/*
				 * Add a new OrderExpression using the visited expression to the
				 * alternate list when it exists.
				 */
				alternate?.Add(new OrderExpression(expr.OrderType, orderByExpression));
			}

			/*
			 * Return the alternate read-only list when changes were recorded.
			 */
			if (alternate is not null)
				return alternate.AsReadOnly();
		}

		/*
		 * No changes were made; return the original collection.
		 */
		return expressions;
	}
	/// <summary>
	/// Visit a collection of ColumnAssignment nodes and return a read-only
	/// collection containing rewritten assignments when changes are detected.
	/// </summary>
	protected virtual ReadOnlyCollection<ColumnAssignment> VisitColumnAssignments(ReadOnlyCollection<ColumnAssignment> assignments)
	{
		/*
		 * Iterate the assignments and visit each ColumnAssignment. If any
		 * assignment changes during visiting, build an alternate list and copy
		 * the earlier unchanged prefix to preserve identity when possible.
		 */
		List<ColumnAssignment>? alternate = null;
		/*
		 * Loop over the assignments to visit each one in turn.
		 */
		for (var i = 0; i < assignments.Count; i++)
		{
			var current = assignments[i];
			/*
			 * Visit the current assignment which itself visits the column and
			 * expression components; throw if the visit returns null.
			 */
			var assignment = VisitColumnAssignment(current);
			/*
			 * On the first change, allocate an alternate list and copy the
			 * unchanged prefix so the new list begins with the same items.
			 */
			if (alternate is null && assignment != current)
				alternate = [.. assignments.Take(i)];
			/*
			 * Append the visited assignment to the alternate list when it exists.
			 */
			alternate?.Add(assignment);
		}

		/*
		 * Return the read-only alternate list when changes were observed.
		 */
		if (alternate is not null)
			return alternate.AsReadOnly();

		/*
		 * No changes detected; return the original collection to avoid
		 * unnecessary allocations and to preserve reference identity.
		 */
		return assignments;
	}
	/// <summary>
	/// Visit a collection of column declarations, visiting each declaration's
	/// expression and returning a rewritten read-only collection when any
	/// declaration changed.
	/// </summary>
	protected virtual ReadOnlyCollection<ColumnDeclaration> VisitColumnDeclarations(ReadOnlyCollection<ColumnDeclaration> columns)
	{
		/*
		 * Iterate through the column declarations, visiting each declaration's
		 * expression. If the expression of a declaration changes, record the
		 * change. If any changes were recorded (indicating that the original
		 * collection is different), allocate a new list and copy the changed
		 * declarations to preserve memory, only allocating new lists when
		 * necessary.
		 */
		List<ColumnDeclaration>? alternate = null;
		/*
		 * Loop over each column declaration to visit and possibly rewrite its expression.
		 */
		for (var i = 0; i < columns.Count; i++)
		{
			var column = columns[i];
			/*
			 * Visit the column's expression so nested expressions can be rewritten.
			 * The Visit call allows derived visitors to transform inner subexpressions.
			 */
			if (Visit(column.Expression) is not Expression columnDeclarationExpression)
				throw new NullReferenceException(nameof(columnDeclarationExpression));
			/*
			 * On the first detected difference, allocate the alternate list and copy
			 * the earlier (unchanged) declarations. This avoids allocating a new
			 * list when no changes occur.
			 */
			if (alternate is null && columnDeclarationExpression != column.Expression)
				alternate = [.. columns.Take(i)];
			/*
			 * If we have an alternate list, add a rewritten declaration to it.
			 * Otherwise, nothing is added until a difference is detected.
			 */
			alternate?.Add(new ColumnDeclaration(column.Name, columnDeclarationExpression, column.DataType));
		}

		/*
		 * If any differences were recorded, return the read-only alternate list.
		 * Returning a ReadOnlyCollection preserves immutability of the returned
		 * collection and matches the original API contract.
		 */
		if (alternate is not null)
			return alternate.AsReadOnly();

		/*
		 * No differences were found; return the original collection to avoid
		 * unnecessary allocations and to preserve reference identity.
		 */
		return columns;
	}
}
