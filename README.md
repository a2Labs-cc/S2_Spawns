<div align="center">

# [SwiftlyS2] Spawns

[![GitHub Release](https://img.shields.io/github/v/release/a2Labs-cc/S2_Spawns?color=FFFFFF&style=flat-square)](https://github.com/a2Labs-cc/S2_Spawns/releases/latest)
[![GitHub Issues](https://img.shields.io/github/issues/a2Labs-cc/S2_Spawns?color=FF0000&style=flat-square)](https://github.com/a2Labs-cc/S2_Spawns/issues)
[![GitHub Downloads](https://img.shields.io/github/downloads/a2Labs-cc/S2_Spawns/total?color=blue&style=flat-square)](https://github.com/a2Labs-cc/S2_Spawns/releases)
[![GitHub Stars](https://img.shields.io/github/stars/a2Labs-cc/S2_Spawns?style=social)](https://github.com/a2Labs-cc/S2_Spawns/stargazers)<br/>
  <sub>Made by <a href="https://github.com/agasking1337" rel="noopener noreferrer" target="_blank">aga</a></sub>
  <br/>

</div>

## Overview

This plugin helps you to create spawn points for the Deathmatch game mode and saves the spawns possition in a json format `game\csgo\addons\swiftlys2\data\Spawns\mapname.json` other than that the plugin is useless.


## Commands

| Command | Usage | Description |
|---|---|---|
| `!editspawns` | `[ct\|t]` | Enable spawn editing for the current map. If you pass `ct` or `t`, newly added spawns (via `!addspawn` without args or the hotkey) will be saved with that team. If you run `!editspawns` without args, new spawns will be saved without a `team` field in the JSON. |
| `!addspawn` | `[t\|ct\|dm\|any]` | Add a spawn at your current position and view angle. If you omit the argument while editing, the plugin uses your `!editspawns` team selection (or no team if none was selected). Spawns are saved to file immediately. |
| `!remove` | `<id\|all>` | Remove a spawn by its ID (as shown by visualization), or remove all spawns. Changes are saved to file immediately. |
| `!spawns` | `on\|off` | Toggle spawn visualization on/off. |

### Editing hotkeys

| Key | Action |
|---|---|
| `F` | Add spawn (same as `!addspawn` with no args). |
| `R` | Remove the spawn you are aiming at. |

## Download Shortcuts
<ul>
  <li>
    <code>📦</code>
    <strong>&nbspDownload Latest Plugin Version</strong> ⇢
    <a href="https://github.com/a2Labs-cc/S2_Spawns/releases/latest" target="_blank" rel="noopener noreferrer">Click Here</a>
  </li>
  <li>
    <code>⚙️</code>
    <strong>&nbspDownload Latest SwiftlyS2 Version</strong> ⇢
    <a href="https://github.com/swiftly-solution/swiftlys2/releases/latest" target="_blank" rel="noopener noreferrer">Click Here</a>
  </li>
</ul>

## Installation

1. Download/build the plugin.
2. Copy the published plugin folder to your server:

```
.../game/csgo/addons/swiftlys2/plugins/Retakes/
```

3. Ensure the plugin has its `resources/` folder alongside the DLL (maps, translations, gamedata).
4. Start/restart the server.
## Building

```bash
dotnet build
```

## Credits
- Readme template by [criskkky](https://github.com/criskkky)