using System.Linq.Expressions;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Reflection;
using Connected.Data.Expressions.Mappings;
using Connected.Data.Expressions.Expressions;
using Connected.Data.Expressions.Visitors;

namespace Connected.Data.Expressions.Translation.Rewriters;

public sealed class ComparisonRewriter : DatabaseVisitor
{
	private ComparisonRewriter()
	{
	}

	public static Expression Rewrite(Expression expression)
	{
		return new ComparisonRewriter().Visit(expression);
	}

	protected override Expression VisitBinary(BinaryExpression b)
	{
		switch (b.NodeType)
		{
			case ExpressionType.Equal:
			case ExpressionType.NotEqual:
				var result = Compare(b);

				if (result == b)
					goto default;

				return Visit(result);
			default:
				return base.VisitBinary(b);
		}
	}

	private static Expression SkipConvert(Expression expression)
	{
		while (expression.NodeType == ExpressionType.Convert)
			expression = ((UnaryExpression)expression).Operand;

		return expression;
	}

	private Expression Compare(BinaryExpression bop)
	{
		var e1 = SkipConvert(bop.Left);
		var e2 = SkipConvert(bop.Right);
		var oj1 = e1 as OuterJoinedExpression;
		var oj2 = e2 as OuterJoinedExpression;
		var entity1 = oj1 is not null ? oj1.Expression as EntityExpression : e1 as EntityExpression;
		var entity2 = oj2 is not null ? oj2.Expression as EntityExpression : e2 as EntityExpression;
		var negate = bop.NodeType == ExpressionType.NotEqual;

		if (oj1 is not null && e2.NodeType == ExpressionType.Constant && ((ConstantExpression)e2).Value is null)
			return MakeIsNull(oj1.Test, negate);
		else if (oj2 is not null && e1.NodeType == ExpressionType.Constant && ((ConstantExpression)e1).Value is null)
			return MakeIsNull(oj2.Test, negate);

		if (entity1 is not null)
			return MakePredicate(e1, e2, MappingsCache.Get(entity1.EntityType).Members.Where(f => f.IsPrimaryKey).Select(f => f.MemberInfo), negate);
		else if (entity2 is not null)
			return MakePredicate(e1, e2, MappingsCache.Get(entity2.EntityType).Members.Where(f => f.IsPrimaryKey).Select(f => f.MemberInfo), negate);

		var dm1 = GetDefinedMembers(e1);
		var dm2 = GetDefinedMembers(e2);

		if (dm1 is null && dm2 is null)
			return bop;

		if (dm1 is not null && dm2 is not null)
		{
			var names1 = new HashSet<string>(dm1.Select(m => m.Name));
			var names2 = new HashSet<string>(dm2.Select(m => m.Name));

			if (names1.IsSubsetOf(names2) && names2.IsSubsetOf(names1))
				return MakePredicate(e1, e2, dm1, negate);
		}
		else if (dm1 is not null)
			return MakePredicate(e1, e2, dm1, negate);
		else if (dm2 is not null)
			return MakePredicate(e1, e2, dm2, negate);

		throw new InvalidOperationException("Cannot compare two constructed types with different sets of members assigned.");
	}

	private static Expression MakeIsNull(Expression expression, bool negate)
	{
		var isnull = new IsNullExpression(expression);

		return negate ? Expression.Not(isnull) : isnull;
	}

	private static Expression? MakePredicate(Expression e1, Expression e2, IEnumerable<MemberInfo> members, bool negate)
	{
		var pred = members.Select(m => Binder.Bind(e1, m).Equal(Binder.Bind(e2, m))).Join(ExpressionType.And);

		if (negate)
			pred = Expression.Not(pred);

		return pred;
	}

	private static IEnumerable<MemberInfo> GetDefinedMembers(Expression expr)
	{
		var mini = expr as MemberInitExpression;

		if (mini is not null)
		{
			var members = mini.Bindings.Select(b => FixMember(b.Member));

			if (mini.NewExpression.Members is not null)
				members = members.Concat(mini.NewExpression.Members.Select(FixMember));

			return members;
		}
		else
		{
			var nex = expr as NewExpression;

			if (nex is not null && nex.Members is not null)
				return nex.Members.Select(FixMember);
		}

		return null;
	}

	private static MemberInfo FixMember(MemberInfo member)
	{
		if (member is MethodInfo && member.Name.StartsWith("get_"))
			return member.DeclaringType.GetTypeInfo().GetDeclaredProperty(member.Name[4..]);

		return member;
	}
}
