using System.Reflection;

namespace Connected.Reflection;

public static class Members
{
	public static IEnumerable<MemberInfo> GetDataMembers(this Type type, string? name = null, bool includeNonPublic = false)
	{
		return type.GetInheritedProperites()
				 .Where(p => p.CanRead && p.GetMethod is not null && !p.GetMethod.IsStatic && (p.GetMethod.IsPublic || includeNonPublic) && (string.IsNullOrEmpty(name) || string.Equals(p.Name, name, StringComparison.Ordinal)))
				 .Cast<MemberInfo>().Concat(type.GetInheritedFields()
				 .Where(f => !f.IsStatic && (f.IsPublic || includeNonPublic) && (string.IsNullOrEmpty(name) || string.Equals(f.Name, name, StringComparison.Ordinal))));
	}

	public static MemberInfo? GetDataMember(this Type type, string name, bool includeNonPublic = false)
	{
		return type.GetDataMembers(name, includeNonPublic).FirstOrDefault();
	}

	public static IEnumerable<FieldInfo> GetInheritedFields(this Type type)
	{
		foreach (var info in type.GetInheritedTypeInfos())
		{
			foreach (var p in info.GetRuntimeFields())
				yield return p;
		}
	}

	public static Type? GetMemberType(MemberInfo mi)
	{
		if (mi is FieldInfo fi)
			return fi.FieldType;

		if (mi is PropertyInfo pi)
			return pi.PropertyType;

		if (mi is EventInfo ei)
			return ei.EventHandlerType;

		if (mi is MethodInfo me)
			return me.ReturnType;

		return default;
	}

	public static bool IsReadOnly(MemberInfo member)
	{
		if (member is PropertyInfo pi)
			return !pi.CanWrite || pi.SetMethod is null;

		if (member is FieldInfo fi)
			return (fi.Attributes & FieldAttributes.InitOnly) != 0;

		return true;
	}
}
