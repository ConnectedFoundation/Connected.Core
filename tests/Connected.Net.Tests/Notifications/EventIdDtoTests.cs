using Connected.Notifications.Dtos;
using Connected.Services;
using ValidationContext = Connected.Services.Validation.ValidationContext;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel.DataAnnotations;

namespace Connected.Notifications.Tests;

/// <summary>
/// Holds what an event is raised with when the change has no key, such as clearing the identity: the id of the event
/// payload is optional, while the primary key of a request is still required.
/// </summary>
[TestClass]
public sealed class EventIdDtoTests
{
	private static Task Validate(IDto dto)
	{
		var context = new ValidationContext(null!, new ServiceCollection().BuildServiceProvider());

		return context.Validate(null!, dto);
	}

	private static InsertEventDto<string, IPrimaryKeyDto<string?>> Event(IPrimaryKeyDto<string?> payload) => new()
	{
		Sender = null!,
		Service = "service",
		Event = ServiceEvents.Updated,
		Dto = payload
	};

	[TestMethod]
	public async Task An_event_payload_without_an_id_passes_validation() =>
		await Validate(Event(new EventIdDto<string> { Id = null }));

	[TestMethod]
	public async Task An_event_payload_with_an_id_passes_validation() =>
		await Validate(Event(new EventIdDto<string> { Id = "token" }));

	[TestMethod]
	public async Task A_primary_key_request_without_an_id_still_fails_validation()
	{
		var ex = await Assert.ThrowsExceptionAsync<ValidationException>(() => Validate(new PrimaryKeyDto<string?> { Id = null }));

		StringAssert.Contains(ex.Message, "id");
	}

	[TestMethod]
	public void An_event_listener_is_found_by_the_primary_key_interface_of_the_payload()
	{
		var dtos = new EventIdDto<string>().GetType().GetImplementedDtos();

		CollectionAssert.AreEqual(new[] { typeof(IPrimaryKeyDto<string>) }, dtos);
	}
}
