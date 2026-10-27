using System.Text.Json;
using SlaBreach.Models;

namespace SlaBreach.Services;

public class DataLoader
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static List<Ticket> LoadTickets(string path, List<string> skipReasons)
    {
        if (!File.Exists(path))
        {
            skipReasons.Add($"Tickets file not found: {path}");
            return new();
        }

        try
        {
            var json = File.ReadAllText(path);
            var rawTickets = JsonSerializer.Deserialize<List<Ticket>>(json, JsonOpts);

            if (rawTickets == null) return new();

            var parsed = new List<Ticket>();
            foreach (var t in rawTickets)
            {
                ParseTicketDates(t);
                if (ValidateTicket(t, skipReasons))
                    parsed.Add(t);
            }
            return parsed;
        }
        catch (Exception ex)
        {
            skipReasons.Add($"Failed to parse tickets.json: {ex.Message}");
            return new();
        }
    }

    private static void ParseTicketDates(Ticket t)
    {
        t.CreatedAt = TryParseDateTime(t.CreatedAtRaw);
        t.ResolvedAt = TryParseDateTime(t.ResolvedAtRaw);

        t.OnHoldPeriods = t.OnHoldPeriodsRaw
            .Select(r =>
            {
                var s = TryParseDateTime(r.StartRaw);
                var e = TryParseDateTime(r.EndRaw);
                return s.HasValue && e.HasValue
                    ? new OnHoldPeriod { Start = s.Value, End = e.Value }
                    : null;
            })
            .Where(p => p != null)
            .Select(p => p!)
            .ToList();
    }

    private static DateTime? TryParseDateTime(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        if (DateTime.TryParse(s, out var dt))
            return DateTime.SpecifyKind(dt, DateTimeKind.Unspecified);
        return null;
    }

    public static List<SlaPolicy> LoadPolicies(string path, List<string> skipReasons)
    {
        if (!File.Exists(path))
        {
            skipReasons.Add($"SLA policy file not found: {path}");
            return new();
        }

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<List<SlaPolicy>>(json, JsonOpts)
                ?? new();
        }
        catch (Exception ex)
        {
            skipReasons.Add($"Failed to parse sla_policy.json: {ex.Message}");
            return new();
        }
    }

    public static List<Holiday> LoadHolidays(string path, List<string> skipReasons)
    {
        if (!File.Exists(path))
        {
            skipReasons.Add($"Holiday file not found: {path}");
            return new();
        }

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<List<Holiday>>(json, JsonOpts)
                ?? new();
        }
        catch (Exception ex)
        {
            skipReasons.Add($"Failed to parse holiday.json: {ex.Message}");
            return new();
        }
    }

    public static BusinessHoursConfig LoadBusinessHours(
        string path, List<string> skipReasons)
    {
        if (!File.Exists(path))
        {
            skipReasons.Add($"Business hours file not found: {path}");
            return new();
        }

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<BusinessHoursConfig>(json, JsonOpts)
                ?? new();
        }
        catch (Exception ex)
        {
            skipReasons.Add(
                $"Failed to parse business_hours.json: {ex.Message}");
            return new();
        }
    }

    private static bool ValidateTicket(Ticket ticket, List<string> skipReasons)
    {
        if (string.IsNullOrWhiteSpace(ticket.Id))
        {
            skipReasons.Add("Skipped ticket with missing ID");
            return false;
        }

        if (string.IsNullOrWhiteSpace(ticket.Priority))
        {
            skipReasons.Add(
                $"Ticket '{ticket.Id}': missing priority");
            return false;
        }

        if (!ticket.CreatedAt.HasValue || ticket.CreatedAt == default)
        {
            skipReasons.Add(
                $"Ticket '{ticket.Id}': invalid or missing createdAt");
            return false;
        }

        var isResolved = ticket.Status?.Equals(
            "resolved", StringComparison.OrdinalIgnoreCase) == true;
        if (isResolved
            && (!ticket.ResolvedAt.HasValue || ticket.ResolvedAt.Value == default))
        {
            skipReasons.Add(
                $"Ticket '{ticket.Id}': resolved but invalid resolvedAt");
            return false;
        }

        return true;
    }
}
