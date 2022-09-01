using System.Diagnostics.CodeAnalysis;

namespace Common.Data.Extensions;

internal static class DataExtensions
{
    public static bool IsNull<T>([NotNullWhen(false)]
        this IEnumerable<T> enumerable) => !enumerable?.Any() ?? true;

    public static bool IsNull<T>([NotNullWhen(false)]
        this ICollection<T> list) => !(list?.Count > 0);
}
