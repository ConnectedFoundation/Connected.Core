using Connected.Annotations;
using Connected.Reflection;
using System.Collections;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Web;

namespace Connected.Services.Validation;

internal sealed class ValidationContext(/*IHttpContextAccessor http, IAntiforgery antiforgery, */IMiddlewareService middleware, IServiceProvider services) : IValidationContext
{
	public async Task Validate<TDto>(ICallerContext context, TDto dto)
		where TDto : IDto
	{
		//TODO KOM: try to fix this. It seems it is not working regardless of the request. 
		//await ValidateAntiforgery(dto);

		var results = new List<ValidationResult>();
		var refs = new List<object>();

		Validate(results, dto, refs);

		if (results.Count > 0)
			throw new ValidationException(results[0].ErrorMessage);

		if (middleware is null)
			return;

		if (await middleware.Query<IValidator<TDto>>(context) is not IImmutableList<IValidator<TDto>> middlewares)
			return;

		foreach (var m in middlewares)
			await m.Invoke(context, dto);
	}

	//private async Task ValidateAntiforgery(object? value)
	//{
	//	if (antiforgery is null || value is null)
	//		return;

	//	if (value.GetType().GetCustomAttribute<ValidateAntiforgeryAttribute>() is ValidateAntiforgeryAttribute attribute && !attribute.ValidateRequest)
	//		return;

	//	var ctx = http?.HttpContext;

	//	if (ctx is null)
	//		return;

	//	if (!ctx.Request.IsAjaxRequest())
	//		return;

	//	if (IsSafeMethod(ctx.Request.Method))
	//		return;
	//	/*
	//	 * No need to validate antiforgery more than once.
	//	 */
	//	if (!await antiforgery.IsRequestValidAsync(ctx))
	//		return;

	//	throw new ValidationException(SR.ValAntiForgery);
	//}

	private void Validate(List<ValidationResult> results, object? value, List<object> references)
	{
		if (value is null || value is Type)
			return;

		if (value.GetType().IsTypePrimitive())
			return;

		if (value is null || references.Contains(value))
			return;

		references.Add(value);

		var properties = value.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

		if (properties.Length == 0)
			return;

		var publicProps = new List<PropertyInfo>();
		var nonPublicProps = new List<PropertyInfo>();

		foreach (var property in properties)
		{
			if (property.GetMethod is null)
				continue;

			if (property.GetCustomAttribute<SkipValidationAttribute>() is not null)
				continue;

			if (property.GetMethod.IsPublic)
				publicProps.Add(property);
			else
				nonPublicProps.Add(property);
		}
		/*
		 * First, iterate only through the public properties
		 * At this point we won't validate complex objects, only the attributes directly on the
		 * passed instance
		 */
		foreach (var property in publicProps)
			ValidateProperty(results, value, property);
		/*
		 * If root validation failed we won't go deep because this would probably cause
	    * duplicate and/or confusing validation messages
		 */
		if (results.Count > 0)
			return;
		/*
		 * Second step is to validate complex public members and collections. 
		 */
		foreach (var property in publicProps)
		{
			if (property.PropertyType.IsEnumerable())
			{
				if (GetValue(value, property) is not IEnumerable ien)
					continue;

				var en = ien.GetEnumerator();

				while (en.MoveNext())
				{
					if (en.Current is null)
						continue;

					Validate(results, en.Current, references);
				}
			}
			else
			{
				if (GetValue(value, property) is not object val)
					continue;

				Validate(results, val, references);
			}
		}
		/*
		 * If any complex validation failed we won't validate private members because
		 * it is possible that initialization would fail for the reason of validation being failed.
		 */
		if (results.Count > 0)
			return;
		/*
		 * Now that validation of the public properties succeed we can go validate nonpublic members
		 */
		foreach (var property in nonPublicProps)
			ValidateProperty(results, value, property);
	}

	private void ValidateProperty(List<ValidationResult> results, object value, PropertyInfo property)
	{
		var attributes = property.GetCustomAttributes(false);

		if (!ValidateRequestValue(results, value, property))
			return;

		if (property.PropertyType.IsEnum && !Enum.TryParse(property.PropertyType, Convert.ToString(property.GetValue(value)), out _))
			results.Add(new ValidationResult($"{SR.ValEnumValueNotDefined} ({property.PropertyType.ShortName()}, {property.GetValue(value)})", [property.Name]));

		foreach (var attribute in attributes)
		{
			if (attribute is ValidationAttribute val)
			{
				var serviceProvider = new ValidationServiceProvider(services);
				var displayName = property.Name;

				var ctx = new System.ComponentModel.DataAnnotations.ValidationContext(value, serviceProvider, new Dictionary<object, object?>())
				{
					DisplayName = displayName.ToLower(),
					MemberName = property.Name,
				};

				val.Validate(GetValue(value, property), ctx);
			}
		}
	}

	private static bool ValidateRequestValue(List<ValidationResult> results, PropertyInfo property, object? value)
	{
		if (value is null)
			return true;

		var att = property.FindAttribute<ValidateRequestAttribute>();

		if (att is not null && !att.ValidateRequest)
			return true;

		if (HttpUtility.HtmlDecode(value.ToString()) is not string decoded)
			return true;

		if (decoded.Replace(" ", string.Empty).Contains("<script>"))
		{
			results.Add(new ValidationResult(SR.ValScriptTagNotAllowed, [property.Name]));
			return false;
		}

		return true;
	}

	private static bool ValidateRequestValue(List<ValidationResult> results, object? value, PropertyInfo property)
	{
		if (property.PropertyType != typeof(string))
			return true;

		if (!property.CanWrite)
			return true;

		return ValidateRequestValue(results, property, GetValue(value, property));
	}

	private static object? GetValue(object? value, PropertyInfo property)
	{
		try
		{
			if (value is null)
				return null;

			return property.GetValue(value);
		}
		catch (TargetInvocationException ex)
		{
			if (ex.InnerException is ValidationException)
				throw ex.InnerException;

			throw;
		}
	}

	//private static bool IsSafeMethod(string? method)
	//{
	//	if (method is null)
	//		return true;

	//	return string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase)
	//		|| string.Equals(method, "TRACE", StringComparison.OrdinalIgnoreCase)
	//		|| string.Equals(method, "HEAD", StringComparison.OrdinalIgnoreCase)
	//		|| string.Equals(method, "OPTIONS", StringComparison.OrdinalIgnoreCase);
	//}
}