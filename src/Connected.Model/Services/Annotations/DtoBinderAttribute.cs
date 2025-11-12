namespace Connected.Services.Annotations;

/// <summary>
/// Specifies that a DTO has a custom binder.
/// </summary>
/// <typeparam name="TBinder">The type of the binder that implements <see cref="IDtoBinder"/> and is used when binding request parameters to the DTO.</typeparam>
/// <remarks>
/// If a DTO cannot be bound directly, it is recommended that service operations decorate
/// arguments with a custom binder which performs binding based on request parameters.
/// This attribute enables custom deserialization logic for DTOs that require special
/// handling beyond standard model binding.
/// </remarks>
[AttributeUsage(AttributeTargets.Class)]
public sealed class DtoBinderAttribute<TBinder>
	: Attribute
	where TBinder : IDtoBinder
{
}