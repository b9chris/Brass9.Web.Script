using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Brass9.Data.Linq
{
	public static class IEnumerableExtensions
	{
		public static HashSet<T> ToHashSet<T>(this IEnumerable<T> enumerable)
		{
			HashSet<T> map = new HashSet<T>();
			foreach (T item in enumerable)
				map.Add(item);

			return map;
		}

		public static HashSet<TKey> ToHashSet<TSource, TKey>(this IEnumerable<TSource> enumerable, Func<TSource, TKey> keyMapFn)
		{
			HashSet<TKey> map = new HashSet<TKey>();
			foreach (TSource item in enumerable)
				map.Add(keyMapFn(item));

			return map;
		}

		public static Dictionary<TKey, TValue> ToDictionaryIgnoreCollisions<TKey, TValue>(this IEnumerable<TValue> enumerable, Func<TValue, TKey> keyMapFn)
		{
			Dictionary<TKey, TValue> dict = new Dictionary<TKey,TValue>();

			foreach (TValue item in enumerable)
			{
				if (item == null)
					continue;

				TKey key = keyMapFn(item);

				if (key == null || dict.ContainsKey(key))
					continue;

				dict.Add(key, item);
			}

			return dict;
		}
	}
}
