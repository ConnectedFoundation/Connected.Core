using Connected.Services;

namespace Connected.Storage.Containers;

/// <summary>
/// Names a storage container beside the default storage.
/// </summary>
public interface IStorageContainerDto
	: IDto
{
	/// <summary>
	/// Gets or sets the container's name: letters, digits and underscores, starting with a letter.
	/// </summary>
	string Name { get; set; }
}
