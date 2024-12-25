using Connected.Services;

namespace Connected.Notifications;

public interface IInsertEventDto<TService, TDto> : IDto
	where TDto : IDto
{
	IOperationState Sender { get; set; }

	TService Service { get; set; }

	string Event { get; set; }

	TDto Dto { get; set; }
}