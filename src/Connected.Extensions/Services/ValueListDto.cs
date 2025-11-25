using Connected.Annotations;

namespace Connected.Services;

internal class ValueListDto<TPrimaryKey> : Dto, IValueListDto<TPrimaryKey>
{
	[NonDefault, SkipValidation]
	public required List<TPrimaryKey> Items { get; set; } = [];
}
