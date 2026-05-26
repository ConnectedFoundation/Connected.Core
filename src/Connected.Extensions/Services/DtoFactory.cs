using Microsoft.Extensions.DependencyInjection;

namespace Connected.Services;

public static class DtoFactory
{
	public static T Create<T>()
		where T : IDto
	{
		if (ServiceExtensionsStartup.Services is null)
		{
			var provider = ServiceExtensionsStartup.ServicesCollection.BuildServiceProvider(false);

			return provider.GetRequiredService<T>();
		}

		return ServiceExtensionsStartup.Services.GetRequiredService<T>();
	}

	public static T Create<T>(Action<T> initializer)
		where T : IDto
	{
		ArgumentNullException.ThrowIfNull(initializer);

		var dto = Create<T>();

		initializer(dto);

		return dto;
	}
}
