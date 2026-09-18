# Implementation status

## TBC client import milestone

| Subsystem | Status | Notes |
|---|---|---|
| Client Scanner | Tested | Async/cancelable recursive scan; read-only source treatment |
| FrameXML Parser | Partial | Frames, templates, regions, anchors, scripts, dependencies |
| GlueXML Parser | Partial | Parsed and isolated with `Glue` environment badge |
| Template Registry | Integrated | Virtual templates, inheritance and `$parent`; now applied by live Lua `CreateFrame(..., template)` |
| BLP Decoder | Renderer integrated | BLP1 JPEG/palette and locally observed BLP2 palette/BGRA/DXT; decoded in memory by live preview |
| TGA Decoder | Renderer integrated | 24/32-bit true-color, RLE, alpha and orientation; decoded in memory by live preview |
| Asset Resolver | Renderer integrated | Live texture/backdrop paths resolve through the scanned client index |
| Asset Browser | Implemented | Lazy preview, search/filter, metadata, references and copy path |
| Texture Cache | Renderer integrated | Shared live-preview decode cache with timestamp/size key and memory-budget eviction |
| API Usage Scanner | Implemented | Method/global call counts, files, lines and context |
| Event Scanner | Implemented | RegisterEvent names, counts and sources |
| Original Blizzard UI Preview | Partial | Virtual templates and runtime template application work; full FrameXML module execution remains pending |

## Integration audit (2026-08-26)

| Existing subsystem | Audit classification | Live-path evidence / remaining gap |
|---|---|---|
| BLP decoder | Implemented and integrated | `SetTexture`/backdrop → resolver → cache → decoder → WPF brush. Advanced texture transforms remain open. |
| TGA decoder | Implemented and integrated | Same live path as BLP. No files are converted in the Interface tree. |
| AssetResolver | Implemented and integrated | `MainWindow.StartTextureLoad` resolves WoW paths from the active client index. |
| TextureCache | Implemented and integrated | Live renderer and asset browser use the existing cache implementation. |
| TemplateRegistry / XML definitions | Partially integrated | Runtime `CreateFrame` now applies scanned templates, inherited size/anchors/regions/scripts and `$parent` names. Full child-frame XML instantiation and visual fidelity remain open. |
| FrameXML parser | Partially integrated | Its templates feed runtime creation; whole-module TOC/include/script execution is not yet connected. |
| API Usage Scanner | Implemented but isolated from preview | Feeds scan reports/backlog, not runtime dispatch. |
| Event Scanner | Implemented but isolated from preview | Static registrations are indexed; live event invocation UI remains open. |

The earlier “decoder tested, real loading open” wording conflated four stages. Decoder and resolver correctness are tested, and simple runtime/renderer integration now exists. Production fidelity remains incomplete for texture coordinates, vertex tint, backdrop edges/insets, status-bar cropping, and fonts.

## Real target probes

- `MasterProgression_AIO_Client.lua`: initializes without runtime errors, registers 10 handlers, and manual `OpenWindow` invocation creates its main named frame. Nested visual fidelity remains incomplete.
- `ZContributeAIO_Client.lua`: initializes, registers 2 handlers, invokes `OpenWindow`, attaches addon tables such as `frame.header`/`frame.renderPool`, configures its ScrollFrame, and completes that handler without runtime errors.
- `ItemVault_Client.lua`: initializes without runtime errors and registers 5 handlers; its available handlers do not expose a parameter-free Open-named entry for the automatic probe.
- `RaidFrame.lua`: the real extracted FrameXML Lua module executes without runtime errors in the client runtime. Its XML-owned frames and live game-state behavior are not yet instantiated, so this is execution coverage rather than a full visual preview.

These are staged outcomes, not claims of full previewability.

The CLR userdata bridge now uses a custom descriptor layered over the existing reflected widget API. It permits arbitrary lowercase addon fields while retaining strict errors for unknown API-style members, avoiding a parallel UI model or renderer.

The configured client remains external and no Blizzard asset is included in the repository.

## Phase 0 — TBC 2.4.3 API catalog

- [x] Versioned `Data/TBC243` catalog directory
- [x] Separate widget, global, event, template and constant catalogs
- [x] Availability independent from emulator/test status
- [x] Historical evidence and per-entry classification notes
- [x] Explicit later-version negative controls
- [x] Strongly typed loader and automated schema/consistency checks
- [ ] Exhaustive binary-derived widget method dump for build 8606
- [ ] Exhaustive global/event/constant extraction from all 2.4.3 FrameXML files
- [ ] Manual resolution of entries currently marked `Uncertain`

## Historical documentation evidence

- [x] Offline metadata cache and configurable source URLs
- [x] Historical index HTML parser with source URL/confidence retention
- [x] Unified stock/custom/Compat/addon/client evidence merge model
- [x] Conflict flag for direct-client observations contradicting stock classification
- [x] Explicit emulator strategies: VisualModel, MockQuery, SimulatedAction, SafeNoOp, Unsupported
- [x] Searchable API Explorer using the versioned local catalogs
- [x] Global, widget, and documentation-source coverage reports
- [ ] Background/cancelable HTTP refresh command
- [ ] Individual API-page argument/return/version extraction
- [ ] Evidence conflict and uncertainty panes in the explorer

On 2026-08-26 the interface customization portal was reachable, while the requested Global API and Widget API index fetches returned HTTP 402. No API metadata was fabricated or imported from those unavailable pages.

## Initial vertical slice

- [x] Windows WPF solution and dark IDE shell
- [x] Open, edit, reload and save Lua
- [x] AvalonEdit syntax highlighting and line numbers
- [x] Sandboxed managed Lua runtime
- [x] CreateFrame, UIParent and dynamic loops
- [x] Internal WoW UI model separated from WPF
- [x] Frame hierarchy and selectable canvas objects
- [x] Property inspector for geometry
- [x] Drag and resize routed through safe source patches
- [x] Literal SetWidth/SetHeight/SetSize/SetPoint X/Y mapping
- [x] Runtime errors and API call trace
- [x] AIO client guard semantics, handler capture, and no-network mocks
- [x] Multi-anchor model, common SetPoint overloads, derived geometry, and live renderer layout integration
- [x] Live CreateFrame template application using the existing scanned template registry
- [x] Screenshot background, resolution profiles and UI scale
- [x] Source-level undo/redo for designer patches
- [x] Automated runtime, dynamic-loop, timeout-signature and patch-safety tests

## Implemented or partial APIs

- **Implemented model behavior:** CreateFrame; runtime template application; Set/GetWidth; Set/GetHeight; SetSize; multi-point SetPoint; GetPoint/GetNumPoints; ClearAllPoints; SetAllPoints; calculated GetLeft/GetRight/GetTop/GetBottom/GetCenter/GetRect; Show; Hide; IsShown; IsVisible; Set/GetAlpha; Set/GetText; Set/GetTexture; CreateTexture; CreateFontString; SetScript/HookScript registration; status-bar min/max/value.
- **Partial/no visual detail:** SetBackdrop; buttons/check buttons; EditBox flags; StatusBar color/texture; FontString color/justification; parent-relative anchors; textures.
- **Mocked:** AIO.AddAddon, AIO.AddHandlers, AIO.Handle, GetScreenWidth, GetScreenHeight, WorldFrame.
- **Unsupported:** remaining Phase 1 surface and all later-phase APIs. Unknown calls are reported as Lua errors rather than silently treated as TBC-compatible.

## Requirement groups still open

- [ ] Complete Phase 1 API coverage and compatibility analyzer
- [x] Instruction-yield execution timeout for initial loads, compatibility layers, and AIO handlers
- [x] External cancellation tokens for initial preview and AIO handler execution
- [x] Open, Reload, and AIO invocation run away from the WPF UI thread
- [ ] Event and frame-script invocation UI using the shared execution guard
- [ ] AST/token-based static analysis and source navigation
- [ ] Encoding/BOM/line-ending preservation and optional backups
- [x] Basic real texture/TGA/BLP runtime loading
- [ ] Accurate texture coordinates/tint, backdrops, status bars and fonts
- [ ] ScrollFrame clipping/scrolling and EditBox interaction fidelity
- [ ] Project `.masterwowui` persistence and file watching
- [ ] Configurable AIO/game-state mock data
- [ ] Complete template child-frame behavior, events, GameTooltip and remaining Phase 2
- [ ] Phase 3 broad API/data support
- [ ] Phase 4 visual generation/debug/documentation tooling

This file intentionally does not mark unimplemented UI affordances as complete; planned commands are disabled or absent.
