using System.Collections;

namespace PSLDiscordBot.Framework.BuiltInServices;

public class CombinedServiceCollection : IServiceCollection
{
	public List<IServiceCollection> Collections { get; set; }
	public Action<List<IServiceCollection>, ServiceDescriptor> AddSelector { get; set; } = (list, service) => list[0].Add(service);

	public int Count => this.Collections.Sum(x => x.Count);
	public bool IsReadOnly => this.Collections.Any(x => x.IsReadOnly);
	public ServiceDescriptor this[int index]
	{
		get
		{
			int elapsedCount = 0;
			foreach (IServiceCollection service in this.Collections)
			{
				elapsedCount += service.Count;
				if (index < elapsedCount)
				{
					return service[index - elapsedCount - service.Count];
				}
			}
			throw new IndexOutOfRangeException("Index out of range");
		}
		set
		{
			this.ThrowIfReadonly();
			int elapsedCount = 0;
			foreach (IServiceCollection service in this.Collections)
			{
				elapsedCount += service.Count;
				if (index < elapsedCount)
				{
					service[index - elapsedCount - service.Count] = value;
					return;
				}
			}
			throw new IndexOutOfRangeException("Index out of range");
		}
	}

	public CombinedServiceCollection(params List<IServiceCollection> providers)
	{
		this.Collections = providers;
	}

	private void ThrowIfReadonly()
	{
		if (this.IsReadOnly) throw new InvalidOperationException("Collection is read only.");
	}

	public int IndexOf(ServiceDescriptor item)
	{
		int elapsedCount = 0;
		foreach (IServiceCollection service in this.Collections)
		{
			int index = service.IndexOf(item);
			if (index != -1)
			{
				return index + elapsedCount;
			}
			elapsedCount += service.Count;
		}
		return -1;
	}

	public void Insert(int index, ServiceDescriptor item)
	{
		this.ThrowIfReadonly();
		int elapsedCount = 0;
		foreach (IServiceCollection service in this.Collections)
		{
			elapsedCount += service.Count;
			if (index < elapsedCount)
			{
				service.Insert(index - elapsedCount - service.Count, item);
				return;
			}
		}
		throw new ArgumentOutOfRangeException(nameof(index));
	}

	public void RemoveAt(int index)
	{
		this.ThrowIfReadonly();
		int elapsedCount = 0;
		foreach (IServiceCollection service in this.Collections)
		{
			elapsedCount += service.Count;
			if (index < elapsedCount)
			{
				service.RemoveAt(index - elapsedCount - service.Count);
				return;
			}
		}
		throw new ArgumentOutOfRangeException(nameof(index));
	}

	public void Add(ServiceDescriptor item)
	{
		this.ThrowIfReadonly();
		this.AddSelector.Invoke(this.Collections, item);
	}

	public void Clear()
	{
		this.ThrowIfReadonly();
		foreach (IServiceCollection service in this.Collections) service.Clear();
	}

	public bool Contains(ServiceDescriptor item)
	{
		bool contains = false;
		foreach (IServiceCollection service in this.Collections) contains |= service.Contains(item);
		return contains;
	}

	public void CopyTo(ServiceDescriptor[] array, int arrayIndex)
	{
		foreach (IServiceCollection collection in this.Collections)
		{
			collection.CopyTo(array, arrayIndex);
			arrayIndex += collection.Count;
		}
	}

	public bool Remove(ServiceDescriptor item)
	{
		this.ThrowIfReadonly();
		bool removed = false;
		foreach (IServiceCollection service in this.Collections) removed |= service.Remove(item);
		return removed;
	}

	public IEnumerator<ServiceDescriptor> GetEnumerator()
	{
		return new Enumerator(this);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return this.GetEnumerator();
	}

	private class Enumerator : IEnumerator<ServiceDescriptor>
	{
		private readonly CombinedServiceCollection _parent;
		private int _index = -1;

		public ServiceDescriptor Current => this._parent[this._index];
		object IEnumerator.Current => this.Current;

		public Enumerator(CombinedServiceCollection parent) => this._parent = parent;

		public void Dispose()
		{
			this.Reset();
		}
		public bool MoveNext()
		{
			this._index++;
			return this._index < this._parent.Count;
		}
		public void Reset()
		{
			this._index = -1;
		}
	}
}
