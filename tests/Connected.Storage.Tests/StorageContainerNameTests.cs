using Connected.Storage.PostgreSql.Containers;
using Connected.Storage.Sql.Containers;
using System.ComponentModel.DataAnnotations;

namespace Connected.Storage.Tests;

/// <summary>
/// Checks that providers refuse invalid container names before reaching a server, whatever DTO they are given.
/// </summary>
[TestClass]
public sealed class StorageContainerNameTests
{
	/*
	 * The connection strings point where nothing listens, so a name that passed validation would fail on connecting
	 * with a different exception.
	 */
	public static IEnumerable<object[]> InvalidNames =>
	[
		[""],
		[new string('a', 64)],
		["1starts_with_a_digit"],
		["has-a-dash"],
		["has space"],
		["quote\"d"],
		["bracket]d"],
		["trailing_newline\n"]
	];

	[TestMethod]
	[DynamicData(nameof(InvalidNames))]
	public async Task SqlServerRefusesAnInvalidName(string name)
	{
		var provider = new SqlStorageContainerProvider(new TestConfiguration("Server=127.0.0.1,1;Connect Timeout=1"));

		await Assert.ThrowsExceptionAsync<ValidationException>(() => provider.Create(TestConfiguration.Container(name)));
		await Assert.ThrowsExceptionAsync<ValidationException>(() => provider.Drop(TestConfiguration.Container(name)));
	}

	[TestMethod]
	[DynamicData(nameof(InvalidNames))]
	public async Task PostgreSqlRefusesAnInvalidName(string name)
	{
		var provider = new PostgreSqlStorageContainerProvider(new TestConfiguration("Host=127.0.0.1;Port=1;Timeout=1"));

		await Assert.ThrowsExceptionAsync<ValidationException>(() => provider.Create(TestConfiguration.Container(name)));
		await Assert.ThrowsExceptionAsync<ValidationException>(() => provider.Drop(TestConfiguration.Container(name)));
	}
}
