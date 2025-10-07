using System.Threading.Tasks;
using System;

namespace Connected.Threading;

public class AsyncLazy<T> : Lazy<Task<T>>
{
	public AsyncLazy(Task<T> value)
		: base(value)
	{

	}
}
