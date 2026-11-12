using Connected.Storage.Sql.Schemas;
using System.Data;

namespace Connected.Storage.Tests;

/// <summary>
/// Pins how the SQL Server schema synchronization reads an existing table back, so a table that matches its entity is
/// left alone instead of being altered or recreated at every start.
/// </summary>
[TestClass]
public sealed class SqlSchemaSynchronizationTests
{
	[TestMethod]
	public void ASmallintColumnIsReadBackAsInt16()
	{
		Assert.AreEqual(DbType.Int16, SchemaExtensions.ToDbType("smallint"));
		Assert.AreEqual(DbType.Int16, SchemaExtensions.ToDbType("SMALLINT"));
	}

	[TestMethod]
	[DataRow("(N'1')", "1")]
	[DataRow("(N'Approved')", "Approved")]
	[DataRow("(N'it''s')", "it's")]
	[DataRow("(N'')", "")]
	[DataRow("(getutcdate())", "getutcdate()")]
	[DataRow("((0))", "(0)")]
	public void ADefaultIsReadBackAsTheEntityDeclaresIt(string stored, string expected)
	{
		var constraint = new ObjectConstraint { Type = "DEFAULT on column status", Keys = stored };

		Assert.AreEqual(expected, constraint.DefaultValue);
	}

	[TestMethod]
	[DataRow("1")]
	[DataRow("Approved")]
	[DataRow("getutcdate()")]
	public void ADefaultReadBackIsWrittenTheSameWayAgain(string declared)
	{
		var written = SchemaExtensions.ParseDefaultValue(declared);
		var stored = new ObjectConstraint { Type = "DEFAULT on column status", Keys = $"({written})" };

		Assert.AreEqual(declared, stored.DefaultValue);
		Assert.AreEqual(written, SchemaExtensions.ParseDefaultValue(stored.DefaultValue));
	}

	[TestMethod]
	public void AConstraintThatIsNotADefaultKeepsItsKeys()
	{
		var constraint = new ObjectConstraint { Type = "PRIMARY KEY (clustered)", Keys = "(N'1')" };

		Assert.AreEqual("(N'1')", constraint.DefaultValue);
	}

	[TestMethod]
	[DataRow(DbType.Double, "float")]
	[DataRow(DbType.Single, "real")]
	[DataRow(DbType.Int16, "smallint")]
	public void ACopiedValueKeepsTheTypeItsColumnIsCreatedWith(DbType type, string sqlType)
	{
		Assert.AreEqual(sqlType, DataCopy.ConversionString(type, 0, 0));
	}

	[TestMethod]
	public void AnIndexOnTheSameColumnsInAnotherOrderIsTheSameIndex()
	{
		Assert.IsTrue(TableAlter.HasSameColumns(["b", "a"], ["A", "B"]));
	}

	[TestMethod]
	public void AnIndexThatDiffersInAColumnIsAnotherIndex()
	{
		Assert.IsFalse(TableAlter.HasSameColumns(["a", "b"], ["a", "c"]));
		Assert.IsFalse(TableAlter.HasSameColumns(["a"], ["b"]));
	}

	[TestMethod]
	public void AnIndexWithAnotherNumberOfColumnsIsAnotherIndex()
	{
		Assert.IsFalse(TableAlter.HasSameColumns(["a", "b"], ["a"]));
	}

	[TestMethod]
	public void TheColumnsAreReadInTheirOrderInTheTable()
	{
		StringAssert.Contains(Columns.CommandTextFor("dbo", "items"), "ORDER BY ORDINAL_POSITION");
	}
}
