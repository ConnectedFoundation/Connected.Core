using Connected.Annotations;
using Connected.Services;
using System.ComponentModel.DataAnnotations;

namespace Connected.Storage.Containers.Dtos;

/// <inheritdoc cref="IStorageContainerDto"/>
internal sealed class StorageContainerDto
	: Dto, IStorageContainerDto
{
	/// <inheritdoc/>
	/// <remarks>
	/// Restricted to characters every provider accepts in a container name without quoting, so a name can never
	/// reach a provider's commands as anything but a name.
	/// </remarks>
	[NonDefault, MaxLength(StorageContainerProvider.NameMaxLength), RegularExpression(StorageContainerProvider.NamePattern)]
	public required string Name { get; set; }
}
