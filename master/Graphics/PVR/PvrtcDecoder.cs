using System;

namespace TTG_Tools.Graphics.PVR
{
    // Self-contained PVRTC (PowerVR Texture Compression) decompressor for 2bpp and 4bpp
    // modes. Faithful port of Imagination Technologies' public reference implementation
    // (PowerVR SDK, PVRTDecompress.cpp). Used to turn PS Vita (platform 9) PVRTC textures
    // into editable RGBA so the tool can export them as PNG.
    //
    // Telltale/T3 surface formats handled here (see TelltaleToolLib eSurface_* enum):
    //   0x50 eSurface_PVRTC2  (2bpp, RGB)
    //   0x51 eSurface_PVRTC4  (4bpp, RGB)
    //   0x52 eSurface_PVRTC2a (2bpp, RGBA)
    //   0x53 eSurface_PVRTC4a (4bpp, RGBA)
    public static class PvrtcDecoder
    {
        public static bool IsPvrtcFormat(uint format)
        {
            return format == 0x50 || format == 0x51 || format == 0x52 || format == 0x53;
        }

        public static bool Is2Bpp(uint format)
        {
            return format == 0x50 || format == 0x52;
        }

        // True for the RGB-only formats, whose alpha channel is undefined and should be
        // forced opaque when producing a PNG.
        public static bool IsOpaque(uint format)
        {
            return format == 0x50 || format == 0x51;
        }

        // Decodes a single mip level's PVRTC block stream into 32-bit RGBA (R,G,B,A order).
        // Returns width*height*4 bytes, or null on failure. Textures smaller than the
        // minimum PVRTC size (8x8 for 4bpp, 16x8 for 2bpp) are decoded padded then cropped.
        public static byte[] Decode(byte[] compressedData, int width, int height, bool do2bppMode, bool forceOpaque)
        {
            if (compressedData == null || width <= 0 || height <= 0) return null;

            try
            {
                int wordWidth = do2bppMode ? 8 : 4;
                int wordHeight = 4;

                int paddedWidth = Math.Max(width, wordWidth * 2);
                int paddedHeight = Math.Max(height, wordHeight * 2);

                int needed = (paddedWidth / wordWidth) * (paddedHeight / wordHeight) * 8;
                byte[] data = compressedData;
                if (data.Length < needed)
                {
                    // Textures smaller than the minimum PVRTC word grid store fewer words than the
                    // 2x2 minimum. Tile (repeat) the existing words instead of zero-padding: a solid
                    // colour stored as a single word must fill every neighbour, otherwise PVRTC's
                    // inter-word colour interpolation blends the real colour toward black.
                    data = new byte[needed];
                    if (compressedData.Length > 0)
                    {
                        for (int off = 0; off < needed; off += compressedData.Length)
                        {
                            int n = Math.Min(compressedData.Length, needed - off);
                            Array.Copy(compressedData, 0, data, off, n);
                        }
                    }
                }

                int[] decoded = PvrtcDecompress(data, paddedWidth, paddedHeight, do2bppMode);

                byte[] rgba = new byte[width * height * 4];
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int px = decoded[y * paddedWidth + x];
                        int di = (y * width + x) * 4;
                        rgba[di] = (byte)(px & 0xFF);           // R
                        rgba[di + 1] = (byte)((px >> 8) & 0xFF);  // G
                        rgba[di + 2] = (byte)((px >> 16) & 0xFF); // B
                        rgba[di + 3] = forceOpaque ? (byte)0xFF : (byte)((px >> 24) & 0xFF); // A
                    }
                }

                return rgba;
            }
            catch
            {
                return null;
            }
        }

        // ---- Pixel packing: an int holds R,G,B,A in bytes 0..3 (little end = R) ----
        private static int PackRGBA(int r, int g, int b, int a)
        {
            return (r & 0xFF) | ((g & 0xFF) << 8) | ((b & 0xFF) << 16) | ((a & 0xFF) << 24);
        }

        private static void GetColorA(uint colorData, int[] c)
        {
            if ((colorData & 0x8000u) != 0)
            {
                // Opaque Color Mode - RGB 554
                c[0] = (int)((colorData & 0x7c00u) >> 10);
                c[1] = (int)((colorData & 0x3e0u) >> 5);
                c[2] = (int)((colorData & 0x1eu) | ((colorData & 0x1eu) >> 4));
                c[3] = 0xf;
            }
            else
            {
                // Transparent Color Mode - ARGB 3443
                c[0] = (int)(((colorData & 0xf00u) >> 7) | ((colorData & 0xf00u) >> 11));
                c[1] = (int)(((colorData & 0xf0u) >> 3) | ((colorData & 0xf0u) >> 7));
                c[2] = (int)(((colorData & 0xeu) << 1) | ((colorData & 0xeu) >> 2));
                c[3] = (int)((colorData & 0x7000u) >> 11);
            }
        }

        private static void GetColorB(uint colorData, int[] c)
        {
            if ((colorData & 0x80000000u) != 0)
            {
                // Opaque Color Mode - RGB 555
                c[0] = (int)((colorData & 0x7c000000u) >> 26);
                c[1] = (int)((colorData & 0x3e00000u) >> 21);
                c[2] = (int)((colorData & 0x1f0000u) >> 16);
                c[3] = 0xf;
            }
            else
            {
                // Transparent Color Mode - ARGB 3444
                c[0] = (int)(((colorData & 0xf000000u) >> 23) | ((colorData & 0xf000000u) >> 27));
                c[1] = (int)(((colorData & 0xf00000u) >> 19) | ((colorData & 0xf00000u) >> 23));
                c[2] = (int)(((colorData & 0xf0000u) >> 15) | ((colorData & 0xf0000u) >> 19));
                c[3] = (int)((colorData & 0x70000000u) >> 27);
            }
        }

        // Bilinear upscale of the 2x2 word colours to full word resolution.
        // outPixels: packed RGBA ints, length wordWidth*wordHeight.
        private static void InterpolateColors(int[] P, int[] Q, int[] R, int[] S, int[] outPixels, bool do2bppMode)
        {
            int wordWidth = do2bppMode ? 8 : 4;
            int wordHeight = 4;

            int[] hP = { P[0], P[1], P[2], P[3] };
            int[] hQ = { Q[0], Q[1], Q[2], Q[3] };
            int[] hR = { R[0], R[1], R[2], R[3] };
            int[] hS = { S[0], S[1], S[2], S[3] };

            int[] QminusP = { hQ[0] - hP[0], hQ[1] - hP[1], hQ[2] - hP[2], hQ[3] - hP[3] };
            int[] SminusR = { hS[0] - hR[0], hS[1] - hR[1], hS[2] - hR[2], hS[3] - hR[3] };

            for (int i = 0; i < 4; i++)
            {
                hP[i] *= wordWidth;
                hR[i] *= wordWidth;
            }

            if (do2bppMode)
            {
                for (int x = 0; x < wordWidth; x++)
                {
                    int[] result = { 4 * hP[0], 4 * hP[1], 4 * hP[2], 4 * hP[3] };
                    int[] dY = { hR[0] - hP[0], hR[1] - hP[1], hR[2] - hP[2], hR[3] - hP[3] };

                    for (int y = 0; y < wordHeight; y++)
                    {
                        int r = (result[0] >> 7) + (result[0] >> 2);
                        int g = (result[1] >> 7) + (result[1] >> 2);
                        int b = (result[2] >> 7) + (result[2] >> 2);
                        int a = (result[3] >> 5) + (result[3] >> 1);
                        outPixels[y * wordWidth + x] = PackRGBA(r, g, b, a);

                        for (int i = 0; i < 4; i++) result[i] += dY[i];
                    }

                    for (int i = 0; i < 4; i++)
                    {
                        hP[i] += QminusP[i];
                        hR[i] += SminusR[i];
                    }
                }
            }
            else
            {
                for (int y = 0; y < wordHeight; y++)
                {
                    int[] result = { 4 * hP[0], 4 * hP[1], 4 * hP[2], 4 * hP[3] };
                    int[] dY = { hR[0] - hP[0], hR[1] - hP[1], hR[2] - hP[2], hR[3] - hP[3] };

                    for (int x = 0; x < wordWidth; x++)
                    {
                        int r = (result[0] >> 6) + (result[0] >> 1);
                        int g = (result[1] >> 6) + (result[1] >> 1);
                        int b = (result[2] >> 6) + (result[2] >> 1);
                        int a = (result[3] >> 4) + (result[3]);
                        outPixels[y * wordWidth + x] = PackRGBA(r, g, b, a);

                        for (int i = 0; i < 4; i++) result[i] += dY[i];
                    }

                    for (int i = 0; i < 4; i++)
                    {
                        hP[i] += QminusP[i];
                        hR[i] += SminusR[i];
                    }
                }
            }
        }

        private static void UnpackModulations(uint colorData, uint modulationData, int offsetX, int offsetY,
            int[,] modulationValues, int[,] modulationModes, bool do2bppMode)
        {
            uint WordModMode = colorData & 0x1u;
            uint ModulationBits = modulationData;

            if (do2bppMode)
            {
                if (WordModMode != 0)
                {
                    if ((ModulationBits & 0x1u) != 0)
                    {
                        if ((ModulationBits & (0x1u << 20)) != 0)
                        {
                            WordModMode = 3;
                        }
                        else
                        {
                            WordModMode = 2;
                        }

                        if ((ModulationBits & (0x1u << 21)) != 0)
                        {
                            ModulationBits |= (0x1u << 20);
                        }
                        else
                        {
                            ModulationBits &= ~(0x1u << 20);
                        }
                    }

                    if ((ModulationBits & 0x2u) != 0)
                    {
                        ModulationBits |= 0x1u;
                    }
                    else
                    {
                        ModulationBits &= ~0x1u;
                    }

                    for (int y = 0; y < 4; y++)
                    {
                        for (int x = 0; x < 8; x++)
                        {
                            modulationModes[x + offsetX, y + offsetY] = (int)WordModMode;

                            if (((x ^ y) & 1) == 0)
                            {
                                modulationValues[x + offsetX, y + offsetY] = (int)(ModulationBits & 3u);
                                ModulationBits >>= 2;
                            }
                        }
                    }
                }
                else
                {
                    for (int y = 0; y < 4; y++)
                    {
                        for (int x = 0; x < 8; x++)
                        {
                            modulationModes[x + offsetX, y + offsetY] = (int)WordModMode;

                            if ((ModulationBits & 1u) != 0)
                            {
                                modulationValues[x + offsetX, y + offsetY] = 0x3;
                            }
                            else
                            {
                                modulationValues[x + offsetX, y + offsetY] = 0x0;
                            }
                            ModulationBits >>= 1;
                        }
                    }
                }
            }
            else
            {
                if (WordModMode != 0)
                {
                    for (int y = 0; y < 4; y++)
                    {
                        for (int x = 0; x < 4; x++)
                        {
                            int v = (int)(ModulationBits & 3u);
                            if (v == 1) v = 4;
                            else if (v == 2) v = 14; // +10 tells the decompressor to punch through alpha.
                            else if (v == 3) v = 8;
                            modulationValues[y + offsetY, x + offsetX] = v;
                            ModulationBits >>= 2;
                        }
                    }
                }
                else
                {
                    for (int y = 0; y < 4; y++)
                    {
                        for (int x = 0; x < 4; x++)
                        {
                            int v = (int)(ModulationBits & 3u);
                            v *= 3;
                            if (v > 3) v -= 1;
                            modulationValues[y + offsetY, x + offsetX] = v;
                            ModulationBits >>= 2;
                        }
                    }
                }
            }
        }

        private static readonly int[] RepVals0 = { 0, 3, 5, 8 };

        private static int GetModulationValues(int[,] modulationValues, int[,] modulationModes, int xPos, int yPos, bool do2bppMode)
        {
            if (do2bppMode)
            {
                if (modulationModes[xPos, yPos] == 0)
                {
                    return RepVals0[modulationValues[xPos, yPos]];
                }
                else
                {
                    if (((xPos ^ yPos) & 1) == 0)
                    {
                        return RepVals0[modulationValues[xPos, yPos]];
                    }
                    else if (modulationModes[xPos, yPos] == 1)
                    {
                        return (RepVals0[modulationValues[xPos, yPos - 1]] + RepVals0[modulationValues[xPos, yPos + 1]] +
                                RepVals0[modulationValues[xPos - 1, yPos]] + RepVals0[modulationValues[xPos + 1, yPos]] + 2) / 4;
                    }
                    else if (modulationModes[xPos, yPos] == 2)
                    {
                        return (RepVals0[modulationValues[xPos - 1, yPos]] + RepVals0[modulationValues[xPos + 1, yPos]] + 1) / 2;
                    }
                    else
                    {
                        return (RepVals0[modulationValues[xPos, yPos - 1]] + RepVals0[modulationValues[xPos, yPos + 1]] + 1) / 2;
                    }
                }
            }
            else
            {
                return modulationValues[xPos, yPos];
            }
        }

        private static void PvrtcGetDecompressedPixels(uint pColor, uint pMod, uint qColor, uint qMod,
            uint rColor, uint rMod, uint sColor, uint sMod, int[] pColorData, bool do2bppMode)
        {
            int[,] modulationValues = new int[16, 8];
            int[,] modulationModes = new int[16, 8];
            int[] upscaledColorA = new int[32];
            int[] upscaledColorB = new int[32];

            int wordWidth = do2bppMode ? 8 : 4;
            int wordHeight = 4;

            UnpackModulations(pColor, pMod, 0, 0, modulationValues, modulationModes, do2bppMode);
            UnpackModulations(qColor, qMod, wordWidth, 0, modulationValues, modulationModes, do2bppMode);
            UnpackModulations(rColor, rMod, 0, wordHeight, modulationValues, modulationModes, do2bppMode);
            UnpackModulations(sColor, sMod, wordWidth, wordHeight, modulationValues, modulationModes, do2bppMode);

            int[] cA = new int[4], cB = new int[4], cC = new int[4], cD = new int[4];
            GetColorA(pColor, cA); GetColorA(qColor, cB); GetColorA(rColor, cC); GetColorA(sColor, cD);
            InterpolateColors(cA, cB, cC, cD, upscaledColorA, do2bppMode);
            GetColorB(pColor, cA); GetColorB(qColor, cB); GetColorB(rColor, cC); GetColorB(sColor, cD);
            InterpolateColors(cA, cB, cC, cD, upscaledColorB, do2bppMode);

            for (int y = 0; y < wordHeight; y++)
            {
                for (int x = 0; x < wordWidth; x++)
                {
                    int mod = GetModulationValues(modulationValues, modulationModes, x + wordWidth / 2, y + wordHeight / 2, do2bppMode);
                    bool punchthroughAlpha = false;
                    if (mod > 10)
                    {
                        punchthroughAlpha = true;
                        mod -= 10;
                    }

                    int idx = y * wordWidth + x;
                    int aPix = upscaledColorA[idx];
                    int bPix = upscaledColorB[idx];

                    int aR = aPix & 0xFF, aG = (aPix >> 8) & 0xFF, aB = (aPix >> 16) & 0xFF, aA = (aPix >> 24) & 0xFF;
                    int bR = bPix & 0xFF, bG = (bPix >> 8) & 0xFF, bB = (bPix >> 16) & 0xFF, bA = (bPix >> 24) & 0xFF;

                    int r = (aR * (8 - mod) + bR * mod) / 8;
                    int g = (aG * (8 - mod) + bG * mod) / 8;
                    int b = (aB * (8 - mod) + bB * mod) / 8;
                    int a = punchthroughAlpha ? 0 : (aA * (8 - mod) + bA * mod) / 8;

                    int packed = PackRGBA(r, g, b, a);

                    if (do2bppMode)
                    {
                        pColorData[y * wordWidth + x] = packed;
                    }
                    else
                    {
                        pColorData[y + x * wordHeight] = packed;
                    }
                }
            }
        }

        private static uint TwiddleUV(uint XSize, uint YSize, uint XPos, uint YPos)
        {
            uint MinDimension = XSize;
            uint MaxValue = YPos;
            uint Twiddled = 0;
            uint SrcBitPos = 1;
            uint DstBitPos = 1;
            int ShiftCount = 0;

            if (YSize < XSize)
            {
                MinDimension = YSize;
                MaxValue = XPos;
            }

            while (SrcBitPos < MinDimension)
            {
                if ((YPos & SrcBitPos) != 0) { Twiddled |= DstBitPos; }
                if ((XPos & SrcBitPos) != 0) { Twiddled |= (DstBitPos << 1); }

                SrcBitPos <<= 1;
                DstBitPos <<= 2;
                ShiftCount += 1;
            }

            MaxValue >>= ShiftCount;
            Twiddled |= (MaxValue << (2 * ShiftCount));

            return Twiddled;
        }

        private static int WrapWordIndex(int numWords, int word)
        {
            return ((word + numWords) % numWords);
        }

        private static void MapDecompressedData(int[] pOutput, int width, int[] pWord,
            int[] wP, int[] wQ, int[] wR, int[] wS, bool do2bppMode)
        {
            int wordWidth = do2bppMode ? 8 : 4;
            int wordHeight = 4;

            for (int y = 0; y < wordHeight / 2; y++)
            {
                for (int x = 0; x < wordWidth / 2; x++)
                {
                    pOutput[((wP[1] * wordHeight) + y + wordHeight / 2) * width + wP[0] * wordWidth + x + wordWidth / 2]
                        = pWord[y * wordWidth + x];

                    pOutput[((wQ[1] * wordHeight) + y + wordHeight / 2) * width + wQ[0] * wordWidth + x]
                        = pWord[y * wordWidth + x + wordWidth / 2];

                    pOutput[((wR[1] * wordHeight) + y) * width + wR[0] * wordWidth + x + wordWidth / 2]
                        = pWord[(y + wordHeight / 2) * wordWidth + x];

                    pOutput[((wS[1] * wordHeight) + y) * width + wS[0] * wordWidth + x]
                        = pWord[(y + wordHeight / 2) * wordWidth + x + wordWidth / 2];
                }
            }
        }

        private static int[] PvrtcDecompress(byte[] data, int width, int height, bool do2bppMode)
        {
            int wordWidth = do2bppMode ? 8 : 4;
            int wordHeight = 4;

            int numWords32 = data.Length / 4;
            uint[] words32 = new uint[numWords32];
            for (int i = 0; i < numWords32; i++)
            {
                words32[i] = BitConverter.ToUInt32(data, i * 4);
            }

            int numX = width / wordWidth;
            int numY = height / wordHeight;

            int[] output = new int[width * height];
            int[] pPixels = new int[wordWidth * wordHeight];

            int[] wP = new int[2], wQ = new int[2], wR = new int[2], wS = new int[2];

            for (int wordY = -1; wordY < numY - 1; wordY++)
            {
                for (int wordX = -1; wordX < numX - 1; wordX++)
                {
                    wP[0] = WrapWordIndex(numX, wordX); wP[1] = WrapWordIndex(numY, wordY);
                    wQ[0] = WrapWordIndex(numX, wordX + 1); wQ[1] = WrapWordIndex(numY, wordY);
                    wR[0] = WrapWordIndex(numX, wordX); wR[1] = WrapWordIndex(numY, wordY + 1);
                    wS[0] = WrapWordIndex(numX, wordX + 1); wS[1] = WrapWordIndex(numY, wordY + 1);

                    uint offP = TwiddleUV((uint)numX, (uint)numY, (uint)wP[0], (uint)wP[1]) * 2;
                    uint offQ = TwiddleUV((uint)numX, (uint)numY, (uint)wQ[0], (uint)wQ[1]) * 2;
                    uint offR = TwiddleUV((uint)numX, (uint)numY, (uint)wR[0], (uint)wR[1]) * 2;
                    uint offS = TwiddleUV((uint)numX, (uint)numY, (uint)wS[0], (uint)wS[1]) * 2;

                    PvrtcGetDecompressedPixels(
                        words32[offP + 1], words32[offP],
                        words32[offQ + 1], words32[offQ],
                        words32[offR + 1], words32[offR],
                        words32[offS + 1], words32[offS],
                        pPixels, do2bppMode);

                    MapDecompressedData(output, width, pPixels, wP, wQ, wR, wS, do2bppMode);
                }
            }

            return output;
        }
    }
}
