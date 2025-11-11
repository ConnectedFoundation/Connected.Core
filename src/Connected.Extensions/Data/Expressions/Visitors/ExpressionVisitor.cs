using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;

namespace Connected.Data.Expressions.Visitors;

public abstract class ExpressionVisitor : IDisposable
{
	/// <summary>
	/// Indicates whether the visitor has been disposed.
	/// </summary>
	protected bool IsDisposed { get; private set; }

	/// <summary>
	/// Dispatches an expression to the corresponding visit method based on its node type.
	/// </summary>
	/// <param name="expression">The expression to visit.</param>
	/// <returns>The visited (possibly updated) expression.</returns>
	/// <exception cref="NullReferenceException">Thrown when <paramref name="expression"/> is null.</exception>
	protected virtual Expression Visit(Expression expression)
	{
		/*
		 * Central dispatcher: analyzes NodeType and delegates to the specific
		 * Visit* method for that expression kind. Each branch returns the visited
		 * (possibly rewritten) expression; unknown nodes go to VisitUnknown.
		 */
		if (expression is null)
			throw new NullReferenceException("Expected expression.");

		return expression.NodeType switch
		{
			ExpressionType.Negate or ExpressionType.NegateChecked or ExpressionType.Not or ExpressionType.Convert or ExpressionType.ConvertChecked
				 or ExpressionType.ArrayLength or ExpressionType.Quote or ExpressionType.TypeAs or ExpressionType.UnaryPlus => VisitUnary((UnaryExpression)expression),
			ExpressionType.Add or ExpressionType.AddChecked or ExpressionType.Subtract or ExpressionType.SubtractChecked or ExpressionType.Multiply
				 or ExpressionType.MultiplyChecked or ExpressionType.Divide or ExpressionType.Modulo or ExpressionType.And or ExpressionType.AndAlso
				 or ExpressionType.Or or ExpressionType.OrElse or ExpressionType.LessThan or ExpressionType.LessThanOrEqual or ExpressionType.GreaterThan
				 or ExpressionType.GreaterThanOrEqual or ExpressionType.Equal or ExpressionType.NotEqual or ExpressionType.Coalesce or ExpressionType.ArrayIndex
				 or ExpressionType.RightShift or ExpressionType.LeftShift or ExpressionType.ExclusiveOr or ExpressionType.Power => VisitBinary((BinaryExpression)expression),
			ExpressionType.TypeIs => VisitTypeIs((TypeBinaryExpression)expression),
			ExpressionType.Conditional => VisitConditional((ConditionalExpression)expression),
			ExpressionType.Constant => VisitConstant((ConstantExpression)expression),
			ExpressionType.Parameter => VisitParameter((ParameterExpression)expression),
			ExpressionType.MemberAccess => VisitMemberAccess((MemberExpression)expression),
			ExpressionType.Call => VisitMethodCall((MethodCallExpression)expression),
			ExpressionType.Lambda => VisitLambda((LambdaExpression)expression),
			ExpressionType.New => VisitNew((NewExpression)expression),
			ExpressionType.NewArrayInit or ExpressionType.NewArrayBounds => VisitNewArray((NewArrayExpression)expression),
			ExpressionType.Invoke => VisitInvocation((InvocationExpression)expression),
			ExpressionType.MemberInit => VisitMemberInit((MemberInitExpression)expression),
			ExpressionType.ListInit => VisitListInit((ListInitExpression)expression),
			_ => VisitUnknown(expression),
		};
	}

	/// <summary>
	/// Visits a unary expression and rebuilds it if the operand changed.
	/// </summary>
	/// <param name="expression">The unary expression.</param>
	/// <returns>The visited expression.</returns>
	protected virtual Expression VisitUnary(UnaryExpression expression)
	{
		/*
		 * Visit the operand; if it was rewritten, create a new unary expression
		 * preserving the original node type, target type and method (if present).
		 */
		if (Visit(expression.Operand) is not Expression visited)
			throw new NullReferenceException(nameof(UnaryExpression));

		return UpdateUnary(expression, visited, expression.Type, expression.Method);
	}

	/// <summary>
	/// Rebuilds a unary expression if any component differs from the original.
	/// </summary>
	/// <param name="expression">Original unary expression.</param>
	/// <param name="operand">Visited operand.</param>
	/// <param name="resultType">Target result type.</param>
	/// <param name="method">Associated method info, if any.</param>
	/// <returns>Updated or original unary expression.</returns>
	protected static UnaryExpression UpdateUnary(UnaryExpression expression, Expression operand, Type resultType, MethodInfo? method)
	{
		/*
		 * Compare operand, result type and method; allocate a new node only when
		 * something changed to reduce churn and preserve identity otherwise.
		 */
		if (expression.Operand != operand || expression.Type != resultType || expression.Method != method)
			return Expression.MakeUnary(expression.NodeType, operand, resultType, method);

		return expression;
	}

	/// <summary>
	/// Visits a binary expression and rebuilds it if any side or conversion changed.
	/// </summary>
	/// <param name="expression">The binary expression.</param>
	/// <returns>The visited expression.</returns>
	protected virtual Expression VisitBinary(BinaryExpression expression)
	{
		/*
		 * Visit left & right operands plus the optional conversion lambda (for
		 * coalesce). Use UpdateBinary to produce a new node only when differences
		 * are detected.
		 */
		if (Visit(expression.Left) is not Expression left)
			throw new NullReferenceException(nameof(left));

		if (Visit(expression.Right) is not Expression right)
			throw new NullReferenceException(nameof(right));

		var conversion = expression.Conversion is not null ? Visit(expression.Conversion) : null;

		return UpdateBinary(expression, left, right, conversion, expression.IsLiftedToNull, expression.Method);
	}

	/// <summary>
	/// Rebuilds a binary expression if any component differs from the original.
	/// </summary>
	/// <param name="expression">Original binary expression.</param>
	/// <param name="left">Visited left operand.</param>
	/// <param name="right">Visited right operand.</param>
	/// <param name="conversion">Visited conversion lambda (for coalesce), if any.</param>
	/// <param name="isLiftedToNull">Whether the node is lifted to null.</param>
	/// <param name="method">Associated method info, if any.</param>
	/// <returns>Updated or original binary expression.</returns>
	protected static BinaryExpression UpdateBinary(BinaryExpression expression, Expression left, Expression right, Expression? conversion, bool isLiftedToNull, MethodInfo? method)
	{
		/*
		 * For coalesce with conversion, use Expression.Coalesce to retain the
		 * conversion delegate; otherwise rely on MakeBinary specifying lifted
		 * semantics & method info. Skip allocation when everything matches.
		 */
		if (left != expression.Left || right != expression.Right || conversion != expression.Conversion || method != expression.Method || isLiftedToNull != expression.IsLiftedToNull)
		{
			if (expression.NodeType == ExpressionType.Coalesce && expression.Conversion is not null)
				return Expression.Coalesce(left, right, conversion as LambdaExpression);
			else
				return Expression.MakeBinary(expression.NodeType, left, right, isLiftedToNull, method);
		}

		return expression;
	}

	/// <summary>
	/// Visits a type test expression and rebuilds it if the inner expression changed.
	/// </summary>
	/// <param name="expression">The type test expression.</param>
	/// <returns>The visited expression.</returns>
	protected virtual Expression VisitTypeIs(TypeBinaryExpression expression)
	{
		/*
		 * Visit the operand of the type test; rebuild only if operand changed
		 * or if the tested type differs (the latter rarely happens here).
		 */
		if (Visit(expression.Expression) is not Expression visited)
			throw new NullReferenceException(nameof(visited));

		return UpdateTypeIs(expression, visited, expression.TypeOperand);
	}

	/// <summary>
	/// Rebuilds a type test expression if any component differs from the original.
	/// </summary>
	/// <param name="expression">Original type test expression.</param>
	/// <param name="e">Visited inner expression.</param>
	/// <param name="typeOperand">The type to test against.</param>
	/// <returns>Updated or original type test expression.</returns>
	protected static TypeBinaryExpression UpdateTypeIs(TypeBinaryExpression expression, Expression e, Type typeOperand)
	{
		/*
		 * Allocate a new TypeIs node only when the operand or target type changed.
		 */
		if (e != expression.Expression || typeOperand != expression.TypeOperand)
			return Expression.TypeIs(e, typeOperand);

		return expression;
	}

	/// <summary>
	/// Visits a conditional expression and rebuilds it if any child changed.
	/// </summary>
	/// <param name="expression">The conditional expression.</param>
	/// <returns>The visited expression.</returns>
	protected virtual Expression VisitConditional(ConditionalExpression expression)
	{
		/*
		 * Visit test, then the true and false branches. Only when a difference
		 * is detected does UpdateConditional allocate a new conditional node.
		 */
		if (Visit(expression.Test) is not Expression test)
			throw new NullReferenceException(nameof(test));

		if (Visit(expression.IfTrue) is not Expression ifTrue)
			throw new NullReferenceException(nameof(ifTrue));

		if (Visit(expression.IfFalse) is not Expression ifFalse)
			throw new NullReferenceException(nameof(ifFalse));

		return UpdateConditional(expression, test, ifTrue, ifFalse);
	}

	/// <summary>
	/// Rebuilds a conditional expression if any component differs from the original.
	/// </summary>
	/// <param name="expression">Original conditional expression.</param>
	/// <param name="test">Visited test expression.</param>
	/// <param name="ifTrue">Visited true branch.</param>
	/// <param name="ifFalse">Visited false branch.</param>
	/// <returns>Updated or original conditional expression.</returns>
	protected static ConditionalExpression UpdateConditional(ConditionalExpression expression, Expression test, Expression ifTrue, Expression ifFalse)
	{
		/*
		 * Create a new conditional node only when any of its three children changed.
		 */
		if (test != expression.Test || ifTrue != expression.IfTrue || ifFalse != expression.IfFalse)
			return Expression.Condition(test, ifTrue, ifFalse);

		return expression;
	}

	/// <summary>
	/// Visits a constant expression.
	/// </summary>
	/// <param name="expression">The constant expression.</param>
	/// <returns>The original expression.</returns>
	protected virtual Expression VisitConstant(ConstantExpression expression)
	{
		/*
		 * Constants are leaves; return as-is.
		 */
		return expression;
	}

	/// <summary>
	/// Visits a parameter expression.
	/// </summary>
	/// <param name="expression">The parameter expression.</param>
	/// <returns>The original expression.</returns>
	protected virtual Expression VisitParameter(ParameterExpression expression)
	{
		/*
		 * Parameters are leaves; return unchanged.
		 */
		return expression;
	}

	/// <summary>
	/// Visits a member access expression and rebuilds it if the instance changed.
	/// </summary>
	/// <param name="expression">The member access expression.</param>
	/// <returns>The visited expression.</returns>
	protected virtual Expression VisitMemberAccess(MemberExpression expression)
	{
		/*
		 * Visit the instance part (may be null for static members). Rebuild only
		 * when the instance differs; member info itself rarely changes.
		 */
		if (expression.Expression is null || Visit(expression.Expression) is not Expression member)
			throw new NullReferenceException(nameof(member));

		return UpdateMemberAccess(expression, member, expression.Member);
	}

	/// <summary>
	/// Rebuilds a member access expression if any component differs from the original.
	/// </summary>
	/// <param name="expression">Original member access expression.</param>
	/// <param name="e">Visited instance expression.</param>
	/// <param name="member">The member info.</param>
	/// <returns>Updated or original member access expression.</returns>
	protected static MemberExpression UpdateMemberAccess(MemberExpression expression, Expression e, MemberInfo member)
	{
		/*
		 * Allocate a new member access only when instance or member changed.
		 */
		if (e != expression.Expression || member != expression.Member)
			return Expression.MakeMemberAccess(e, member);

		return expression;
	}

	/// <summary>
	/// Visits a method call expression and rebuilds it if the object or arguments changed.
	/// </summary>
	/// <param name="expression">The method call expression.</param>
	/// <returns>The visited expression.</returns>
	protected virtual Expression VisitMethodCall(MethodCallExpression expression)
	{
		/*
		 * Visit instance (if any) and each argument; rebuild only if differences
		 * appear to minimize allocations.
		 */
		return UpdateMethodCall(expression, expression.Object is not null ? Visit(expression.Object) : null, expression.Method, VisitExpressionList(expression.Arguments));
	}

	/// <summary>
	/// Rebuilds a method call expression if any component differs from the original.
	/// </summary>
	/// <param name="expression">Original method call expression.</param>
	/// <param name="e">Visited instance expression (or null for static).</param>
	/// <param name="method">The called method.</param>
	/// <param name="args">Visited argument list.</param>
	/// <returns>Updated or original method call expression.</returns>
	protected static MethodCallExpression UpdateMethodCall(MethodCallExpression expression, Expression? e, MethodInfo method, IEnumerable<Expression> args)
	{
		/*
		 * Only construct a new call when instance, method info, or argument list changed.
		 */
		if (e != expression.Object || method != expression.Method || args != expression.Arguments)
			return Expression.Call(e, method, args);

		return expression;
	}

	/// <summary>
	/// Visits a lambda expression and rebuilds it if the body or parameters changed.
	/// </summary>
	/// <param name="lambda">The lambda expression.</param>
	/// <returns>The visited expression.</returns>
	protected virtual Expression VisitLambda(LambdaExpression lambda)
	{
		/*
		 * Visit the body; parameters are immutable collection so we only compare
		 * by reference. Rebuild when body or delegate type changed.
		 */
		if (Visit(lambda.Body) is not Expression body)
			throw new NullReferenceException(nameof(body));

		return UpdateLambda(lambda, lambda.Type, body, lambda.Parameters);
	}

	/// <summary>
	/// Rebuilds a lambda expression if any component differs from the original.
	/// </summary>
	/// <param name="lambda">Original lambda.</param>
	/// <param name="delegateType">Delegate type of the lambda.</param>
	/// <param name="body">Visited body.</param>
	/// <param name="parameters">Visited parameter list.</param>
	/// <returns>Updated or original lambda expression.</returns>
	protected static LambdaExpression UpdateLambda(LambdaExpression lambda, Type delegateType, Expression body, IEnumerable<ParameterExpression> parameters)
	{
		/*
		 * Allocate new lambda only when body, parameter list or delegate type differs.
		 */
		if (body != lambda.Body || parameters != lambda.Parameters || delegateType != lambda.Type)
			return Expression.Lambda(delegateType, body, parameters);

		return lambda;
	}

	/// <summary>
	/// Visits a <see cref="NewExpression"/> and rebuilds it if constructor, members, or arguments changed.
	/// </summary>
	/// <param name="expression">The new expression.</param>
	/// <returns>The visited expression.</returns>
	protected virtual NewExpression VisitNew(NewExpression expression)
	{
		/*
		 * Visit each argument and associated member (for anonymous types etc.).
		 * Rebuild only when differences are found.
		 */
		return UpdateNew(expression, expression.Constructor, VisitMemberAndExpressionList(expression.Members, expression.Arguments), expression.Members);
	}

	/// <summary>
	/// Rebuilds a new expression if any component differs from the original.
	/// </summary>
	/// <param name="expression">Original new expression.</param>
	/// <param name="constructor">Constructor info.</param>
	/// <param name="args">Visited argument list.</param>
	/// <param name="members">Visited member info list, if any.</param>
	/// <returns>Updated or original new expression.</returns>
	protected static NewExpression UpdateNew(NewExpression expression, ConstructorInfo? constructor, IEnumerable<Expression> args, IEnumerable<MemberInfo>? members)
	{
		/*
		 * Build a new NewExpression only when argument list, constructor or member
		 * list changed; handle member-attached version vs simple constructor.
		 */
		if (args != expression.Arguments || constructor != expression.Constructor || members != expression.Members)
		{
			if (constructor is null)
				throw new NullReferenceException(nameof(constructor));

			if (expression.Members is not null)
				return Expression.New(constructor, args, members);
			else
				return Expression.New(constructor, args);
		}

		return expression;
	}

	/// <summary>
	/// Visits an invocation expression and rebuilds it if target or arguments changed.
	/// </summary>
	/// <param name="expression">The invocation expression.</param>
	/// <returns>The visited expression.</returns>
	protected virtual Expression VisitInvocation(InvocationExpression expression)
	{
		/*
		 * Visit the target delegate expression and arguments, rebuilding only if
		 * differences appear.
		 */
		if (Visit(expression.Expression) is not Expression invocation)
			throw new NullReferenceException(nameof(invocation));

		return UpdateInvocation(expression, invocation, VisitExpressionList(expression.Arguments));
	}

	/// <summary>
	/// Rebuilds an invocation expression if any component differs from the original.
	/// </summary>
	/// <param name="expression">Original invocation expression.</param>
	/// <param name="e">Visited target expression.</param>
	/// <param name="args">Visited argument list.</param>
	/// <returns>Updated or original invocation expression.</returns>
	protected static InvocationExpression UpdateInvocation(InvocationExpression expression, Expression e, IEnumerable<Expression> args)
	{
		/*
		 * Construct new invocation only when target or arguments differ.
		 */
		if (args != expression.Arguments || e != expression.Expression)
			return Expression.Invoke(e, args);

		return expression;
	}

	/// <summary>
	/// Visits a member-init expression and rebuilds it if the new expression or bindings changed.
	/// </summary>
	/// <param name="expression">The member-init expression.</param>
	/// <returns>The visited expression.</returns>
	protected virtual Expression VisitMemberInit(MemberInitExpression expression)
	{
		/*
		 * Visit the underlying NewExpression and each binding; rebuild when any
		 * binding or new expression differs.
		 */
		return UpdateMemberInit(expression, VisitNew(expression.NewExpression), VisitBindingList(expression.Bindings));
	}

	/// <summary>
	/// Rebuilds a member-init expression if any component differs from the original.
	/// </summary>
	/// <param name="init">Original member-init expression.</param>
	/// <param name="e">Visited new expression.</param>
	/// <param name="bindings">Visited bindings.</param>
	/// <returns>Updated or original member-init expression.</returns>
	protected static MemberInitExpression UpdateMemberInit(MemberInitExpression init, NewExpression e, IEnumerable<MemberBinding> bindings)
	{
		/*
		 * Allocate a new member init only when the new expression or bindings list changed.
		 */
		if (e != init.NewExpression || bindings != init.Bindings)
			return Expression.MemberInit(e, bindings);

		return init;
	}

	/// <summary>
	/// Visits a list-init expression and rebuilds it if the new expression or initializers changed.
	/// </summary>
	/// <param name="expression">The list-init expression.</param>
	/// <returns>The visited expression.</returns>
	protected virtual Expression VisitListInit(ListInitExpression expression)
	{
		/*
		 * Visit the NewExpression and each element initializer; rebuild only if
		 * either component changed.
		 */
		return UpdateListInit(expression, VisitNew(expression.NewExpression), VisitElementInitializerList(expression.Initializers));
	}

	/// <summary>
	/// Rebuilds a list-init expression if any component differs from the original.
	/// </summary>
	/// <param name="expression">Original list-init expression.</param>
	/// <param name="e">Visited new expression.</param>
	/// <param name="initializers">Visited element initializers.</param>
	/// <returns>Updated or original list-init expression.</returns>
	protected static ListInitExpression UpdateListInit(ListInitExpression expression, NewExpression e, IEnumerable<ElementInit> initializers)
	{
		/*
		 * Allocate a new list-init only when constructor or initializer list differ.
		 */
		if (e != expression.NewExpression || initializers != expression.Initializers)
			return Expression.ListInit(e, initializers);

		return expression;
	}

	/// <summary>
	/// Visits a paired members/expressions list and returns a possibly updated collection.
	/// </summary>
	/// <param name="members">Optional member infos aligned with <paramref name="expressions"/>.</param>
	/// <param name="expressions">The expressions to visit.</param>
	/// <returns>A read-only collection of visited expressions.</returns>
	protected virtual ReadOnlyCollection<Expression> VisitMemberAndExpressionList(ReadOnlyCollection<MemberInfo>? members, ReadOnlyCollection<Expression>? expressions)
	{
		/*
		 * Iterate each expression, visiting with the associated member (if present).
		 * Lazy allocate an alternate list only when a difference is found to keep
		 * identity stable when no changes occur.
		 */
		if (expressions is null)
			return new ReadOnlyCollection<Expression>([]);

		List<Expression>? result = null;

		for (int i = 0; i < expressions.Count; i++)
		{
			var current = expressions[i];
			var visited = VisitMemberAndExpression(members?[i], expressions[i]);

			if (visited is null)
				continue;

			if (result is not null)
				result.Add(visited);
			else if (visited != current)
			{
				result = new List<Expression>(expressions.Count);
				for (var j = 0; j < i; j++)
					result.Add(expressions[j]);
				result.Add(visited);
			}
		}

		if (result is not null)
			return result.AsReadOnly();

		return expressions;
	}

	/// <summary>
	/// Visits a list of expressions and returns a possibly updated collection.
	/// </summary>
	/// <param name="expressions">Expressions to visit.</param>
	/// <returns>A read-only collection of visited expressions.</returns>
	protected virtual ReadOnlyCollection<Expression> VisitExpressionList(ReadOnlyCollection<Expression> expressions)
	{
		/*
		 * Sequentially visit each expression; allocate alternate list only on
		 * first difference to minimize allocations.
		 */
		List<Expression>? result = null;

		for (var i = 0; i < expressions.Count; i++)
		{
			var current = expressions[i];
			var visited = Visit(current);

			if (visited is null)
				continue;

			if (result is not null)
				result.Add(visited);
			else if (visited != current)
			{
				result = new List<Expression>(expressions.Count);
				for (var j = 0; j < i; j++)
					result.Add(expressions[j]);
				result.Add(visited);
			}
		}

		if (result is not null)
			return result.AsReadOnly();

		return expressions;
	}

	/// <summary>
	/// Visits a list of member bindings and returns a possibly updated sequence.
	/// </summary>
	/// <param name="bindings">Bindings to visit.</param>
	/// <returns>An enumerable of visited bindings.</returns>
	protected virtual IEnumerable<MemberBinding> VisitBindingList(ReadOnlyCollection<MemberBinding> bindings)
	{
		/*
		 * Iterate bindings; visit each; lazily allocate alternate list on first
		 * change. Return original sequence when no changes found.
		 */
		List<MemberBinding>? result = null;

		for (int i = 0; i < bindings.Count; i++)
		{
			var current = bindings[i];
			var visited = VisitBinding(current);

			if (result is not null)
				result.Add(visited);
			else if (visited != current)
			{
				result = new List<MemberBinding>(bindings.Count);
				for (var j = 0; j < i; j++)
					result.Add(bindings[j]);
				result.Add(visited);
			}
		}

		if (result is not null)
			return result;

		return bindings;
	}

	/// <summary>
	/// Visits a list of element initializers and returns a possibly updated sequence.
	/// </summary>
	/// <param name="elements">Element initializers to visit.</param>
	/// <returns>An enumerable of visited initializers.</returns>
	protected virtual IEnumerable<ElementInit> VisitElementInitializerList(ReadOnlyCollection<ElementInit> elements)
	{
		/*
		 * Visit each initializer; lazy allocate alternate list on first change.
		 * Preserves identity when no updates are required.
		 */
		List<ElementInit>? result = null;

		for (int i = 0; i < elements.Count; i++)
		{
			var current = elements[i];
			var visited = VisitElementInitializer(current);

			if (result is not null)
				result.Add(visited);
			else if (visited != current)
			{
				result = new List<ElementInit>(elements.Count);
				for (var j = 0; j < i; j++)
					result.Add(elements[j]);
				result.Add(visited);
			}
		}

		if (result is not null)
			return result;

		return elements;
	}

	/// <summary>
	/// Visits a member binding and dispatches to the specific handler.
	/// </summary>
	/// <param name="binding">The member binding.</param>
	/// <returns>The visited binding.</returns>
	/// <exception cref="NotSupportedException">Thrown for unsupported binding types.</exception>
	protected virtual MemberBinding VisitBinding(MemberBinding binding)
	{
		/*
		 * Switch over the binding kind to delegate to the concrete Visit* method
		 * allowing specialized rewriting per binding subtype.
		 */
		return binding.BindingType switch
		{
			MemberBindingType.Assignment => VisitMemberAssignment((MemberAssignment)binding),
			MemberBindingType.MemberBinding => VisitMemberMemberBinding((MemberMemberBinding)binding),
			MemberBindingType.ListBinding => VisitMemberListBinding((MemberListBinding)binding),
			_ => throw new NotSupportedException($"Unhandled binding type '{binding.BindingType}'"),
		};
	}

	/// <summary>
	/// Visits a member assignment binding and rebuilds it if the expression changed.
	/// </summary>
	/// <param name="assignment">The assignment binding.</param>
	/// <returns>The visited binding.</returns>
	protected virtual MemberAssignment VisitMemberAssignment(MemberAssignment assignment)
	{
		/*
		 * Visit the assigned expression; rebuild binding only when expression or
		 * member differs to keep identity stable.
		 */
		if (Visit(assignment.Expression) is not Expression assignmentExpression)
			throw new NullReferenceException(nameof(assignmentExpression));

		return UpdateMemberAssignment(assignment, assignment.Member, assignmentExpression);
	}

	/// <summary>
	/// Rebuilds a member assignment binding if any component differs from the original.
	/// </summary>
	/// <param name="assignment">Original assignment binding.</param>
	/// <param name="member">Target member.</param>
	/// <param name="expression">Visited expression.</param>
	/// <returns>Updated or original assignment binding.</returns>
	protected static MemberAssignment UpdateMemberAssignment(MemberAssignment assignment, MemberInfo member, Expression expression)
	{
		/*
		 * Allocate new binding only when expression or member changed.
		 */
		if (expression != assignment.Expression || member != assignment.Member)
			return Expression.Bind(member, expression);

		return assignment;
	}

	/// <summary>
	/// Visits a member-member binding and rebuilds it if the bindings changed.
	/// </summary>
	/// <param name="binding">The member-member binding.</param>
	/// <returns>The visited binding.</returns>
	protected virtual MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding binding)
	{
		/*
		 * Visit nested bindings list; rebuild only on difference.
		 */
		return UpdateMemberMemberBinding(binding, binding.Member, VisitBindingList(binding.Bindings));
	}

	/// <summary>
	/// Visits a member-list binding and rebuilds it if the initializers changed.
	/// </summary>
	/// <param name="binding">The member-list binding.</param>
	/// <returns>The visited binding.</returns>
	protected virtual MemberListBinding VisitMemberListBinding(MemberListBinding binding)
	{
		/*
		 * Visit initializer list; rebuild only if list differs.
		 */
		return UpdateMemberListBinding(binding, binding.Member, VisitElementInitializerList(binding.Initializers));
	}

	/// <summary>
	/// Rebuilds a member-list binding if any component differs from the original.
	/// </summary>
	/// <param name="binding">Original member-list binding.</param>
	/// <param name="member">Target member.</param>
	/// <param name="initializers">Visited initializers.</param>
	/// <returns>Updated or original member-list binding.</returns>
	protected static MemberListBinding UpdateMemberListBinding(MemberListBinding binding, MemberInfo member, IEnumerable<ElementInit> initializers)
	{
		/*
		 * Allocate new list binding only when member or initializer sequence changed.
		 */
		if (initializers != binding.Initializers || member != binding.Member)
			return Expression.ListBind(member, initializers);

		return binding;
	}

	/// <summary>
	/// Rebuilds a member-member binding if any component differs from the original.
	/// </summary>
	/// <param name="binding">Original member-member binding.</param>
	/// <param name="member">Target member.</param>
	/// <param name="bindings">Visited bindings.</param>
	/// <returns>Updated or original member-member binding.</returns>
	protected static MemberMemberBinding UpdateMemberMemberBinding(MemberMemberBinding binding, MemberInfo member, IEnumerable<MemberBinding> bindings)
	{
		/*
		 * Allocate new member binding only when member or nested bindings collection changed.
		 */
		if (bindings != binding.Bindings || member != binding.Member)
			return Expression.MemberBind(member, bindings);

		return binding;
	}

	/// <summary>
	/// Visits a new-array expression and rebuilds it if the element type or expressions changed.
	/// </summary>
	/// <param name="expression">The new-array expression.</param>
	/// <returns>The visited expression.</returns>
	protected virtual Expression VisitNewArray(NewArrayExpression expression)
	{
		/*
		 * Visit each element expression; rebuild only when list or array type changed.
		 */
		return UpdateNewArray(expression, expression.Type, VisitExpressionList(expression.Expressions));
	}

	/// <summary>
	/// Rebuilds a new-array expression if any component differs from the original.
	/// </summary>
	/// <param name="expression">Original new-array expression.</param>
	/// <param name="arrayType">Array type.</param>
	/// <param name="expressions">Visited expressions.</param>
	/// <returns>Updated or original new-array expression.</returns>
	protected static NewArrayExpression UpdateNewArray(NewArrayExpression expression, Type arrayType, IEnumerable<Expression> expressions)
	{
		/*
		 * Determine element type and create either bounds or init form. Skip allocation
		 * when expressions and array type match originals.
		 */
		if (expressions != expression.Expressions || expression.Type != arrayType)
		{
			if (arrayType.GetElementType() is not Type elementType)
				throw new NullReferenceException(nameof(elementType));

			if (expression.NodeType == ExpressionType.NewArrayInit)
				return Expression.NewArrayInit(elementType, expressions);
			else
				return Expression.NewArrayBounds(elementType, expressions);
		}

		return expression;
	}

	/// <summary>
	/// Fallback hook for unknown expressions.
	/// </summary>
	/// <param name="expression">The expression to report.</param>
	/// <returns>Never returns; always throws.</returns>
	/// <exception cref="NotSupportedException">Always thrown for unknown nodes.</exception>
	protected virtual Expression VisitUnknown(Expression expression)
	{
		/*
		 * Throw for any unknown expression node to surface unsupported constructs early.
		 */
		throw new NotSupportedException(expression.ToString());
	}

	/// <summary>
	/// Visits a single expression with an optional associated member.
	/// </summary>
	/// <param name="member">Optional member info aligned with <paramref name="expression"/>.</param>
	/// <param name="expression">Expression to visit.</param>
	/// <returns>The visited expression.</returns>
	protected virtual Expression VisitMemberAndExpression(MemberInfo? member, Expression expression)
	{
		/*
		 * Delegate to generic Visit; member is ignored by default. Override to use
		 * member metadata while visiting.
		 */
		return Visit(expression);
	}

	/// <summary>
	/// Visits an element initializer and rebuilds it if the arguments changed.
	/// </summary>
	/// <param name="initializer">The element initializer.</param>
	/// <returns>The visited initializer.</returns>
	protected virtual ElementInit VisitElementInitializer(ElementInit initializer)
	{
		/*
		 * Visit each argument; rebuild only when argument list differs (by reference).
		 */
		var arguments = VisitExpressionList(initializer.Arguments);

		if (arguments != initializer.Arguments)
			return Expression.ElementInit(initializer.AddMethod, arguments);

		return initializer;
	}

	/// <summary>
	/// Disposes resources used by the visitor.
	/// </summary>
	/// <param name="disposing">True when called from <see cref="Dispose()"/>.</param>
	private void Dispose(bool disposing)
	{
		/*
		 * Standard dispose pattern: invoke OnDisposing for managed cleanup once,
		 * then mark IsDisposed. Suppression done in public Dispose method.
		 */
		if (!IsDisposed)
		{
			if (disposing)
				OnDisposing();

			IsDisposed = true;
		}
	}

	/// <summary>
	/// Overridable hook invoked during disposal to release managed resources.
	/// </summary>
	protected virtual void OnDisposing()
	{
		/*
		 * Override in derived types to free managed resources. Intentionally empty.
		 */
	}

	/// <summary>
	/// Disposes the visitor and suppresses finalization.
	/// </summary>
	public void Dispose()
	{
		/*
		 * Entry point for disposing: call internal dispose then suppress finalizer.
		 */
		Dispose(true);
		GC.SuppressFinalize(this);
	}
}
