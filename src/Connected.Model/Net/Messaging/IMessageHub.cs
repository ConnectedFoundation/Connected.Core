using Connected.Services;

namespace Connected.Net.Messaging;
public interface IMessageHub<TDto>
	where TDto : IDto
{
	Task Notify(IMessageAcknowledgeDto ack, TDto? dto);
	Task Exception(IServerExceptionDto dto);
}
