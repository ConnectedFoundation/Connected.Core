using Connected.Authentication;
using Connected.Reflection;
using Connected.Runtime;
using Connected.Services;
using Connected.Storage.Schemas;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Connected.Entities;
public sealed class EntitiesStartup : Startup
{
	protected override async Task OnStart()
	{
		await SynchronizeSchemas();
	}

	private async Task SynchronizeSchemas()
	{
		using var scope = await Scope.Create().WithSystemIdentity();

		var rt = scope.ServiceProvider.GetRequiredService<IRuntimeService>();
		var logger = scope.ServiceProvider.GetRequiredService<ILogger<EntitiesStartup>>();

		if (!(await rt.SelectStartOptions()).HasFlag(StartOptions.SynchronizeSchemas))
			return;

		var types = new List<Type>();

		foreach (var assembly in await rt.QueryDependencies())
		{
			var all = assembly.GetTypes();

			foreach (var type in all)
			{
				if (type.IsAbstract || !type.ImplementsInterface<IEntity>())
					continue;

				types.Add(type);
			}
		}

		if (types.Count == 0)
			return;

		var schemas = scope.ServiceProvider.GetRequiredService<ISchemaService>() ?? throw new NullReferenceException(nameof(ISchemaService));

		try
		{
			var dto = scope.ServiceProvider.GetRequiredService<IUpdateSchemaDto>();

			dto.Schemas = types;

			await schemas.Update(dto);
			await scope.Commit();
		}
		catch (Exception ex)
		{
			await scope.Rollback();

			logger.LogError(ex, null);

			throw;
		}
	}
}
