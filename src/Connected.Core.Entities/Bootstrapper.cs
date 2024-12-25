using Connected.Reflection;
using Connected.Runtime;
using Connected.Storage.Schemas;
using Microsoft.Extensions.DependencyInjection;

namespace Connected.Entities;
internal sealed class Bootstrapper : Startup
{
	public static IServiceProvider Services { get; private set; } = default!;

	protected override async Task OnInitialize()
	{
		if (ServiceProvider is not null)
			Services = ServiceProvider;

		await base.OnInitialize();
	}

	protected override async Task OnStart()
	{
		await SynchronizeSchemas();
	}

	private async Task SynchronizeSchemas()
	{
		using var scope = Services.CreateAsyncScope();

		var rt = scope.ServiceProvider.GetRequiredService<IRuntimeService>();

		if (!(await rt.SelectStartOptions()).HasFlag(StartOptions.SynchronizeSchemas))
			return;

		var types = new List<Type>();

		foreach (var assembly in await rt.QueryMicroServices())
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
