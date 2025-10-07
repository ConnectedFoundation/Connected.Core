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

public sealed class Binder : DatabaseVisitor
{
	private Binder(ExpressionCompilationContext context, Expression expression)
	{
		Context = context;
		Expression = expression;

		ParameterMapping = new();
		GroupByMap = new();
	}

	private ExpressionCompilationContext Context { get; }
	private Dictionary<ParameterExpression, Expression> ParameterMapping { get; }
	private Dictionary<Expression, GroupByDescriptor> GroupByMap { get; }
	private List<OrderExpression>? ThenBys { get; set; }
	private Expression CurrentGroupElement { get; set; }
	private Expression Expression { get; set; }
	public static Expression? Bind(ExpressionCompilationContext context, Expression expression)
	{
		return new Binder(context, expression).Visit(expression);
	}

	public static Expression Bind(Expression source, MemberInfo member)
	{
		switch (source.NodeType)
		{
			case (ExpressionType)DatabaseExpressionType.Entity:
				var ex = (EntityExpression)source;
				var result = Bind(ex.Expression, member);
				var mex = result as MemberExpression;

				if (mex is not null && mex.Expression == ex.Expression && mex.Member == member)
					return Expression.MakeMemberAccess(source, member);

				return result;
			case ExpressionType.Convert:
				var ux = (UnaryExpression)source;

				return Bind(ux.Operand, member);
			case ExpressionType.MemberInit:
				var min = (MemberInitExpression)source;

				for (var i = 0; i < min.Bindings.Count; i++)
				{
					var assign = min.Bindings[i] as MemberAssignment;

					if (assign is not null && MembersMatch(assign.Member, member))
						return assign.Expression;
				}

				break;
			case ExpressionType.New:
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
				var proj = (ProjectionExpression)source;
				var newProjector = Bind(proj.Projector, member);
				var mt = Members.GetMemberType(member);

				return new ProjectionExpression(proj.Select, newProjector, Aggregator.GetAggregator(mt, typeof(IEnumerable<>).MakeGenericType(mt)));

			case (ExpressionType)DatabaseExpressionType.OuterJoined:
				var oj = (OuterJoinedExpression)source;
				var em = Bind(oj.Expression, member);

				if (em is ColumnExpression)
					return em;

				return new OuterJoinedExpression(oj.Test, em);
			case ExpressionType.Conditional:
				var cex = (ConditionalExpression)source;

				return Expression.Condition(cex.Test, Bind(cex.IfTrue, member), Bind(cex.IfFalse, member));
			case ExpressionType.Constant:
				var con = (ConstantExpression)source;
				var memberType = Members.GetMemberType(member);

				if (con.Value is null)
					return Expression.Constant(GetDefault(memberType), memberType);
				else
					return Expression.Constant(GetValue(con.Value, member), memberType);
		}

		return Expression.MakeMemberAccess(source, member);
	}

	protected override Expression? VisitMethodCall(MethodCallExpression expression)
	{
		if (expression.Method.IsInQueryable())
		{
			switch (expression.Method.Name)
			{
				case "Where":
					return BindWhere(expression.Arguments[0], GetLambda(expression.Arguments[1]));
				case "Select":
					return BindSelect(expression.Arguments[0], GetLambda(expression.Arguments[1]));
				case "SelectMany":

					if (expression.Arguments.Count == 2)
						return BindSelectMany(expression.Arguments[0], GetLambda(expression.Arguments[1]), null);
					else if (expression.Arguments.Count == 3)
						return BindSelectMany(expression.Arguments[0], GetLambda(expression.Arguments[1]), GetLambda(expression.Arguments[2]));

					break;
				case "Join":

					return BindJoin(expression.Arguments[0], expression.Arguments[1], GetLambda(expression.Arguments[2]),
						 GetLambda(expression.Arguments[3]), GetLambda(expression.Arguments[4]));

				case "GroupJoin":

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

	private Expression BindAggregate(Expression expression, string aggregateName, Type returnType, LambdaExpression? argument, bool isRoot)
	{
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

		if (argument is not null && hasPredicateArg)
		{
			var enType = expression.Type.GetEnumerableElementType();
			expression = Expression.Call(typeof(Queryable), "Where", enType is null ? null : new[] { enType }, expression, argument);
			argument = null;
			argumentWasPredicate = true;
		}

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

		if (isRoot)
		{
			var p = Expression.Parameter(typeof(IEnumerable<>).MakeGenericType(aggExpr.Type), "p");
			var gator = Expression.Lambda(Expression.Call(typeof(Enumerable), "Single", new Type[] { returnType }, p), p);

			return new ProjectionExpression(select, new ColumnExpression(returnType, Context.Language.TypeSystem.ResolveColumnType(returnType), alias, ""), gator);
		}

		var subquery = new ScalarExpression(returnType, select);

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

	private static LambdaExpression GetLambda(Expression expression)
	{
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

	private Expression BindWhere(Expression source, LambdaExpression predicate)
	{
		var projection = VisitSequence(source);

		ParameterMapping[predicate.Parameters[0]] = projection.Projector;

		var where = Visit(predicate.Body);
		var alias = Alias.New();
		var pc = ProjectColumns(projection.Projector, alias, projection.Select.Alias);

		return new ProjectionExpression(new SelectExpression(alias, pc.Columns, projection.Select, where), pc.Projector);
	}

	private ProjectionExpression VisitSequence(Expression source) => ConvertToSequence(Visit(source));

	private static ProjectionExpression ConvertToSequence(Expression expression)
	{
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

	private static NewExpression? GetNewExpression(Expression expression)
	{
		while (expression.NodeType == ExpressionType.Convert || expression.NodeType == ExpressionType.ConvertChecked)
			expression = ((UnaryExpression)expression).Operand;

		return expression as NewExpression;
	}

	private Expression BindSelect(Expression source, LambdaExpression selector)
	{
		var projection = VisitSequence(source);

		ParameterMapping[selector.Parameters[0]] = projection.Projector;

		var expression = Visit(selector.Body);
		var alias = Alias.New();
		var pc = ProjectColumns(expression, alias, projection.Select.Alias);

		return new ProjectionExpression(new SelectExpression(alias, pc.Columns, projection.Select, null), pc.Projector);
	}

	private ProjectedColumns ProjectColumns(Expression expression, Alias alias, params Alias[] existingAliases)
	{
		return ColumnProjector.ProjectColumns(Context.Language, expression, null, alias, existingAliases);
	}

	private Expression BindSelectMany(Expression source, LambdaExpression collectionSelector, LambdaExpression resultSelector)
	{
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

	private Expression BindJoin(Expression outerSource, Expression innerSource, LambdaExpression outerKey, LambdaExpression innerKey, LambdaExpression resultSelector)
	{
		if (VisitSequence(outerSource) is not ProjectionExpression outerProjection)
			throw new NullReferenceException(nameof(outerProjection));

		if (VisitSequence(innerSource) is not ProjectionExpression innerProjection)
			throw new NullReferenceException(nameof(innerProjection));

		ParameterMapping[outerKey.Parameters[0]] = outerProjection.Projector;

		var outerKeyExpr = Visit(outerKey.Body);

		ParameterMapping[innerKey.Parameters[0]] = innerProjection.Projector;

		var innerKeyExpr = Visit(innerKey.Body);

		ParameterMapping[resultSelector.Parameters[0]] = outerProjection.Projector;
		ParameterMapping[resultSelector.Parameters[1]] = innerProjection.Projector;

		if (Visit(resultSelector.Body) is not Expression resultExpression)
			throw new NullReferenceException(nameof(resultExpression));

		var join = new JoinExpression(JoinType.InnerJoin, outerProjection.Select, innerProjection.Select, outerKeyExpr.Equal(innerKeyExpr));
		var alias = Alias.New();
		var pc = ProjectColumns(resultExpression, alias, outerProjection.Select.Alias, innerProjection.Select.Alias);

		return new ProjectionExpression(new SelectExpression(alias, pc.Columns, join, null), pc.Projector);
	}

	private Expression BindGroupJoin(MethodInfo groupJoinMethod, Expression outerSource, Expression innerSource, LambdaExpression outerKey,
		 LambdaExpression innerKey, LambdaExpression resultSelector)
	{
		/*
	  * A database will treat this no differently than a SelectMany w/ result selector, so just use that translation instead
	  */
		var args = groupJoinMethod.GetGenericArguments();
		var outerProjection = VisitSequence(outerSource);

		ParameterMapping[outerKey.Parameters[0]] = outerProjection.Projector;

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

	private Expression BindOrderBy(Expression source, LambdaExpression orderSelector, OrderType orderType)
	{
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

	private Expression BindThenBy(Expression source, LambdaExpression orderSelector, OrderType orderType)
	{
		ThenBys ??= new List<OrderExpression>();

		ThenBys.Add(new OrderExpression(orderType, orderSelector));

		return Visit(source);
	}

	private Expression BindGroupBy(Expression source, LambdaExpression keySelector, LambdaExpression elementSelector, LambdaExpression resultSelector)
	{
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

	private Expression BindDistinct(Expression source)
	{
		var projection = VisitSequence(source);
		var alias = Alias.New();
		var pc = ColumnProjector.ProjectColumns(Context.Language, ProjectionAffinity.Server, projection.Projector, null, alias, projection.Select.Alias);

		return new ProjectionExpression(new SelectExpression(alias, pc.Columns, projection.Select, null, null, null, true, null, null, false), pc.Projector);
	}

	private Expression BindTake(Expression source, Expression take)
	{
		var projection = VisitSequence(source);

		take = Visit(take);

		var alias = Alias.New();
		var pc = ProjectColumns(projection.Projector, alias, projection.Select.Alias);

		return new ProjectionExpression(new SelectExpression(alias, pc.Columns, projection.Select, null, null, null, false, null, take, false), pc.Projector);
	}

	private Expression BindSkip(Expression source, Expression skip)
	{
		var projection = VisitSequence(source);

		skip = Visit(skip);

		var alias = Alias.New();
		var pc = ProjectColumns(projection.Projector, alias, projection.Select.Alias);

		return new ProjectionExpression(new SelectExpression(alias, pc.Columns, projection.Select, null, null, null, false, skip, null, false), pc.Projector);
	}

	private Expression BindFirst(Expression source, LambdaExpression predicate, string kind, bool isRoot)
	{
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

	private Expression BindAnyAll(Expression source, MethodInfo method, LambdaExpression predicate, bool isRoot)
	{
		var isAll = string.Equals(method.Name, "All", StringComparison.Ordinal);
		var constSource = source as ConstantExpression;

		if (constSource is not null && !IsQuery(constSource))
		{
			System.Diagnostics.Debug.Assert(!isRoot);
			Expression where = null;

			foreach (object value in (IEnumerable)constSource.Value)
			{
				var expr = Expression.Invoke(predicate, Expression.Constant(value, predicate.Parameters[0].Type));

				if (where is null)
					where = expr;
				else if (isAll)
					where = where.And(expr);
				else
					where = where.Or(expr);
			}

			return Visit(where);
		}
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

	private Expression BindContains(Expression source, Expression match, bool isRoot)
	{
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

	private Expression BindCast(Expression source, Type targetElementType)
	{
		var projection = VisitSequence(source);
		var elementType = GetTrueUnderlyingType(projection.Projector);

		if (!targetElementType.IsAssignableFrom(elementType))
			throw new InvalidOperationException($"Cannot cast elements from type '{elementType}' to type '{targetElementType}'");

		return projection;
	}

	private Expression BindIntersect(Expression outerSource, Expression innerSource, bool negate)
	{
		/*
	  * SELECT * FROM outer WHERE EXISTS(SELECT * FROM inner WHERE inner = outer))
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

	private Expression BindReverse(Expression expression)
	{
		var projection = VisitSequence(expression);
		var alias = Alias.New();
		var pc = ProjectColumns(projection.Projector, alias, projection.Select.Alias);

		return new ProjectionExpression(new SelectExpression(alias, pc.Columns, projection.Select, null).SetReverse(true), pc.Projector);
	}

	private static Expression? BuildPredicateWithNullsEqual(IEnumerable<Expression> source1, IEnumerable<Expression> source2)
	{
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

	private static bool IsQuery(Expression expression)
	{
		var elementType = Enumerables.GetEnumerableElementType(expression.Type);

		return elementType is not null && typeof(IQueryable<>).MakeGenericType(elementType).IsAssignableFrom(expression.Type);
	}

	private Expression GetSingletonSequence(Expression expr, string aggregator)
	{
		var p = Expression.Parameter(typeof(IEnumerable<>).MakeGenericType(expr.Type), "p");
		LambdaExpression gator = null;

		if (aggregator is not null)
			gator = Expression.Lambda(Expression.Call(typeof(Enumerable), aggregator, new Type[] { expr.Type }, p), p);

		var alias = Alias.New();
		var colType = Context.Language.TypeSystem.ResolveColumnType(expr.Type);
		var select = new SelectExpression(alias, new[] { new ColumnDeclaration("value", expr, colType) }, null, null);

		return new ProjectionExpression(select, new ColumnExpression(expr.Type, colType, alias, "value"), gator);
	}

	private static Type GetTrueUnderlyingType(Expression expression)
	{
		while (expression.NodeType == ExpressionType.Convert)
			expression = ((UnaryExpression)expression).Operand;

		return expression.Type;
	}

	private static bool MembersMatch(MemberInfo a, MemberInfo b)
	{
		if (a.Name == b.Name)
			return true;

		if (a is MethodInfo && b is PropertyInfo info)
			return a.Name == info.GetMethod.Name;
		else if (a is PropertyInfo info1 && b is MethodInfo)
			return info1.GetMethod.Name == b.Name;

		return false;
	}

	private static object? GetValue(object instance, MemberInfo member)
	{
		var fi = member as FieldInfo;

		if (fi is not null)
			return fi.GetValue(instance);

		var pi = member as PropertyInfo;

		if (pi is not null)
			return pi.GetValue(instance, null);

		return null;
	}

	private static object? GetDefault(Type type)
	{
		if (!type.GetTypeInfo().IsValueType || type.IsNullable())
			return null;
		else
			return Activator.CreateInstance(type);
	}

	protected override Expression VisitConstant(ConstantExpression expression)
	{
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

	protected override Expression VisitParameter(ParameterExpression expression)
	{
		if (ParameterMapping.TryGetValue(expression, out Expression? e))
			return e;

		return expression;
	}

	protected override Expression VisitInvocation(InvocationExpression expression)
	{
		if (expression.Expression is LambdaExpression lambda)
		{
			for (var i = 0; i < lambda.Parameters.Count; i++)
				ParameterMapping[lambda.Parameters[i]] = expression.Arguments[i];

			return Visit(lambda.Body);
		}

		return base.VisitInvocation(expression);
	}

	protected override Expression VisitMemberAccess(MemberExpression expression)
	{
		if (expression.Expression is not null
			 && expression.Expression.NodeType == ExpressionType.Parameter
			 && !ParameterMapping.ContainsKey((ParameterExpression)expression.Expression)
			 && IsQuery(expression))
		{
			//var mapping = MappingsCache.Get();


			//  return this.VisitSequence(MappingsCache.Mapper.GetQueryExpression(Mapper.Mapping.GetMapping(expression.Member)));
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

	private bool IsRemoteQuery(Expression expression)
	{
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
