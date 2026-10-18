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
	[NonDefault, MaxLength(63), RegularExpression("^[A-Za-z][A-Za-z0-9_]*$")]
	public required string Name { get; set; }
}
