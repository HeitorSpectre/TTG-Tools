using System;

namespace TTG_Tools.Graphics.Swizzles
{
    public class Xbox360
    {
        public static byte[] Unswizzle(byte[] data, int width, int height, int texelBytePitch, int blockPixelSize, bool performByteSwap)
        {
            byte[] processedData = data;

            if (performByteSwap)
            {
                processedData = SwapByteOrder(data);
            }

            return Convert(processedData, width, height, blockPixelSize, texelBytePitch, false);
        }

        public static byte[] Swizzle(byte[] data, int width, int height, int texelBytePitch, int blockPixelSize, bool performByteSwap)
        {
            byte[] swizzledData = Convert(data, width, height, blockPixelSize, texelBytePitch, true);

            if (performByteSwap)
            {
                return SwapByteOrder(swizzledData);
            }

            return swizzledData;
        }

        private static byte[] SwapByteOrder(byte[] data)
        {
            int length = data.Length & ~1;
            byte[] swapped = new byte[data.Length];
            Array.Copy(data, swapped, data.Length);

            for (int i = 0; i < length; i += 2)
            {
                swapped[i] = data[i + 1];
                swapped[i + 1] = data[i];
            }
            return swapped;
        }

        private static int GetLogBpp(int texelBytePitch)
        {
            return (texelBytePitch >> 2) + ((texelBytePitch >> 1) >> (texelBytePitch >> 2));
        }

        private static int GetAddressX(int blockOffset, int widthInBlocks, int texelBytePitch)
        {
            long alignedWidth = (widthInBlocks + 31) & ~31;
            long logBpp = GetLogBpp(texelBytePitch);
            long offsetByte = (long)blockOffset << (int)logBpp;
            long offsetTile = (((offsetByte & ~0xFFF) >> 3) + ((offsetByte & 0x700) >> 2) + (offsetByte & 0x3F));
            long offsetMacro = offsetTile >> (7 + (int)logBpp);

            long macroX = (offsetMacro % (alignedWidth >> 5)) << 2;
            long tile = (((offsetTile >> (5 + (int)logBpp)) & 2) + (offsetByte >> 6)) & 3;
            long macro = (macroX + tile) << 3;
            long micro = ((((offsetTile >> 1) & ~0xF) + (offsetTile & 0xF)) & ((texelBytePitch << 3) - 1)) >> (int)logBpp;

            return (int)(macro + micro);
        }

        private static int GetAddressY(int blockOffset, int widthInBlocks, int texelBytePitch)
        {
            long alignedWidth = (widthInBlocks + 31) & ~31;
            long logBpp = GetLogBpp(texelBytePitch);
            long offsetByte = (long)blockOffset << (int)logBpp;
            long offsetTile = (((offsetByte & ~0xFFF) >> 3) + ((offsetByte & 0x700) >> 2) + (offsetByte & 0x3F));
            long offsetMacro = offsetTile >> (7 + (int)logBpp);

            long macroY = (offsetMacro / (alignedWidth >> 5)) << 2;
            long tile = ((offsetTile >> (6 + (int)logBpp)) & 1) + ((offsetByte & 0x800) >> 10);
            long macro = (macroY + tile) << 3;
            long micro = (((offsetTile & ((texelBytePitch << 6) - 1 & ~0x1F)) + ((offsetTile & 0xF) << 1)) >> (3 + (int)logBpp)) & ~1;

            return (int)(macro + micro + ((offsetTile & 0x10) >> 4));
        }

        private static byte[] Convert(byte[] imageData, int imageWidth, int imageHeight, int blockPixelSize, int texelBytePitch, bool swizzleFlag)
        {
            // ==========================================================
            // INÍCIO DA CORREÇÃO
            // ==========================================================
            // Para formatos baseados em blocos (DXT), a dimensão mínima é de 1 bloco.
            // A divisão de inteiros pode resultar em 0 para mipmaps < 4 pixels, o que é incorreto.
            // Usamos Math.Max(1, ...) para garantir que mesmo um mipmap de 1x1 ocupe um bloco.
            int widthInBlocks = blockPixelSize > 1 ? Math.Max(1, imageWidth / blockPixelSize) : imageWidth / blockPixelSize;
            int heightInBlocks = blockPixelSize > 1 ? Math.Max(1, imageHeight / blockPixelSize) : imageHeight / blockPixelSize;
            // ==========================================================
            // FIM DA CORREÇÃO
            // ==========================================================

            int paddedWidthInBlocks = (widthInBlocks + 31) & ~31;
            int paddedHeightInBlocks = (heightInBlocks + 31) & ~31;
            int totalPaddedBlocks = paddedWidthInBlocks * paddedHeightInBlocks;

            byte[] convertedData;

            if (!swizzleFlag)
            {
                convertedData = new byte[widthInBlocks * heightInBlocks * texelBytePitch];
            }
            else
            {
                convertedData = new byte[totalPaddedBlocks * texelBytePitch];
            }

            for (int blockOffset = 0; blockOffset < totalPaddedBlocks; blockOffset++)
            {
                int x = GetAddressX(blockOffset, paddedWidthInBlocks, texelBytePitch);
                int y = GetAddressY(blockOffset, paddedWidthInBlocks, texelBytePitch);

                if (x < widthInBlocks && y < heightInBlocks)
                {
                    if (!swizzleFlag)
                    {
                        int srcByteOffset = blockOffset * texelBytePitch;
                        int destByteOffset = (y * widthInBlocks + x) * texelBytePitch;
                        if (srcByteOffset + texelBytePitch <= imageData.Length && destByteOffset + texelBytePitch <= convertedData.Length)
                        {
                            Array.Copy(imageData, srcByteOffset, convertedData, destByteOffset, texelBytePitch);
                        }
                    }
                    else
                    {
                        int srcByteOffset = (y * widthInBlocks + x) * texelBytePitch;
                        int destByteOffset = blockOffset * texelBytePitch;
                        if (srcByteOffset + texelBytePitch <= imageData.Length && destByteOffset + texelBytePitch <= convertedData.Length)
                        {
                            Array.Copy(imageData, srcByteOffset, convertedData, destByteOffset, texelBytePitch);
                        }
                    }
                }
            }
            return convertedData;
        }

        public static byte[] ConvertARGBtoBGRA(byte[] argbData)
        {
            if (argbData.Length % 4 != 0)
            {
                return argbData;
            }

            byte[] bgraData = new byte[argbData.Length];

            for (int i = 0; i < argbData.Length; i += 4)
            {
                byte a = argbData[i];
                byte r = argbData[i + 1];
                byte g = argbData[i + 2];
                byte b = argbData[i + 3];

                bgraData[i] = b;
                bgraData[i + 1] = g;
                bgraData[i + 2] = r;
                bgraData[i + 3] = a;
            }
            return bgraData;
        }

        public static byte[] ConvertBGRAtoARGB(byte[] bgraData)
        {
            if (bgraData.Length % 4 != 0)
            {
                return bgraData;
            }

            byte[] argbData = new byte[bgraData.Length];

            for (int i = 0; i < bgraData.Length; i += 4)
            {
                // Bytes de entrada: [B, G, R, A]
                byte b = bgraData[i];
                byte g = bgraData[i + 1];
                byte r = bgraData[i + 2];
                byte a = bgraData[i + 3];

                // Bytes de saída: [A, R, G, B]
                argbData[i] = a;
                argbData[i + 1] = r;
                argbData[i + 2] = g;
                argbData[i + 3] = b;
            }
            return argbData;
        }
    }
}