using Connected.Annotations;
using Connected.Reflection;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Runtime.CompilerServices;
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
		/*
		 * One walk over the whole request: every complex member and every collection item, each object once.
		 * See Visited for how the walk ends on cycles and on members that hand back new equal copies.
		 */
		Validate(results, dto, new Visited { FromRequest = RequestDtos.IsBound(dto) });

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

	/// <summary>
	/// What the walk has been through, so that it ends.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The objects already walked are remembered by reference, in a set: telling whether one was seen is then one lookup,
	/// where a list searched item by item compared it with every object before it (a request carrying 100k objects spent
	/// hours there). Nothing is hashed by value, so a record that holds itself does not hash forever.
	/// </para>
	/// <para>
	/// The objects on the way down to the current one are also compared by value. A member that hands back a new copy
	/// equal to an object above it (a record's <c>this with { }</c>, or two records handing back each other) is a new
	/// reference every time and would otherwise be walked without end. Only the path is compared, a few objects deep,
	/// so the cost stays linear in the size of the request.
	/// </para>
	/// </remarks>
	private sealed class Visited
	{
		/// <summary>
		/// Every object walked so far in this request, by reference.
		/// </summary>
		/// <remarks>
		/// <see cref="ReferenceEqualityComparer"/> hashes by identity: an object's own <c>GetHashCode</c> is never called,
		/// so neither a record that holds itself nor a type with an expensive hash costs anything here.
		/// </remarks>
		public HashSet<object> Seen { get; } = new(ReferenceEqualityComparer.Instance);

		/// <summary>
		/// Whether the request being walked was bound from a caller (<see cref="RequestDtos"/>). Only then are its DTOs,
		/// and the DTOs nested in it, held to their required members.
		/// </summary>
		public bool FromRequest { get; init; }

		/// <summary>
		/// The objects from the request down to the one being walked, outermost first.
		/// </summary>
		public List<object> Path { get; } = [];

		/// <summary>
		/// Decides whether <paramref name="value"/> is walked, and if so records it as seen and as the deepest object on
		/// the path.
		/// </summary>
		/// <param name="value">The object about to be walked. Never null and never a primitive.</param>
		/// <returns>
		/// False when the object equals one of the objects above it (a new copy handed back by a member: walking it would
		/// never end) or when this very object was already walked (a cycle, or the same object reached twice). True
		/// otherwise; the caller must then call <see cref="Leave"/> once the object is walked.
		/// </returns>
		/// <remarks>
		/// The path is compared by <c>Equals</c>, as the list this replaced compared every object; only the path, so the
		/// comparisons per object are as many as the walk is deep, not as many as the objects before it.
		/// </remarks>
		public bool Enter(object value)
		{
			foreach (var above in Path)
			{
				if (above.Equals(value))
					return false;
			}

			if (!Seen.Add(value))
				return false;

			Path.Add(value);

			return true;
		}

		/// <summary>
		/// Takes the object last entered off the path once it and everything beneath it was walked. It stays seen.
		/// </summary>
		public void Leave() => Path.RemoveAt(Path.Count - 1);
	}

	/// <summary>
	/// Validates one object of the request and, through <see cref="Walk"/>, everything beneath it.
	/// </summary>
	/// <param name="results">Where failed validations are collected.</param>
	/// <param name="value">The object; null, types and primitives (value types included) are not walked.</param>
	/// <param name="visited">What this request's walk has been through.</param>
	private void Validate(List<ValidationResult> results, object? value, Visited visited)
	{
		if (value is null || value is Type)
			return;

		if (value.GetType().IsTypePrimitive())
			return;

		if (!visited.Enter(value))
			return;

		try
		{
			Walk(results, value, visited);
		}
		finally
		{
			visited.Leave();
		}
	}

	/// <summary>
	/// Validates the members of an object already entered: first the attributes of its public members, then its complex
	/// members and collection items, then its non-public members.
	/// </summary>
	/// <remarks>
	/// Members marked <see cref="SkipValidationAttribute"/> are neither validated nor read. A request carrying large
	/// in-memory data the server built itself should mark it so; walking it costs time and finds nothing.
	/// </remarks>
	private void Walk(List<ValidationResult> results, object value, Visited visited)
	{
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
		 * A request's required members: only in a request a caller sent, and only on objects that are DTOs, so
		 * neither a DTO built by code nor data the server built and carries in a request is refused for them.
		 */
		if (results.Count == 0 && visited.FromRequest && value is IDto)
		{
			foreach (var property in publicProps)
				ValidateRequiredMember(results, value, property);
		}
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

					Validate(results, en.Current, visited);
				}
			}
			else
			{
				if (GetValue(value, property) is not object val)
					continue;

				Validate(results, val, visited);
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

	/// <summary>
	/// Whether each property is a <c>required</c> member whose type does not allow null, worked out once per property.
	/// </summary>
	/// <remarks>
	/// Reflection over attributes and nullability costs more than the check itself, and a request type's properties never
	/// change, so the answer is kept for the life of the process.
	/// </remarks>
	private static readonly ConcurrentDictionary<PropertyInfo, bool> RequiredNotNull = new();

	/// <summary>
	/// Refuses a request that left a <c>required</c> member null when the member's type does not allow null.
	/// </summary>
	/// <remarks>
	/// <para>
	/// C# <c>required</c> is enforced by the compiler only. A request bound from JSON, or a DTO made by a factory, can
	/// arrive with the member null; the operation then fails later with a null reference, which reaches the caller as an
	/// error that names nothing ("Object reference not set to an instance of an object" instead of the member the caller
	/// left out).
	/// </para>
	/// <para>
	/// Not checked: members whose type allows null (<c>required string?</c>, <c>required int?</c>), because null is a value
	/// their contract admits; and value types (<c>required int</c>), because a member left out cannot be told from its
	/// default. An empty string is a value: whether it is allowed is for an attribute or the operation to decide.
	/// </para>
	/// <para>
	/// Only requests a binder marked as a caller's (<see cref="RequestDtos"/>) are checked: a DTO built by code through
	/// the factory is not refused for what it leaves unset, so no internal call starts failing because of this check.
	/// </para>
	/// <para>
	/// The member is named as a request spells it (camel case), so the caller can find it in what it sent.
	/// </para>
	/// </remarks>
	/// <param name="results">Where a refusal is collected.</param>
	/// <param name="value">The request being validated.</param>
	/// <param name="property">One of its public properties.</param>
	private static void ValidateRequiredMember(List<ValidationResult> results, object value, PropertyInfo property)
	{
		if (!RequiredNotNull.GetOrAdd(property, IsRequiredNotNull))
			return;

		if (GetValue(value, property) is not null)
			return;

		var name = property.Name.ToCamelCase();

		results.Add(new ValidationResult(string.Format(SR.ValRequiredMember, name), [property.Name]));
	}

	/// <summary>
	/// Whether <paramref name="property"/> is declared <c>required</c> and its type does not allow null.
	/// </summary>
	/// <remarks>
	/// The compiler marks a <c>required</c> member with <see cref="RequiredMemberAttribute"/> on the declaring type's
	/// property, which <see cref="Type.GetProperties(BindingFlags)"/> returns for derived types too. A
	/// <see cref="NullabilityInfoContext"/> is not safe to share between threads, so each answer uses its own.
	/// </remarks>
	private static bool IsRequiredNotNull(PropertyInfo property)
	{
		if (!property.IsDefined(typeof(RequiredMemberAttribute), inherit: true))
			return false;

		if (property.PropertyType.IsValueType)
			return false;

		return new NullabilityInfoContext().Create(property).WriteState == NullabilityState.NotNull;
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

			if (property.GetIndexParameters().Length > 0)
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