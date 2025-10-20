using System.Collections;

namespace Connected.Data.Expressions.Collections;

internal sealed class DeferredList<T>(IEnumerable<T> source)
	: IDeferredList<T>, ICollection<T>, IEnumerable<T>, IList, ICollection, IEnumerable, IDeferLoadable
{
	private IEnumerable<T> Source => source;
	private List<T> Values { get; set; } = [];

	public void Load()
	{
		if (!IsLoaded)
			Values = [.. Source];
	}

	public bool IsLoaded => Values is not null;

	#region IList<T> Members

	public int IndexOf(T item)
	{
		Load();

		return Values.IndexOf(item);
	}

	public void Insert(int index, T item)
	{
		Load();

		Values.Insert(index, item);
	}

	public void RemoveAt(int index)
	{
		Load();

		Values.RemoveAt(index);
	}

	public T this[int index]
	{
		get
		{
			Load();

			return Values[index];
		}
		set
		{
			Load();

			Values[index] = value;
		}
	}

	#endregion

	#region ICollection<T> Members

	public void Add(T item)
	{
		Load();

		Values.Add(item);
	}

	public void Clear()
	{
		Load();

		Values.Clear();
	}

	public bool Contains(T item)
	{
		Load();

		return Values.Contains(item);
	}

	public void CopyTo(T[] array, int arrayIndex)
	{
		Load();

		Values.CopyTo(array, arrayIndex);
	}

	public int Count
	{
		get
		{
			Load();

			return Values.Count;
		}
	}

	public bool IsReadOnly => false;

	public bool Remove(T item)
	{
		Load();

		return Values.Remove(item);
	}

	#endregion

	#region IEnumerable<T> Members

	public IEnumerator<T> GetEnumerator()
	{
		Load();

		return Values.GetEnumerator();
	}

	#endregion

	#region IEnumerable Members

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	#endregion

	#region IList Members

	public int Add(object? value)
	{
		Load();

		return ((IList)Values).Add(value);
	}

	public bool Contains(object? value)
	{
		Load();

		return ((IList)Values).Contains(value);
	}

	public int IndexOf(object? value)
	{
		Load();

		return ((IList)Values).IndexOf(value);
	}

	public void Insert(int index, object? value)
	{
		Load();

		((IList)Values).Insert(index, value);
	}

	public bool IsFixedSize => false;

	public void Remove(object? value)
	{
		Load();

		((IList)Values).Remove(value);
	}

	object? IList.this[int index]
	{
		get
		{
			Load();

			return ((IList)Values)[index];
		}
		set
		{
			Load();

			((IList)Values)[index] = value;
		}
	}

	#endregion

	#region ICollection Members

	public void CopyTo(Array array, int index)
	{
		Load();

		((IList)Values).CopyTo(array, index);
	}

	public bool IsSynchronized => false;
	public object SyncRoot => new();

	#endregion
}