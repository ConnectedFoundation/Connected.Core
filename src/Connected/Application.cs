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
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

	public static async Task Start(string[] args)
	{
		try
		{
			var builder = WebApplication.CreateBuilder(args);

			var entryAssembly = Assembly.GetEntryAssembly();

			if (entryAssembly is not null)
				builder.Configuration.AddUserSecrets(entryAssembly, true);

			builder.Configuration.AddEnvironmentVariables();

			var services = builder.Services;

			RegisterConfiguredDependencies(builder.Configuration);
			RegisterImages(builder.Configuration);
			RegisterCoreDependencies();

			foreach (var startup in Dependencies.Startups)
				startup.Prepare(builder.Configuration);

			AddOutOfMemoryExceptionHandler();
			AddAutoRestart();

			services.AddLogging(builder => builder.AddConsole());

			builder.Services.AddGrpc();
			builder.AddLocalization();
			builder.AddHttpServices();
			builder.AddCors();
			builder.Services.AddRouting();
			builder.AddDependenciess();
			builder.ConfigureDependenciesServices();

			services.AddSignalR(o =>
			{
				o.EnableDetailedErrors = true;
				o.DisableImplicitFromServicesParameters = false;
			});

			var webApp = builder.Build();

			webApp.ActivateRequestAuthentication();
			webApp.ActivateLocalization();
			webApp.ActivateCors();
			webApp.UseRouting();
			webApp.ConfigureDependencies(builder.Environment);
			webApp.ActivateHttpServices();
			webApp.ActivateRest();

			await InitializeDependencies(webApp);
			await StartDependencies();

			HasStarted = true;

			await webApp.RunAsync();
		}
		finally
		{
			if (IsErrorServerStarted)
			{
				while (true)
					Thread.Sleep(1000);
			}
		}
	}
	private static void AddOutOfMemoryExceptionHandler()
	{
		AppDomain.CurrentDomain.FirstChanceException += OnFirstChanceException;
	}

	private static void AddAutoRestart()
	{
		AppDomain.CurrentDomain.UnhandledException += OnUnhandledExceptionThrown;
	}

	private static void AddDependenciess(this WebApplicationBuilder builder)
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
		using var scope = Scope.Create();
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
		var fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, assemblyName);

		Dependencies.Register(AssemblyLoadContext.Default.LoadFromAssemblyPath(fileName));
	}

	private static void RegisterCoreDependencies()
	{
		var entry = Assembly.GetEntryAssembly();

		if (entry is not null)
			RegisterDependency(entry);

		RegisterDependency(typeof(Application).Assembly);

		RegisterDependency("Connected.dll");
		RegisterDependency("Connected.Extensions.dll");
		RegisterDependency("Connected.Authorization.dll");
	}

	private static void RegisterConfiguredDependencies(IConfiguration configuration)
	{
		var section = configuration.GetSection("dependencies");

		foreach (var child in section.GetChildren())
		{
			var value = child.Value;

			if (!string.IsNullOrWhiteSpace(value))
				RegisterDependency(value);
		}
	}

	private static void RegisterImages(IConfiguration configuration)
	{
		var section = configuration.GetSection("images");

		foreach (var child in section.GetChildren())
		{
			var value = child.Value;

			if (string.IsNullOrWhiteSpace(value))
				continue;

			var fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, value);
			var name = AssemblyName.GetAssemblyName(fileName);
			var assembly = AppDomain.CurrentDomain.Load(name);
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
						RegisterDependency(dependency);
				}
			}
		}
	}

	internal static void TriggerDiscoverType(IServiceCollection services, Type type)
	{
		DiscoverType?.Invoke(services, new DependencyTypeDiscoveryEventArgs(services, type));
	}
}