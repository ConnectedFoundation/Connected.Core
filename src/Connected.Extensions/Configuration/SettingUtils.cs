using Connected.Configuration.Settings;
using Connected.Reflection;
using Connected.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Connected.Configuration;
public static class SettingUtils
{
	public static async Task<TValue?> GetValue<TValue>(string key)
	{
		using var scope = Scope.Create();

		try
		{
			var service = scope.ServiceProvider.GetRequiredService<ISettingService>();
			var setting = await service.Select(Dto.Factory.CreateName(key));

			if (setting is null || setting.Value is null)
				return default;

			return Types.Convert<TValue>(setting.Value);
		}
		catch
		{
			await scope.Rollback();

			throw;
		}
		finally
		{
			await scope.Flush();
		}
	}
}
