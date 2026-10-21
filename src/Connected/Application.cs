using Connected.Annotations;
using Connected.Authentication;
using Connected.Configuration;
using Connected.Globalization;
using Connected.Net;
using Connected.Net.Routing;
using Connected.Net.Routing.Dtos;
using Connected.Reflection;
using Connected.Runtime;
using Connected.Services;
using Connected.Workers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Mime;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;

namespace Connected;

public delegate void DependencyTypeDiscoveryEventHandler(object sender, DependencyTypeDiscoveryEventArgs e);
public static class Application
{
	public static event DependencyTypeDiscoveryEventHandler? DiscoverType;

	private static object _lastException = new();
	private static bool IsErrorServerStarted { get; set; }
	public static bool HasStarted { get; private set; }

	/*
	 * The options of the running application. Statics are per load context, so a nested application loaded into
	 * its own context has its own copy.
	 */
	private static ApplicationOptions Options { get; set; } = new();

	/*
	 * Assemblies load into the context Connected itself was loaded into: the default context for a standalone
	 * application, the nested host's own context otherwise, so nothing a nested application loads leaks into
	 * the hosting application.
	 */
	private static AssemblyLoadContext LoadContext => AssemblyLoadContext.GetLoadContext(typeof(Application).Assembly) ?? AssemblyLoadContext.Default;

	private static string BaseDirectory => Options.BaseDirectory ?? AppDomain.CurrentDomain.BaseDirectory;
	/*
	 * Configured dependencies and images come from the hosting application's configuration, so they resolve against
	 * its directory: a nested application loads the same plugins as its host, as its own copies in its own context.
	 */
	private static string PluginDirectory => AppDomain.CurrentDomain.BaseDirectory;

	/// <summary>Gets whether background workers are registered; see <see cref="ApplicationOptions.Workers"/>.</summary>
	internal static bool WorkersEnabled => Options.Workers;

	public static async Task Start(string[] args)
	{
		await Start(args, new ApplicationOptions());
	}

	public static async Task Start(string[] args, ApplicationOptions options)
	{
		Options = options;
		HasStarted = false;

		WebApplication? webApp = null;
		var running = false;

		Caching.Cache.Start();

		try
		{
			/*
			 * A stop requested before the start builds nothing, and one requested while it builds skips the work that
			 * only a running application needs (migrations, schedulers, external connections).
			 */
			Options.Stopping.ThrowIfCancellationRequested();
			/*
			 * A nested application shares its host's content root and so its configuration, plugins included; settings of
			 * its own come from Options.Configuration.
			 */
			var builder = WebApplication.CreateBuilder(args);

			var entryAssembly = Options.EntryAssembly ?? Assembly.GetEntryAssembly();

			if (entryAssembly is not null)
				builder.Configuration.AddUserSecrets(entryAssembly, true);

			builder.Configuration.AddEnvironmentVariables();

			if (Options.Configuration.Count > 0)
				builder.Configuration.AddInMemoryCollection(Options.Configuration);

			var services = builder.Services;

			RegisterConfiguredDependencies(builder.Configuration);
			RegisterImages(builder.Configuration);
			RegisterCoreDependencies();

			foreach (var startup in Dependencies.Startups)
				startup.Prepare(builder.Configuration);

			/*
			 * Both handlers act on the whole process; a nested application must never exit or take over its host.
			 */
			if (!Options.Nested)
			{
				AddOutOfMemoryExceptionHandler();
				AddAutoRestart();
			}

			services.AddLogging(builder => builder.AddConsole());

			builder.Services.AddGrpc(options => options.Interceptors.Add<Net.Grpc.ExceptionInterceptor>());
			builder.AddLocalization();
			builder.AddHttpServices();
			builder.AddCors();
			builder.Services.AddRouting();
			builder.ConfigureDependenciesServices();
			builder.AddDependencies();

			if (!Options.Workers)
				RemoveWorkers(services);

			services.AddSignalR(o =>
			{
				o.EnableDetailedErrors = true;
				o.DisableImplicitFromServicesParameters = true;
			});

			webApp = builder.Build();

			await OnConfigureStaticFiles(webApp);

			webApp.ActivateRequestAuthentication();
			webApp.ActivateLocalization();
			webApp.ActivateCors();
			webApp.UseRouting();
			webApp.ConfigureDependencies(builder.Environment);
			webApp.ActivateHttpServices();
			webApp.ActivateRest();

			var socketOptions = new WebSocketOptions();

			foreach (var origin in builder.Configuration.GetSection("net:sockets:allowedOrigins").Get<string[]>() ?? [])
				socketOptions.AllowedOrigins.Add(origin);

			webApp.UseWebSockets(socketOptions);

			Options.Stopping.ThrowIfCancellationRequested();
			await InitializeDependencies(webApp);
			Options.Stopping.ThrowIfCancellationRequested();
			await StartDependencies();
			/*
			 * Only while the server listens, so a caller waiting for it can send requests straight away and stops once
			 * it no longer can.
			 */
			webApp.Lifetime.ApplicationStarted.Register(() => HasStarted = true);
			webApp.Lifetime.ApplicationStopping.Register(() => HasStarted = false);
			/*
			 * RunAsync disposes the web application itself, whichever way it ends.
			 */
			running = true;

			await HostingAbstractionsHostExtensions.RunAsync(webApp, Options.Stopping);
		}
		catch (OperationCanceledException) when (Options.Stopping.IsCancellationRequested)
		{
			/*
			 * Stopped before the server finished starting, which is a normal stop.
			 */
		}
		finally
		{
			HasStarted = false;
			/*
			 * Runs on a failed start as well; otherwise the cache scavenger, the bootstrap providers and the web
			 * application stay alive, and a nested application's load context can never unload.
			 */
			await Shutdown(running ? null : webApp);

			if (IsErrorServerStarted)
			{
				while (true)
					Thread.Sleep(1000);
			}
		}
	}
	private static async Task Shutdown(WebApplication? webApp)
	{
		if (webApp is not null)
		{
			try
			{
				await webApp.DisposeAsync();
			}
			catch
			{
				/*
				 * A failed dispose must not hide the exception that stopped the application.
				 */
			}
		}
		/*
		 * The provider, the collection and the mapped gRPC services belong to this run; a later start in the same process
		 * builds its own.
		 */
		await ServiceExtensionsStartup.Reset();
		RuntimeExtensions.GrpcServices.Clear();
		/*
		 * Last, so singletons the web application and the bootstrap providers dispose still see the cache.
		 */
		Caching.Cache.Shutdown();
	}

	/*
	 * The single place background workers are turned off, so a worker registered any way at all, by discovery or
	 * directly by a startup, never runs. Other hosted services (telemetry, health checks, data protection) are part
	 * of the host and keep running.
	 */
	private static void RemoveWorkers(IServiceCollection services)
	{
		for (var i = services.Count - 1; i >= 0; i--)
		{
			var descriptor = services[i];

			if (descriptor.ServiceType != typeof(IHostedService) || descriptor.IsKeyedService)
				continue;

			if (descriptor.ImplementationFactory is { } factory)
			{
				/*
				 * A factory names no type, so what it builds is only known once it runs; a worker it builds is
				 * replaced with a hosted service that does nothing. The container does not track what a factory builds
				 * itself, so the stand-in disposes the worker when the container disposes it; a worker the factory
				 * resolved from the container is then disposed twice, which Dispose allows.
				 */
				services[i] = new ServiceDescriptor(typeof(IHostedService), sp =>
				{
					var instance = factory(sp);

					return instance is IWorker ? new IdleHostedService(instance) : instance;
				}, descriptor.Lifetime);

				continue;
			}

			var type = descriptor.ImplementationType ?? descriptor.ImplementationInstance?.GetType();

			if (type is not null && typeof(IWorker).IsAssignableFrom(type))
				services.RemoveAt(i);
		}
	}

	/// <summary>
	/// Stands in for a worker built by a factory while workers are off. It never starts the worker, and disposes it when
	/// the container disposes this service.
	/// </summary>
	private sealed class IdleHostedService(object worker) : IHostedService, IAsyncDisposable, IDisposable
	{
		public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

		public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

		public async ValueTask DisposeAsync()
		{
			if (worker is IAsyncDisposable asyncDisposable)
				await asyncDisposable.DisposeAsync();
			else if (worker is IDisposable disposable)
				disposable.Dispose();
		}

		public void Dispose()
		{
			if (worker is IDisposable disposable)
				disposable.Dispose();
			else if (worker is IAsyncDisposable asyncDisposable)
				asyncDisposable.DisposeAsync().AsTask().GetAwaiter().GetResult();
		}
	}
	/*
	 * Unsubscribed first, so a later start in the same process does not subscribe a second time.
	 */
	private static void AddOutOfMemoryExceptionHandler()
	{
		AppDomain.CurrentDomain.FirstChanceException -= OnFirstChanceException;
		AppDomain.CurrentDomain.FirstChanceException += OnFirstChanceException;
	}

	private static void AddAutoRestart()
	{
		AppDomain.CurrentDomain.UnhandledException -= OnUnhandledExceptionThrown;
		AppDomain.CurrentDomain.UnhandledException += OnUnhandledExceptionThrown;
	}

	private static void AddDependencies(this WebApplicationBuilder builder)
	{
		foreach (var dependency in Dependencies.All)
			builder.Services.AddDependency(dependency);

		builder.Services.RegisterServices();
	}

	private static void ConfigureDependenciesServices(this IHostApplicationBuilder builder)
	{
		var startups = Dependencies.Startups;

		foreach (var startup in startups)
			startup.ConfigureServices(builder.Services);
	}

	private static void ConfigureDependencies(this IApplicationBuilder builder, IWebHostEnvironment environment)
	{
		var startups = Dependencies.Startups;

		if (builder is WebApplication web)
		{
			foreach (var dependency in Dependencies.All)
				web.MapDependency(dependency);
		}

		foreach (var startup in startups)
			startup.Configure(builder, environment);
	}

	private static async Task RegisterGrpcRoutes()
	{
		await using var scope = await Scope.Create().WithSystemIdentity();
		var routing = scope.ServiceProvider.GetService<IRoutingService>();
		var configuration = scope.ServiceProvider.GetService<IConfigurationService>();
		var baseUrl = configuration?.Routing.BaseUrl;

		if (routing is null || baseUrl is null)
			return;

		foreach (var type in RuntimeExtensions.GrpcServices)
		{
			var serviceName = ResolveGrpcProxyName(type);

			if (serviceName is null)
				continue;

			var dto = Dto.Factory.Create<IInsertRouteDto>();

			dto.Protocol = RouteProtocol.Grpc;
			dto.Service = serviceName;
			dto.Url = baseUrl;

			await routing.Insert(dto);
		}

		await scope.Commit();
	}

	private static string? ResolveGrpcProxyName(Type type)
	{
		if (type.FullName is null)
			return null;

		string? serviceName = null;
		var proxyType = typeof(ServiceProxyAttribute<>);

		foreach (var att in type.GetCustomAttributes())
		{
			if (att.GetType().IsGenericType && att.GetType().GetGenericTypeDefinition() == proxyType)
			{
				serviceName = att.GetType().GetGenericArguments()[0].FullName;
				break;
			}
		}

		return serviceName;
	}

	private static async Task InitializeDependencies(IHost host)
	{
		var startups = Dependencies.Startups;

		await RegisterGrpcRoutes();

		foreach (var startup in startups)
			await startup.Initialize(host);
	}

	private static async Task StartDependencies()
	{
		var startups = Dependencies.Startups;

		foreach (var startup in startups)
			await startup.Start();
	}

	private static void OnFirstChanceException(object? sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
	{
		if (e.Exception is OutOfMemoryException)
		{
			Console.WriteLine("Out of memory exception caught, shutting down");
			Environment.Exit(101);
		}
	}

	private static void OnUnhandledExceptionThrown(object sender, UnhandledExceptionEventArgs e)
	{
		StartErrorServer(e.ExceptionObject);
	}

	private static void StartErrorServer(object exception)
	{
		_lastException = exception;

		if (IsErrorServerStarted)
			return;

		IsErrorServerStarted = true;

		var app = WebApplication.CreateBuilder().Build();

		app.MapGet("/shutdown", () => { Environment.Exit(100); });
		app.MapGet("{**catchAll}", (httpContext) =>
		{
			var html = $"""
				<a href="/shutdown">Shutdown instance</a>
				<div>Error starting application:</div>
				<div><code>{_lastException}</code></div>
				""";

			httpContext.Response.ContentType = MediaTypeNames.Text.Html;
			httpContext.Response.ContentLength = Encoding.UTF8.GetByteCount(html);
			httpContext.Response.StatusCode = 500;

			return httpContext.Response.WriteAsync(html);

		});

		app.Run();
	}

	private static void RegisterDependency(Assembly assembly)
	{
		Dependencies.Register(assembly);
	}

	private static void RegisterDependency(string assemblyName)
	{
		RegisterDependency(BaseDirectory, assemblyName);
	}

	private static void RegisterDependency(string directory, string assemblyName)
	{
		Dependencies.Register(Load(Path.Combine(directory, assemblyName)));
	}

	/*
	 * An assembly already in the context is used as it is: a second build with the same name cannot be loaded into one
	 * context, and the first one is what everything bound so far uses.
	 */
	private static Assembly Load(string fileName)
	{
		var name = AssemblyName.GetAssemblyName(fileName);

		if (Loaded(name) is { } loaded)
			return loaded;

		var assembly = LoadContext.LoadFromAssemblyPath(fileName);

		if (LoadContext != AssemblyLoadContext.Default)
			LoadReferences(assembly, Resolver(fileName), Path.GetDirectoryName(fileName) ?? PluginDirectory);

		return assembly;
	}

	private static Assembly? Loaded(AssemblyName name)
	{
		return LoadContext.Assemblies.FirstOrDefault(f => string.Equals(f.GetName().Name, name.Name, StringComparison.OrdinalIgnoreCase));
	}

	/*
	 * A nested context resolves names from the application's own directory and leaves anything else to the default
	 * context, where a plugin's own dependencies would be shared with the host and never unload. So the references a
	 * plugin brings with it, the ones the application does not have itself, are loaded into the nested context from
	 * the plugin's location before anything binds them; a later bind by name finds them there.
	 */
	private static void LoadReferences(Assembly assembly, AssemblyDependencyResolver? resolver, string directory)
	{
		foreach (var reference in assembly.GetReferencedAssemblies())
		{
			if (reference.Name is null || Loaded(reference) is not null)
				continue;
			/*
			 * The application's own assemblies bind through the host's context as usual, which decides what is shared.
			 */
			if (File.Exists(Path.Combine(BaseDirectory, $"{reference.Name}.dll")))
				continue;
			/*
			 * Shared framework assemblies always come from the default context; a copy beside the plugin would give the
			 * plugin types of its own that nothing else in the application can use.
			 */
			if (IsFramework(reference.Name))
				continue;

			var path = resolver?.ResolveAssemblyToPath(reference) ?? Path.Combine(directory, $"{reference.Name}.dll");

			if (!File.Exists(path))
				continue;

			LoadReferences(LoadContext.LoadFromAssemblyPath(path), resolver, directory);
		}
	}

	private static readonly string[] FrameworkPrefixes = ["System.", "Microsoft.AspNetCore.", "Microsoft.Extensions.", "Microsoft.NETCore.",
		"Microsoft.Win32.", "Microsoft.CSharp", "Microsoft.VisualBasic", "Microsoft.JSInterop", "mscorlib", "netstandard", "WindowsBase"];
	private static readonly (HashSet<string> Shared, HashSet<string> Local) TrustedPlatformAssemblies =
		ClassifyTrustedPlatformAssemblies(ResolveTrustedPlatformAssemblies(), AppContext.BaseDirectory);

	private static bool IsFramework(string name) => IsFramework(name, TrustedPlatformAssemblies.Shared, TrustedPlatformAssemblies.Local);

	/// <summary>
	/// Splits the trusted platform assemblies into those from outside the application's directory and those from
	/// inside it, its runtime-specific subdirectories included.
	/// </summary>
	internal static (HashSet<string> Shared, HashSet<string> Local) ClassifyTrustedPlatformAssemblies(IEnumerable<string> paths, string applicationDirectory)
	{
		var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(applicationDirectory)) + Path.DirectorySeparatorChar;
		var shared = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var local = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		foreach (var path in paths)
		{
			var assembly = Path.GetFileNameWithoutExtension(path);

			if (Path.GetFullPath(path).StartsWith(root, StringComparison.OrdinalIgnoreCase))
				local.Add(assembly);
			else
				shared.Add(assembly);
		}

		return (shared, local);
	}

	/// <summary>
	/// Decides whether a referenced assembly belongs to the shared framework, from the trusted platform assemblies as
	/// <see cref="ClassifyTrustedPlatformAssemblies"/> splits them.
	/// </summary>
	/// <remarks>
	/// <para>
	/// A trusted platform assembly from outside the application's directory (its runtime-specific subdirectories
	/// included) is framework; the application's directory is never framework, since that is where its own and its
	/// plugins' assemblies are.
	/// </para>
	/// <para>
	/// A self-contained or single-file host carries the framework in its own directory, so no core library comes from
	/// outside it. Framework is then a trusted platform assembly with a framework name: the runtime pack is always
	/// trusted, while a package only a plugin ships never is, whatever its name.
	/// </para>
	/// </remarks>
	internal static bool IsFramework(string name, HashSet<string> shared, HashSet<string> local)
	{
		if (shared.Contains(name))
			return true;

		if (shared.Contains("System.Private.CoreLib") || !local.Contains(name))
			return false;

		return name == "System" || FrameworkPrefixes.Any(f => name.StartsWith(f, StringComparison.OrdinalIgnoreCase));
	}

	private static string[] ResolveTrustedPlatformAssemblies()
	{
		if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is not string trusted)
			return [];

		return trusted.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
	}

	private static AssemblyDependencyResolver? Resolver(string fileName)
	{
		try
		{
			return new AssemblyDependencyResolver(fileName);
		}
		catch (InvalidOperationException)
		{
			/*
			 * No dependency manifest the resolver can read; the plugin's directory is probed instead.
			 */
			return null;
		}
	}

	private static void RegisterCoreDependencies()
	{
		var entry = Options.EntryAssembly ?? Assembly.GetEntryAssembly();

		if (entry is not null)
			RegisterDependency(entry);

		RegisterDependency(typeof(Application).Assembly);

		RegisterDependency("Connected.dll");
		RegisterDependency("Connected.Extensions.dll");
		RegisterDependency("Connected.Authorization.dll");
	}

	private static void RegisterConfiguredDependencies(ConfigurationManager configuration)
	{
		var section = configuration.GetSection("dependencies");

		foreach (var child in section.GetChildren())
		{
			var value = child.Value;

			if (!string.IsNullOrWhiteSpace(value))
				RegisterDependency(PluginDirectory, value);
		}
	}

	private static void RegisterImages(ConfigurationManager configuration)
	{
		var section = configuration.GetSection("images");

		foreach (var child in section.GetChildren())
		{
			var value = child.Value;

			if (string.IsNullOrWhiteSpace(value))
				continue;

			var assembly = Load(Path.Combine(PluginDirectory, value));
			var types = assembly.GetTypes();

			Dependencies.Register(assembly);

			foreach (var type in types)
			{
				if (type.IsClass && !type.IsAbstract && type.ImplementsInterface<IRuntimeImage>())
				{
					if (Activator.CreateInstance(type) is not IRuntimeImage instance)
						continue;

					instance.Register();

					foreach (var dependency in instance.Dependencies)
						RegisterDependency(PluginDirectory, dependency);
				}
			}
		}
	}

	internal static void TriggerDiscoverType(IServiceCollection services, Type type)
	{
		DiscoverType?.Invoke(services, new DependencyTypeDiscoveryEventArgs(services, type));
	}

	private static async Task OnConfigureStaticFiles(IApplicationBuilder app)
	{
		var fileProviders = new List<IFileProvider>();
		var startups = Dependencies.Startups;

		foreach (var startup in startups)
		{
			var items = await startup.QueryStaticFileProviders();

			if (items.Any())
				fileProviders.AddRange(items);
		}

		if (fileProviders.Count == 0)
			return;

		var staticOptions = new StaticFileOptions
		{
			FileProvider = new CompositeFileProvider(fileProviders)
		};

		app.UseStaticFiles(staticOptions);
	}
}