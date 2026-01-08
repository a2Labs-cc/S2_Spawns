using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Plugins;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Spawns;

[PluginMetadata(Id = "Spawns", Version = "1.0.0", Name = "Spawns", Author = "aga", Description = "No description.")]
public partial class Spawns : BasePlugin
{
  private static readonly object SpawnFileLock = new();
  private Guid ChatHookGuid = Guid.Empty;
  private Guid WarmupEndHookGuid = Guid.Empty;

  private readonly HashSet<int> EditingPlayers = new();
  private readonly Dictionary<int, string?> EditTeamBySlot = new();
  private bool SpawnsAreVisible;
  private bool WarmupForced;
  private bool WarmupEnforceActive;
  private long NextWarmupEnforceMs;
  private string? LoadedMapName;
  private MapSpawnFile? LoadedSpawnFile;

  private readonly Dictionary<nint, int> PlayerIdByMovementServicesAddress = new();

  private readonly List<uint> BeamEntityIndices = new();
  private readonly Dictionary<int, List<uint>> TextEntityIndicesByViewer = new();
  private readonly Dictionary<uint, Vector> TextPositions = new();
  private readonly HashSet<int> ActiveViewers = new();

  private bool TickHandlerRegistered;

  private static readonly JsonSerializerOptions JsonOptions = new()
  {
    WriteIndented = true,
    PropertyNameCaseInsensitive = true
  };

  public Spawns(ISwiftlyCore core) : base(core)
  {
  }

  public override void ConfigureSharedInterface(IInterfaceManager interfaceManager)
  {
  }

  public override void UseSharedInterface(IInterfaceManager interfaceManager)
  {
  }

  public override void Load(bool hotReload)
  {
    Core.Logger.LogInformation("[Spawns] Loaded (hotReload={HotReload}). PluginDataDirectory={PluginDataDirectory}", hotReload, Core.PluginDataDirectory);

    ChatHookGuid = Core.Command.HookClientChat(OnClientChat);
    Core.Event.OnMovementServicesRunCommandHook += OnMovementServicesRunCommandHook;
  }

  public override void Unload()
  {
    HideVisualizedSpawns();

    if (ChatHookGuid != Guid.Empty)
    {
      Core.Command.UnhookClientChat(ChatHookGuid);
      ChatHookGuid = Guid.Empty;
    }

    Core.Event.OnMovementServicesRunCommandHook -= OnMovementServicesRunCommandHook;

    EditingPlayers.Clear();
    EditTeamBySlot.Clear();
    LoadedMapName = null;
    LoadedSpawnFile = null;
    PlayerIdByMovementServicesAddress.Clear();
    SpawnsAreVisible = false;
    WarmupForced = false;
    WarmupEnforceActive = false;
    NextWarmupEnforceMs = 0;
  }
}
