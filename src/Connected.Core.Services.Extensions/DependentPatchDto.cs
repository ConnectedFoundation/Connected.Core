using Connected.Annotations;

namespace Connected.Services;

internal class DependentPatchDto<THead, TPrimaryKey> : PatchDto<TPrimaryKey>, IPatchDto<TPrimaryKey>, IDependentPatchDto<THead, TPrimaryKey>
	where THead : notnull
	where TPrimaryKey : notnull
{
	[MinValue(1)]
	public required THead Head { get; set; }
}
