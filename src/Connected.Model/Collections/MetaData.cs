using Connected.Annotations.Entities;
using Connected.Collections.Queues;

namespace Connected.Collections;
public static class MetaData
{
	public const string QueueMessageKey = $"{SchemaAttribute.CoreSchema}.{nameof(IQueueMessage)}";
}
