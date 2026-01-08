using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using System;
using System.Collections.Generic;
using System.Linq;

using Vector = SwiftlyS2.Shared.Natives.Vector;
using QAngle = SwiftlyS2.Shared.Natives.QAngle;
using Color = SwiftlyS2.Shared.Natives.Color;

namespace Spawns;

public partial class Spawns
{
  private void EnsureTickHandlerRegistered()
  {
    if (TickHandlerRegistered) return;
    Core.Event.OnTick += OnTick;
    TickHandlerRegistered = true;
  }

  private void UnregisterTickHandlerIfNotNeeded()
  {
    if (!TickHandlerRegistered) return;
    if (ActiveViewers.Count != 0) return;
    if (WarmupEnforceActive) return;

    Core.Event.OnTick -= OnTick;
    TickHandlerRegistered = false;
  }

  private void OnTick()
  {
    WarmupEnforceTick();

    foreach (var viewerSlot in ActiveViewers)
    {
      var player = Core.PlayerManager.GetPlayer(viewerSlot);
      if (player is null || !player.IsValid || player.PlayerPawn is null) continue;

      var playerPos = player.PlayerPawn.AbsOrigin;
      if (playerPos is null) continue;

      if (!TextEntityIndicesByViewer.TryGetValue(viewerSlot, out var textIndices)) continue;

      foreach (var index in textIndices)
      {
        var text = Core.EntitySystem.GetEntityByIndex<CPointWorldText>(index);
        if (text is null || !text.IsValid) continue;

        if (!TextPositions.TryGetValue(index, out var textPos)) continue;

        var dx = playerPos.Value.X - textPos.X;
        var dy = playerPos.Value.Y - textPos.Y;
        var yaw = MathF.Atan2(dy, dx) * (180f / MathF.PI) + 90f;

        var newAngles = new QAngle(0f, yaw, 90f);
        text.Teleport(textPos, newAngles, Vector.Zero);
      }
    }
  }

  private void EnsureViewerInitialized(IPlayer viewer)
  {
    if (!TextEntityIndicesByViewer.ContainsKey(viewer.Slot))
    {
      TextEntityIndicesByViewer[viewer.Slot] = new List<uint>();
    }

    ActiveViewers.Add(viewer.Slot);
    EnsureTickHandlerRegistered();
  }

  private void HideVisualizedSpawns()
  {
    foreach (var idx in BeamEntityIndices)
    {
      var beam = Core.EntitySystem.GetEntityByIndex<CBeam>(idx);
      if (beam is not null && beam.IsValid)
      {
        beam.Despawn();
      }
    }

    foreach (var kvp in TextEntityIndicesByViewer)
    {
      foreach (var idx in kvp.Value)
      {
        var text = Core.EntitySystem.GetEntityByIndex<CPointWorldText>(idx);
        if (text is not null && text.IsValid)
        {
          text.Despawn();
        }

        TextPositions.Remove(idx);
      }
      kvp.Value.Clear();
    }

    BeamEntityIndices.Clear();
    TextEntityIndicesByViewer.Clear();
    TextPositions.Clear();
    ActiveViewers.Clear();
    UnregisterTickHandlerIfNotNeeded();
  }

  private void CreateBeam(Vector start, Color color)
  {
    try
    {
      var beam = Core.EntitySystem.CreateEntityByDesignerName<CBeam>("beam");
      if (beam is null)
      {
        return;
      }

      beam.StartFrame = 0;
      beam.FrameRate = 0;
      beam.LifeState = 1;
      beam.Width = 5.0f;
      beam.EndWidth = 5.0f;
      beam.Amplitude = 0;
      beam.Speed = 50;
      beam.BeamFlags = 0;
      beam.BeamType = BeamType_t.BEAM_HOSE;
      beam.FadeLength = 10.0f;
      beam.Render = color;
      beam.TurnedOff = false;

      beam.EndPos.X = start.X;
      beam.EndPos.Y = start.Y;
      beam.EndPos.Z = start.Z + 100.0f;

      beam.Teleport(start, new QAngle(0, 0, 0), Vector.Zero);
      beam.DispatchSpawn();

      beam.LifeStateUpdated();
      beam.StartFrameUpdated();
      beam.FrameRateUpdated();
      beam.WidthUpdated();
      beam.EndWidthUpdated();
      beam.AmplitudeUpdated();
      beam.SpeedUpdated();
      beam.BeamFlagsUpdated();
      beam.BeamTypeUpdated();
      beam.FadeLengthUpdated();
      beam.TurnedOffUpdated();
      beam.EndPosUpdated();
      beam.RenderUpdated();

      BeamEntityIndices.Add(beam.Index);
    }
    catch (Exception ex)
    {
      Core.Logger.LogError(ex, "[Spawns] Failed to create beam.");
    }
  }

  private void CreateLabelForViewer(IPlayer viewer, Vector pos, string label, List<IPlayer> allViewers)
  {
    try
    {
      var text = Core.EntitySystem.CreateEntityByDesignerName<CPointWorldText>("point_worldtext");
      if (text is null)
      {
        return;
      }

      text.DispatchSpawn();

      text.MessageText = label;
      text.Enabled = true;
      text.Color = new Color(255, 255, 255, 255);
      text.FontSize = 48f;
      text.Fullbright = true;
      text.WorldUnitsPerPx = 0.1f;
      text.JustifyHorizontal = PointWorldTextJustifyHorizontal_t.POINT_WORLD_TEXT_JUSTIFY_HORIZONTAL_CENTER;
      text.JustifyVertical = PointWorldTextJustifyVertical_t.POINT_WORLD_TEXT_JUSTIFY_VERTICAL_CENTER;

      var textPos = new Vector(pos.X, pos.Y, pos.Z + 50f);
      var viewerPos = viewer.PlayerPawn?.AbsOrigin;
      var yaw = 0f;
      if (viewerPos is not null)
      {
        var dx = viewerPos.Value.X - textPos.X;
        var dy = viewerPos.Value.Y - textPos.Y;
        yaw = MathF.Atan2(dy, dx) * (180f / MathF.PI) + 90f;
      }

      var angles = new QAngle(0f, yaw, 90f);
      text.Teleport(textPos, angles, Vector.Zero);

      if (TextEntityIndicesByViewer.TryGetValue(viewer.Slot, out var list))
      {
        list.Add(text.Index);
      }

      TextPositions[text.Index] = textPos;

      foreach (var other in allViewers)
      {
        if (other.Slot == viewer.Slot) continue;
        other.ShouldBlockTransmitEntity((int)text.Index, true);
      }
    }
    catch (Exception ex)
    {
      Core.Logger.LogError(ex, "[Spawns] Failed to create label.");
    }
  }

  private void VisualizeLoadedSpawns()
  {
    HideVisualizedSpawns();

    if (LoadedSpawnFile is null || LoadedSpawnFile.Spawnpoints.Count == 0)
    {
      return;
    }

    var spawnPoints = new List<(string Team, Vector Pos)>(LoadedSpawnFile.Spawnpoints.Count);
    foreach (var sp in LoadedSpawnFile.Spawnpoints)
    {
      if (sp.Pos is null) continue;
      if (!TryParseVector(sp.Pos, out var pos)) continue;
      spawnPoints.Add((sp.Team ?? "any", pos));
    }

    for (var i = 0; i < spawnPoints.Count; i++)
    {
      var sp = spawnPoints[i];
      CreateBeam(sp.Pos, GetTeamColor(sp.Team));
    }

    var viewers = Core.PlayerManager.GetAllPlayers().Where(p => p.IsValid).ToList();
    foreach (var viewer in viewers)
    {
      EnsureViewerInitialized(viewer);
      for (var i = 0; i < spawnPoints.Count; i++)
      {
        var sp = spawnPoints[i];
        var label = $"[{NormalizeTeamLabel(sp.Team)}] SPAWN | ID {i + 1}";
        CreateLabelForViewer(viewer, sp.Pos, label, viewers);
      }
    }
  }
}
