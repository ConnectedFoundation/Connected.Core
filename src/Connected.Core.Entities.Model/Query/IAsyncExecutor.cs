using System.Threading.Tasks;
using System.Threading;
using System.Linq.Expressions;

namespace Connected.Entities.Query;

public interface IAsyncExecutor
{
	Task<TResult?> Execute<TResult>(Expression expression, CancellationToken cancellationToken = default);
}