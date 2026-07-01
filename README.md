# Scav.ExpiesCurse

Scav.ExpiesCurse is a Casualties Unknown / Scav Prototype challenge mod that periodically inflicts a random dangerous but usually non-instant-lethal injury on the player.

The player character receives a short warning before the injury lands: dialogue, a temporary "Sense of impending doom" moodle, then the selected injury after a short delay.

The mod also keeps developer console commands for manually testing injuries.

## Requirements

- Casualties Unknown / Scav Prototype
- Windows, if using the installer

The installer downloads and installs missing runtime dependencies automatically:

- BepInEx 5.x
- ScavLib API
- Scav.WorldSettingsHelper

Manual installs require those dependencies in the game folder:

- BepInEx 5.x installed in the game root
- ScavLib API installed in `BepInEx/plugins/`
- Scav.WorldSettingsHelper installed in `BepInEx/plugins/`

For building from source:

- .NET SDK / MSBuild
- .NET Framework 4.8 Developer Pack

## Build

Set `GameDir` to your game install path when building:

```powershell
dotnet msbuild Scav.ExpiesCurse.csproj /p:Configuration=Release /p:GameDir="C:\Path\To\Casualties Unknown Demo"
```

Copy the output DLL to:

```text
<Game>/BepInEx/plugins/Scav.ExpiesCurse.dll
```

This workspace builds against `ScavLib.API.dll` installed in the game's `BepInEx/plugins` folder and the sibling `Scav.WorldSettingsHelper` project.

## World Settings

Scav.ExpiesCurse adds settings to the existing custom world settings menu:

- `Enable Expie's curse`: enables/disables automatic random injuries for the run
- `Injury Severity`: `Min`, `Default`, or `Max`; defaults to `Default`
- `Delay between injuries`: interval from `1` to `30` in-game minutes

Automatic injuries use the game's `WorldGeneration.TotalRunTime()` timer, so speedups/resting acceleration affect the curse timer the same way they affect game run time.

## Test Commands

Open the in-game developer console and run:

```text
ri list
ri random
ri bleeding_wound
ri shrapnel
ri infection
ri fracture
ri dislocation
ri internal_bleeding
ri venom
ri radiation
ri sickness
ri hunger
ri thirst
ri happiness
ri hearing
ri brain_damage
ri status
```

Every injury also accepts an optional severity:

```text
ri bleeding_wound min
ri bleeding_wound medium
ri bleeding_wound high
ri bleeding_wound random
ri random high
```

Severity behavior:

- `min` uses the minimum value in each range.
- `medium` uses the average of the minimum and maximum.
- `high` uses the maximum value.
- `random` uses a random value in the range and is the default.

Use the game's existing health UI/debug tools to inspect the result after each command.

Delayed manual injury examples:

```text
ri random true
ri bleeding_wound high delay=true
```

`ri status` prints loaded world settings and curse timer state.

## Notes

- `ri random` picks one injury from the same list.
- The injury values are intentionally dangerous but not designed as instant-kill values.
- Fracture/dislocation use the game's own limb injury methods.

## Release Package

A release should provide:

```text
Scav.ExpiesCurse.dll
install-expies-curse.cmd
```

Dependencies are not bundled in the release. The installer downloads them from their public release URLs when they are missing.

For non-technical users, just double-click:

```text
install-expies-curse.cmd
```

The installer auto-detects common Steam install paths, installs BepInEx 5 x64 if missing, creates `BepInEx/plugins`, and installs `Scav.ExpiesCurse.dll`, `Scav.WorldSettingsHelper.dll`, and `ScavLib.API.dll`. It only asks for input if it cannot find the game folder automatically.
