using Connected.Services;

namespace Connected.Net.Hubs;

[Flags]
public enum SendFilterFlags
{
	None = 0,
	UserConnections = 1,
	ExceptSender = 2
}

public interface ISendContextDto : IDto
{
	string Method { get; set; }
	SendFilterFlags Filter { get; set; }
	string? Connection { get; set; }
	long User { get; set; }
}