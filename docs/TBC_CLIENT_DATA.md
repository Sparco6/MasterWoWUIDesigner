# TBC client data

The configured extracted `Interface` directory is read-only reference data and takes precedence over historical web documentation. Profiles persist locally under the user's application-data directory and contain a display name, target build, Interface root, optional asset root, cache root, and compatibility notes. Windows paths are never emitted into addon Lua.

`InterfaceScanner.ScanAsync` enumerates supported files on a worker task, reports progress, honors cancellation, reads only texture headers during indexing, separates `FrameXML` from `GlueXML`, and writes a cache under `Cache/TBC243`. Incremental scans reuse texture metadata when disk path, size, and last-write timestamp match.

The current client integration test verifies the local extraction without copying or modifying it. The observed extraction contains BLP2 assets, so BLP2 support is enabled alongside the requested BLP1 pipeline.
