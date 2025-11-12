namespace Connected.Net.Messaging;

internal sealed class Client : IClient
{
	public required Guid Id { get; set; }
	public required string Connection { get; set; }
	public string? Identity { get; set; }
	public DateTime RetentionDeadline { get; set; }

	public int CompareTo(IClient? other)
	{
		if (other is null)
			return 1;

		if (!string.Equals(Identity, other.Identity, StringComparison.Ordinal))
			return 1;

		return 0;
	}
}