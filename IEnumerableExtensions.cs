namespace Extensions.IEnumerable
{
    public static class IEnumerableExtensions
    {
        public static bool IsNullOrEmpty(this IEnumerable enumerable) => !enumerable?.Any() ?? true;
        public static bool IsNotNullOrEmpty(this IEnumerable enumerable) => !enumerable.IsNullOrEmpty();
        public static bool IsNotNullOrEmpty<T>(this IList<T> list) => list?.Count > 0;
        public static bool IsNullOrEmpty<T>(this IList<T> list) => !list.IsNotNullOrEmpty();
        
        public static string ToEnumeratedString(this IEnumerable enumerable, string delimiter) =>
            enumerabele is null ? Array.Empty<string>() : string.Join(
                delimiter, enumerable.Select(e => e?.ToString() ?? ""));
    }
}