using Connected.Annotations;
using System.Collections.Immutable;

namespace Connected.Services;

internal class HeadListDto<TPrimaryKey> : Dto, IHeadListDto<TPrimaryKey>
	where TPrimaryKey : notnull
{
	[NonDefault]
	public required List<TPrimaryKey> Items { get; set; }

	public static implicit operator HeadListDto<TPrimaryKey>(ImmutableList<TPrimaryKey> value)
	{
		return new HeadListDto<TPrimaryKey>
		{
			Items = [.. value]
		};
	}

	public static implicit operator HeadListDto<TPrimaryKey>(List<TPrimaryKey>? value)
	{
		return new HeadListDto<TPrimaryKey>
		{
			Items = value ?? []
		};
	}
}
