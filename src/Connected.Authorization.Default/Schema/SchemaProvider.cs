using Connected.Annotations.Entities;
using Connected.Membership.Claims;

namespace Connected.Authorization.Default.Schema;
internal sealed class SchemaProvider
	: ClaimSchemaProvider
{
	protected override async Task OnInvoke()
	{
		if (DtoEquals(null, null))
		{
			Add(SchemaAttribute.CoreSchema, SchemaAttribute.CoreSchema, Strings.CoreDomain);
		}

		await Task.CompletedTask;
	}
}
