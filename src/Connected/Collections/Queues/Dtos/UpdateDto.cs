using Connected.Annotations;
using Connected.Services;

namespace Connected.Collections.Queues.Dtos;
internal sealed class UpdateDto : Dto, IUpdateDto
{
	public TimeSpan NextVisible { get; set; }

	[NonDefault]
	public Guid Value { get; set; }
}
