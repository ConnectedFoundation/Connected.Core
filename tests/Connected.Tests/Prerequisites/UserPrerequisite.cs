using Connected.Core.Identities.Mock;
using Connected.Core.Identities.Mock.Dto;
using Connected.Core.Mock;
using Connected.Core.Services.Mock;
using Connected.Identities;

namespace Connected.Core.Membership.Test.Prerequisites;
internal class UserPrerequisite()
	: RestTest(IdentitiesUrls.Users)
{
	public string Token { get; private set; } = default!;
	private long Id { get; set; }

	public async Task Initialize()
	{
		var dto = new InsertUserDtoMock
		{
			FirstName = ValueGenerator.Generate(16),
			LastName = ValueGenerator.Generate(32),
			Email = $"{ValueGenerator.Generate(8)}@{ValueGenerator.Generate(4)}.com"
		};

		Id = await Insert<InsertUserDtoMock, long>(dto);

		Assert.IsTrue(Id > 0);

		var entity = await Select<PrimaryKeyDtoMock<long>, UserMock>(Id);

		Assert.IsNotNull(entity);

		Token = entity.Token;
	}

	public async Task Cleanup()
	{
		await Delete<PrimaryKeyDtoMock<long>>(Id);
	}
}
