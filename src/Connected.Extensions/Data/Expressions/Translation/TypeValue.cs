using System;

namespace Connected.Data.Expressions.Translation;

internal readonly struct TypeValue : IEquatable<TypeValue>
{
	private readonly Type _type;
	private readonly object _value;
	private readonly int _hash;

	public TypeValue(Type type, object value)
	{
		_type = type;
		_value = value;
		_hash = type.GetHashCode() + (value is not null ? value.GetHashCode() : 0);
	}

	public override bool Equals(object? obj)
	{
		if (obj is not TypeValue)
			return false;

		return Equals((TypeValue)obj);
	}

	public bool Equals(TypeValue vt)
	{
		return vt._type == _type && Equals(vt._value, _value);
	}

	public override int GetHashCode()
	{
		return _hash;
	}
}