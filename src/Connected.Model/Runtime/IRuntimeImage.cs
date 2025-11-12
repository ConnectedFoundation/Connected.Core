using System.Collections.Immutable;

namespace Connected.Runtime;

/// <summary>
/// Represents a runtime image that can be registered and loaded into the application.
/// </summary>
/// <remarks>
/// A runtime image encapsulates a modular component of the application with its own
/// dependencies and registration logic. Images are used to dynamically load and configure
/// features, modules, or plugins at runtime. Each image maintains a list of dependencies
/// that must be satisfied before the image can be successfully loaded. This enables
/// modular application architectures where functionality can be added or removed based
/// on deployment requirements or runtime conditions.
/// </remarks>
public interface IRuntimeImage
{
	/// <summary>
	/// Registers the runtime image with the application.
	/// </summary>
	/// <remarks>
	/// This method performs the necessary registration steps to integrate the image
	/// into the application's runtime environment. This typically includes service
	/// registrations, middleware configuration, and initialization of image-specific
	/// resources. Implementations should ensure that all dependencies are available
	/// before attempting registration.
	/// </remarks>
	void Register();

	/// <summary>
	/// Gets the collection of dependency identifiers required by this runtime image.
	/// </summary>
	/// <value>
	/// An immutable list of strings representing the identifiers of dependencies that
	/// must be present before this image can be loaded.
	/// </value>
	/// <remarks>
	/// Dependencies are typically other runtime images or system components that this
	/// image relies on. The runtime uses this information to determine the correct
	/// loading order and to validate that all required dependencies are available
	/// before registration.
	/// </remarks>
	IImmutableList<string> Dependencies { get; }
}
