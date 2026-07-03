using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TTG_Tools.Graphics.Xbox360
{
    /// <summary>
    /// Support for The Walking Dead: Season One Xbox 360 .font files.
    ///
    /// Container layout (reverse engineered against the validation samples):
    ///
    ///   "ERTM" + classCount(u32) + classTable(classCount*12 bytes)
    ///   samplerStateBlock(u32 size, then size-4 payload containing the font name)
    ///   font.One(1 byte)
    ///   BaseSize(float)
    ///   [optional] halfValue(float 0.5 or 1.0)
    ///   [if hasScaleValue] BlockCoordSize_check + CharCount_check + maybe oneValue
    ///   BlockCoordSize(u32)
    ///   CharCount(u32)
    ///   CharCount * 28 bytes glyph entries (TexNum:i32 + 6 floats: XStart, XEnd,
    ///     YStart, YEnd, CharWidth, CharHeight)
    ///   BlockTexSize(u32)
    ///   TexCount(u32)
    ///   Texture sub-block (one entry, ObjectName + SubobjectName + 010 +
    ///     numMipLevels + mD3DFormat = 0x20015400 + several Xbox-specific fields
    ///     + the marker {1.0f, 1.0f} + 4 zero bytes + dataSize(u32) + DXT5 data)
    ///
    /// Texture pixels are stored Xbox 360-style: byte-swapped 16-bit words +
    /// GPU tiled (32x32 macro-tiles aligned to 128px). After untiling + decoding
    /// we get a 32-bit ARGB atlas. Width is 512 in every validation sample but
    /// we derive it from the first non-empty glyph so other dimensions work too.
    /// </summary>
    internal static class XboxFontSupport
    {
        /// <summary>Xbox 360 font texture format magic (mD3DFormat field).</summary>
        private const uint Xbox360FontFormat = 0x20015400;

        /// <summary>Marker that immediately precedes the (4-zero, dataSize, data) trailer.</summary>
        private static readonly byte[] FloatPairMarker = { 0x00, 0x00, 0x80, 0x3F, 0x00, 0x00, 0x80, 0x3F };

        internal sealed class XboxGlyph
        {
            public int TexNum;
            public float XStart, XEnd, YStart, YEnd;
            public float CharWidth, CharHeight;
        }

        internal sealed class XboxTexturePage
        {
            public int Width;
            public int Height;
            public int DataOffset;     // offset in the original file where DXT5 payload starts
            public int DataSize;       // size in bytes of the DXT5 payload
            public byte[] Argb;         // decoded preview, A,R,G,B byte order
        }

        internal sealed class XboxFontData
        {
            public byte[] OriginalBytes;
            public string FontName;
            public float BaseSize;
            public bool HasFlags;
            public bool HasScaleValue;
            public bool HasOneFloatValue;
            public float OneValue;
            public float HalfValue;
            public int TexCount;
            public int CharCount;
            public int BlockCoordSize;
            public int BlockTexSize;
            public int GlyphTableOffset;     // start of CharCount * 28-byte glyph entries
            public List<XboxTexturePage> Pages = new List<XboxTexturePage>();
            public List<XboxGlyph> Glyphs;

            // Convenience accessors that target page 0 (the only page for most fonts).
            public int TextureWidth { get { return Pages.Count > 0 ? Pages[0].Width : 0; } }
            public int TextureHeight { get { return Pages.Count > 0 ? Pages[0].Height : 0; } }
            public int TextureDataOffset { get { return Pages.Count > 0 ? Pages[0].DataOffset : 0; } }
            public int TextureDataSize { get { return Pages.Count > 0 ? Pages[0].DataSize : 0; } }
            public byte[] TextureArgb { get { return Pages.Count > 0 ? Pages[0].Argb : null; } }
        }

        internal static bool TryLoadFontForEditor(string fontPath, out XboxFontData data)
        {
            data = null;
            byte[] d;
            try { d = File.ReadAllBytes(fontPath); }
            catch { return false; }

            if (d.Length < 32 || d[0] != 'E' || d[1] != 'R' || d[2] != 'T' || d[3] != 'M')
                return false;

            int o = 4;
            uint classCount = U32(d, ref o);
            if (classCount == 0 || classCount > 64) return false;

            bool hasFlags = false;
            bool hasScaleValue = false;
            bool hasT3Texture = false;
            for (uint i = 0; i < classCount; i++)
            {
                if (o + 12 > d.Length) return false;
                ulong crc = BitConverter.ToUInt64(d, o);
                o += 12;
                if (crc == 0x9A3A4A9E63375381UL) hasT3Texture = true;
                else if (crc == 0x84283CB979D71641UL) hasFlags = true;
                else if (crc == 0x937F5D487A0988E3UL) hasScaleValue = true;
            }

            if (!hasT3Texture) return false;

            // mSamplerState block — size includes the 4-byte size header.
            if (o + 4 > d.Length) return false;
            int samplerBlockSize = (int)U32(d, ref o);
            if (samplerBlockSize < 8 || o + samplerBlockSize - 4 > d.Length) return false;
            int samplerEnd = o + samplerBlockSize - 4;

            // Inside the sampler block: [u32 nameLen][nameLen bytes font name].
            int nameLen = (int)U32(d, ref o);
            if (nameLen < 0 || o + nameLen > samplerEnd) return false;
            string fontName = Encoding.ASCII.GetString(d, o, nameLen);
            o = samplerEnd;

            if (o + 1 + 4 > d.Length) return false;
            byte one = d[o++];
            float baseSize = ReadF32(d, ref o);

            float halfValue = 0f;
            if (o + 4 <= d.Length)
            {
                float maybeHalf = BitConverter.ToSingle(d, o);
                if (maybeHalf == 0.5f || maybeHalf == 1.0f)
                {
                    halfValue = maybeHalf;
                    o += 4;
                }
            }

            bool hasOneFloatValue = false;
            float oneValue = 0f;
            if (hasScaleValue)
            {
                if (o + 8 > d.Length) return false;
                int bcsCheck = (int)U32(d, o);
                int ccCheck = (int)U32(d, o + 4);
                if (ccCheck * 48 + 8 != bcsCheck)
                {
                    hasOneFloatValue = true;
                    oneValue = BitConverter.ToSingle(d, o);
                    o += 4;
                }
            }

            // blockSize path (assumed true for ERTM Telltale fonts).
            if (o + 8 > d.Length) return false;
            int blockCoordSize = (int)U32(d, ref o);
            int charCount = (int)U32(d, ref o);

            if (charCount < 0 || charCount > 4096) return false;
            // Glyph entries: 28 bytes each (TexNum + 6 floats), no NewFormat path.
            if (o + charCount * 28 > d.Length) return false;

            int glyphTableOffset = o;
            var glyphs = new List<XboxGlyph>(charCount);
            for (int i = 0; i < charCount; i++)
            {
                glyphs.Add(new XboxGlyph
                {
                    TexNum     = BitConverter.ToInt32(d, o),
                    XStart     = BitConverter.ToSingle(d, o + 4),
                    XEnd       = BitConverter.ToSingle(d, o + 8),
                    YStart     = BitConverter.ToSingle(d, o + 12),
                    YEnd       = BitConverter.ToSingle(d, o + 16),
                    CharWidth  = BitConverter.ToSingle(d, o + 20),
                    CharHeight = BitConverter.ToSingle(d, o + 24),
                });
                o += 28;
            }

            if (o + 8 > d.Length) return false;
            int blockTexSize = (int)U32(d, ref o);
            int texCount = (int)U32(d, ref o);
            if (texCount < 1 || texCount > 8) return false;

            // For each texture page, find a (format magic, marker) pair and
            // parse the trailing [4 zeros][size][data] block.
            var pages = new List<XboxTexturePage>(texCount);
            int searchFrom = o;
            for (int pageIdx = 0; pageIdx < texCount; pageIdx++)
            {
                int fmtIdx = FindFormatMagic(d, searchFrom);
                if (fmtIdx < 0) return false;

                int markerIdx = IndexOf(d, FloatPairMarker, fmtIdx + 4);
                if (markerIdx < 0) return false;

                int sizeFieldOff = markerIdx + 8 + 4; // marker + 4 zero bytes
                if (sizeFieldOff + 4 > d.Length) return false;
                int dataSize = (int)U32(d, sizeFieldOff);
                int dataOff = sizeFieldOff + 4;
                if (dataOff + dataSize > d.Length) return false;

                // Derive width from the first non-degenerate glyph mapped to
                // this page (TexNum == pageIdx). Falls back to 512 if no
                // glyph references this page (shouldn't happen in practice).
                int width = 512;
                foreach (var g in glyphs)
                {
                    if (g.TexNum != pageIdx) continue;
                    float dx = g.XEnd - g.XStart;
                    if (dx > 0.0001f && g.CharWidth > 0)
                    {
                        int w = (int)Math.Round(g.CharWidth / dx);
                        if (w >= 32 && w <= 4096)
                        {
                            width = ((w + 15) / 32) * 32;
                            break;
                        }
                    }
                }
                if (width <= 0 || dataSize % width != 0) return false;
                int height = dataSize / width;

                byte[] tiledDxt = new byte[dataSize];
                Array.Copy(d, dataOff, tiledDxt, 0, dataSize);
                byte[] argb;
                try
                {
                    using (var bmp = XboxTexture.DecodeTiled(tiledDxt, width, height, "DXT5", 16))
                    {
                        argb = BitmapToBgra(bmp);
                    }
                }
                catch
                {
                    return false;
                }

                pages.Add(new XboxTexturePage
                {
                    Width      = width,
                    Height     = height,
                    DataOffset = dataOff,
                    DataSize   = dataSize,
                    Argb       = argb,
                });

                // Continue searching after the data we just consumed.
                searchFrom = dataOff + dataSize;
            }

            data = new XboxFontData
            {
                OriginalBytes      = d,
                FontName           = fontName,
                BaseSize           = baseSize,
                HasFlags           = hasFlags,
                HasScaleValue      = hasScaleValue,
                HasOneFloatValue   = hasOneFloatValue,
                OneValue           = oneValue,
                HalfValue          = halfValue,
                TexCount           = texCount,
                CharCount          = charCount,
                BlockCoordSize     = blockCoordSize,
                BlockTexSize       = blockTexSize,
                GlyphTableOffset   = glyphTableOffset,
                Pages              = pages,
                Glyphs             = glyphs,
            };
            return true;
        }

        /// <summary>
        /// Re-encode page <paramref name="pageIndex"/> from a new ARGB atlas
        /// and patch the corresponding payload region in <paramref name="output"/>
        /// (which must already contain the original file bytes). The atlas is
        /// DXT5-encoded, tiled and byte-swapped using the same pipeline as
        /// the .d3dtx reinserter.
        /// </summary>
        /// <param name="argb">A,R,G,B byte order, length w*h*4.</param>
        private static void EncodePageInto(XboxFontData font, byte[] output,
            int pageIndex, byte[] argb, int w, int h)
        {
            var page = font.Pages[pageIndex];
            if (w != page.Width || h != page.Height)
                throw new InvalidDataException("Page " + pageIndex + " must be "
                    + page.Width + "x" + page.Height + " (was " + w + "x" + h + ").");
            if (argb == null || argb.Length < w * h * 4)
                throw new InvalidDataException("Page " + pageIndex + " buffer is too small.");

            byte[] rgba = new byte[w * h * 4];
            for (int i = 0; i < w * h; i++)
            {
                rgba[i * 4]     = argb[i * 4 + 1]; // R
                rgba[i * 4 + 1] = argb[i * 4 + 2]; // G
                rgba[i * 4 + 2] = argb[i * 4 + 3]; // B
                rgba[i * 4 + 3] = argb[i * 4];     // A
            }

            byte[] newPayload = XboxTexture.BuildBlockBuffer(rgba, w, h, "DXT5", 16);
            // BuildBlockBuffer emits the full mip chain; Walking Dead font
            // atlases store only the top mip - trim to the original size.
            if (newPayload.Length > page.DataSize)
            {
                byte[] trimmed = new byte[page.DataSize];
                Array.Copy(newPayload, 0, trimmed, 0, page.DataSize);
                newPayload = trimmed;
            }
            Array.Copy(newPayload, 0, output, page.DataOffset,
                       Math.Min(newPayload.Length, page.DataSize));
        }

        /// <summary>
        /// FontEditor save flow:
        ///  1) Write the original .font bytes to <paramref name="outputPath"/>.
        ///  2) Patch the glyph table from <paramref name="editedGlyphs"/> (in
        ///     normalised 0..1 atlas coordinates). Pass null to skip.
        ///  3) If <paramref name="importedAtlasArgb"/> is non-null, re-encode
        ///     it as DXT5 (tiled + byte-swapped) and replace the texture
        ///     payload.
        /// </summary>
        internal static bool TrySaveFontForEditor(XboxFontData font, string outputPath,
            IList<XboxGlyph> editedGlyphs, IDictionary<int, ImportedPage> importedPages,
            out string error)
        {
            error = null;
            if (font == null || font.OriginalBytes == null)
            {
                error = "No source font loaded.";
                return false;
            }

            try
            {
                byte[] output = new byte[font.OriginalBytes.Length];
                Array.Copy(font.OriginalBytes, 0, output, 0, output.Length);

                if (editedGlyphs != null && editedGlyphs.Count > 0)
                {
                    int n = Math.Min(editedGlyphs.Count, font.CharCount);
                    int gOff = font.GlyphTableOffset;
                    for (int i = 0; i < n; i++)
                    {
                        var g = editedGlyphs[i];
                        int off = gOff + i * 28;
                        WriteI32(output, off,      g.TexNum);
                        WriteF32(output, off + 4,  g.XStart);
                        WriteF32(output, off + 8,  g.XEnd);
                        WriteF32(output, off + 12, g.YStart);
                        WriteF32(output, off + 16, g.YEnd);
                        WriteF32(output, off + 20, g.CharWidth);
                        WriteF32(output, off + 24, g.CharHeight);
                    }
                }

                if (importedPages != null)
                {
                    foreach (var kv in importedPages)
                    {
                        int idx = kv.Key;
                        if (idx < 0 || idx >= font.Pages.Count) continue;
                        var p = kv.Value;
                        if (p == null || p.Argb == null) continue;
                        EncodePageInto(font, output, idx, p.Argb, p.Width, p.Height);
                    }
                }

                File.WriteAllBytes(outputPath, output);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        /// <summary>Holds a freshly imported atlas for one texture page, used by <see cref="TrySaveFontForEditor"/>.</summary>
        internal sealed class ImportedPage
        {
            public byte[] Argb;
            public int Width;
            public int Height;
        }

        private static void WriteI32(byte[] d, int o, int v)
        {
            d[o]     = (byte)(v & 0xFF);
            d[o + 1] = (byte)((v >> 8) & 0xFF);
            d[o + 2] = (byte)((v >> 16) & 0xFF);
            d[o + 3] = (byte)((v >> 24) & 0xFF);
        }

        private static void WriteF32(byte[] d, int o, float v)
        {
            byte[] b = BitConverter.GetBytes(v);
            d[o]     = b[0];
            d[o + 1] = b[1];
            d[o + 2] = b[2];
            d[o + 3] = b[3];
        }

        // -----------------------------------------------------------------
        private static int FindFormatMagic(byte[] d, int from)
        {
            // Scan for mD3DFormat = 0x20015400 (LE: 00 54 01 20).
            byte b0 = (byte)(Xbox360FontFormat & 0xFF);
            byte b1 = (byte)((Xbox360FontFormat >> 8) & 0xFF);
            byte b2 = (byte)((Xbox360FontFormat >> 16) & 0xFF);
            byte b3 = (byte)((Xbox360FontFormat >> 24) & 0xFF);
            for (int i = from; i + 4 <= d.Length; i++)
                if (d[i] == b0 && d[i + 1] == b1 && d[i + 2] == b2 && d[i + 3] == b3)
                    return i;
            return -1;
        }

        private static int IndexOf(byte[] data, byte[] pattern, int from)
        {
            for (int i = from; i + pattern.Length <= data.Length; i++)
            {
                bool ok = true;
                for (int k = 0; k < pattern.Length; k++)
                    if (data[i + k] != pattern[k]) { ok = false; break; }
                if (ok) return i;
            }
            return -1;
        }

        private static uint U32(byte[] d, int o)
        {
            return (uint)(d[o] | d[o + 1] << 8 | d[o + 2] << 16 | d[o + 3] << 24);
        }

        private static uint U32(byte[] d, ref int o)
        {
            uint v = U32(d, o);
            o += 4;
            return v;
        }

        private static float ReadF32(byte[] d, ref int o)
        {
            float v = BitConverter.ToSingle(d, o);
            o += 4;
            return v;
        }

        /// <summary>
        /// Returns the decoded atlas as A,R,G,B-ordered bytes (the convention
        /// the FontEditor expects in <see cref="ClassesStructs.TextureClass.OldT3Texture"/>.Content
        /// and which matches the Wii/PS2 helpers). Full RGBA is preserved so
        /// the texture can be round-tripped to TGA/PNG and back without loss.
        /// </summary>
        private static byte[] BitmapToBgra(System.Drawing.Bitmap bmp)
        {
            int w = bmp.Width, h = bmp.Height;
            var rect = new System.Drawing.Rectangle(0, 0, w, h);
            var bd = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            byte[] bgra = new byte[w * h * 4];
            System.Runtime.InteropServices.Marshal.Copy(bd.Scan0, bgra, 0, bgra.Length);
            bmp.UnlockBits(bd);

            // 32bppArgb memory layout is B,G,R,A. Convert to A,R,G,B byte order.
            byte[] argb = new byte[bgra.Length];
            for (int i = 0; i < w * h; i++)
            {
                argb[i * 4]     = bgra[i * 4 + 3]; // A
                argb[i * 4 + 1] = bgra[i * 4 + 2]; // R
                argb[i * 4 + 2] = bgra[i * 4 + 1]; // G
                argb[i * 4 + 3] = bgra[i * 4];     // B
            }
            return argb;
        }
    }
}
