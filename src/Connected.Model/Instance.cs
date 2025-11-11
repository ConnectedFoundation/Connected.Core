namespace Connected;

/// <summary>
/// Represents the unique identifier for the current application instance.
/// </summary>
/// <remarks>
/// This static class generates and maintains a unique GUID that identifies the current
/// application instance throughout its lifetime. The identifier is created once during
/// static initialization and remains constant until the application terminates. This is
/// useful for tracking specific application instances in distributed systems, logging,
/// or service discovery scenarios where multiple instances of the same application may
/// be running simultaneously.
/// </remarks>
public static class Instance
{
	/// <summary>
	/// Initializes static members of the <see cref="Instance"/> class.
	/// </summary>
	/// <remarks>
	/// Generates a new unique identifier for this application instance using <see cref="Guid.NewGuid"/>.
	/// </remarks>
	static Instance()
	{
		Id = Guid.NewGuid();
	}

	/// <summary>
	/// Gets the unique identifier for the current application instance.
	/// </summary>
	/// <value>
	/// A <see cref="Guid"/> that uniquely identifies this application instance.
	/// </value>
	/// <remarks>
	/// This identifier is generated once during static initialization and remains
	/// constant throughout the application's lifetime. Each time the application
	/// starts, a new unique identifier is generated.
	/// </remarks>
	public static Guid Id { get; }
}