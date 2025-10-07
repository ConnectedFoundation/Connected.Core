using Connected.Entities;

namespace Connected.Collections.Queues.Serialization;

internal sealed class TypeSerializer : EntityPropertySerializer
{
	protected override async Task<object?> OnSerialize(object? value)
	{
		if (value is null)
			return null;

		if (value is not Type type)
			return null;

		return await Task.FromResult<object?>($"{type.FullName}, {type.Assembly.GetName().Name}");
	}

	protected override async Task<object?> OnDeserialize(object? value)
	{
		if (value is null)
			return null;

		if (value is not string s)
			return null;

		return await Task.FromResult<object?>(Type.GetType(s));
	}
}