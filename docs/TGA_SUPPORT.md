# TGA support

The built-in TGA decoder handles true-color type 2 and RLE true-color type 10 at 24 or 32 bits per pixel. It preserves alpha, vertical and horizontal origin flags, and outputs top-left RGBA32. Color-mapped and grayscale TGA variants are deliberately rejected with a diagnostic until encountered and specified.

Synthetic tests cover RGB/RGBA, uncompressed/RLE, orientation, and alpha. No proprietary TGA fixture is stored in the repository.
