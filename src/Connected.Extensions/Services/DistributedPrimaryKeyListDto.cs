using Connected.Annotations;

namespace Connected.Services;

internal sealed class DistributedPrimaryKeyListDto<THead, TPrimaryKey>
	: Dto, IDistributedPrimaryKeyListDto<THead, TPrimaryKey>
	where THead : notnull
	where TPrimaryKey : notnull
{
	[NonDefault]
	public required List<Tuple<THead, TPrimaryKey>> Items { get; set; }
}
