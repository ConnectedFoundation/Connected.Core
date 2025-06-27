using Connected.Annotations;
using System.ComponentModel.DataAnnotations;

namespace Connected.Services;

internal sealed class DependentPrimaryKeyDto<THead, TPrimaryKey>
	: Dto, IDependentPrimaryKeyDto<THead, TPrimaryKey>
	where THead : notnull
	where TPrimaryKey : notnull
{
	[Required, MinValue(1)]
	public required THead Head { get; set; }

	[Required, MinValue(1)]
	public required TPrimaryKey Id { get; set; }
}
