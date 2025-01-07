using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System.Net.Mime;
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

		foreach (var startup in startups)
			startup.Configure(builder, environment);
	}

	public static async Task InitializeMicroServices(this IApplicationBuilder builder, IHost host)
	{
		var startups = MicroServices.Startups;

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

	public static void RegisterMicroService<TMicroService>()
		where TMicroService : Runtime.IStartup
	{
		MicroServices.Register<TMicroService>();
	}

	public static void RegisterCoreMicroServices()
	{
		MicroServices.Register<RuntimeStartup>();
		MicroServices.Register<Authorization.AuthorizationStartup>();
		MicroServices.Register<Services.ServiceExtensionsStartup>();
		MicroServices.Register<Services.ServicesStartup>();
		MicroServices.Register<Entities.EntitiesStartup>();
		MicroServices.Register<Net.NetStartup>();
		MicroServices.Register<Configuration.Settings.SettingsStartup>();
		MicroServices.Register<Caching.CachingStartup>();
		MicroServices.Register<Storage.StorageExtensionsStartup>();
		MicroServices.Register<Configuration.ConfigurationStartup>();
		MicroServices.Register<Notifications.NotificationsStartup>();
		MicroServices.Register<Authentication.AuthenticationStartup>();
		MicroServices.Register<Identities.Globalization.IdentitiesGlobalizationStartup>();
		MicroServices.Register<Globalization.Languages.LanguagesStartup>();
	}
}