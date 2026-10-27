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
    public string? CreatedAtRaw { get; set; }

    [JsonPropertyName("resolvedAt")]
    public string? ResolvedAtRaw { get; set; }

    [JsonPropertyName("onHoldPeriods")]
    public List<OnHoldPeriodRaw> OnHoldPeriodsRaw { get; set; } = new();

    [JsonIgnore]
    public DateTime? CreatedAt { get; set; }

    [JsonIgnore]
    public DateTime? ResolvedAt { get; set; }

    [JsonIgnore]
    public List<OnHoldPeriod> OnHoldPeriods { get; set; } = new();
}

public class OnHoldPeriodRaw
{
    [JsonPropertyName("start")]
    public string? StartRaw { get; set; }

    [JsonPropertyName("end")]
    public string? EndRaw { get; set; }
}

public class OnHoldPeriod
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
}
