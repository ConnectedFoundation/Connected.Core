using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

namespace Connected.Hosting.Tests;

/// <summary>
/// Holds that a Connected application can run nested inside another process, in its own collectible load
/// context, and that it can be stopped and unloaded at will without leaving anything behind in the host.
/// </summary>
/// <remarks>
/// The application under test is <c>Connected.Hosting.Tests.App</c>, which this project builds but never references,
/// so the test process's default context never loads Connected. It has no database configured: these tests cover
/// hosting, start-up and shutdown, not storage.
/// </remarks>
[TestClass]
[DoNotParallelize]
public sealed class NestedApplicationTests
{
	private static readonly TimeSpan StartTimeout = TimeSpan.FromSeconds(60);

	[TestMethod]
	[Timeout(300_000)]
	public async Task StartsStopsAndUnloadsRepeatedly()
	{
		for (var round = 1; round <= 2; round++)
		{
			var (context, started) = await RunAndStop();

			Assert.IsTrue(started, $"Round {round}: the nested application did not start.");
			Assert.IsTrue(await Collected(context), $"Round {round}: the load context was not collected after unloading.");
		}
	}

	[TestMethod]
	[Timeout(300_000)]
	public async Task LoadsNothingIntoTheDefaultContext()
	{
		var (context, started) = await RunAndStop();

		Assert.IsTrue(started);
		Assert.IsTrue(await Collected(context));
		string[] connected = ["Connected", "Connected.Extensions", "Connected.Model", "Connected.Authorization", "Connected.Sdk", "Connected.Hosting.Tests.App"];
		var leaked = AssemblyLoadContext.Default.Assemblies.Select(f => f.GetName().Name).Where(f => connected.Contains(f)).ToList();

		Assert.AreEqual(0, leaked.Count, $"Loaded into the host's default context: {string.Join(", ", leaked)}.");
	}

	[TestMethod]
	[Timeout(300_000)]
	public async Task InstallsNoProcessWideHandlers()
	{
		var unhandled = HandlerCount("UnhandledException");
		var firstChance = HandlerCount("FirstChanceException");

		if (unhandled is null || firstChance is null)
			Assert.Inconclusive("The runtime's exception events could not be inspected.");

		var host = NestedHost.Create();

		try
		{
			Assert.IsTrue(await host.Start(), "The nested application did not start.");
			Assert.AreEqual(unhandled, HandlerCount("UnhandledException"), "The nested application subscribed to UnhandledException.");
			Assert.AreEqual(firstChance, HandlerCount("FirstChanceException"), "The nested application subscribed to FirstChanceException.");
		}
		finally
		{
			await host.Stop();
		}
	}

	[TestMethod]
	[Timeout(300_000)]
	public async Task ConfigurationOverridesWinOverTheHostEnvironment()
	{
		Environment.SetEnvironmentVariable("hosting__test__value", "from-environment");

		var host = NestedHost.Create();

		try
		{
			host.Overrides["hosting:test:value"] = "from-options";

			Assert.IsTrue(await host.Start(), "The nested application did not start.");
			Assert.AreEqual("from-options", host.Read("hosting:test:value"));
		}
		finally
		{
			Environment.SetEnvironmentVariable("hosting__test__value", null);
			await host.Stop();
		}
	}

	[TestMethod]
	[Timeout(300_000)]
	public async Task WorkersCanBeTurnedOff()
	{
		Assert.IsTrue(await HostedServices(workers: true) > 0, "With workers on, Connected registers its background workers.");
		Assert.AreEqual(0, await HostedServices(workers: false), "With workers off, no Connected background worker is registered.");
	}

	private static async Task<int> HostedServices(bool workers)
	{
		var host = NestedHost.Create();

		try
		{
			host.Workers = workers;

			Assert.IsTrue(await host.Start(), "The nested application did not start.");

			return host.ConnectedHostedServices();
		}
		finally
		{
			await host.Stop();
		}
	}

	[TestMethod]
	[Timeout(120_000)]
	public async Task DefaultOptionsKeepStandaloneBehaviour()
	{
		var context = new ApplicationContext(NestedHost.ApplicationPath);

		try
		{
			var connected = context.LoadFromAssemblyName(new AssemblyName("Connected"));
			var options = Activator.CreateInstance(connected.GetType("Connected.ApplicationOptions", true)!)!;
			var type = options.GetType();

			Assert.IsNull(type.GetProperty("EntryAssembly")!.GetValue(options));
			Assert.IsNull(type.GetProperty("BaseDirectory")!.GetValue(options));
			Assert.IsFalse((bool)type.GetProperty("Nested")!.GetValue(options)!);
			Assert.IsFalse(((CancellationToken)type.GetProperty("Stopping")!.GetValue(options)!).CanBeCanceled);
			Assert.IsTrue((bool)type.GetProperty("Workers")!.GetValue(options)!);
		}
		finally
		{
			context.Unload();
		}

		await Task.CompletedTask;
	}

	/*
	 * Kept out of line so no local of the caller's frame holds the context alive while the test waits for it to be
	 * collected.
	 */
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static async Task<(WeakReference Context, bool Started)> RunAndStop()
	{
		var host = NestedHost.Create();
		var started = await host.Start();

		await host.Stop();

		return (host.Context, started);
	}

	private static async Task<bool> Collected(WeakReference context)
	{
		for (var i = 0; i < 50 && context.IsAlive; i++)
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
			await Task.Delay(100);
		}

		return !context.IsAlive;
	}

	private static int? HandlerCount(string name)
	{
		var field = typeof(AppContext).GetField(name, BindingFlags.NonPublic | BindingFlags.Static);

		if (field is null)
			return null;

		return (field.GetValue(null) as Delegate)?.GetInvocationList().Length ?? 0;
	}

	/// <summary>
	/// Starts the test application nested, the way a production host would, through reflection only.
	/// </summary>
	private sealed class NestedHost
	{
		private readonly ApplicationContext _context;
		private readonly CancellationTokenSource _stopping = new();
		private Task? _running;
		private PropertyInfo? _hasStarted;

		private NestedHost(ApplicationContext context)
		{
			_context = context;
			Context = new WeakReference(context);
		}

		public static string ApplicationPath
		{
			get
			{
				var path = typeof(NestedApplicationTests).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
					.First(f => f.Key == "NestedApplication").Value!;

				return Path.GetFullPath(path.Replace('\\', Path.DirectorySeparatorChar));
			}
		}

		public WeakReference Context { get; }

		public Dictionary<string, string?> Overrides { get; } = [];

		public bool Workers { get; set; } = true;

		/// <summary>Counts the hosted services of the running nested application that come from Connected.</summary>
		public int ConnectedHostedServices()
		{
			var startup = _context.LoadFromAssemblyName(new AssemblyName("Connected.Extensions")).GetType("Connected.Services.ServiceExtensionsStartup", true)!;
			var services = (IServiceProvider)startup.GetProperty("Services")!.GetValue(null)!;
			var hosted = (IEnumerable<object>)services.GetService(typeof(IEnumerable<Microsoft.Extensions.Hosting.IHostedService>))!;

			return hosted.Count(f => f.GetType().Assembly.GetName().Name?.StartsWith("Connected", StringComparison.Ordinal) == true);
		}

		public static NestedHost Create() => new(new ApplicationContext(ApplicationPath));

		/// <summary>Reads a configuration value as the running nested application sees it.</summary>
		public string? Read(string key)
		{
			var startup = _context.LoadFromAssemblyName(new AssemblyName("Connected.Extensions")).GetType("Connected.Services.ServiceExtensionsStartup", true)!;
			var services = (IServiceProvider)startup.GetProperty("Services")!.GetValue(null)!;
			var configuration = (Microsoft.Extensions.Configuration.IConfiguration)services.GetService(typeof(Microsoft.Extensions.Configuration.IConfiguration))!;

			return configuration[key];
		}

		public async Task<bool> Start()
		{
			var entry = _context.LoadFromAssemblyPath(ApplicationPath);
			var connected = _context.LoadFromAssemblyName(new AssemblyName("Connected"));
			var application = connected.GetType("Connected.Application", true)!;
			var optionsType = connected.GetType("Connected.ApplicationOptions", true)!;
			var options = Activator.CreateInstance(optionsType)!;

			optionsType.GetProperty("EntryAssembly")!.SetValue(options, entry);
			optionsType.GetProperty("BaseDirectory")!.SetValue(options, Path.GetDirectoryName(ApplicationPath));
			optionsType.GetProperty("Nested")!.SetValue(options, true);
			optionsType.GetProperty("Stopping")!.SetValue(options, _stopping.Token);
			optionsType.GetProperty("Workers")!.SetValue(options, Workers);

			var configuration = (IDictionary<string, string?>)optionsType.GetProperty("Configuration")!.GetValue(options)!;

			foreach (var (key, value) in Overrides)
				configuration[key] = value;

			_hasStarted = application.GetProperty("HasStarted");

			var start = application.GetMethod("Start", [typeof(string[]), optionsType])!;
			var args = new[] { "--urls", $"http://127.0.0.1:{FreePort()}" };

			using (_context.EnterContextualReflection())
				_running = (Task)start.Invoke(null, [args, options])!;

			var deadline = DateTime.UtcNow + StartTimeout;

			while (DateTime.UtcNow < deadline && !_running.IsCompleted)
			{
				if ((bool)_hasStarted!.GetValue(null)!)
					return true;

				await Task.Delay(100);
			}

			return false;
		}

		public async Task Stop()
		{
			await _stopping.CancelAsync();

			if (_running is not null)
				await _running.WaitAsync(TimeSpan.FromSeconds(30));

			_running = null;
			_hasStarted = null;
			_context.Unload();
		}

		private static int FreePort()
		{
			using var listener = new TcpListener(IPAddress.Loopback, 0);

			listener.Start();

			return ((IPEndPoint)listener.LocalEndpoint).Port;
		}
	}

	/// <summary>
	/// A collectible context resolving the application's dependencies from its own directory.
	/// </summary>
	/// <remarks>
	/// Microsoft.Data.SqlClient subscribes to the default context's Resolving event from a static constructor, which
	/// would keep any context it is loaded into alive; a host shares it from the default context instead.
	/// </remarks>
	private sealed class ApplicationContext(string applicationPath)
		: AssemblyLoadContext(isCollectible: true)
	{
		private readonly AssemblyDependencyResolver _resolver = new(applicationPath);

		static ApplicationContext()
		{
			var resolver = new AssemblyDependencyResolver(NestedHost.ApplicationPath);

			Default.Resolving += (context, name) => resolver.ResolveAssemblyToPath(name) is { } path ? context.LoadFromAssemblyPath(path) : null;
		}

		protected override Assembly? Load(AssemblyName name)
		{
			if (_resolver.ResolveAssemblyToPath(name) is not { } path)
				return null;

			if (name.Name?.StartsWith("Microsoft.Data.SqlClient", StringComparison.Ordinal) == true)
				return Default.Assemblies.FirstOrDefault(f => f.GetName().Name == name.Name) ?? Default.LoadFromAssemblyPath(path);

			return LoadFromAssemblyPath(path);
		}

		protected override IntPtr LoadUnmanagedDll(string name) => _resolver.ResolveUnmanagedDllToPath(name) is { } path ? LoadUnmanagedDllFromPath(path) : IntPtr.Zero;
	}
}
