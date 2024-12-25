using Connected.Services;

namespace Connected.Collections.Queues;
public interface IQueryDto : IDto
{
	int MaxCount { get; set; }
	TimeSpan NextVisible { get; set; }
	int? Priority { get; set; }
	string Queue { get; set; }
}

