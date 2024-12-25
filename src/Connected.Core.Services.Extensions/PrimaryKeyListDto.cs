using Connected.Annotations;

namespace Connected.Services;

public class PrimaryKeyListDto<T> : Dto, IPrimaryKeyListDto<T>
{
	[NonDefault]
	public List<T> Items { get; init; } = null!;

	public static implicit operator PrimaryKeyListDto<T>(ImmutableList<T> value)
	{
		return new PrimaryKeyListDto<T>
		{
			Items = value.ToList()
		};
	}

	public static implicit operator PrimaryKeyListDto<T>(List<T>? value)
	{
		return new PrimaryKeyListDto<T>
		{
			Items = value ?? new()
		};
	}
}
