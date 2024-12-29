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
	private static bool _startedErrorServer = false;
	private static IHost? _host;

	public static async Task Run(string[] args)
	{
		RegisterCoreMicroServices();

		AppDomain.CurrentDomain.UnhandledException += OnUnhandledExceptionThrown;
		AppDomain.CurrentDomain.FirstChanceException += OnFirstChanceException;

		try
		{

			var builder = WebApplication.CreateBuilder(args);

			builder.WebHost.UseStaticWebAssets();

			var startups = MicroServices.Startups;

			foreach (var microService in MicroServices.All)
				builder.Services.AddMicroService(microService);

			foreach (var startup in startups)
				startup.ConfigureServices(builder.Services);

			var webApp = builder.Build();

			_host = webApp;

			foreach (var startup in startups)
				startup.Configure(webApp, builder.Environment);

			foreach (var startup in startups)
				await startup.Initialize(_host);

			foreach (var startup in startups)
				await startup.Start();

			await _host.RunAsync();
		}
		finally
		{
			if (_startedErrorServer)
			{
				while (true)
					Thread.Sleep(1000);
			}
		}
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

		if (_startedErrorServer)
			return;

		_startedErrorServer = true;

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

	private static void RegisterCoreMicroServices()
	{

		MicroServices.Register<RuntimeStartup>();
		MicroServices.Register<Startup.Mvc.MvcStartup>();
		MicroServices.Register<Startup.Authentication.AuthenticationStartup>();
		MicroServices.Register<Startup.Cors.CorsStartup>();
		MicroServices.Register<Startup.Diagnostics.DiagnosticsStartup>();
		MicroServices.Register<Startup.Http.HttpStartup>();
		MicroServices.Register<Startup.Localization.LocalizationStartup>();
		MicroServices.Register<Startup.SignalR.SignalRStartup>();
		MicroServices.Register<Startup.Routing.RoutingStartup>();
		MicroServices.Register<Startup.StaticFiles.StaticFilesStartup>();
		MicroServices.Register<Authorization.AuthorizationStartup>();
		MicroServices.Register<Services.ServiceExtensionsStartup>();
		MicroServices.Register<Services.ServicesStartup>();
		MicroServices.Register<Entities.EntitiesStartup>();
		MicroServices.Register<Net.NetStartup>();
		MicroServices.Register<Web.Views.ViewsStartup>();
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