using Connected.Entities;
using Connected.Reflection;
using Connected.Services;
using System.Text.Json;

namespace Connected.Collections.Queues.Serialization;

internal sealed class DtoSerializer : EntityPropertySerializer
{
	protected override async Task<object?> OnSerialize(object? value)
	{
		if (value is null)
			return null;

		if (value is not IDto dto)
			return null;

		return await Task.FromResult<object?>(JsonSerializer.SerializeToUtf8Bytes(dto, dto.GetType(), JsonSerializerOptions.Default));
	}

	protected override async Task<object?> OnDeserialize(object? value)
	{
		if (value is null)
			return null;

		if (value is not byte[] bytes)
			return null;

		if (Entity is not QueueMessage entity || entity.DtoTypeName is null)
			return null;

		var dtoType = Types.GetType(entity.DtoTypeName);

		if (dtoType is null)
			return null;

		var span = new ReadOnlySpan<byte>(bytes);

		return await Task.FromResult(JsonSerializer.Deserialize(span, dtoType, JsonSerializerOptions.Default));
	}
}