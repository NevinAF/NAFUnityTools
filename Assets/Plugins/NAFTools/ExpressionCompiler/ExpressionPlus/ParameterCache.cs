using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;

#nullable disable

namespace NAF.ExpressionCompiler
{
	internal class CacheDict<TKey, TValue>
	{
		// cache size is always ^2. 
		// items are placed at [hash ^ mask]
		// new item will displace previous one at the same location.
		protected readonly int mask;
		protected readonly Entry[] entries;

		// class, to ensure atomic updates.
		internal class Entry
		{
			internal readonly int hash;
			internal readonly TKey key;
			internal readonly TValue value;

			internal Entry(int hash, TKey key, TValue value)
			{
				this.hash = hash;
				this.key = key;
				this.value = value;
			}
		}

		/// <summary>
		/// Creates a dictionary-like object used for caches.
		/// </summary>
		/// <param name="maxSize">The maximum number of elements to store will be this number aligned to next ^2.</param>
		internal CacheDict(int size) {
			var alignedSize = AlignSize(size);
			this.mask = alignedSize - 1;
			this.entries = new Entry[alignedSize];

		}
		private static int AlignSize(int size)
		{
			Debug.Assert(size > 0);

			size--;
			size |= size >> 1;
			size |= size >> 2;
			size |= size >> 4;
			size |= size >> 8;
			size |= size >> 16;
			return size + 1;
		}

		/// <summary>
		/// Tries to get the value associated with 'key', returning true if it's found and
		/// false if it's not present.
		/// </summary>
		internal bool TryGetValue(TKey key, out TValue value) {
			int hash = key.GetHashCode();
			int idx = hash & mask;

			var entry = Volatile.Read(ref this.entries[idx]);
			if (entry != null && entry.hash == hash && entry.key.Equals(key))
			{
				value = entry.value;
				return true;
			}

			value = default(TValue);
			return false;
		}

		/// <summary>
		/// Adds a new element to the cache, possibly replacing some
		/// element that is already present.
		/// </summary>
		internal void Add(TKey key, TValue value) {
			var hash = key.GetHashCode();
			var idx = hash & mask;

			var entry = Volatile.Read(ref this.entries[idx]);
			if (entry == null || entry.hash != hash || !entry.key.Equals(key))
			{
				Volatile.Write(ref entries[idx], new Entry(hash, key, value));
			}
		}

		/// <summary>
		/// Returns the value associated with the given key, or throws KeyNotFoundException
		/// if the key is not present.
		/// </summary>
		internal TValue this[TKey key] {
			get {
				TValue res;
				if (TryGetValue(key, out res)) {
					return res;
				}
				throw new KeyNotFoundException();
			}
			set {
				Add(key, value);
			}
		}
	}

	public static class ParameterCache
	{

		private static readonly CacheDict<MethodBase, ParameterInfo[]> _ParamInfoCache = new CacheDict<MethodBase, ParameterInfo[]>(75);

		internal static ParameterInfo[] GetParametersCached(this MethodBase method)
		{
			ParameterInfo[] pis;
			var pic = _ParamInfoCache;
			if (!pic.TryGetValue(method, out pis)) {
				pis = method.GetParameters();

				Type t = method.DeclaringType;
				if (t != null && TypeUtils.CanCache(t)) {
					pic[method] = pis;
				}
			}

			return pis;
		}
	}
}
