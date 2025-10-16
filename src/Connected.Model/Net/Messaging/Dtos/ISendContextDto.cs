using Connected.Services;

namespace Connected.Net.Messaging.Dtos;

[Flags]
public enum SendFilterFlags
{
	None = 0,
	IdentityConnections = 1,
	ExceptSender = 2
}

public interface ISendContextDto
	: IDto
{
	string Method { get; set; }
	SendFilterFlags Filter { get; set; }
	string? Connection { get; set; }
	string? Identity { get; set; }
}