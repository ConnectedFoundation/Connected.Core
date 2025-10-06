using Connected.Core.Membership.Mock.Claims;
using Connected.Core.Membership.Mock.Claims.Dtos;
using Connected.Core.Membership.Test.Prerequisites;
using Connected.Core.Mock;
using Connected.Core.Services.Mock;
using Connected.Membership;
using Connected.Membership.Claims;

namespace Connected.Core.Membership.Test;

[TestClass]
public class Claims()
	: RestTest(MembershipUrls.ClaimService)
{
	private UserPrerequisite Users { get; } = new();


	[TestInitialize]
	public async Task Initialize()
	{
		await Users.Initialize();
	}

	[TestCleanup]
	public async Task Cleanup()
	{
		await Users.Cleanup();
	}

	[TestMethod]
	public async Task Invoke()
	{
		var insertDto = new InsertClaimDtoMock
		{
			Entity = ValueGenerator.Generate(8),
			EntityId = ValueGenerator.Generate(8),
			Identity = Users.Token,
			Schema = "Users",
			Value = "Claim 10"
		};

		var id = await Insert<InsertClaimDtoMock, long>(insertDto);

		Assert.IsTrue(id > 0);

		var entity = await Select<PrimaryKeyDtoMock<long>, ClaimMock>(id);

		Assert.IsNotNull(entity);

		var hasClaim = await GetNonEntity<RequestClaimDtoMock, bool>(nameof(IClaimService.Request), new()
		{
			Entity = insertDto.Entity,
			EntityId = insertDto.EntityId,
			Identity = Users.Token,
			Values = insertDto.Value
		});

		Assert.IsTrue(hasClaim);

		var entities = await Query<QueryClaimDtoMock, ClaimMock>(new()
		{
			Identity = Users.Token,
			Entity = insertDto.Entity,
			EntityId = insertDto.EntityId,
			Schema = "Users"
		});

		Assert.IsTrue(entities.Count > 0);

		await Delete<PrimaryKeyDtoMock<long>>(id);
	}
}
