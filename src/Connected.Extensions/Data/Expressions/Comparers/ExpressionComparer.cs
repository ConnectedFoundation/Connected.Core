using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;
using System;
using Connected.Data.Expressions.Collections;

namespace Connected.Data.Expressions.Comparers;

internal class ExpressionComparer
{
	protected ExpressionComparer(ScopedDictionary<ParameterExpression, ParameterExpression>? parameterScope, Func<object?, object?, bool>? comparer)
	{
		ParameterScope = parameterScope;
		Comparer = comparer;
	}

	protected Func<object?, object?, bool>? Comparer { get; }
	private ScopedDictionary<ParameterExpression, ParameterExpression>? ParameterScope { get; set; }

	public static bool AreEqual(Expression a, Expression b)
	{
		return AreEqual(null, a, b);
	}

	public static bool AreEqual(Expression a, Expression b, Func<object?, object?, bool>? fnCompare)
	{
		return AreEqual(null, a, b, fnCompare);
	}

	public static bool AreEqual(ScopedDictionary<ParameterExpression, ParameterExpression>? parameterScope, Expression a, Expression b)
	{
		return new ExpressionComparer(parameterScope, null).Compare(a, b);
	}

	public static bool AreEqual(ScopedDictionary<ParameterExpression, ParameterExpression>? parameterScope, Expression a, Expression b, Func<object?, object?, bool>? fnCompare)
	{
		return new ExpressionComparer(parameterScope, fnCompare).Compare(a, b);
	}

	protected virtual bool Compare(Expression? a, Expression? b)
	{
		if (a == b)
			return true;

		if (a is null || b is null)
			return false;

		if (a.NodeType != b.NodeType)
			return false;

		if (a.Type != b.Type)
			return false;

		return a.NodeType switch
		{
			ExpressionType.Negate or ExpressionType.NegateChecked or ExpressionType.Not or ExpressionType.Convert or ExpressionType.ConvertChecked or ExpressionType.ArrayLength or ExpressionType.Quote or ExpressionType.TypeAs or ExpressionType.UnaryPlus => CompareUnary((UnaryExpression)a, (UnaryExpression)b),
			ExpressionType.Add or ExpressionType.AddChecked or ExpressionType.Subtract or ExpressionType.SubtractChecked or ExpressionType.Multiply or ExpressionType.MultiplyChecked or ExpressionType.Divide or ExpressionType.Modulo or ExpressionType.And or ExpressionType.AndAlso or ExpressionType.Or or ExpressionType.OrElse or ExpressionType.LessThan or ExpressionType.LessThanOrEqual or ExpressionType.GreaterThan or ExpressionType.GreaterThanOrEqual or ExpressionType.Equal or ExpressionType.NotEqual or ExpressionType.Coalesce or ExpressionType.ArrayIndex or ExpressionType.RightShift or ExpressionType.LeftShift or ExpressionType.ExclusiveOr or ExpressionType.Power => CompareBinary((BinaryExpression)a, (BinaryExpression)b),
			ExpressionType.TypeIs => CompareTypeIs((TypeBinaryExpression)a, (TypeBinaryExpression)b),
			ExpressionType.Conditional => CompareConditional((ConditionalExpression)a, (ConditionalExpression)b),
			ExpressionType.Constant => CompareConstant((ConstantExpression)a, (ConstantExpression)b),
			ExpressionType.Parameter => CompareParameter((ParameterExpression)a, (ParameterExpression)b),
			ExpressionType.MemberAccess => CompareMemberAccess((MemberExpression)a, (MemberExpression)b),
			ExpressionType.Call => CompareMethodCall((MethodCallExpression)a, (MethodCallExpression)b),
			ExpressionType.Lambda => CompareLambda((LambdaExpression)a, (LambdaExpression)b),
			ExpressionType.New => CompareNew((NewExpression)a, (NewExpression)b),
			ExpressionType.NewArrayInit or ExpressionType.NewArrayBounds => CompareNewArray((NewArrayExpression)a, (NewArrayExpression)b),
			ExpressionType.Invoke => CompareInvocation((InvocationExpression)a, (InvocationExpression)b),
			ExpressionType.MemberInit => CompareMemberInit((MemberInitExpression)a, (MemberInitExpression)b),
			ExpressionType.ListInit => CompareListInit((ListInitExpression)a, (ListInitExpression)b),
			_ => throw new NotSupportedException($"Unhandled expression type: '{a.NodeType}'")
		};
	}

	protected virtual bool CompareUnary(UnaryExpression a, UnaryExpression b)
	{
		return a.NodeType == b.NodeType
			&& a.Method == b.Method
			&& a.IsLifted == b.IsLifted
			&& a.IsLiftedToNull == b.IsLiftedToNull
			&& Compare(a.Operand, b.Operand);
	}

	protected virtual bool CompareBinary(BinaryExpression a, BinaryExpression b)
	{
		return a.NodeType == b.NodeType
			&& a.Method == b.Method
			&& a.IsLifted == b.IsLifted
			&& a.IsLiftedToNull == b.IsLiftedToNull
			&& Compare(a.Left, b.Left)
			&& Compare(a.Right, b.Right);
	}

	protected virtual bool CompareTypeIs(TypeBinaryExpression a, TypeBinaryExpression b)
	{
		return a.TypeOperand == b.TypeOperand
			&& Compare(a.Expression, b.Expression);
	}

	protected virtual bool CompareConditional(ConditionalExpression a, ConditionalExpression b)
	{
		return Compare(a.Test, b.Test)
			&& Compare(a.IfTrue, b.IfTrue)
			&& Compare(a.IfFalse, b.IfFalse);
	}

	protected virtual bool CompareConstant(ConstantExpression a, ConstantExpression b)
	{
		if (Comparer is not null)
			return Comparer(a.Value, b.Value);
		else
			return Equals(a.Value, b.Value);
	}

	protected virtual bool CompareParameter(ParameterExpression a, ParameterExpression b)
	{
		if (ParameterScope is not null)
		{
			if (ParameterScope.TryGetValue(a, out ParameterExpression? mapped))
				return mapped == b;
		}

		return a == b;
	}

	protected virtual bool CompareMemberAccess(MemberExpression a, MemberExpression b)
	{
		return a.Member == b.Member
			&& Compare(a.Expression, b.Expression);
	}

	protected virtual bool CompareMethodCall(MethodCallExpression a, MethodCallExpression b)
	{
		return a.Method == b.Method
			&& Compare(a.Object, b.Object)
			&& CompareExpressionList(a.Arguments, b.Arguments);
	}

	protected virtual bool CompareLambda(LambdaExpression a, LambdaExpression b)
	{
		var n = a.Parameters.Count;

		if (b.Parameters.Count != n)
			return false;

		for (var i = 0; i < n; i++)
		{
			if (a.Parameters[i].Type != b.Parameters[i].Type)
				return false;
		}

		var save = ParameterScope;

		ParameterScope = new ScopedDictionary<ParameterExpression, ParameterExpression>(null);

		try
		{
			for (var i = 0; i < n; i++)
				ParameterScope.Add(a.Parameters[i], b.Parameters[i]);

			return Compare(a.Body, b.Body);
		}
		finally
		{
			ParameterScope = save;
		}
	}

	protected virtual bool CompareNew(NewExpression a, NewExpression b)
	{
		return a.Constructor == b.Constructor
			&& CompareExpressionList(a.Arguments, b.Arguments)
			&& CompareMemberList(a.Members, b.Members);
	}

	protected virtual bool CompareExpressionList(ReadOnlyCollection<Expression>? a, ReadOnlyCollection<Expression>? b)
	{
		if (a == b)
			return true;

		if (a is null || b is null)
			return false;

		if (a.Count != b.Count)
			return false;

		for (var i = 0; i < a.Count; i++)
		{
			if (!Compare(a[i], b[i]))
				return false;
		}

		return true;
	}

	protected virtual bool CompareMemberList(ReadOnlyCollection<MemberInfo>? a, ReadOnlyCollection<MemberInfo>? b)
	{
		if (a == b)
			return true;

		if (a is null || b is null)
			return false;

		if (a.Count != b.Count)
			return false;

		for (var i = 0; i < a.Count; i++)
		{
			if (a[i] != b[i])
				return false;
		}

		return true;
	}

	protected virtual bool CompareNewArray(NewArrayExpression a, NewArrayExpression b)
	{
		return CompareExpressionList(a.Expressions, b.Expressions);
	}

	protected virtual bool CompareInvocation(InvocationExpression a, InvocationExpression b)
	{
		return Compare(a.Expression, b.Expression) && CompareExpressionList(a.Arguments, b.Arguments);
	}

	protected virtual bool CompareMemberInit(MemberInitExpression a, MemberInitExpression b)
	{
		return Compare(a.NewExpression, b.NewExpression) && CompareBindingList(a.Bindings, b.Bindings);
	}

	protected virtual bool CompareBindingList(ReadOnlyCollection<MemberBinding> a, ReadOnlyCollection<MemberBinding> b)
	{
		if (a == b)
			return true;

		if (a is null || b is null)
			return false;

		if (a.Count != b.Count)
			return false;

		for (var i = 0; i < a.Count; i++)
		{
			if (!CompareBinding(a[i], b[i]))
				return false;
		}

		return true;
	}

	protected virtual bool CompareBinding(MemberBinding a, MemberBinding b)
	{
		if (a == b)
			return true;

		if (a is null || b is null)
			return false;

		if (a.BindingType != b.BindingType)
			return false;

		if (a.Member != b.Member)
			return false;

		return a.BindingType switch
		{
			MemberBindingType.Assignment => CompareMemberAssignment((MemberAssignment)a, (MemberAssignment)b),
			MemberBindingType.ListBinding => CompareMemberListBinding((MemberListBinding)a, (MemberListBinding)b),
			MemberBindingType.MemberBinding => CompareMemberMemberBinding((MemberMemberBinding)a, (MemberMemberBinding)b),
			_ => throw new NotSupportedException($"Unhandled binding type: '{a.BindingType}'")
		};
	}

	protected virtual bool CompareMemberAssignment(MemberAssignment a, MemberAssignment b)
	{
		return a.Member == b.Member && Compare(a.Expression, b.Expression);
	}

	protected virtual bool CompareMemberListBinding(MemberListBinding a, MemberListBinding b)
	{
		return a.Member == b.Member && CompareElementInitList(a.Initializers, b.Initializers);
	}

	protected virtual bool CompareMemberMemberBinding(MemberMemberBinding a, MemberMemberBinding b)
	{
		return a.Member == b.Member && CompareBindingList(a.Bindings, b.Bindings);
	}

	protected virtual bool CompareListInit(ListInitExpression a, ListInitExpression b)
	{
		return Compare(a.NewExpression, b.NewExpression) && CompareElementInitList(a.Initializers, b.Initializers);
	}

	protected virtual bool CompareElementInitList(ReadOnlyCollection<ElementInit>? a, ReadOnlyCollection<ElementInit>? b)
	{
		if (a == b)
			return true;

		if (a is null || b is null)
			return false;

		if (a.Count != b.Count)
			return false;

		for (var i = 0; i < a.Count; i++)
		{
			if (!CompareElementInit(a[i], b[i]))
				return false;
		}

		return true;
	}

	protected virtual bool CompareElementInit(ElementInit a, ElementInit b)
	{
		return a.AddMethod == b.AddMethod
			&& CompareExpressionList(a.Arguments, b.Arguments);
	}
}