namespace Spawns;

using System.Text.Json.Serialization;

internal sealed class ExportSpawnPoint
{
  [JsonPropertyName("pos")]
  public string? Pos { get; set; }

  [JsonPropertyName("angle")]
  public string? Angle { get; set; }
}
