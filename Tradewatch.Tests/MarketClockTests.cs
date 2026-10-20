using System.Globalization;
using Tradewatch;
using Tradewatch.ViewModels;

namespace Tradewatch.Tests;

// Runs MarketClock against the real exchange list and the real holidays.json the app ships with,
// at chosen moments. Times are written in the exchange's own local time unless the test says UTC.
public class MarketClockTests
{
    private static readonly List<Exchange> Exchanges = MainViewModel.GetExchanges();
    private static readonly Dictionary<string, Dictionary<string, string>> Holidays = new();
    private static readonly Dictionary<string, Dictionary<string, TimeSpan>> EarlyCloses = new();

    static MarketClockTests() => MainViewModel.ReadEmbeddedHolidays(Holidays, EarlyCloses);

    private static Exchange Find(string nameContains) => Exchanges.Single(e => e.Name.Contains(nameContains));

    private static MarketSnapshot AtUtc(string exchange, DateTime utc)
    {
        var e = Find(exchange);
        var tz = TimeZoneInfo.FindSystemTimeZoneById(e.TimeZone);
        Holidays.TryGetValue(e.Name, out var h);
        EarlyCloses.TryGetValue(e.Name, out var ec);
        return MarketClock.Evaluate(e, tz, DateTime.SpecifyKind(utc, DateTimeKind.Utc), h, ec);
    }

    // "yyyy-MM-dd HH:mm" in the exchange's local time
    private static MarketSnapshot At(string exchange, string local)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(Find(exchange).TimeZone);
        var t = DateTime.ParseExact(local, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        return AtUtc(exchange, TimeZoneInfo.ConvertTimeToUtc(t, tz));
    }

    // ── Regular US session, with pre-market and after-hours (2026-10-20 is a Tuesday) ──

    [Theory]
    [InlineData("2026-10-20 03:00", MarketStatus.Closed,     "Opens in 6h 30m")]
    [InlineData("2026-10-20 04:00", MarketStatus.PreMarket,  "Opens in 5h 30m")]
    [InlineData("2026-10-20 09:00", MarketStatus.PreMarket,  "Opens in 30m 00s")]
    [InlineData("2026-10-20 09:30", MarketStatus.Open,       "Closes in 6h 30m")]
    [InlineData("2026-10-20 15:59", MarketStatus.Open,       "Closes in 1m 00s")]
    [InlineData("2026-10-20 16:00", MarketStatus.AfterHours, "Closes in 4h 00m")]
    [InlineData("2026-10-20 20:00", MarketStatus.Closed,     "Opens in 13h 30m")]
    public void Nyse_weekday_sessions(string local, MarketStatus status, string countdown)
    {
        var s = At("NYSE", local);
        Assert.Equal(status, s.Status);
        Assert.Equal(countdown, s.Countdown);
    }

    [Fact]
    public void Nyse_weekend_counts_down_to_monday()
    {
        var s = At("NYSE", "2026-10-24 12:00");   // Saturday
        Assert.Equal(MarketStatus.Closed, s.Status);
        Assert.Equal("Opens in 45h 30m", s.Countdown);
    }

    [Fact]
    public void Countdown_across_dst_change_is_real_elapsed_time()
    {
        // US clocks go back 1h early on Sunday 2026-11-01, so Saturday noon → Monday 09:30 is 46h30m, not 45h30m.
        var s = At("NYSE", "2026-10-31 12:00");
        Assert.Equal("Opens in 46h 30m", s.Countdown);
        Assert.Equal(TimeSpan.FromHours(46.5), s.UntilOpen);
    }

    [Fact]
    public void Same_utc_instant_follows_us_daylight_saving()
    {
        // 13:30 UTC is 09:30 EDT in July (open) but 08:30 EST in January (pre-market).
        Assert.Equal(MarketStatus.Open, AtUtc("NYSE", new DateTime(2026, 7, 15, 13, 30, 0)).Status);
        Assert.Equal(MarketStatus.PreMarket, AtUtc("NYSE", new DateTime(2026, 1, 15, 13, 30, 0)).Status);
    }

    // ── Holidays and early closes ──

    [Fact]
    public void Holiday_is_closed_with_its_name()
    {
        var s = At("NYSE", "2026-11-26 11:00");   // Thanksgiving
        Assert.Equal(MarketStatus.Holiday, s.Status);
        Assert.Equal("Thanksgiving Day", s.HolidayName);
        Assert.Equal("Opens in 22h 30m", s.Countdown);
    }

    [Fact]
    public void Holiday_has_no_pre_market()
    {
        Assert.Equal(MarketStatus.Holiday, At("NYSE", "2026-11-26 08:00").Status);
    }

    [Fact]
    public void Early_close_shortens_the_session()
    {
        var s = At("NYSE", "2026-11-27 12:00");   // day after Thanksgiving, closes 13:00
        Assert.Equal(MarketStatus.Open, s.Status);
        Assert.Equal(new TimeSpan(13, 0, 0), s.Close);
        Assert.Equal("Closes in 1h 00m", s.Countdown);
        Assert.NotNull(s.EarlyCloseNote);
    }

    [Fact]
    public void After_hours_start_at_the_early_close()
    {
        var s = At("NYSE", "2026-11-27 13:30");
        Assert.Equal(MarketStatus.AfterHours, s.Status);
    }

    [Fact]
    public void Normal_day_has_no_early_close_note()
    {
        Assert.Null(At("NYSE", "2026-10-20 12:00").EarlyCloseNote);
    }

    [Fact]
    public void Holidays_are_found_under_a_non_gregorian_locale()
    {
        // A Thai Windows locale formats years in the Buddhist calendar (2569), which must not break date lookups.
        var old = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("th-TH");
            Assert.Equal(MarketStatus.Holiday, At("NYSE", "2026-11-26 11:00").Status);
        }
        finally { CultureInfo.CurrentCulture = old; }
    }

    // ── Lunch breaks ──

    [Theory]
    [InlineData("2026-10-20 11:29", MarketStatus.Open)]
    [InlineData("2026-10-20 11:30", MarketStatus.Closed)]
    [InlineData("2026-10-20 12:59", MarketStatus.Closed)]
    [InlineData("2026-10-20 13:00", MarketStatus.Open)]
    public void Shanghai_lunch_break(string local, MarketStatus status)
    {
        Assert.Equal(status, At("Shanghai", local).Status);
    }

    [Fact]
    public void Lunch_break_counts_down_to_the_afternoon_session()
    {
        var s = At("Shanghai", "2026-10-20 12:00");
        Assert.Equal("Opens in 1h 00m", s.Countdown);
        Assert.Equal(TimeSpan.FromHours(1), s.UntilOpen);
    }

    [Fact]
    public void Indonesia_has_a_longer_friday_lunch()
    {
        Assert.Equal(MarketStatus.Open, At("IDX", "2026-10-22 13:45").Status);     // Thursday: back at 13:30
        var friday = At("IDX", "2026-10-23 13:45");                                // Friday: back at 14:00
        Assert.Equal(MarketStatus.Closed, friday.Status);
        Assert.Equal("Opens in 15m 00s", friday.Countdown);
    }

    // ── Friday closes and Friday/Saturday weekends ──

    [Fact]
    public void Tel_aviv_closes_early_on_fridays()
    {
        Assert.Equal(MarketStatus.Open, At("TASE", "2026-10-22 14:00").Status);    // Thursday
        Assert.Equal(MarketStatus.Closed, At("TASE", "2026-10-23 14:00").Status);  // Friday, closed 13:50
    }

    [Fact]
    public void Saudi_weekend_is_friday_and_saturday()
    {
        Assert.Equal(MarketStatus.Closed, At("Tadawul", "2026-10-23 11:00").Status);   // Friday
        Assert.Equal(MarketStatus.Open, At("Tadawul", "2026-10-25 11:00").Status);     // Sunday
        // Thursday after close → Sunday 10:00
        Assert.Equal("Opens in 66h 30m", At("Tadawul", "2026-10-22 15:30").Countdown);
    }

    // ── Countdown text ──

    [Theory]
    [InlineData(0, "0s")]
    [InlineData(59, "59s")]
    [InlineData(60, "1m 00s")]
    [InlineData(3599, "59m 59s")]
    [InlineData(3600, "1h 00m")]
    [InlineData(-5, "0s")]
    public void FormatCountdown(int seconds, string expected)
    {
        Assert.Equal(expected, MarketClock.FormatCountdown(TimeSpan.FromSeconds(seconds)));
    }

    // ── Sanity checks on the exchange data itself ──

    [Fact]
    public void Every_exchange_has_a_valid_time_zone_and_hours()
    {
        Assert.All(Exchanges, e =>
        {
            Assert.NotNull(TimeZoneInfo.FindSystemTimeZoneById(e.TimeZone));
            Assert.True(e.Open < e.Close, $"{e.Name}: opens after it closes");
            if (e.LunchStart.HasValue)
            {
                Assert.True(e.Open < e.LunchStart && e.LunchStart < e.LunchEnd && e.LunchEnd < e.Close,
                    $"{e.Name}: lunch break outside the session");
            }
        });
    }

    [Fact]
    public void Every_exchange_has_holiday_data()
    {
        var missing = Exchanges.Where(e => !Holidays.ContainsKey(e.Name)).Select(e => e.Name).ToList();
        Assert.Empty(missing);
    }

    [Fact]
    public void Exchange_names_are_unique()
    {
        // Settings and holidays are keyed by name.
        Assert.Equal(Exchanges.Count, Exchanges.Select(e => e.Name).Distinct().Count());
    }
}
