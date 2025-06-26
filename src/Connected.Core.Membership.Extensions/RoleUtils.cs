namespace Connected.Membership;

public static class RoleUtils
{
	public static bool IsVirtual(string role)
	{
		return string.Equals(role, VirtualRoles.Everyone, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(role, VirtualRoles.Authenticated, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(role, VirtualRoles.Anonimous, StringComparison.OrdinalIgnoreCase);
	}

	public static bool IsVirtual(int id)
	{
		return id < 0;
	}
}
