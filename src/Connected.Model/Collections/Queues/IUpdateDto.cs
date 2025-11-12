using Connected.Services;

namespace Connected.Collections.Queues;

/// <summary>
/// DTO used to update queue message metadata such as visibility timeout.
/// </summary>
public interface IUpdateDto : IValueDto<Guid>
{
	/// <summary>
	/// Gets or sets the visibility timeout after which the message becomes visible again.
	/// </summary>
	TimeSpan NextVisible { get; set; }
}
