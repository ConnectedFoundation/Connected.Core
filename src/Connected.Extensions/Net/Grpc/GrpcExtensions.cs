using Connected.Authentication;
using Connected.Net.Grpc.Dtos;
using Connected.Reflection;
using Connected.Services;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Connected.Net.Grpc;
public static class GrpcExtensions
{
	public static async Task<TReturnValue?> Invoke<TService, TDto, TReturnValue>(this ServerCallContext context, string operation, object request, Func<AsyncServiceScope, Task>? invoking)
		where TService : notnull
		where TDto : IDto
	{
		using var scope = Scope.Create();

		var dto = Serializer.Merge(Dto.Factory.Create<TDto>(), request);
		var service = scope.ServiceProvider.GetRequiredService<TService>();
		var method = typeof(TService).ResolveMethod(operation, null, [typeof(TDto)]) ?? throw new NullReferenceException($"{Strings.ErrMethodNotFound} ('{typeof(TService).Name}.{operation}')");

		if (invoking is not null)
			await invoking(scope);

		var result = await Methods.InvokeAsync(method, service, dto);

		await scope.Commit();

		return GrpcConverter.Convert<TReturnValue>(result);
	}

	public static async Task<TReturnValue?> Invoke<TService, TDto, TReturnValue>(this ServerCallContext context, string operation, object request)
		where TService : notnull
		where TDto : IDto
	{
		return await Invoke<TService, TDto, TReturnValue>(context, operation, request, null);
	}

	public static async Task<Metadata> WithCurrentCredentials(this Metadata metadata, IAuthenticationService authentication)
	{
		var identity = await authentication.SelectIdentity();

		if (identity is null)
			return metadata;

		metadata.Add("Authorization", $"Bearer {identity.Token}");

		return metadata;
	}

	public static async Task<GrpcChannel?> ResolveChannel<TService>(this IGrpcService service)
	{
		var dto = Dto.Factory.Create<ISelectChannelDto>();

		dto.Service = typeof(TService);

		return await service.SelectChannel(dto);
	}
}
