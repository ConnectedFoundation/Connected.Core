﻿using Connected.Configuration.Settings;
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

			var setting = await service.Select((NameDto)key);

			if (setting is null)
				return default;

			return (TValue?)Convert.ChangeType(setting.Value, typeof(TValue));
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
