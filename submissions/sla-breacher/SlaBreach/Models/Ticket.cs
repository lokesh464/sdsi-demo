using System.Text.Json.Serialization;

namespace SlaBreach.Models;

public class Ticket
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("priority")]
    public string Priority { get; set; } = "";

    [JsonPropertyName("status")]
    public string Status { get; set; } = "";

    [JsonPropertyName("createdAt")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("resolvedAt")]
    public DateTime? ResolvedAt { get; set; }

    [JsonPropertyName("onHoldPeriods")]
    public List<OnHoldPeriod> OnHoldPeriods { get; set; } = new();
}

public class OnHoldPeriod
{
    [JsonPropertyName("start")]
    public DateTime Start { get; set; }

    [JsonPropertyName("end")]
    public DateTime End { get; set; }
}
