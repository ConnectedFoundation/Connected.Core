using Connected.Services;

namespace Connected.Net.Rest.Dtos;

/// <summary>
/// Represents a data transfer object for REST request arguments.
/// </summary>
/// <remarks>
/// This interface encapsulates a single argument or parameter from a REST API request,
/// consisting of a property name and its corresponding value. It enables dynamic handling
/// of request parameters where argument names and values need to be processed generically.
/// This is useful for scenarios such as custom argument handlers, parameter validation,
/// transformation pipelines, or dynamic query building where request arguments need to be
/// inspected and processed without strong typing to specific DTOs. The property-value pair
/// structure allows flexible argument processing across different API operations.
/// </remarks>
public interface IRequestArgumentDto
	: IDto
{
	/// <summary>
	/// Gets or sets the property name of the request argument.
	/// </summary>
	/// <value>
	/// A string representing the argument or parameter name.
	/// </value>
	/// <remarks>
	/// The property name identifies which parameter of the request this argument represents,
	/// typically corresponding to query string parameters, route values, or request body
	/// property names in REST API operations.
	/// </remarks>
	string Property { get; set; }

	/// <summary>
	/// Gets or sets the value of the request argument.
	/// </summary>
	/// <value>
	/// An object containing the argument value, or null if the argument has no value.
	/// </value>
	/// <remarks>
	/// The value contains the actual data for this argument. Since it is typed as object,
	/// it can represent any data type, requiring handlers to perform appropriate type
	/// checking and conversion when processing the value.
	/// </remarks>
	object? Value { get; set; }
}
