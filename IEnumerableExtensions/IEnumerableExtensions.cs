using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Extensions.IEnumerables
{
    public static class IEnumerableExtensions
    {
        public static bool IsNullOrEmpty<T>(
            this IEnumerable<T> enumerable)
            => !enumerable?.Any() ?? true;

        public static bool IsNotNullOrEmpty<T>(
            this IEnumerable<T> enumerable)
            => enumerable != null && enumerable.Any();

        public static bool IsNullOrEmpty<T>(
            this ICollection<T> list) => list.IsNotNullOrEmpty();

        public static bool IsNotNullOrEmpty<T>(
            this ICollection<T> list) => list?.Count > 0;

        public static string FirstOrBlank(
            this IEnumerable<string> enumerable)
            => enumerable?.FirstOrDefault() ?? "";

        public static string LastOrBlank(
            this IEnumerable<string> enumerable)
            => enumerable?.LastOrDefault() ?? "";

        public static IList<T> TryAdd<T>(
            this IList<T> list, T item)
        {
            if (list != null && item != null)
                list.Add(item);
            return list ?? Array.Empty<T>();
        }

        public static IEnumerable<string> SplitToString(
            this string str, params char[] chars)
        {
            if (chars is null) chars = new char[] { ' ', ',', '.', '?', '!', '#', '/', '\\', '\r', '\n', '\t', '\'', '\"' };
            return str?.Split(chars, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
        }

        public static string ToEnumeratedString<T>(
            this IEnumerable<T> data, string div = ", ")
            => data is null ? "" : string.Join(div,
                data.Select(o => o?.ToString() ?? ""));

        //public static string ToEnumeratedNames<T>(
        //    this IEnumerable<T?> data, string div = ", ") where T : struct, Enum //Nullable<Enum>
        //    => data?.Select(d => d.GetName()).ToEnumeratedString(div) ?? "";

        public static async Task<IEnumerable<TResult>> SelectAsync<TSource, TResult>(
            this IEnumerable<TSource> source, Func<TSource, Task<TResult>> method, int concurrency = int.MaxValue)
        {
            var semaphore = new SemaphoreSlim(concurrency);
            try
            {
                return await Task.WhenAll(source.Select(async s =>
                {
                    try
                    {
                        await semaphore.WaitAsync();
                        return await method(s);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }
            finally
            {
                semaphore.Dispose();
            }
        }

        public static IList<TResult> FunctionAdd<TSource, TResult>( //TODO test
            this IEnumerable<TSource> items, Func<TSource, TResult> method, CancellationToken ct = default)
        {
            IList<TResult> results = new List<TResult>();
            if (items != null)
                foreach (var item in items)
                    if (!ct.IsCancellationRequested)
                        results.Add(method(item));
            return results;
        }

        // Iterates through IEnumerable<T> and applies Action<T>.
        public static void ActionEach<T>( //TODO test
            this IEnumerable<T> items, Action<T> action)
        {
            if (items != null)
                foreach (var item in items)
                    action(item);
        }
    }
}
