using Connected.Identities.Graphics;
using Connected.Identities.MetaData;
using Connected.SaaS.Storage;
using Connected.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Connected.Identities;
public static class IdentityMetaDataExtensions
{
	public const string AvatarDirectoryName = "Avatars";
	public const string AvatarFileNameExtension = "png";
	public static async Task<string?> CreateDefaultAvatar(this IIdentityMetaDataService service, IIdentity identity)
	{
		using var scope = Bootstrapper.Services.CreateAsyncScope();

		var files = scope.ServiceProvider.GetRequiredService<IFileService>();
		var directories = scope.ServiceProvider.GetRequiredService<IDirectoryService>();

		var fileName = $"{identity.Token}.{AvatarFileNameExtension}";
		var avatar = Avatar.Create(identity.Token, 512, 512);

		await directories.Ensure(AvatarDirectoryName);
		await files.Ensure(AvatarDirectoryName, fileName, avatar);

		return fileName;
	}

	public static async Task<string?> UserName(this IIdentity identity)
	{
		using var scope = Bootstrapper.Services.CreateAsyncScope();

		var metaDataService = scope.ServiceProvider.GetService<IIdentityMetaDataService>();

		if (metaDataService is null)
			return null;

		var metaData = await metaDataService.Select(new PrimaryKeyDto<string> { Id = identity.Token });

		if (metaData is null)
			return null;

		return metaData.UserName;
	}
}
