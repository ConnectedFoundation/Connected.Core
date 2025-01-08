using Connected.Annotations;
using Connected.Net.Http;
using Connected.Reflection;
using Connected.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Reflection;
using System.Text.Json;

namespace Connected.Net.Rest.Dtos;

internal sealed class DtoRequestDelegate(HttpContext context)
	: HttpRequestHandler(context)
{
	private static readonly JsonSerializerOptions _options = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

	protected override async Task OnInvoke()
	{
		using var scope = Scope.Create();

		if ((await scope.ServiceProvider.GetRequiredService<IResolutionService>().SelectMethod(Context)) is not InvokeDescriptor descriptor)
		{
			Context.Response.StatusCode = (int)HttpStatusCode.NotFound;
			return;
		}

		if (descriptor.Parameters is null || descriptor.Parameters.Length == 0)
			return;

		await ResolveDto(descriptor.Parameters[0]);
	}

	private async Task ResolveDto(Type argument)
	{
		var descriptor = CreateDescriptor(argument);
		using var ms = new MemoryStream();
		await JsonSerializer.SerializeAsync(ms, descriptor, _options, Context.RequestAborted);

		ms.Seek(0, SeekOrigin.Begin);

		await Write(DateTime.UtcNow, "application/json", ms.ToArray());
	}

	private static DtoDescriptor CreateDescriptor(Type type)
	{
		var result = new DtoDescriptor();
		var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty);

		foreach (var property in properties)
		{
			if (!property.CanWrite)
				continue;

			var descriptor = new DtoPropertyDescriptor
			{
				Name = property.Name.ToCamelCase(),
				Type = ResolvePropertyType(property),
				Text = ResolvePropertyText(property),
				Description = ResolvePropertyDescription(property),
			};

			ResolvePassword(descriptor, property);
			ResolveRequired(descriptor, property);
			ResolveMinLength(descriptor, property);
			ResolveMaxLength(descriptor, property);
			ResolveMinValue(descriptor, property);
			ResolveMaxValue(descriptor, property);
			ResolveEmail(descriptor, property);

			result.Properties.Add(descriptor);
		}

		return result;
	}

	private static string ResolvePropertyType(PropertyInfo property)
	{
		if (property.PropertyType == typeof(string)
			|| property.PropertyType == typeof(char))
			return "string";
		else if (property.PropertyType.IsNumber())
			return "number";
		else if (property.PropertyType == typeof(DateTime)
			|| property.PropertyType == typeof(DateTimeOffset))
			return "date";
		else if (property.PropertyType == typeof(bool))
			return "bool";
		else if (property.PropertyType == typeof(Guid))
			return "string";

		return "object";
	}

	private static string ResolvePropertyText(PropertyInfo property)
	{
		var displayAttribute = property.FindAttribute<DisplayAttribute>();

		if (displayAttribute is null)
			return property.Name;

		var result = displayAttribute.GetName();

		if (result is null)
			return property.Name;

		return result;
	}

	private static string? ResolvePropertyDescription(PropertyInfo property)
	{
		var displayAttribute = property.FindAttribute<DisplayAttribute>();

		if (displayAttribute is null)
			return null;

		return displayAttribute.GetDescription();
	}

	private static void ResolveRequired(DtoPropertyDescriptor descriptor, PropertyInfo property)
	{
		var requiredAttribute = property.FindAttribute<RequiredAttribute>();

		if (requiredAttribute is null)
			return;

		descriptor.Required.IsRequired = true;
		descriptor.Required.ErrorMessage = requiredAttribute.ErrorMessage;
	}

	private static void ResolveMinLength(DtoPropertyDescriptor descriptor, PropertyInfo property)
	{
		var minLengthAttribute = property.FindAttribute<MinLengthAttribute>();

		if (minLengthAttribute is null)
			return;

		descriptor.MinLength.Value = minLengthAttribute.Length;
		descriptor.MinLength.ErrorMessage = minLengthAttribute.ErrorMessage;
	}

	private static void ResolveMaxLength(DtoPropertyDescriptor descriptor, PropertyInfo property)
	{
		var maxLengthAttribute = property.FindAttribute<MaxLengthAttribute>();

		if (maxLengthAttribute is null)
			return;

		descriptor.MaxLength.Value = maxLengthAttribute.Length;
		descriptor.MaxLength.ErrorMessage = maxLengthAttribute.ErrorMessage;
	}

	private static void ResolveMinValue(DtoPropertyDescriptor descriptor, PropertyInfo property)
	{
		var minValueAttribute = property.FindAttribute<MinValueAttribute>();

		if (minValueAttribute is null)
			return;

		descriptor.MinValue.Value = minValueAttribute.Value;
		descriptor.MinValue.ErrorMessage = minValueAttribute.ErrorMessage;
	}

	private static void ResolveMaxValue(DtoPropertyDescriptor descriptor, PropertyInfo property)
	{
		var maxValueAttribute = property.FindAttribute<RangeAttribute>();

		if (maxValueAttribute is null)
			return;

		descriptor.MaxValue.Value = Convert.ToDouble(maxValueAttribute.Maximum);
		descriptor.MaxValue.ErrorMessage = maxValueAttribute.ErrorMessage;
	}

	private static void ResolveEmail(DtoPropertyDescriptor descriptor, PropertyInfo property)
	{
		var emailAttribute = property.FindAttribute<EmailAddressAttribute>();

		if (emailAttribute is null)
			return;

		descriptor.Email.IsEnabled = true;
		descriptor.Email.ErrorMessage = emailAttribute.ErrorMessage;
	}

	private static void ResolvePassword(DtoPropertyDescriptor descriptor, PropertyInfo property)
	{
		descriptor.IsPassword = property.FindAttribute<PasswordPropertyTextAttribute>() is not null;
	}
}