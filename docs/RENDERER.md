# Renderer

The WPF renderer walks the internal hierarchy and computes positions from WoW anchor points, parent/relative frame geometry, UI scale, and TBC's upward-positive Y offset convention. It draws custom borders/text instead of Windows Button controls. Selection, dragging, and a resize handle feed safe source patches.

Texture files and nine-slice backdrop edges are not decoded yet; current fills are deliberate placeholders documented as partial behavior.
