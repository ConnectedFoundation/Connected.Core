using System.Threading.Tasks;

namespace Connected.Services;

public interface IFunction<TDto, TReturnValue> : IServiceOperation<TDto>
	where TDto : IDto
{
	Task<TReturnValue> Invoke(TDto dto);
}
