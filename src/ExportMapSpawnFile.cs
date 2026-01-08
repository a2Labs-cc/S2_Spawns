namespace Spawns;

using System.Collections.Generic;
using System.Text.Json.Serialization;

internal sealed class ExportMapSpawnFile
{
  [JsonPropertyName("spawnpoints")]
  public List<ExportSpawnPoint> Spawnpoints { get; set; } = [];
}
