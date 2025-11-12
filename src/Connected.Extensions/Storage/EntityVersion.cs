namespace Connected.Storage;

public sealed class EntityVersion : IComparable, IEquatable<EntityVersion>, IComparable<EntityVersion>
{
	public static readonly EntityVersion? Zero = default;

	private readonly ulong Value;

	private EntityVersion(ulong value)
	{
		Value = value;
	}

	public static EntityVersion? Parse(object? value)
	{
		var converted = value?.ToString();

		if (converted is null)
			return Zero;

		if (string.IsNullOrWhiteSpace(converted))
			return Zero;

		return new EntityVersion(Convert.ToUInt64(converted, 16));
	}

	public static implicit operator EntityVersion(ulong value)
	{
		return new EntityVersion(value);
	}

	public static implicit operator EntityVersion(long value)
	{
		return new EntityVersion(unchecked((ulong)value));
	}

	public static explicit operator EntityVersion?(byte[] value)
	{
		if (value is null)
			return null;

		return new EntityVersion((ulong)value[0] << 56 | (ulong)value[1] << 48 | (ulong)value[2] << 40 | (ulong)value[3] << 32 | (ulong)value[4] << 24 | (ulong)value[5] << 16 | (ulong)value[6] << 8 | value[7]);
	}

	public static implicit operator byte[](EntityVersion? timestamp)
	{
		var r = new byte[8];

		if (timestamp is null)
			return r;

		r[0] = (byte)(timestamp.Value >> 56);
		r[1] = (byte)(timestamp.Value >> 48);
		r[2] = (byte)(timestamp.Value >> 40);
		r[3] = (byte)(timestamp.Value >> 32);
		r[4] = (byte)(timestamp.Value >> 24);
		r[5] = (byte)(timestamp.Value >> 16);
		r[6] = (byte)(timestamp.Value >> 8);
		r[7] = (byte)timestamp.Value;

		return r;
	}

	public override bool Equals(object? obj)
	{
		return obj is Version version && Equals(version);
	}

	public override int GetHashCode()
	{
		return Value.GetHashCode();
	}

	public bool Equals(EntityVersion? other)
	{
		return other?.Value == Value;
	}

	int IComparable.CompareTo(object? obj)
	{
		return obj is null ? 1 : CompareTo((EntityVersion)obj);
	}

	public int CompareTo(EntityVersion? other)
	{
		return Value == other?.Value ? 0 : Value < other?.Value ? -1 : 1;
	}

	public static bool operator ==(EntityVersion comparand1, EntityVersion comparand2)
	{
		return comparand1.Equals(comparand2);
	}

	public static bool operator !=(EntityVersion comparand1, EntityVersion comparand2)
	{
		return !comparand1.Equals(comparand2);
	}

	public static bool operator >(EntityVersion comparand1, EntityVersion comparand2)
	{
		return comparand1.CompareTo(comparand2) > 0;
	}

	public static bool operator >=(EntityVersion comparand1, EntityVersion comparand2)
	{
		return comparand1.CompareTo(comparand2) >= 0;
	}

	public static bool operator <(EntityVersion comparand1, EntityVersion comparand2)
	{
		return comparand1.CompareTo(comparand2) < 0;
	}

	public static bool operator <=(EntityVersion comparand1, EntityVersion comparand2)
	{
		return comparand1.CompareTo(comparand2) <= 0;
	}

	public override string ToString()
	{
		return Value.ToString("x16");
	}

	public static EntityVersion Max(EntityVersion comparand1, EntityVersion comparand2)
	{
		return comparand1.Value < comparand2.Value ? comparand2 : comparand1;
	}
}