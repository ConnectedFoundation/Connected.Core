using Connected.Core.Membership.Mock.Roles;
using Connected.Core.Membership.Mock.Roles.Dtos;
using Connected.Core.Mock;
using Connected.Core.Services.Mock;
using Connected.Entities;
using Connected.Membership;
using Connected.Services;

namespace Connected.Tests.Membership;

[TestClass]
public class Roles()
	: RestTest(MembershipUrls.RoleService)
{
	[TestMethod]
	public async Task Crud()
	{
		var id = await Insert<InsertRoleDtoMock, int>(new()
		{
			Name = ValueGenerator.Generate(10),
			Status = Status.Enabled
		});

		Assert.IsTrue(id > 0);

		var entity = await Select<PrimaryKeyDtoMock<int>, RoleMock>(id);

		Assert.IsNotNull(entity);

		var updateDto = new UpdateRoleDtoMock
		{
			Id = id,
			Name = ValueGenerator.Generate(10),
			Status = Status.Disabled,
		};

		await Update(updateDto);

		entity = await Select<PrimaryKeyDtoMock<int>, RoleMock>(id);

		Assert.IsNotNull(entity);
		Assert.AreEqual(entity.Name, updateDto.Name);
		Assert.AreEqual(entity.Status, updateDto.Status);

		await Delete<PrimaryKeyDtoMock<int>>(id);

		entity = await Select<PrimaryKeyDtoMock<int>, RoleMock>(id);

		Assert.IsNull(entity);
	}

	[TestMethod]
	public async Task QuerySelect()
	{
		var id = await Insert<InsertRoleDtoMock, int>(new()
		{
			Name = ValueGenerator.Generate(10),
			Status = Status.Enabled
		});

		Assert.IsTrue(id > 0);

		var entity = await Select<PrimaryKeyDtoMock<int>, RoleMock>(id);

		Assert.IsNotNull(entity);

		entity = await Get<ValueDtoMock<string>, RoleMock>(ServiceOperations.SelectByToken, entity.Token);

		Assert.IsNotNull(entity);

		entity = await Get<NameDtoMock, RoleMock>(ServiceOperations.SelectByName, entity.Name);

		Assert.IsNotNull(entity);

		var entities = await GetList<PrimaryKeyListDtoMock<string>, RoleMock>(ServiceOperations.LookupByTokens, new(entity.Token));

		Assert.IsTrue(entities.Count > 0);

		await Delete<PrimaryKeyDtoMock<int>>(id);
	}
}
