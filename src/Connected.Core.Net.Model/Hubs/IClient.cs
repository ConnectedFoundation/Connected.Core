using System;

namespace Connected.Net.Hubs;

public interface IClient : IComparable<IClient>
{
	string Id { get; set; }
	string Connection { get; set; }
	long User { get; }
	DateTime RetentionDeadline { get; set; }
}
