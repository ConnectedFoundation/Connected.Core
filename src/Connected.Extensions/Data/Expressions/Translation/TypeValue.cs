namespace Connected.Data.Expressions.Translation;

internal readonly struct TypeValue(Type type, object value)
		: IEquatable<TypeValue>
{
	private readonly Type _type = type;
	private readonly object _value = value;
	private readonly int _hash = type.GetHashCode() + (value is not null ? value.GetHashCode() : 0);

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