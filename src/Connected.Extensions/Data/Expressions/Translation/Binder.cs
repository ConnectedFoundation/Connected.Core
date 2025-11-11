using Connected.Data.Expressions.Evaluation;
using Connected.Data.Expressions.Expressions;
using Connected.Data.Expressions.Mappings;
using Connected.Data.Expressions.Reflection;
using Connected.Data.Expressions.Translation.Projections;
using Connected.Data.Expressions.Visitors;
using Connected.Reflection;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace Connected.Data.Expressions.Translation;

/// <summary>
/// Binds and rewrites high-level LINQ expression trees into the database-aware
/// expression model used by the translator.
/// </summary>
/// <remarks>
/// The <see cref="Binder"/> visits an expression tree produced by user code
/// and performs several transformations: mapping parameters to projected
/// projectors, rewriting aggregate and grouping constructs, introducing
/// joins and subqueries and ensuring that expressions are represented using
/// the database expression types (for example <see cref="SelectExpression"/>,
/// <see cref="ProjectionExpression"/>, <see cref="AggregateExpression"/>, etc.).
/// </remarks>
public sealed class Binder
	: DatabaseVisitor
{
	/*
	 * The Binder encapsulates the translation from arbitrary LINQ expressions
	 * into a normalized set of database-aware expression nodes. The overall
	 * process performed by the Binder can be summarized as:
	 * 1) Replace lambda parameters with projector expressions so inner lambdas
	 *    are evaluated against the projected row representation.
	 * 2) Normalize collection operators (Select, Where, Join, GroupBy, etc.)
	 *    into Select/Projection/Join/Aggregate expressions used by translators.
	 * 3) Introduce subqueries, scalar subqueries and EXISTS/IN constructs where
	 *    language translation requires them.
	 * 4) Track group-by projections so aggregates can bind correctly to the
	 *    grouping element and produce AggregateSubquery expressions when needed.
	 * The implementation is intentionally conservative: it prefers explicit
	 * projection construction and simple, testable transformations over magic
	 * heuristics. This keeps the translator predictable and easier to debug.
	 */
	private Binder(ExpressionCompilationContext context, Expression expression)
	{
		Context = context;
		Expression = expression;

		ParameterMapping = new();
		GroupByMap = new();
	}

	/// <summary>
	/// Compilation context containing language-specific settings, parameters and variables.
	/// </summary>
	private ExpressionCompilationContext Context { get; }

	/// <summary>
	/// Maps lambda parameters to replacement expressions (typically a projector expression).
	/// </summary>
	private Dictionary<ParameterExpression, Expression> ParameterMapping { get; }

	/// <summary>
	/// Tracks group-by projections and associated descriptors used when binding aggregates.
	/// </summary>
	private Dictionary<Expression, GroupByDescriptor> GroupByMap { get; }

	/// <summary>
	/// Temporary storage for ThenBy ordering expressions while binding ordering chains.
	/// </summary>
	private List<OrderExpression>? ThenBys { get; set; }

	/// <summary>
	/// The current element used when processing group projections.
	/// </summary>
	private Expression CurrentGroupElement { get; set; }

	/// <summary>
	/// The root expression being bound.
	/// </summary>
	private Expression Expression { get; set; }

	/// <summary>
	/// Binds the specified expression using the given compilation context.
	/// </summary>
	/// <param name="context">The compilation context.</param>
	/// <param name="expression">The expression to bind.</param>
	/// <returns>The bound expression or <c>null</c> when binding failed.</returns>
	public static Expression? Bind(ExpressionCompilationContext context, Expression expression)
	{
		/*
		 * Create a Binder instance and perform a full visit of the expression
		 * tree. The Visit traversal will invoke specific Bind* helpers based on
		 * encountered method calls and expression shapes, producing database-
		 * aware expressions suitable for translation.
		 */
		return new Binder(context, expression).Visit(expression);
	}

	/// <summary>
	/// Resolves a member access expression against a projected source expression.
	/// </summary>
	public static Expression Bind(Expression source, MemberInfo member)
	{
		/*
		 * This helper examines the runtime shape of the source expression and
		 * maps the requested member access to an appropriate expression the
		 * translation pipeline understands: columns, constants, projection
		 * expressions, outer-joined wrappers or conditional mappings. The switch
		 * below enumerates supported source node types and implements the
		 * resolution logic for each.
		 */
		switch (source.NodeType)
		{
			case (ExpressionType)DatabaseExpressionType.Entity:
				/*
				 * Entity: delegate to the inner expression; preserve original entity
				 * member access when possible to keep entity semantics intact.
				 */
				var ex = (EntityExpression)source;
				var result = Bind(ex.Expression, member);
				var mex = result as MemberExpression;

				if (mex is not null && mex.Expression == ex.Expression && mex.Member == member)
					return Expression.MakeMemberAccess(source, member);

				return result;

			case ExpressionType.Convert:
				/*
				 * Convert: strip and re-bind against the operand so member lookups
				 * are not affected by CLR conversions/wrappers.
				 */
				var ux = (UnaryExpression)source;

				return Bind(ux.Operand, member);

			case ExpressionType.MemberInit:
				/*
				 * MemberInit: look up the MemberAssignment for the requested member
				 * and return the assigned expression when available (anonymous type
				 * and object initializer scenarios).
				 */
				var min = (MemberInitExpression)source;

				for (var i = 0; i < min.Bindings.Count; i++)
				{
					var assign = min.Bindings[i] as MemberAssignment;

					if (assign is not null && MembersMatch(assign.Member, member))
						return assign.Expression;
				}

				break;

			case ExpressionType.New:
				/*
				 * New: map anonymous type / constructor argument members back to the
				 * corresponding constructor argument expression. Special-case grouping
				 * result types where Key is located in the first argument.
				 */
				var nex = (NewExpression)source;

				if (nex.Members is not null)
				{
					for (var i = 0; i < nex.Members.Count; i++)
					{
						if (MembersMatch(nex.Members[i], member))
							return nex.Arguments[i];
					}
				}
				else if (nex.Type.GetTypeInfo().IsGenericType && nex.Type.GetGenericTypeDefinition() == typeof(Grouping<,>))
				{
					if (string.Equals(member.Name, "Key", StringComparison.Ordinal))
						return nex.Arguments[0];
				}

				break;

			case (ExpressionType)DatabaseExpressionType.Projection:
				/*
				 * Projection: delegate member binding to the projector expression
				 * and wrap the result as a ProjectionExpression to preserve the
				 * projection's select context and potential aggregator.
				 */
				var proj = (ProjectionExpression)source;
				var newProjector = Bind(proj.Projector, member);
				var mt = Members.GetMemberType(member);

				return new ProjectionExpression(proj.Select, newProjector, Aggregator.GetAggregator(mt, typeof(IEnumerable<>).MakeGenericType(mt)));

			case (ExpressionType)DatabaseExpressionType.OuterJoined:
				/*
				 * OuterJoined: bind inner expression; if the result is a simple
				 * ColumnExpression return it, otherwise preserve outer-join semantics
				 * by returning an OuterJoinedExpression wrapper.
				 */
				var oj = (OuterJoinedExpression)source;
				var em = Bind(oj.Expression, member);

				if (em is ColumnExpression)
					return em;

				return new OuterJoinedExpression(oj.Test, em);

			case ExpressionType.Conditional:
				/*
				 * Conditional: bind member on both branches and rebuild a conditional
				 * that selects the bound member according to the original test.
				 */
				var cex = (ConditionalExpression)source;

				return Expression.Condition(cex.Test, Bind(cex.IfTrue, member), Bind(cex.IfFalse, member));

			case ExpressionType.Constant:
				/*
				 * Constant: extract captured closure values (fields/properties) and
				 * return a typed ConstantExpression. If the closure is null return the
				 * default CLR value for the member's type.
				 */
				var con = (ConstantExpression)source;
				var memberType = Members.GetMemberType(member);

				if (con.Value is null)
					return Expression.Constant(GetDefault(memberType), memberType);
				else
					return Expression.Constant(GetValue(con.Value, member), memberType);
		}

		/*
		 * Default: no special mapping found, return a normal member access node.
		 */
		return Expression.MakeMemberAccess(source, member);
	}

	/// <summary>
	/// Visits a method call and dispatches to the appropriate Bind* implementation
	/// for supported query operators (Where, Select, Join, GroupBy, etc.).
	/// </summary>
	protected override Expression? VisitMethodCall(MethodCallExpression expression)
	{
		/*
		 * Central dispatcher for LINQ method calls. Determine whether the called
		 * method is part of the queryable operators and route to the dedicated
		 * binding helper that will normalize it into the database expression
		 * model. Lambda arguments are normalized via GetLambda when necessary.
		 */
		if (expression.Method.IsInQueryable())
		{
			/*
			 * Map operator names to binding helpers. Many operators have multiple
			 * overloads determined by argument count; interpret the shape and
			 * choose the correct Bind* helper accordingly.
			 */
			switch (expression.Method.Name)
			{
				case "Where":
					/*
					 * Where(source, predicate)
					 */
					return BindWhere(expression.Arguments[0], GetLambda(expression.Arguments[1]));
				case "Select":
					/*
					 * Select(source, selector)
					 */
					return BindSelect(expression.Arguments[0], GetLambda(expression.Arguments[1]));
				case "SelectMany":

					/*
					 * SelectMany has two common forms. Normalize lambdas and delegate
					 * to BindSelectMany which will produce an appropriate join.
					 */
					if (expression.Arguments.Count == 2)
						return BindSelectMany(expression.Arguments[0], GetLambda(expression.Arguments[1]), null);
					else if (expression.Arguments.Count == 3)
						return BindSelectMany(expression.Arguments[0], GetLambda(expression.Arguments[1]), GetLambda(expression.Arguments[2]));

					break;
				case "Join":

					/*
					 * Join(source, inner, outerKeySelector, innerKeySelector, resultSelector)
					 */
					return BindJoin(expression.Arguments[0], expression.Arguments[1], GetLambda(expression.Arguments[2]),
						 GetLambda(expression.Arguments[3]), GetLambda(expression.Arguments[4]));

				case "GroupJoin":

					/*
					 * GroupJoin translated to GroupJoin helper which in turn uses Where + grouping.
					 */
					if (expression.Arguments.Count == 5)
					{
						return BindGroupJoin(expression.Method, expression.Arguments[0], expression.Arguments[1], GetLambda(expression.Arguments[2]),
							 GetLambda(expression.Arguments[3]), GetLambda(expression.Arguments[4]));
					}

					break;
				case "OrderBy":
					return BindOrderBy(expression.Arguments[0], GetLambda(expression.Arguments[1]), OrderType.Ascending);
				case "OrderByDescending":
					return BindOrderBy(expression.Arguments[0], GetLambda(expression.Arguments[1]), OrderType.Descending);
				case "ThenBy":
					return BindThenBy(expression.Arguments[0], GetLambda(expression.Arguments[1]), OrderType.Ascending);
				case "ThenByDescending":
					return BindThenBy(expression.Arguments[0], GetLambda(expression.Arguments[1]), OrderType.Descending);
				case "GroupBy":

					/*
					 * GroupBy overloads: (source, key) or (source, key, element) or with result selector
					 */
					if (expression.Arguments.Count == 2)
						return BindGroupBy(expression.Arguments[0], GetLambda(expression.Arguments[1]), null, null);
					else if (expression.Arguments.Count == 3)
					{
						var lambda1 = GetLambda(expression.Arguments[1]);
						var lambda2 = GetLambda(expression.Arguments[2]);

						if (lambda2.Parameters.Count == 1)
							return BindGroupBy(expression.Arguments[0], lambda1, lambda2, null);
						else if (lambda2.Parameters.Count == 2)
							return BindGroupBy(expression.Arguments[0], lambda1, null, lambda2);
					}
					else if (expression.Arguments.Count == 4)
						return BindGroupBy(expression.Arguments[0], GetLambda(expression.Arguments[1]), GetLambda(expression.Arguments[2]), GetLambda(expression.Arguments[3]));

					break;
				case "Distinct":

					if (expression.Arguments.Count == 1)
						return BindDistinct(expression.Arguments[0]);

					break;
				case "Skip":

					if (expression.Arguments.Count == 2)
						return BindSkip(expression.Arguments[0], expression.Arguments[1]);

					break;
				case "Take":

					if (expression.Arguments.Count == 2)
						return BindTake(expression.Arguments[0], expression.Arguments[1]);

					break;
				case "First":
				case "FirstOrDefault":
				case "Single":
				case "SingleOrDefault":
				case "Last":
				case "LastOrDefault":

					if (expression.Arguments.Count == 1)
						return BindFirst(expression.Arguments[0], null, expression.Method.Name, expression == Expression);
					else if (expression.Arguments.Count == 2)
						return BindFirst(expression.Arguments[0], GetLambda(expression.Arguments[1]), expression.Method.Name, expression == Expression);

					break;
				case "Any":

					if (expression.Arguments.Count == 1)
						return BindAnyAll(expression.Arguments[0], expression.Method, null, expression == Expression);
					else if (expression.Arguments.Count == 2)
						return BindAnyAll(expression.Arguments[0], expression.Method, GetLambda(expression.Arguments[1]), expression == Expression);

					break;
				case "All":
					if (expression.Arguments.Count == 2)
						return BindAnyAll(expression.Arguments[0], expression.Method, GetLambda(expression.Arguments[1]), expression == Expression);
					break;
				case "Contains":
					if (expression.Arguments.Count == 2)
						return BindContains(expression.Arguments[0], expression.Arguments[1], expression == Expression);
					break;
				case "Cast":
					if (expression.Arguments.Count == 1)
						return BindCast(expression.Arguments[0], expression.Method.GetGenericArguments()[0]);
					break;
				case "Reverse":
					return BindReverse(expression.Arguments[0]);
				case "Intersect":
				case "Except":
					if (expression.Arguments.Count == 2)
						return BindIntersect(expression.Arguments[0], expression.Arguments[1], expression.Method.Name == "Except");
					break;
			}
		}

		if (Context.Language.IsAggregate(expression.Method))
		{
			var lambda = expression.Arguments.Count > 1 ? GetLambda(expression.Arguments[1]) : null;
			return BindAggregate(expression.Arguments[0], expression.Method.Name, expression.Method.ReturnType, lambda, expression == Expression);
		}

		return base.VisitMethodCall(expression);
	}

	/// <summary>
	/// Binds aggregate method calls (Sum, Count, Average, etc.) into aggregate
	/// expressions or scalar subqueries depending on the query shape.
	/// </summary>
	private Expression BindAggregate(Expression expression, string aggregateName, Type returnType, LambdaExpression? argument, bool isRoot)
	{
		/*
		 * Aggregate binding overview:
		 * - Detect special shapes (Distinct wrappers, predicate overloads).
		 * - Ensure the source is a ProjectionExpression so we can reference
		 *   projected columns when building the aggregate argument.
		 * - Produce an AggregateExpression node and wrap it in a Select that
		 *   either becomes a scalar subquery or a root-level ProjectionExpression.
		 * - If the aggregate is bound to a GroupBy'd projection, produce a
		 *   correlated AggregateSubquery so the aggregate runs relative to the group.
		 */
		var hasPredicateArg = Context.Language.IsAggregateArgumentPredicate(aggregateName);
		var isDistinct = false;
		var argumentWasPredicate = false;
		var useAlternateArg = false;
		var methodCall = expression as MethodCallExpression;

		if (methodCall is not null && !hasPredicateArg && argument is null)
		{
			if (string.Equals(methodCall.Method.Name, "Distinct", StringComparison.Ordinal) && methodCall.Arguments.Count == 1 &&
				 methodCall.Method.IsInQueryable() && Context.Language.AllowDistinctInAggregates)
			{
				expression = methodCall.Arguments[0];
				isDistinct = true;
			}
		}

		/*
		 * Predicate aggregate handling: rewrite Count(predicate) to Where + Count
		 * so the predicate is applied on the server and subsequent binding sees
		 * the filtered projection.
		 */
		if (argument is not null && hasPredicateArg)
		{
			var enType = expression.Type.GetEnumerableElementType();
			expression = Expression.Call(typeof(Queryable), "Where", enType is null ? null : new[] { enType }, expression, argument);
			argument = null;
			argumentWasPredicate = true;
		}

		/*
		 * Ensure the source is a ProjectionExpression so we can project columns
		 * for use in the aggregate argument.
		 */
		var projection = VisitSequence(expression);
		Expression? argExpr = null;

		if (argument is not null)
		{
			ParameterMapping[argument.Parameters[0]] = projection.Projector;
			argExpr = Visit(argument.Body);
		}
		else if (!hasPredicateArg || useAlternateArg)
			argExpr = projection.Projector;

		var alias = Alias.New();

		ProjectColumns(projection.Projector, alias, projection.Select.Alias);

		var aggExpr = new AggregateExpression(returnType, aggregateName, argExpr, isDistinct);
		var colType = Context.Language.TypeSystem.ResolveColumnType(returnType);
		var select = new SelectExpression(alias, new ColumnDeclaration[] { new ColumnDeclaration(string.Empty, aggExpr, colType) }, projection.Select, null);

		/*
		 * Root-level aggregates must be materialized as a projection with an
		 * attached aggregator lambda so callers receive a scalar rather than a
		 * subquery expression.
		 */
		if (isRoot)
		{
			var p = Expression.Parameter(typeof(IEnumerable<>).MakeGenericType(aggExpr.Type), "p");
			var gator = Expression.Lambda(Expression.Call(typeof(Enumerable), "Single", new Type[] { returnType }, p), p);

			return new ProjectionExpression(select, new ColumnExpression(returnType, Context.Language.TypeSystem.ResolveColumnType(returnType), alias, ""), gator);
		}

		var subquery = new ScalarExpression(returnType, select);

		/*
		 * Group-aware aggregates: if the projection belongs to a GroupBy we must
		 * interpret the aggregate relative to the grouping element and produce a
		 * correlated AggregateSubquery expression referencing the group's alias.
		 */
		if (!argumentWasPredicate && GroupByMap.TryGetValue(projection, out GroupByDescriptor? info))
		{
			if (argument is not null)
			{
				ParameterMapping[argument.Parameters[0]] = info.Element;
				argExpr = Visit(argument.Body);
			}
			else if (!hasPredicateArg || useAlternateArg)
				argExpr = info.Element;

			if (aggExpr is not null)
				aggExpr = new AggregateExpression(returnType, aggregateName, argExpr, isDistinct);

			if (projection == CurrentGroupElement)
				return aggExpr;

			return new AggregateSubqueryExpression(info.Alias, aggExpr, subquery);
		}

		return subquery;
	}

	/// <summary>
	/// Normalize a node that may represent a quoted or constant lambda into a LambdaExpression.
	/// </summary>
	private static LambdaExpression GetLambda(Expression expression)
	{
		/*
		 * Unwrap possible Quote nodes or Constant-based captured delegates so
		 * callers always receive a concrete LambdaExpression instance.
		 */
		while (expression.NodeType == ExpressionType.Quote)
			expression = ((UnaryExpression)expression).Operand;

		if (expression.NodeType == ExpressionType.Constant)
		{
			if (expression is not ConstantExpression constantExpression)
				throw new InvalidCastException(nameof(ConstantExpression));

			if (constantExpression.Value is not LambdaExpression lambdaExpression)
				throw new InvalidCastException(nameof(LambdaExpression));

			return lambdaExpression;
		}

		if (expression is not LambdaExpression lambda)
			throw new InvalidCastException(nameof(LambdaExpression));

		return lambda;
	}

	/// <summary>
	/// Bind a Where operator by mapping the predicate parameter to the projection's projector
	/// and producing a new ProjectionExpression containing the WHERE clause.
	/// </summary>
	private ProjectionExpression BindWhere(Expression source, LambdaExpression predicate)
	{
		/*
		 * Steps performed:
		 * 1) Convert source to a projection using VisitSequence.
		 * 2) Map predicate parameter to the projection's projector so the
		 *    predicate refers to projected columns rather than original parameters.
		 * 3) Visit/translate the predicate body to produce a server-side predicate
		 *    expression. 4) Project the original projector into a new alias and
		 *    attach the translated WHERE expression to the new SelectExpression.
		 */
		var projection = VisitSequence(source);

		/*
		 * Map the lambda parameter from the predicate expression to the projector
		 * from the projection expression. This effectively replaces the parameter
		 * in the predicate with the correct mapping, allowing the predicate to be
		 * evaluated against the projected data.
		 */
		ParameterMapping[predicate.Parameters[0]] = projection.Projector;

		/*
		 * Visit the body of the predicate lambda expression. This process involves
		 * recursively visiting the expression tree of the body, applying necessary
		 * bindings and transformations to produce a predicate expression that can
		 * be understood and executed by the database (e.g., translating it to SQL).
		 */
		var where = Visit(predicate.Body);

		var alias = Alias.New();
		var pc = ProjectColumns(projection.Projector, alias, projection.Select.Alias);

		/*
		 * Create and return a new ProjectionExpression that represents the result
		 * of applying the WHERE clause to the SELECT statement. The new projection
		 * expression will have its own alias and will include the original select
		 * columns along with the translated WHERE condition.
		 */
		return new ProjectionExpression(new SelectExpression(alias, pc.Columns, projection.Select, where), pc.Projector);
	}

	/// <summary>
	/// Ensures the provided expression is represented as a ProjectionExpression.
	/// </summary>
	private ProjectionExpression VisitSequence(Expression source) => ConvertToSequence(Visit(source));

	/// <summary>
	/// Converts various expression shapes into a ProjectionExpression when possible.
	/// </summary>
	private static ProjectionExpression ConvertToSequence(Expression expression)
	{
		/*
		 * Recognize shapes that represent sequences: ProjectionExpression directly,
		 * NewExpression wrapping a grouping projection, or expressions that can be
		 * unwrapped to a NewExpression after removing conversions. Throw when the
		 * provided expression cannot be treated as a sequence.
		 */
		switch (expression.NodeType)
		{
			case (ExpressionType)DatabaseExpressionType.Projection:
				return (ProjectionExpression)expression;
			case ExpressionType.New:
				var nex = (NewExpression)expression;

				if (expression.Type.GetTypeInfo().IsGenericType && expression.Type.GetGenericTypeDefinition() == typeof(Grouping<,>))
					return (ProjectionExpression)nex.Arguments[1];

				goto default;
			case ExpressionType.MemberAccess:

				if (expression.NodeType != ExpressionType.MemberAccess)
					return ConvertToSequence(expression);

				goto default;
			default:

				if (GetNewExpression(expression) is Expression newExpression)
				{
					expression = newExpression;

					goto case ExpressionType.New;
				}

				throw new NotSupportedException($"The expression of type '{expression.Type}' is not a sequence");
		}
	}

	/// <summary>
	/// Unwraps any convert nodes and returns a NewExpression if present.
	/// </summary>
	private static NewExpression? GetNewExpression(Expression expression)
	{
		/*
		 * Remove Convert/ConvertChecked wrappers to reveal an underlying NewExpression
		 * that may indicate grouped projection shapes.
		 */
		while (expression.NodeType == ExpressionType.Convert || expression.NodeType == ExpressionType.ConvertChecked)
			expression = ((UnaryExpression)expression).Operand;

		return expression as NewExpression;
	}

	/// <summary>
	/// Bind a Select operator by translating the selector against the input projection.
	/// </summary>
	private Expression BindSelect(Expression source, LambdaExpression selector)
	{
		/*
		 * Translate selector by mapping its parameter to the source projector,
		 * visiting the selector body so any member accesses are resolved to
		 * projected columns, and projecting the final expression into a fresh alias.
		 */
		var projection = VisitSequence(source);

		ParameterMapping[selector.Parameters[0]] = projection.Projector;

		var expression = Visit(selector.Body);
		var alias = Alias.New();
		var pc = ProjectColumns(expression, alias, projection.Select.Alias);

		return new ProjectionExpression(new SelectExpression(alias, pc.Columns, projection.Select, null), pc.Projector);
	}

	/// <summary>
	/// Helper that calls the column projector to produce projected columns for a projector expression.
	/// </summary>
	private ProjectedColumns ProjectColumns(Expression expression, Alias alias, params Alias[] existingAliases)
	{
		/*
		 * Delegate to ColumnProjector which will scan the expression tree, hoist
		 * subexpressions into column declarations and return the resulting
		 * projector expression together with the list of column declarations.
		 */
		return ColumnProjector.ProjectColumns(Context.Language, expression, null, alias, existingAliases);
	}

	/// <summary>
	/// Bind SelectMany (flatMap) translating collection and result selectors and producing joins.
	/// </summary>
	private Expression BindSelectMany(Expression source, LambdaExpression collectionSelector, LambdaExpression resultSelector)
	{
		/*
		 * SelectMany translation outline:
		 * 1) Bind outer source to a projection and map the collection selector
		 *    parameter to the outer projector.
		 * 2) Bind the collection selector to a sequence (collectionProjection).
		 * 3) Decide join type (CrossJoin for tables, CrossApply/OuterApply for subqueries).
		 * 4) If result selector exists map its parameters and project the result
		 *    expression; otherwise project the collection projector.
		 */
		var projection = VisitSequence(source);

		ParameterMapping[collectionSelector.Parameters[0]] = projection.Projector;

		var collection = collectionSelector.Body;
		var defaultIfEmpty = false;
		var mcs = collection as MethodCallExpression;

		if (mcs is not null && string.Equals(mcs.Method.Name, "DefaultIfEmpty", StringComparison.Ordinal) && mcs.Arguments.Count == 1 && mcs.Method.IsInQueryable())
		{
			collection = mcs.Arguments[0];
			defaultIfEmpty = true;
		}

		var collectionProjection = VisitSequence(collection);
		var isTable = collectionProjection.Select.From is TableExpression;
		var joinType = isTable ? JoinType.CrossJoin : defaultIfEmpty ? JoinType.OuterApply : JoinType.CrossApply;

		if (joinType == JoinType.OuterApply)
			collectionProjection = Context.Language.AddOuterJoinTest(collectionProjection);

		var join = new JoinExpression(joinType, projection.Select, collectionProjection.Select, null);
		var alias = Alias.New();
		ProjectedColumns pc;

		if (resultSelector is null)
			pc = ProjectColumns(collectionProjection.Projector, alias, projection.Select.Alias, collectionProjection.Select.Alias);
		else
		{
			ParameterMapping[resultSelector.Parameters[0]] = projection.Projector;
			ParameterMapping[resultSelector.Parameters[1]] = collectionProjection.Projector;

			var result = Visit(resultSelector.Body);

			pc = ProjectColumns(result, alias, projection.Select.Alias, collectionProjection.Select.Alias);
		}

		return new ProjectionExpression(new SelectExpression(alias, pc.Columns, join, null), pc.Projector);
	}

	/// <summary>
	/// Bind an inner join between two sequences using provided key selectors and result selector.
	/// </summary>
	private Expression BindJoin(Expression outerSource, Expression innerSource, LambdaExpression outerKey, LambdaExpression innerKey, LambdaExpression resultSelector)
	{
		/*
		 * Join binding steps:
		 * - Bind both sources to projections.
		 * - Map key selector parameters to their respective projectors and visit
		 *   key expressions to obtain join condition.
		 * - Map result selector parameters and visit result expression to obtain
		 *   final projector. Construct an InnerJoin and project the result.
		 */
		if (VisitSequence(outerSource) is not ProjectionExpression outerProjection)
			throw new NullReferenceException(nameof(outerProjection));

		if (VisitSequence(innerSource) is not ProjectionExpression innerProjection)
			throw new NullReferenceException(nameof(innerProjection));

		/*
		 * Map the outer key selector parameters to the corresponding projector from the outer projection.
		 * This allows the outer key expression to reference the correct columns in the projected result.
		 */
		ParameterMapping[outerKey.Parameters[0]] = outerProjection.Projector;

		/*
		 * Visit the body of the outer key selector lambda expression.
		 * This will analyze and translate the key expression into a form the
		 * database can understand, typically involving projection columns.
		 */
		var outerKeyExpr = Visit(outerKey.Body);

		/*
		 * Repeat the parameter mapping and body visiting process for the inner key selector.
		 */
		ParameterMapping[innerKey.Parameters[0]] = innerProjection.Projector;

		var innerKeyExpr = Visit(innerKey.Body);

		/*
		 * Map the result selector parameters to the corresponding projectors from
		 * both the outer and inner projections. This ensures the result expression
		 * has access to all necessary columns from both sides of the join.
		 */
		ParameterMapping[resultSelector.Parameters[0]] = outerProjection.Projector;
		ParameterMapping[resultSelector.Parameters[1]] = innerProjection.Projector;

		/*
		 * Visit the result selector body to translate it into a projectable expression.
		 * This expression determines what data will be included in the final result set
		 * after the join is performed.
		 */
		if (Visit(resultSelector.Body) is not Expression resultExpression)
			throw new NullReferenceException(nameof(resultExpression));

		/*
		 * Create and return a new ProjectionExpression that represents the result of the join.
		 * This includes the join condition and the projected result columns.
		 */
		var join = new JoinExpression(JoinType.InnerJoin, outerProjection.Select, innerProjection.Select, outerKeyExpr.Equal(innerKeyExpr));
		var alias = Alias.New();
		var pc = ProjectColumns(resultExpression, alias, outerProjection.Select.Alias, innerProjection.Select.Alias);

		return new ProjectionExpression(new SelectExpression(alias, pc.Columns, join, null), pc.Projector);
	}

	/// <summary>
	/// Bind a GroupJoin by translating it into a grouped SelectMany-like structure.
	/// </summary>
	private Expression BindGroupJoin(MethodInfo groupJoinMethod, Expression outerSource, Expression innerSource, LambdaExpression outerKey,
	 LambdaExpression innerKey, LambdaExpression resultSelector)
	{
		/*
		 * Treat GroupJoin as a form of SelectMany where the inner source is
		 * filtered by the outer key and materialized as a group (IEnumerable)
		 * bound to the result selector's second parameter.
		 */
		var args = groupJoinMethod.GetGenericArguments();
		var outerProjection = VisitSequence(outerSource);

		ParameterMapping[outerKey.Parameters[0]] = outerProjection.Projector;

		/*
		 * Build a predicate that tests inner key equality to outer key. This predicate
		 * is used to filter the inner sequence so that only matching elements are included
		 * in the join result.
		 */
		var predicateLambda = Expression.Lambda(innerKey.Body.Equal(outerKey.Body), innerKey.Parameters[0]);
		var callToWhere = Expression.Call(typeof(Enumerable), "Where", new Type[] { args[1] }, innerSource, predicateLambda);
		var group = Visit(callToWhere);

		ParameterMapping[resultSelector.Parameters[0]] = outerProjection.Projector;

		if (group is not null)
			ParameterMapping[resultSelector.Parameters[1]] = group;

		var resultExpr = Visit(resultSelector.Body);
		var alias = Alias.New();
		var pc = ProjectColumns(resultExpr, alias, outerProjection.Select.Alias);

		return new ProjectionExpression(new SelectExpression(alias, pc.Columns, outerProjection.Select, null), pc.Projector);
	}

	/// <summary>
	/// Bind ordering operations and incorporate any pending ThenBy chains.
	/// </summary>
	private Expression BindOrderBy(Expression source, LambdaExpression orderSelector, OrderType orderType)
	{
		/*
		 * OrderBy binding algorithm:
		 * - Capture pending ThenBy list and clear it.
		 * - Translate the source projection and map the primary ordering.
		 * - Replay ThenBy expressions in reverse capture order to preserve
		 *   the intended stable sort semantics.
		 */
		var myThenBys = ThenBys;

		ThenBys = null;

		var projection = VisitSequence(source);

		ParameterMapping[orderSelector.Parameters[0]] = projection.Projector;

		var orderings = new List<OrderExpression> { new OrderExpression(orderType, Visit(orderSelector.Body)) };

		if (myThenBys is not null)
		{
			for (var i = myThenBys.Count - 1; i >= 0; i--)
			{
				var tb = myThenBys[i];
				var lambda = (LambdaExpression)tb.Expression;

				ParameterMapping[lambda.Parameters[0]] = projection.Projector;

				orderings.Add(new OrderExpression(tb.OrderType, Visit(lambda.Body)));
			}
		}

		var alias = Alias.New();
		var pc = ProjectColumns(projection.Projector, alias, projection.Select.Alias);

		return new ProjectionExpression(new SelectExpression(alias, pc.Columns, projection.Select, null, orderings.AsReadOnly(), null), pc.Projector);
	}

	/// <summary>
	/// Collects ThenBy expressions to be applied to the next OrderBy.
	/// </summary>
	private Expression BindThenBy(Expression source, LambdaExpression orderSelector, OrderType orderType)
	{
		/*
		 * Defer ThenBy handling until the next OrderBy by capturing the ordering
		 * lambda and its direction. This preserves fluent chaining semantics.
		 */
		ThenBys ??= new List<OrderExpression>();

		ThenBys.Add(new OrderExpression(orderType, orderSelector));

		return Visit(source);
	}

	/// <summary>
	/// Bind a GroupBy, build grouping subqueries and register group descriptors for later aggregate binding.
	/// </summary>
	private Expression BindGroupBy(Expression source, LambdaExpression keySelector, LambdaExpression elementSelector, LambdaExpression resultSelector)
	{
		/*
		 * GroupBy binding summary (high level):
		 * - Bind key and element selectors to determine grouped columns.
		 * - Build an element subquery that can produce the group's elements for
		 *   a given key (used by grouping result and aggregates).
		 * - Record GroupByDescriptor linking the element subquery to an alias so
		 *   later aggregate bindings can resolve correlated aggregates.
		 * - If a result selector exists visit it with parameter mappings; otherwise
		 *   produce a default Grouping<TKey,TElement> result.
		 */
		var projection = VisitSequence(source);

		ParameterMapping[keySelector.Parameters[0]] = projection.Projector;

		var keyExpr = Visit(keySelector.Body);
		var elemExpr = projection.Projector;

		if (elementSelector is not null)
		{
			ParameterMapping[elementSelector.Parameters[0]] = projection.Projector;

			elemExpr = Visit(elementSelector.Body);
		}

		var keyProjection = ProjectColumns(keyExpr, projection.Select.Alias, projection.Select.Alias);
		var groupExprs = keyProjection.Columns.Select(c => c.Expression).ToArray();
		var subqueryBasis = VisitSequence(source);

		ParameterMapping[keySelector.Parameters[0]] = subqueryBasis.Projector;

		var subqueryKey = Visit(keySelector.Body);
		var subqueryKeyPC = ProjectColumns(subqueryKey, subqueryBasis.Select.Alias, subqueryBasis.Select.Alias);
		var subqueryGroupExprs = subqueryKeyPC.Columns.Select(c => c.Expression).ToArray();
		var subqueryCorrelation = BuildPredicateWithNullsEqual(subqueryGroupExprs, groupExprs);
		var subqueryElemExpr = subqueryBasis.Projector;

		if (elementSelector is not null)
		{
			ParameterMapping[elementSelector.Parameters[0]] = subqueryBasis.Projector;

			subqueryElemExpr = Visit(elementSelector.Body);
		}

		var elementAlias = Alias.New();
		var elementPC = ProjectColumns(subqueryElemExpr, elementAlias, subqueryBasis.Select.Alias);
		var elementSubquery = new ProjectionExpression(new SelectExpression(elementAlias, elementPC.Columns, subqueryBasis.Select, subqueryCorrelation), elementPC.Projector);
		var alias = Alias.New();
		var info = new GroupByDescriptor(alias, elemExpr);

		GroupByMap.Add(elementSubquery, info);

		Expression resultExpr;

		if (resultSelector is not null)
		{
			var saveGroupElement = CurrentGroupElement;

			CurrentGroupElement = elementSubquery;

			ParameterMapping[resultSelector.Parameters[0]] = keyProjection.Projector;
			ParameterMapping[resultSelector.Parameters[1]] = elementSubquery;

			resultExpr = Visit(resultSelector.Body);

			CurrentGroupElement = saveGroupElement;
		}
		else
		{
			resultExpr = Expression.New(typeof(Grouping<,>).MakeGenericType(keyExpr.Type, subqueryElemExpr.Type).GetTypeInfo().DeclaredConstructors.First(),
				  new Expression[] { keyExpr, elementSubquery });

			resultExpr = Expression.Convert(resultExpr, typeof(IGrouping<,>).MakeGenericType(keyExpr.Type, subqueryElemExpr.Type));
		}

		var pc = ProjectColumns(resultExpr, alias, projection.Select.Alias);

		var newResult = GetNewExpression(pc.Projector);

		if (newResult is not null && newResult.Type.GetTypeInfo().IsGenericType && newResult.Type.GetGenericTypeDefinition() == typeof(Grouping<,>))
		{
			var projectedElementSubquery = newResult.Arguments[1];

			GroupByMap.Add(projectedElementSubquery, info);
		}

		return new ProjectionExpression(new SelectExpression(alias, pc.Columns, projection.Select, null, null, groupExprs), pc.Projector);
	}

	/// <summary>
	/// Bind a Distinct operator by projecting server-side distinct columns.
	/// </summary>
	private Expression BindDistinct(Expression source)
	{
		/*
		 * Project the source projector with server affinity so DISTINCT can be
		 * applied by the database engine using the projected columns.
		 */
		var projection = VisitSequence(source);
		var alias = Alias.New();
		var pc = ColumnProjector.ProjectColumns(Context.Language, ProjectionAffinity.Server, projection.Projector, null, alias, projection.Select.Alias);

		return new ProjectionExpression(new SelectExpression(alias, pc.Columns, projection.Select, null, null, null, true, null, null, false), pc.Projector);
	}

	/// <summary>
	/// Bind a Take (LIMIT) operator.
	/// </summary>
	private Expression BindTake(Expression source, Expression take)
	{
		/*
		 * Attach a translated Take expression to a projected select so the
		 * database can apply a row count limit to the result set.
		 */
		var projection = VisitSequence(source);

		take = Visit(take);

		var alias = Alias.New();
		var pc = ProjectColumns(projection.Projector, alias, projection.Select.Alias);

		return new ProjectionExpression(new SelectExpression(alias, pc.Columns, projection.Select, null, null, null, false, null, take, false), pc.Projector);
	}

	/// <summary>
	/// Bind a Skip (OFFSET) operator.
	/// </summary>
	private Expression BindSkip(Expression source, Expression skip)
	{
		/*
		 * Attach a translated Skip expression to a projected select so the
		 * database can apply an offset to the result set.
		 */
		var projection = VisitSequence(source);

		skip = Visit(skip);

		var alias = Alias.New();
		var pc = ProjectColumns(projection.Projector, alias, projection.Select.Alias);

		return new ProjectionExpression(new SelectExpression(alias, pc.Columns, projection.Select, null, null, null, false, skip, null, false), pc.Projector);
	}

	/// <summary>
	/// Bind First/Single semantics by optionally applying a take/where and
	/// returning either a projection or the root projection with an aggregator.
	/// </summary>
	private Expression BindFirst(Expression source, LambdaExpression predicate, string kind, bool isRoot)
	{
		/*
		 * Behavior overview:
		 * - Translate optional predicate to a WHERE clause by mapping parameter to
		 *   the projector.
		 * - Attach Take(1) for First/Last semantics and mark reversed selection
		 *   for Last when supported.
		 * - If called at root wrap projection with an aggregator lambda (First/Single)
		 *   to materialize scalar result.
		 */
		var projection = VisitSequence(source);
		Expression? where = null;

		if (predicate is not null)
		{
			ParameterMapping[predicate.Parameters[0]] = projection.Projector;
			where = Visit(predicate.Body);
		}

		var isFirst = kind.StartsWith("First");
		var isLast = kind.StartsWith("Last");
		var take = isFirst || isLast ? Expression.Constant(1) : null;

		if (take is not null || where is not null)
		{
			var alias = Alias.New();
			var pc = ProjectColumns(projection.Projector, alias, projection.Select.Alias);
			projection = new ProjectionExpression(new SelectExpression(alias, pc.Columns, projection.Select, where, null, null, false, null, take, isLast), pc.Projector);
		}

		if (isRoot)
		{
			var elementType = projection.Projector.Type;
			var p = Expression.Parameter(typeof(IEnumerable<>).MakeGenericType(elementType), "p");
			var gator = Expression.Lambda(Expression.Call(typeof(Enumerable), kind, new Type[] { elementType }, p), p);

			return new ProjectionExpression(projection.Select, projection.Projector, gator);
		}

		return projection;
	}

	/// <summary>
	/// Bind Any/All semantics by converting to Exists or a count-based expression depending on environment.
	/// </summary>
	private Expression BindAnyAll(Expression source, MethodInfo method, LambdaExpression predicate, bool isRoot)
	{
		/*
		 * Implementation previously documented; preserved as-is.
		 */
		var isAll = string.Equals(method.Name, "All", StringComparison.Ordinal);
		var constSource = source as ConstantExpression;

		/*
		 * Handle the simple case where the source is an in-memory collection (constant) and
		 * not an IQueryable. In that situation we can evaluate the predicate for each
		 * element locally and produce a boolean expression (no server translation required).
		 *
		 * Example: collection.All(x => x > 0)  ->  combine predicates with AND for All
		 *          collection.Any(x => x > 0)  ->  combine predicates with OR for Any
		 */
		if (constSource is not null && !IsQuery(constSource))
		{
			System.Diagnostics.Debug.Assert(!isRoot);
			Expression where = null;

			// For each concrete value in the collection, build an invocation of the predicate
			// with the value as the parameter, then combine these invocations using AND/OR.
			foreach (object value in (IEnumerable)constSource.Value)
			{
				// Invoke the predicate against the constant value: predicate(value)
				var expr = Expression.Invoke(predicate, Expression.Constant(value, predicate.Parameters[0].Type));

				if (where is null)
					where = expr; // first element sets the initial expression
				else if (isAll)
					where = where.And(expr); // All -> conjunction of all invocations
				else
					where = where.Or(expr); // Any -> disjunction
			}

			// Visit the combined expression so it is reduced/translated by the binder
			return Visit(where);
		}

		/*
		 * For remote/queryable sources we need to translate All/Any into server-side
		 * constructs. There are two common strategies:
		 * 1) Use EXISTS/NOT EXISTS semantics (preferred when the result is used as a subquery)
		 * 2) Use a COUNT aggregate and compare to zero (used when subqueries in select are not allowed)
		 */
		else
		{
			if (isAll)
				predicate = Expression.Lambda(Expression.Not(predicate.Body), predicate.Parameters.ToArray());

			if (predicate is not null)
				source = Expression.Call(typeof(Enumerable), "Where", method.GetGenericArguments(), source, predicate);

			var projection = VisitSequence(source);
			Expression result = new ExistsExpression(projection.Select);

			if (isAll)
				result = Expression.Not(result);

			if (isRoot)
			{
				if (Context.Language.AllowSubqueryInSelectWithoutFrom)
					return GetSingletonSequence(result, "SingleOrDefault");
				else
				{
					var colType = Context.Language.TypeSystem.ResolveColumnType(typeof(int));
					var newSelect = projection.Select.SetColumns(new[] { new ColumnDeclaration("value", new AggregateExpression(typeof(int), "Count", null, false), colType) });
					var colx = new ColumnExpression(typeof(int), colType, newSelect.Alias, "value");
					var exp = isAll ? colx.Equal(Expression.Constant(0)) : colx.GreaterThan(Expression.Constant(0));

					return new ProjectionExpression(newSelect, exp, Aggregator.GetAggregator(typeof(bool), typeof(IEnumerable<bool>)));
				}
			}

			return result;
		}
	}

	/// <summary>
	/// Bind a Contains call. Handles constant collections and subqueries.
	/// </summary>
	private Expression BindContains(Expression source, Expression match, bool isRoot)
	{
		/*
		 * Handle three cases:
		 * - constant in-memory collection: produce an InExpression with literals
		 * - root+no-subquery-in-select: rewrite as Any() predicate and re-bind
		 * - otherwise produce an InExpression referencing a subquery SelectExpression
		 */
		var constSource = source as ConstantExpression;

		if (constSource is not null && !IsQuery(constSource))
		{
			System.Diagnostics.Debug.Assert(!isRoot);
			var values = new List<Expression>();

			foreach (object value in (IEnumerable)constSource.Value)
				values.Add(Expression.Constant(Types.Convert(value, match.Type), match.Type));

			match = Visit(match);

			return new InExpression(match, values);
		}
		else if (isRoot && !Context.Language.AllowSubqueryInSelectWithoutFrom)
		{
			var p = Expression.Parameter(Enumerables.GetEnumerableElementType(source.Type), "x");
			var predicate = Expression.Lambda(p.Equal(match), p);
			var exp = Expression.Call(typeof(Queryable), "Any", new Type[] { p.Type }, source, predicate);

			Expression = exp;

			return Visit(exp);
		}
		else
		{
			var projection = VisitSequence(source);

			match = Visit(match);

			var result = new InExpression(match, projection.Select);

			if (isRoot)
				return GetSingletonSequence(result, "SingleOrDefault");

			return result;
		}
	}

	/// <summary>
	/// Validate and bind a Cast operation between element types.
	/// </summary>
	private Expression BindCast(Expression source, Type targetElementType)
	{
		/*
		 * Ensure the element type of the projected source is assignable to the
		 * requested target element type. The binder enforces CLR assignability
		 * rather than performing any runtime conversion.
		 */
		var projection = VisitSequence(source);
		var elementType = GetTrueUnderlyingType(projection.Projector);

		if (!targetElementType.IsAssignableFrom(elementType))
			throw new InvalidOperationException($"Cannot cast elements from type '{elementType}' to type '{targetElementType}'");

		return projection;
	}

	/// <summary>
	/// Bind an Intersect/Except operation by producing an EXISTS/NOT EXISTS predicate.
	/// </summary>
	private Expression BindIntersect(Expression outerSource, Expression innerSource, bool negate)
	{
		/*
		 * Translate set operations to EXISTS expressions that compare the inner
		 * projection to the outer projection. Negate the EXISTS for Except.
		 */
		var outerProjection = VisitSequence(outerSource);
		var innerProjection = VisitSequence(innerSource);

		Expression exists = new ExistsExpression(new SelectExpression(Alias.New(), null, innerProjection.Select, innerProjection.Projector.Equal(outerProjection.Projector)));

		if (negate)
			exists = Expression.Not(exists);

		var alias = Alias.New();
		var pc = ProjectColumns(outerProjection.Projector, alias, outerProjection.Select.Alias);

		return new ProjectionExpression(new SelectExpression(alias, pc.Columns, outerProjection.Select, exists), pc.Projector, outerProjection.Aggregator);
	}

	/// <summary>
	/// Bind Reverse by setting the reverse flag on the select.
	/// </summary>
	private Expression BindReverse(Expression expression)
	{
		/*
		 * Project the source and mark the produced SelectExpression as reversed
		 * so downstream translators know to invert ordering semantics when
		 * generating SQL/target language code.
		 */
		var projection = VisitSequence(expression);
		var alias = Alias.New();
		var pc = ProjectColumns(projection.Projector, alias, projection.Select.Alias);

		return new ProjectionExpression(new SelectExpression(alias, pc.Columns, projection.Select, null).SetReverse(true), pc.Projector);
	}

	/// <summary>
	/// Builds a predicate that compares two sequences of expressions treating nulls as equal.
	/// </summary>
	private static Expression? BuildPredicateWithNullsEqual(IEnumerable<Expression> source1, IEnumerable<Expression> source2)
	{
		/*
		 * Build a conjunction of equality comparisons for two expression lists
		 * treating pairs as equal when both are NULL or when their values are equal.
		 * This accommodates SQL's three-valued logic and NULL semantics.
		 */
		var en1 = source1.GetEnumerator();
		var en2 = source2.GetEnumerator();
		Expression? result = null;

		while (en1.MoveNext() && en2.MoveNext())
		{
			var compare = Expression.Or(new IsNullExpression(en1.Current).And(new IsNullExpression(en2.Current)), en1.Current.Equal(en2.Current));

			result = result is null ? compare : result.And(compare);
		}

		return result;
	}

	/// <summary>
	/// Returns true when the provided expression represents an IQueryable source.
	/// </summary>
	private static bool IsQuery(Expression expression)
	{
		/*
		 * Determine whether the expression Type implements IQueryable<T> for
		 * some element type; used to distinguish remote query sources from
		 * in-memory collections (constants).
		 */
		var elementType = Enumerables.GetEnumerableElementType(expression.Type);

		return elementType is not null && typeof(IQueryable<>).MakeGenericType(elementType).IsAssignableFrom(expression.Type);
	}

	/// <summary>
	/// Create a singleton projection around an expression using the provided aggregator.
	/// </summary>
	private Expression GetSingletonSequence(Expression expr, string aggregator)
	{
		/*
		 * Wrap the expression in a one-column SelectExpression and optionally
		 * attach an aggregator lambda (for example Single/SingleOrDefault) so
		 * the caller can materialize a scalar from the singleton projection.
		 */
		var p = Expression.Parameter(typeof(IEnumerable<>).MakeGenericType(expr.Type), "p");
		LambdaExpression gator = null;

		if (aggregator is not null)
			gator = Expression.Lambda(Expression.Call(typeof(Enumerable), aggregator, new Type[] { expr.Type }, p), p);

		var alias = Alias.New();
		var colType = Context.Language.TypeSystem.ResolveColumnType(expr.Type);
		var select = new SelectExpression(alias, new[] { new ColumnDeclaration("value", expr, colType) }, null, null);

		return new ProjectionExpression(select, new ColumnExpression(expr.Type, colType, alias, "value"), gator);
	}

	/// <summary>
	/// Strip Convert nodes to determine the true underlying CLR type of an expression.
	/// </summary>
	private static Type GetTrueUnderlyingType(Expression expression)
	{
		/*
		 * Remove any Convert wrapper to obtain the underlying expression whose
		 * Type reflects the true CLR element type being projected.
		 */
		while (expression.NodeType == ExpressionType.Convert)
			expression = ((UnaryExpression)expression).Operand;

		return expression.Type;
	}

	/// <summary>
	/// Compares member signatures (method vs property accessor) to determine match.
	/// </summary>
	private static bool MembersMatch(MemberInfo a, MemberInfo b)
	{
		/*
		 * Two members match when they share the same name. Also handle the case
		 * where a property accessor appears as a MethodInfo in one place and as
		 * a PropertyInfo in another.
		 */
		if (a.Name == b.Name)
			return true;

		if (a is MethodInfo && b is PropertyInfo info)
			return a.Name == info.GetMethod.Name;
		else if (a is PropertyInfo info1 && b is MethodInfo)
			return info1.GetMethod.Name == b.Name;

		return false;
	}

	/// <summary>
	/// Extracts a value from a captured closure (field or property) for constant expressions.
	/// </summary>
	private static object? GetValue(object instance, MemberInfo member)
	{
		/*
		 * Read the value of a captured field or property from the closure object
		 * used by the expression tree and return it for constant projection.
		 */
		var fi = member as FieldInfo;

		if (fi is not null)
			return fi.GetValue(instance);

		var pi = member as PropertyInfo;

		if (pi is not null)
			return pi.GetValue(instance, null);

		return null;
	}

	/// <summary>
	/// Returns the default value for a CLR type (null for nullable/reference types).
	/// </summary>
	private static object? GetDefault(Type type)
	{
		/*
		 * Return null for reference or nullable types, otherwise construct a
		 * default value for the value type using Activator.CreateInstance.
		 */
		if (!type.GetTypeInfo().IsValueType || type.IsNullable())
			return null;
		else
			return Activator.CreateInstance(type);
	}

	/// <summary>
	/// Visit a constant expression; if it represents an IQueryable, inline its mapping.
	/// </summary>
	protected override Expression VisitConstant(ConstantExpression expression)
	{
		/*
		 * If the constant is an IQueryable produced by a mapping, inline the
		 * mapper's expression so the binder operates on the mapped query shape.
		 * Otherwise partially evaluate the contained expression and visit it.
		 */
		if (IsQuery(expression))
		{
			var q = (IQueryable)expression.Value;

			if (q.Expression.NodeType == ExpressionType.Constant)
			{
				var mapping = MappingsCache.Get(q.ElementType);

				return VisitSequence(mapping.CreateExpression(Context));
			}
			else
			{
				var pev = PartialEvaluator.Eval(Context, q.Expression);

				return Visit(pev);
			}
		}

		return expression;
	}

	/// <summary>
	/// Visit a parameter expression; replace it with a mapped expression when available.
	/// </summary>
	protected override Expression VisitParameter(ParameterExpression expression)
	{
		/*
		 * Replace lambda parameters with the mapped projector expressions when
		 * an entry exists in ParameterMapping. This is the mechanism by which
		 * inner lambdas are evaluated against projected rows.
		 */
		if (ParameterMapping.TryGetValue(expression, out Expression? e))
			return e;

		return expression;
	}

	/// <summary>
	/// Visit an invocation expression where the invoked expression is a lambda; map parameters to arguments.
	/// </summary>
	protected override Expression VisitInvocation(InvocationExpression expression)
	{
		/*
		 * Inline invocation of captured lambda delegates by mapping the lambda
		 * parameters to the invocation arguments and visiting the body in the
		 * current parameter mapping context.
		 */
		if (expression.Expression is LambdaExpression lambda)
		{
			for (var i = 0; i < lambda.Parameters.Count; i++)
				ParameterMapping[lambda.Parameters[i]] = expression.Arguments[i];

			return Visit(lambda.Body);
		}

		return base.VisitInvocation(expression);
	}

	/// <summary>
	/// Visit a member access; if the member pertains to a remote query aggregate, bind accordingly.
	/// </summary>
	protected override Expression VisitMemberAccess(MemberExpression expression)
	{
		/*
		 * Attempt to resolve member accesses that target query sources or remote
		 * aggregates. If the member corresponds to a known aggregate and the
		 * source is remote, bind it using BindAggregate; otherwise delegate to
		 * the static Bind helper which maps members to columns/projections.
		 */
		if (expression.Expression is not null
			 && expression.Expression.NodeType == ExpressionType.Parameter
			 && !ParameterMapping.ContainsKey((ParameterExpression)expression.Expression)
			 && IsQuery(expression))
		{
			/*
			 * Original implementation had a commented-out mapping hook here for
			 * entity mapping resolution. Preserve that intent as a block comment.
			 */
			/*
			var mapping = MappingsCache.Get();


			  return this.VisitSequence(MappingsCache.Mapper.GetQueryExpression(Mapper.Mapping.GetMapping(expression.Member)));
			*/
		}

		var source = Visit(expression.Expression);

		if (Context.Language.IsAggregate(expression.Member) && IsRemoteQuery(source))
			return BindAggregate(expression.Expression, expression.Member.Name, Members.GetMemberType(expression.Member), null, expression == Expression);

		var result = Bind(source, expression.Member);
		var mex = result as MemberExpression;

		if (mex is not null && mex.Member == expression.Member && mex.Expression == expression.Expression)
			return expression;

		return result;
	}

	/// <summary>
	/// Determines whether the provided expression represents a remote (database) query
	/// or contains nodes that originate from the database expression model.
	/// </remarks>
	private bool IsRemoteQuery(Expression expression)
	{
		/*
		 * Recursively determine if the expression is or contains a database
		 * expression node. Inspect member access inner expression and method call
		 * source argument to locate the remote query source.
		 */
		if (expression.NodeType.IsDatabaseExpression())
			return true;

		switch (expression.NodeType)
		{
			case ExpressionType.MemberAccess:
				return IsRemoteQuery(((MemberExpression)expression).Expression);
			case ExpressionType.Call:
				var mc = (MethodCallExpression)expression;

				if (mc.Object is not null)
					return IsRemoteQuery(mc.Object);
				else if (mc.Arguments.Count > 0)
					return IsRemoteQuery(mc.Arguments[0]);
				break;
		}

		return false;
	}
}
