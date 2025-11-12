using System.ComponentModel.DataAnnotations;

namespace Connected.Services;

public sealed class OrderByDescriptor : IOrderByDescriptor
{
	[Required, MaxLength(128)]
	public string Property { get; set; } = default!;

	public OrderByMode Mode { get; set; } = OrderByMode.Ascending;
}
