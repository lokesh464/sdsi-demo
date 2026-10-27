using System.Text.Json;
using SlaBreach.Models;
using SlaBreach.Services;

var skipReasons = new List<string>();

DateTime now = DateTime.UtcNow;
for (int i = 0; i < args.Length; i++)
{
    if (args[i].Equals("--now", StringComparison.OrdinalIgnoreCase)
        && i + 1 < args.Length)
    {
        if (!DateTime.TryParse(args[i + 1], out now))
        {
            Console.Error.WriteLine(
                $"Invalid --now: {args[i + 1]}. Using UTC now.");
            now = DateTime.UtcNow;
        }
        else
            now = DateTime.SpecifyKind(now, DateTimeKind.Unspecified);
        break;
    }
}

// Resolve data directory: try Data/ alongside exe, then ../Data relative
// to project root (for dotnet run), then current dir.
string dataDir = Path.Combine(AppContext.BaseDirectory, "Data");
if (!Directory.Exists(dataDir))
{
    dataDir = Path.Combine(
        Directory.GetCurrentDirectory(), "..", "..", "..", "Data");
    if (!Directory.Exists(dataDir))
        dataDir = "Data";
}

var policies = DataLoader.LoadPolicies(
    Path.Combine(dataDir, "sla_policy.json"), skipReasons);
var holidays = DataLoader.LoadHolidays(
    Path.Combine(dataDir, "holiday.json"), skipReasons);
var bizCfg = DataLoader.LoadBusinessHours(
    Path.Combine(dataDir, "business_hours.json"), skipReasons);
var tickets = DataLoader.LoadTickets(
    Path.Combine(dataDir, "tickets.json"), skipReasons);

var calc = new SlaCalculator(policies, holidays, bizCfg);

var results = new List<BreachResult>();
foreach (var ticket in tickets)
{
    var r = calc.Compute(ticket, now);
    if (r != null)
        results.Add(r);
}

var breachByPrio = results
    .Where(r => r.Breached)
    .GroupBy(r => r.Priority)
    .ToDictionary(g => g.Key, g => g.Count());

var summary = new BreachSummary
{
    Results = results,
    BreachCountByPriority = breachByPrio,
    TicketsProcessed = tickets.Count,
    TicketsSkipped = skipReasons.Count,
    SkipReasons = skipReasons
};

// Console table
Console.WriteLine();
Console.WriteLine("=== SLA Breach Report ===");
Console.WriteLine($"Analysis at: {now:yyyy-MM-dd HH:mm:ss} UTC");
Console.WriteLine($"Processed: {summary.TicketsProcessed}  Skipped: {summary.TicketsSkipped}");
Console.WriteLine();
Console.WriteLine(
    $"{"ID",-12} {"Prio",-6} {"SLA Target",-18} {"Elapsed",-9} {"Limit",-7} {"Breach",-9} {"Breached"}");
Console.WriteLine(new string('-', 72));

foreach (var r in results)
{
    Console.WriteLine(
        $"{r.TicketId,-12} {r.Priority,-6} {r.SlaTarget,-18} " +
        $"{r.ElapsedBusinessHours,-9:F2} {r.SlaThresholdHours,-7:F2} " +
        $"{r.BreachAmount,-9:F2} {(r.Breached ? "YES" : "no")}");
}

Console.WriteLine();
Console.WriteLine("=== Breach Count by Priority ===");
if (!breachByPrio.Any())
    Console.WriteLine("  No breaches.");
else
    foreach (var (p, c) in breachByPrio.OrderBy(x => x.Key))
        Console.WriteLine($"  {p}: {c}");

var allByPrio = results
    .GroupBy(r => r.Priority)
    .ToDictionary(g => g.Key, g => g.Count());
Console.WriteLine();
Console.WriteLine("=== Evaluated by Priority ===");
foreach (var (p, c) in allByPrio.OrderBy(x => x.Key))
{
    var b = breachByPrio.GetValueOrDefault(p, 0);
    Console.WriteLine($"  {p}: {c} evaluated, {b} breached");
}

// Write output
var outDir = Path.Combine(
    Directory.GetCurrentDirectory(), "output");
Directory.CreateDirectory(outDir);
var json = JsonSerializer.Serialize(summary,
    new JsonSerializerOptions { WriteIndented = true });
File.WriteAllText(Path.Combine(outDir, "breaches.json"), json);
Console.WriteLine($"\nReport -> {Path.Combine(outDir, "breaches.json")}");
