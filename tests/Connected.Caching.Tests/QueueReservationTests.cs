using Connected.Collections.Queues;

namespace Connected.Caching.Tests;

/// <summary>
/// Holds that the queue host leaves a message to the job that reserved it while the job keeps it invisible, and takes
/// over only one whose reservation has run out.
/// </summary>
[TestClass]
public sealed class QueueReservationTests
{
	private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

	private static bool IsReserved(Guid? popReceipt, DateTimeOffset nextVisible) =>
		QueueHost<IQueueMessage, IQueueMessageCache>.IsReserved(popReceipt, nextVisible, Now);

	[TestMethod]
	public void AMessageAJobKeepsInvisibleIsReserved()
	{
		Assert.IsTrue(IsReserved(Guid.NewGuid(), Now.AddSeconds(45)));
	}

	[TestMethod]
	public void AMessageWhoseReservationRanOutIsNotReserved()
	{
		Assert.IsFalse(IsReserved(Guid.NewGuid(), Now.AddSeconds(-1)));
		Assert.IsFalse(IsReserved(Guid.NewGuid(), Now));
	}

	[TestMethod]
	public void AMessageNoJobDequeuedIsNotReserved()
	{
		Assert.IsFalse(IsReserved(null, Now.AddSeconds(45)));
	}
}
