using Connected.Annotations.Entities;
using Connected.Collections.Queues;
using Connected.Storage.Schemas;
using Connected.Storage.Sql.Schemas;
using System.Data;
using System.Reflection;

namespace Connected.Storage.Tests;

/// <summary>
/// Checks that a queue message payload is not limited to a fixed number of bytes and that the SQL Server
/// schema code turns that declaration into a VARBINARY(MAX) column.
/// </summary>
[TestClass]
public sealed class QueueMessageDtoLengthTests
{
	/// <summary>
	/// Exposes the protected column text builder of the SQL Server schema code to the tests.
	/// </summary>
	private sealed class ColumnTextProbe
		: TableTransaction
	{
		public static string Text(ISchemaColumn column) => CreateColumnCommandText(column);

		protected override Task OnExecute() => Task.CompletedTask;
	}

	[TestMethod]
	public void DtoColumnDeclaresMaximumLength()
	{
		var property = typeof(QueueMessage).GetProperty(nameof(QueueMessage.Dto), BindingFlags.Public | BindingFlags.Instance);
		var length = property?.GetCustomAttribute<LengthAttribute>();

		Assert.IsNotNull(length);
		Assert.AreEqual(-1, length.Value);
		Assert.AreEqual(QueueMessage.DtoLength, length.Value);
	}

	[TestMethod]
	public void MaximumLengthBinaryColumnIsVarbinaryMax()
	{
		var column = new ExistingColumn(null!) { Name = "dto", DataType = DbType.Binary, MaxLength = -1 };

		StringAssert.Contains(ColumnTextProbe.Text(column), "[varbinary](MAX)");
	}

	[TestMethod]
	public void FixedLengthBinaryColumnKeepsItsLength()
	{
		var column = new ExistingColumn(null!) { Name = "dto", DataType = DbType.Binary, MaxLength = 1024 };

		StringAssert.Contains(ColumnTextProbe.Text(column), "[varbinary](1024)");
	}
}
