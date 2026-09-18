# Compatibility layer

`Compat.lua` is loaded before target code and is not treated as an addon. Global polyfills and Lua wrappers remain in the same MoonSharp global environment, so a Lua replacement of `CreateFrame` is visible to target scripts.

UI objects are CLR userdata. Their native MoonSharp prototypes are not treated as mutable WoW C-object prototypes. The runtime therefore exposes equivalent adapter methods directly and records `CLR compatibility adapter` in `WowDocument.CompatibilityLayers`. It emits a diagnostic for every compatibility load and records partial failures instead of silently claiming success.

The real workspace compatibility source is covered by an integration test. It currently loads, creates fallback FontObjects, installs global helpers such as `Clamp`, wraps `CreateFrame`, creates its probe objects, and registers `PLAYER_LOGIN` on its event frame.

Static compatibility discovery remains data-driven through `AddonCorpusScanner`; the coverage report links detected definitions to their source file and line.
