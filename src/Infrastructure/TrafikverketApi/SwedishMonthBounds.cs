using System.Globalization;

namespace TvTidsserieembedding.Infrastructure.TrafikverketApi;

/// <summary>
/// Kalendergränser i svensk väggtid (Europe/Stockholm) för Trafikverkets <c>Observation.Sample</c>-filter.
/// </summary>
public static class SwedishMonthBounds
{
    private static readonly Lazy<TimeZoneInfo> SwedenTz = new(ResolveSweden);

    private static TimeZoneInfo ResolveSweden()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Stockholm");
        }
        catch
        {
            return TimeZoneInfo.Utc;
        }
    }

    /// <param name="monthsAgo">1 = föregående kalendermånad, 2 = månaden före den.</param>
    public static (DateTime FromInclusive, DateTime ToInclusive, string LabelSv) GetCalendarMonthMonthsAgo(
        int monthsAgo)
    {
        if (monthsAgo < 1)
            monthsAgo = 1;

        var nowSweden = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, SwedenTz.Value);
        var firstOfThisMonth = new DateTime(nowSweden.Year, nowSweden.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var targetMonth = firstOfThisMonth.AddMonths(-monthsAgo);
        var y = targetMonth.Year;
        var m = targetMonth.Month;
        var from = new DateTime(y, m, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var lastDay = DateTime.DaysInMonth(y, m);
        var to = new DateTime(y, m, lastDay, 23, 59, 59, DateTimeKind.Unspecified);
        var culture = new CultureInfo("sv-SE");
        var label = $"{from.ToString("MMMM", culture)} {from.Year}";
        if (label.Length > 0)
            label = char.ToUpper(label[0], culture) + label[1..];
        return (from, to, label);
    }

    /// <summary>
    /// Slumpmässig <see cref="DateTime"/> (UTC) inom kalendermånaden <paramref name="monthsAgo"/> räknat i svensk väggtid.
    /// </summary>
    public static DateTime RandomUtcInSwedishCalendarMonthMonthsAgo(int monthsAgo, Random rnd)
    {
        var (from, to, _) = GetCalendarMonthMonthsAgo(monthsAgo);
        var tz = SwedenTz.Value;
        var fromLocal = DateTime.SpecifyKind(from, DateTimeKind.Unspecified);
        var toLocal = DateTime.SpecifyKind(to, DateTimeKind.Unspecified);
        var fromUtc = TimeZoneInfo.ConvertTimeToUtc(fromLocal, tz);
        var toUtc = TimeZoneInfo.ConvertTimeToUtc(toLocal, tz);
        var spanTicks = toUtc.Ticks - fromUtc.Ticks;
        if (spanTicks <= 0)
            return fromUtc;
        var offset = (long)(rnd.NextDouble() * spanTicks);
        return new DateTime(fromUtc.Ticks + offset, DateTimeKind.Utc);
    }
}
