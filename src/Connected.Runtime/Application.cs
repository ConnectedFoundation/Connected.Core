using Connected.Annotations;
using Connected.Authentication;
using Connected.Configuration;
using Connected.Globalization;
using Connected.Net;
using Connected.Net.Routing;
using Connected.Net.Routing.Dtos;
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
using System.Text;

namespace Connected;

public static class Application
{
	private static object _lastException = new();

	public static bool IsErrorServerStarted { get; private set; }
	public static void AddOutOfMemoryExceptionHandler()
	{
		AppDomain.CurrentDomain.FirstChanceException += OnFirstChanceException;
	}

	public static void AddAutoRestart()
	{
		AppDomain.CurrentDomain.UnhandledException += OnUnhandledExceptionThrown;
	}

	public static void AddMicroServices(this WebApplicationBuilder builder)
	{
		var startups = MicroServices.Startups;

		foreach (var microService in MicroServices.All)
			builder.Services.AddMicroService(microService);

		builder.Services.RegisterServices();
	}

	public static void ConfigureMicroServicesServices(this IHostApplicationBuilder builder)
	{
		var startups = MicroServices.Startups;

		foreach (var startup in startups)
			startup.ConfigureServices(builder.Services);
	}

	public static void ConfigureMicroServices(this IApplicationBuilder builder, IWebHostEnvironment environment)
	{
		var startups = MicroServices.Startups;

		if (builder is WebApplication web)
		{
			foreach (var microService in MicroServices.All)
				web.MapMicroService(microService);
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

	public static async Task InitializeMicroServices(this IApplicationBuilder builder, IHost host)
	{
		var startups = MicroServices.Startups;

		await RegisterGrpcRoutes();

		foreach (var startup in startups)
			await startup.Initialize(host);
	}

	public static async Task StartMicroServices(this IApplicationBuilder builder)
	{
		var startups = MicroServices.Startups;

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

	public static void RegisterMicroService(Assembly assembly)
	{
		MicroServices.Register(assembly);
	}

	public static void RegisterMicroService(string assemblyName)
	{
		var fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, assemblyName);
		var name = AssemblyName.GetAssemblyName(fileName);

		MicroServices.Register(AppDomain.CurrentDomain.Load(name));
	}

	public static void RegisterCoreMicroServices()
	{
		var entry = Assembly.GetEntryAssembly();

		if (entry is not null)
			RegisterMicroService(entry);

		RegisterMicroService(typeof(RuntimeStartup).Assembly);
		RegisterMicroService(typeof(Authorization.AuthorizationStartup).Assembly);
		RegisterMicroService(typeof(Services.ServiceExtensionsStartup).Assembly);
		RegisterMicroService(typeof(Services.ServicesStartup).Assembly);
		RegisterMicroService(typeof(Entities.EntitiesStartup).Assembly);
		RegisterMicroService(typeof(Net.NetStartup).Assembly);
		RegisterMicroService(typeof(Configuration.Settings.SettingsStartup).Assembly);
		RegisterMicroService(typeof(Caching.CachingStartup).Assembly);
		RegisterMicroService(typeof(Storage.StorageExtensionsStartup).Assembly);
		RegisterMicroService(typeof(Configuration.ConfigurationStartup).Assembly);
		RegisterMicroService(typeof(Notifications.NotificationsStartup).Assembly);
		RegisterMicroService(typeof(Identities.Globalization.IdentitiesGlobalizationStartup).Assembly);
		RegisterMicroService(typeof(Globalization.Languages.LanguagesStartup).Assembly);
		RegisterMicroService(typeof(Net.NetExtensionsStartup).Assembly);
		RegisterCoreMicroService("Authentication");
		RegisterCoreMicroService("Storage.Sql");
		RegisterCoreMicroService("Net.Routing");
	}

	private static void RegisterCoreMicroService(string name)
	{
		RegisterMicroService($"Connected.Core.{name}.dll");
	}

	public static async Task StartDefaultApplication(string[] args)
	{
		try
		{
			RegisterCoreMicroServices();
			AddOutOfMemoryExceptionHandler();
			AddAutoRestart();

			var builder = WebApplication.CreateBuilder(args);

			builder.Configuration.AddUserSecrets(Assembly.GetEntryAssembly(), true);
			builder.Configuration.AddEnvironmentVariables();

			var services = builder.Services;

			services.AddLogging(builder => builder.AddConsole());

			builder.Services.AddGrpc();
			builder.AddLocalization();
			builder.AddHttpServices();
			builder.AddCors();
			builder.Services.AddRouting();
			builder.AddMicroServices();
			builder.ConfigureMicroServicesServices();

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
			webApp.ConfigureMicroServices(builder.Environment);
			webApp.ActivateHttpServices();
			webApp.ActivateAuthenticationCookieMiddleware();
			webApp.ActivateRest();

			await webApp.InitializeMicroServices(webApp);
			await webApp.StartMicroServices();
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
}