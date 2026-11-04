using Connected.Annotations;
using Connected.Services;

namespace Connected.Notifications;

[ServiceRegistration(ServiceRegistrationMode.Manual)]
internal sealed class EventIdDto<T>
	 : Dto, IPrimaryKeyDto<T?>
{
	public T? Id { get; set; }
}