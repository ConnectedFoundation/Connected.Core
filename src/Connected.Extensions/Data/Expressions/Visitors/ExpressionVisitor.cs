using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Connected.Data.Expressions.Visitors;

public abstract class ExpressionVisitor : IDisposable
{
	protected bool IsDisposed { get; private set; }

	protected virtual Expression? Visit(Expression? expression)
	{
		if (expression is null)
			return default;

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

	protected virtual Expression VisitUnary(UnaryExpression expression)
	{
		if (Visit(expression.Operand) is not Expression visited)
			throw new NullReferenceException(nameof(UnaryExpression));

		return UpdateUnary(expression, visited, expression.Type, expression.Method);
	}

	protected static UnaryExpression UpdateUnary(UnaryExpression expression, Expression operand, Type resultType, MethodInfo? method)
	{
		if (expression.Operand != operand || expression.Type != resultType || expression.Method != method)
			return Expression.MakeUnary(expression.NodeType, operand, resultType, method);

		return expression;
	}

	protected virtual Expression VisitBinary(BinaryExpression expression)
	{
		if (Visit(expression.Left) is not Expression left)
			throw new NullReferenceException(nameof(left));

		if (Visit(expression.Right) is not Expression right)
			throw new NullReferenceException(nameof(right));

		var conversion = Visit(expression.Conversion);

		return UpdateBinary(expression, left, right, conversion, expression.IsLiftedToNull, expression.Method);
	}

	protected static BinaryExpression UpdateBinary(BinaryExpression expression, Expression left, Expression right, Expression? conversion, bool isLiftedToNull, MethodInfo? method)
	{
		if (left != expression.Left || right != expression.Right || conversion != expression.Conversion || method != expression.Method || isLiftedToNull != expression.IsLiftedToNull)
		{
			if (expression.NodeType == ExpressionType.Coalesce && expression.Conversion is not null)
				return Expression.Coalesce(left, right, conversion as LambdaExpression);
			else
				return Expression.MakeBinary(expression.NodeType, left, right, isLiftedToNull, method);
		}

		return expression;
	}

	protected virtual Expression VisitTypeIs(TypeBinaryExpression expression)
	{
		if (Visit(expression.Expression) is not Expression visited)
			throw new NullReferenceException(nameof(visited));

		return UpdateTypeIs(expression, visited, expression.TypeOperand);
	}

	protected static TypeBinaryExpression UpdateTypeIs(TypeBinaryExpression expression, Expression e, Type typeOperand)
	{
		if (e != expression.Expression || typeOperand != expression.TypeOperand)
			return Expression.TypeIs(e, typeOperand);

		return expression;
	}

	protected virtual Expression VisitConditional(ConditionalExpression expression)
	{
		if (Visit(expression.Test) is not Expression test)
			throw new NullReferenceException(nameof(test));

		if (Visit(expression.IfTrue) is not Expression ifTrue)
			throw new NullReferenceException(nameof(ifTrue));

		if (Visit(expression.IfFalse) is not Expression ifFalse)
			throw new NullReferenceException(nameof(ifFalse));

		return UpdateConditional(expression, test, ifTrue, ifFalse);
	}

	protected static ConditionalExpression UpdateConditional(ConditionalExpression expression, Expression test, Expression ifTrue, Expression ifFalse)
	{
		if (test != expression.Test || ifTrue != expression.IfTrue || ifFalse != expression.IfFalse)
			return Expression.Condition(test, ifTrue, ifFalse);

		return expression;
	}

	protected virtual Expression VisitConstant(ConstantExpression expression)
	{
		return expression;
	}

	protected virtual Expression VisitParameter(ParameterExpression expression)
	{
		return expression;
	}

	protected virtual Expression VisitMemberAccess(MemberExpression expression)
	{
		if (Visit(expression.Expression) is not Expression member)
			throw new NullReferenceException(nameof(member));

		return UpdateMemberAccess(expression, member, expression.Member);
	}

	protected static MemberExpression UpdateMemberAccess(MemberExpression expression, Expression e, MemberInfo member)
	{
		if (e != expression.Expression || member != expression.Member)
			return Expression.MakeMemberAccess(e, member);

		return expression;
	}

	protected virtual Expression? VisitMethodCall(MethodCallExpression expression)
	{
		return UpdateMethodCall(expression, Visit(expression.Object), expression.Method, VisitExpressionList(expression.Arguments));
	}

	protected static MethodCallExpression UpdateMethodCall(MethodCallExpression expression, Expression? e, MethodInfo method, IEnumerable<Expression> args)
	{
		if (e != expression.Object || method != expression.Method || args != expression.Arguments)
			return Expression.Call(e, method, args);

		return expression;
	}

	protected virtual Expression VisitLambda(LambdaExpression lambda)
	{
		if (Visit(lambda.Body) is not Expression body)
			throw new NullReferenceException(nameof(body));

		return UpdateLambda(lambda, lambda.Type, body, lambda.Parameters);
	}

	protected static LambdaExpression UpdateLambda(LambdaExpression lambda, Type delegateType, Expression body, IEnumerable<ParameterExpression> parameters)
	{
		if (body != lambda.Body || parameters != lambda.Parameters || delegateType != lambda.Type)
			return Expression.Lambda(delegateType, body, parameters);

		return lambda;
	}

	protected virtual NewExpression VisitNew(NewExpression expression)
	{
		return UpdateNew(expression, expression.Constructor, VisitMemberAndExpressionList(expression.Members, expression.Arguments), expression.Members);
	}

	protected static NewExpression UpdateNew(NewExpression expression, ConstructorInfo? constructor, IEnumerable<Expression> args, IEnumerable<MemberInfo>? members)
	{
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

	protected virtual Expression VisitInvocation(InvocationExpression expression)
	{
		if (Visit(expression.Expression) is not Expression invocation)
			throw new NullReferenceException(nameof(invocation));

		return UpdateInvocation(expression, invocation, VisitExpressionList(expression.Arguments));
	}

	protected static InvocationExpression UpdateInvocation(InvocationExpression expression, Expression e, IEnumerable<Expression> args)
	{
		if (args != expression.Arguments || e != expression.Expression)
			return Expression.Invoke(e, args);

		return expression;
	}

	protected virtual Expression VisitMemberInit(MemberInitExpression expression)
	{
		return UpdateMemberInit(expression, VisitNew(expression.NewExpression), VisitBindingList(expression.Bindings));
	}

	protected static MemberInitExpression UpdateMemberInit(MemberInitExpression init, NewExpression e, IEnumerable<MemberBinding> bindings)
	{
		if (e != init.NewExpression || bindings != init.Bindings)
			return Expression.MemberInit(e, bindings);

		return init;
	}

	protected virtual Expression VisitListInit(ListInitExpression expression)
	{
		return UpdateListInit(expression, VisitNew(expression.NewExpression), VisitElementInitializerList(expression.Initializers));
	}

	protected static ListInitExpression UpdateListInit(ListInitExpression expression, NewExpression e, IEnumerable<ElementInit> initializers)
	{
		if (e != expression.NewExpression || initializers != expression.Initializers)
			return Expression.ListInit(e, initializers);

		return expression;
	}

	protected virtual ReadOnlyCollection<Expression> VisitMemberAndExpressionList(ReadOnlyCollection<MemberInfo>? members, ReadOnlyCollection<Expression>? expressions)
	{
		if (expressions is null)
			return new ReadOnlyCollection<Expression>(new List<Expression>());

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

	protected virtual ReadOnlyCollection<Expression> VisitExpressionList(ReadOnlyCollection<Expression>? expressions)
	{
		if (expressions is null)
			return new ReadOnlyCollection<Expression>(new List<Expression>());

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

	protected virtual IEnumerable<MemberBinding> VisitBindingList(ReadOnlyCollection<MemberBinding> bindings)
	{
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

	protected virtual IEnumerable<ElementInit> VisitElementInitializerList(ReadOnlyCollection<ElementInit> elements)
	{
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

	protected virtual MemberBinding VisitBinding(MemberBinding binding)
	{
		return binding.BindingType switch
		{
			MemberBindingType.Assignment => VisitMemberAssignment((MemberAssignment)binding),
			MemberBindingType.MemberBinding => VisitMemberMemberBinding((MemberMemberBinding)binding),
			MemberBindingType.ListBinding => VisitMemberListBinding((MemberListBinding)binding),
			_ => throw new NotSupportedException($"Unhandled binding type '{binding.BindingType}'"),
		};
	}

	protected virtual MemberAssignment VisitMemberAssignment(MemberAssignment assignment)
	{
		if (Visit(assignment.Expression) is not Expression assignmentExpression)
			throw new NullReferenceException(nameof(assignmentExpression));

		return UpdateMemberAssignment(assignment, assignment.Member, assignmentExpression);
	}

	protected static MemberAssignment UpdateMemberAssignment(MemberAssignment assignment, MemberInfo member, Expression expression)
	{
		if (expression != assignment.Expression || member != assignment.Member)
			return Expression.Bind(member, expression);

		return assignment;
	}

	protected virtual MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding binding)
	{
		return UpdateMemberMemberBinding(binding, binding.Member, VisitBindingList(binding.Bindings));
	}

	protected virtual MemberListBinding VisitMemberListBinding(MemberListBinding binding)
	{
		return UpdateMemberListBinding(binding, binding.Member, VisitElementInitializerList(binding.Initializers));
	}

	protected static MemberListBinding UpdateMemberListBinding(MemberListBinding binding, MemberInfo member, IEnumerable<ElementInit> initializers)
	{
		if (initializers != binding.Initializers || member != binding.Member)
			return Expression.ListBind(member, initializers);

		return binding;
	}

	protected static MemberMemberBinding UpdateMemberMemberBinding(MemberMemberBinding binding, MemberInfo member, IEnumerable<MemberBinding> bindings)
	{
		if (bindings != binding.Bindings || member != binding.Member)
			return Expression.MemberBind(member, bindings);

		return binding;
	}

	protected virtual Expression VisitNewArray(NewArrayExpression expression)
	{
		return UpdateNewArray(expression, expression.Type, VisitExpressionList(expression.Expressions));
	}

	protected static NewArrayExpression UpdateNewArray(NewArrayExpression expression, Type arrayType, IEnumerable<Expression> expressions)
	{
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

	protected virtual Expression? VisitUnknown(Expression expression)
	{
		throw new NotSupportedException(expression.ToString());
	}

	protected virtual Expression? VisitMemberAndExpression(MemberInfo? member, Expression expression)
	{
		return Visit(expression);
	}

	protected virtual ElementInit VisitElementInitializer(ElementInit initializer)
	{
		var arguments = VisitExpressionList(initializer.Arguments);

		if (arguments != initializer.Arguments)
			return Expression.ElementInit(initializer.AddMethod, arguments);

		return initializer;
	}

	private void Dispose(bool disposing)
	{
		if (!IsDisposed)
		{
			if (disposing)
				OnDisposing();

			IsDisposed = true;
		}
	}

	protected virtual void OnDisposing()
	{

	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}
}
