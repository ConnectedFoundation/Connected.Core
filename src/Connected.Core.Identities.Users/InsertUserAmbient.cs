using Connected.Identities.Dtos;
using Connected.Services;
using System.ComponentModel.DataAnnotations;

namespace Connected.Identities;

internal sealed class InsertUserAmbient : AmbientProvider<IInsertUserDto>, IInsertUserAmbient
{
	[MaxLength(256)]
	public required string Token { get; set; }
	public UserStatus Status { get; set; } = UserStatus.Disabled;

	protected override async Task OnInvoke()
	{
		Token ??= Guid.NewGuid().ToString();

		await Task.CompletedTask;
	}
}
