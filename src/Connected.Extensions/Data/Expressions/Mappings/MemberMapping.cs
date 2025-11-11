using Connected.Annotations.Entities;
using Connected.Reflection;
using System.Reflection;

namespace Connected.Data.Expressions.Mappings;

internal sealed class MemberMapping
{
	public MemberMapping(PropertyInfo property)
	{
		Property = property;

		var persistence = Property.GetCustomAttribute<PersistenceAttribute>();

		if (persistence is null || persistence.Mode.HasFlag(PersistenceMode.Read))
			IsValid = true;

		IsReadOnly = persistence is not null && persistence.Mode.HasFlag(PersistenceMode.Read);
		IsPrimaryKey = Property.GetCustomAttribute<PrimaryKeyAttribute>() is not null;

		var memberAttribute = Property.GetCustomAttribute<MemberAttribute>();

		if (memberAttribute is not null && !string.IsNullOrWhiteSpace(memberAttribute.Member))
			Name = memberAttribute.Member;
		else
			Name = MemberInfo.Name.ToCamelCase();
	}

	private PropertyInfo Property { get; }

	public MemberInfo MemberInfo => Property;
	public bool IsValid { get; private set; }
	public bool IsPrimaryKey { get; private set; }
	public bool IsReadOnly { get; private set; }
	public string Name { get; private set; }
	public Type Type => Property.PropertyType;
}

