using Connected.Net.Events.Dtos;

namespace Connected.Net.Events;
internal static class EventExtensions
{
	public static string Key(this ISubscribeEventDto dto) => $"{dto.Service}/{dto.Event}".ToLowerInvariant();
	public static string Key(this ISendEventDto dto) => $"{dto.Service}/{dto.Event}".ToLowerInvariant();
}
