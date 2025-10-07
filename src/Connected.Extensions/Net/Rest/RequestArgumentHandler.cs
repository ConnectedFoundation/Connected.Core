using Connected.Net.Rest.Dtos;

namespace Connected.Net.Rest;

public abstract class RequestArgumentHandler : Middleware, IRequestArgumentHandler
{
	protected IRequestArgumentDto Dto { get; private set; } = default!;
	public async Task Invoke(IRequestArgumentDto dto)
	{
		Dto = dto;

		await OnInvoke();
	}

	protected virtual async Task OnInvoke()
	{
		await Task.CompletedTask;
	}
}
