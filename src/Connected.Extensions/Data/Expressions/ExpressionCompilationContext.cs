using Connected.Data.Expressions.Languages;
using System.Linq.Expressions;

namespace Connected.Data.Expressions;

/// <summary>
/// Holds context used during expression translation and compilation for a specific
/// query language. The context aggregates the language instance, parameter values
/// and variable collections that may be bound during translation.
/// </summary>
/// <param name="language">The target query language driving translation rules.</param>
public sealed class ExpressionCompilationContext(QueryLanguage language)
{
	/// <summary>
	/// Gets the query language that provides type system and language-specific rules
	/// used by the translator when rewriting and formatting expressions.
	/// </summary>
	public QueryLanguage Language { get; } = language;
	/*
	 * A dictionary of named parameter expressions captured during compilation.
	 * Keys are parameter names; values are constant expressions used to substitute
	 * literal values into the translated tree while preserving parameterization.
	 */
	/// <summary>
	/// Gets the collection of named parameter expressions captured during translation.
	/// </summary>
	public Dictionary<string, ConstantExpression> Parameters { get; } = [];
	/*
	 * A dictionary of variable collections used when translation introduces scoped
	 * variables (e.g., client-join grouping or temporary values). Each name maps to
	 * a list of values that may be consumed by client-side operations.
	 */
	/// <summary>
	/// Gets the collection of named variable lists captured during translation.
	/// </summary>
	public Dictionary<string, List<object?>> Variables { get; } = [];
}
