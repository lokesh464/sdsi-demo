using System.Text.Json.Serialization;

namespace SlaBreach.Models;

public class BreachResult
{
    [JsonPropertyName("ticketId")]
    public string TicketId { get; set; } = "";

    [JsonPropertyName("priority")]
    public string Priority { get; set; } = "";

    [JsonPropertyName("slaTarget")]
    public string SlaTarget { get; set; } = "";

    [JsonPropertyName("elapsedBusinessHours")]
    public double ElapsedBusinessHours { get; set; }

    [JsonPropertyName("slaThresholdHours")]
    public double SlaThresholdHours { get; set; }

    [JsonPropertyName("breachAmount")]
    public double BreachAmount { get; set; }

    [JsonPropertyName("breached")]
    public bool Breached { get; set; }
}

public class BreachSummary
{
    public List<BreachResult> Results { get; set; } = new();
    public Dictionary<string, int> BreachCountByPriority { get; set; } = new();
    public int TicketsProcessed { get; set; }
    public int TicketsSkipped { get; set; }
    public List<string> SkipReasons { get; set; } = new();
}
