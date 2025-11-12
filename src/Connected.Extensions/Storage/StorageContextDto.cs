using Connected.Annotations;
using Connected.Services;

namespace Connected.Storage;
internal sealed class StorageContextDto : Dto, IStorageContextDto
{
	[NonDefault, SkipValidation]
	public IStorageOperation Operation { get; set; } = default!;
	public StorageConnectionMode ConnectionMode { get; set; } = StorageConnectionMode.Shared;
}
