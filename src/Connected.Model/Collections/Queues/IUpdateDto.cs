using Connected.Services;

namespace Connected.Collections.Queues;
public interface IUpdateDto : IValueDto<Guid>
{
	TimeSpan NextVisible { get; set; }
}
