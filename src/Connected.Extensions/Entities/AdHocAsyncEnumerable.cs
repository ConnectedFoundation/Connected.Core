namespace Connected.Entities;

internal class AdHocAsyncEnumerable<T> : IAsyncEnumerable<T>
{
	private readonly IQueryable<T> _source;

	public AdHocAsyncEnumerable(IQueryable<T> source)
	{
		_source = source;
	}

	public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
	{
		foreach (var item in _source)
		{
			cancellationToken.ThrowIfCancellationRequested();

			yield return item;

			await Task.CompletedTask;
		}
	}
}
