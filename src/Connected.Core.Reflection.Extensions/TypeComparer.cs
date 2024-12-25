namespace Connected.Reflection;

public static class TypeComparer
{
	public static bool Compare(object? left, object? right)
	{
		if (left is null && right is null)
			return true;

		if (left is null && right is not null)
			return false;

		if (left is not null && right is null)
			return false;

		var leftString = left?.ToString();
		var rightString = right?.ToString();

		if (Guid.TryParse(leftString, out Guid lg) && Guid.TryParse(rightString, out Guid rg))
			return lg == rg;

		return string.Equals(leftString, rightString, StringComparison.Ordinal);
	}
}
