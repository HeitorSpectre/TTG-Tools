using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace TTG_Tools.Graphics.Xbox360
{
    /// <summary>
    /// Decoding of Xbox 360 "tiled" DXT textures.
    /// Pipeline: 16-bit byte-swap -> untile (XGUntile) -> DXT decode -> RGBA.
    /// </summary>
    public static class XboxTexture
    {
        public static Bitmap Decode(D3dtx t)
        {
            if (t.Kind == TexKind.Compressed)
            {
                // "Type 2" texture: XMemCompress (LZX) + planar DXT.
                byte[] tiled = TelltaleType2.DecodeToTiledDxt(t);
                return DecodeTiled(tiled, t.Width, t.Height, t.DxtName, t.BytesPerBlock);
            }

            byte[] file = File.ReadAllBytes(t.FilePath);
            int size = (int)t.PixelDataSize;
            byte[] data = new byte[size];
            Array.Copy(file, t.PixelDataOffset, data, 0, size);
            return DecodeTiled(data, t.Width, t.Height, t.DxtName, t.BytesPerBlock);
        }

        /// <summary>
        /// Tells whether a texture uses the Xbox 360 GPU "tiled" layout.
        ///
        /// Tiling operates on 32x32-block macro-tiles (= 128-pixel rows for DXT).
        /// A texture is tiled as soon as its width reaches one macro-tile (>= 32
        /// blocks = 128 pixels). The height does NOT need to reach a macro-tile:
        /// short-but-wide textures (Walking Dead font atlas pages such as 512x64
        /// or 512x128) are still tiled, just with fewer macro-tile rows.
        /// </summary>
        public static bool IsTiled(int width, int height)
        {
            int wBlocks = (width + 3) / 4;
            return wBlocks >= 32;
        }

        /// <summary>
        /// Decodes an Xbox 360 DXT buffer (already interleaved) to a Bitmap.
        /// Pipeline: 16-bit byte-swap -> [untile, if tiled] -> DXT decode.
        /// </summary>
        public static Bitmap DecodeTiled(byte[] data, int width, int height,
                                         string dxtName, int bpb)
        {
            byte[] copy = new byte[data.Length];
            Array.Copy(data, copy, data.Length);

            // 1) Xbox 360 stores texture data as swapped 16-bit words.
            ByteSwap16(copy);

            // 2) Undo the GPU "tiling" - only for textures large enough to
            //    use the tiled layout (>= 32x32 blocks).
            byte[] blocks = IsTiled(width, height)
                ? Untile(copy, width, height, bpb)
                : copy;

            // 3) Decode the DXT blocks into RGBA.
            byte[] rgba;
            if (bpb == 8)
                rgba = DecodeDxt1(blocks, width, height);
            else if (dxtName == "DXT3")
                rgba = DecodeDxt3(blocks, width, height);
            else
                rgba = DecodeDxt5(blocks, width, height);

            return RgbaToBitmap(rgba, width, height);
        }

        // ---- byte swap ----------------------------------------------------
        private static void ByteSwap16(byte[] d)
        {
            for (int i = 0; i + 1 < d.Length; i += 2)
            {
                byte tmp = d[i];
                d[i] = d[i + 1];
                d[i + 1] = tmp;
            }
        }

        // ---- untile (Xbox 360 / XGUntile) --------------------------------
        // Based on UEViewer (UnTexture.cpp) / XboxDecoder.cs.
        private static byte[] Untile(byte[] src, int w, int h, int bpb)
        {
            byte[] dst = new byte[src.Length];
            int alignedW = D3dtx.Align(w, 128);
            int tiledBlockW = alignedW / 4;
            int blockW = w / 4;
            int blockH = h / 4;
            int logBpp = Log2(bpb);

            for (int dy = 0; dy < blockH; dy++)
            {
                for (int dx = 0; dx < blockW; dx++)
                {
                    uint sa = TiledOffset(dx, dy, tiledBlockW, logBpp);
                    int sy = (int)(sa / (uint)tiledBlockW);
                    int sx = (int)(sa % (uint)tiledBlockW);
                    int dstStart = (dy * blockW + dx) * bpb;
                    int srcStart = (sy * tiledBlockW + sx) * bpb;
                    if (srcStart + bpb <= src.Length && dstStart + bpb <= dst.Length)
                        Array.Copy(src, srcStart, dst, dstStart, bpb);
                }
            }
            return dst;
        }

        // ---- tile (exact inverse of Untile) - used during reinsertion ----
        private static byte[] Tile(byte[] linear, int w, int h, int bpb)
        {
            int tiledBlockW = D3dtx.Align(w, 128) / 4;
            int blockW = w / 4, blockH = h / 4;
            int logBpp = Log2(bpb);
            byte[] dst = new byte[tiledBlockW * blockH * bpb];
            for (int dy = 0; dy < blockH; dy++)
                for (int dx = 0; dx < blockW; dx++)
                {
                    uint sa = TiledOffset(dx, dy, tiledBlockW, logBpp);
                    int dstStart = (int)sa * bpb;
                    int srcStart = (dy * blockW + dx) * bpb;
                    if (dstStart + bpb <= dst.Length && srcStart + bpb <= linear.Length)
                        Array.Copy(linear, srcStart, dst, dstStart, bpb);
                }
            return dst;
        }

        // ---- 2x2 box downsample of an RGBA image (used to generate mips) ----
        private static byte[] Downsample(byte[] rgba, int w, int h, out int nw, out int nh)
        {
            nw = Math.Max(1, w / 2);
            nh = Math.Max(1, h / 2);
            byte[] outp = new byte[nw * nh * 4];
            for (int y = 0; y < nh; y++)
                for (int x = 0; x < nw; x++)
                {
                    for (int c = 0; c < 4; c++)
                    {
                        int x0 = Math.Min(x * 2, w - 1), x1 = Math.Min(x * 2 + 1, w - 1);
                        int y0 = Math.Min(y * 2, h - 1), y1 = Math.Min(y * 2 + 1, h - 1);
                        int sum = rgba[(y0 * w + x0) * 4 + c] + rgba[(y0 * w + x1) * 4 + c]
                                + rgba[(y1 * w + x0) * 4 + c] + rgba[(y1 * w + x1) * 4 + c];
                        outp[(y * nw + x) * 4 + c] = (byte)(sum / 4);
                    }
                }
            return outp;
        }

        /// <summary>
        /// Builds the DXT block buffer (interleaved, byte-swapped, tiled)
        /// from an RGBA image - generates the full mip chain, encodes it in
        /// DXT, applies the Xbox 360 tiling where it fits and the 16-bit
        /// byte-swap. The result is the "B" that
        /// <see cref="TelltaleType2.RebuildD3dtx"/> consumes.
        /// </summary>
        public static byte[] BuildBlockBuffer(byte[] rgba, int w, int h,
                                              string dxtName, int bpb)
        {
            using (var ms = new MemoryStream())
            {
                byte[] cur = rgba;
                int cw = w, ch = h;
                while (true)
                {
                    byte[] linDxt = (bpb == 8)
                        ? DxtEncoder.EncodeDxt1(cur, cw, ch)
                        : DxtEncoder.EncodeDxt5(cur, cw, ch);
                    byte[] mip = IsTiled(cw, ch) ? Tile(linDxt, cw, ch, bpb) : linDxt;
                    byte[] swapped = (byte[])mip.Clone();
                    ByteSwap16(swapped);
                    ms.Write(swapped, 0, swapped.Length);
                    if (cw <= 1 && ch <= 1) break;
                    cur = Downsample(cur, cw, ch, out cw, out ch);
                }
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Reinserts a "Type 1" (RawTiled) texture from an RGBA image.
        /// RawTiled textures store a single tiled DXT mip at the end of the
        /// file, padded to 128-pixel aligned dimensions. This method DXT-
        /// encodes the image, tiles it with the padded pitch, applies the
        /// 16-bit byte-swap and replaces the trailing pixel-data region.
        /// The .d3dtx header is preserved byte-for-byte.
        /// </summary>
        public static byte[] ReinsertRawTiled(D3dtx t, byte[] rgba)
        {
            if (t.Kind != TexKind.RawTiled)
                throw new InvalidDataException("Not a RawTiled (Type 1) texture.");

            // 1) DXT-encode the image at the original w x h.
            byte[] linDxt = (t.BytesPerBlock == 8)
                ? DxtEncoder.EncodeDxt1(rgba, t.Width, t.Height)
                : DxtEncoder.EncodeDxt5(rgba, t.Width, t.Height);

            // 2) Tile into a buffer with 128-pixel padded dimensions (RawTiled rule).
            int padW = D3dtx.Align(t.Width, 128);
            int padH = D3dtx.Align(t.Height, 128);
            int pitch = padW / 4;
            int padBh = padH / 4;
            int bw = t.Width / 4;
            int bh = t.Height / 4;
            int bpb = t.BytesPerBlock;
            int logBpp = Log2(bpb);
            byte[] tiled = new byte[pitch * padBh * bpb];
            for (int dy = 0; dy < bh; dy++)
                for (int dx = 0; dx < bw; dx++)
                {
                    uint sa = TiledOffset(dx, dy, pitch, logBpp);
                    int dstStart = (int)sa * bpb;
                    int srcStart = (dy * bw + dx) * bpb;
                    if (dstStart + bpb <= tiled.Length)
                        Array.Copy(linDxt, srcStart, tiled, dstStart, bpb);
                }

            // 3) 16-bit byte-swap (Xbox 360 storage convention).
            ByteSwap16(tiled);

            if (tiled.Length != t.PixelDataSize)
                throw new InvalidDataException(
                    "RawTiled rebuild produced " + tiled.Length
                    + " bytes, expected " + t.PixelDataSize + ".");

            // 4) Reassemble file = original header + new pixel data.
            byte[] orig = File.ReadAllBytes(t.FilePath);
            byte[] outp = new byte[t.PixelDataOffset + tiled.Length];
            Array.Copy(orig, 0, outp, 0, t.PixelDataOffset);
            Array.Copy(tiled, 0, outp, (int)t.PixelDataOffset, tiled.Length);
            return outp;
        }

        private static uint TiledOffset(int x, int y, int width, int logBpb)
        {
            int alignedWidth = D3dtx.Align(width, 32);
            int macro = ((x >> 5) + (y >> 5) * (alignedWidth >> 5)) << (logBpb + 7);
            int micro = ((x & 7) + ((y & 0xE) << 2)) << logBpb;
            int offset = macro + ((micro & ~0xF) << 1) + (micro & 0xF) + ((y & 1) << 4);
            return (uint)((((offset & ~0x1FF) << 3) +
                           ((y & 16) << 7) +
                           ((offset & 0x1C0) << 2) +
                           (((((y & 8) >> 2) + (x >> 3)) & 3) << 6) +
                           (offset & 0x3F)) >> logBpb);
        }

        private static int Log2(int n)
        {
            int r = -1;
            while (n != 0) { n >>= 1; r++; }
            return r;
        }

        // ---- DXT decoders -------------------------------------------------
        private static void Color565(ushort c, out int r, out int g, out int b)
        {
            int r5 = (c >> 11) & 0x1F, g6 = (c >> 5) & 0x3F, b5 = c & 0x1F;
            r = (r5 << 3) | (r5 >> 2);
            g = (g6 << 2) | (g6 >> 4);
            b = (b5 << 3) | (b5 >> 2);
        }

        private static byte[] DecodeDxt1(byte[] d, int w, int h)
        {
            byte[] outp = new byte[w * h * 4];
            int bw = (w + 3) / 4, bh = (h + 3) / 4, o = 0;
            int[] r = new int[4], g = new int[4], b = new int[4], a = new int[4];
            for (int by = 0; by < bh; by++)
                for (int bx = 0; bx < bw; bx++, o += 8)
                {
                    if (o + 8 > d.Length) return outp;
                    ushort c0 = (ushort)(d[o] | d[o + 1] << 8);
                    ushort c1 = (ushort)(d[o + 2] | d[o + 3] << 8);
                    uint bits = (uint)(d[o + 4] | d[o + 5] << 8 | d[o + 6] << 16 | d[o + 7] << 24);
                    Color565(c0, out r[0], out g[0], out b[0]);
                    Color565(c1, out r[1], out g[1], out b[1]);
                    a[0] = a[1] = a[2] = a[3] = 255;
                    if (c0 > c1)
                    {
                        r[2] = (2 * r[0] + r[1]) / 3; g[2] = (2 * g[0] + g[1]) / 3; b[2] = (2 * b[0] + b[1]) / 3;
                        r[3] = (r[0] + 2 * r[1]) / 3; g[3] = (g[0] + 2 * g[1]) / 3; b[3] = (b[0] + 2 * b[1]) / 3;
                    }
                    else
                    {
                        r[2] = (r[0] + r[1]) / 2; g[2] = (g[0] + g[1]) / 2; b[2] = (b[0] + b[1]) / 2;
                        r[3] = g[3] = b[3] = 0; a[3] = 0;
                    }
                    for (int py = 0; py < 4; py++)
                        for (int px = 0; px < 4; px++)
                        {
                            int x = bx * 4 + px, y = by * 4 + py;
                            if (x >= w || y >= h) continue;
                            int idx = (int)((bits >> (2 * (py * 4 + px))) & 3);
                            int di = (y * w + x) * 4;
                            outp[di] = (byte)r[idx]; outp[di + 1] = (byte)g[idx];
                            outp[di + 2] = (byte)b[idx]; outp[di + 3] = (byte)a[idx];
                        }
                }
            return outp;
        }

        private static byte[] DecodeDxt3(byte[] d, int w, int h)
        {
            byte[] outp = new byte[w * h * 4];
            int bw = (w + 3) / 4, bh = (h + 3) / 4, o = 0;
            int[] r = new int[4], g = new int[4], b = new int[4];
            for (int by = 0; by < bh; by++)
                for (int bx = 0; bx < bw; bx++, o += 16)
                {
                    if (o + 16 > d.Length) return outp;
                    ulong alpha = BitConverter.ToUInt64(d, o);
                    int co = o + 8;
                    ushort c0 = (ushort)(d[co] | d[co + 1] << 8);
                    ushort c1 = (ushort)(d[co + 2] | d[co + 3] << 8);
                    uint bits = BitConverter.ToUInt32(d, co + 4);
                    Color565(c0, out r[0], out g[0], out b[0]);
                    Color565(c1, out r[1], out g[1], out b[1]);
                    r[2] = (2 * r[0] + r[1]) / 3; g[2] = (2 * g[0] + g[1]) / 3; b[2] = (2 * b[0] + b[1]) / 3;
                    r[3] = (r[0] + 2 * r[1]) / 3; g[3] = (g[0] + 2 * g[1]) / 3; b[3] = (b[0] + 2 * b[1]) / 3;
                    for (int py = 0; py < 4; py++)
                        for (int px = 0; px < 4; px++)
                        {
                            int x = bx * 4 + px, y = by * 4 + py;
                            if (x >= w || y >= h) continue;
                            int pi = py * 4 + px;
                            int idx = (int)((bits >> (2 * pi)) & 3);
                            int av = (int)((alpha >> (4 * pi)) & 0xF);
                            int di = (y * w + x) * 4;
                            outp[di] = (byte)r[idx]; outp[di + 1] = (byte)g[idx];
                            outp[di + 2] = (byte)b[idx]; outp[di + 3] = (byte)(av * 17);
                        }
                }
            return outp;
        }

        private static byte[] DecodeDxt5(byte[] d, int w, int h)
        {
            byte[] outp = new byte[w * h * 4];
            int bw = (w + 3) / 4, bh = (h + 3) / 4, o = 0;
            int[] r = new int[4], g = new int[4], b = new int[4];
            int[] al = new int[8];
            for (int by = 0; by < bh; by++)
                for (int bx = 0; bx < bw; bx++, o += 16)
                {
                    if (o + 16 > d.Length) return outp;
                    int a0 = d[o], a1 = d[o + 1];
                    ulong abits = 0;
                    for (int i = 0; i < 6; i++) abits |= (ulong)d[o + 2 + i] << (8 * i);
                    al[0] = a0; al[1] = a1;
                    if (a0 > a1)
                        for (int i = 1; i < 7; i++) al[i + 1] = ((7 - i) * a0 + i * a1) / 7;
                    else
                    {
                        for (int i = 1; i < 5; i++) al[i + 1] = ((5 - i) * a0 + i * a1) / 5;
                        al[6] = 0; al[7] = 255;
                    }
                    int co = o + 8;
                    ushort c0 = (ushort)(d[co] | d[co + 1] << 8);
                    ushort c1 = (ushort)(d[co + 2] | d[co + 3] << 8);
                    uint bits = BitConverter.ToUInt32(d, co + 4);
                    Color565(c0, out r[0], out g[0], out b[0]);
                    Color565(c1, out r[1], out g[1], out b[1]);
                    r[2] = (2 * r[0] + r[1]) / 3; g[2] = (2 * g[0] + g[1]) / 3; b[2] = (2 * b[0] + b[1]) / 3;
                    r[3] = (r[0] + 2 * r[1]) / 3; g[3] = (g[0] + 2 * g[1]) / 3; b[3] = (b[0] + 2 * b[1]) / 3;
                    for (int py = 0; py < 4; py++)
                        for (int px = 0; px < 4; px++)
                        {
                            int x = bx * 4 + px, y = by * 4 + py;
                            if (x >= w || y >= h) continue;
                            int pi = py * 4 + px;
                            int idx = (int)((bits >> (2 * pi)) & 3);
                            int aidx = (int)((abits >> (3 * pi)) & 7);
                            int di = (y * w + x) * 4;
                            outp[di] = (byte)r[idx]; outp[di + 1] = (byte)g[idx];
                            outp[di + 2] = (byte)b[idx]; outp[di + 3] = (byte)al[aidx];
                        }
                }
            return outp;
        }

        // ---- RGBA -> Bitmap ----------------------------------------------
        private static Bitmap RgbaToBitmap(byte[] rgba, int w, int h)
        {
            var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            var rect = new Rectangle(0, 0, w, h);
            BitmapData bd = bmp.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            byte[] bgra = new byte[rgba.Length];
            for (int i = 0; i < w * h; i++)
            {
                bgra[i * 4 + 0] = rgba[i * 4 + 2]; // B
                bgra[i * 4 + 1] = rgba[i * 4 + 1]; // G
                bgra[i * 4 + 2] = rgba[i * 4 + 0]; // R
                bgra[i * 4 + 3] = rgba[i * 4 + 3]; // A
            }
            Marshal.Copy(bgra, 0, bd.Scan0, bgra.Length);
            bmp.UnlockBits(bd);
            return bmp;
        }
    }
}
