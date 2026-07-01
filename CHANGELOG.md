# Changelog

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
