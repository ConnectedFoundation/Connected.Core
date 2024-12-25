using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System.Net.Mime;
using System.Text;

namespace Connected;

public static class Runtime
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

	private static void RegisterCoreMicroServices()
	{
		MicroServices.Register<Authorization.AuthorizationStartup>();
		MicroServices.Register<Services.ServiceExtensionsStartup>();
		MicroServices.Register<Entities.EntitiesStartup>();
		MicroServices.Register<Net.NetStartup>();

	}
}