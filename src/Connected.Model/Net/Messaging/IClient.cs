namespace Connected.Net.Messaging;

public interface IClient
	: IComparable<IClient>
{
	Guid Id { get; set; }
	string Connection { get; set; }
	string? Identity { get; }
	DateTime RetentionDeadline { get; set; }
}
