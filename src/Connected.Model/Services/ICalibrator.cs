namespace Connected.Services;

/// <summary>
/// Middleware for correcting <see cref="IDto"/> objects and <see cref="IAmbientProvider{TDto}"/> objects before they are validated.
/// </summary>
/// <typeparam name="TDto">The type of the data transfer object to be calibrated.</typeparam>
/// <remarks>
/// When this stage occurs, all DtoValueProviders and Ambient providers have already executed,
/// and it is guaranteed that they do not interfere when calibration middlewares are executed.
/// Calibrators provide an opportunity to normalize, adjust, or correct DTO values after ambient
/// and value providers have completed their work but before validation occurs.
/// </remarks>
public interface ICalibrator<TDto>
	: IMiddleware
	where TDto : IDto
{
	/// <summary>
	/// Asynchronously calibrates the specified data transfer object.
	/// </summary>
	/// <param name="dto">The data transfer object to be calibrated.</param>
	/// <returns>A task that represents the asynchronous operation.</returns>
	Task Invoke(TDto dto);
}