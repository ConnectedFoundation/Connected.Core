using Connected.Data.Expressions.Languages;
using Connected.Data.Expressions.Translation;

namespace Connected.Data.Expressions.Expressions;

/// <summary>
/// Represents a reference to a column inside a query expression.
/// </summary>
/// <remarks>
/// A <see cref="ColumnExpression"/> encodes the column's alias (the source
/// reference), the logical column name and the element type used by the
/// expression tree. It is primarily used by query translators to produce
/// column references for SQL or other target languages.
/// </remarks>
public sealed class ColumnExpression(Type type, DataType dataType, Alias alias, string name)
		: DatabaseExpression(DatabaseExpressionType.Column, type), IEquatable<ColumnExpression>
{
	/*
	 * The alias identifying the query source (table or subquery) that owns this column.
	 */
	public Alias Alias { get; } = alias;

	/*
	 * The logical name of the column within the source.
	 */
	public string Name { get; } = name;

	/*
	 * The data type information for the column. This contains metadata such as
	 * nullability, length, precision and scale which can be useful for translators
	 * or type coercion logic.
	 */
	public DataType QueryType { get; } = dataType;

	/// <summary>
	/// Returns a compact textual representation of the column expression used
	/// for debugging and diagnostics.
	/// </summary>
	/// <returns>A short string in the form "{Alias}.C({Name})".</returns>
	public override string ToString()
	{
		/*
		 * The string format is intentionally compact: the alias followed by a
		 * token and the column name. Translators usually ignore this method but
		 * it is handy for debugging and unit tests.
		 */
		return $"{Alias}.C({Name})";
	}

	/// <summary>
	/// Computes a hash code suitable for use in dictionaries keyed by column
	/// expressions. Combines the alias and column name hash codes.
	/// </summary>
	public override int GetHashCode()
	{
		/*
		 * Combine the hash codes of alias and name. The operation is intentionally
		 * simple and fast; collisions are acceptable but unlikely for typical
		 * usage scenarios.
		 */
		return Alias.GetHashCode() + Name.GetHashCode();
	}

	/// <summary>
	/// Determines whether the specified object is equal to the current column expression.
	/// </summary>
	/// <param name="obj">The object to compare with the current instance.</param>
	/// <returns><c>true</c> if the objects are equal; otherwise, <c>false</c>.</returns>
	public override bool Equals(object? obj)
	{
		/*
		 * Delegate to the strongly-typed equality implementation to avoid
		 * duplicating equality logic.
		 */
		return Equals(obj as ColumnExpression);
	}

	/// <summary>
	/// Determines whether the specified <see cref="ColumnExpression"/> is equal
	/// to the current instance by comparing alias and name.
	/// </summary>
	/// <param name="other">The other column expression to compare.</param>
	/// <returns><c>true</c> if both alias and name match; otherwise, <c>false</c>.</returns>
	public bool Equals(ColumnExpression? other)
	{
		/*
		 * Null check and quick reference/equality comparison. The implementation
		 * favors readability and simplicity: alias and name determine equality.
		 */
		return other is not null && this == other || Alias == other?.Alias && Name == other.Name;
	}
}
