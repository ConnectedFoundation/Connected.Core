using Connected.Services;

namespace Connected.Reflection;

public static class DtoExtensions
{
	public static TDto AsDto<TDto>(this IDto dto, params object[] sources) where TDto : IDto
	{
		var instance = typeof(TDto).CreateInstance<TDto>();
		var result = Serializer.Merge(instance, sources.ToArray(), dto);

		if (result is null)
			throw new NullReferenceException("Couldn't merge Dto");

		return result;
	}
}
