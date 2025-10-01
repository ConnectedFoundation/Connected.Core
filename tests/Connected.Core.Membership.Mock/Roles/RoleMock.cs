using Connected.Core.Entities.Mock;
using Connected.Entities;
using Connected.Membership.Roles;

namespace Connected.Core.Membership.Mock.Roles;
public class RoleMock : EntityMock<int>, IRole
{
	public required string Name { get; init; }
	public Status Status { get; init; }
	public int? Parent { get; init; }
	public required string Token { get; init; }
}
