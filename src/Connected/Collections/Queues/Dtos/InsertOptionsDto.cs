using Connected.Annotations;
using Connected.Annotations.Entities;
using Connected.Services;
using System.ComponentModel.DataAnnotations;

namespace Connected.Collections.Queues.Dtos;
internal sealed class InsertOptionsDto : Dto, IInsertOptionsDto
{
	[NonDefault, Date(DateKind.DateTime2, 7)]
	public DateTimeOffset Expire { get; set; }

	[MaxLength(256)]
	public string? Batch { get; set; }

	public int Priority { get; set; }

	[Date(DateKind.DateTime2, 7)]
	public DateTimeOffset? NextVisible { get; set; }

	[Required, MaxLength(128)]
	public required string Queue { get; set; }

	public int MaxDequeueCount { get; set; }
}
