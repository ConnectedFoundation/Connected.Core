namespace Connected.Net.Hubs;

internal sealed class Client : IClient
{
	public required string Id { get; set; }
	public required string Connection { get; set; }
	public long User { get; set; }
	public DateTime RetentionDeadline { get; set; }

	public int CompareTo(IClient? other)
	{
		if (other is null)
			return 1;

		if (User != other.User)
			return 1;

		return 0;
	}
}