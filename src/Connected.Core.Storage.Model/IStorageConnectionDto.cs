using Connected.Services;

namespace Connected.Storage;

public interface IStorageConnectionDto : IDto
{
	string? ConnectionString { get; set; }

	StorageConnectionMode Mode { get; set; }
}
