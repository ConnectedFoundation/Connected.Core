using Connected;

namespace TestClient;

public static class Program
{
	public static async Task Main(string[] args)
	{
		//Application.RegisterMicroService<Connected.Common.Types.MeasureUnits.MeasureUnitsStartup>();
		Application.RegisterMicroService(typeof(Connected.Storage.Sql.SqlStartup).Assembly);

		await Application.StartDefaultApplication(args);
	}
}