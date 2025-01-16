using Connected;

namespace TestClient;

public static class Program
{
	public static async Task Main(string[] args)
	{
		//Application.RegisterMicroService<Connected.Common.Types.MeasureUnits.MeasureUnitsStartup>();
		Application.RegisterMicroService<Connected.Storage.Sql.SqlStartup>();

		await Application.StartDefaultApplication(args);
	}
}