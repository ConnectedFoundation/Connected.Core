using Connected;
using Connected.Core.Tests;
using Connected.Identities;

namespace TestClient;

public static class Program
{
	public static async Task Main(string[] args)
	{
		new IdentitiesImage().Register();
		Application.RegisterMicroService(typeof(Connected.Storage.Sql.SqlStartup).Assembly);
		Application.RegisterMicroService("Connected.Core.Tests.dll");

		var thread = new Thread(new ThreadStart(async () =>
		{
			while (!Application.HasStarted)
				Thread.Sleep(500);

			await UserTests.Run();
		}));

		thread.Start();

		await Application.StartDefaultApplication(args);
	}
}