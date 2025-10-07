using Connected.Services;

namespace Connected.Collections.Queues;
public interface IInsertOptionsDto : IDto
{
	DateTimeOffset Expire { get; set; }
	string? Batch { get; set; }
	int Priority { get; set; }
	DateTimeOffset? NextVisible { get; set; }
	string Queue { get; set; }
	int MaxDequeueCount { get; set; }
}

