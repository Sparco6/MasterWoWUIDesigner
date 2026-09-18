# BLP support

Supported decoding output is RGBA32 and source assets are never converted beside the client files.

- BLP1 paletted color with 0/1/4/8-bit alpha
- BLP1 JPEG with shared JPEG header reconstruction
- BLP2 paletted color
- BLP2 BGRA
- BLP2 DXT1, DXT3 and DXT5
- Header metadata: version, dimensions, encoding/compression, alpha depth and mip count
- Explicit mip selection with safe fallback

The configured local client primarily contains BLP2 DXT assets, and an integration test decodes a discovered local asset. Unknown encodings produce diagnostics instead of crashing the scan.
