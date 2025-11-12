using Connected.Annotations;
using System.Collections.Immutable;

namespace Connected.Services;

internal class PrimaryKeyListDto<TPrimaryKey> : Dto, IPrimaryKeyListDto<TPrimaryKey>
	where TPrimaryKey : notnull
{
	[NonDefault]
	public required List<TPrimaryKey> Items { get; set; }

	public static implicit operator PrimaryKeyListDto<TPrimaryKey>(ImmutableList<TPrimaryKey> value)
	{
		return new PrimaryKeyListDto<TPrimaryKey>
		{
			Items = [.. value]
		};
	}

	public static implicit operator PrimaryKeyListDto<TPrimaryKey>(List<TPrimaryKey>? value)
	{
		return new PrimaryKeyListDto<TPrimaryKey>
		{
			Items = value ?? []
		};
	}
}
