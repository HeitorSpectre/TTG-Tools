using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using TTG_Tools.Graphics.Xbox360;

namespace TTG_Tools.Graphics.DDS
{
    // Converts between DDS textures and PNG for the "extract textures as PNG" option.
    // Supported (both ways): BC1, BC2, BC3, BC4, BC5. Anything else returns null so the
    // caller can fall back to plain DDS. Internal RGBA buffers are in R,G,B,A byte order
    // (matching DxtEncoder); conversion to/from the BGRA GDI+ bitmap happens at the PNG edge.
    public static class DdsPngConverter
    {
        private const uint BC1 = 0x40, BC2 = 0x41, BC3 = 0x42, BC4 = 0x43, BC5 = 0x44;

        // Old-format (OldTextureFormat) fourCC values, used by pre-Poker-Night-2 textures.
        private const uint DX_DXT1 = 0x31545844, DX_DXT3 = 0x33545844, DX_DXT5 = 0x35545844;

        public static bool IsConvertible(uint format)
        {
            return format == BC1 || format == BC2 || format == BC3 || format == BC4 || format == BC5;
        }

        // Maps a NewFormat (BCx) or OldFormat (DXTn fourCC) texture format to the BCx value used
        // by the encoder. Returns false for formats PNG conversion doesn't support.
        public static bool TryNormalize(uint format, out uint bcFormat)
        {
            switch (format)
            {
                case DX_DXT1: bcFormat = BC1; return true;
                case DX_DXT3: bcFormat = BC2; return true;
                case DX_DXT5: bcFormat = BC3; return true;
                default:
                    bcFormat = format;
                    return IsConvertible(format);
            }
        }

        private static int BlockBytes(uint format)
        {
            return (format == BC1 || format == BC4) ? 8 : 16;
        }

        // ---------- extraction: DDS bytes -> PNG bytes (null if unsupported) ----------
        public static byte[] DdsToPng(byte[] dds)
        {
            try
            {
                if (dds == null || dds.Length < 128) return null;
                if (dds[0] != 'D' || dds[1] != 'D' || dds[2] != 'S' || dds[3] != ' ') return null;

                int height = BitConverter.ToInt32(dds, 12);
                int width = BitConverter.ToInt32(dds, 16);
                string fourCc = System.Text.Encoding.ASCII.GetString(dds, 84, 4);

                uint format;
                int dataOffset;

                if (fourCc == "DX10")
                {
                    dataOffset = 148;
                    if (dds.Length < 148) return null;
                    int dxgi = BitConverter.ToInt32(dds, 128);
                    if (!DxgiToFormat(dxgi, out format)) return null;
                }
                else
                {
                    dataOffset = 128;
                    if (!FourCcToFormat(fourCc, out format)) return null;
                }

                if (width <= 0 || height <= 0) return null;

                byte[] rgba = DecodeFirstMip(dds, dataOffset, format, width, height);
                if (rgba == null) return null;

                return RgbaToPng(rgba, width, height);
            }
            catch
            {
                return null;
            }
        }

        // ---------- reinsertion: PNG bytes -> DDS bytes (null if unsupported) ----------
        // Rebuilds the whole mip chain (downscaling from the PNG) so the game keeps its mip count.
        public static byte[] PngToDds(byte[] png, uint format, int mipCount)
        {
            try
            {
                if (!IsConvertible(format)) return null;
                if (mipCount < 1) mipCount = 1;

                int w, h;
                byte[] rgba0 = PngToRgba(png, out w, out h);
                if (rgba0 == null || w <= 0 || h <= 0) return null;

                using (MemoryStream body = new MemoryStream())
                {
                    int mw = w, mh = h;
                    byte[] rgba = rgba0;
                    for (int m = 0; m < mipCount; m++)
                    {
                        if (m > 0) rgba = Downscale(rgba0, w, h, mw, mh);
                        byte[] enc = EncodeMip(rgba, format, mw, mh);
                        if (enc == null) return null;
                        body.Write(enc, 0, enc.Length);

                        if (mw > 1) mw /= 2;
                        if (mh > 1) mh /= 2;
                    }

                    return BuildDdsHeader(format, w, h, mipCount).Concat(body.ToArray());
                }
            }
            catch
            {
                return null;
            }
        }

        // ---------- reinsertion: PNG -> uncompressed DDS (ARGB8 0x00 / A8 0x10) ----------
        // Produces a full mip chain of uncompressed data readable by TextureWorker.ReadDDSHeader.
        // Used for PS Vita, where PVRTC textures are re-imported as lossless ARGB8 (format 0x00)
        // and A8 textures stay A8. Returns null on failure.
        public static byte[] PngToUncompressedDds(byte[] png, uint format, int mipCount)
        {
            try
            {
                if (format != 0x00 && format != 0x10) return null;
                if (mipCount < 1) mipCount = 1;

                int w, h;
                byte[] rgba0 = PngToRgba(png, out w, out h);
                if (rgba0 == null || w <= 0 || h <= 0) return null;

                using (MemoryStream body = new MemoryStream())
                {
                    int mw = w, mh = h;
                    for (int m = 0; m < mipCount; m++)
                    {
                        byte[] rgba = (m == 0) ? rgba0 : Downscale(rgba0, w, h, mw, mh);
                        int px = mw * mh;

                        if (format == 0x00) // ARGB8 stored as BGRA
                        {
                            byte[] enc = new byte[px * 4];
                            for (int i = 0; i < px; i++)
                            {
                                enc[i * 4] = rgba[i * 4 + 2];     // B
                                enc[i * 4 + 1] = rgba[i * 4 + 1]; // G
                                enc[i * 4 + 2] = rgba[i * 4];     // R
                                enc[i * 4 + 3] = rgba[i * 4 + 3]; // A
                            }
                            body.Write(enc, 0, enc.Length);
                        }
                        else // A8: value is stored in the grayscale RGB channel on extract, so read R
                        {
                            byte[] enc = new byte[px];
                            for (int i = 0; i < px; i++) enc[i] = rgba[i * 4];
                            body.Write(enc, 0, enc.Length);
                        }

                        if (mw > 1) mw /= 2;
                        if (mh > 1) mh /= 2;
                    }

                    return BuildUncompressedDdsHeader(format, w, h, mipCount).Concat(body.ToArray());
                }
            }
            catch
            {
                return null;
            }
        }

        // Converts an edited PNG into a single-mip PVR3 file holding uncompressed ARGB4444
        // (Telltale eSurface_ARGB4, 0x04). This is the format the community iOS mods use to
        // re-inject edited UI/textures: 2 bytes/pixel (half of ARGB8), no PVRTC re-encoder
        // needed, and natively supported by the iOS engine. The 16-bit value is packed as
        // B<<12 | G<<8 | R<<4 | A (alpha in the low nibble) so it is the exact inverse of
        // TextureWorker.DecodeUncompressedToRgba's 0x04 path. The PVR pixel-format label is
        // "rgba4444" (ChannelType 4), matching GenPvrHeader's 0x04 case so the re-insert
        // pipeline reads it back as texture format 0x04.
        public static byte[] PngToPvrArgb4(byte[] png)
        {
            try
            {
                int w, h;
                byte[] rgba = PngToRgba(png, out w, out h);
                if (rgba == null || w <= 0 || h <= 0) return null;

                int px = w * h;
                byte[] data = new byte[px * 2];
                for (int i = 0; i < px; i++)
                {
                    int r = (rgba[i * 4] * 15 + 127) / 255;
                    int g = (rgba[i * 4 + 1] * 15 + 127) / 255;
                    int b = (rgba[i * 4 + 2] * 15 + 127) / 255;
                    int a = (rgba[i * 4 + 3] * 15 + 127) / 255;
                    int v = (b << 12) | (g << 8) | (r << 4) | a;
                    data[i * 2] = (byte)(v & 0xFF);
                    data[i * 2 + 1] = (byte)((v >> 8) & 0xFF);
                }

                return BuildArgb4PvrHeader(w, h).Concat(data);
            }
            catch
            {
                return null;
            }
        }

        // 52-byte PVR3 header for a single-mip uncompressed rgba4444 surface.
        private static byte[] BuildArgb4PvrHeader(int width, int height)
        {
            byte[] hdr = new byte[52];
            using (MemoryStream ms = new MemoryStream(hdr))
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                bw.Write(BitConverter.ToUInt32(System.Text.Encoding.ASCII.GetBytes("PVR\x03"), 0)); // Version
                bw.Write((uint)0);                                                                  // Flags
                bw.Write(BitConverter.ToUInt64(System.Text.Encoding.ASCII.GetBytes("rgba\x4\x4\x4\x4"), 0)); // PixelFormat
                bw.Write((uint)0);   // ColorSpace (linear)
                bw.Write((uint)4);   // ChannelType (unsigned short normalised)
                bw.Write((uint)height);
                bw.Write((uint)width);
                bw.Write((uint)1);   // Depth
                bw.Write((uint)1);   // Surface
                bw.Write((uint)1);   // Face
                bw.Write((uint)1);   // Mip
                bw.Write((uint)0);   // MetaSize
            }
            return hdr;
        }

        // Standard 128-byte DDS header for uncompressed ARGB8 / A8, matching what
        // TextureWorker.ReadDDSHeader detects (DDPF_RGB 32bpp -> ARGB8, DDPF_ALPHA 8bpp -> A8).
        private static byte[] BuildUncompressedDdsHeader(uint format, int width, int height, int mipCount)
        {
            byte[] h = new byte[128];
            using (MemoryStream ms = new MemoryStream(h))
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                bw.Write(new byte[] { (byte)'D', (byte)'D', (byte)'S', (byte)' ' });
                bw.Write(124);                       // dwSize
                bw.Write(0x0002100F);                // CAPS|HEIGHT|WIDTH|PITCH|PIXELFORMAT|MIPMAPCOUNT
                bw.Write(height);
                bw.Write(width);
                int bpp = (format == 0x00) ? 4 : 1;
                bw.Write(width * bpp);               // dwPitchOrLinearSize (row pitch of mip 0)
                bw.Write(0);                         // depth
                bw.Write(mipCount);
                bw.Write(new byte[44]);              // reserved1[11]
                bw.Write(32);                        // ddspf.dwSize
                if (format == 0x00)
                {
                    bw.Write(0x41);                  // DDPF_RGB | DDPF_ALPHAPIXELS
                    bw.Write(new byte[4]);           // fourCC = 0
                    bw.Write(32);                    // RGBBitCount
                    bw.Write(0x00FF0000);            // R mask
                    bw.Write(0x0000FF00);            // G mask
                    bw.Write(0x000000FF);            // B mask
                    unchecked { bw.Write((int)0xFF000000); } // A mask
                }
                else // A8
                {
                    bw.Write(0x2);                   // DDPF_ALPHA (exact value ReadDDSHeader checks)
                    bw.Write(new byte[4]);           // fourCC = 0
                    bw.Write(8);                     // RGBBitCount
                    bw.Write(0);                     // R mask
                    bw.Write(0);                     // G mask
                    bw.Write(0);                     // B mask
                    unchecked { bw.Write((int)0xFF000000); } // A mask
                }
                bw.Write(0x401008);                  // caps: TEXTURE|COMPLEX|MIPMAP
                bw.Write(new byte[16]);              // caps2..reserved2
            }
            return h;
        }

        // ---------- format mapping ----------
        private static bool FourCcToFormat(string fourCc, out uint format)
        {
            switch (fourCc)
            {
                case "DXT1": format = BC1; return true;
                case "DXT3": format = BC2; return true;
                case "DXT5": format = BC3; return true;
                case "ATI1": case "BC4U": format = BC4; return true;
                case "ATI2": case "BC5U": format = BC5; return true;
                default: format = 0; return false;
            }
        }

        private static bool DxgiToFormat(int dxgi, out uint format)
        {
            switch (dxgi)
            {
                case 70: case 71: case 72: format = BC1; return true; // BC1
                case 73: case 74: case 75: format = BC2; return true; // BC2
                case 76: case 77: case 78: format = BC3; return true; // BC3
                case 79: case 80: case 81: format = BC4; return true; // BC4
                case 82: case 83: case 84: format = BC5; return true; // BC5
                default: format = 0; return false;
            }
        }

        private static string FourCcForFormat(uint format)
        {
            switch (format)
            {
                case BC1: return "DXT1";
                case BC2: return "DXT3";
                case BC3: return "DXT5";
                case BC4: return "ATI1";
                case BC5: return "ATI2";
                default: return "\0\0\0\0";
            }
        }

        // ---------- DDS header (standard 128 bytes, fourCC that ReadDDSHeader understands) ----------
        private static byte[] BuildDdsHeader(uint format, int width, int height, int mipCount)
        {
            byte[] h = new byte[128];
            using (MemoryStream ms = new MemoryStream(h))
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                bw.Write(new byte[] { (byte)'D', (byte)'D', (byte)'S', (byte)' ' });
                bw.Write(124);                       // dwSize
                bw.Write(0x000A1007);                // CAPS|HEIGHT|WIDTH|PIXELFORMAT|MIPMAPCOUNT|LINEARSIZE
                bw.Write(height);
                bw.Write(width);
                int blocks = ((width + 3) / 4) * ((height + 3) / 4);
                bw.Write(blocks * BlockBytes(format)); // dwPitchOrLinearSize (mip 0)
                bw.Write(0);                          // depth
                bw.Write(mipCount);
                bw.Write(new byte[44]);               // reserved1[11]
                bw.Write(32);                         // ddspf.dwSize
                bw.Write(0x4);                        // DDPF_FOURCC
                bw.Write(System.Text.Encoding.ASCII.GetBytes(FourCcForFormat(format)));
                bw.Write(0);                          // RGBBitCount
                bw.Write(new byte[16]);               // masks
                bw.Write(0x401008);                   // caps: TEXTURE|COMPLEX|MIPMAP
                bw.Write(new byte[16]);               // caps2..reserved2
            }
            return h;
        }

        private static byte[] Concat(this byte[] a, byte[] b)
        {
            byte[] r = new byte[a.Length + b.Length];
            Buffer.BlockCopy(a, 0, r, 0, a.Length);
            Buffer.BlockCopy(b, 0, r, a.Length, b.Length);
            return r;
        }

        // ---------- encode a single mip ----------
        private static byte[] EncodeMip(byte[] rgba, uint format, int w, int h)
        {
            switch (format)
            {
                case BC1: return DxtEncoder.EncodeDxt1(rgba, w, h);
                case BC2: return DxtEncoder.EncodeDxt3(rgba, w, h);
                case BC3: return DxtEncoder.EncodeDxt5(rgba, w, h);
                case BC4: return DxtEncoder.EncodeBc4(rgba, w, h);
                case BC5: return DxtEncoder.EncodeBc5(rgba, w, h);
                default: return null;
            }
        }

        // ---------- decode only the first (largest) mip ----------
        private static byte[] DecodeFirstMip(byte[] data, int off, uint format, int w, int h)
        {
            byte[] rgba = new byte[w * h * 4];
            switch (format)
            {
                case BC1: DecodeDxt1(data, off, w, h, rgba); return rgba;
                case BC2: DecodeDxt3(data, off, w, h, rgba); return rgba;
                case BC3: DecodeDxt5(data, off, w, h, rgba); return rgba;
                case BC4: DecodeBc4(data, off, w, h, rgba); return rgba;
                case BC5: DecodeBc5(data, off, w, h, rgba); return rgba;
                default: return null;
            }
        }

        // ---------- block decoders (output RGBA, R,G,B,A order) ----------
        private static void SetPixel(byte[] rgba, int w, int h, int bx, int by, int px, int py, int r, int g, int b, int a)
        {
            int x = bx * 4 + px, y = by * 4 + py;
            if (x >= w || y >= h) return;
            int d = (y * w + x) * 4;
            rgba[d] = (byte)r; rgba[d + 1] = (byte)g; rgba[d + 2] = (byte)b; rgba[d + 3] = (byte)a;
        }

        private static void Rgb565(ushort c, out int r, out int g, out int b)
        {
            int r5 = (c >> 11) & 0x1F, g6 = (c >> 5) & 0x3F, b5 = c & 0x1F;
            r = (r5 << 3) | (r5 >> 2);
            g = (g6 << 2) | (g6 >> 4);
            b = (b5 << 3) | (b5 >> 2);
        }

        private static void ColorPalette(ushort c0, ushort c1, int[] pr, int[] pg, int[] pb, out bool oneBitAlpha)
        {
            Rgb565(c0, out pr[0], out pg[0], out pb[0]);
            Rgb565(c1, out pr[1], out pg[1], out pb[1]);
            oneBitAlpha = c0 <= c1;
            if (!oneBitAlpha)
            {
                pr[2] = (2 * pr[0] + pr[1]) / 3; pg[2] = (2 * pg[0] + pg[1]) / 3; pb[2] = (2 * pb[0] + pb[1]) / 3;
                pr[3] = (pr[0] + 2 * pr[1]) / 3; pg[3] = (pg[0] + 2 * pg[1]) / 3; pb[3] = (pb[0] + 2 * pb[1]) / 3;
            }
            else
            {
                pr[2] = (pr[0] + pr[1]) / 2; pg[2] = (pg[0] + pg[1]) / 2; pb[2] = (pb[0] + pb[1]) / 2;
                pr[3] = 0; pg[3] = 0; pb[3] = 0;
            }
        }

        private static void DecodeColorBlock(byte[] data, int off, byte[] rgba, int w, int h, int bx, int by, byte[] alpha16)
        {
            ushort c0 = BitConverter.ToUInt16(data, off);
            ushort c1 = BitConverter.ToUInt16(data, off + 2);
            uint idx = BitConverter.ToUInt32(data, off + 4);
            int[] pr = new int[4], pg = new int[4], pb = new int[4];
            bool oneBit;
            ColorPalette(c0, c1, pr, pg, pb, out oneBit);

            for (int i = 0; i < 16; i++)
            {
                int code = (int)((idx >> (2 * i)) & 3);
                int a = alpha16 != null ? alpha16[i] : ((oneBit && code == 3) ? 0 : 255);
                SetPixel(rgba, w, h, bx, by, i & 3, i >> 2, pr[code], pg[code], pb[code], a);
            }
        }

        private static void DecodeDxt1(byte[] data, int off, int w, int h, byte[] rgba)
        {
            int bw = (w + 3) / 4, bh = (h + 3) / 4;
            for (int by = 0; by < bh; by++)
                for (int bx = 0; bx < bw; bx++)
                {
                    DecodeColorBlock(data, off, rgba, w, h, bx, by, null);
                    off += 8;
                }
        }

        private static void DecodeDxt3(byte[] data, int off, int w, int h, byte[] rgba)
        {
            int bw = (w + 3) / 4, bh = (h + 3) / 4;
            byte[] a16 = new byte[16];
            for (int by = 0; by < bh; by++)
                for (int bx = 0; bx < bw; bx++)
                {
                    ulong abits = BitConverter.ToUInt64(data, off);
                    for (int i = 0; i < 16; i++)
                    {
                        int a4 = (int)((abits >> (4 * i)) & 0xF);
                        a16[i] = (byte)(a4 * 17);
                    }
                    DecodeColorBlock(data, off + 8, rgba, w, h, bx, by, a16);
                    off += 16;
                }
        }

        private static void DecodeGrayBlock(byte[] data, int off, byte[] outVals16)
        {
            byte r0 = data[off], r1 = data[off + 1];
            byte[] p = new byte[8];
            p[0] = r0; p[1] = r1;
            if (r0 > r1)
                for (int i = 1; i < 7; i++) p[i + 1] = (byte)(((7 - i) * r0 + i * r1) / 7);
            else
            {
                for (int i = 1; i < 5; i++) p[i + 1] = (byte)(((5 - i) * r0 + i * r1) / 5);
                p[6] = 0; p[7] = 255;
            }
            ulong bits = 0;
            for (int i = 0; i < 6; i++) bits |= ((ulong)data[off + 2 + i]) << (8 * i);
            for (int i = 0; i < 16; i++) outVals16[i] = p[(int)((bits >> (3 * i)) & 7)];
        }

        private static void DecodeDxt5(byte[] data, int off, int w, int h, byte[] rgba)
        {
            int bw = (w + 3) / 4, bh = (h + 3) / 4;
            byte[] a16 = new byte[16];
            for (int by = 0; by < bh; by++)
                for (int bx = 0; bx < bw; bx++)
                {
                    DecodeGrayBlock(data, off, a16);
                    DecodeColorBlock(data, off + 8, rgba, w, h, bx, by, a16);
                    off += 16;
                }
        }

        private static void DecodeBc4(byte[] data, int off, int w, int h, byte[] rgba)
        {
            int bw = (w + 3) / 4, bh = (h + 3) / 4;
            byte[] v = new byte[16];
            for (int by = 0; by < bh; by++)
                for (int bx = 0; bx < bw; bx++)
                {
                    DecodeGrayBlock(data, off, v);
                    for (int i = 0; i < 16; i++) SetPixel(rgba, w, h, bx, by, i & 3, i >> 2, v[i], v[i], v[i], 255);
                    off += 8;
                }
        }

        private static void DecodeBc5(byte[] data, int off, int w, int h, byte[] rgba)
        {
            int bw = (w + 3) / 4, bh = (h + 3) / 4;
            byte[] vr = new byte[16], vg = new byte[16];
            for (int by = 0; by < bh; by++)
                for (int bx = 0; bx < bw; bx++)
                {
                    DecodeGrayBlock(data, off, vr);
                    DecodeGrayBlock(data, off + 8, vg);
                    for (int i = 0; i < 16; i++) SetPixel(rgba, w, h, bx, by, i & 3, i >> 2, vr[i], vg[i], 0, 255);
                    off += 16;
                }
        }

        // ---------- PNG <-> RGBA (R,G,B,A) via GDI+ (which is B,G,R,A) ----------
        // Public so other decoders (e.g. PVRTC for PS Vita) can emit PNGs with the same encoding.
        public static byte[] RgbaToPng(byte[] rgba, int w, int h)
        {
            using (Bitmap bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb))
            {
                BitmapData bd = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                byte[] bgra = new byte[w * h * 4];
                for (int i = 0; i < w * h; i++)
                {
                    bgra[i * 4] = rgba[i * 4 + 2];     // B
                    bgra[i * 4 + 1] = rgba[i * 4 + 1]; // G
                    bgra[i * 4 + 2] = rgba[i * 4];     // R
                    bgra[i * 4 + 3] = rgba[i * 4 + 3]; // A
                }
                Marshal.Copy(bgra, 0, bd.Scan0, bgra.Length);
                bmp.UnlockBits(bd);

                using (MemoryStream ms = new MemoryStream())
                {
                    bmp.Save(ms, ImageFormat.Png);
                    return ms.ToArray();
                }
            }
        }

        private static byte[] PngToRgba(byte[] png, out int w, out int h)
        {
            using (MemoryStream ms = new MemoryStream(png))
            using (Bitmap src = new Bitmap(ms))
            using (Bitmap bmp = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb))
            {
                using (var g = System.Drawing.Graphics.FromImage(bmp)) g.DrawImage(src, 0, 0, src.Width, src.Height);
                w = bmp.Width; h = bmp.Height;
                BitmapData bd = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                byte[] bgra = new byte[w * h * 4];
                Marshal.Copy(bd.Scan0, bgra, 0, bgra.Length);
                bmp.UnlockBits(bd);

                byte[] rgba = new byte[w * h * 4];
                for (int i = 0; i < w * h; i++)
                {
                    rgba[i * 4] = bgra[i * 4 + 2];     // R
                    rgba[i * 4 + 1] = bgra[i * 4 + 1]; // G
                    rgba[i * 4 + 2] = bgra[i * 4];     // B
                    rgba[i * 4 + 3] = bgra[i * 4 + 3]; // A
                }
                return rgba;
            }
        }

        // Downscale the full-res RGBA to (mw, mh) for a mip level, via GDI+.
        private static byte[] Downscale(byte[] rgba0, int w, int h, int mw, int mh)
        {
            using (Bitmap full = new Bitmap(w, h, PixelFormat.Format32bppArgb))
            {
                BitmapData bd = full.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                byte[] bgra = new byte[w * h * 4];
                for (int i = 0; i < w * h; i++)
                {
                    bgra[i * 4] = rgba0[i * 4 + 2]; bgra[i * 4 + 1] = rgba0[i * 4 + 1];
                    bgra[i * 4 + 2] = rgba0[i * 4]; bgra[i * 4 + 3] = rgba0[i * 4 + 3];
                }
                Marshal.Copy(bgra, 0, bd.Scan0, bgra.Length);
                full.UnlockBits(bd);

                using (Bitmap small = new Bitmap(mw, mh, PixelFormat.Format32bppArgb))
                {
                    using (var g = System.Drawing.Graphics.FromImage(small))
                    {
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        g.DrawImage(full, 0, 0, mw, mh);
                    }
                    BitmapData sbd = small.LockBits(new Rectangle(0, 0, mw, mh), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                    byte[] sbgra = new byte[mw * mh * 4];
                    Marshal.Copy(sbd.Scan0, sbgra, 0, sbgra.Length);
                    small.UnlockBits(sbd);

                    byte[] rgba = new byte[mw * mh * 4];
                    for (int i = 0; i < mw * mh; i++)
                    {
                        rgba[i * 4] = sbgra[i * 4 + 2]; rgba[i * 4 + 1] = sbgra[i * 4 + 1];
                        rgba[i * 4 + 2] = sbgra[i * 4]; rgba[i * 4 + 3] = sbgra[i * 4 + 3];
                    }
                    return rgba;
                }
            }
        }
    }
}
