using System;

namespace TTG_Tools.Graphics.Swizzles
{
    public static class PSVita
    {
        private static int NextPowerOfTwo(int value)
        {
            if (value <= 1)
            {
                return 1;
            }

            int power = 1;
            while (power < value)
            {
                power <<= 1;
            }

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

        public static byte[] Swizzle(byte[] deswizzledData, int width, int height, int bytesPerPixelSet, int formatBitsPerPixel)
        {
            if (bytesPerPixelSet <= 0 || deswizzledData == null || deswizzledData.Length <= bytesPerPixelSet)
            {
                return deswizzledData;
            }

            int paddedWidth = NextPowerOfTwo(width);
            int paddedHeight = NextPowerOfTwo(height);

            int calculatedBufferSize = (formatBitsPerPixel * width * height) / 8;
            int paddedBufferSize = (formatBitsPerPixel * paddedWidth * paddedHeight) / 8;
            byte[] swizzledData = new byte[Math.Max(Math.Max(calculatedBufferSize, paddedBufferSize), bytesPerPixelSet)];

            int maxU = IntegerLog2(paddedWidth);
            int maxV = IntegerLog2(paddedHeight);
            int maxSwizzledTexels = Math.Min((swizzledData.Length / bytesPerPixelSet), paddedWidth * paddedHeight);

            for (int j = 0; j < maxSwizzledTexels; j++)
            {
                int u = 0;
                int v = 0;
                int origCoord = j;

                for (int k = 0; k < maxU || k < maxV; k++)
                {
                    if (k < maxV)
                    {
                        v |= (origCoord & 1) << k;
                        origCoord >>= 1;
                    }

                    if (k < maxU)
                    {
                        u |= (origCoord & 1) << k;
                        origCoord >>= 1;
                    }
                }

                if (u < width && v < height)
                {
                    int srcPos = (v * width + u) * bytesPerPixelSet;
                    int dstPos = j * bytesPerPixelSet;

                    if (srcPos + bytesPerPixelSet <= deswizzledData.Length && dstPos + bytesPerPixelSet <= swizzledData.Length)
                    {
                        Array.Copy(deswizzledData, srcPos, swizzledData, dstPos, bytesPerPixelSet);
                    }
                }
            }

            return swizzledData;
        }

        public static byte[] Unswizzle(byte[] swizzledData, int width, int height, int bytesPerPixelSet, int formatBitsPerPixel)
        {
            if (bytesPerPixelSet <= 0 || swizzledData == null || swizzledData.Length <= bytesPerPixelSet)
            {
                return swizzledData;
            }

            int calculatedBufferSize = (formatBitsPerPixel * width * height) / 8;
            byte[] unswizzledData = new byte[Math.Max(calculatedBufferSize, bytesPerPixelSet)];

            int paddedWidth = NextPowerOfTwo(width);
            int paddedHeight = NextPowerOfTwo(height);
            int maxU = IntegerLog2(paddedWidth);
            int maxV = IntegerLog2(paddedHeight);
            int maxSwizzledTexels = Math.Min((swizzledData.Length / bytesPerPixelSet), paddedWidth * paddedHeight);

            for (int j = 0; j < maxSwizzledTexels; j++)
            {
                int u = 0;
                int v = 0;
                int origCoord = j;

                for (int k = 0; k < maxU || k < maxV; k++)
                {
                    if (k < maxV)
                    {
                        v |= (origCoord & 1) << k;
                        origCoord >>= 1;
                    }

                    if (k < maxU)
                    {
                        u |= (origCoord & 1) << k;
                        origCoord >>= 1;
                    }
                }

                if (u < width && v < height)
                {
                    int srcPos = j * bytesPerPixelSet;
                    int dstPos = (v * width + u) * bytesPerPixelSet;

                    if (srcPos + bytesPerPixelSet <= swizzledData.Length && dstPos + bytesPerPixelSet <= unswizzledData.Length)
                    {
                        Array.Copy(swizzledData, srcPos, unswizzledData, dstPos, bytesPerPixelSet);
                    }
                }
            }

            return unswizzledData;
        }
    }
}
