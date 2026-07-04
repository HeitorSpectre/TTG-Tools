using System;

namespace TTG_Tools.Graphics.Swizzles
{
    // Nintendo Wii U (GX2 / "Cafe" GPU) texture swizzle.
    //
    // The tiling is AMD's R600-family "AddrLib". This is a C# port of the public
    // reference implementation by AddrLib/Exzap/AboodXD (WiiU-swizzling-code /
    // GTX-Extractor). It was validated to be byte-for-byte identical to that
    // reference on every sample of Minecraft: Story Mode (the only Telltale Wii U
    // game): 558 .d3dtx textures (all mip levels, formats BC1/BC3/BC4/ARGB8) and
    // the glyph atlases embedded in .font files. In the D3DTX header these textures
    // are identified by platform id 13.
    //
    // Telltale strips the GX2 surface header, so the tile mode and the padded pitch
    // are derived from the surface dimensions, matching AddrLib's getDefaultGX2TileMode /
    // computeSurfaceMipLevelTileMode / getSurfaceInfo:
    //   - Base tile mode (from the mip-0 pixel size): macro (addr mode 4) when
    //     width >= 32 OR height >= 16, otherwise micro (addr mode 2). When the base is
    //     micro every mip level is micro (e.g. a 4x4 texture -> a single 8x8 micro tile).
    //   - When the base is macro, mip level 0 stays macro and deeper levels degrade to
    //     micro when nextPow2(blockWidth) < 32 or nextPow2(blockHeight) < 16 (the
    //     widthAlignFactor is always 1 for the formats Telltale uses, bpp >= 32).
    //   - Padded surface size: macro = align(bw,32) * align(bh,16) * bytesPerElement,
    //     micro = align(bw,8) * align(bh,8) * bytesPerElement.
    public static class WiiU
    {
        // D3DTX tex.platform.platform value for Wii U textures/fonts.
        public const uint PlatformId = 13;

        private const int m_banks = 4;
        private const int m_banksBitcount = 2;
        private const int m_pipes = 2;
        private const int m_pipesBitcount = 1;
        private const int m_pipeInterleaveBytesBitcount = 8;
        private const int MicroTilePixels = 64;

        public static bool IsSupportedFormat(uint texFormat)
        {
            return texFormat == 0x00 || texFormat == 0x40 || texFormat == 0x42 || texFormat == 0x43;
        }

        // Deswizzle one mip: 'data' is the padded, tiled block as stored in the file;
        // the return value is the linear, cropped block (blockWidth * blockHeight * bytesPerElement).
        // baseWidth/baseHeight are the mip-0 dimensions of the whole texture (needed to pick the
        // base tile mode); the size of this particular mip is derived from them and mipLevel.
        public static byte[] Deswizzle(byte[] data, int baseWidth, int baseHeight, uint texFormat, int mipLevel)
        {
            return Process(data, baseWidth, baseHeight, texFormat, mipLevel, true);
        }

        // Swizzle one mip (inverse of Deswizzle): 'data' is a linear block, the return value
        // is the padded, tiled block ready to be written back into a Wii U D3DTX/font.
        public static byte[] Swizzle(byte[] data, int baseWidth, int baseHeight, uint texFormat, int mipLevel)
        {
            return Process(data, baseWidth, baseHeight, texFormat, mipLevel, false);
        }

        // GX2 stores the uncompressed 32-bit format (Telltale format 0x00) as R8 G8 B8 A8,
        // but the tool's DDS/PNG pipeline for that format expects B8 G8 R8 A8 (DirectX
        // A8R8G8B8 byte order). Without this the red Mojang logo comes out blue, etc.
        // Swapping R and B is its own inverse, so the same call fixes extraction and reinsertion.
        // No-op for the block-compressed formats (BC1/BC3/BC4), whose channel order is intrinsic.
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

        // Padded, tiled size of a mip as it is stored on disc.
        public static int GetSwizzledMipSize(int baseWidth, int baseHeight, uint texFormat, int mipLevel)
        {
            int mipW = Math.Max(1, baseWidth >> mipLevel);
            int mipH = Math.Max(1, baseHeight >> mipLevel);

            int bytesPerElement, blockW, blockH;
            if (!GetFormatInfo(texFormat, mipW, mipH, out bytesPerElement, out blockW, out blockH)) return 0;

            bool macro = IsMacroTiled(mipLevel, blockW, blockH, baseWidth, baseHeight);
            int pitch = macro ? Align(blockW, 32) : Align(blockW, 8);
            int paddedH = macro ? Align(blockH, 16) : Align(blockH, 8);
            return pitch * paddedH * bytesPerElement;
        }

        private static byte[] Process(byte[] data, int baseWidth, int baseHeight, uint texFormat, int mipLevel, bool deswizzle)
        {
            int mipW = Math.Max(1, baseWidth >> mipLevel);
            int mipH = Math.Max(1, baseHeight >> mipLevel);

            int bytesPerElement, blockW, blockH;
            if (!GetFormatInfo(texFormat, mipW, mipH, out bytesPerElement, out blockW, out blockH)) return data;

            int bpp = bytesPerElement * 8;
            bool macro = IsMacroTiled(mipLevel, blockW, blockH, baseWidth, baseHeight);
            int tileMode = macro ? 4 : 2;
            int pitch = macro ? Align(blockW, 32) : Align(blockW, 8);
            int paddedH = macro ? Align(blockH, 16) : Align(blockH, 8);

            int linearSize = blockW * blockH * bytesPerElement;
            int swizzledSize = pitch * paddedH * bytesPerElement;

            byte[] result = new byte[deswizzle ? linearSize : swizzledSize];

            for (int y = 0; y < blockH; y++)
            {
                for (int x = 0; x < blockW; x++)
                {
                    long tiled = macro
                        ? AddrMacroTiled(x, y, bpp, pitch, tileMode)
                        : AddrMicroTiled(x, y, bpp, pitch, tileMode);
                    int linear = (y * blockW + x) * bytesPerElement;

                    if (deswizzle)
                    {
                        if (tiled + bytesPerElement <= data.Length && linear + bytesPerElement <= result.Length)
                            Array.Copy(data, tiled, result, linear, bytesPerElement);
                    }
                    else
                    {
                        if (linear + bytesPerElement <= data.Length && tiled + bytesPerElement <= result.Length)
                            Array.Copy(data, linear, result, tiled, bytesPerElement);
                    }
                }
            }

            return result;
        }

        private static bool GetFormatInfo(uint texFormat, int width, int height, out int bytesPerElement, out int blockW, out int blockH)
        {
            switch (texFormat)
            {
                case 0x40: // BC1 (DXT1)
                case 0x43: // BC4
                    bytesPerElement = 8; blockW = (width + 3) / 4; blockH = (height + 3) / 4; return true;
                case 0x42: // BC3 (DXT5)
                    bytesPerElement = 16; blockW = (width + 3) / 4; blockH = (height + 3) / 4; return true;
                case 0x00: // ARGB8
                    bytesPerElement = 4; blockW = width; blockH = height; return true;
                default:
                    bytesPerElement = 0; blockW = 0; blockH = 0; return false;
            }
        }

        // Matches AddrLib getDefaultGX2TileMode (base) + computeSurfaceMipLevelTileMode (per level)
        // for the formats Telltale uses. baseWidth/baseHeight are mip-0 pixel dimensions.
        private static bool IsMacroTiled(int mipLevel, int blockW, int blockH, int baseWidth, int baseHeight)
        {
            // Base tile mode: a texture small enough in both axes is micro tiled throughout.
            bool baseMacro = baseWidth >= 32 || baseHeight >= 16;
            if (!baseMacro) return false;
            if (mipLevel <= 0) return true;
            return NextPow2(blockW) >= 32 && NextPow2(blockH) >= 16;
        }

        private static int Align(int value, int align)
        {
            return ((value + align - 1) / align) * align;
        }

        private static int NextPow2(int v)
        {
            int p = 1;
            while (p < v) p <<= 1;
            return p;
        }

        private static int ComputeSurfaceThickness(int tileMode)
        {
            if (tileMode == 3 || tileMode == 7 || tileMode == 11 || tileMode == 13 || tileMode == 15) return 4;
            if (tileMode == 16 || tileMode == 17) return 8;
            return 1;
        }

        private static int ComputePixelIndexWithinMicroTile(int x, int y, int bpp)
        {
            int b0, b1, b2, b3, b4, b5;

            if (bpp == 0x08)
            {
                b0 = x & 1; b1 = (x & 2) >> 1; b2 = (x & 4) >> 2; b3 = (y & 2) >> 1; b4 = y & 1; b5 = (y & 4) >> 2;
            }
            else if (bpp == 0x10)
            {
                b0 = x & 1; b1 = (x & 2) >> 1; b2 = (x & 4) >> 2; b3 = y & 1; b4 = (y & 2) >> 1; b5 = (y & 4) >> 2;
            }
            else if (bpp == 0x20 || bpp == 0x60)
            {
                b0 = x & 1; b1 = (x & 2) >> 1; b2 = y & 1; b3 = (x & 4) >> 2; b4 = (y & 2) >> 1; b5 = (y & 4) >> 2;
            }
            else if (bpp == 0x40)
            {
                b0 = x & 1; b1 = y & 1; b2 = (x & 2) >> 1; b3 = (x & 4) >> 2; b4 = (y & 2) >> 1; b5 = (y & 4) >> 2;
            }
            else if (bpp == 0x80)
            {
                b0 = y & 1; b1 = x & 1; b2 = (x & 2) >> 1; b3 = (x & 4) >> 2; b4 = (y & 2) >> 1; b5 = (y & 4) >> 2;
            }
            else
            {
                b0 = x & 1; b1 = (x & 2) >> 1; b2 = y & 1; b3 = (x & 4) >> 2; b4 = (y & 2) >> 1; b5 = (y & 4) >> 2;
            }

            return (32 * b5) | (16 * b4) | (8 * b3) | (4 * b2) | (2 * b1) | b0;
        }

        private static int ComputePipeFromCoordWoRotation(int x, int y)
        {
            return ((y >> 3) ^ (x >> 3)) & 1;
        }

        private static int ComputeBankFromCoordWoRotation(int x, int y)
        {
            int bankBit0 = ((y / (16 * m_pipes)) ^ (x >> 3)) & 1;
            return bankBit0 | 2 * (((y / (8 * m_pipes)) ^ (x >> 4)) & 1);
        }

        private static long AddrMicroTiled(int x, int y, int bpp, int pitch, int tileMode)
        {
            int microTileThickness = (tileMode == 3) ? 4 : 1;
            int microTileBytes = (MicroTilePixels * microTileThickness * bpp + 7) / 8;
            int microTilesPerRow = pitch >> 3;
            long microTileOffset = (long)microTileBytes * ((x >> 3) + (y >> 3) * microTilesPerRow);
            int pixelIndex = ComputePixelIndexWithinMicroTile(x, y, bpp);
            return (((long)bpp * pixelIndex) >> 3) + microTileOffset;
        }

        private static long AddrMacroTiled(int x, int y, int bpp, int pitch, int tileMode)
        {
            int numPipes = m_pipes;
            int numBanks = m_banks;
            int numGroupBits = m_pipeInterleaveBytesBitcount;
            int numPipeBits = m_pipesBitcount;
            int numBankBits = m_banksBitcount;
            int microTileThickness = ComputeSurfaceThickness(tileMode);

            int pixelIndex = ComputePixelIndexWithinMicroTile(x, y, bpp);
            long elemOffset = ((long)bpp * pixelIndex + 7) / 8;

            int pipe = ComputePipeFromCoordWoRotation(x, y);
            int bank = ComputeBankFromCoordWoRotation(x, y);
            int bankPipe = pipe + numPipes * bank;
            // swizzle_ is 0 for Telltale's Wii U surfaces (each texture is its own surface at offset 0).
            bankPipe %= numPipes * numBanks;
            pipe = bankPipe % numPipes;
            bank = bankPipe / numPipes;

            int macroTilePitch = 8 * m_banks;   // 32
            int macroTileHeight = 8 * m_pipes;  // 16

            int macroTilesPerRow = pitch / macroTilePitch;
            long macroTileBytes = ((long)microTileThickness * bpp * macroTileHeight * macroTilePitch + 7) / 8;
            int macroTileIndexX = x / macroTilePitch;
            int macroTileIndexY = y / macroTileHeight;
            long macroTileOffset = (long)(macroTileIndexX + macroTilesPerRow * macroTileIndexY) * macroTileBytes;

            int groupMask = (1 << numGroupBits) - 1;
            int numSwizzleBits = numBankBits + numPipeBits;
            long totalOffset = elemOffset + (macroTileOffset >> numSwizzleBits);

            long offsetHigh = (totalOffset & ~(long)groupMask) << numSwizzleBits;
            long offsetLow = groupMask & totalOffset;
            long pipeBits = (long)pipe << numGroupBits;
            long bankBits = (long)bank << (numPipeBits + numGroupBits);

            return bankBits | pipeBits | offsetLow | offsetHigh;
        }
    }
}
