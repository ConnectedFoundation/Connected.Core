namespace Connected.Threading;

public class AsyncLazy<T>(Task<T> value)
	: Lazy<Task<T>>(value)
{
}
