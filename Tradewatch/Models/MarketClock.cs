using System;
using System.Collections.Generic;
using System.Globalization;

namespace Tradewatch
{
    // One exchange's state at one instant. Countdown is the text shown in the grid.
    public readonly record struct MarketSnapshot(
        MarketStatus Status,
        DateTime LocalTime,
        TimeSpan Close,
        TimeSpan NormalClose,
        string? HolidayName,
        string? EarlyCloseNote,
        string Countdown,
        TimeSpan UntilOpen);   // time until the regular session (re)opens; Zero while open

    // The open/closed rules, kept free of UI state and the real clock so they can be unit-tested.
    public static class MarketClock
    {
        public static MarketSnapshot Evaluate(Exchange e, TimeZoneInfo tz, DateTime nowUtc,
            IReadOnlyDictionary<string, string>? holidays, IReadOnlyDictionary<string, TimeSpan>? earlyCloses)
        {
            var localTime = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, tz);
            var now = localTime.TimeOfDay;

            string? holidayName = null;
            bool isHoliday = holidays != null && holidays.TryGetValue(DateKey(localTime), out holidayName);
            bool isWeekend = e.WeekendDays.Contains(localTime.DayOfWeek);
            bool isTradingDay = !isHoliday && !isWeekend;

            var earlyClose = EarlyCloseOn(e, localTime.Date, earlyCloses, out var normalClose);
            var close = earlyClose ?? normalClose;
            // On early-close days after-hours trading starts at the early close (e.g. NYSE 13:00).
            var afterHoursOpen = earlyClose.HasValue && e.AfterHoursOpen.HasValue ? earlyClose : e.AfterHoursOpen;
            var lunchEnd = LunchEndOn(e, localTime.DayOfWeek);

            bool inLunch = isTradingDay
                        && e.LunchStart.HasValue && lunchEnd.HasValue
                        && now >= e.LunchStart.Value && now < lunchEnd.Value;
            bool isOpen = isTradingDay && !inLunch && now >= e.Open && now < close;
            bool isPreMarket = isTradingDay && !isOpen
                        && e.PreMarketOpen.HasValue && e.PreMarketClose.HasValue
                        && now >= e.PreMarketOpen.Value && now < e.PreMarketClose.Value;
            bool isAfterHours = isTradingDay && !isOpen
                        && afterHoursOpen.HasValue && e.AfterHoursClose.HasValue
                        && now >= afterHoursOpen.Value && now < e.AfterHoursClose.Value;

            var status = isOpen       ? MarketStatus.Open
                       : isPreMarket  ? MarketStatus.PreMarket
                       : isAfterHours ? MarketStatus.AfterHours
                       : isHoliday    ? MarketStatus.Holiday
                                      : MarketStatus.Closed;

            // Real elapsed time (via UTC), so countdowns across a DST change are right.
            TimeSpan Until(DateTime local) => ToUtc(local, tz) - nowUtc;
            var untilOpen = isOpen ? TimeSpan.Zero
                          : inLunch ? Until(localTime.Date + lunchEnd!.Value)
                          : Until(GetNextOpen(e, localTime, holidays));

            string countdown = status == MarketStatus.Open
                ? "Closes in " + FormatCountdown(Until(localTime.Date + close))
                : status == MarketStatus.AfterHours
                ? "Closes in " + FormatCountdown(Until(localTime.Date + e.AfterHoursClose!.Value))
                : "Opens in " + FormatCountdown(untilOpen);

            string? earlyCloseNote = earlyClose.HasValue && isTradingDay
                ? $"Early close today at {close:hh\\:mm} (normally {normalClose:hh\\:mm})"
                : null;

            return new MarketSnapshot(status, localTime, close, normalClose,
                isHoliday ? holidayName : null, earlyCloseNote, countdown, untilOpen);
        }

        public static TimeSpan CloseOn(Exchange e, DayOfWeek day)
            => day == DayOfWeek.Friday && e.FridayClose.HasValue ? e.FridayClose.Value : e.Close;

        public static TimeSpan? LunchEndOn(Exchange e, DayOfWeek day)
            => day == DayOfWeek.Friday && e.FridayLunchEnd.HasValue ? e.FridayLunchEnd : e.LunchEnd;

        // The early close for that exchange-local date, if earlier than the normal close.
        public static TimeSpan? EarlyCloseOn(Exchange e, DateTime localDate,
            IReadOnlyDictionary<string, TimeSpan>? earlyCloses, out TimeSpan normalClose)
        {
            normalClose = CloseOn(e, localDate.DayOfWeek);
            return earlyCloses != null && earlyCloses.TryGetValue(DateKey(localDate), out var ec) && ec < normalClose
                ? ec : null;
        }

        // Exchange-local date+time of the next regular open (today if it hasn't opened yet).
        public static DateTime GetNextOpen(Exchange e, DateTime localTime, IReadOnlyDictionary<string, string>? holidays)
        {
            var today = localTime.Date;
            bool IsTradingDay(DateTime d) => !e.WeekendDays.Contains(d.DayOfWeek)
                                          && (holidays == null || !holidays.ContainsKey(DateKey(d)));

            if (IsTradingDay(today) && localTime.TimeOfDay < e.Open)
                return today + e.Open;

            for (int i = 1; i <= 14; i++)
                if (IsTradingDay(today.AddDays(i)))
                    return today.AddDays(i) + e.Open;

            return localTime;
        }

        public static DateTime ToUtc(DateTime local, TimeZoneInfo tz)
        {
            local = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
            if (tz.IsInvalidTime(local)) local = local.AddHours(1);  // skipped by a DST jump
            return TimeZoneInfo.ConvertTimeToUtc(local, tz);
        }

        public static string FormatCountdown(TimeSpan span)
        {
            if (span < TimeSpan.Zero) span = TimeSpan.Zero;
            if (span.TotalHours >= 1)
                return $"{(int)span.TotalHours}h {span.Minutes:D2}m";
            if (span.TotalMinutes >= 1)
                return $"{span.Minutes}m {span.Seconds:D2}s";
            return $"{span.Seconds}s";
        }

        // Invariant culture: a Thai or Arabic Windows locale would otherwise change the calendar/year.
        private static string DateKey(DateTime d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }
}
