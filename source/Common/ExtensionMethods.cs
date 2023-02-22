using System.Diagnostics.CodeAnalysis;

namespace Zue.Common
{
    public static class ExtensionMethods
    {
        public static bool IsValid<T>([NotNullWhen(true)] this T person) => person is not null;

        public static bool EqualsThis(this string strA, string strB) =>
            string.Equals(strA, strB, StringComparison.OrdinalIgnoreCase);

        // Pluralise with an "s"
        public static string S(this int count) =>
            Math.Abs(count) == 1 ? "" : "s";

        public static string ToPlural(this string word, int count) =>
            Math.Abs(count) == 1 ? word : word.ToPlural();

        public static string ToPlural(this string singular)
        {
            // Multiple words in the form A of B : Apply the plural to the first word only (A)
            int index = singular.LastIndexOf(" of ");
            if (index > 0) return (singular.Substring(0, index)) + singular.Remove(0, index).ToPlural();

            // single Word rules
            //sibilant ending rule
            if (singular.EndsWith("sh")) return singular + "es";
            if (singular.EndsWith("ch")) return singular + "es";
            if (singular.EndsWith("us")) return singular + "es";
            if (singular.EndsWith("ss")) return singular + "es";
            //-ies rule
            if (singular.EndsWith("y")) return singular.Remove(singular.Length - 1, 1) + "ies";
            // -oes rule
            if (singular.EndsWith("o")) return singular.Remove(singular.Length - 1, 1) + "oes";
            // -s suffix rule
            return singular + "s";
        }

        //// https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-character-encoding
        //public static readonly JsonSerializerOptions JsonOptions =
        //    new JsonSerializerOptions() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

        //public static string Serialise<T>(T data, bool useOptions = false)
        //{
        //    var jsonOptions = useOptions ? JsonOptions : null;
        //    return System.Text.Json.JsonSerializer.Serialize(data, jsonOptions);
        //}

        //// Serializing to a UTF-8 byte array is about 5-10% faster than using the string-based methods.
        //public static byte[] SerialiseToUtf8Bytes<T>(T data)
        //{
        //    return JsonSerializer.SerializeToUtf8Bytes(data);
        //}

        //public static async Task SerialiseAsync(Stream data, object value)
        //{
        //    await JsonSerializer.SerializeAsync(data, value);
        //}

        public static TimeSpan GetDelayFromNow(this TimeSpan relativeTime)
            => relativeTime.GetAbsoluteTime().GetDelayFromNow();

        public static TimeSpan GetDelayFromNow(
            this DateTime scheduledTime, TimeSpan? timeToNext = null)
        {
            var dateTimeNow = DateTime.Now;
            var delay = (dateTimeNow < scheduledTime) ?
                scheduledTime - dateTimeNow : TimeSpan.Zero;
            if (timeToNext is TimeSpan timeToAdd)
                if (timeToAdd > TimeSpan.Zero)
                    delay += timeToAdd;
            return delay;
        }

        public static DateTime GetAbsoluteTime(
           this TimeSpan relativeTime, ushort? daysToAdd = null)
        {
            var dateTimeNow = DateTime.Now;

            var scheduledTime = new DateTime(
                dateTimeNow.Year, dateTimeNow.Month, dateTimeNow.Day,
                relativeTime.Hours, relativeTime.Minutes, relativeTime.Seconds);

            if (relativeTime.Days != 0)
                scheduledTime = scheduledTime.AddDays(relativeTime.Days);

            if (daysToAdd > 0 && scheduledTime < dateTimeNow)
                scheduledTime = scheduledTime.AddDays(daysToAdd.Value);

            return scheduledTime;
        }

        public static TimeSpan GetRelativeTime(this DateTime absoluteTime, bool getDays = false)
        {
            int daysDiff = getDays ? (absoluteTime - DateTime.Now).Days : 0;
            return new TimeSpan(daysDiff, absoluteTime.Hour, absoluteTime.Minute, absoluteTime.Second);
        }
    }
}
