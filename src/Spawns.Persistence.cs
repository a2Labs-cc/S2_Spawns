using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text.Json;

namespace Spawns;

public partial class Spawns
{
  private string GetDataDir()
  {
    return Core.PluginDataDirectory;
  }

  private string GetSpawnFilePath(string mapName)
  {
    return Path.Combine(GetDataDir(), $"{mapName}.json");
  }

  private bool EnsureSpawnFileLoaded(string mapName, bool createIfMissing)
  {
    if (LoadedSpawnFile is not null && string.Equals(LoadedMapName, mapName, StringComparison.OrdinalIgnoreCase))
    {
      return true;
    }

    var dir = GetDataDir();
    var filePath = GetSpawnFilePath(mapName);
    var legacyFilePath = Path.Combine(Core.PluginDataDirectory, "data", $"{mapName}.json");

    lock (SpawnFileLock)
    {
      try
      {
        Directory.CreateDirectory(dir);
      }
      catch (Exception ex)
      {
        Core.Logger.LogError(ex, "[Spawns] Failed to create data directory: {Dir}", dir);
        return false;
      }

      if (!File.Exists(filePath))
      {
        if (File.Exists(legacyFilePath))
        {
          try
          {
            var json = File.ReadAllText(legacyFilePath);
            LoadedSpawnFile = JsonSerializer.Deserialize<MapSpawnFile>(json, JsonOptions) ?? new MapSpawnFile();
            LoadedSpawnFile.Spawnpoints ??= [];
            LoadedMapName = mapName;
            return true;
          }
          catch (Exception ex)
          {
            Core.Logger.LogError(ex, "[Spawns] Failed to load legacy spawn file: {FilePath}", legacyFilePath);
          }
        }

        if (!createIfMissing)
        {
          return false;
        }

        try
        {
          var empty = new MapSpawnFile();
          var json = JsonSerializer.Serialize(empty, JsonOptions);
          File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
          Core.Logger.LogError(ex, "[Spawns] Failed to create spawn file: {FilePath}", filePath);
          return false;
        }
      }

      try
      {
        var json = File.ReadAllText(filePath);
        LoadedSpawnFile = JsonSerializer.Deserialize<MapSpawnFile>(json, JsonOptions) ?? new MapSpawnFile();
        LoadedSpawnFile.Spawnpoints ??= [];
        LoadedMapName = mapName;
        return true;
      }
      catch (Exception ex)
      {
        Core.Logger.LogError(ex, "[Spawns] Failed to load spawn file: {FilePath}", filePath);
        LoadedSpawnFile = new MapSpawnFile();
        LoadedMapName = mapName;
        return true;
      }
    }
  }

  private bool SaveLoadedSpawnFile(string mapName)
  {
    if (!EnsureSpawnFileLoaded(mapName, true))
    {
      return false;
    }

    var dir = GetDataDir();
    var filePath = GetSpawnFilePath(mapName);

    lock (SpawnFileLock)
    {
      try
      {
        Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(LoadedSpawnFile ?? new MapSpawnFile(), JsonOptions);
        var tmp = filePath + ".tmp";
        File.WriteAllText(tmp, json);
        File.Move(tmp, filePath, true);
        return true;
      }
      catch (Exception ex)
      {
        Core.Logger.LogError(ex, "[Spawns] Failed to save spawn file: {FilePath}", filePath);
        return false;
      }
    }
  }

  private void ApplyInfiniteWarmupSettings()
  {
    var gameRules = Core.EntitySystem.GetGameRules();
    if (gameRules is not null && gameRules.IsValid)
    {
      var now = Core.Engine.GlobalVars.CurrentTime;

      gameRules.WarmupPeriod = true;
      gameRules.WarmupPeriodUpdated();

      gameRules.WarmupPeriodStart.Value = now;
      gameRules.WarmupPeriodStartUpdated();

      gameRules.WarmupPeriodEnd.Value = now + 999999f;
      gameRules.WarmupPeriodEndUpdated();
    }
    else
    {
      Core.Logger.LogWarning("[Spawns] GameRules not valid yet; cannot force warmup via GameRules (gameRules null={IsNull}).", gameRules is null);
    }

    var doWarmup = Core.ConVar.Find<bool>("mp_do_warmup_period");
    doWarmup?.SetInternal(true);

    var warmupTime = Core.ConVar.Find<int>("mp_warmuptime");
    warmupTime?.SetInternal(999999);

    var warmupAllPlayers = Core.ConVar.Find<int>("mp_warmuptime_all_players_connected")
      ?? Core.ConVar.Find<int>("mp_warmuptime_allplayers_connected");
    warmupAllPlayers?.SetInternal(999999);

    var warmupPause = Core.ConVar.Find<bool>("mp_warmup_pausetimer");
    warmupPause?.SetInternal(true);
  }

  private void WarmupEnforceTick()
  {
    if (!WarmupEnforceActive)
    {
      return;
    }

    if (EditingPlayers.Count == 0)
    {
      WarmupEnforceActive = false;
      UnregisterTickHandlerIfNotNeeded();
      return;
    }

    var nowMs = Environment.TickCount64;
    if (nowMs < NextWarmupEnforceMs)
    {
      return;
    }

    NextWarmupEnforceMs = nowMs + 1000;

    try
    {
      ApplyInfiniteWarmupSettings();
    }
    catch (Exception ex)
    {
      Core.Logger.LogError(ex, "[Spawns] Failed to enforce warmup mode.");
    }
  }

  private void EnsureInfiniteWarmup()
  {
    if (WarmupForced)
    {
      return;
    }

    WarmupForced = true;

    try
    {
      WarmupEnforceActive = true;
      NextWarmupEnforceMs = 0;

      EnsureTickHandlerRegistered();
      ApplyInfiniteWarmupSettings();

      var gameRules = Core.EntitySystem.GetGameRules();
      if (gameRules is not null && gameRules.IsValid)
      {
        Core.Logger.LogInformation("[Spawns] Warmup forced. WarmupPeriod={WarmupPeriod} WarmupEnd={WarmupEnd}", gameRules.WarmupPeriod, gameRules.WarmupPeriodEnd.Value);
      }
    }
    catch (Exception ex)
    {
      Core.Logger.LogError(ex, "[Spawns] Failed to force warmup mode.");
    }
  }
}
