using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Connected.Reflection.Merging;

internal abstract class Merger
{
	protected static bool IsArray(PropertyInfo property)
	{
		var fullName = typeof(IEnumerable).FullName;

		if (fullName is null)
			return false;

		return property.PropertyType.IsArray || property.PropertyType.GetInterface(fullName) is not null;
	}

	protected static List<object> GetElements(object collection)
	{
		IEnumerator? en = null;

		if (collection is Array array)
			en = array.GetEnumerator();
		else if (collection is IEnumerable enm)
			en = enm.GetEnumerator();

		var result = new List<object>();

		if (en is null)
			return result;

		while (en.MoveNext())
			result.Add(en.Current);

		return result;
	}
}
