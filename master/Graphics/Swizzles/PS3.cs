using System;

namespace TTG_Tools.Graphics.Swizzles
{
    // Sony PlayStation 3 (RSX / "Reality Synthesizer") texture swizzle.
    //
    // On the RSX only *uncompressed* textures are stored "swizzled" (Morton /
    // Z-order interleaving of the X and Y coordinate bits), and only when both
    // dimensions are powers of two. Block-compressed textures (BC1/DXT1, BC3/DXT5)
    // are always stored linearly and must NOT be touched.
    //
    // Validated against every Minecraft: Story Mode PS3 sample (the only Telltale
    // PS3 game in this set): 1665 .d3dtx files. All 73 uncompressed textures
    // (72 ARGB8 + 1 A8) are power-of-two and Morton swizzled; deswizzling them
    // yields coherent images (e.g. the gold "ENDERCON" banner lettering), while
    // all BC1/BC3 textures - including the many non-power-of-two mesh atlases -
    // decode as clean images when read linearly. The Morton round-trip
    // (deswizzle -> swizzle) reproduced all 381 uncompressed mip levels byte for
    // byte. In the D3DTX header these textures are identified by platform id 5.
    //
    // Unlike the tiled console formats (PS4/Vita/Switch/Wii U) the RSX Morton
    // layout is size-preserving for power-of-two surfaces: the stored mip size
    // equals the linear mip size (width * height * bytesPerPixel), so no padding
    // bookkeeping is needed on reinsertion.
    public static class PS3
    {
        // D3DTX tex.platform.platform value for PS3 textures/fonts.
        public const uint PlatformId = 5;

        // Only the uncompressed formats are swizzled on the RSX. BC1/BC3 (and any
        // other block-compressed format) are stored linearly and are left alone.
        public static bool IsSupportedFormat(uint texFormat)
        {
            return texFormat == 0x00 /*ARGB8*/ || texFormat == 0x10 /*A8*/;
        }

        // The RSX stores the uncompressed 32-bit format (Telltale format 0x00) as
        // R8 G8 B8 A8, but the tool's DDS/PNG pipeline for that format expects
        // B8 G8 R8 A8 (DirectX A8R8G8B8 byte order). Without this the gold Mojang/
        // EnderCon artwork comes out with red and blue swapped. Swapping R and B is
        // its own inverse, so the same call fixes extraction and reinsertion.
        // No-op for the single-channel A8 format, which has no R/B to swap.
        public static void FixColorChannels(byte[] pixels, uint texFormat)
        {
            if (texFormat != 0x00 || pixels == null) return;

            for (int i = 0; i + 3 < pixels.Length; i += 4)
            {
                byte r = pixels[i];
                pixels[i] = pixels[i + 2];
                pixels[i + 2] = r;
            }
        }

        private static int BytesPerPixel(uint texFormat)
        {
            switch (texFormat)
            {
                case 0x00: return 4; // ARGB8
                case 0x10: return 1; // A8
                default: return 0;
            }
        }

        // Stored (on-disc) size of a mip. The RSX Morton layout is size-preserving
        // for power-of-two surfaces, so this is just the linear size; it exists so
        // the reinsertion path can mirror the other platforms' size bookkeeping.
        public static int GetSwizzledMipSize(int baseWidth, int baseHeight, uint texFormat, int mipLevel)
        {
            int bpp = BytesPerPixel(texFormat);
            if (bpp == 0) return 0;

            int w = Math.Max(1, baseWidth >> mipLevel);
            int h = Math.Max(1, baseHeight >> mipLevel);
            return w * h * bpp;
        }

        // Deswizzle one mip: 'data' is the Morton-ordered block as stored in the
        // file; the return value is the linear block. baseWidth/baseHeight are the
        // mip-0 dimensions; this mip's size is derived from them and mipLevel.
        public static byte[] Deswizzle(byte[] data, int baseWidth, int baseHeight, uint texFormat, int mipLevel)
        {
            return Process(data, baseWidth, baseHeight, texFormat, mipLevel, true);
        }

        // Swizzle one mip (inverse of Deswizzle): 'data' is a linear block, the
        // return value is the Morton-ordered block ready to be written back.
        public static byte[] Swizzle(byte[] data, int baseWidth, int baseHeight, uint texFormat, int mipLevel)
        {
            return Process(data, baseWidth, baseHeight, texFormat, mipLevel, false);
        }

        private static byte[] Process(byte[] data, int baseWidth, int baseHeight, uint texFormat, int mipLevel, bool deswizzle)
        {
            int bpp = BytesPerPixel(texFormat);
            if (bpp == 0 || data == null) return data;

            int w = Math.Max(1, baseWidth >> mipLevel);
            int h = Math.Max(1, baseHeight >> mipLevel);

            int paddedW = NextPowerOfTwo(w);
            int paddedH = NextPowerOfTwo(h);

            int linearSize = w * h * bpp;
            byte[] result = new byte[Math.Max(linearSize, bpp)];

            int maxU = IntegerLog2(paddedW);
            int maxV = IntegerLog2(paddedH);
            int texels = paddedW * paddedH;

            for (int j = 0; j < texels; j++)
            {
                // Morton order: X bits are interleaved first (validated against the
                // real files - swapping the order splits/mirrors the image).
                int u = 0;
                int v = 0;
                int origCoord = j;

                for (int k = 0; k < maxU || k < maxV; k++)
                {
                    if (k < maxU)
                    {
                        u |= (origCoord & 1) << k;
                        origCoord >>= 1;
                    }

                    if (k < maxV)
                    {
                        v |= (origCoord & 1) << k;
                        origCoord >>= 1;
                    }
                }

                if (u >= w || v >= h) continue;

                int linearPos = (v * w + u) * bpp;
                int swizzledPos = j * bpp;

                if (deswizzle)
                {
                    if (swizzledPos + bpp <= data.Length && linearPos + bpp <= result.Length)
                        Array.Copy(data, swizzledPos, result, linearPos, bpp);
                }
                else
                {
                    if (linearPos + bpp <= data.Length && swizzledPos + bpp <= result.Length)
                        Array.Copy(data, linearPos, result, swizzledPos, bpp);
                }
            }

            return result;
        }

        private static int NextPowerOfTwo(int value)
        {
            if (value <= 1) return 1;
            int power = 1;
            while (power < value) power <<= 1;
            return power;
        }

        private static int IntegerLog2(int value)
        {
            int log = 0;
            while (value > 1)
            {
                value >>= 1;
                log++;
            }
            return log;
        }
    }
}
