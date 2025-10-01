using Connected.Core.Membership.Mock.Roles;
using Connected.Core.Membership.Mock.Roles.Dtos;
using Connected.Core.Mock;
using Connected.Entities;
using Connected.Membership;

namespace Connected.Core.Membership.Test;

[TestClass]
public class Roles()
	: RestTest(MembershipUrls.RoleService)
{
	[TestMethod]
	public async Task Invoke()
	{
		var id = await Insert<InsertRoleDtoMock, int>(new InsertRoleDtoMock
		{
			Name = "Role 1",
			Status = Status.Enabled
		});

		Assert.IsTrue(id > 0);

		var entities = await Query<RoleMock>();

		Assert.IsTrue(entities is not null && entities.Count > 0);

		await Update(new UpdateRoleDtoMock
		{
			Id = id,
			Name = "Role 2",
			Status = Status.Disabled,
		});

		var entity = await Select<RoleMock>(id);

		Assert.IsTrue(entity is not null && entity.Name == "Role 2" && entity.Status == Status.Disabled);

		await Delete(id);

		entity = await Select<RoleMock>(id);

		Assert.IsNull(entity);
	}
}
