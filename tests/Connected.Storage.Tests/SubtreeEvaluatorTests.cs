using Connected.Data.Expressions;
using Connected.Data.Expressions.Evaluation;
using Connected.Storage.Sql.Query;
using System.Linq.Expressions;

namespace Connected.Storage.Tests;

/// <summary>
/// Pins what the partial evaluator makes of the values a query's filter captures: each subtree that does not depend on a row is
/// run once and replaced by its value. It needs no database.
/// </summary>
[TestClass]
public sealed class SubtreeEvaluatorTests
{
	private sealed record Bounds(int From, int To);

	private sealed record Wanted(Bounds Bounds, string? Name);

	private static readonly DateTimeOffset Since = new(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);

	[TestMethod]
	public void EveryShapeOfCapturedValueIsReplacedByItsValue()
	{
		var scalar = 7;
		var wanted = new Wanted(new Bounds(3, 9), "name");
		string? absent = null;

		// A local, and a member of a member.
		Assert.AreEqual(7, Value(() => scalar));
		Assert.AreEqual(9, Value(() => wanted.Bounds.To));
		Assert.AreEqual("name", Value(() => wanted.Name));
		// Values worked out by a call: arithmetic, a method of a captured value, one built from two.
		Assert.AreEqual(11, Value(() => Math.Max(scalar, wanted.Bounds.From) + 4));
		Assert.AreEqual(Since.AddDays(7), Value(() => Since.AddDays(scalar)));
		Assert.AreEqual("name-7", Value(() => string.Concat(wanted.Name, "-", scalar.ToString())));
		// A captured null, and what stands in for it.
		Assert.IsNull(Value(() => absent));
		Assert.AreEqual("other", Value(() => absent ?? "other"));
	}

	[TestMethod]
	public void AnExceptionFromACapturedValueReachesTheCaller()
	{
		var text = "not a number";

		Assert.ThrowsException<FormatException>(() => Value(() => int.Parse(text)));
	}

	/// <summary>
	/// A known limitation, pinned as it behaves: a captured variable's query parameter is named after the variable, so a filter that
	/// captures the same variable twice, or two members of the same name, is refused. A fix changes this test.
	/// </summary>
	[TestMethod]
	public void CapturingOneNameTwiceIsRefused()
	{
		var scalar = 7;

		Assert.ThrowsException<ArgumentException>(() => Evaluate<Func<int, bool>>(row => row >= scalar && row <= scalar));
	}

	/// <summary>The value an expression without rows is replaced by.</summary>
	private static object? Value<T>(Expression<Func<T>> captured)
	{
		var body = Evaluate(captured).Body;

		Assert.IsInstanceOfType<ConstantExpression>(body);

		return ((ConstantExpression)body).Value;
	}

	private static LambdaExpression Evaluate<TDelegate>(Expression<TDelegate> expression)
	{
		var context = new ExpressionCompilationContext(new TSqlLanguage());

		return (LambdaExpression)PartialEvaluator.Eval(context, expression);
	}
}
