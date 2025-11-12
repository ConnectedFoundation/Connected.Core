namespace Connected.Identities.MetaData.Dtos;

/// <summary>
/// Represents a data transfer object for inserting new identity metadata.
/// </summary>
/// <remarks>
/// This interface extends the base identity metadata DTO to support the creation
/// of new metadata records. It inherits all metadata properties from the base interface,
/// including the primary key identifier required for insertion.
/// </remarks>
public interface IInsertIdentityMetaDataDto
	: IIdentityMetaDataDto
{
}
