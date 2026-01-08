namespace Spawns;

using System.Text.Json.Serialization;

internal sealed class SpawnPoint
{
  [JsonPropertyName("team")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? Team { get; set; }

  [JsonPropertyName("pos")]
  public string? Pos { get; set; }

  [JsonPropertyName("angle")]
  public string? Angle { get; set; }
}
