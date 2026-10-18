using Connected.Entities;

namespace Connected.Storage.Containers;

/// <summary>
/// The entity type container provider resolution asks storage connection providers about.
/// </summary>
/// <remarks>
/// It has no table, so schema synchronization never persists it; it only lets connection providers propose the
/// connection type they would use for any entity.
/// </remarks>
internal sealed record ContainerEntity
	: Entity<int>
{
}
