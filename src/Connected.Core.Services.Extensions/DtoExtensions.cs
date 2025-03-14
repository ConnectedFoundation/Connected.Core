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

	public static IPatchDto<TPrimaryKey> CreatePatch<TPrimaryKey>(this IDto dto, TPrimaryKey id, Dictionary<string, object?> properties)
		where TPrimaryKey : notnull
	{
		var result = dto.Create<IPatchDto<TPrimaryKey>>();

		result.Id = id;
		result.Properties = properties;

		return result;
	}
}
