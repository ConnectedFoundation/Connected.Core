using Connected.Data.Expressions.Languages;
using System.Linq.Expressions;

namespace Connected.Data.Expressions;

public sealed class ExpressionCompilationContext(QueryLanguage language)
{
	public QueryLanguage Language { get; } = language;
	public Dictionary<string, ConstantExpression> Parameters { get; } = new();
	public Dictionary<string, List<object?>> Variables { get; } = new();
}
