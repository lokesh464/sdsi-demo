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
                $"Invalid --now value: {args[i + 1]}. Using current UTC.");
            now = DateTime.UtcNow;
        }
        else
        {
            now = DateTime.SpecifyKind(now, DateTimeKind.Utc);
        }
        break;
    }
}

string dataDir = GetDataDirectory();

var policies = DataLoader.LoadPolicies(
    Path.Combine(dataDir, "sla_policy.json"), skipReasons);
var holidays = DataLoader.LoadHolidays(
    Path.Combine(dataDir, "holiday.json"), skipReasons);
var businessConfig = DataLoader.LoadBusinessHours(
    Path.Combine(dataDir, "business_hours.json"), skipReasons);
var tickets = DataLoader.LoadTickets(
    Path.Combine(dataDir, "tickets.json"), skipReasons);

var calculator = new SlaCalculator(policies, holidays, businessConfig);

var results = new List<BreachResult>();
foreach (var ticket in tickets)
{
    var result = calculator.Compute(ticket, now);
    if (result != null)
        results.Add(result);
}

var breachCountByPriority = results
    .Where(r => r.Breached)
    .GroupBy(r => r.Priority)
    .ToDictionary(g => g.Key, g => g.Count());

var summary = new BreachSummary
{
    Results = results,
    BreachCountByPriority = breachCountByPriority,
    TicketsProcessed = tickets.Count,
    TicketsSkipped = skipReasons.Count,
    SkipReasons = skipReasons
};

Console.WriteLine();
Console.WriteLine("=== SLA Breach Report ===");
Console.WriteLine($"Analysis at: {now:yyyy-MM-dd HH:mm:ss} UTC");
Console.WriteLine($"Tickets processed: {summary.TicketsProcessed}");
Console.WriteLine($"Tickets skipped: {summary.TicketsSkipped}");
Console.WriteLine();

Console.WriteLine(
    $"{"ID",-12} {"Prio",-6} {"SLA Target",-18} " +
    $"{"Elapsed",-10} {"Limit",-8} {"Breach",-10} {"Breached"}");
Console.WriteLine(new string('-', 75));

foreach (var r in results)
{
    Console.WriteLine(
        $"{r.TicketId,-12} {r.Priority,-6} {r.SlaTarget,-18} " +
        $"{r.ElapsedBusinessHours,-10:F2} {r.SlaThresholdHours,-8:F2} " +
        $"{r.BreachAmount,-10:F2} {(r.Breached ? "YES" : "no")}");
}

Console.WriteLine();
Console.WriteLine("=== Breach Count by Priority ===");
if (breachCountByPriority.Count == 0)
{
    Console.WriteLine("  No breaches found.");
}
else
{
    foreach (var (prio, count) in breachCountByPriority.OrderBy(kv => kv.Key))
        Console.WriteLine($"  {prio}: {count} breached");
}

var allByPriority = results
    .GroupBy(r => r.Priority)
    .ToDictionary(g => g.Key, g => g.Count());

Console.WriteLine();
Console.WriteLine("=== Evaluated by Priority ===");
foreach (var (prio, count) in allByPriority.OrderBy(kv => kv.Key))
{
    var breached = breachCountByPriority.GetValueOrDefault(prio, 0);
    Console.WriteLine($"  {prio}: {count} evaluated, {breached} breached");
}

string outputDir = "output";
if (!Directory.Exists(outputDir))
    Directory.CreateDirectory(outputDir);

var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
var outputJson = JsonSerializer.Serialize(summary, jsonOptions);
File.WriteAllText(Path.Combine(outputDir, "breaches.json"), outputJson);

Console.WriteLine();
Console.WriteLine(
    $"Report written to {Path.Combine(outputDir, "breaches.json")}");

static string GetDataDirectory()
{
    if (File.Exists("Data/sla_policy.json") || File.Exists("sla_policy.json"))
        return Directory.Exists("Data") ? "Data" : ".";

    var baseDir = AppContext.BaseDirectory;
    if (Directory.Exists(Path.Combine(baseDir, "Data")))
        return Path.Combine(baseDir, "Data");
    return baseDir;
}
