# Changelog

## 0.1.3

- Allows venom and hearing injuries to apply when the rolled effect lands exactly on the safety cap.

## 0.1.2

- Fixes dislocation safety checks so skipped injuries do not mutate limbs first.
- Chooses from safe candidate limbs for limb-based injuries instead of skipping after one bad random limb.
- Copies the limb list before filtering candidate limbs.

## 0.1.1

- Adds a hard dependency on `Scav.WorldSettingsHelper` for correct BepInEx load order.
- Restores hooked curse timer updates used by the game runtime.
- Skips additive injuries when the rolled effect would exceed safety limits.
- Adjusts hunger and thirst injury target ranges.

## 0.1.0

- Initial release build.
- Adds automatic random injuries through existing world settings.
- Adds manual console injury commands via `ri`.
- Adds delayed warning dialogue and temporary doom moodle before delayed injuries.
- Supports `Min`, `Default`, and `Max` severity through `WorldSettingsHelper`.
