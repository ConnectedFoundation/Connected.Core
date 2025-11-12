using System.Threading.Tasks;
namespace Connected.Services;

public abstract class ServiceAction<TDto> : ServiceOperation<TDto>, IAction<TDto>
	where TDto : IDto
{
	public async Task Invoke(TDto dto)
	{
		Dto = dto;

		await OnInvoke();
	}

	protected virtual async Task OnInvoke()
	{
		await Task.CompletedTask;
	}
}
