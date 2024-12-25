namespace Connected.Configuration.Authentication;

public interface IAuthenticationConfiguration
{
	IJwTokenConfiguration JwToken { get; }
}
