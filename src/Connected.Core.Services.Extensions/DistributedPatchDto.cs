using Connected.Annotations;

namespace Connected.Services;

internal class DistributedPatchDto<THead, TPrimaryKey> : PatchDto<TPrimaryKey>, IPatchDto<TPrimaryKey>, IDistributedPatchDto<THead, TPrimaryKey>
	where THead : notnull
	where TPrimaryKey : notnull
{
	[MinValue(1)]
	public required THead Head { get; set; }
}
