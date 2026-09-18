# Template registry

Scanned virtual and concrete XML objects form the template registry. Each definition preserves its raw inheritance list and a cycle-safe resolved ancestor order, environment, defining source, geometry, anchors, regions, scripts, and child names. Multiple inherited template names separated by spaces or commas are supported.

Instantiation expands `$parent` using the concrete preview name and applies inherited definitions before the child definition. This is an initial property merger; richer XML elements will be added based on observed client usage.
