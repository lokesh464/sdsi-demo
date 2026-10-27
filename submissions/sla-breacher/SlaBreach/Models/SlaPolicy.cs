using System.Text.Json.Serialization;

namespace SlaBreach.Models;

public class SlaPolicy
{
    [JsonPropertyName("priority")]
    public string Priority { get; set; } = "";

    [JsonPropertyName("slaTarget")]
    public string SlaTarget { get; set; } = "";

    [JsonPropertyName("maxBusinessHours")]
    public double MaxBusinessHours { get; set; }

    [JsonPropertyName("maxResolutionBusinessHours")]
    public double MaxResolutionBusinessHours { get; set; }
}
