> **Disclaimer:**
> Most of mods I work with are some old and outdated ones that their authors haven't updated so far to match 1.0. I am not a professional modder myself, but I am good with coding and gaming. If you experience any problems with the mods I published, you can find me in <a href="https://discord.com/channels/1522110224947871817/1522118606937133136">Hexium</a> discord server by typing DMT.

# HammerHotkeys

A Valheim mod that binds keys to build pieces and stations. Press a key and the hammer is equipped and the bound piece is selected for you - the placement ghost appears immediately, without opening the build menu. Especially useful for emergency situations, when you urgently need to place a workbench to start raising ground to save yourself from wolves or fulings, or when you need to quickly place a portal, then escape back to the base.

**GitHub:** <img height="18" src="https://github.githubassets.com/favicons/favicon-dark.svg"></img> [DaiMinhTri/HammerHotkeys](https://github.com/DaiMinhTri/HammerHotkeys)

## Features

### Key = Piece Binds
Configure as many binds as you like in one setting:

```
F6=Workbench;F7=Portal;F8=Stonecutter
```

Pressing a key equips the tool that contains the piece (preferring the hammer) and selects the piece, exactly like clicking it in the build menu.

### Piece Name Matching
The piece name is matched case-insensitively against:

1. the prefab name (e.g. `Workbench`, `Portal`)
2. the localization key (e.g. `piece_chestwood`)
3. the English display name (e.g. `Chest`)

So `Chest` resolves to the wooden chest, while `Reinforced Chest` matches the iron one.

### Availability Check
By default the mod follows the vanilla rule: pieces you have not unlocked yet are refused with a message instead of being selected. This can be disabled.

### Feedback
Plays the vanilla selection sound and shows a center screen message (both optional). The build menu is left closed, but if you had it open it is closed after the selection - same as clicking a piece in the vanilla menu.

## Configuration

| Setting | Default | Description |
|---------|---------|-------------|
| Binds | `F6=Workbench;F7=Portal;F8=Stonecutter` | Key=Piece pairs separated by `;` |
| Auto Equip Hammer | `true` | Equip the tool holding the piece if it is not already held |
| Require Available | `true` | Only select pieces that are currently unlocked |
| Play Sound | `true` | Play the vanilla selection sound |
| Show Messages | `true` | Show center screen messages |
| Open Build Menu On Select | `false` | Open the build menu after selecting instead of closing it |

Config file: `BepInEx/config/DMT.HammerHotkeys.cfg`

### Useful Piece Names

| Station / piece | Name to use |
|-----------------|-------------|
| Workbench | `Workbench` |
| Forge | `Forge` |
| Stonecutter | `Stonecutter` |
| Smelter | `Smelter` |
| Charcoal Kiln | `CharcoalKiln` |
| Artisan Table | `ArtisanTable` |
| Black Forge | `BlackForge` |
| Galdr Table | `GaldrTable` |
| Portal | `Portal` |
| Wooden chest | `Chest` |
| Reinforced chest | `Reinforced Chest` |

Any other hammer piece works the same way - use its prefab name, `$piece_...` key, or English name. Unknown names are logged to `BepInEx/LogOutput.log`.

## Installation

1. Install [BepInEx](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/)
2. Extract `HammerHotkeys.dll` into `BepInEx/plugins/HammerHotkeys/`
3. Launch the game once to generate `BepInEx/config/DMT.HammerHotkeys.cfg`
4. Edit the `Binds` setting to your preferred keys

## Compatibility

- Client-side only - other players do not need the mod
- No conflicts with building mods expected: it drives the vanilla selection path (`Player.SetSelectedPiece`)
- Uses the legacy input API (`Input.GetKeyDown`), same as other maintained hotkey mods

## Building

```powershell
cd D:\ai\projects\val\mymods\HammerHotkeys
& "C:\Program Files\dotnet\dotnet.exe" build HammerHotkeys.csproj -c Release
```

Output: `bin\Release\HammerHotkeys.dll`

References come from `valheim_Data\Managed\publicized_assemblies\` and the BepInEx core of the profile in `environment.props`.

> **Note:** the publicized assembly only fixes *compile-time* visibility - Unity Mono enforces field visibility at runtime (`FieldAccessException`). Before using any game member, audit it against the **original** `assembly_valheim.dll` (e.g. with Mono.Cecil and its `IsPublic`/`IsFamily`/`IsPrivate` booleans). Prefer public APIs like `Humanoid.GetInventory()`, `Humanoid.RightItem` and `Localization.Localize()` over fields such as `m_inventory`.

## Credits

- Built by DaiMinhTri

## Buy Me a Coffee

If you enjoy this mod, consider buying me a coffee: [![Buy Me A Coffee](https://img.shields.io/badge/Buy%20Me%20A%20Coffee-daiminhtri-yellow)](https://buymeacoffee.com/daiminhtri)

## Changelog

See [CHANGELOG.md](Package/CHANGELOG.md)
