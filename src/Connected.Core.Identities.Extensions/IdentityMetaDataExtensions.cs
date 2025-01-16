using Connected.Identities.MetaData;
using Connected.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Connected.Identities;
public static class IdentityMetaDataExtensions
{
	public const string AvatarDirectoryName = "Avatars";
	public const string AvatarFileNameExtension = "png";
	public static Task<string?> CreateDefaultAvatar(this IIdentityMetaDataService service, IIdentity identity)
	{
		//TODO: move this extension method out of the core model since it requires a reference to the service model.
		throw new NotImplementedException();
		//using var scope = Bootstrapper.Services.CreateAsyncScope();

		//var files = scope.ServiceProvider.GetRequiredService<IFileService>();
		//var directories = scope.ServiceProvider.GetRequiredService<IDirectoryService>();

		//var fileName = $"{identity.Token}.{AvatarFileNameExtension}";
		//var avatar = Avatar.Create(identity.Token, 512, 512);

		//await directories.Ensure(AvatarDirectoryName);
		//await files.Ensure(AvatarDirectoryName, fileName, avatar);

		//return fileName;
	}

	public static async Task<string?> UserName(this IIdentity identity)
	{
		using var scope = Scope.Create();

		try
		{
			var metaDataService = scope.ServiceProvider.GetService<IIdentityMetaDataService>();

			if (metaDataService is null)
				return null;

			var metaData = await metaDataService.Select(Dto.Factory.CreatePrimaryKey(identity.Token));

			if (metaData is null)
				return null;

			return metaData.UserName;
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
