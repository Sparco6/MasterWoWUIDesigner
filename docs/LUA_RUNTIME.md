# Lua runtime

MoonSharp 2.0 supplies a managed, Lua 5.1-oriented runtime. `CoreModules.Preset_SoftSandbox` excludes host IO and OS access. The host registers UIParent, WorldFrame, screen-size queries, CreateFrame, and a minimal AIO table. Calls mutate `WowUiObject` instances and are traced.

Lua errors become diagnostics so the editor remains alive. Unknown methods currently produce a normal Lua error; configurable strict/compatible/lenient dispatch and an instruction-budget worker boundary are next.
# Runtime execution safety

Lua is not rejected based on source text. In particular, `while true do` is valid and is allowed to execute. MoonSharp chunks and handlers run as coroutines with `AutoYieldCounter` instruction boundaries. At every yield the runtime checks elapsed time and external cancellation.

The shared guard currently covers `InitialLoad`, `CompatLayer`, and `AioHandler`. Its context model also reserves `EventHandler`, `FrameScript`, and `FrameXmlScript` for the corresponding invocation paths.

Timeout diagnostics include execution context, elapsed time, configured limit, and source file or handler identity. Open, Reload, and AIO Inspector invocation use background tasks so the WPF dispatcher remains responsive. Unknown APIs remain strict errors; the timeout mechanism does not convert them to no-ops.
