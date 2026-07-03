using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using TTG_Tools.ClassesStructs;

namespace TTG_Tools.Graphics.Swizzles
{
    internal static class PS2
    {
        private static readonly Encoding Ascii = Encoding.ASCII;

        internal sealed class TextureRecord
        {
            public string Name;
            public int Offset;
            public int PayloadOffset;
            public int Width;
            public int Height;
            public uint FormatA;
            public uint FormatB;
            public byte[] HeaderTail;
            public int MipCount;
            public int IndexDataLength;
            public int PaletteOffset;
            public int PayloadLength;
            public byte[] Indices;
            public byte[] MipIndices;
            public byte[] Palette;
            public byte[] Bgra32;
            public byte[] Rgba16;
        }

        internal sealed class Glyph
        {
            public int Index;
            public int TextureIndex;
            public float U0;
            public float U1;
            public float V0;
            public float V1;
            public float Width;
            public float Height;
        }

        internal sealed class FontDocument
        {
            public string FontName;
            public float BaseSize;
            public int GlyphBytes;
            public int GlyphDataOffset;
            public readonly List<Glyph> Glyphs = new List<Glyph>();
        }

        internal sealed class Document
        {
            public string FilePath;
            public string DisplayName;
            public bool IsFont;
            public byte[] OriginalData;
            public FontDocument Font;
            public readonly List<TextureRecord> Textures = new List<TextureRecord>();
        }

        internal static bool TryExtractContainer(string inputPath, string outputDir, out string result)
        {
            result = null;
            Document document;
            if (!TryLoad(inputPath, out document)) return false;

            Directory.CreateDirectory(outputDir);
            string baseName = Path.GetFileNameWithoutExtension(inputPath);

            for (int i = 0; i < document.Textures.Count; i++)
            {
                string suffix = document.Textures.Count == 1 ? string.Empty : "_" + i.ToString();
                string outputPath = Path.Combine(outputDir, baseName + suffix + ".png");
                using (Bitmap bitmap = Decode(document.Textures[i]))
                {
                    bitmap.Save(outputPath, ImageFormat.Png);
                }
            }

            if (document.Font != null)
            {
                ExportFnt(document, Path.Combine(outputDir, baseName + ".fnt"));
            }

            result = "File " + Path.GetFileName(inputPath) + " successfully extracted (PS2/PNG).";
            return true;
        }

        internal static bool TryRepackContainer(string inputPath, string inputDir, string outputDir, out string result)
        {
            result = null;
            Document document;
            if (!TryLoad(inputPath, out document)) return false;

            Dictionary<int, string> replacements = new Dictionary<int, string>();
            for (int i = 0; i < document.Textures.Count; i++)
            {
                string png = FindReplacementPng(inputDir, Path.GetFileNameWithoutExtension(inputPath), document.Textures.Count, i);
                if (!string.IsNullOrEmpty(png)) replacements[i] = png;
            }

            string fntPath = Path.Combine(inputDir, Path.GetFileNameWithoutExtension(inputPath) + ".fnt");
            bool hasFnt = document.Font != null && File.Exists(fntPath);
            if (replacements.Count == 0 && !hasFnt) return false;

            byte[] data = Copy(document.OriginalData);
            if (hasFnt)
            {
                ImportFnt(document, fntPath);
                WriteFontGlyphs(data, document);
            }

            ApplyTextureImports(data, document, replacements);

            Directory.CreateDirectory(outputDir);
            File.WriteAllBytes(Path.Combine(outputDir, Path.GetFileName(inputPath)), data);
            result = "File " + Path.GetFileName(inputPath) + " successfully imported (PS2/PNG).";
            return true;
        }

        internal static bool TryLoadFontForEditor(string fontPath, out Document document)
        {
            document = null;
            Document loaded;
            if (!TryLoad(fontPath, out loaded)) return false;
            if (!loaded.IsFont || loaded.Font == null) return false;
            document = loaded;
            return true;
        }

        internal static byte[] DecodeTextureAsArgb(TextureRecord texture, out int width, out int height)
        {
            width = texture.Width;
            height = texture.Height;

            using (Bitmap bitmap = Decode(texture))
            {
                Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
                BitmapData bits = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                try
                {
                    byte[] bgra = new byte[bits.Stride * bits.Height];
                    Marshal.Copy(bits.Scan0, bgra, 0, bgra.Length);

                    byte[] argb = new byte[bitmap.Width * bitmap.Height * 4];
                    for (int y = 0; y < bitmap.Height; y++)
                    {
                        int srcRow = y * bits.Stride;
                        for (int x = 0; x < bitmap.Width; x++)
                        {
                            int src = srcRow + (x * 4);
                            int dst = ((y * bitmap.Width) + x) * 4;
                            argb[dst] = bgra[src + 3];
                            argb[dst + 1] = bgra[src + 2];
                            argb[dst + 2] = bgra[src + 1];
                            argb[dst + 3] = bgra[src];
                        }
                    }

                    return argb;
                }
                finally
                {
                    bitmap.UnlockBits(bits);
                }
            }
        }

        internal static bool SaveFontForEditor(Document document, FontClass.ClassFont font, string outputPath, IDictionary<int, string> texturePaths, out string error)
        {
            error = null;
            try
            {
                if (document == null || document.Font == null) throw new InvalidDataException("No PS2 font is loaded.");
                byte[] data = Copy(document.OriginalData);

                for (int i = 0; i < font.glyph.CharCount && i < document.Font.Glyphs.Count; i++)
                {
                    TextureRecord texture = document.Textures[Math.Max(0, Math.Min(font.glyph.chars[i].TexNum, document.Textures.Count - 1))];
                    Glyph glyph = document.Font.Glyphs[i];
                    glyph.TextureIndex = font.glyph.chars[i].TexNum;
                    glyph.U0 = texture.Width == 0 ? 0 : font.glyph.chars[i].XStart / texture.Width;
                    glyph.U1 = texture.Width == 0 ? 0 : font.glyph.chars[i].XEnd / texture.Width;
                    glyph.V0 = texture.Height == 0 ? 0 : font.glyph.chars[i].YStart / texture.Height;
                    glyph.V1 = texture.Height == 0 ? 0 : font.glyph.chars[i].YEnd / texture.Height;
                    if (document.Font.GlyphBytes == 28)
                    {
                        glyph.Width = font.glyph.chars[i].CharWidth;
                        glyph.Height = font.glyph.chars[i].CharHeight;
                    }
                }

                WriteFontGlyphs(data, document);
                ApplyTextureImports(data, document, texturePaths);
                File.WriteAllBytes(outputPath, data);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        internal static bool TryLoad(string path, out Document document)
        {
            document = null;
            byte[] data;
            try
            {
                data = File.ReadAllBytes(path);
            }
            catch
            {
                return false;
            }

            if (!HasMagic(data, "BMS3")) return false;

            document = new Document();
            document.FilePath = path;
            document.DisplayName = Path.GetFileName(path);
            document.IsFont = string.Equals(Path.GetExtension(path), ".font", StringComparison.OrdinalIgnoreCase);
            document.OriginalData = data;

            if (document.IsFont)
            {
                TryParseFont(data, document);
            }

            foreach (TextureRecord texture in FindTextureRecords(data, document.IsFont ? 0x20 : 0x0C))
            {
                document.Textures.Add(texture);
            }

            if (document.Textures.Count == 0)
            {
                document = null;
                return false;
            }

            return true;
        }

        private static bool TryParseFont(byte[] data, Document document)
        {
            int infoId = FindBytes(data, Ascii.GetBytes("S.0!"), 0x0C);
            if (infoId < 0 || infoId + 12 >= data.Length) return false;

            int infoSize = ReadInt32(data, infoId + 8);
            int pos = infoId + 12;
            int infoEnd = pos + infoSize;
            if (infoEnd > data.Length) return false;

            string name;
            if (!TryReadSizedString(data, ref pos, infoEnd, out name)) return false;
            if (pos >= infoEnd) return false;

            byte one = data[pos++];
            if (one != 0x30 && one != 0x31) return false;
            if (pos + 4 > data.Length) return false;

            FontDocument font = new FontDocument();
            font.FontName = name;
            font.BaseSize = ReadSingle(data, pos);
            pos += 4;

            if (pos + 8 > data.Length) return false;
            int blockCoordSize = ReadInt32(data, pos);
            int charCount = ReadInt32(data, pos + 4);
            if (charCount <= 0 || charCount > 65536) return false;
            int glyphBytes = (blockCoordSize - 8) / charCount;
            if ((glyphBytes != 20 && glyphBytes != 28) || blockCoordSize != (charCount * glyphBytes) + 8) return false;

            pos += 8;
            if (pos + (charCount * glyphBytes) > data.Length) return false;
            font.GlyphBytes = glyphBytes;
            font.GlyphDataOffset = pos;

            for (int i = 0; i < charCount; i++)
            {
                Glyph glyph = new Glyph();
                glyph.Index = i;
                glyph.TextureIndex = ReadInt32(data, pos);
                glyph.U0 = ReadSingle(data, pos + 4);
                glyph.U1 = ReadSingle(data, pos + 8);
                glyph.V0 = ReadSingle(data, pos + 12);
                glyph.V1 = ReadSingle(data, pos + 16);
                if (glyphBytes == 28)
                {
                    glyph.Width = ReadSingle(data, pos + 20);
                    glyph.Height = ReadSingle(data, pos + 24);
                }
                font.Glyphs.Add(glyph);
                pos += glyphBytes;
            }

            document.Font = font;
            return true;
        }

        private static IEnumerable<TextureRecord> FindTextureRecords(byte[] data, int start)
        {
            List<int> seen = new List<int>();
            for (int pos = Math.Max(0, start); pos < data.Length - 64; pos++)
            {
                TextureRecord texture;
                if (TryReadTextureRecord(data, pos, out texture))
                {
                    bool duplicate = false;
                    foreach (int old in seen)
                    {
                        if (Math.Abs(old - texture.Offset) < 8)
                        {
                            duplicate = true;
                            break;
                        }
                    }

                    if (!duplicate)
                    {
                        seen.Add(texture.Offset);
                        yield return texture;
                        pos = texture.PayloadOffset + texture.PayloadLength - 1;
                    }
                }
            }
        }

        private static bool TryReadTextureRecord(byte[] data, int offset, out TextureRecord texture)
        {
            texture = null;
            int pos = offset;
            string name;
            if (!TryReadSizedString(data, ref pos, data.Length, out name)) return false;
            if (string.IsNullOrEmpty(name) || name.IndexOf(".d3dtx", StringComparison.OrdinalIgnoreCase) < 0) return false;

            if (pos + 29 > data.Length) return false;
            string flags = Ascii.GetString(data, pos, 4);
            if (!IsBinaryFlagString(flags)) return false;
            pos += 4;

            int width = ReadInt32(data, pos);
            int height = ReadInt32(data, pos + 4);
            uint formatA = ReadUInt32(data, pos + 8);
            uint formatB = ReadUInt32(data, pos + 16);
            byte[] headerTail = CopyRange(data, pos + 20, 5);

            if (width <= 0 || height <= 0 || width > 4096 || height > 4096) return false;

            int payloadOffset = pos + 25;
            bool isClut8 = formatA == 0x13 || formatB == 0x13;
            bool isBgra32 = formatA == 0 && formatB == 0;
            bool isRgba16 = formatA == 0x02 || formatB == 0x02;
            if (!isClut8 && !isBgra32 && !isRgba16) return false;

            int mipCount = isClut8 ? Math.Max(1, (int)headerTail[4]) : 1;
            long indexLength = isClut8 ? (long)width * height : 0;
            long indexDataLength = isClut8 ? CalculateMipIndexLength(width, height, mipCount) : 0;
            long paletteLength = isClut8 ? 256 * 4 : 0;
            long bgraLength = isBgra32 ? (long)width * height * 4 : 0;
            long rgba16Length = isRgba16 ? (long)width * height * 2 : 0;
            long payloadLength = indexDataLength + paletteLength + bgraLength + rgba16Length;
            if (payloadOffset < 0 || payloadOffset + payloadLength > data.Length) return false;

            texture = new TextureRecord();
            texture.Name = name;
            texture.Offset = offset;
            texture.PayloadOffset = payloadOffset;
            texture.Width = width;
            texture.Height = height;
            texture.FormatA = formatA;
            texture.FormatB = formatB;
            texture.HeaderTail = headerTail;
            texture.MipCount = mipCount;
            texture.IndexDataLength = (int)indexDataLength;
            texture.PayloadLength = (int)payloadLength;
            if (isClut8)
            {
                texture.Indices = CopyRange(data, payloadOffset, (int)indexLength);
                texture.MipIndices = CopyRange(data, payloadOffset + (int)indexLength, (int)(indexDataLength - indexLength));
                texture.PaletteOffset = payloadOffset + (int)indexDataLength;
                texture.Palette = CopyRange(data, texture.PaletteOffset, (int)paletteLength);
            }
            else
            {
                texture.PaletteOffset = -1;
                if (isBgra32) texture.Bgra32 = CopyRange(data, payloadOffset, (int)bgraLength);
                else texture.Rgba16 = CopyRange(data, payloadOffset, (int)rgba16Length);
            }
            return true;
        }

        private static Bitmap Decode(TextureRecord texture)
        {
            if (texture.Bgra32 != null) return DecodeBgra32(texture);
            if (texture.Rgba16 != null) return DecodeRgba16(texture);
            if (texture.Indices == null || texture.Palette == null) throw new InvalidOperationException("Texture has no supported PS2 payload.");

            Bitmap bitmap = new Bitmap(texture.Width, texture.Height, PixelFormat.Format32bppArgb);
            BitmapData bits = bitmap.LockBits(new Rectangle(0, 0, texture.Width, texture.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            bool psmt8Layout = UsesPsmt8Layout(texture);
            bool useAlpha = UsesAlpha(texture);

            try
            {
                byte[] output = new byte[bits.Stride * texture.Height];
                for (int y = 0; y < texture.Height; y++)
                {
                    int dstRow = y * bits.Stride;
                    int linearRow = y * texture.Width;
                    for (int x = 0; x < texture.Width; x++)
                    {
                        int src = psmt8Layout ? GetPsmt8Offset(x, y, texture.Width) : linearRow + x;
                        if (src < 0 || src >= texture.Indices.Length) continue;

                        int colorIndex = UnswizzleClutIndex(texture.Indices[src]);
                        int paletteOffset = colorIndex * 4;
                        byte r = texture.Palette[paletteOffset];
                        byte g = texture.Palette[paletteOffset + 1];
                        byte b = texture.Palette[paletteOffset + 2];
                        byte a = useAlpha ? ExpandPs2Alpha(texture.Palette[paletteOffset + 3]) : (byte)255;

                        int dst = dstRow + (x * 4);
                        output[dst] = b;
                        output[dst + 1] = g;
                        output[dst + 2] = r;
                        output[dst + 3] = a;
                    }
                }
                Marshal.Copy(output, 0, bits.Scan0, output.Length);
            }
            finally
            {
                bitmap.UnlockBits(bits);
            }

            return bitmap;
        }

        private static Bitmap DecodeBgra32(TextureRecord texture)
        {
            Bitmap bitmap = new Bitmap(texture.Width, texture.Height, PixelFormat.Format32bppArgb);
            BitmapData bits = bitmap.LockBits(new Rectangle(0, 0, texture.Width, texture.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            bool useAlpha = UsesAlpha(texture);

            try
            {
                byte[] output = new byte[bits.Stride * texture.Height];
                for (int y = 0; y < texture.Height; y++)
                {
                    int dstRow = y * bits.Stride;
                    int srcRow = y * texture.Width * 4;
                    for (int x = 0; x < texture.Width; x++)
                    {
                        int src = srcRow + (x * 4);
                        int dst = dstRow + (x * 4);
                        output[dst] = texture.Bgra32[src];
                        output[dst + 1] = texture.Bgra32[src + 1];
                        output[dst + 2] = texture.Bgra32[src + 2];
                        output[dst + 3] = useAlpha ? ExpandPs2Alpha(texture.Bgra32[src + 3]) : (byte)255;
                    }
                }
                Marshal.Copy(output, 0, bits.Scan0, output.Length);
            }
            finally
            {
                bitmap.UnlockBits(bits);
            }

            return bitmap;
        }

        private static Bitmap DecodeRgba16(TextureRecord texture)
        {
            Bitmap bitmap = new Bitmap(texture.Width, texture.Height, PixelFormat.Format32bppArgb);
            BitmapData bits = bitmap.LockBits(new Rectangle(0, 0, texture.Width, texture.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            bool useAlpha = UsesAlpha(texture);

            try
            {
                byte[] output = new byte[bits.Stride * texture.Height];
                for (int y = 0; y < texture.Height; y++)
                {
                    int dstRow = y * bits.Stride;
                    int srcRow = y * texture.Width * 2;
                    for (int x = 0; x < texture.Width; x++)
                    {
                        int src = srcRow + (x * 2);
                        ushort value = (ushort)(texture.Rgba16[src] | (texture.Rgba16[src + 1] << 8));
                        byte r = Expand5To8(value & 0x1F);
                        byte g = Expand5To8((value >> 5) & 0x1F);
                        byte b = Expand5To8((value >> 10) & 0x1F);
                        byte a = useAlpha && (value & 0x8000) != 0 ? (byte)255 : (byte)0;
                        if (!useAlpha) a = 255;

                        int dst = dstRow + (x * 4);
                        output[dst] = b;
                        output[dst + 1] = g;
                        output[dst + 2] = r;
                        output[dst + 3] = a;
                    }
                }
                Marshal.Copy(output, 0, bits.Scan0, output.Length);
            }
            finally
            {
                bitmap.UnlockBits(bits);
            }

            return bitmap;
        }

        private static void ApplyTextureImports(byte[] data, Document document, IDictionary<int, string> texturePaths)
        {
            if (texturePaths == null || texturePaths.Count == 0) return;

            foreach (var pair in texturePaths)
            {
                int textureIndex = pair.Key;
                if (textureIndex < 0 || textureIndex >= document.Textures.Count) continue;
                if (string.IsNullOrEmpty(pair.Value) || !File.Exists(pair.Value)) continue;

                TextureRecord texture = document.Textures[textureIndex];
                using (Bitmap bitmap = new Bitmap(pair.Value))
                {
                    if (bitmap.Width != texture.Width || bitmap.Height != texture.Height)
                    {
                        throw new InvalidDataException(string.Format("PNG dimensions must be {0}x{1}; received {2}x{3}.", texture.Width, texture.Height, bitmap.Width, bitmap.Height));
                    }

                    byte[] replacement = EncodeTexture(texture, bitmap);
                    Buffer.BlockCopy(replacement, 0, data, texture.PayloadOffset, replacement.Length);
                }
            }
        }

        private static byte[] EncodeTexture(TextureRecord texture, Bitmap bitmap)
        {
            if (texture.Bgra32 != null) return EncodeBgra32(texture, bitmap);
            if (texture.Rgba16 != null) return EncodeRgba16(texture, bitmap);
            if (texture.Indices != null && texture.Palette != null) return EncodeClut8(texture, bitmap);
            throw new InvalidOperationException("Texture has no supported PS2 payload.");
        }

        private static byte[] EncodeClut8(TextureRecord texture, Bitmap bitmap)
        {
            int pixelCount = texture.Width * texture.Height;
            int mipLength = texture.MipIndices != null ? texture.MipIndices.Length : 0;
            byte[] payload = new byte[pixelCount + mipLength + texture.Palette.Length];
            Dictionary<int, byte> cache = new Dictionary<int, byte>();
            bool psmt8Layout = UsesPsmt8Layout(texture);

            using (Bitmap original = Decode(texture))
            {
                for (int y = 0; y < texture.Height; y++)
                {
                    int linearRow = y * texture.Width;
                    for (int x = 0; x < texture.Width; x++)
                    {
                        Color color = bitmap.GetPixel(x, y);
                        int dst = psmt8Layout ? GetPsmt8Offset(x, y, texture.Width) : linearRow + x;
                        if (dst < 0 || dst >= pixelCount) continue;

                        if (color.ToArgb() == original.GetPixel(x, y).ToArgb())
                        {
                            payload[dst] = texture.Indices[dst];
                            continue;
                        }

                        byte paletteIndex;
                        int argb = color.ToArgb();
                        if (!cache.TryGetValue(argb, out paletteIndex))
                        {
                            paletteIndex = FindNearestPaletteIndex(texture.Palette, color, UsesAlpha(texture));
                            cache[argb] = paletteIndex;
                        }

                        payload[dst] = (byte)UnswizzleClutIndex(paletteIndex);
                    }
                }
            }

            if (mipLength > 0) Buffer.BlockCopy(texture.MipIndices, 0, payload, pixelCount, mipLength);
            Buffer.BlockCopy(texture.Palette, 0, payload, pixelCount + mipLength, texture.Palette.Length);
            return payload;
        }

        private static byte[] EncodeBgra32(TextureRecord texture, Bitmap bitmap)
        {
            byte[] payload = new byte[texture.Width * texture.Height * 4];
            using (Bitmap original = Decode(texture))
            {
                for (int y = 0; y < texture.Height; y++)
                {
                    int row = y * texture.Width * 4;
                    for (int x = 0; x < texture.Width; x++)
                    {
                        Color color = bitmap.GetPixel(x, y);
                        int offset = row + (x * 4);
                        if (color.ToArgb() == original.GetPixel(x, y).ToArgb())
                        {
                            payload[offset] = texture.Bgra32[offset];
                            payload[offset + 1] = texture.Bgra32[offset + 1];
                            payload[offset + 2] = texture.Bgra32[offset + 2];
                            payload[offset + 3] = texture.Bgra32[offset + 3];
                            continue;
                        }

                        payload[offset] = color.B;
                        payload[offset + 1] = color.G;
                        payload[offset + 2] = color.R;
                        payload[offset + 3] = CompressPs2Alpha(color.A);
                    }
                }
            }
            return payload;
        }

        private static byte[] EncodeRgba16(TextureRecord texture, Bitmap bitmap)
        {
            byte[] payload = new byte[texture.Width * texture.Height * 2];
            using (Bitmap original = Decode(texture))
            {
                for (int y = 0; y < texture.Height; y++)
                {
                    int row = y * texture.Width * 2;
                    for (int x = 0; x < texture.Width; x++)
                    {
                        Color color = bitmap.GetPixel(x, y);
                        int offset = row + (x * 2);
                        if (color.ToArgb() == original.GetPixel(x, y).ToArgb())
                        {
                            payload[offset] = texture.Rgba16[offset];
                            payload[offset + 1] = texture.Rgba16[offset + 1];
                            continue;
                        }

                        ushort value = 0;
                        value |= (ushort)(color.R >> 3);
                        value |= (ushort)((color.G >> 3) << 5);
                        value |= (ushort)((color.B >> 3) << 10);
                        if (color.A >= 128) value |= 0x8000;
                        payload[offset] = (byte)(value & 0xFF);
                        payload[offset + 1] = (byte)(value >> 8);
                    }
                }
            }
            return payload;
        }

        private static void WriteFontGlyphs(byte[] data, Document document)
        {
            if (document.Font == null) return;

            int pos = document.Font.GlyphDataOffset;
            foreach (Glyph glyph in document.Font.Glyphs)
            {
                WriteInt32(data, pos, glyph.TextureIndex);
                WriteSingle(data, pos + 4, glyph.U0);
                WriteSingle(data, pos + 8, glyph.U1);
                WriteSingle(data, pos + 12, glyph.V0);
                WriteSingle(data, pos + 16, glyph.V1);
                if (document.Font.GlyphBytes == 28)
                {
                    WriteSingle(data, pos + 20, glyph.Width);
                    WriteSingle(data, pos + 24, glyph.Height);
                }
                pos += document.Font.GlyphBytes;
            }
        }

        private static void ExportFnt(Document document, string path)
        {
            if (document.Font == null) return;
            string textureBaseName = Path.GetFileNameWithoutExtension(path);
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("info face=\"{0}\" size={1} bold=0 italic=0 charset=\"\" unicode=0 smooth=0 aa=0 padding=0,0,0,0 spacing=0,0 outline=0", document.Font.FontName, (int)document.Font.BaseSize);
            sb.AppendLine();
            TextureRecord first = document.Textures.Count > 0 ? document.Textures[0] : null;
            int width = first != null ? first.Width : 0;
            int height = first != null ? first.Height : 0;
            sb.AppendFormat("common lineHeight={0} base={0} scaleW={1} scaleH={2} pages={3} packed=0 alphaChnl=0 redChnl=0 greenChnl=0 blueChnl=0", (int)document.Font.BaseSize, width, height, document.Textures.Count);
            sb.AppendLine();
            for (int i = 0; i < document.Textures.Count; i++)
            {
                string pageFile = document.Textures.Count == 1 ? textureBaseName + ".png" : textureBaseName + "_" + i + ".png";
                sb.AppendFormat("page id={0} file=\"{1}\"", i, pageFile);
                sb.AppendLine();
            }
            sb.AppendFormat("chars count={0}", document.Font.Glyphs.Count);
            sb.AppendLine();

            foreach (Glyph glyph in document.Font.Glyphs)
            {
                TextureRecord texture = document.Textures[Math.Max(0, Math.Min(glyph.TextureIndex, document.Textures.Count - 1))];
                float x = glyph.U0 * texture.Width;
                float y = glyph.V0 * texture.Height;
                float glyphWidth = (glyph.U1 - glyph.U0) * texture.Width;
                float glyphHeight = (glyph.V1 - glyph.V0) * texture.Height;
                int widthOut = document.Font.GlyphBytes == 28 ? (int)Math.Round(glyph.Width) : (int)Math.Round(glyphWidth);
                int heightOut = document.Font.GlyphBytes == 28 ? (int)Math.Round(glyph.Height) : (int)Math.Round(glyphHeight);
                sb.AppendFormat("char id={0,-5} x={1,-5} y={2,-5} width={3,-5} height={4,-5} xoffset=0    yoffset=0    xadvance={5,-5} page={6,-3} chnl=15", glyph.Index, (int)Math.Round(x), (int)Math.Round(y), widthOut, heightOut, widthOut, glyph.TextureIndex);
                sb.AppendLine();
            }

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }

        private static void ImportFnt(Document document, string path)
        {
            Dictionary<int, Glyph> map = new Dictionary<int, Glyph>();
            foreach (string line in File.ReadLines(path))
            {
                string trimmed = line.Trim();
                if (!trimmed.StartsWith("char ")) continue;

                Dictionary<string, string> data = ParseFntPairs(trimmed);
                int id = ParseInt(data["id"]);
                int page = ParseInt(data["page"]);
                if (page < 0 || page >= document.Textures.Count) continue;
                TextureRecord texture = document.Textures[page];
                float x = ParseFloat(data["x"]);
                float y = ParseFloat(data["y"]);
                float width = ParseFloat(data["width"]);
                float height = ParseFloat(data["height"]);

                Glyph glyph = new Glyph();
                glyph.Index = id;
                glyph.TextureIndex = page;
                glyph.U0 = texture.Width == 0 ? 0 : x / texture.Width;
                glyph.U1 = texture.Width == 0 ? 0 : (x + width) / texture.Width;
                glyph.V0 = texture.Height == 0 ? 0 : y / texture.Height;
                glyph.V1 = texture.Height == 0 ? 0 : (y + height) / texture.Height;
                glyph.Width = width;
                glyph.Height = height;
                map[id] = glyph;
            }

            for (int i = 0; i < document.Font.Glyphs.Count; i++)
            {
                Glyph glyph;
                if (map.TryGetValue(i, out glyph)) document.Font.Glyphs[i] = glyph;
            }
        }

        private static Dictionary<string, string> ParseFntPairs(string line)
        {
            Dictionary<string, string> pairs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string[] tokens = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 1; i < tokens.Length; i++)
            {
                int eq = tokens[i].IndexOf('=');
                if (eq <= 0) continue;
                pairs[tokens[i].Substring(0, eq)] = tokens[i].Substring(eq + 1).Trim('"');
            }
            return pairs;
        }

        private static string FindReplacementPng(string directory, string baseName, int textureCount, int textureIndex)
        {
            if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(baseName)) return null;
            if (textureCount == 1)
            {
                string direct = Path.Combine(directory, baseName + ".png");
                if (File.Exists(direct)) return direct;
                string indexed = Path.Combine(directory, baseName + "_0.png");
                return File.Exists(indexed) ? indexed : null;
            }

            string path = Path.Combine(directory, baseName + "_" + textureIndex + ".png");
            return File.Exists(path) ? path : null;
        }

        private static bool UsesPsmt8Layout(TextureRecord texture)
        {
            return texture.Indices != null && texture.FormatB != 0x13;
        }

        private static bool UsesAlpha(TextureRecord texture)
        {
            if (texture.HeaderTail == null || texture.HeaderTail.Length == 0) return true;
            return texture.HeaderTail[0] != 0x30 && HasNonZeroAlpha(texture);
        }

        private static bool HasNonZeroAlpha(TextureRecord texture)
        {
            if (texture.Palette != null && texture.Indices != null)
            {
                bool[] used = new bool[256];
                foreach (byte index in texture.Indices)
                {
                    used[UnswizzleClutIndex(index)] = true;
                }

                for (int i = 0; i < used.Length; i++)
                {
                    if (used[i] && texture.Palette[(i * 4) + 3] != 0) return true;
                }
                return false;
            }

            if (texture.Bgra32 != null)
            {
                for (int i = 3; i < texture.Bgra32.Length; i += 4)
                {
                    if (texture.Bgra32[i] != 0) return true;
                }
                return false;
            }

            if (texture.Rgba16 != null)
            {
                for (int i = 0; i + 1 < texture.Rgba16.Length; i += 2)
                {
                    ushort value = (ushort)(texture.Rgba16[i] | (texture.Rgba16[i + 1] << 8));
                    if ((value & 0x8000) != 0) return true;
                }
                return false;
            }

            return true;
        }

        private static int UnswizzleClutIndex(int index)
        {
            return (index & 0xE7) | ((index & 0x08) << 1) | ((index & 0x10) >> 1);
        }

        private static int GetPsmt8Offset(int x, int y, int width)
        {
            int blockLocation = (y & ~0x0F) * width + (x & ~0x0F) * 2;
            int swapSelector = (((y + 2) >> 2) & 1) * 4;
            int posY = (((y & ~3) >> 1) + (y & 1)) & 7;
            int columnLocation = (posY * width * 2) + (((x + swapSelector) & 7) * 4);
            int byteNumber = ((y >> 1) & 1) + ((x >> 2) & 2);
            return blockLocation + columnLocation + byteNumber;
        }

        private static byte FindNearestPaletteIndex(byte[] palette, Color color, bool useAlpha)
        {
            int bestIndex = 0;
            int bestScore = int.MaxValue;

            for (int i = 0; i < 256; i++)
            {
                int offset = i * 4;
                int r = palette[offset];
                int g = palette[offset + 1];
                int b = palette[offset + 2];
                int a = ExpandPs2Alpha(palette[offset + 3]);
                int dr = color.R - r;
                int dg = color.G - g;
                int db = color.B - b;
                int da = useAlpha ? color.A - a : 0;
                int score = (dr * dr) + (dg * dg) + (db * db) + (da * da * 2);
                if (score < bestScore)
                {
                    bestScore = score;
                    bestIndex = i;
                    if (score == 0) break;
                }
            }

            return (byte)bestIndex;
        }

        private static long CalculateMipIndexLength(int width, int height, int mipCount)
        {
            long total = 0;
            int mipWidth = width;
            int mipHeight = height;
            for (int i = 0; i < mipCount; i++)
            {
                total += (long)mipWidth * mipHeight;
                mipWidth = Math.Max(1, mipWidth / 2);
                mipHeight = Math.Max(1, mipHeight / 2);
            }
            return total;
        }

        private static bool IsBinaryFlagString(string flags)
        {
            if (flags == null || flags.Length != 4) return false;
            for (int i = 0; i < flags.Length; i++)
            {
                if (flags[i] != '0' && flags[i] != '1') return false;
            }
            return true;
        }

        private static bool TryReadSizedString(byte[] data, ref int pos, int limit, out string value)
        {
            value = null;
            if (pos + 4 > limit) return false;

            int length = ReadInt32(data, pos);
            pos += 4;

            if (pos + 4 <= limit)
            {
                int nestedLength = ReadInt32(data, pos);
                if (length - nestedLength == 8 && nestedLength > 0 && nestedLength < length)
                {
                    length = nestedLength;
                    pos += 4;
                }
            }

            if (length <= 0 || length > 1024 || pos + length > limit) return false;
            for (int i = 0; i < length; i++)
            {
                byte b = data[pos + i];
                if (b < 0x20 || b > 0x7E) return false;
            }

            value = Ascii.GetString(data, pos, length);
            pos += length;
            return true;
        }

        private static bool HasMagic(byte[] data, string magic)
        {
            byte[] bytes = Ascii.GetBytes(magic);
            if (data.Length < bytes.Length) return false;
            for (int i = 0; i < bytes.Length; i++)
            {
                if (data[i] != bytes[i]) return false;
            }
            return true;
        }

        private static int FindBytes(byte[] data, byte[] pattern, int start)
        {
            for (int i = Math.Max(0, start); i <= data.Length - pattern.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (data[i + j] != pattern[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match) return i;
            }
            return -1;
        }

        private static byte Expand5To8(int value)
        {
            return (byte)((value << 3) | (value >> 2));
        }

        private static byte ExpandPs2Alpha(byte alpha)
        {
            int expanded = alpha * 2;
            return (byte)(expanded > 255 ? 255 : expanded);
        }

        private static byte CompressPs2Alpha(byte alpha)
        {
            int compressed = (alpha + 1) / 2;
            return (byte)(compressed > 128 ? 128 : compressed);
        }

        private static int ParseInt(string value)
        {
            int parsed;
            return int.TryParse(value, out parsed) ? parsed : 0;
        }

        private static float ParseFloat(string value)
        {
            float parsed;
            return float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out parsed) ? parsed : 0;
        }

        private static int ReadInt32(byte[] data, int offset) { return BitConverter.ToInt32(data, offset); }
        private static uint ReadUInt32(byte[] data, int offset) { return BitConverter.ToUInt32(data, offset); }
        private static float ReadSingle(byte[] data, int offset) { return BitConverter.ToSingle(data, offset); }
        private static byte[] CopyRange(byte[] data, int offset, int count)
        {
            byte[] result = new byte[count];
            Buffer.BlockCopy(data, offset, result, 0, count);
            return result;
        }

        private static byte[] Copy(byte[] data)
        {
            byte[] copy = new byte[data.Length];
            Buffer.BlockCopy(data, 0, copy, 0, data.Length);
            return copy;
        }

        private static void WriteInt32(byte[] data, int offset, int value)
        {
            Buffer.BlockCopy(BitConverter.GetBytes(value), 0, data, offset, 4);
        }

        private static void WriteSingle(byte[] data, int offset, float value)
        {
            Buffer.BlockCopy(BitConverter.GetBytes(value), 0, data, offset, 4);
        }
    }
}
