using Connected.Services;

namespace Connected.Storage;
internal sealed class StorageConnectionDto : Dto, IStorageConnectionDto
{
	public string? ConnectionString { get; set; }
	public StorageConnectionMode Mode { get; set; } = StorageConnectionMode.Shared;
}
