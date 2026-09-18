# AIO preview runtime

The client guard `if AIO.AddAddon() then return end` receives `false`, allowing normal client code to initialize. `AIO.AddHandlers(module, table)` captures functions assigned to the returned table. The AIO Inspector lists the module and handler and can invoke the selected handler with an empty payload; invocation errors are reported as diagnostics.

No network traffic or server execution occurs. Persistent named payloads, JSON/Lua conversion, and generated payload skeletons remain backlog items.
