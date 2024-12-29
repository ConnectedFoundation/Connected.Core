using Connected.Data.Expressions.Expressions;
using Connected.Data.Expressions.Languages;
using Connected.Data.Expressions.Visitors;
using Connected.Reflection;
using System.Linq.Expressions;

namespace Connected.Data.Expressions.Translation;

internal sealed class Parameterizer : DatabaseVisitor
{
	private Parameterizer(QueryLanguage language)
	{
		Language = language;
		Map = new();
		ParameterMap = new();
	}

	private QueryLanguage Language { get; }
	private Dictionary<TypeValue, NamedValueExpression> Map { get; }
	private Dictionary<HashedExpression, NamedValueExpression> ParameterMap { get; }
	private int Counter { get; set; }

	public static Expression Parameterize(QueryLanguage language, Expression expression)
	{
		if (new Parameterizer(language).Visit(expression) is not Expression parameterizedExpression)
			throw new NullReferenceException(nameof(parameterizedExpression));

		return parameterizedExpression;
	}

	protected override Expression VisitProjection(ProjectionExpression expression)
	{
		if (Visit(expression.Select) is not SelectExpression selectExpression)
			throw new NullReferenceException(nameof(selectExpression));

		return UpdateProjection(expression, selectExpression, expression.Projector, expression.Aggregator);
	}

	protected override Expression VisitUnary(UnaryExpression expression)
	{
		if (expression.NodeType == ExpressionType.Convert && expression.Operand.NodeType == ExpressionType.ArrayIndex)
		{
			var b = (BinaryExpression)expression.Operand;

			if (IsConstantOrParameter(b.Left) && IsConstantOrParameter(b.Right))
				return GetNamedValue(expression);
		}

		return base.VisitUnary(expression);
	}

	private static bool IsConstantOrParameter(Expression expression)
	{
		return expression is not null && (expression.NodeType == ExpressionType.Constant || expression.NodeType == ExpressionType.Parameter);
	}

	protected override Expression VisitBinary(BinaryExpression expression)
	{
		if (Visit(expression.Left) is not Expression leftBinaryExpression)
			throw new NullReferenceException(nameof(leftBinaryExpression));

		if (Visit(expression.Right) is not Expression rightBinaryExpression)
			throw new NullReferenceException(nameof(rightBinaryExpression));

		if (leftBinaryExpression.NodeType == (ExpressionType)DatabaseExpressionType.NamedValue && rightBinaryExpression.NodeType == (ExpressionType)DatabaseExpressionType.Column)
		{
			var nv = (NamedValueExpression)leftBinaryExpression;
			var c = (ColumnExpression)rightBinaryExpression;

			leftBinaryExpression = new NamedValueExpression(nv.Name, c.QueryType, nv.Value);
		}
		else if (rightBinaryExpression.NodeType == (ExpressionType)DatabaseExpressionType.NamedValue && leftBinaryExpression.NodeType == (ExpressionType)DatabaseExpressionType.Column)
		{
			var nv = (NamedValueExpression)rightBinaryExpression;
			var c = (ColumnExpression)leftBinaryExpression;

			rightBinaryExpression = new NamedValueExpression(nv.Name, c.QueryType, nv.Value);
		}

		return UpdateBinary(expression, leftBinaryExpression, rightBinaryExpression, expression.Conversion, expression.IsLiftedToNull, expression.Method);
	}

	protected override ColumnAssignment VisitColumnAssignment(ColumnAssignment ca)
	{
		ca = base.VisitColumnAssignment(ca);

		var expression = ca.Expression;
		var nv = expression as NamedValueExpression;

		if (nv is not null)
			expression = new NamedValueExpression(nv.Name, ca.Column.QueryType, nv.Value);

		return UpdateColumnAssignment(ca, ca.Column, expression);
	}

	protected override Expression VisitConstant(ConstantExpression expression)
	{
		if (expression.Value is not null && !IsNumeric(expression.Value.GetType()))
		{
			var tv = new TypeValue(expression.Type, expression.Value);

			if (!Map.TryGetValue(tv, out NamedValueExpression? nv))
			{
				var name = $"p{Counter++}";

				nv = new NamedValueExpression(name, Language.TypeSystem.ResolveColumnType(expression.Type), expression);

				Map.Add(tv, nv);
			}

			return nv;
		}

		return expression;
	}

	protected override Expression VisitParameter(ParameterExpression expression) => GetNamedValue(expression);

	protected override Expression VisitMemberAccess(MemberExpression expression)
	{
		expression = (MemberExpression)base.VisitMemberAccess(expression);

		var nv = expression.Expression as NamedValueExpression;

		if (nv is not null)
		{
			var x = Expression.MakeMemberAccess(nv.Value, expression.Member);

			return GetNamedValue(x);
		}

		return expression;
	}

	private Expression GetNamedValue(Expression expression)
	{
		var he = new HashedExpression(expression);

		if (!ParameterMap.TryGetValue(he, out NamedValueExpression? nv))
		{
			var name = "$p{(iParam++)}";

			nv = new NamedValueExpression(name, Language.TypeSystem.ResolveColumnType(expression.Type), expression);

			ParameterMap.Add(he, nv);
		}

		return nv;
	}

	private static bool IsNumeric(Type type)
	{
		return type.GetTypeCode() switch
		{
			TypeCode.Boolean or TypeCode.Byte or TypeCode.Decimal or TypeCode.Double or TypeCode.Int16 or TypeCode.Int32 or TypeCode.Int64
			or TypeCode.SByte or TypeCode.Single or TypeCode.UInt16 or TypeCode.UInt32 or TypeCode.UInt64 => true,
			_ => false,
		};
	}
}
