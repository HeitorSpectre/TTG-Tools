using System;

namespace TTG_Tools.Graphics.Xbox360
{
    /// <summary>
    /// Codificador DXT1 / DXT5 (RGBA -> blocos DXT lineares). Usado para
    /// reinserir texturas modificadas. A compressao DXT e com perdas (e a
    /// natureza do formato), mas o resultado e um DXT valido.
    /// </summary>
    public static class DxtEncoder
    {
        /// <summary>Codifica RGBA (w*h*4) em blocos DXT1 lineares (8 B/bloco).</summary>
        public static byte[] EncodeDxt1(byte[] rgba, int w, int h)
        {
            int bw = (w + 3) / 4, bh = (h + 3) / 4;
            byte[] outp = new byte[bw * bh * 8];
            int o = 0;
            var px = new int[16 * 4];
            for (int by = 0; by < bh; by++)
                for (int bx = 0; bx < bw; bx++)
                {
                    GatherBlock(rgba, w, h, bx, by, px);
                    EncodeColorBlock(px, outp, o);
                    o += 8;
                }
            return outp;
        }

        /// <summary>Codifica RGBA (w*h*4) em blocos DXT5 lineares (16 B/bloco).</summary>
        public static byte[] EncodeDxt5(byte[] rgba, int w, int h)
        {
            int bw = (w + 3) / 4, bh = (h + 3) / 4;
            byte[] outp = new byte[bw * bh * 16];
            int o = 0;
            var px = new int[16 * 4];
            for (int by = 0; by < bh; by++)
                for (int bx = 0; bx < bw; bx++)
                {
                    GatherBlock(rgba, w, h, bx, by, px);
                    EncodeAlphaBlock(px, outp, o);
                    EncodeColorBlock(px, outp, o + 8);
                    o += 16;
                }
            return outp;
        }

        /// <summary>Codifica RGBA em blocos DXT3/BC2 (alpha explicito 4 bits + cor DXT1, 16 B/bloco).</summary>
        public static byte[] EncodeDxt3(byte[] rgba, int w, int h)
        {
            int bw = (w + 3) / 4, bh = (h + 3) / 4;
            byte[] outp = new byte[bw * bh * 16];
            int o = 0;
            var px = new int[16 * 4];
            for (int by = 0; by < bh; by++)
                for (int bx = 0; bx < bw; bx++)
                {
                    GatherBlock(rgba, w, h, bx, by, px);
                    ulong abits = 0;
                    for (int i = 0; i < 16; i++)
                    {
                        int a4 = (px[i * 4 + 3] * 15 + 127) / 255; // 8-bit alpha -> 4-bit
                        abits |= (ulong)(a4 & 0xF) << (4 * i);
                    }
                    for (int i = 0; i < 8; i++) outp[o + i] = (byte)(abits >> (8 * i));
                    EncodeColorBlock(px, outp, o + 8);
                    o += 16;
                }
            return outp;
        }

        /// <summary>Codifica RGBA em blocos BC4/ATI1 (canal unico = vermelho, 8 B/bloco).</summary>
        public static byte[] EncodeBc4(byte[] rgba, int w, int h)
        {
            int bw = (w + 3) / 4, bh = (h + 3) / 4;
            byte[] outp = new byte[bw * bh * 8];
            int o = 0;
            var px = new int[16 * 4];
            for (int by = 0; by < bh; by++)
                for (int bx = 0; bx < bw; bx++)
                {
                    GatherBlock(rgba, w, h, bx, by, px);
                    EncodeGrayBlock(px, 0, outp, o); // red channel
                    o += 8;
                }
            return outp;
        }

        /// <summary>Codifica RGBA em blocos BC5/ATI2 (dois canais: vermelho + verde, 16 B/bloco).</summary>
        public static byte[] EncodeBc5(byte[] rgba, int w, int h)
        {
            int bw = (w + 3) / 4, bh = (h + 3) / 4;
            byte[] outp = new byte[bw * bh * 16];
            int o = 0;
            var px = new int[16 * 4];
            for (int by = 0; by < bh; by++)
                for (int bx = 0; bx < bw; bx++)
                {
                    GatherBlock(rgba, w, h, bx, by, px);
                    EncodeGrayBlock(px, 0, outp, o);     // red
                    EncodeGrayBlock(px, 1, outp, o + 8); // green
                    o += 16;
                }
            return outp;
        }

        // Coleta os 16 pixels RGBA de um bloco 4x4 (replica a borda se exceder).
        private static void GatherBlock(byte[] rgba, int w, int h, int bx, int by, int[] px)
        {
            for (int i = 0; i < 16; i++)
            {
                int x = bx * 4 + (i & 3);
                int y = by * 4 + (i >> 2);
                if (x >= w) x = w - 1;
                if (y >= h) y = h - 1;
                int s = (y * w + x) * 4;
                px[i * 4] = rgba[s]; px[i * 4 + 1] = rgba[s + 1];
                px[i * 4 + 2] = rgba[s + 2]; px[i * 4 + 3] = rgba[s + 3];
            }
        }

        // ---- bloco de cor (DXT1, modo 4 cores) ----
        private static void EncodeColorBlock(int[] px, byte[] outp, int o)
        {
            // endpoints = cantos da caixa delimitadora RGB
            int rMin = 255, gMin = 255, bMin = 255, rMax = 0, gMax = 0, bMax = 0;
            for (int i = 0; i < 16; i++)
            {
                int r = px[i * 4], g = px[i * 4 + 1], b = px[i * 4 + 2];
                if (r < rMin) rMin = r; if (r > rMax) rMax = r;
                if (g < gMin) gMin = g; if (g > gMax) gMax = g;
                if (b < bMin) bMin = b; if (b > bMax) bMax = b;
            }
            ushort c0 = Pack565(rMax, gMax, bMax);
            ushort c1 = Pack565(rMin, gMin, bMin);
            if (c0 < c1) { ushort tmp = c0; c0 = c1; c1 = tmp; }
            // se c0 == c1, garante modo 4 cores valido
            if (c0 == c1) { if (c1 > 0) c1--; else c0 = 1; }

            int[] pr = new int[4], pg = new int[4], pb = new int[4];
            Unpack565(c0, out pr[0], out pg[0], out pb[0]);
            Unpack565(c1, out pr[1], out pg[1], out pb[1]);
            for (int k = 0; k < 3; k++)
            {
                pr[2] = (2 * pr[0] + pr[1]) / 3; pg[2] = (2 * pg[0] + pg[1]) / 3; pb[2] = (2 * pb[0] + pb[1]) / 3;
                pr[3] = (pr[0] + 2 * pr[1]) / 3; pg[3] = (pg[0] + 2 * pg[1]) / 3; pb[3] = (pb[0] + 2 * pb[1]) / 3;
            }

            uint bits = 0;
            for (int i = 0; i < 16; i++)
            {
                int r = px[i * 4], g = px[i * 4 + 1], b = px[i * 4 + 2];
                int best = 0, bestD = int.MaxValue;
                for (int p = 0; p < 4; p++)
                {
                    int dr = r - pr[p], dg = g - pg[p], db = b - pb[p];
                    int d = dr * dr + dg * dg + db * db;
                    if (d < bestD) { bestD = d; best = p; }
                }
                bits |= (uint)best << (2 * i);
            }
            outp[o] = (byte)(c0 & 0xFF); outp[o + 1] = (byte)(c0 >> 8);
            outp[o + 2] = (byte)(c1 & 0xFF); outp[o + 3] = (byte)(c1 >> 8);
            outp[o + 4] = (byte)bits; outp[o + 5] = (byte)(bits >> 8);
            outp[o + 6] = (byte)(bits >> 16); outp[o + 7] = (byte)(bits >> 24);
        }

        // ---- bloco de alpha (DXT5, modo 8 alphas) ----
        private static void EncodeAlphaBlock(int[] px, byte[] outp, int o)
        {
            EncodeGrayBlock(px, 3, outp, o); // DXT5 alpha = channel 3
        }

        // Bloco de 8 bytes com 8 valores interpolados de um unico canal (usado por
        // DXT5 alpha, BC4 e BC5). ch = 0 R, 1 G, 2 B, 3 A.
        private static void EncodeGrayBlock(int[] px, int ch, byte[] outp, int o)
        {
            int aMin = 255, aMax = 0;
            for (int i = 0; i < 16; i++)
            {
                int a = px[i * 4 + ch];
                if (a < aMin) aMin = a;
                if (a > aMax) aMax = a;
            }
            if (aMin == aMax) { if (aMax < 255) aMax++; else aMin--; }
            int a0 = aMax, a1 = aMin;     // a0 > a1 -> 8 valores interpolados
            int[] al = new int[8];
            al[0] = a0; al[1] = a1;
            for (int i = 1; i < 7; i++) al[i + 1] = ((7 - i) * a0 + i * a1) / 7;

            outp[o] = (byte)a0;
            outp[o + 1] = (byte)a1;
            ulong bits = 0;
            for (int i = 0; i < 16; i++)
            {
                int a = px[i * 4 + ch];
                int best = 0, bestD = int.MaxValue;
                for (int p = 0; p < 8; p++)
                {
                    int d = a - al[p]; if (d < 0) d = -d;
                    if (d < bestD) { bestD = d; best = p; }
                }
                bits |= (ulong)best << (3 * i);
            }
            for (int i = 0; i < 6; i++)
                outp[o + 2 + i] = (byte)(bits >> (8 * i));
        }

        private static ushort Pack565(int r, int g, int b)
        {
            return (ushort)(((r >> 3) << 11) | ((g >> 2) << 5) | (b >> 3));
        }

        private static void Unpack565(ushort c, out int r, out int g, out int b)
        {
            int r5 = (c >> 11) & 0x1F, g6 = (c >> 5) & 0x3F, b5 = c & 0x1F;
            r = (r5 << 3) | (r5 >> 2);
            g = (g6 << 2) | (g6 >> 4);
            b = (b5 << 3) | (b5 >> 2);
        }
    }
}
