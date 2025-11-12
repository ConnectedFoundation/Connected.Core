using Connected.Services;

namespace Connected.Notifications.Dtos;
internal sealed class InsertEventDto<TService, TDto> : IInsertEventDto<TService, TDto>
	where TDto : IDto
{
	public required IOperationState Sender { get; set; }
	public required TService Service { get; set; }
	public required string Event { get; set; }
	public required TDto Dto { get; set; }
}
