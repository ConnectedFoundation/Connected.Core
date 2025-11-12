using Connected.Annotations;
using Connected.Services;

namespace Connected.Collections.Queues;
/// <summary>
/// Dto used when updating (pinging) the queue message
/// </summary>
internal sealed class UpdateDto : Dto, IUpdateDto
{
	/// <summary>
	/// The period with which message will move its NextVisible value.
	/// </summary>
	public TimeSpan NextVisible { get; set; }

	[NonDefault]
	public Guid Value { get; set; }
}