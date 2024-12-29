using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

namespace Connected.Data.Expressions.Collections;

internal class EnumerateOnce<T> : IEnumerable<T>, IEnumerable
{
	private IEnumerable<T>? _enumerable;

	public EnumerateOnce(IEnumerable<T> enumerable)
	{
		_enumerable = enumerable;
	}

	public IEnumerator<T> GetEnumerator()
	{
		var en = Interlocked.Exchange(ref _enumerable, null);

		if (en is not null)
			return en.GetEnumerator();

		throw new Exception("Enumerated more than once.");
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
