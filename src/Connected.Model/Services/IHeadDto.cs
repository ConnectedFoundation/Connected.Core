namespace Connected.Services;

/// <summary>
/// Represents a data transfer object containing a head value.
/// </summary>
/// <typeparam name="T">The type of the head value.</typeparam>
/// <remarks>
/// This interface provides a standardized way to transfer a single head value, typically
/// used to identify a partition, tenant, or organizational unit in multi-tenant or
/// distributed scenarios.
/// </remarks>
public interface IHeadDto<T>
	: IDto
{
	/// <summary>
	/// Gets or sets the head value.
	/// </summary>
	T? Head { get; set; }
}
