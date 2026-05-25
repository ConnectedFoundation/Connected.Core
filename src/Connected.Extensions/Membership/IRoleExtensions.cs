using Connected.Annotations;
using Connected.Membership.Dtos;
using System.Collections.Immutable;

namespace Connected.Membership;

[Service]
public interface IRoleExtensions
{
	Task<IImmutableList<string>> Query(IQueryUserDto dto);
}
