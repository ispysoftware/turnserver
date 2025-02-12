using System;
using System.Collections.Generic;
using System.Threading;

namespace SocketServers
{
	public class ThreadSafeDictionary<K, T> where T : class
	{
		private ReaderWriterLockSlim sync;

		private Dictionary<K, T> dictionary;

		public ThreadSafeDictionary()
			: this(-1, (IEqualityComparer<K>)null)
		{
		}

		public ThreadSafeDictionary(int capacity)
			: this(capacity, (IEqualityComparer<K>)null)
		{
		}

		public ThreadSafeDictionary(int capacity, IEqualityComparer<K> comparer)
		{
			sync = new ReaderWriterLockSlim();
			if (capacity > 0)
			{
				dictionary = new Dictionary<K, T>(capacity, comparer);
			}
			else
			{
				dictionary = new Dictionary<K, T>(comparer);
			}
		}

		public void Clear()
		{
			try
			{
				sync.EnterWriteLock();
				dictionary.Clear();
			}
			finally
			{
				sync.ExitWriteLock();
			}
		}

		public void Add(K key, T value)
		{
			try
			{
				sync.EnterWriteLock();
				dictionary.Add(key, value);
			}
			finally
			{
				sync.ExitWriteLock();
			}
		}

		public bool TryAdd(K key, T value)
		{
			try
			{
				sync.EnterWriteLock();
				if (dictionary.ContainsKey(key))
				{
					return false;
				}
				dictionary.Add(key, value);
				return true;
			}
			finally
			{
				sync.ExitWriteLock();
			}
		}

		public T Replace(K key, T value)
		{
			try
			{
				sync.EnterWriteLock();
				if (dictionary.TryGetValue(key, out T value2))
				{
					dictionary.Remove(key);
				}
				dictionary.Add(key, value);
				return value2;
			}
			finally
			{
				sync.ExitWriteLock();
			}
		}

		public bool Remove(K key, T value)
		{
			bool result = false;
			try
			{
				sync.EnterWriteLock();
				if (!dictionary.TryGetValue(key, out T value2))
				{
					return result;
				}
				if (value2 == value)
				{
					return dictionary.Remove(key);
				}
				return result;
			}
			finally
			{
				sync.ExitWriteLock();
			}
		}

		public void ForEach(Action<T> action)
		{
			try
			{
				sync.EnterReadLock();
				foreach (KeyValuePair<K, T> item in dictionary)
				{
					action(item.Value);
				}
			}
			finally
			{
				sync.ExitReadLock();
			}
		}

		public bool Contain(Func<T, bool> predicate)
		{
			try
			{
				sync.EnterReadLock();
				foreach (KeyValuePair<K, T> item in dictionary)
				{
					if (predicate(item.Value))
					{
						return true;
					}
				}
			}
			finally
			{
				sync.ExitReadLock();
			}
			return false;
		}

		public void Remove(Predicate<K> match, Action<T> removed)
		{
			try
			{
				sync.EnterWriteLock();
				List<K> list = new List<K>();
				foreach (KeyValuePair<K, T> item in dictionary)
				{
					if (match(item.Key))
					{
						removed(item.Value);
						list.Add(item.Key);
					}
				}
				foreach (K item2 in list)
				{
					dictionary.Remove(item2);
				}
			}
			finally
			{
				sync.ExitWriteLock();
			}
		}

		public bool TryGetValue(K key, out T value)
		{
			try
			{
				sync.EnterReadLock();
				return dictionary.TryGetValue(key, out value);
			}
			finally
			{
				sync.ExitReadLock();
			}
		}

		public T GetValue(K key)
		{
			try
			{
				sync.EnterReadLock();
				dictionary.TryGetValue(key, out T value);
				return value;
			}
			finally
			{
				sync.ExitReadLock();
			}
		}

		public bool ContainsKey(K key)
		{
			try
			{
				sync.EnterReadLock();
				return dictionary.ContainsKey(key);
			}
			finally
			{
				sync.ExitReadLock();
			}
		}
	}
}
