using System.Text.Json;
using SlaBreach.Models;

namespace SlaBreach.Services;

public class DataLoader
{
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
            var tickets = JsonSerializer.Deserialize<List<Ticket>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return tickets?.Where(t => ValidateTicket(t, skipReasons)).ToList()
                ?? new();
        }
        catch (Exception ex)
        {
            skipReasons.Add($"Failed to parse tickets.json: {ex.Message}");
            return new();
        }
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
            return JsonSerializer.Deserialize<List<SlaPolicy>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
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
            return JsonSerializer.Deserialize<List<Holiday>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
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
            return JsonSerializer.Deserialize<BusinessHoursConfig>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
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
                $"Ticket '{ticket.Id}': missing priority — skipped");
            return false;
        }

        if (!ticket.CreatedAt.HasValue || ticket.CreatedAt == default)
        {
            skipReasons.Add(
                $"Ticket '{ticket.Id}': invalid or missing createdAt — skipped");
            return false;
        }

        var isResolved = ticket.Status?.Equals(
            "resolved", StringComparison.OrdinalIgnoreCase) == true;
        if (isResolved
            && (!ticket.ResolvedAt.HasValue || ticket.ResolvedAt.Value == default))
        {
            skipReasons.Add(
                $"Ticket '{ticket.Id}': marked resolved but invalid resolvedAt — skipped");
            return false;
        }

        return true;
    }
}
