using Connected.Services;

namespace Connected.Collections.Queues;
/// <summary>
/// Dto used when updating (pinging) the queue message
/// </summary>
internal sealed class UpdateDto : ValueDto<Guid>, IUpdateDto
{
	/// <summary>
	/// The period with which message will move its NextVisible value.
	/// </summary>
	public TimeSpan NextVisible { get; set; }
}