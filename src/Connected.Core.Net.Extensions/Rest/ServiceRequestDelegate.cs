using System.Reflection;
using System.Text.Json.Nodes;
using TomPIT.Annotations;
using TomPIT.Interop;
using TomPIT.Services;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TomPIT.Authorization;
using System.Linq;
using TomPIT.Data.Transactions;
using Connected.Net.Rest.Formatters;

namespace Connected.Net.Rest;

internal sealed class ServiceRequestDelegate : IDisposable
{
	private IContext? _context;

	public ServiceRequestDelegate(HttpContext httpContext)
	{
		HttpContext = httpContext;

		var contextProvider = httpContext.RequestServices.GetService<IContextProvider>();

		if (contextProvider is null)
			throw new NullReferenceException(nameof(IContextProvider));

		_context = contextProvider.Create();

		HttpContext.RequestAborted.Register(() =>
		{
			Context?.Cancel();
		});

		InitializeFormatter();
	}

	private bool IsDisposed { get; set; }
	private HttpContext HttpContext { get; }
	private IContext? Context => _context;
	private Formatter Formatter { get; set; } = default!;

	private void InitializeFormatter()
	{
		var contentType = HttpContext.Request.ContentType;

		if (string.IsNullOrWhiteSpace(contentType))
			Formatter = new JsonFormatter();
		else
		{
			if (contentType.Contains(';'))
				contentType = contentType.Split(';')[0].Trim();

			if (string.Compare(contentType, JsonFormatter.ContentType, true) == 0)
				Formatter = new JsonFormatter();
			else if (string.Compare(contentType, FormFormatter.ContentType, true) == 0)
				Formatter = new FormFormatter();
			else if (string.Compare(contentType, OctetStreamFormatter.ContentType, true) == 0)
				Formatter = new OctetStreamFormatter();
			else
			{
				HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

				throw new NotSupportedException($"{SR.ErrContentTypeNotSupported} ({contentType})");
			}
		}

		Formatter.Context = HttpContext;
	}
	/// <summary>
	/// This method invokes the Api Service method with the request parameters.
	/// </summary>
	/// <returns>Result of the Api method or nothing if the methods return type is void.</returns>
	public async Task Invoke()
	{
		try
		{
			var result = await Execute();
			/*
			 * Now, commit changes made in the context.
			 */
			await Context.Commit();
			/*
			 * Send result to the client.
			 */
			await RenderResult(result);
		}
		catch
		{
			await Context.Rollback();

			throw;
		}
	}

	private async Task<object?> Execute()
	{
		/*
		 * First, try to get appropriate method (target) from the resolution service.
		 * Methods must be defined with interface which have ApiServie attribute
		 */
		if (Context is null || Context.GetService<IResolutionService>()?.ResolveMethod(HttpContext) is not InvokeDescriptor descriptor
			|| descriptor.Service is null || descriptor.Method is null)
			throw new NullReferenceException();
		/*
		 * Event if the method is found we must validate if it is defined for the current Http method.
		 */
		ValidateVerb(descriptor);
		/*
		 * Now map request arguments with the method's one.
		 */
		var arguments = await MapArguments(descriptor.Method);
		/*
		 * And instantiate the Scoped service from the DI.
		 */
		var service = Context.GetService(descriptor.Service) as IService;

		if (service is null)
			throw new NullReferenceException();
		/*
		 * Invoking the method with parsed arguments and rendering results with the formatter
		 * specified in the request content type (probably Json).
		 */
		var methodArguments = arguments is null ? new object[0] : arguments.ToArray();

		try
		{
			return await Methods.InvokeAsync(descriptor.Method, service, methodArguments);
		}
		finally
		{
			service?.Dispose();
		}
	}

	private async Task RenderResult(object? content)
	{
		await Formatter.RenderResult(content);
	}

	private void ValidateVerb(InvokeDescriptor descriptor)
	{
		if (string.Equals(HttpContext.Request.Method, HttpMethods.Get, StringComparison.OrdinalIgnoreCase))
		{
			if ((descriptor.Verbs & ServiceOperationVerbs.Get) != ServiceOperationVerbs.Get)
				throw new InvalidOperationException();
		}
		else if (string.Equals(HttpContext.Request.Method, HttpMethods.Post, StringComparison.OrdinalIgnoreCase))
		{
			if ((descriptor.Verbs & ServiceOperationVerbs.Post) != ServiceOperationVerbs.Post)
				throw new InvalidOperationException();
		}
		else if (string.Equals(HttpContext.Request.Method, HttpMethods.Put, StringComparison.OrdinalIgnoreCase))
		{
			if ((descriptor.Verbs & ServiceOperationVerbs.Put) != ServiceOperationVerbs.Put)
				throw new InvalidOperationException();
		}
		else if (string.Equals(HttpContext.Request.Method, HttpMethods.Delete, StringComparison.OrdinalIgnoreCase))
		{
			if ((descriptor.Verbs & ServiceOperationVerbs.Delete) != ServiceOperationVerbs.Delete)
				throw new InvalidOperationException();
		}
		else if (string.Equals(HttpContext.Request.Method, HttpMethods.Patch, StringComparison.OrdinalIgnoreCase))
		{
			if ((descriptor.Verbs & ServiceOperationVerbs.Patch) != ServiceOperationVerbs.Patch)
				throw new InvalidOperationException();
		}
		else if (string.Equals(HttpContext.Request.Method, HttpMethods.Options, StringComparison.OrdinalIgnoreCase))
		{
			if ((descriptor.Verbs & ServiceOperationVerbs.Options) != ServiceOperationVerbs.Options)
				throw new InvalidOperationException();
		}
	}
	/// <summary>
	/// This method maps request arguments to method arguments.
	/// </summary>
	/// <param name="method">The <see cref="MethodInfo"/> to which arguments will be mapped.</param>
	/// <returns>List of method's arguments needed to successfully invoke a method.</returns>
	/// <exception cref="SysException">Thrown if a method argument is interface but no <see cref="ArgsBindingAttribute{T}"/> is present.</exception>
	private async Task<List<object?>?> MapArguments(MethodInfo? method)
	{
		if (method is null)
			return null;

		var arguments = new List<object?>();
		var requestParams = await ParseArguments();
		/*
		 * Look for all method parameters. Note that this is already an implementation method not the interface one.
		 */
		foreach (var parameter in method.GetParameters())
		{
			/*
			 * Most Api methods will have only one parameter which inherits from IDto.
			 */
			var dtoName = typeof(IDto).FullName;

			if (dtoName is null)
				continue;

			if (parameter.ParameterType.GetInterface(dtoName) is not null && parameter.ParameterType.IsInterface)
			{
				if (requestParams is null || !requestParams.Any())
				{
					if (!parameter.IsOptional && !parameter.IsNullable())
						throw new ValidationException($"{RestStrings.ValParseRequestArguments} ('{parameter.ParameterType.ShortName()}')");

					arguments.Add(null);
				}
				else if (Context?.GetService<IResolutionService>()?.ResolveDto(parameter) is Type resolvedType)
				{
					var argument = Context.GetService(resolvedType);

					if (argument is not null)
					{
						/*
						 * Merge request properties into argument instance.
						 */
						Serializer.Merge(argument, requestParams);

						arguments.Add(argument);
					}
				}
				else
					throw new InvalidOperationException($"{RestStrings.ErrBindingAttributeMissing} ({method?.DeclaringType?.FullName}.{method?.Name})");
			}
			else
			{
				if (requestParams is null || !requestParams.Any())
				{
					if (!parameter.IsOptional && !parameter.IsNullable())
						throw new ValidationException($"{RestStrings.ValParseRequestArguments} ('{parameter.ParameterType.ShortName()}')");

					arguments.Add(null);
				}
				else
				{
					/*
					 * It's not an IDto, we are currently supporting only types from DI.
					 * We are going to support binding to 
					 */
					if (parameter.ParameterType.IsTypePrimitive())
					{
						var value = ResolvePrimitiveArgument(parameter, requestParams);

						if (value is not null)
							arguments.Add(value);
					}
					else if (Activator.CreateInstance(parameter.ParameterType) is object argument)
					{
						var binder = ResolveBinder(argument);

						if (binder is not null)
							binder.Invoke(argument, requestParams ?? new Dictionary<string, object?>());
						else
							Serializer.Merge(argument, requestParams);

						arguments.Add(argument);
					}
					else
						throw new BadHttpRequestException("Cannot bind request parameters");
				}
			}
		}

		return arguments;
	}

	private static object? ResolvePrimitiveArgument(ParameterInfo parameter, IDictionary<string, object?>? requestParams)
	{
		if (requestParams is null || parameter.Name is null)
			return null;

		if (!requestParams.TryGetValue(parameter.Name, out object? value))
			return null;

		if (TypeConversion.TryConvert(value, out object? result, parameter.ParameterType))
			return result;

		return null;
	}
	/// <summary>
	/// This method parses Request arguments into JsonNode.
	/// </summary>
	/// <returns>A JsonNode representing request parameters.</returns>
	private async Task<IDictionary<string, object?>?> ParseArguments()
	{
		var method = HttpContext.Request.Method;
		/*
		 * Post, Delete, Put and Patch methods have parameters in the request body, let formatter do the work.
		 */
		if (method.Equals(HttpMethods.Post, StringComparison.OrdinalIgnoreCase) || method.Equals(HttpMethods.Delete, StringComparison.Ordinal)
			|| method.Equals(HttpMethods.Put, StringComparison.OrdinalIgnoreCase) || method.Equals(HttpMethods.Patch, StringComparison.OrdinalIgnoreCase))
			return await Formatter.ParseArguments();
		else
		{
			/*
			 * For Get, Options and Trace use query string
			 */
			var result = new Dictionary<string, object?>();

			foreach (var i in HttpContext.Request.Query.Keys)
				result.Add(i, HttpContext.Request.Query[i].ToString());

			return result;
		}
	}

	private IDtoBinder? ResolveBinder(object argument)
	{
		foreach (var attribute in argument.GetType().GetCustomAttributes())
		{
			if (string.Equals(attribute.GetType().Name, "DtoBinderAttribute`1", StringComparison.Ordinal))
			{
				var binderType = attribute.GetType().GetGenericArguments()[0];

				return binderType.CreateInstance<IDtoBinder>();
			}
		}

		return null;
	}

	private void Dispose(bool disposing)
	{
		if (!IsDisposed)
		{
			if (disposing)
			{
				if (_context is not null)
				{
					_context.Dispose();
					_context = null;
				}
			}

			IsDisposed = true;
		}
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}
}
