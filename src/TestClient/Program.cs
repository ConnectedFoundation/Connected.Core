using Connected;
using Connected.Authentication;
using Connected.Globalization;
using Connected.Net;

namespace TestClient;

public static class Program
{
	public static async Task Main(string[] args)
	{
		Application.RegisterMicroService<Connected.Common.Types.MeasureUnits.MeasureUnitsStartup>();
		Application.RegisterMicroService<Connected.Storage.Sql.SqlStartup>();

		try
		{
			Application.RegisterCoreMicroServices();
			Application.AddOutOfMemoryExceptionHandler();
			Application.AddAutoRestart();

			var builder = WebApplication.CreateBuilder(args);
			var services = builder.Services;

			services.AddLogging(builder => builder.AddConsole());

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
			if (Application.IsErrorServerStarted)
			{
				while (true)
					Thread.Sleep(1000);
			}
		}
	}
}