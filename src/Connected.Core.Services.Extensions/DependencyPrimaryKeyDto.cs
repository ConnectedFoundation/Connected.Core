using Connected.Annotations;
using System.ComponentModel.DataAnnotations;

namespace Connected.Services;

internal sealed class DependencyPrimaryKeyDto<THead, TPrimaryKey>
	: Dto, IDependencyPrimaryKeyDto<THead, TPrimaryKey>
	where THead : notnull
	where TPrimaryKey : notnull
{
	[Required, MinValue(1)]
	public required THead Head { get; set; }

	[Required, MinValue(1)]
	public required TPrimaryKey Id { get; set; }
}
