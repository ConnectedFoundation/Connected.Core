using Connected.Annotations;

namespace Connected.Services;

internal class TagListDto : Dto, ITagListDto
{
	[NonDefault]
	public required List<string> Items { get; set; }
}
