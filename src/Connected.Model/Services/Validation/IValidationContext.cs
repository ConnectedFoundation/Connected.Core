namespace Connected.Services.Validation;

public interface IValidationContext
{
	Task Validate<TDto>(ICallerContext caller, TDto value) where TDto : IDto;
}
