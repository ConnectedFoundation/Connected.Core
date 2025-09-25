using Connected.Entities;
using Connected.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Connected.Services;
public static class DtoExtensions
{
	public static IDto? Create(this IDto dto, Type type)
	{
		ArgumentNullException.ThrowIfNull(dto);

		if (ServiceExtensionsStartup.Services is null)
		{
			var provider = ServiceExtensionsStartup.ServicesCollection.BuildServiceProvider(false);

			return provider.GetRequiredService(type) as IDto;
		}

		return ServiceExtensionsStartup.Services.GetRequiredService(type) as IDto;
	}

	public static TDto Create<TDto>(this IDto dto)
		where TDto : IDto
	{
		ArgumentNullException.ThrowIfNull(dto);

		if (ServiceExtensionsStartup.Services is null)
		{
			var provider = ServiceExtensionsStartup.ServicesCollection.BuildServiceProvider(false);

			return provider.GetRequiredService<TDto>();
		}

		return ServiceExtensionsStartup.Services.GetRequiredService<TDto>();
	}

	public static IPrimaryKeyDto<T> CreatePrimaryKey<T>(this IDto dto, T id)
	{
		var result = dto.Create<IPrimaryKeyDto<T>>();

		result.Id = id;

		return result;
	}

	public static IPrimaryKeyListDto<TPrimaryKey> CreatePrimaryKeyList<TPrimaryKey>(this IDto dto, List<TPrimaryKey> items)
		where TPrimaryKey : notnull
	{
		var result = dto.Create<IPrimaryKeyListDto<TPrimaryKey>>();

		result.Items = items;

		return result;
	}

	public static IHeadListDto<TPrimaryKey> CreateHeadList<TPrimaryKey>(this IDto dto, IEnumerable<IPrimaryKeyEntity<TPrimaryKey>> items)
		where TPrimaryKey : notnull
	{
		var result = dto.Create<IHeadListDto<TPrimaryKey>>();

		result.Items = [];

		foreach (var item in items)
			result.Items.Add(item.Id);

		return result;
	}

	public static IDistributedPrimaryKeyListDto<THead, TPrimaryKey> CreateDistributedPrimaryKeyList<THead, TPrimaryKey>(this IDto dto, List<Tuple<THead, TPrimaryKey>> items)
		where THead : notnull
		where TPrimaryKey : notnull
	{
		var result = dto.Create<IDistributedPrimaryKeyListDto<THead, TPrimaryKey>>();

		result.Items = items;

		return result;
	}

	public static INameDto CreateName(this IDto dto, string name)
	{
		var result = dto.Create<INameDto>();

		result.Name = name;

		return result;
	}

	public static IEntityDto CreateEntity(this IDto dto, string entity, string entityId)
	{
		var result = dto.Create<IEntityDto>();

		result.Entity = entity;
		result.EntityId = entityId;

		return result;
	}

	public static IValueDto<T> CreateValue<T>(this IDto dto, T value)
	{
		var result = dto.Create<IValueDto<T>>();

		result.Value = value;

		return result;
	}

	public static IHeadDto<T> CreateHead<T>(this IDto dto, T value)
	{
		var result = dto.Create<IHeadDto<T>>();

		result.Head = value;

		return result;
	}

	public static IParentDto<T> CreateParent<T>(this IDto dto, T value)
	{
		var result = dto.Create<IParentDto<T>>();

		result.Parent = value;

		return result;
	}

	public static ITagListDto CreateTagList(this IDto dto, List<string> items)
	{
		var result = dto.Create<ITagListDto>();

		result.Items = items;

		return result;
	}

	public static IPatchDto<TPrimaryKey> CreatePatch<TPrimaryKey>(this IDto dto, TPrimaryKey id, Dictionary<string, object?> properties)
		where TPrimaryKey : notnull
	{
		var result = dto.Create<IPatchDto<TPrimaryKey>>();

		result.Id = id;
		result.Properties = properties;

		return result;
	}

	public static IDistributedPatchDto<THead, TPrimaryKey> CreateDistributedPatch<THead, TPrimaryKey>(this IDto dto, THead head, TPrimaryKey id, Dictionary<string, object?> properties)
		where THead : notnull
		where TPrimaryKey : notnull
	{
		var result = dto.Create<IDistributedPatchDto<THead, TPrimaryKey>>();

		result.Head = head;
		result.Id = id;
		result.Properties = properties;

		return result;
	}

	public static IDistributedPrimaryKeyDto<THead, TPrimaryKey> CreateDistributedPrimaryKey<THead, TPrimaryKey>(this IDto dto, THead head, TPrimaryKey id)
		where THead : notnull
		where TPrimaryKey : notnull
	{
		var result = dto.Create<IDistributedPrimaryKeyDto<THead, TPrimaryKey>>();

		result.Head = head;
		result.Id = id;

		return result;
	}

	public static IDistributedPrimaryKeyDto<THead, TPrimaryKey> CreateDistributedPrimaryKey<THead, TPrimaryKey>(this IDto dto, string value)
		where THead : notnull
		where TPrimaryKey : notnull
	{
		var tokens = value.Split('.');

		if (tokens.Length != 2)
			throw new ArgumentException($"{SR.ErrExpectingHeadAndKey} ({value})");

		var result = dto.Create<IDistributedPrimaryKeyDto<THead, TPrimaryKey>>();

		result.Head = Types.Convert<THead>(tokens[0]);
		result.Id = Types.Convert<TPrimaryKey>(tokens[1]);

		return result;
	}

	public static string DistributedKey<THead, TPrimaryKey>(this IDistributedPrimaryKeyDto<THead, TPrimaryKey> dto)
		where THead : notnull
		where TPrimaryKey : notnull
	{
		return $"{dto.Head}.{dto.Id}";
	}
}
