using Connected.Annotations;
using Connected.Services;
using System.ComponentModel.DataAnnotations;

namespace Connected.Collections.Queues.Dtos;
internal sealed class QueryDto : Dto, IQueryDto
{
	[MinValue(1)]
	public int MaxCount { get; set; }
	public TimeSpan NextVisible { get; set; }
	public int? Priority { get; set; }

	[Required, MaxLength(128)]
	public required string Queue { get; set; }
}
