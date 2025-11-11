using Connected.Reflection;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using ExpressionVisitor = Connected.Data.Expressions.Visitors.ExpressionVisitor;

namespace Connected.Data.Expressions.Serialization;

internal enum Indentation
{
	Same = 0,
	Inner = 1,
	Outer = 2
}

internal class ExpressionSerializer
	: ExpressionVisitor
{
	private const char NewLine = '\n';
	private const char Space = ' ';
	private const string Null = "null";
	static ExpressionSerializer()
	{
		Splitters = new char[] { '\n', '\r' };
		Special = new char[] { '\n', '\n', '\\' };
	}
	protected ExpressionSerializer(TextWriter writer)
	{
		Writer = writer;
	}

	protected int IndentationWidth { get; set; } = 2;
	private int Depth { get; set; }
	private TextWriter Writer { get; }
	private static char[] Splitters { get; }
	private static char[] Special { get; }

	public static void Serialize(TextWriter writer, Expression expression)
	{
		new ExpressionSerializer(writer).Visit(expression);
	}

	public static string Serialize(Expression expression)
	{
		var writer = new StringWriter();

		Serialize(writer, expression);

		return writer.ToString();
	}

	protected void WriteLine(Indentation style)
	{
		Writer.WriteLine();

		Indent(style);

		for (var i = 0; i < Depth * IndentationWidth; i++)
			Writer.Write(Space);
	}

	protected void Write(char? text)
	{
		if (!text.HasValue)
			return;

		Writer.Write(text.ToString());
	}
	protected void Write(string? text)
	{
		if (string.IsNullOrEmpty(text))
			return;

		if (text.Contains(NewLine))
		{
			var lines = text.Split(Splitters, StringSplitOptions.RemoveEmptyEntries);
			var length = lines.Length;

			for (var i = 0; i < length; i++)
			{
				Write(lines[i]);

				if (i < length - 1)
					WriteLine(Indentation.Same);
			}
		}
		else
			Writer.Write(text);
	}

	protected void Indent(Indentation style)
	{
		if (style == Indentation.Inner)
			Depth++;
		else if (style == Indentation.Outer)
		{
			Depth--;

			System.Diagnostics.Debug.Assert(Depth >= 0);
		}
	}

	protected virtual string? ResolveOperator(ExpressionType type)
	{
		return type switch
		{
			ExpressionType.Not => "!",
			ExpressionType.Add or ExpressionType.AddChecked => "+",
			ExpressionType.Negate or ExpressionType.NegateChecked or ExpressionType.Subtract or ExpressionType.SubtractChecked => "-",
			ExpressionType.Multiply or ExpressionType.MultiplyChecked => "*",
			ExpressionType.Divide => "/",
			ExpressionType.Modulo => "%",
			ExpressionType.And => "&",
			ExpressionType.AndAlso => "&&",
			ExpressionType.Or => "|",
			ExpressionType.OrElse => "||",
			ExpressionType.LessThan => "<",
			ExpressionType.LessThanOrEqual => "<=",
			ExpressionType.GreaterThan => ">",
			ExpressionType.GreaterThanOrEqual => ">=",
			ExpressionType.Equal => "==",
			ExpressionType.NotEqual => "!=",
			ExpressionType.Coalesce => "??",
			ExpressionType.RightShift => ">>",
			ExpressionType.LeftShift => "<<",
			ExpressionType.ExclusiveOr => "^",
			_ => null,
		};
	}

	protected override Expression VisitBinary(BinaryExpression expression)
	{
		switch (expression.NodeType)
		{
			case ExpressionType.ArrayIndex:
				Visit(expression.Left);
				Write("[");
				Visit(expression.Right);
				Write("]");
				break;
			case ExpressionType.Power:
				Write("POW(");
				Visit(expression.Left);
				Write(", ");
				Visit(expression.Right);
				Write(")");
				break;
			default:
				Visit(expression.Left);
				Write(Space);
				Write(ResolveOperator(expression.NodeType));
				Write(Space);
				Visit(expression.Right);
				break;
		}

		return expression;
	}

	protected override Expression VisitUnary(UnaryExpression expression)
	{
		switch (expression.NodeType)
		{
			case ExpressionType.Convert:
			case ExpressionType.ConvertChecked:
				Write("((");
				Write(GetTypeName(expression.Type));
				Write(")");
				Visit(expression.Operand);
				Write(")");
				break;
			case ExpressionType.ArrayLength:
				Visit(expression.Operand);
				Write(".Length");
				break;
			case ExpressionType.Quote:
				Visit(expression.Operand);
				break;
			case ExpressionType.TypeAs:
				Visit(expression.Operand);
				Write(" as ");
				Write(GetTypeName(expression.Type));
				break;
			case ExpressionType.UnaryPlus:
				Visit(expression.Operand);
				break;
			default:
				Write(ResolveOperator(expression.NodeType));
				Visit(expression.Operand);
				break;
		}

		return expression;
	}

	protected virtual string GetTypeName(Type type)
	{
		var name = type.Name.Replace('+', '.');
		var iGeneneric = name.IndexOf('`');

		if (iGeneneric > 0)
			name = name[..iGeneneric];

		var info = type.GetTypeInfo();

		if (info.IsGenericType || info.IsGenericTypeDefinition)
		{
			var sb = new StringBuilder();

			sb.Append(name);
			sb.Append('<');

			var args = info.GenericTypeArguments;

			for (int i = 0; i < args.Length; i++)
			{
				if (i > 0)
					sb.Append(',');

				if (info.IsGenericType)
					sb.Append(GetTypeName(args[i]));
			}

			sb.Append('>');

			name = sb.ToString();
		}

		return name;
	}

	protected override Expression VisitConditional(ConditionalExpression expression)
	{
		Visit(expression.Test);
		WriteLine(Indentation.Inner);
		Write("? ");
		Visit(expression.IfTrue);
		WriteLine(Indentation.Same);
		Write(": ");
		Visit(expression.IfFalse);
		Indent(Indentation.Outer);

		return expression;
	}

	protected override IEnumerable<MemberBinding> VisitBindingList(ReadOnlyCollection<MemberBinding> bindings)
	{
		var length = bindings.Count;

		for (var i = 0; i < length; i++)
		{
			VisitBinding(bindings[i]);

			if (i < length - 1)
			{
				Write(',');
				WriteLine(Indentation.Same);
			}
		}

		return bindings;
	}

	protected override Expression VisitConstant(ConstantExpression expression)
	{
		if (expression.Value is null)
			Write(Null);
		else if (expression.Type == typeof(string))
		{
			if (expression.Value.ToString() is string value)
			{
				if (value.IndexOfAny(Special) >= 0)
					Write('@');

				Write('"');
				Write(expression.Value.ToString());
				Write('"');
			}
		}
		else if (expression.Type == typeof(DateTime))
		{
			Write("new DateTime(\"");
			Write(expression.Value.ToString());
			Write("\")");
		}
		else if (expression.Type.IsArray)
		{
			if (expression.Type.GetElementType() is Type elementType)
				VisitNewArray(Expression.NewArrayInit(elementType, ((IEnumerable)expression.Value).OfType<object>().Select(v => (Expression)Expression.Constant(v, elementType))));
		}
		else
			Write(expression.Value.ToString());

		return expression;
	}

	protected override ElementInit VisitElementInitializer(ElementInit initializer)
	{
		if (initializer.Arguments.Count > 1)
		{
			Write('{');

			var length = initializer.Arguments.Count;

			for (var i = 0; i < length; i++)
			{
				Visit(initializer.Arguments[i]);

				if (i < length - 1)
					Write(", ");
			}

			Write('}');
		}
		else
			Visit(initializer.Arguments[0]);

		return initializer;
	}

	protected override IEnumerable<ElementInit> VisitElementInitializerList(ReadOnlyCollection<ElementInit> original)
	{
		var length = original.Count;

		for (var i = 0; i < length; i++)
		{
			VisitElementInitializer(original[i]);

			if (i < length - 1)
			{
				Write(',');
				WriteLine(Indentation.Same);
			}
		}

		return original;
	}

	protected override ReadOnlyCollection<Expression> VisitExpressionList(ReadOnlyCollection<Expression> original)
	{
		var length = original.Count;

		for (var i = 0; i < length; i++)
		{
			Visit(original[i]);

			if (i < length - 1)
			{
				Write(',');
				WriteLine(Indentation.Same);
			}
		}

		return original;
	}

	protected override Expression VisitInvocation(InvocationExpression expression)
	{
		Write("Invoke(");
		WriteLine(Indentation.Inner);
		VisitExpressionList(expression.Arguments);
		Write(", ");
		WriteLine(Indentation.Same);
		Visit(expression.Expression);
		WriteLine(Indentation.Same);
		Write(')');
		Indent(Indentation.Outer);

		return expression;
	}

	protected override Expression VisitLambda(LambdaExpression lambda)
	{
		if (lambda.Parameters.Count != 1)
		{
			Write('(');

			var length = lambda.Parameters.Count;

			for (var i = 0; i < length; i++)
			{
				Write(lambda.Parameters[i].Name);

				if (i < length - 1)
					Write(", ");
			}

			Write(')');
		}
		else
			Write(lambda.Parameters[0].Name);

		Write(" => ");
		Visit(lambda.Body);

		return lambda;
	}

	protected override Expression VisitListInit(ListInitExpression expression)
	{
		Visit(expression.NewExpression);
		Write(" {");
		WriteLine(Indentation.Inner);
		VisitElementInitializerList(expression.Initializers);
		WriteLine(Indentation.Outer);
		Write('}');

		return expression;
	}

	protected override Expression VisitMemberAccess(MemberExpression expression)
	{
		if (expression.Expression is null)
			throw new NullReferenceException(SR.ErrExpectedExpression);

		Visit(expression.Expression);
		Write('.');
		Write(expression.Member.Name);

		return expression;
	}

	protected override MemberAssignment VisitMemberAssignment(MemberAssignment assignment)
	{
		Write(assignment.Member.Name);
		Write(" = ");
		Visit(assignment.Expression);

		return assignment;
	}

	protected override Expression VisitMemberInit(MemberInitExpression expression)
	{
		Visit(expression.NewExpression);
		Write(" {");
		WriteLine(Indentation.Inner);
		VisitBindingList(expression.Bindings);
		WriteLine(Indentation.Outer);
		Write('}');

		return expression;
	}

	protected override MemberListBinding VisitMemberListBinding(MemberListBinding binding)
	{
		Write(binding.Member.Name);
		Write(" = {");
		WriteLine(Indentation.Inner);
		VisitElementInitializerList(binding.Initializers);
		WriteLine(Indentation.Outer);
		Write('}');

		return binding;
	}

	protected override MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding binding)
	{
		Write(binding.Member.Name);
		Write(" = {");
		WriteLine(Indentation.Inner);
		VisitBindingList(binding.Bindings);
		WriteLine(Indentation.Outer);
		Write('}');

		return binding;
	}

	protected override Expression VisitMethodCall(MethodCallExpression expression)
	{
		if (expression.Object is not null)
			Visit(expression.Object);
		else
		{
			if (expression.Method.DeclaringType is null)
				throw new NullReferenceException(nameof(expression.Method.DeclaringType));

			Write(GetTypeName(expression.Method.DeclaringType));
		}

		Write('.');
		Write(expression.Method.Name);
		Write('(');

		if (expression.Arguments.Count > 1)
			WriteLine(Indentation.Inner);

		VisitExpressionList(expression.Arguments);

		if (expression.Arguments.Count > 1)
			WriteLine(Indentation.Outer);

		Write(')');

		return expression;
	}

	protected override NewExpression VisitNew(NewExpression expression)
	{
		if (expression.Constructor?.DeclaringType is null)
			throw new NullReferenceException(nameof(expression.Constructor.DeclaringType));

		Write("new ");
		Write(GetTypeName(expression.Constructor.DeclaringType));
		Write('(');

		if (expression.Arguments.Count > 1)
			WriteLine(Indentation.Inner);

		VisitExpressionList(expression.Arguments);

		if (expression.Arguments.Count > 1)
			WriteLine(Indentation.Outer);

		Write(')');

		return expression;
	}

	protected override Expression VisitNewArray(NewArrayExpression expression)
	{
		if (Enumerables.GetEnumerableElementType(expression.Type) is not Type enumerableType)
			throw new NullReferenceException(nameof(enumerableType));

		Write("new ");
		Write(GetTypeName(enumerableType));
		Write("[] {");

		if (expression.Expressions.Count > 1)
			WriteLine(Indentation.Inner);

		VisitExpressionList(expression.Expressions);

		if (expression.Expressions.Count > 1)
			WriteLine(Indentation.Outer);

		Write('}');

		return expression;
	}

	protected override Expression VisitParameter(ParameterExpression expression)
	{
		Write(expression.Name);

		return expression;
	}

	protected override Expression VisitTypeIs(TypeBinaryExpression expression)
	{
		Visit(expression.Expression);
		Write(" is ");
		Write(GetTypeName(expression.TypeOperand));

		return expression;
	}

	protected override Expression VisitUnknown(Expression expression)
	{
		Write(expression.ToString());

		return expression;
	}

	protected override void OnDisposing()
	{
		Writer?.Dispose();
	}
}