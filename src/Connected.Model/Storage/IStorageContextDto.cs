using Connected.Services;

namespace Connected.Storage;

public interface IStorageContextDto : IDto
{
	public IStorageOperation Operation { get; set; }

	public StorageConnectionMode ConnectionMode { get; set; }
}
