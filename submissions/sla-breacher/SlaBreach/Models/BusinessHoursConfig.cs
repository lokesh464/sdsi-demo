using System.Text.Json.Serialization;

namespace SlaBreach.Models;

public class BusinessHoursConfig
{
    [JsonPropertyName("timeZone")]
    public string TimeZone { get; set; } = "";

    [JsonPropertyName("businessHoursStart")]
    public string BusinessHoursStart { get; set; } = "";

    [JsonPropertyName("businessHoursEnd")]
    public string BusinessHoursEnd { get; set; } = "";

    [JsonPropertyName("businessDays")]
    public List<string> BusinessDays { get; set; } = new();
}
