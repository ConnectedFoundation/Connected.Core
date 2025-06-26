using Connected.Identities;
using Connected.Identities.Users;
using Connected.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Connected.Core.Tests;

public sealed class UserTests(IUserService users)
{
	public static async Task Run()
	{
		using var scope = Scope.Create();

		var service = scope.ServiceProvider.GetRequiredService<InsertUser>();

		var id = await service.Invoke();

		await scope.Commit();
	}
}
