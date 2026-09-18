# Global API coverage

Current versioned `Data/TBC243/globals.json` contains 10 curated entries. This is not claimed to be an exhaustive client-binary catalog.

| Classification | Count |
|---|---:|
| Stock TBC supported | 9 |
| Explicitly not stock TBC | 1 |
| Emulator implemented | 2 |
| Emulator mocked | 3 |
| Emulator missing | 5 |

Historical index import added zero API entries because the requested Global API index was unavailable. Existing source references were retained, but no availability was upgraded from page presence. Corpus-observed globals remain in the evidence/backlog layer until classified against direct client, Compat, or reliable version evidence.

Global emulation strategies are modeled independently as `VisualModel`, `MockQuery`, `SimulatedAction`, `SafeNoOp`, or `Unsupported`. Unknown calls do not automatically become no-ops.
