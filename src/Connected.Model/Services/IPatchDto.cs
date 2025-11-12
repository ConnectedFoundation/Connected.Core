namespace Connected.Services;

/// <summary>
/// Represents a data transfer object for partial entity updates.
/// </summary>
/// <typeparam name="TPrimaryKey">The type of the primary key identifier.</typeparam>
/// <remarks>
/// This interface combines primary key identification with property tracking capabilities
/// to enable partial updates where only specified properties are modified. The property
/// provider mechanism allows tracking which fields should be updated without affecting
/// other entity properties.
/// </remarks>
public interface IPatchDto<TPrimaryKey>
	: IPrimaryKeyDto<TPrimaryKey>, IPropertyProvider
	where TPrimaryKey : notnull
{
}
