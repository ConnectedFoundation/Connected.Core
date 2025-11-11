namespace Connected.Identities.MetaData.Dtos;

/// <summary>
/// Represents a data transfer object for updating existing identity metadata.
/// </summary>
/// <remarks>
/// This interface extends the base identity metadata DTO to support the modification
/// of existing metadata records. It inherits all metadata properties from the base interface,
/// including the primary key identifier required for updating the correct record.
/// </remarks>
public interface IUpdateIdentityMetaDataDto
	: IIdentityMetaDataDto
{
}
