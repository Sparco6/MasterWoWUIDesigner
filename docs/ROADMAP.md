# Roadmap

1. Harden runtime execution with an instruction budget/cancellable worker, leniency modes, and broader Phase 1 APIs.
2. Replace regex mapping with a token-preserving Lua parser; preserve encoding, BOM and line endings; add backups.
3. Add texture asset resolution, TGA/BLP decoding/cache, accurate backdrops, fonts, clipping and ScrollFrame behavior.
4. Add templates, event simulation, GameTooltip, configurable AIO/game mocks, multi-file projects, API analyzer and project persistence.
5. Add component palette, new-UI generation, debug overlays, resolution tests and broader TBC FrameXML coverage.

## Client-data stages

- Stage A first vertical slice: implemented and tested (scanner, index/cache, resolver, BLP/TGA, asset browser, live texture/backdrop resolution).
- Stage B: partial (virtual templates, inheritance, `$parent`, dependencies and preview work; complete XML coverage remains).
- Stage C: partial (API/event/global usage statistics and report work; registry merge and full coverage UI remain).
- Stage D: pending (complete original Blizzard UI loading, dependency execution, Interface Explorer and accuracy dashboards).
