using Connected.Annotations.Entities;
using Connected.Entities;

namespace Connected.Storage.Sql.Schemas;

[Persistence(PersistenceMode.InMemory)]
internal sealed record AdHocSchemaEntity : Entity
{
	public bool Result { get; init; }
}
