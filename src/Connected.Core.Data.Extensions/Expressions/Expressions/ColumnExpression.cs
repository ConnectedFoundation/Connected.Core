using System;
using Connected.Data.Expressions.Languages;
using Connected.Data.Expressions.Translation;

namespace Connected.Data.Expressions.Expressions;

public sealed class ColumnExpression : DatabaseExpression, IEquatable<ColumnExpression>
{
	public ColumnExpression(Type type, DataType dataType, Alias alias, string name)
		: base(DatabaseExpressionType.Column, type)
	{
		if (dataType is null)
			throw new ArgumentNullException(nameof(dataType));

		if (name is null)
			throw new ArgumentNullException(nameof(name));

		Alias = alias;
		Name = name;
		QueryType = dataType;
	}

	public Alias Alias { get; }
	public string Name { get; }
	public DataType QueryType { get; }

	public override string ToString()
	{
		return $"{Alias}.C({Name})";
	}

	public override int GetHashCode()
	{
		return Alias.GetHashCode() + Name.GetHashCode();
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as ColumnExpression);
	}

	public bool Equals(ColumnExpression? other)
	{
		return other is not null && this == other || Alias == other?.Alias && Name == other.Name;
	}
}
