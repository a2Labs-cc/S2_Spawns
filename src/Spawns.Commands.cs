using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using System;
using System.Collections.Generic;
using System.Globalization;

using Vector = SwiftlyS2.Shared.Natives.Vector;
using QAngle = SwiftlyS2.Shared.Natives.QAngle;

namespace Spawns;

public partial class Spawns
{
  private void HandleEditSpawns(IPlayer player, string[] args)
  {
    var mapName = Core.Engine.GlobalVars.MapName.Value;
    if (string.IsNullOrWhiteSpace(mapName))
    {
      player.SendChat("[Spawns] MapName is empty.");
      return;
    }

    string? editTeam = null;
    if (args.Length >= 1)
    {
      var t = args[0].Trim().ToLowerInvariant();
      if (t == "ct" || t == "t")
      {
        editTeam = t;
      }
      else
      {
        player.SendChat("[Spawns] Usage: !editspawns [ct|t]");
        return;
      }
    }

    EnsureInfiniteWarmup();

    if (!EnsureSpawnFileLoaded(mapName, true))
    {
      player.SendChat("[Spawns] Failed to load/create spawn file.");
      return;
    }

    EditingPlayers.Add(player.Slot);
    EditTeamBySlot[player.Slot] = editTeam;
    SpawnsAreVisible = true;
    VisualizeLoadedSpawns();

    player.SendChat($"[Spawns] Editing enabled for {mapName}. Press F or use !addspawn.");
  }

  private void HandleAddSpawn(IPlayer player, string[] args, bool viaHotkey = false)
  {
    _ = viaHotkey;

    var mapName = Core.Engine.GlobalVars.MapName.Value;
    if (string.IsNullOrWhiteSpace(mapName))
    {
      player.SendChat("[Spawns] MapName is empty.");
      return;
    }

    if (!EnsureSpawnFileLoaded(mapName, true))
    {
      player.SendChat("[Spawns] Failed to load/create spawn file.");
      return;
    }

    string? team;
    if (args.Length >= 1)
    {
      team = args[0].Trim().ToLowerInvariant();
      if (team != "t" && team != "ct" && team != "dm" && team != "any")
      {
        player.SendChat("[Spawns] Usage: !addspawn [t|ct|dm|any]");
        return;
      }
    }
    else
    {
      if (EditingPlayers.Contains(player.Slot) && EditTeamBySlot.TryGetValue(player.Slot, out var selectedTeam))
      {
        team = selectedTeam;
      }
      else
      {
        team = player.RequiredPawn.Team switch
        {
          Team.T => "t",
          Team.CT => "ct",
          _ => null
        };
      }
    }

    Vector pos;
    QAngle ang;
    try
    {
      pos = player.RequiredPlayerPawn.AbsOrigin ?? Vector.Zero;
      ang = player.RequiredPlayerPawn.EyeAngles;
    }
    catch
    {
      player.SendChat("[Spawns] Player position not ready.");
      return;
    }

    var posStr = FormatVector(pos);
    var angStr = FormatQAngle(ang);

    LoadedSpawnFile ??= new MapSpawnFile();
    LoadedSpawnFile.Spawnpoints ??= [];

    var newKey = team is null ? CreatePosKey(pos) : CreateKey(team, pos);
    var existingKeys = new HashSet<string>(StringComparer.Ordinal);
    foreach (var sp in LoadedSpawnFile.Spawnpoints)
    {
      if (sp.Pos == null) continue;

      if (team is null)
      {
        var posParts = sp.Pos.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (posParts.Length != 3) continue;
        if (!TryParsePosFloat(posParts[0], out var x) || !TryParsePosFloat(posParts[1], out var y) || !TryParsePosFloat(posParts[2], out var z)) continue;
        existingKeys.Add($"{FormatKeyFloat(x)}|{FormatKeyFloat(y)}|{FormatKeyFloat(z)}");
      }
      else
      {
        if (sp.Team == null) continue;
        if (!TryCreateKey(sp.Team, sp.Pos, out var key)) continue;
        existingKeys.Add(key);
      }
    }

    if (existingKeys.Contains(newKey))
    {
      player.SendChat("[Spawns] Duplicate spawn ignored.");
      return;
    }

    LoadedSpawnFile.Spawnpoints.Add(new SpawnPoint
    {
      Team = team,
      Pos = posStr,
      Angle = angStr
    });

    if (!SaveLoadedSpawnFile(mapName))
    {
      player.SendChat("[Spawns] Added spawn, but failed to save to file.");
    }

    if (SpawnsAreVisible)
    {
      VisualizeLoadedSpawns();
    }

    player.SendChat(team is null
      ? $"[Spawns] Added spawn #{LoadedSpawnFile.Spawnpoints.Count}."
      : $"[Spawns] Added spawn #{LoadedSpawnFile.Spawnpoints.Count} ({team}).");
  }

  private void HandleRemove(IPlayer player, string[] args)
  {
    if (args.Length != 1)
    {
      player.SendChat("[Spawns] Usage: !remove <id|all>");
      return;
    }

    var mapName = Core.Engine.GlobalVars.MapName.Value;
    if (string.IsNullOrWhiteSpace(mapName))
    {
      player.SendChat("[Spawns] MapName is empty.");
      return;
    }

    if (!EnsureSpawnFileLoaded(mapName, true))
    {
      player.SendChat("[Spawns] Failed to load/create spawn file.");
      return;
    }

    var arg = args[0].Trim().ToLowerInvariant();
    if (arg == "all")
    {
      var removed = LoadedSpawnFile?.Spawnpoints?.Count ?? 0;
      LoadedSpawnFile!.Spawnpoints.Clear();

      if (!SaveLoadedSpawnFile(mapName))
      {
        player.SendChat("[Spawns] Removed spawns, but failed to save to file.");
      }

      if (SpawnsAreVisible) VisualizeLoadedSpawns();
      player.SendChat($"[Spawns] Removed {removed} spawns.");
      return;
    }

    if (!int.TryParse(arg, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) || id <= 0)
    {
      player.SendChat("[Spawns] Usage: !remove <id|all>");
      return;
    }

    if (LoadedSpawnFile is null || LoadedSpawnFile.Spawnpoints.Count == 0)
    {
      player.SendChat("[Spawns] No spawns loaded.");
      return;
    }

    var index = id - 1;
    if (index < 0 || index >= LoadedSpawnFile.Spawnpoints.Count)
    {
      player.SendChat("[Spawns] Invalid id.");
      return;
    }

    LoadedSpawnFile.Spawnpoints.RemoveAt(index);

    if (!SaveLoadedSpawnFile(mapName))
    {
      player.SendChat("[Spawns] Removed spawn, but failed to save to file.");
    }

    if (SpawnsAreVisible) VisualizeLoadedSpawns();
    player.SendChat($"[Spawns] Removed spawn #{id}.");
  }

  private void HandleRemoveAimedSpawn(IPlayer player)
  {
    var mapName = Core.Engine.GlobalVars.MapName.Value;
    if (string.IsNullOrWhiteSpace(mapName))
    {
      return;
    }

    if (!EnsureSpawnFileLoaded(mapName, true))
    {
      return;
    }

    if (LoadedSpawnFile is null || LoadedSpawnFile.Spawnpoints.Count == 0)
    {
      player.SendChat("[Spawns] No spawns to remove.");
      return;
    }

    var index = FindAimedSpawnIndex(player);
    if (index is null)
    {
      player.SendChat("[Spawns] No spawn found in crosshair.");
      return;
    }

    var removedId = index.Value + 1;
    LoadedSpawnFile.Spawnpoints.RemoveAt(index.Value);

    if (!SaveLoadedSpawnFile(mapName))
    {
      player.SendChat("[Spawns] Removed spawn, but failed to save to file.");
    }

    if (SpawnsAreVisible) VisualizeLoadedSpawns();
    player.SendChat($"[Spawns] Removed spawn #{removedId}.");
  }

  private int? FindAimedSpawnIndex(IPlayer player)
  {
    if (LoadedSpawnFile is null || LoadedSpawnFile.Spawnpoints.Count == 0)
    {
      return null;
    }

    Vector eye;
    QAngle ang;
    try
    {
      var origin = player.RequiredPlayerPawn.AbsOrigin ?? Vector.Zero;
      eye = new Vector(origin.X, origin.Y, origin.Z + 64f);
      ang = player.RequiredPlayerPawn.EyeAngles;
    }
    catch
    {
      return null;
    }

    var forward = AngleToForward(ang);
    var bestIndex = (int?)null;
    var bestDistSq = float.MaxValue;

    for (var i = 0; i < LoadedSpawnFile.Spawnpoints.Count; i++)
    {
      var sp = LoadedSpawnFile.Spawnpoints[i];
      if (sp.Pos is null) continue;
      if (!TryParseVector(sp.Pos, out var pos)) continue;

      var to = new Vector(pos.X - eye.X, pos.Y - eye.Y, pos.Z - eye.Z);
      var t = (to.X * forward.X) + (to.Y * forward.Y) + (to.Z * forward.Z);
      if (t < 0f) continue;
      if (t > 4096f) continue;

      var closest = new Vector(eye.X + forward.X * t, eye.Y + forward.Y * t, eye.Z + forward.Z * t);
      var dx = pos.X - closest.X;
      var dy = pos.Y - closest.Y;
      var dz = pos.Z - closest.Z;
      var distSq = (dx * dx) + (dy * dy) + (dz * dz);

      if (distSq > (96f * 96f)) continue;

      if (distSq < bestDistSq)
      {
        bestDistSq = distSq;
        bestIndex = i;
      }
    }

    return bestIndex;
  }

  private void HandleSpawnsToggle(IPlayer player, string[] args)
  {
    var mapName = Core.Engine.GlobalVars.MapName.Value;
    if (string.IsNullOrWhiteSpace(mapName))
    {
      player.SendChat("[Spawns] MapName is empty.");
      return;
    }

    var arg = args.Length >= 1 ? args[0].Trim().ToLowerInvariant() : string.Empty;
    if (arg != "on" && arg != "off")
    {
      player.SendChat("[Spawns] Usage: !spawns on|off");
      return;
    }

    if (arg == "off")
    {
      SpawnsAreVisible = false;
      HideVisualizedSpawns();
      player.SendChat("[Spawns] Spawns hidden.");
      return;
    }

    if (!EnsureSpawnFileLoaded(mapName, false))
    {
      player.SendChat("[Spawns] No saved spawns for this map.");
      return;
    }

    SpawnsAreVisible = true;
    VisualizeLoadedSpawns();
    player.SendChat("[Spawns] Spawns shown.");
  }
}
