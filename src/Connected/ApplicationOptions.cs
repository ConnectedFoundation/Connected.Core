using System.Reflection;

namespace Connected;

/// <summary>
/// Options for starting an application, for hosts that run it as something other than the process's only
/// application.
/// </summary>
/// <remarks>
/// The defaults reproduce <see cref="Application.Start(string[])"/> exactly. A nested host loads the application into
/// its own collectible <see cref="System.Runtime.Loader.AssemblyLoadContext"/>, sets <see cref="Nested"/>, and stops it
/// through <see cref="Stopping"/>; because every assembly is then loaded into that context and no process-wide handler
/// is installed, the context can be unloaded once <see cref="Application.Start(string[], ApplicationOptions)"/> returns.
/// </remarks>
public sealed class ApplicationOptions
{
	/// <summary>
	/// Gets or sets the assembly to treat as the application's entry assembly, or <see langword="null"/> for
	/// <see cref="Assembly.GetEntryAssembly"/>. A nested host's entry assembly is the hosting process's, never its own.
	/// </summary>
	public Assembly? EntryAssembly { get; set; }

	/// <summary>
	/// Gets or sets the directory configured dependencies and images are loaded from, or <see langword="null"/> for the
	/// process's base directory.
	/// </summary>
	public string? BaseDirectory { get; set; }

	/// <summary>
	/// Gets or sets whether the application runs nested inside another application. A nested application installs no
	/// process-wide exception handlers, so it can never exit the process or replace it with the error server; its
	/// failures surface as exceptions from <see cref="Application.Start(string[], ApplicationOptions)"/>.
	/// </summary>
	public bool Nested { get; set; }

	/// <summary>
	/// Gets or sets a token that stops the application. When it is cancelled the host shuts down gracefully and
	/// <see cref="Application.Start(string[], ApplicationOptions)"/> returns.
	/// </summary>
	public CancellationToken Stopping { get; set; }
}
