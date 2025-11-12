using Connected.Annotations.Entities;
using Connected.Entities;
using Connected.Reflection;
using System.Data;
using System.Reflection;

namespace Connected.Storage;
/// <summary>
/// Performs mapping between <see cref="IDataReader"/> fields and entity properties.
/// </summary>
/// <typeparam name="TEntity">The entity type to be used.</typeparam>
internal class FieldMappings<TEntity>
{
	/// <summary>
	/// Creates a new <see cref="FieldMappings{TEntity}"/> object.
	/// </summary>
	/// <param name="reader">The <see cref="IDataReader"/> providing entity records.</param>
	public FieldMappings(IDataReader reader)
	{
		Properties = [];

		Initialize(reader);
	}
	/// <summary>
	/// Cached properties use when looping through the records.
	/// </summary>
	private Dictionary<int, PropertyContext> Properties { get; }
	/// <summary>
	/// Initializes the mappings base on the provided <see cref="IDataReader"/>
	/// and <typeparamref name="TEntity"/>
	/// </summary>
	/// <param name="reader">The active reader containing records.</param>
	private void Initialize(IDataReader reader)
	{
		/*
		 * For primitive types there are no mappings since it's an scalar call.
		 */
		if (typeof(TEntity).IsTypePrimitive())
			return;
		/*
		 * We are binding only properties, not fields.
		 */
		var properties = typeof(TEntity).GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

		for (var i = 0; i < reader.FieldCount; i++)
		{
			if (FieldMappings<TEntity>.ResolveProperty(properties, reader.GetName(i)) is PropertyInfo property)
				Properties.Add(i, new PropertyContext(property, property.ResolveEntityPropertySerializer()));
		}
	}
	/// <summary>
	/// Creates a new instance of the <see cref="TEntity"/> and binds
	/// properties from the provided <see cref="IDataReader"/>.
	/// </summary>
	/// <param name="reader">The <see cref="IDataReader"/> containing the record.</param>
	/// <returns>A new instance of the <see cref="TEntity"/> with bound values from the <see cref="IDataReader"/>.</returns>
	public async Task<TEntity?> CreateInstance(IDataReader reader, CancellationToken cancel)
	{
		/*
		 * For primitive return values we'll use only the first field and return itž
		 * to the caller.
		 */
		if (typeof(TEntity).IsTypePrimitive())
		{
			if (reader.FieldCount == 0)
				return default;

			return (TEntity?)reader[0];
		}
		/*
		 * It's an actual entity. First, create a new instance. Entities should have
		 * public parameterless constructor.
		 */
		if (typeof(TEntity?).CreateInstance<TEntity>() is not TEntity instance)
			throw new NullReferenceException(typeof(TEntity).FullName);

		foreach (var property in Properties)
			await Bind(instance, property, reader, cancel);

		return instance;
	}
	/// <summary>
	/// Resolves a correct property from the entity's properties based on a <see cref="IDataReader"/> field name.
	/// </summary>
	/// <param name="properties">The entity's properties.</param>
	/// <param name="name">The <see cref="IDataReader"/> field name.</param>
	/// <returns>A <see cref="PropertyInfo"/> if found, <c>null</c> otherwise.</returns>
	private static PropertyInfo? ResolveProperty(PropertyInfo[] properties, string name)
	{
		/*
		 * There are two ways to map a property (evaluated in the following order):
		 * 1. from property name
		 * 2. from MemberAttribute
		 * 
		 * We'll first perform case insensitive comparison because fields in the database are usually stored in a camelCase format.
		 */
		if (properties.FirstOrDefault(f => string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase)) is PropertyInfo property && property.CanWrite)
		{
			/*
			 * Property is found, examine if the persistence from the storage is supported.
			 */
			var att = property.FindAttribute<PersistenceAttribute>();

			if (att is null || att.Mode.HasFlag(PersistenceMode.Read))
				return property;
		}
		/*
		 * Property wasn't found, let's try to find it via MemberAttribute.
		 */
		foreach (var prop in properties)
		{
			/*
			 * It's case insensitive comparison again because we don't want bother user with exact matching. Since a database is probably case insensitive anyway
			 * there is no option to store columns with case sensitive names.
			 */
			if (prop.FindAttribute<MemberAttribute>() is MemberAttribute nameAttribute && string.Compare(nameAttribute.Member, name, true) == 0 && prop.CanWrite)
				return prop;
		}
		/*
		 * Property could't be found. The field will be ognored when reading data.
		 */
		return default;
	}
	/// <summary>
	/// Binds the <see cref="IDataReader"/> value to the entity's property.
	/// </summary>
	/// <param name="instance">The instance of the entity.</param>
	/// <param name="property">The property on which value to be set.</param>
	/// <param name="reader">The <see cref="IDataReader"/>providing the value.</param>
	private static async Task Bind(object instance, KeyValuePair<int, PropertyContext> property, IDataReader reader, CancellationToken cancel)
	{
		var value = reader.GetValue(property.Key);

		if (property.Value.Serializer is not null)
		{
			if (instance is not IEntity entity)
				throw new InvalidOperationException($"{Strings.ErrInterfaceExpected} ('{instance.GetType()}, {nameof(IEntity)}')");

			value = await property.Value.Serializer.Deserialize(entity, value, cancel);
		}
		/*
		 * We won't bind null values. We'll leave the property as is.
		 */
		if (value is null || Convert.IsDBNull(value))
			return;
		/*
		 * We have a few exceptions when binding values.
		 */
		var prop = property.Value.Property;
		var type = prop.PropertyType;

		if (type == typeof(string) && value is byte[] bv)
		{
			/*
			 * If the property is string and the reader's value is byte array we are probably dealing
			 * with Consistency field. We'll first check if the property contains the attribute. If so,
			 * we'll convert byte array to eTag kind of value. If not we'll simply convert value to base64
			 * string.
			 */
			if (prop.FindAttribute<ETagAttribute>() is not null)
			{
				var versionValue = (EntityVersion?)bv;

				if (versionValue is null)
					value = Convert.ToBase64String(bv);
				else
					value = versionValue.ToString();
			}
			else
				value = Convert.ToBase64String(bv);
		}
		else if (type == typeof(DateTimeOffset))
		{
			/*
			 * We don't perform any conversions on dates. All dates should be stored in a UTC
			 * format so we simply set the correct kind of date so it can be later correctly
			 * converted
			 */
			if (value is DateTime time)
				value = new DateTimeOffset(DateTime.SpecifyKind(time, DateTimeKind.Utc));
		}
		else if (type == typeof(DateTime))
		{
			/*
			 * Like DateTimeOffset, the same is true for DateTime values
			 */
			if (value is DateTimeOffset offset)
				value = DateTime.SpecifyKind(offset.DateTime, DateTimeKind.Utc);
			else if (value is DateTime dateTime)
				value = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
		}
		else
		{
			/*
			 * For other values we just perform a conversion.
			 */
			value = Types.Convert(value, type);
		}
		/*
		 * Now bind the property from the converted value.
		 */
		prop.SetValue(instance, value);
	}
}
