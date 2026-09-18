# Asset pipeline

The dependency direction is `WoW path → AssetResolver → TextureAsset → decoder → DecodedTexture RGBA32 → TextureCache → WPF renderer`. Decoder code is independent from WPF. The resolver normalizes slash direction, duplicate separators and case, supports omitted extensions, and prefers native BLP/TGA before designer-only formats.

Scanning records disk and WoW paths without modifying source files. Live Texture objects and backdrop backgrounds resolve through the active client index. Missing files and decode failures become diagnostics. The Asset Browser decodes only the selected item and offers search, filtering, metadata, copy-WoW-path, runtime preview assignment, and reference search.

The in-memory cache key includes path, source size, and last-write timestamp; least-recently-used entries are removed when the configured memory budget is exceeded.
