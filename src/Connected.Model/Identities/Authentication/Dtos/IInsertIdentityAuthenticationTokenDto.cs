namespace Connected.Identities.Authentication.Dtos;

public interface IInsertIdentityAuthenticationTokenDto : IIdentityAuthenticationTokenDto
{
	string Identity { get; set; }
	string Key { get; set; }
}