using SlaBreach.Models;

namespace SlaBreach.Services;

public class SlaCalculator
{
    private readonly Dictionary<string, SlaPolicy> _policies;
    private readonly HashSet<DateOnly> _holidays;
    private readonly HashSet<int> _businessDays;
    private readonly TimeOnly _businessStart;
    private readonly TimeOnly _businessEnd;
    private readonly TimeZoneInfo _timeZone;

    public SlaCalculator(
        List<SlaPolicy> policies,
        List<Holiday> holidays,
        BusinessHoursConfig config)
    {
        _policies = policies
            .Where(p => !string.IsNullOrWhiteSpace(p.Priority))
            .ToDictionary(p => p.Priority, p => p);

        _holidays = holidays
            .Where(h => h.Date != default)
            .Select(h => h.Date)
            .ToHashSet();

        _businessDays = config.BusinessDays
            .Select(d => d.Trim().ToLowerInvariant() switch
            {
                "sunday" => 0,
                "monday" => 1,
                "tuesday" => 2,
                "wednesday" => 3,
                "thursday" => 4,
                "friday" => 5,
                "saturday" => 6,
                _ => -1
            })
            .Where(d => d >= 0)
            .ToHashSet();

        _businessStart = ParseTimeOnly(config.BusinessHoursStart);
        _businessEnd = ParseTimeOnly(config.BusinessHoursEnd);

        var tzId = MapWindowsTz(config.TimeZone);
        _timeZone = TimeZoneInfo.FindSystemTimeZoneById(tzId);
    }

    public BreachResult? Compute(Ticket ticket, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(ticket.Id))
            return null;

        if (!_policies.TryGetValue(ticket.Priority, out var policy))
            return null;

        if (policy.SlaTarget.Equals("none", StringComparison.OrdinalIgnoreCase)
            || policy.MaxBusinessHours <= 0)
            return null;

        var startUtc = NormalizeToUtc(ticket.CreatedAt);
        if (startUtc == null) return null;

        var endUtc = ticket.Status.Equals("resolved",
            StringComparison.OrdinalIgnoreCase)
            ? NormalizeToUtc(ticket.ResolvedAt)
            : NormalizeToUtc(now);

        if (endUtc == null) return null;
        if (endUtc < startUtc) return null;

        var onHoldSpans = ticket.OnHoldPeriods
            ?.Select(p =>
            {
                var s = NormalizeToUtc(p.Start);
                var e = NormalizeToUtc(p.End);
                return s != null && e != null && e > s
                    ? (Start: s.Value, End: e.Value)
                    : ((DateTime Start, DateTime End)?)null;
            })
            .Where(p => p.HasValue)
            .Select(p => p!.Value)
            .OrderBy(p => p.Start)
            .ToList() ?? new();

        onHoldSpans = MergeSpans(onHoldSpans);

        double businessHours = ComputeBusinessHours(
            startUtc.Value, endUtc.Value, onHoldSpans);

        double breachAmount = Math.Max(0,
            businessHours - policy.MaxBusinessHours);

        return new BreachResult
        {
            TicketId = ticket.Id,
            Priority = ticket.Priority,
            SlaTarget = BuildSlaTargetLabel(policy),
            ElapsedBusinessHours = Math.Round(businessHours, 2),
            SlaThresholdHours = policy.MaxBusinessHours,
            BreachAmount = Math.Round(breachAmount, 2),
            Breached = breachAmount > 0
        };
    }

    internal double ComputeBusinessHours(
        DateTime startUtc,
        DateTime endUtc,
        List<(DateTime Start, DateTime End)> onHoldSpans)
    {
        double total = 0;
        var current = startUtc;

        const int maxSteps = 365 * 24 * 4;
        int steps = 0;

        while (current < endUtc && steps < maxSteps)
        {
            steps++;

            var inHold = onHoldSpans
                .FirstOrDefault(h => current >= h.Start && current < h.End);
            if (inHold != default)
            {
                current = inHold.End;
                continue;
            }

            var local = TimeZoneInfo.ConvertTimeFromUtc(current, _timeZone);
            var date = DateOnly.FromDateTime(local);

            if (IsBusinessDay(local.DayOfWeek) && !_holidays.Contains(date))
            {
                var time = TimeOnly.FromDateTime(local);
                if (time >= _businessStart && time < _businessEnd)
                {
                    total += 0.25;
                }
            }

            current = current.AddMinutes(15);
        }

        return total;
    }

    internal bool IsBusinessDay(DayOfWeek day)
        => _businessDays.Contains((int)day);

    internal bool IsHoliday(DateOnly date)
        => _holidays.Contains(date);

    private DateTime? NormalizeToUtc(DateTime? dt)
    {
        if (dt == null) return null;
        try
        {
            if (dt.Value.Kind == DateTimeKind.Utc)
                return dt.Value;
            var unspecified = DateTime.SpecifyKind(dt.Value,
                DateTimeKind.Unspecified);
            return TimeZoneInfo.ConvertTimeToUtc(unspecified, _timeZone);
        }
        catch
        {
            return null;
        }
    }

    private static TimeOnly ParseTimeOnly(string time)
    {
        if (TimeSpan.TryParse(time, out var ts))
            return TimeOnly.FromTimeSpan(ts);
        return new TimeOnly(9, 0);
    }

    internal static string MapWindowsTz(string tz)
    {
        return (tz.Trim()) switch
        {
            "America/New_York" or "US/Eastern" => "Eastern Standard Time",
            "America/Chicago" or "US/Central" => "Central Standard Time",
            "America/Denver" or "US/Mountain" => "Mountain Standard Time",
            "America/Los_Angeles" or "US/Pacific" => "Pacific Standard Time",
            "Europe/London" => "GMT Standard Time",
            "Europe/Paris" or "Europe/Berlin" => "W. Europe Standard Time",
            "UTC" or "Etc/UTC" => "UTC",
            _ => tz.Trim()
        };
    }

    private static string BuildSlaTargetLabel(SlaPolicy p)
    {
        var target = p.SlaTarget ?? "";
        if (target.Equals("none", StringComparison.OrdinalIgnoreCase))
            return "none";
        return $"{target} ({p.MaxBusinessHours}h)";
    }

    internal static List<(DateTime Start, DateTime End)> MergeSpans(
        List<(DateTime Start, DateTime End)> spans)
    {
        if (spans.Count <= 1)
            return new List<(DateTime, DateTime)>(spans);

        var merged = new List<(DateTime Start, DateTime End)>();
        var current = spans[0];

        for (int i = 1; i < spans.Count; i++)
        {
            if (spans[i].Start <= current.End)
            {
                current = (
                    current.Start,
                    current.End > spans[i].End ? current.End : spans[i].End);
            }
            else
            {
                merged.Add(current);
                current = spans[i];
            }
        }
        merged.Add(current);
        return merged;
    }
}
