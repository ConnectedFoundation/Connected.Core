using Connected.Entities;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace Connected.Collections.Tagging;

public class TagIndexer<TEntity, TPrimaryKey>
	where TEntity : ITaggedEntity<TPrimaryKey>
	where TPrimaryKey : notnull
{
	public const char TagDelimiter = ',';

	public TagIndexer()
	{
		Items = new(StringComparer.OrdinalIgnoreCase);
	}
	private ConcurrentDictionary<string, List<TEntity>> Items { get; }

	public void Index(IImmutableList<TEntity>? items)
	{
		Items.Clear();

		if (items is null)
			return;

		foreach (var item in items)
			Index(item);
	}

	public void Invalidate(TEntity item)
	{
		Remove(item.Id);
		Index(item);
	}

	public void Remove(TPrimaryKey id)
	{
		foreach (var key in Items)
		{
			var items = key.Value.Where(f => Comparer.Default.Compare(f.Id, id) == 0).ToImmutableList();

			if (items.IsEmpty)
				continue;

			lock (key.Value)
			{
				foreach (var existing in items)
					key.Value.Remove(existing);
			}
		}
	}

	private void Index(TEntity item)
	{
		if (item.Tags is null)
			return;

		var tokens = item.Tags.Split(TagDelimiter);

		foreach (var token in tokens)
		{
			if (Items.TryGetValue(token, out List<TEntity>? entities) && entities is not null)
			{
				if (!entities.Contains(item))
					entities.Add(item);
			}
			else
			{
				entities = [item];
				Items.TryAdd(token, entities);
			}
		}
	}

	public IImmutableList<TEntity> Query(List<string> tags)
	{
		var result = new List<TEntity>();

		foreach (var token in tags)
		{
			if (Items.TryGetValue(token, out List<TEntity>? items) && items is not null)
			{
				foreach (var entity in items)
				{
					if (result.Contains(entity))
						continue;

					result.Add(entity);
				}
			}
		}

		return result.ToImmutableList();
	}

	public IImmutableList<TEntity> Query(string tags)
	{
		return Query([.. tags.Split(TagDelimiter, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)]);
	}
}
