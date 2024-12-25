using Connected.Services;

namespace Connected.Annotations;
/// <summary>
/// Specifies that a Dto has a custom binder.
/// </summary>
/// <remarks>
/// If a Dto cannot be bound directly it's recommended that service operations decorate
/// arguments with a custom binder which performs a binding based on a request parameters.
/// </remarks>
/// <typeparam name="TBinder">The binder that should be used when binding request parameters to Dto. TBinder
/// should implement IDtoBinder interface.</typeparam>
[AttributeUsage(AttributeTargets.Class)]
public sealed class DtoBinderAttribute<TBinder> : Attribute where TBinder : IDtoBinder
{

}