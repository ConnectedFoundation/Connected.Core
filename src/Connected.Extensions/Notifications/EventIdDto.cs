using Connected.Annotations;
using Connected.Services;

namespace Connected.Notifications;

/// <summary>
/// The payload of an event, carrying the id of the entity it was raised for.
/// </summary>
/// <remarks>
/// Unlike <see cref="PrimaryKeyDto{T}"/>, whose id is required for requests, the id is optional, so an event can be
/// raised for a change that has no key. It is exposed as <c>IPrimaryKeyDto&lt;T&gt;</c>, which is the interface the
/// listeners are declared on, and for a reference type that is the same interface the listeners already use. A
/// listener therefore receives a null <see cref="Id"/> that it is typed to treat as not null, and has to check it
/// before using it. Only reference type ids can be missing: for a value type the id is its default value, which is
/// delivered as a real key.
/// </remarks>
/// <typeparam name="T">The type of the id.</typeparam>
[ServiceRegistration(ServiceRegistrationMode.Manual)]
internal sealed class EventIdDto<T>
	 : Dto, IPrimaryKeyDto<T?>
{
	/// <summary>
	/// Gets or sets the id of the entity the event was raised for, or <c>null</c> when the change has no key.
	/// </summary>
	public T? Id { get; set; }
}