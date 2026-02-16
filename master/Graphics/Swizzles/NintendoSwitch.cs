// Original algorithm by gdkchan
// Ported and improved (a tiny bit) by Stella/AboodXD
// C# port by Sudakov Pavel

using System;

namespace TTG_Tools.Graphics.Swizzles
{
    public class NintendoSwitch
    {
        private static int[] bpps = { 4, 4, 4, 2, 2, 2, 1, 2, 8, 16, 16, 8, 8, 16, 16 };
        private static int[,] xBases = { { 1, 2, 4, 8, 16 }, { 4, 3, 2, 1, 0 } };
        private static int[,] padds = { { 1, 2, 4, 8, 16 }, { 64, 32, 16, 8, 4 } };

        private static int GetData(int[,] mas, int bpp)
        {
            for (int i = 0; i < mas.Length; i++)
            {
                for (int j = 0; j < mas.Length; j++)
                {
                    if (mas[i, j] == bpp)
                    {
                        return mas[++i, j];
                    }
                }
            }

            return 0;
        }

        private static int GetBpp(int[] bpps, int code)
        {
            switch (code)
            {
                case 0x40:
                    return bpps[8];

                case 0x41:
                    return bpps[9];

                case 0x42:
                    return bpps[10];
            }
            return 0;
        }

        private static int RoundSize(int size, int pad)
        {
            int mark = pad - 1;

            if((size & mark) != 0)
            {
                size &= ~mark;
                size += pad;
            }

            return size;
        }

        private static int pow2RoundUp(int v)
        {
            v--;

            v |= (v++) >> 1;
            v |= v >> 2;
            v |= v >> 4;
            v |= v >> 8;
            v |= v >> 16;

            return v + 1;
        }

        private static bool isPow2(int v)
        {
            return (v != 0) && ((v & (v - 1)) == 0);
        }

        private static int CountZeros(int v)
        {
            int numZeros = 0;

            for (int i = 0; i < 32; i++)
            {
                if ((v & (1 << i)) != 0)
                    break;

                numZeros++;
            }

            return numZeros;
        }

        private static int getAddress(int x, int y, int xb, int yb, int width, int xBase)
        {
            int xCnt = xBase;
            int yCnt = 1;
            int xUsed = 0;
            int yUsed = 0;
            int address = 0;

            int xMask = 0;
            int yMask = 0;

            while((xUsed < xBase + 2) && (xUsed + xCnt < xb))
            {
                xMask = (1 << xCnt) - 1;
                yMask = (1 << yCnt) - 1;

                address |= (x & xMask) << xUsed + yUsed;
                address |= (y & yMask) << xUsed + yUsed + xCnt;

                x >>= xCnt;
                y >>= yCnt;

                xUsed += xCnt;
                yUsed += yCnt;

                xCnt = Math.Max(Math.Min(xb - xUsed, 1), 0);
                yCnt = Math.Max(Math.Min(yb - yUsed, yCnt << 1), 0);
            }

            address |= (x + y * (width >> xUsed)) << (xUsed + yUsed);

            return address;
        }

        private static int GetRequiredSwizzledSize(int originWidth, int originHeight, int bpp, int paddedWidth, int xb, int yb, int xBase)
        {
            int maxPos = 0;

            for (int y = 0; y < originHeight; y++)
            {
                for (int x = 0; x < originWidth; x++)
                {
                    int pos = getAddress(x, y, xb, yb, paddedWidth, xBase) * bpp;

                    if (pos > maxPos)
                    {
                        maxPos = pos;
                    }
                }
            }

            return maxPos + bpp;
        }

        public static int GetSwizzledMipSize(int width, int height, int code)
        {
            int bpp = GetBpp(bpps, code);

            if (bpp == 0)
            {
                return 0;
            }

            int originWidth = width;
            int originHeight = height;

            if (code >= 0x40 && code <= 0x42)
            {
                originHeight = (originHeight + 3) / 4;
                originWidth = (originWidth + 3) / 4;
            }

            int xb = CountZeros(pow2RoundUp(originWidth));
            int yb = CountZeros(pow2RoundUp(originHeight));

            int hh = pow2RoundUp(originHeight) >> 1;

            if (!isPow2(originHeight) && (originHeight <= (hh + (hh / 3))) && (yb > 3))
            {
                yb--;
            }

            int paddedWidth = RoundSize(originWidth, GetData(padds, bpp));
            int xBase = GetData(xBases, bpp);

            return GetRequiredSwizzledSize(originWidth, originHeight, bpp, paddedWidth, xb, yb, xBase);
        }

        public static byte[] NintendoSwizzle(byte[] content, int width, int height, int code, bool deswizzle)
        {
            int pos_ = 0;
            int bpp = GetBpp(bpps, code);

            if (bpp == 0)
            {
                return content;
            }

            int origin_width = width;
            int origin_height = height;

            if(code >= 0x40 && code <= 0x42)
            {
                origin_height = (origin_height + 3) / 4;
                origin_width = (origin_width + 3) / 4;
            }

            int xb = CountZeros(pow2RoundUp(origin_width));
            int yb = CountZeros(pow2RoundUp(origin_height));

            int hh = pow2RoundUp(origin_height) >> 1;

            if (!isPow2(origin_height) && (origin_height <= (hh + (hh / 3))) && (yb > 3))
                yb--;

            width = RoundSize(origin_width, GetData(padds, bpp));

            int xBase = GetData(xBases, bpp);

            int requiredSwizzledSize = GetRequiredSwizzledSize(origin_width, origin_height, bpp, width, xb, yb, xBase);

            int resultLength = deswizzle ? content.Length : Math.Max(content.Length, requiredSwizzledSize);

            byte[] result = new byte[resultLength];

            for(int y = 0; y < origin_height; y++)
            {
                for(int x = 0; x < origin_width; x++)
                {
                    int pos = getAddress(x, y, xb, yb, width, xBase) * bpp;

                    if (deswizzle)
                    {
                        if ((pos_ + bpp <= result.Length) && (pos + bpp <= content.Length))
                        {
                            Array.Copy(content, pos, result, pos_, bpp);
                        }
                    }
                    else
                    {
                        if ((pos + bpp <= result.Length) && (pos_ + bpp <= content.Length))
                        {
                            Array.Copy(content, pos_, result, pos, bpp);
                        }
                    }

                    pos_ += bpp;
                }
            }

            return result;
        }
    }
}
