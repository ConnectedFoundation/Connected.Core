using Connected.Authentication;
using Connected.Reflection;
using Connected.Runtime;
using Connected.Services;
using Connected.Storage.Schemas;
using Microsoft.Extensions.DependencyInjection;

namespace Connected.Entities;
public sealed class EntitiesStartup : Startup
{
	protected override async Task OnStart()
	{
		await SynchronizeSchemas();
	}

	private async Task SynchronizeSchemas()
	{
		using var scope = Scope.Create().WithSystemIdentity();

		var rt = scope.ServiceProvider.GetRequiredService<IRuntimeService>();

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

		if (!types.Any())
			return;

		var schemas = scope.ServiceProvider.GetRequiredService<ISchemaService>() ?? throw new NullReferenceException(nameof(ISchemaService));

		try
		{
			var dto = scope.ServiceProvider.GetRequiredService<IUpdateSchemaDto>();

			dto.Schemas = types;

			await schemas.Update(dto);
			await scope.Commit();
		}
		catch
		{
			await scope.Rollback();

			throw;
		}
	}
}
