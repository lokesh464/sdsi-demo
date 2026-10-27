using System.Text.Json.Serialization;

namespace SlaBreach.Models;

public class Holiday
{
    [JsonPropertyName("date")]
    public DateOnly Date { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
}
