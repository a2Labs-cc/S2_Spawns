using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using System;
using System.Linq;

using HookResult = SwiftlyS2.Shared.Misc.HookResult;

namespace Spawns;

public partial class Spawns
{
  private HookResult OnClientChat(int playerId, string text, bool teamonly)
  {
    _ = teamonly;

    var player = Core.PlayerManager.GetPlayer(playerId);
    if (player is null || !player.IsValid)
    {
      return HookResult.Continue;
    }

    var trimmed = text?.Trim() ?? string.Empty;
    if (!trimmed.StartsWith('!'))
    {
      return HookResult.Continue;
    }

    var payload = trimmed[1..].Trim();
    if (payload.Length == 0)
    {
      return HookResult.Handled;
    }

    var parts = payload.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    if (parts.Length == 0)
    {
      return HookResult.Handled;
    }

    var cmd = parts[0].ToLowerInvariant();
    var args = parts.Skip(1).ToArray();

    switch (cmd)
    {
      case "editspawns":
        HandleEditSpawns(player, args);
        return HookResult.Handled;

      case "addspawn":
        HandleAddSpawn(player, args);
        return HookResult.Handled;

      case "remove":
        HandleRemove(player, args);
        return HookResult.Handled;

      case "spawns":
        HandleSpawnsToggle(player, args);
        return HookResult.Handled;

      default:
        return HookResult.Continue;
    }
  }

  private void OnMovementServicesRunCommandHook(IOnMovementServicesRunCommandHookEvent @event)
  {
    try
    {
      var fPressed = ((@event.ButtonState.ButtonChanged & GameButtonFlags.F) != 0) && ((@event.ButtonState.ButtonPressed & GameButtonFlags.F) != 0);
      var rPressed = ((@event.ButtonState.ButtonChanged & GameButtonFlags.R) != 0) && ((@event.ButtonState.ButtonPressed & GameButtonFlags.R) != 0);
      if (!fPressed && !rPressed) return;

      if (!TryGetPlayerFromMovementServices(@event.MovementServices, out var player))
      {
        return;
      }

      if (!EditingPlayers.Contains(player.Slot))
      {
        return;
      }

      if (fPressed)
      {
        HandleAddSpawn(player, Array.Empty<string>(), true);
      }

      if (rPressed)
      {
        HandleRemoveAimedSpawn(player);
      }
    }
    catch (Exception ex)
    {
      Core.Logger.LogError(ex, "[Spawns] Error handling F key hook.");
    }
  }

  private bool TryGetPlayerFromMovementServices(CCSPlayer_MovementServices movementServices, out IPlayer player)
  {
    player = null!;
    var addr = (nint)movementServices.Address;
    if (addr == nint.Zero)
    {
      return false;
    }

    if (PlayerIdByMovementServicesAddress.TryGetValue(addr, out var playerId))
    {
      var p = Core.PlayerManager.GetPlayer(playerId);
      if (p is not null && p.IsValid)
      {
        player = p;
        return true;
      }
    }

    foreach (var p in Core.PlayerManager.GetAllPlayers())
    {
      if (p is null || !p.IsValid || p.PlayerPawn is null) continue;
      var ms = p.PlayerPawn.MovementServices;
      if (ms is null || !ms.IsValid) continue;

      if ((nint)ms.Address == addr)
      {
        PlayerIdByMovementServicesAddress[addr] = p.PlayerID;
        player = p;
        return true;
      }
    }

    return false;
  }
}
