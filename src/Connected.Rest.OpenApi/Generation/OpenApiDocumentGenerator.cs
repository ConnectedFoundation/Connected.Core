using Connected.Annotations;
using Connected.Configuration;
using Connected.Net.Rest.OpenApi.Configuration;
using Connected.Net.Rest.OpenApi.Reflection;
using Connected.Reflection;
using Connected.Runtime;
using Connected.Services;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Connected.Net.Rest.OpenApi.Generation;

/// <summary>
/// Generates an <see cref="OpenApiDocument"/> describing every Http-accessible service operation
/// discovered by <see cref="ApiSurfaceScanner"/>.
/// </summary>
internal sealed class OpenApiDocumentGenerator(IRuntimeService runtimeService, IConfigurationService configuration, IOptionsMonitor<OpenApiOptions> options)
{
	/// <summary>
	/// Id of the Bearer security scheme declared on the generated document. Every Connected.Core
	/// Http endpoint runs through the same RequestAuthentication middleware, which reads
	/// credentials from the Authorization header, so a single shared scheme is accurate.
	/// </summary>
	public const string BearerSecurityScheme = "Bearer";

	public async Task<OpenApiDocument> Generate()
	{
		var descriptors = await ApiSurfaceScanner.Scan(runtimeService);
		var document = new OpenApiDocument
		{
			Info = new OpenApiInfo { Title = "Connected API", Version = "v1" },
			Paths = new OpenApiPaths(),
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, OpenApiSchema>(),
				SecuritySchemes = new Dictionary<string, OpenApiSecurityScheme>
				{
					[BearerSecurityScheme] = new OpenApiSecurityScheme { Type = SecuritySchemeType.Http, Scheme = "bearer" }
				}
			}
		};

		document.SecurityRequirements.Add(new OpenApiSecurityRequirement
		{
			[new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = BearerSecurityScheme } }] = []
		});

		var serverUrl = options.CurrentValue.ServerUrl;

		if (string.IsNullOrWhiteSpace(serverUrl))
			serverUrl = configuration.Routing.BaseUrl;

		if (!string.IsNullOrWhiteSpace(serverUrl))
			document.Servers.Add(new OpenApiServer { Url = serverUrl });

		var schemaBuilder = new OpenApiSchemaBuilder(document.Components.Schemas);

		foreach (var group in descriptors.GroupBy(f => f.Url, StringComparer.OrdinalIgnoreCase))
		{
			var pathItem = new OpenApiPathItem();

			foreach (var descriptor in group)
			{
				foreach (var verb in ExpandVerbs(descriptor.Verbs))
					pathItem.Operations[verb] = BuildOperation(descriptor, verb, schemaBuilder);
			}

			document.Paths[group.Key] = pathItem;
		}

		return document;
	}

	private static IEnumerable<OperationType> ExpandVerbs(ServiceOperationVerbs verbs)
	{
		if (verbs.HasFlag(ServiceOperationVerbs.Get)) yield return OperationType.Get;
		if (verbs.HasFlag(ServiceOperationVerbs.Post)) yield return OperationType.Post;
		if (verbs.HasFlag(ServiceOperationVerbs.Put)) yield return OperationType.Put;
		if (verbs.HasFlag(ServiceOperationVerbs.Delete)) yield return OperationType.Delete;
		if (verbs.HasFlag(ServiceOperationVerbs.Patch)) yield return OperationType.Patch;
		if (verbs.HasFlag(ServiceOperationVerbs.Options)) yield return OperationType.Options;
		if (verbs.HasFlag(ServiceOperationVerbs.Trace)) yield return OperationType.Trace;
	}

	private static OpenApiOperation BuildOperation(ApiOperationDescriptor descriptor, OperationType verb, OpenApiSchemaBuilder schemaBuilder)
	{
		var operation = new OpenApiOperation
		{
			/*
			 * Derived from the (unique) url rather than descriptor.Method.Name: overloaded service
			 * methods (e.g. ILanguageService.Select(IPrimaryKeyDto<int>) and
			 * Select(ISelectLanguageDto)) share a method name but map to distinct urls, and
			 * OpenAPI requires operationId to be unique document-wide. A name-based id collided
			 * across such overloads, which confused Scalar's per-operation "try it" state even
			 * though the parameters array itself was correct.
			 */
			OperationId = $"{OperationIdSegment(descriptor.Url)}_{verb}",
			Tags = [new OpenApiTag { Name = ServiceTagName(descriptor.Service) }],
			Responses = new OpenApiResponses()
		};

		/*
		 * Mirrors ServiceRequestDelegate.MapArguments/ParseArguments: read verbs bind from route
		 * values and query string, write verbs bind the same flat field set from a JSON body.
		 */
		var fields = FlattenFields(descriptor.Method, schemaBuilder);
		var usesBody = verb is OperationType.Post or OperationType.Put or OperationType.Patch;

		if (usesBody)
		{
			if (fields.Count > 0)
			{
				var bodySchema = new OpenApiSchema { Type = "object", Properties = new Dictionary<string, OpenApiSchema>() };

				foreach (var field in fields)
				{
					bodySchema.Properties[field.Name] = field.Schema;

					if (field.Required)
						bodySchema.Required.Add(field.Name);
				}

				operation.RequestBody = new OpenApiRequestBody
				{
					Content = new Dictionary<string, OpenApiMediaType>
					{
						["application/json"] = new OpenApiMediaType { Schema = bodySchema }
					}
				};
			}
		}
		else
		{
			foreach (var field in fields)
			{
				operation.Parameters.Add(new OpenApiParameter
				{
					Name = field.Name,
					In = ParameterLocation.Query,
					Required = field.Required,
					Schema = field.Schema
				});
			}
		}

		var responseType = UnwrapResponseType(descriptor.Method.ReturnType);

		operation.Responses["200"] = responseType is null
			? new OpenApiResponse { Description = "Success" }
			: new OpenApiResponse
			{
				Description = "Success",
				Content = new Dictionary<string, OpenApiMediaType>
				{
					["application/json"] = new OpenApiMediaType { Schema = schemaBuilder.Build(responseType) }
				}
			};

		return operation;
	}

	private static string OperationIdSegment(string url)
	{
		return url.Trim('/').Replace('/', '_');
	}

	private static string ServiceTagName(Type service)
	{
		var name = service.Name;

		return name.Length > 1 && name[0] == 'I' && char.IsUpper(name[1]) ? name[1..] : name;
	}

	private static Type? UnwrapResponseType(Type returnType)
	{
		if (returnType == typeof(void) || returnType == typeof(Task))
			return null;

		if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
			return returnType.GetGenericArguments()[0];

		return returnType;
	}

	private static List<(string Name, OpenApiSchema Schema, bool Required)> FlattenFields(MethodInfo method, OpenApiSchemaBuilder schemaBuilder)
	{
		var result = new List<(string Name, OpenApiSchema Schema, bool Required)>();

		foreach (var parameter in method.GetParameters())
		{
			if (typeof(IDto).IsAssignableFrom(parameter.ParameterType))
			{
				foreach (var property in parameter.ParameterType.GetInheritedProperites().DistinctBy(f => f.Name))
				{
					if (property.GetIndexParameters().Length > 0)
						continue;

					result.Add((property.Name.ToCamelCase(), schemaBuilder.Build(property.PropertyType), property.FindAttribute<RequiredAttribute>() is not null));
				}
			}
			else if (parameter.ParameterType.IsTypePrimitive() && parameter.Name is not null)
				result.Add((parameter.Name, schemaBuilder.Build(parameter.ParameterType), !parameter.IsOptional && !parameter.IsNullable()));
		}

		return [.. result.DistinctBy(f => f.Name)];
	}
}
