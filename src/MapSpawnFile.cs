namespace Spawns;

using System.Collections.Generic;
using System.Text.Json.Serialization;

internal sealed class MapSpawnFile
{
  [JsonPropertyName("spawnpoints")]
  public List<SpawnPoint> Spawnpoints { get; set; } = [];
}
