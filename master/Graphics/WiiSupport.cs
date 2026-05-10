using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace TTG_Tools.Graphics
{
    internal static class WiiSupport
    {
        internal struct Resolution
        {
            public int Width;
            public int Height;
        }

        private static readonly byte[] HeaderMagic = Encoding.ASCII.GetBytes("ERTM");
        private static readonly byte[] TplMagic = { 0x00, 0x20, 0xAF, 0x30 };
        private static readonly byte[] HashStyleMagic = { 0x81, 0x53, 0x37, 0x63, 0x9E, 0x4A, 0x3A, 0x9A };
        private static readonly byte[] SomeTexDataHash = { 0xE3, 0x88, 0x09, 0x7A, 0x48, 0x5D, 0x7F, 0x93 };

        internal class WiiGlyph
        {
            public int TexNum;
            public float XStart;
            public float XEnd;
            public float YStart;
            public float YEnd;
            public float CharWidth;
            public float CharHeight;
        }

        internal class WiiFontData
        {
            public byte[] SignatureBytes = HeaderMagic;
            public byte[] ElementsDataBytes = new byte[0];
            public byte[] FontNameBlockBytes = new byte[0];
            public byte[] FontMetadataAfterNameBytes = new byte[0];
            public byte[] OptionalOneValueBytes;
            public byte[] BlockCoordSizeValBytes;
            public byte[] CharCountValBytes = new byte[0];
            public byte[] RawGlyphDataBytes = new byte[0];
            public byte[] SuffixDataBytes = new byte[0];
            public string FontName = string.Empty;
            public float BaseSize;
            public bool IsBlockSizeFont;
            public bool HasScaleValue;
            public int CharCount;
            public int TextureWidth;
            public int TextureHeight;
            public int TexCount = 1;
            public readonly List<WiiGlyph> Glyphs = new List<WiiGlyph>();

            public void Parse(string path, int texWidth, int texHeight)
            {
                TextureWidth = texWidth;
                TextureHeight = texHeight;
                bool preliminaryHasScale = false;

                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                using (var br = new BinaryReader(fs))
                {
                    SignatureBytes = br.ReadBytes(4);
                    if (!SignatureBytes.SequenceEqual(HeaderMagic)) throw new InvalidDataException("Not an ERTM file.");

                    byte[] countElementBytes = br.ReadBytes(4);
                    int countElements = BitConverter.ToInt32(countElementBytes, 0);
                    var elementBytes = new List<byte[]> { countElementBytes };
                    var elementContentForCheck = new List<byte[]>();

                    if (countElements > 0)
                    {
                        byte[] peek = br.ReadBytes(8);
                        fs.Seek(-8, SeekOrigin.Current);
                        bool allHash = peek.SequenceEqual(HashStyleMagic);

                        for (int i = 0; i < countElements; i++)
                        {
                            if (allHash)
                            {
                                byte[] hashPart = br.ReadBytes(8);
                                byte[] padPart = br.ReadBytes(4);
                                elementBytes.Add(hashPart);
                                elementBytes.Add(padPart);
                                elementContentForCheck.Add(hashPart);
                            }
                            else
                            {
                                byte[] lenBytes = br.ReadBytes(4);
                                int len = BitConverter.ToInt32(lenBytes, 0);
                                byte[] strBytes = br.ReadBytes(len);
                                byte[] toolBytes = br.ReadBytes(4);
                                elementBytes.Add(lenBytes);
                                elementBytes.Add(strBytes);
                                elementBytes.Add(toolBytes);
                                elementContentForCheck.Add(strBytes);
                            }
                        }
                    }

                    ElementsDataBytes = Concat(elementBytes);
                    preliminaryHasScale = elementContentForCheck.Any(x => x.SequenceEqual(SomeTexDataHash));

                    long fontNameStart = fs.Position;
                    byte[] possibleBlockSizeBytes = br.ReadBytes(4);
                    int possibleBlockSize = BitConverter.ToInt32(possibleBlockSizeBytes, 0);

                    long afterFirst = fs.Position;
                    byte[] maybeNameLenBytes = br.ReadBytes(4);
                    byte[] actualNameLenBytes;

                    if (maybeNameLenBytes.Length < 4)
                    {
                        IsBlockSizeFont = false;
                        actualNameLenBytes = possibleBlockSizeBytes;
                        fs.Seek(afterFirst, SeekOrigin.Begin);
                    }
                    else
                    {
                        int maybeNameLen = BitConverter.ToInt32(maybeNameLenBytes, 0);
                        if (possibleBlockSize > 0 && maybeNameLen >= 0 && possibleBlockSize - maybeNameLen == 8)
                        {
                            IsBlockSizeFont = true;
                            actualNameLenBytes = maybeNameLenBytes;
                        }
                        else
                        {
                            IsBlockSizeFont = false;
                            actualNameLenBytes = possibleBlockSizeBytes;
                            fs.Seek(afterFirst, SeekOrigin.Begin);
                        }
                    }

                    int nameLen = BitConverter.ToInt32(actualNameLenBytes, 0);
                    byte[] nameBytes = br.ReadBytes(nameLen);
                    FontName = Encoding.ASCII.GetString(nameBytes);

                    long nameEnd = fs.Position;
                    fs.Seek(fontNameStart, SeekOrigin.Begin);
                    FontNameBlockBytes = br.ReadBytes((int)(nameEnd - fontNameStart));

                    long metadataStart = fs.Position;
                    br.ReadByte();
                    BaseSize = br.ReadSingle();
                    int consumed = 5;

                    long peekStart = fs.Position;
                    byte[] peekOptional = br.ReadBytes(4);
                    if (peekOptional.Length == 4 && peekOptional.SequenceEqual(new byte[] { 0xCE, 0xFA, 0xED, 0xFE }))
                    {
                        consumed += 4;
                        peekStart = fs.Position;
                        peekOptional = br.ReadBytes(4);
                    }

                    if (peekOptional.Length == 4)
                    {
                        float half = BitConverter.ToSingle(peekOptional, 0);
                        if (Math.Abs(half - 0.5f) < 0.00001f || Math.Abs(half - 1.0f) < 0.00001f) consumed += 4;
                        else fs.Seek(peekStart, SeekOrigin.Begin);
                    }

                    fs.Seek(metadataStart, SeekOrigin.Begin);
                    FontMetadataAfterNameBytes = br.ReadBytes(consumed);
                    fs.Seek(metadataStart + consumed, SeekOrigin.Begin);

                    if (IsBlockSizeFont && preliminaryHasScale)
                    {
                        OptionalOneValueBytes = br.ReadBytes(4);
                        if (OptionalOneValueBytes.Length < 4) OptionalOneValueBytes = null;
                    }

                    int blockCoordSizeParsed = 0;
                    if (IsBlockSizeFont)
                    {
                        BlockCoordSizeValBytes = br.ReadBytes(4);
                        blockCoordSizeParsed = BitConverter.ToInt32(BlockCoordSizeValBytes, 0);
                    }

                    CharCountValBytes = br.ReadBytes(4);
                    CharCount = BitConverter.ToInt32(CharCountValBytes, 0);

                    if (IsBlockSizeFont && CharCount > 0)
                    {
                        float perGlyph = (blockCoordSizeParsed - 8f) / CharCount;
                        HasScaleValue = Math.Abs(perGlyph - 28f) < 0.001f;
                    }
                    else
                    {
                        HasScaleValue = preliminaryHasScale;
                    }

                    int bytesPerGlyph = HasScaleValue ? 28 : 20;
                    int glyphTotal = CharCount * bytesPerGlyph;
                    RawGlyphDataBytes = br.ReadBytes(glyphTotal);

                    using (var ms = new MemoryStream(RawGlyphDataBytes))
                    using (var gbr = new BinaryReader(ms))
                    {
                        Glyphs.Clear();
                        for (int i = 0; i < CharCount; i++)
                        {
                            var g = new WiiGlyph();
                            g.TexNum = gbr.ReadInt32();
                            float x0 = gbr.ReadSingle();
                            float x1 = gbr.ReadSingle();
                            float y0 = gbr.ReadSingle();
                            float y1 = gbr.ReadSingle();
                            g.XStart = (float)Math.Round(x0 * TextureWidth);
                            g.XEnd = (float)Math.Round(x1 * TextureWidth);
                            g.YStart = (float)Math.Round(y0 * TextureHeight);
                            g.YEnd = (float)Math.Round(y1 * TextureHeight);
                            if (HasScaleValue)
                            {
                                g.CharWidth = (float)Math.Round(gbr.ReadSingle());
                                g.CharHeight = (float)Math.Round(gbr.ReadSingle());
                            }
                            Glyphs.Add(g);
                        }
                    }

                    long suffixStart = fs.Position;
                    fs.Seek(0, SeekOrigin.End);
                    long end = fs.Position;
                    fs.Seek(suffixStart, SeekOrigin.Begin);
                    SuffixDataBytes = br.ReadBytes((int)(end - suffixStart));
                }
            }

            public void Save(string outputPath)
            {
                using (var ms = new MemoryStream())
                using (var bw = new BinaryWriter(ms))
                {
                    foreach (var g in Glyphs)
                    {
                        bw.Write(g.TexNum);
                        bw.Write(TextureWidth == 0 ? 0f : g.XStart / TextureWidth);
                        bw.Write(TextureWidth == 0 ? 0f : g.XEnd / TextureWidth);
                        bw.Write(TextureHeight == 0 ? 0f : g.YStart / TextureHeight);
                        bw.Write(TextureHeight == 0 ? 0f : g.YEnd / TextureHeight);
                        if (HasScaleValue)
                        {
                            bw.Write(g.CharWidth);
                            bw.Write(g.CharHeight);
                        }
                    }

                    byte[] glyphBytes = ms.ToArray();
                    using (var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                    using (var outBw = new BinaryWriter(fs))
                    {
                        outBw.Write(SignatureBytes);
                        outBw.Write(ElementsDataBytes);
                        outBw.Write(FontNameBlockBytes);
                        outBw.Write(FontMetadataAfterNameBytes);
                        if (OptionalOneValueBytes != null) outBw.Write(OptionalOneValueBytes);
                        if (IsBlockSizeFont)
                        {
                            int bytesPerGlyph = HasScaleValue ? 28 : 20;
                            int recalculated = (CharCount * bytesPerGlyph) + 8;
                            outBw.Write(recalculated);
                        }
                        outBw.Write(CharCountValBytes);
                        outBw.Write(glyphBytes);
                        outBw.Write(SuffixDataBytes);
                    }
                }
            }

            public void ExportFnt(string path)
            {
                string textureBaseName = Path.GetFileNameWithoutExtension(path);
                File.WriteAllText(path, BuildFntText(textureBaseName), Encoding.UTF8);
            }

            public bool FntMatchesExport(string path)
            {
                string textureBaseName = Path.GetFileNameWithoutExtension(path);
                string currentText = File.ReadAllText(path, Encoding.UTF8);
                return string.Equals(currentText, BuildFntText(textureBaseName), StringComparison.Ordinal);
            }

            private string BuildFntText(string textureBaseName)
            {
                var sb = new StringBuilder();
                sb.AppendFormat("info face=\"{0}\" size={1} bold=0 italic=0 charset=\"\" unicode=0 smooth=0 aa=0 padding=0,0,0,0 spacing=0,0 outline=0", FontName, (int)BaseSize);
                sb.AppendLine();
                sb.AppendFormat("common lineHeight={0} base={0} scaleW={1} scaleH={2} pages={3} packed=0 alphaChnl=0 redChnl=0 greenChnl=0 blueChnl=0", (int)BaseSize, TextureWidth, TextureHeight, TexCount);
                sb.AppendLine();
                for (int i = 0; i < TexCount; i++)
                {
                    string pageFile = TexCount == 1 ? textureBaseName + ".tga" : textureBaseName + "_" + i + ".tga";
                    sb.AppendFormat("page id={0} file=\"{1}\"", i, pageFile);
                    sb.AppendLine();
                }
                sb.AppendFormat("chars count={0}", Glyphs.Count);
                sb.AppendLine();

                for (int i = 0; i < Glyphs.Count; i++)
                {
                    var g = Glyphs[i];
                    int width = HasScaleValue ? (int)g.CharWidth : (int)(g.XEnd - g.XStart);
                    int height = HasScaleValue ? (int)g.CharHeight : (int)(g.YEnd - g.YStart);
                    sb.AppendFormat("char id={0,-5} x={1,-5} y={2,-5} width={3,-5} height={4,-5} xoffset=0    yoffset=0    xadvance={5,-5} page={6,-3} chnl=15", i, (int)g.XStart, (int)g.YStart, width, height, width, g.TexNum);
                    sb.AppendLine();
                }

                return sb.ToString();
            }

            public void ImportFnt(string path)
            {
                var map = new Dictionary<int, WiiGlyph>();
                int maxId = -1;
                foreach (string line in File.ReadLines(path))
                {
                    string trimmed = line.Trim();
                    if (trimmed.StartsWith("common "))
                    {
                        var data = ParseFntPairs(trimmed);
                        if (data.ContainsKey("pages")) TexCount = ParseInt(data["pages"]);
                    }
                    else if (trimmed.StartsWith("char "))
                    {
                        var data = ParseFntPairs(trimmed);
                        int id = ParseInt(data["id"]);
                        maxId = Math.Max(maxId, id);
                        var g = new WiiGlyph
                        {
                            TexNum = ParseInt(data["page"]),
                            XStart = ParseFloat(data["x"]),
                            YStart = ParseFloat(data["y"])
                        };
                        float width = ParseFloat(data["width"]);
                        float height = ParseFloat(data["height"]);
                        g.XEnd = g.XStart + width;
                        g.YEnd = g.YStart + height;
                        if (HasScaleValue)
                        {
                            g.CharWidth = width;
                            g.CharHeight = height;
                        }
                        map[id] = g;
                    }
                }

                if (maxId + 1 != CharCount)
                {
                    // keep original size, just partial replacement
                }

                for (int i = 0; i < CharCount; i++)
                {
                    WiiGlyph g;
                    if (map.TryGetValue(i, out g)) Glyphs[i] = g;
                }
            }
        }

        internal static bool TryExtractWiiContainer(string inputPath, string outputDir, out string result)
        {
            result = null;
            byte[] data; int version; int offSizes; List<int> tplOffsets; bool alt;
            if (!TryParseContainer(inputPath, out data, out version, out offSizes, out tplOffsets, out alt)) return false;

            string name = Path.GetFileNameWithoutExtension(inputPath);
            string ext = Path.GetExtension(inputPath).ToLowerInvariant();
            List<int> tplSizes = GetTplSegmentSizes(data, version, offSizes, tplOffsets, alt);

            int size1 = tplSizes[0];
            byte[] tpl1 = Slice(data, tplOffsets[0], size1);
            try
            {
                WriteTplAsTga(tpl1, Path.Combine(outputDir, name + ".tga"));
                if (ext == ".font" && tplOffsets.Count > 1)
                    WriteTplAsTga(tpl1, Path.Combine(outputDir, name + "_0.tga"));
            }
            catch (Exception ex)
            {
                result = "File " + Path.GetFileName(inputPath) + " Wii TGA extraction failed: " + ex.Message;
                return true;
            }

            if (ext == ".font")
            {
                var res = ReadTplResolution(data, tplOffsets[0]);
                if (res.HasValue)
                {
                    var font = new WiiFontData();
                    font.Parse(inputPath, res.Value.Width, res.Value.Height);
                    font.TexCount = tplOffsets.Count;
                    font.ExportFnt(Path.Combine(outputDir, name + ".fnt"));
                }
            }

            if (ext == ".font" && tplOffsets.Count > 1)
            {
                for (int i = 1; i < tplOffsets.Count; i++)
                {
                    byte[] tpl = Slice(data, tplOffsets[i], tplSizes[i]);
                    try
                    {
                        WriteTplAsTga(tpl, Path.Combine(outputDir, name + "_" + i + ".tga"));
                    }
                    catch (Exception ex)
                    {
                        result = "File " + Path.GetFileName(inputPath) + " Wii font page TGA extraction failed: " + ex.Message;
                        return true;
                    }
                }
            }
            else if (!(version == 4 && alt) && (version == 4 || version == 7) && tplOffsets.Count > 1)
            {
                int size2 = tplSizes[1];
                if (size2 > 0)
                {
                    byte[] tpl2 = Slice(data, tplOffsets[1], size2);
                    try
                    {
                        WriteTplAsTga(tpl2, Path.Combine(outputDir, name + "_alpha.tga"));
                    }
                    catch (Exception ex)
                    {
                        result = "File " + Path.GetFileName(inputPath) + " Wii alpha TGA extraction failed: " + ex.Message;
                        return true;
                    }
                }
            }

            result = "File " + Path.GetFileName(inputPath) + " successfully extracted (Wii/TGA).";
            return true;
        }

        internal static bool TryRepackWiiContainer(string inputPath, string inputDir, string outputDir, out string result)
        {
            result = null;
            byte[] data; int version; int offSizes; List<int> tplOffsets; bool alt;
            if (!TryParseContainer(inputPath, out data, out version, out offSizes, out tplOffsets, out alt)) return false;

            string name = Path.GetFileNameWithoutExtension(inputPath);
            string ext = Path.GetExtension(inputPath).ToLowerInvariant();
            string tplPath = Path.Combine(inputDir, name + ".tpl");
            string tgaPath = Path.Combine(inputDir, name + ".tga");
            string ddsPath = Path.Combine(inputDir, name + ".dds");
            string fontPage0TgaPath = Path.Combine(inputDir, name + "_0.tga");
            string fontPage0DdsPath = Path.Combine(inputDir, name + "_0.dds");
            string firstImagePath = FindFirstExisting(
                ext == ".font" ? fontPage0TgaPath : null,
                tgaPath,
                ext == ".font" ? fontPage0DdsPath : null,
                ddsPath);
            if (!File.Exists(tplPath) && firstImagePath == null) return false;

            List<int> tplSizes = GetTplSegmentSizes(data, version, offSizes, tplOffsets, alt);
            var replacementTpls = new List<byte[]>();
            for (int i = 0; i < tplOffsets.Count; i++)
                replacementTpls.Add(Slice(data, tplOffsets[i], tplSizes[i]));

            try
            {
                replacementTpls[0] = firstImagePath != null
                    ? BuildTplFromImageFile(firstImagePath, replacementTpls[0])
                    : File.ReadAllBytes(tplPath);
            }
            catch (Exception ex)
            {
                result = "File " + Path.GetFileName(inputPath) + " Wii TGA import failed: " + ex.Message;
                return true;
            }

            if (ext == ".font" && tplOffsets.Count > 1)
            {
                for (int i = 1; i < tplOffsets.Count; i++)
                {
                    string pageTplPath = Path.Combine(inputDir, name + "_" + i + ".tpl");
                    string pageTgaPath = Path.Combine(inputDir, name + "_" + i + ".tga");
                    string pageDdsPath = Path.Combine(inputDir, name + "_" + i + ".dds");
                    string pageImagePath = FindFirstExisting(pageTgaPath, pageDdsPath);
                    try
                    {
                        if (pageImagePath != null)
                            replacementTpls[i] = BuildTplFromImageFile(pageImagePath, replacementTpls[i]);
                        else if (File.Exists(pageTplPath))
                            replacementTpls[i] = File.ReadAllBytes(pageTplPath);
                    }
                    catch (Exception ex)
                    {
                        result = "File " + Path.GetFileName(inputPath) + " Wii font page TGA import failed: " + ex.Message;
                        return true;
                    }
                }
            }
            else if ((version == 4 || version == 7) && !(version == 4 && alt) && tplOffsets.Count > 1)
            {
                string alphaPath = Path.Combine(inputDir, name + "_alpha.tpl");
                string alphaTgaPath = Path.Combine(inputDir, name + "_alpha.tga");
                string alphaDdsPath = Path.Combine(inputDir, name + "_alpha.dds");
                string alphaImagePath = FindFirstExisting(alphaTgaPath, alphaDdsPath);
                if (alphaImagePath != null)
                {
                    try
                    {
                        replacementTpls[1] = BuildTplFromImageFile(alphaImagePath, replacementTpls[1]);
                    }
                    catch (Exception ex)
                    {
                        result = "File " + Path.GetFileName(inputPath) + " Wii alpha TGA import failed: " + ex.Message;
                        return true;
                    }
                }
                else if (File.Exists(alphaPath)) replacementTpls[1] = File.ReadAllBytes(alphaPath);
            }

            byte[] newData = ReplaceTplSegments(data, tplOffsets, tplSizes, replacementTpls);
            if (ext != ".font")
            {
                if (offSizes >= 0 && offSizes + 4 <= newData.Length)
                    Array.Copy(BitConverter.GetBytes(replacementTpls[0].Length), 0, newData, offSizes, 4);
                if ((version == 4 || version == 7) && !(version == 4 && alt) && offSizes + 8 <= newData.Length && replacementTpls.Count > 1)
                    Array.Copy(BitConverter.GetBytes(replacementTpls[1].Length), 0, newData, offSizes + 4, 4);
            }
            string outPath = Path.Combine(outputDir, name + ext);
            File.WriteAllBytes(outPath, newData);

            if (ext == ".font")
            {
                string fntPath = Path.Combine(inputDir, name + ".fnt");
                var res = ReadTplResolution(newData, tplOffsets[0]);
                if (File.Exists(fntPath) && res.HasValue)
                {
                    var font = new WiiFontData();
                    font.Parse(outPath, res.Value.Width, res.Value.Height);
                    font.TexCount = tplOffsets.Count;
                    if (!font.FntMatchesExport(fntPath))
                    {
                        int originalTexCount = font.TexCount;
                        List<WiiGlyph> originalGlyphs = CloneGlyphs(font.Glyphs);
                        font.ImportFnt(fntPath);
                        if (font.TexCount != originalTexCount || !GlyphsEqual(originalGlyphs, font.Glyphs, font.HasScaleValue))
                        {
                            font.Save(outPath);
                        }
                    }
                }
            }

            result = "File " + Path.GetFileName(inputPath) + " successfully imported (Wii/TGA).";
            return true;
        }

        internal static bool TryLoadWiiFontForEditor(string fontPath, out WiiFontData fontData)
        {
            fontData = null;
            byte[] data; int version; int offSizes; List<int> tplOffsets; bool alt;
            if (!TryParseContainer(fontPath, out data, out version, out offSizes, out tplOffsets, out alt)) return false;
            if (Path.GetExtension(fontPath).ToLowerInvariant() != ".font") return false;
            var res = ReadTplResolution(data, tplOffsets[0]);
            if (!res.HasValue) return false;
            var parsed = new WiiFontData();
            parsed.Parse(fontPath, res.Value.Width, res.Value.Height);
            fontData = parsed;
            return true;
        }

        internal static bool TryGetWiiTextureArgbForEditor(string fontPath, int textureIndex, int targetWidth, int targetHeight, out byte[] argb, out int width, out int height)
        {
            argb = null;
            width = 0;
            height = 0;

            byte[] data; int version; int offSizes; List<int> tplOffsets; bool alt;
            if (!TryParseContainer(fontPath, out data, out version, out offSizes, out tplOffsets, out alt)) return false;
            if (textureIndex < 0 || textureIndex >= tplOffsets.Count) return false;

            List<int> tplSizes = GetTplSegmentSizes(data, version, offSizes, tplOffsets, alt);
            byte[] tpl = Slice(data, tplOffsets[textureIndex], tplSizes[textureIndex]);

            TplTexture info;
            if (!TryParseTpl(tpl, out info)) return false;

            byte[] encoded = Slice(tpl, info.DataOffset, tpl.Length - info.DataOffset);
            argb = DecodeWiiTexture(encoded, info.Width, info.Height, info.Format);
            width = info.Width;
            height = info.Height;

            if (targetWidth > 0 && targetHeight > 0 && (targetWidth != width || targetHeight != height))
            {
                argb = ScaleArgbNearest(argb, width, height, targetWidth, targetHeight);
                width = targetWidth;
                height = targetHeight;
            }

            return true;
        }

        internal static bool TryApplyWiiTextureImportsForEditor(string fontPath, IDictionary<int, string> texturePaths, out string error)
        {
            error = null;
            if (texturePaths == null || texturePaths.Count == 0) return true;

            try
            {
                byte[] data; int version; int offSizes; List<int> tplOffsets; bool alt;
                if (!TryParseContainer(fontPath, out data, out version, out offSizes, out tplOffsets, out alt))
                {
                    error = "The saved file is not a supported Wii font container.";
                    return false;
                }

                List<int> tplSizes = GetTplSegmentSizes(data, version, offSizes, tplOffsets, alt);
                var replacementTpls = new List<byte[]>();
                for (int i = 0; i < tplOffsets.Count; i++)
                    replacementTpls.Add(Slice(data, tplOffsets[i], tplSizes[i]));

                foreach (var pair in texturePaths)
                {
                    int index = pair.Key;
                    if (index < 0 || index >= replacementTpls.Count) continue;
                    if (string.IsNullOrEmpty(pair.Value) || !File.Exists(pair.Value)) continue;
                    replacementTpls[index] = BuildTplFromImageFile(pair.Value, replacementTpls[index]);
                }

                byte[] newData = ReplaceTplSegments(data, tplOffsets, tplSizes, replacementTpls);
                if (offSizes >= 0 && offSizes + 4 <= newData.Length)
                    Array.Copy(BitConverter.GetBytes(replacementTpls[0].Length), 0, newData, offSizes, 4);
                if ((version == 4 || version == 7) && !(version == 4 && alt) && offSizes + 8 <= newData.Length && replacementTpls.Count > 1)
                    Array.Copy(BitConverter.GetBytes(replacementTpls[1].Length), 0, newData, offSizes + 4, 4);

                File.WriteAllBytes(fontPath, newData);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static List<int> GetTplSegmentSizes(byte[] data, int version, int offSizes, List<int> tplOffsets, bool alt)
        {
            var sizes = new List<int>();
            for (int i = 0; i < tplOffsets.Count; i++)
            {
                int offset = tplOffsets[i];
                int nextOffset = i + 1 < tplOffsets.Count ? tplOffsets[i + 1] : data.Length;
                int available = nextOffset - offset;
                int storedSize = 0;

                if (i == 0 && offSizes >= 0 && offSizes + 4 <= data.Length)
                    storedSize = BitConverter.ToInt32(data, offSizes);
                else if (i == 1 && (version == 4 || version == 7) && !(version == 4 && alt) && offSizes + 8 <= data.Length)
                    storedSize = BitConverter.ToInt32(data, offSizes + 4);

                if (storedSize > 0 && storedSize <= data.Length - offset)
                {
                    sizes.Add(storedSize);
                    continue;
                }

                int calculatedSize;
                if (TryCalculateTplSize(data, offset, out calculatedSize) && calculatedSize > 0 && calculatedSize <= data.Length - offset)
                    sizes.Add(calculatedSize);
                else
                    sizes.Add(Math.Max(0, available));
            }
            return sizes;
        }

        private static byte[] ReplaceTplSegments(byte[] data, List<int> tplOffsets, List<int> tplSizes, List<byte[]> replacements)
        {
            using (var ms = new MemoryStream())
            {
                int cursor = 0;
                for (int i = 0; i < tplOffsets.Count; i++)
                {
                    int offset = tplOffsets[i];
                    if (offset > cursor)
                        ms.Write(data, cursor, offset - cursor);

                    byte[] replacement = replacements[i];
                    ms.Write(replacement, 0, replacement.Length);
                    cursor = offset + tplSizes[i];
                }

                if (cursor < data.Length)
                    ms.Write(data, cursor, data.Length - cursor);
                return ms.ToArray();
            }
        }

        private static string FindFirstExisting(params string[] paths)
        {
            foreach (string path in paths)
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path)) return path;
            }
            return null;
        }

        private static byte[] ScaleArgbNearest(byte[] src, int srcWidth, int srcHeight, int dstWidth, int dstHeight)
        {
            byte[] dst = new byte[dstWidth * dstHeight * 4];
            for (int y = 0; y < dstHeight; y++)
            {
                int sy = Math.Min(srcHeight - 1, (int)((long)y * srcHeight / dstHeight));
                for (int x = 0; x < dstWidth; x++)
                {
                    int sx = Math.Min(srcWidth - 1, (int)((long)x * srcWidth / dstWidth));
                    int srcPos = ((sy * srcWidth) + sx) * 4;
                    int dstPos = ((y * dstWidth) + x) * 4;
                    dst[dstPos] = src[srcPos];
                    dst[dstPos + 1] = src[srcPos + 1];
                    dst[dstPos + 2] = src[srcPos + 2];
                    dst[dstPos + 3] = src[srcPos + 3];
                }
            }
            return dst;
        }

        private class TplTexture
        {
            public int Width;
            public int Height;
            public int Format;
            public int HeaderOffset;
            public int DataOffset;
            public int MipCount;
        }

        private static void WriteTplAsTga(byte[] tpl, string tgaPath)
        {
            TplTexture info;
            if (!TryParseTpl(tpl, out info)) throw new InvalidDataException("Unsupported TPL texture.");

            byte[] encoded = Slice(tpl, info.DataOffset, tpl.Length - info.DataOffset);
            byte[] rgba = DecodeWiiTexture(encoded, info.Width, info.Height, info.Format);
            WriteTgaRgba(tgaPath, info.Width, info.Height, rgba);
        }

        private static byte[] BuildTplFromImageFile(string imagePath, byte[] originalTpl)
        {
            TplTexture info;
            if (!TryParseTpl(originalTpl, out info)) throw new InvalidDataException("Unsupported TPL texture.");

            int width, height;
            byte[] image = File.ReadAllBytes(imagePath);
            byte[] rgba;
            string ext = Path.GetExtension(imagePath).ToLowerInvariant();
            if (ext == ".tga")
                rgba = ReadTgaRgba(image, out width, out height);
            else if (ext == ".dds")
                rgba = ReadDdsRgba(image, out width, out height);
            else
                throw new NotSupportedException("Only TGA and DDS files are supported for Wii import.");

            if (width == info.Width && height == info.Height)
            {
                byte[] originalEncoded = Slice(originalTpl, info.DataOffset, originalTpl.Length - info.DataOffset);
                byte[] originalRgba = DecodeWiiTexture(originalEncoded, info.Width, info.Height, info.Format);
                if (ByteArraysEqual(rgba, originalRgba))
                {
                    return originalTpl;
                }
            }

            byte[] encoded = EncodeWiiTexture(rgba, width, height, info.Format, info.MipCount);

            byte[] output = new byte[info.DataOffset + encoded.Length];
            Array.Copy(originalTpl, 0, output, 0, Math.Min(info.DataOffset, originalTpl.Length));
            WriteUInt16BE(output, info.HeaderOffset, (ushort)height);
            WriteUInt16BE(output, info.HeaderOffset + 2, (ushort)width);
            WriteUInt32BE(output, info.HeaderOffset + 4, (uint)info.Format);
            WriteUInt32BE(output, info.HeaderOffset + 8, (uint)info.DataOffset);
            Array.Copy(encoded, 0, output, info.DataOffset, encoded.Length);
            return output;
        }

        private static bool ByteArraysEqual(byte[] left, byte[] right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left == null || right == null || left.Length != right.Length) return false;
            for (int i = 0; i < left.Length; i++)
                if (left[i] != right[i]) return false;
            return true;
        }

        private static List<WiiGlyph> CloneGlyphs(IEnumerable<WiiGlyph> glyphs)
        {
            var result = new List<WiiGlyph>();
            foreach (WiiGlyph glyph in glyphs)
            {
                result.Add(new WiiGlyph
                {
                    TexNum = glyph.TexNum,
                    XStart = glyph.XStart,
                    XEnd = glyph.XEnd,
                    YStart = glyph.YStart,
                    YEnd = glyph.YEnd,
                    CharWidth = glyph.CharWidth,
                    CharHeight = glyph.CharHeight
                });
            }
            return result;
        }

        private static bool GlyphsEqual(IList<WiiGlyph> left, IList<WiiGlyph> right, bool compareScale)
        {
            if (left == null || right == null || left.Count != right.Count) return false;
            for (int i = 0; i < left.Count; i++)
            {
                if (left[i].TexNum != right[i].TexNum) return false;
                if (left[i].XStart != right[i].XStart || left[i].XEnd != right[i].XEnd) return false;
                if (left[i].YStart != right[i].YStart || left[i].YEnd != right[i].YEnd) return false;
                if (compareScale && (left[i].CharWidth != right[i].CharWidth || left[i].CharHeight != right[i].CharHeight)) return false;
            }
            return true;
        }

        private static bool TryParseTpl(byte[] tpl, out TplTexture info)
        {
            info = null;
            if (tpl == null || tpl.Length < 0x40) return false;
            if (ReadUInt32BE(tpl, 0) != 0x0020AF30) return false;

            uint images = ReadUInt32BE(tpl, 4);
            uint tableOffset = ReadUInt32BE(tpl, 8);
            if (images == 0 || tableOffset + 8 > tpl.Length) return false;

            int texHeaderOffset = (int)ReadUInt32BE(tpl, (int)tableOffset);
            if (texHeaderOffset <= 0 || texHeaderOffset + 0x24 > tpl.Length) return false;

            int height = ReadUInt16BE(tpl, texHeaderOffset);
            int width = ReadUInt16BE(tpl, texHeaderOffset + 2);
            int format = (int)ReadUInt32BE(tpl, texHeaderOffset + 4);
            int dataOffset = (int)ReadUInt32BE(tpl, texHeaderOffset + 8);
            if (width <= 0 || height <= 0 || dataOffset <= 0 || dataOffset > tpl.Length) return false;

            info = new TplTexture();
            info.Width = width;
            info.Height = height;
            info.Format = format;
            info.HeaderOffset = texHeaderOffset;
            info.DataOffset = dataOffset;
            info.MipCount = tpl[texHeaderOffset + 0x22] + 1;
            if (info.MipCount < 1) info.MipCount = 1;
            return true;
        }

        private static bool TryCalculateTplSize(byte[] data, int tplOffset, out int size)
        {
            size = 0;
            if (tplOffset < 0 || tplOffset >= data.Length) return false;

            byte[] slice = Slice(data, tplOffset, data.Length - tplOffset);
            TplTexture info;
            if (!TryParseTpl(slice, out info)) return false;

            int blockWidth, blockHeight, bitsPerPixel;
            GetWiiFormatInfo(info.Format, out blockWidth, out blockHeight, out bitsPerPixel);

            int payload = 0;
            int w = info.Width;
            int h = info.Height;
            for (int level = 0; level < info.MipCount; level++)
            {
                int alignedWidth = Align(w, blockWidth);
                int alignedHeight = Align(h, blockHeight);
                payload += (alignedWidth * alignedHeight * bitsPerPixel) / 8;
                w = Math.Max(1, w / 2);
                h = Math.Max(1, h / 2);
            }

            size = info.DataOffset + payload;
            return size > info.DataOffset && size <= data.Length - tplOffset;
        }

        private static byte[] DecodeWiiTexture(byte[] src, int width, int height, int format)
        {
            int blockWidth, blockHeight, bitsPerPixel;
            GetWiiFormatInfo(format, out blockWidth, out blockHeight, out bitsPerPixel);

            int alignedWidth = Align(width, blockWidth);
            int alignedHeight = Align(height, blockHeight);
            int blockBytes = blockWidth * blockHeight * bitsPerPixel / 8;
            byte[] rgba = new byte[width * height * 4];
            int srcPos = 0;

            for (int y = 0; y < alignedHeight; y += blockHeight)
            {
                for (int x = 0; x < alignedWidth; x += blockWidth)
                {
                    if (srcPos + blockBytes > src.Length) return rgba;
                    DecodeWiiBlock(src, srcPos, rgba, width, height, x, y, format);
                    srcPos += blockBytes;
                }
            }

            return rgba;
        }

        private static byte[] EncodeWiiTexture(byte[] rgba, int width, int height, int format, int mipCount)
        {
            using (var ms = new MemoryStream())
            {
                byte[] current = rgba;
                int currentWidth = width;
                int currentHeight = height;

                for (int mip = 0; mip < mipCount; mip++)
                {
                    byte[] level = EncodeWiiTextureLevel(current, currentWidth, currentHeight, format);
                    ms.Write(level, 0, level.Length);

                    if (currentWidth == 1 && currentHeight == 1) break;
                    current = BuildNextMip(current, currentWidth, currentHeight);
                    currentWidth = Math.Max(1, currentWidth / 2);
                    currentHeight = Math.Max(1, currentHeight / 2);
                }

                return ms.ToArray();
            }
        }

        private static byte[] EncodeWiiTextureLevel(byte[] rgba, int width, int height, int format)
        {
            int blockWidth, blockHeight, bitsPerPixel;
            GetWiiFormatInfo(format, out blockWidth, out blockHeight, out bitsPerPixel);

            int alignedWidth = Align(width, blockWidth);
            int alignedHeight = Align(height, blockHeight);
            int blockBytes = blockWidth * blockHeight * bitsPerPixel / 8;
            byte[] encoded = new byte[(alignedWidth / blockWidth) * (alignedHeight / blockHeight) * blockBytes];
            int dstPos = 0;

            for (int y = 0; y < alignedHeight; y += blockHeight)
            {
                for (int x = 0; x < alignedWidth; x += blockWidth)
                {
                    EncodeWiiBlock(rgba, width, height, x, y, format, encoded, dstPos);
                    dstPos += blockBytes;
                }
            }

            return encoded;
        }

        private static byte[] BuildNextMip(byte[] rgba, int width, int height)
        {
            int nextWidth = Math.Max(1, width / 2);
            int nextHeight = Math.Max(1, height / 2);
            byte[] next = new byte[nextWidth * nextHeight * 4];

            for (int y = 0; y < nextHeight; y++)
            {
                for (int x = 0; x < nextWidth; x++)
                {
                    int a = 0, r = 0, g = 0, b = 0, count = 0;
                    for (int yy = 0; yy < 2; yy++)
                    {
                        for (int xx = 0; xx < 2; xx++)
                        {
                            int sx = Math.Min(width - 1, x * 2 + xx);
                            int sy = Math.Min(height - 1, y * 2 + yy);
                            int sp = (sy * width + sx) * 4;
                            a += rgba[sp];
                            r += rgba[sp + 1];
                            g += rgba[sp + 2];
                            b += rgba[sp + 3];
                            count++;
                        }
                    }

                    int dp = (y * nextWidth + x) * 4;
                    next[dp] = (byte)(a / count);
                    next[dp + 1] = (byte)(r / count);
                    next[dp + 2] = (byte)(g / count);
                    next[dp + 3] = (byte)(b / count);
                }
            }

            return next;
        }

        private static void GetWiiFormatInfo(int format, out int blockWidth, out int blockHeight, out int bitsPerPixel)
        {
            switch (format)
            {
                case 0:
                    blockWidth = 8; blockHeight = 8; bitsPerPixel = 4; return;
                case 1:
                case 2:
                    blockWidth = 8; blockHeight = 4; bitsPerPixel = 8; return;
                case 3:
                case 4:
                case 5:
                    blockWidth = 4; blockHeight = 4; bitsPerPixel = 16; return;
                case 6:
                    blockWidth = 4; blockHeight = 4; bitsPerPixel = 32; return;
                case 14:
                    blockWidth = 8; blockHeight = 8; bitsPerPixel = 4; return;
                default:
                    throw new NotSupportedException("Unsupported Wii texture format: " + format);
            }
        }

        private static void DecodeWiiBlock(byte[] src, int srcPos, byte[] rgba, int width, int height, int bx, int by, int format)
        {
            int p = srcPos;
            switch (format)
            {
                case 0:
                    for (int y = 0; y < 8; y++)
                    {
                        for (int x = 0; x < 8; x += 2)
                        {
                            byte b = src[p++];
                            SetPixel(rgba, width, height, bx + x, by + y, 255, Expand4((b >> 4) & 0xF), Expand4((b >> 4) & 0xF), Expand4((b >> 4) & 0xF));
                            SetPixel(rgba, width, height, bx + x + 1, by + y, 255, Expand4(b & 0xF), Expand4(b & 0xF), Expand4(b & 0xF));
                        }
                    }
                    break;
                case 1:
                    for (int y = 0; y < 4; y++)
                        for (int x = 0; x < 8; x++)
                        {
                            byte i = src[p++];
                            SetPixel(rgba, width, height, bx + x, by + y, 255, i, i, i);
                        }
                    break;
                case 2:
                    for (int y = 0; y < 4; y++)
                        for (int x = 0; x < 8; x++)
                        {
                            byte b = src[p++];
                            byte a = Expand4((b >> 4) & 0xF);
                            byte i = Expand4(b & 0xF);
                            SetPixel(rgba, width, height, bx + x, by + y, a, i, i, i);
                        }
                    break;
                case 3:
                    for (int y = 0; y < 4; y++)
                        for (int x = 0; x < 4; x++)
                        {
                            byte a = src[p++];
                            byte i = src[p++];
                            SetPixel(rgba, width, height, bx + x, by + y, a, i, i, i);
                        }
                    break;
                case 4:
                    for (int y = 0; y < 4; y++)
                        for (int x = 0; x < 4; x++)
                        {
                            ushort c = ReadUInt16BE(src, p); p += 2;
                            byte r, g, b;
                            DecodeRgb565(c, out r, out g, out b);
                            SetPixel(rgba, width, height, bx + x, by + y, 255, r, g, b);
                        }
                    break;
                case 5:
                    for (int y = 0; y < 4; y++)
                        for (int x = 0; x < 4; x++)
                        {
                            byte a, r, g, b;
                            DecodeRgb5A3(ReadUInt16BE(src, p), out a, out r, out g, out b); p += 2;
                            SetPixel(rgba, width, height, bx + x, by + y, a, r, g, b);
                        }
                    break;
                case 6:
                    for (int y = 0; y < 4; y++)
                        for (int x = 0; x < 4; x++)
                        {
                            int ar = srcPos + ((y * 4) + x) * 2;
                            int gb = srcPos + 32 + ((y * 4) + x) * 2;
                            SetPixel(rgba, width, height, bx + x, by + y, src[ar], src[ar + 1], src[gb], src[gb + 1]);
                        }
                    break;
                case 14:
                    DecodeDxt1Block(src, p, true, rgba, width, height, bx, by);
                    DecodeDxt1Block(src, p + 8, true, rgba, width, height, bx + 4, by);
                    DecodeDxt1Block(src, p + 16, true, rgba, width, height, bx, by + 4);
                    DecodeDxt1Block(src, p + 24, true, rgba, width, height, bx + 4, by + 4);
                    break;
            }
        }

        private static void EncodeWiiBlock(byte[] rgba, int width, int height, int bx, int by, int format, byte[] dst, int dstPos)
        {
            int p = dstPos;
            switch (format)
            {
                case 0:
                    for (int y = 0; y < 8; y++)
                        for (int x = 0; x < 8; x += 2)
                        {
                            byte i0 = ToIntensity4(rgba, width, height, bx + x, by + y);
                            byte i1 = ToIntensity4(rgba, width, height, bx + x + 1, by + y);
                            dst[p++] = (byte)((i0 << 4) | i1);
                        }
                    break;
                case 1:
                    for (int y = 0; y < 4; y++)
                        for (int x = 0; x < 8; x++)
                            dst[p++] = ToIntensity8(rgba, width, height, bx + x, by + y);
                    break;
                case 2:
                    for (int y = 0; y < 4; y++)
                        for (int x = 0; x < 8; x++)
                        {
                            byte a = ToAlpha4(rgba, width, height, bx + x, by + y);
                            byte i = ToIntensity4(rgba, width, height, bx + x, by + y);
                            dst[p++] = (byte)((a << 4) | i);
                        }
                    break;
                case 3:
                    for (int y = 0; y < 4; y++)
                        for (int x = 0; x < 4; x++)
                        {
                            dst[p++] = GetChannel(rgba, width, height, bx + x, by + y, 0);
                            dst[p++] = ToIntensity8(rgba, width, height, bx + x, by + y);
                        }
                    break;
                case 4:
                    for (int y = 0; y < 4; y++)
                        for (int x = 0; x < 4; x++)
                        {
                            ushort c = EncodeRgb565(GetChannel(rgba, width, height, bx + x, by + y, 1), GetChannel(rgba, width, height, bx + x, by + y, 2), GetChannel(rgba, width, height, bx + x, by + y, 3));
                            WriteUInt16BE(dst, p, c); p += 2;
                        }
                    break;
                case 5:
                    for (int y = 0; y < 4; y++)
                        for (int x = 0; x < 4; x++)
                        {
                            ushort c = EncodeRgb5A3(GetChannel(rgba, width, height, bx + x, by + y, 0), GetChannel(rgba, width, height, bx + x, by + y, 1), GetChannel(rgba, width, height, bx + x, by + y, 2), GetChannel(rgba, width, height, bx + x, by + y, 3));
                            WriteUInt16BE(dst, p, c); p += 2;
                        }
                    break;
                case 6:
                    for (int y = 0; y < 4; y++)
                        for (int x = 0; x < 4; x++)
                        {
                            int ar = dstPos + ((y * 4) + x) * 2;
                            int gb = dstPos + 32 + ((y * 4) + x) * 2;
                            dst[ar] = GetChannel(rgba, width, height, bx + x, by + y, 0);
                            dst[ar + 1] = GetChannel(rgba, width, height, bx + x, by + y, 1);
                            dst[gb] = GetChannel(rgba, width, height, bx + x, by + y, 2);
                            dst[gb + 1] = GetChannel(rgba, width, height, bx + x, by + y, 3);
                        }
                    break;
                case 14:
                    EncodeDxt1Block(rgba, width, height, bx, by, dst, p, true);
                    EncodeDxt1Block(rgba, width, height, bx + 4, by, dst, p + 8, true);
                    EncodeDxt1Block(rgba, width, height, bx, by + 4, dst, p + 16, true);
                    EncodeDxt1Block(rgba, width, height, bx + 4, by + 4, dst, p + 24, true);
                    break;
            }
        }

        private static void WriteTgaRgba(string path, int width, int height, byte[] rgba)
        {
            using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                bw.Write((byte)0); // ID length
                bw.Write((byte)0); // no color map
                bw.Write((byte)2); // uncompressed true-color
                bw.Write((ushort)0);
                bw.Write((ushort)0);
                bw.Write((byte)0);
                bw.Write((ushort)0);
                bw.Write((ushort)0);
                bw.Write((ushort)width);
                bw.Write((ushort)height);
                bw.Write((byte)32);
                bw.Write((byte)0x28); // 8 alpha bits, top-left origin

                for (int i = 0; i < width * height; i++)
                {
                    int p = i * 4;
                    bw.Write(rgba[p + 3]);
                    bw.Write(rgba[p + 2]);
                    bw.Write(rgba[p + 1]);
                    bw.Write(rgba[p]);
                }
            }
        }

        private static byte[] ReadTgaRgba(byte[] tga, out int width, out int height)
        {
            if (tga.Length < 18) throw new InvalidDataException("Not a TGA file.");
            int idLength = tga[0];
            int colorMapType = tga[1];
            int imageType = tga[2];
            if (colorMapType != 0 || imageType != 2) throw new NotSupportedException("Only uncompressed true-color TGA files are supported for Wii import.");

            width = BitConverter.ToUInt16(tga, 12);
            height = BitConverter.ToUInt16(tga, 14);
            int bits = tga[16];
            int descriptor = tga[17];
            if (width <= 0 || height <= 0 || (bits != 24 && bits != 32)) throw new InvalidDataException("Invalid TGA dimensions or pixel format.");

            int bytesPerPixel = bits / 8;
            int srcStart = 18 + idLength;
            int expected = srcStart + (width * height * bytesPerPixel);
            if (expected > tga.Length) throw new InvalidDataException("TGA pixel data is truncated.");

            bool topOrigin = (descriptor & 0x20) != 0;
            byte[] rgba = new byte[width * height * 4];
            for (int y = 0; y < height; y++)
            {
                int dstY = topOrigin ? y : (height - 1 - y);
                for (int x = 0; x < width; x++)
                {
                    int src = srcStart + ((y * width) + x) * bytesPerPixel;
                    int dst = ((dstY * width) + x) * 4;
                    rgba[dst] = bits == 32 ? tga[src + 3] : (byte)255;
                    rgba[dst + 1] = tga[src + 2];
                    rgba[dst + 2] = tga[src + 1];
                    rgba[dst + 3] = tga[src];
                }
            }
            return rgba;
        }

        private static void WriteDdsRgba(string path, int width, int height, byte[] rgba)
        {
            using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                bw.Write(Encoding.ASCII.GetBytes("DDS "));
                bw.Write((uint)124);
                bw.Write((uint)(0x1 | 0x2 | 0x4 | 0x8 | 0x1000));
                bw.Write((uint)height);
                bw.Write((uint)width);
                bw.Write((uint)(width * 4));
                bw.Write((uint)0);
                bw.Write((uint)1);
                for (int i = 0; i < 11; i++) bw.Write((uint)0);
                bw.Write((uint)32);
                bw.Write((uint)(0x40 | 0x1));
                bw.Write((uint)0);
                bw.Write((uint)32);
                bw.Write((uint)0x00FF0000);
                bw.Write((uint)0x0000FF00);
                bw.Write((uint)0x000000FF);
                bw.Write((uint)0xFF000000);
                bw.Write((uint)0x1000);
                bw.Write((uint)0);
                bw.Write((uint)0);
                bw.Write((uint)0);
                bw.Write((uint)0);

                for (int i = 0; i < width * height; i++)
                {
                    int p = i * 4;
                    bw.Write(rgba[p + 3]);
                    bw.Write(rgba[p + 2]);
                    bw.Write(rgba[p + 1]);
                    bw.Write(rgba[p]);
                }
            }
        }

        private static byte[] ReadDdsRgba(byte[] dds, out int width, out int height)
        {
            if (dds.Length < 128 || Encoding.ASCII.GetString(dds, 0, 4) != "DDS ") throw new InvalidDataException("Not a DDS file.");
            height = BitConverter.ToInt32(dds, 12);
            width = BitConverter.ToInt32(dds, 16);
            uint pfFlags = BitConverter.ToUInt32(dds, 80);
            string fourCc = Encoding.ASCII.GetString(dds, 84, 4);
            uint rgbBits = BitConverter.ToUInt32(dds, 88);
            uint rMask = BitConverter.ToUInt32(dds, 92);
            uint gMask = BitConverter.ToUInt32(dds, 96);
            uint bMask = BitConverter.ToUInt32(dds, 100);
            uint aMask = BitConverter.ToUInt32(dds, 104);

            if ((pfFlags & 0x4) != 0 && fourCc == "DXT1")
            {
                return DecodeDdsDxt1(dds, 128, width, height);
            }

            if (rgbBits != 32) throw new NotSupportedException("Only 32-bit RGBA and DXT1 DDS files are supported for Wii import.");

            byte[] rgba = new byte[width * height * 4];
            int src = 128;
            for (int i = 0; i < width * height; i++)
            {
                uint pixel = BitConverter.ToUInt32(dds, src);
                int dst = i * 4;
                rgba[dst] = aMask == 0 ? (byte)255 : ExtractMasked(pixel, aMask);
                rgba[dst + 1] = ExtractMasked(pixel, rMask);
                rgba[dst + 2] = ExtractMasked(pixel, gMask);
                rgba[dst + 3] = ExtractMasked(pixel, bMask);
                src += 4;
            }
            return rgba;
        }

        private static byte[] DecodeDdsDxt1(byte[] dds, int offset, int width, int height)
        {
            byte[] rgba = new byte[width * height * 4];
            int p = offset;
            for (int y = 0; y < height; y += 4)
                for (int x = 0; x < width; x += 4)
                {
                    DecodeDxt1Block(dds, p, false, rgba, width, height, x, y);
                    p += 8;
                }
            return rgba;
        }

        private static void DecodeDxt1Block(byte[] src, int srcPos, bool bigEndian, byte[] rgba, int width, int height, int bx, int by)
        {
            ushort c0 = bigEndian ? ReadUInt16BE(src, srcPos) : BitConverter.ToUInt16(src, srcPos);
            ushort c1 = bigEndian ? ReadUInt16BE(src, srcPos + 2) : BitConverter.ToUInt16(src, srcPos + 2);
            uint lookup = bigEndian ? ReadUInt32BE(src, srcPos + 4) : BitConverter.ToUInt32(src, srcPos + 4);
            byte[] pal = BuildDxtPalette(c0, c1);

            for (int y = 0; y < 4; y++)
                for (int x = 0; x < 4; x++)
                {
                    int idx = bigEndian ? (int)((lookup >> (30 - ((y * 4 + x) * 2))) & 3) : (int)((lookup >> ((y * 4 + x) * 2)) & 3);
                    int pp = idx * 4;
                    SetPixel(rgba, width, height, bx + x, by + y, pal[pp], pal[pp + 1], pal[pp + 2], pal[pp + 3]);
                }
        }

        private static void EncodeDxt1Block(byte[] rgba, int width, int height, int bx, int by, byte[] dst, int dstPos, bool bigEndian)
        {
            bool hasAlpha = false;
            int minLum = Int32.MaxValue, maxLum = Int32.MinValue;
            byte minR = 0, minG = 0, minB = 0, maxR = 0, maxG = 0, maxB = 0;

            for (int y = 0; y < 4; y++)
                for (int x = 0; x < 4; x++)
                {
                    byte a = GetChannel(rgba, width, height, bx + x, by + y, 0);
                    byte r = GetChannel(rgba, width, height, bx + x, by + y, 1);
                    byte g = GetChannel(rgba, width, height, bx + x, by + y, 2);
                    byte b = GetChannel(rgba, width, height, bx + x, by + y, 3);
                    if (a < 128) { hasAlpha = true; continue; }
                    int lum = r * 3 + g * 6 + b;
                    if (lum < minLum) { minLum = lum; minR = r; minG = g; minB = b; }
                    if (lum > maxLum) { maxLum = lum; maxR = r; maxG = g; maxB = b; }
                }

            ushort c0 = EncodeRgb565(maxR, maxG, maxB);
            ushort c1 = EncodeRgb565(minR, minG, minB);
            if (!hasAlpha && c0 <= c1) { ushort t = c0; c0 = c1; c1 = t; }
            if (hasAlpha && c0 > c1) { ushort t = c0; c0 = c1; c1 = t; }

            byte[] pal = BuildDxtPalette(c0, c1);
            uint lookup = 0;
            for (int y = 0; y < 4; y++)
                for (int x = 0; x < 4; x++)
                {
                    byte a = GetChannel(rgba, width, height, bx + x, by + y, 0);
                    byte r = GetChannel(rgba, width, height, bx + x, by + y, 1);
                    byte g = GetChannel(rgba, width, height, bx + x, by + y, 2);
                    byte b = GetChannel(rgba, width, height, bx + x, by + y, 3);
                    int best = 0;
                    int bestScore = Int32.MaxValue;
                    for (int i = 0; i < 4; i++)
                    {
                        int pp = i * 4;
                        int da = a - pal[pp];
                        int dr = r - pal[pp + 1];
                        int dg = g - pal[pp + 2];
                        int db = b - pal[pp + 3];
                        int score = da * da + dr * dr + dg * dg + db * db;
                        if (score < bestScore) { bestScore = score; best = i; }
                    }
                    int pixel = y * 4 + x;
                    if (bigEndian) lookup |= (uint)(best << (30 - pixel * 2));
                    else lookup |= (uint)(best << (pixel * 2));
                }

            if (bigEndian)
            {
                WriteUInt16BE(dst, dstPos, c0);
                WriteUInt16BE(dst, dstPos + 2, c1);
                WriteUInt32BE(dst, dstPos + 4, lookup);
            }
            else
            {
                Array.Copy(BitConverter.GetBytes(c0), 0, dst, dstPos, 2);
                Array.Copy(BitConverter.GetBytes(c1), 0, dst, dstPos + 2, 2);
                Array.Copy(BitConverter.GetBytes(lookup), 0, dst, dstPos + 4, 4);
            }
        }

        private static byte[] BuildDxtPalette(ushort c0, ushort c1)
        {
            byte[] pal = new byte[16];
            byte r0, g0, b0, r1, g1, b1;
            DecodeRgb565(c0, out r0, out g0, out b0);
            DecodeRgb565(c1, out r1, out g1, out b1);
            pal[0] = 255; pal[1] = r0; pal[2] = g0; pal[3] = b0;
            pal[4] = 255; pal[5] = r1; pal[6] = g1; pal[7] = b1;
            if (c0 > c1)
            {
                pal[8] = 255; pal[9] = (byte)((2 * r0 + r1) / 3); pal[10] = (byte)((2 * g0 + g1) / 3); pal[11] = (byte)((2 * b0 + b1) / 3);
                pal[12] = 255; pal[13] = (byte)((r0 + 2 * r1) / 3); pal[14] = (byte)((g0 + 2 * g1) / 3); pal[15] = (byte)((b0 + 2 * b1) / 3);
            }
            else
            {
                pal[8] = 255; pal[9] = (byte)((r0 + r1) / 2); pal[10] = (byte)((g0 + g1) / 2); pal[11] = (byte)((b0 + b1) / 2);
                pal[12] = 0; pal[13] = 0; pal[14] = 0; pal[15] = 0;
            }
            return pal;
        }

        private static byte ExtractMasked(uint value, uint mask)
        {
            if (mask == 0) return 0;
            int shift = 0;
            uint temp = mask;
            while ((temp & 1) == 0) { shift++; temp >>= 1; }
            int bits = 0;
            while ((temp & 1) != 0) { bits++; temp >>= 1; }
            uint raw = (value & mask) >> shift;
            uint max = (uint)((1 << bits) - 1);
            return (byte)((raw * 255 + (max / 2)) / max);
        }

        private static byte GetChannel(byte[] rgba, int width, int height, int x, int y, int channel)
        {
            if (x < 0) x = 0;
            if (y < 0) y = 0;
            if (x >= width) x = width - 1;
            if (y >= height) y = height - 1;
            return rgba[((y * width + x) * 4) + channel];
        }

        private static void SetPixel(byte[] rgba, int width, int height, int x, int y, byte a, byte r, byte g, byte b)
        {
            if (x < 0 || y < 0 || x >= width || y >= height) return;
            int p = (y * width + x) * 4;
            rgba[p] = a; rgba[p + 1] = r; rgba[p + 2] = g; rgba[p + 3] = b;
        }

        private static byte Expand4(int value) { return (byte)((value << 4) | value); }
        private static int Align(int value, int alignment) { return ((value + alignment - 1) / alignment) * alignment; }

        private static byte ToIntensity8(byte[] rgba, int width, int height, int x, int y)
        {
            return (byte)((GetChannel(rgba, width, height, x, y, 1) + GetChannel(rgba, width, height, x, y, 2) + GetChannel(rgba, width, height, x, y, 3) + 1) / 3);
        }

        private static byte ToIntensity4(byte[] rgba, int width, int height, int x, int y)
        {
            return (byte)((ToIntensity8(rgba, width, height, x, y) * 15 + 127) / 255);
        }

        private static byte ToAlpha4(byte[] rgba, int width, int height, int x, int y)
        {
            return (byte)((GetChannel(rgba, width, height, x, y, 0) * 15 + 127) / 255);
        }

        private static void DecodeRgb565(ushort c, out byte r, out byte g, out byte b)
        {
            r = (byte)((((c >> 11) & 0x1F) * 255 + 15) / 31);
            g = (byte)((((c >> 5) & 0x3F) * 255 + 31) / 63);
            b = (byte)(((c & 0x1F) * 255 + 15) / 31);
        }

        private static ushort EncodeRgb565(byte r, byte g, byte b)
        {
            return (ushort)((((r * 31 + 127) / 255) << 11) | (((g * 63 + 127) / 255) << 5) | ((b * 31 + 127) / 255));
        }

        private static void DecodeRgb5A3(ushort c, out byte a, out byte r, out byte g, out byte b)
        {
            if ((c & 0x8000) != 0)
            {
                a = 255;
                r = (byte)((((c >> 10) & 0x1F) * 255 + 15) / 31);
                g = (byte)((((c >> 5) & 0x1F) * 255 + 15) / 31);
                b = (byte)(((c & 0x1F) * 255 + 15) / 31);
            }
            else
            {
                a = (byte)((((c >> 12) & 0x7) * 255 + 3) / 7);
                r = (byte)((((c >> 8) & 0xF) * 255 + 7) / 15);
                g = (byte)((((c >> 4) & 0xF) * 255 + 7) / 15);
                b = (byte)(((c & 0xF) * 255 + 7) / 15);
            }
        }

        private static ushort EncodeRgb5A3(byte a, byte r, byte g, byte b)
        {
            if (a < 224)
            {
                return (ushort)((((a * 7 + 127) / 255) << 12) | (((r * 15 + 127) / 255) << 8) | (((g * 15 + 127) / 255) << 4) | ((b * 15 + 127) / 255));
            }
            return (ushort)(0x8000 | (((r * 31 + 127) / 255) << 10) | (((g * 31 + 127) / 255) << 5) | ((b * 31 + 127) / 255));
        }

        private static bool TryParseContainer(string path, out byte[] data, out int version, out int offSizes, out List<int> tplOffsets, out bool alt)
        {
            data = File.ReadAllBytes(path);
            version = 0;
            offSizes = 0;
            tplOffsets = new List<int>();
            alt = false;

            if (data.Length < 8 || !Slice(data, 0, 4).SequenceEqual(HeaderMagic)) return false;

            version = BitConverter.ToInt32(data, 4);
            if (version == 4)
            {
                int @base = 64;
                int length = BitConverter.ToInt32(data, @base);
                int prefixBase = @base + 4 + length;
                int normalOffset = prefixBase + 0x2C;
                int sizeAtNormal = BitConverter.ToInt32(data, normalOffset);
                offSizes = sizeAtNormal == 0 ? (prefixBase + 0xA6) : normalOffset;
                alt = sizeAtNormal == 0;
            }
            else if (version == 2)
            {
                int @base = 32;
                int length = BitConverter.ToInt32(data, @base);
                offSizes = @base + 4 + length + 0x2B;
            }
            else if (version == 7 || version == 5)
            {
                byte[] dxt = Encoding.ASCII.GetBytes("DXT");
                int idx = IndexOf(data, dxt);
                if (idx < 0) return false;
                offSizes = idx + (version == 7 ? 0x21 : 0x1D);
            }
            else return false;

            tplOffsets = FindAll(data, TplMagic);
            return tplOffsets.Count > 0;
        }

        private static Resolution? ReadTplResolution(byte[] data, int tplOffset)
        {
            try
            {
                if (tplOffset + 12 > data.Length) return null;
                uint id = ReadUInt32BE(data, tplOffset);
                if (id != 0x0020AF30) return null;
                uint images = ReadUInt32BE(data, tplOffset + 4);
                uint tableOffset = ReadUInt32BE(data, tplOffset + 8);
                if (images == 0) return null;
                uint imgHeaderOffset = ReadUInt32BE(data, tplOffset + (int)tableOffset);
                ushort height = ReadUInt16BE(data, tplOffset + (int)imgHeaderOffset);
                ushort width = ReadUInt16BE(data, tplOffset + (int)imgHeaderOffset + 2);
                Resolution r = new Resolution();
                r.Width = width * 2;
                r.Height = height * 2;
                return r;
            }
            catch { return null; }
        }

        private static Dictionary<string, string> ParseFntPairs(string line)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string part in parts.Skip(1))
            {
                int idx = part.IndexOf('=');
                if (idx <= 0) continue;
                dict[part.Substring(0, idx)] = part.Substring(idx + 1).Trim('"');
            }
            return dict;
        }

        private static int ParseInt(string value) { return int.Parse(value, CultureInfo.InvariantCulture); }
        private static float ParseFloat(string value) { return float.Parse(value, CultureInfo.InvariantCulture); }

        private static ushort ReadUInt16BE(byte[] data, int offset) { return (ushort)((data[offset] << 8) | data[offset + 1]); }
        private static uint ReadUInt32BE(byte[] data, int offset) { return (uint)((data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3]); }

        private static void WriteUInt16BE(byte[] data, int offset, ushort value)
        {
            data[offset] = (byte)(value >> 8);
            data[offset + 1] = (byte)value;
        }

        private static void WriteUInt32BE(byte[] data, int offset, uint value)
        {
            data[offset] = (byte)(value >> 24);
            data[offset + 1] = (byte)(value >> 16);
            data[offset + 2] = (byte)(value >> 8);
            data[offset + 3] = (byte)value;
        }

        private static byte[] Slice(byte[] source, int offset, int len)
        {
            if (offset < 0 || len < 0 || offset + len > source.Length) return new byte[0];
            byte[] result = new byte[len];
            Array.Copy(source, offset, result, 0, len);
            return result;
        }

        private static byte[] Concat(IEnumerable<byte[]> arrays)
        {
            int len = 0;
            foreach (byte[] a in arrays)
            {
                if (a != null) len += a.Length;
            }
            byte[] output = new byte[len];
            int pos = 0;
            foreach (byte[] arr in arrays)
            {
                if (arr == null) continue;
                Buffer.BlockCopy(arr, 0, output, pos, arr.Length);
                pos += arr.Length;
            }
            return output;
        }

        private static int IndexOf(byte[] data, byte[] target)
        {
            for (int i = 0; i <= data.Length - target.Length; i++)
            {
                bool ok = true;
                for (int j = 0; j < target.Length; j++)
                {
                    if (data[i + j] != target[j]) { ok = false; break; }
                }
                if (ok) return i;
            }
            return -1;
        }

        private static List<int> FindAll(byte[] data, byte[] target)
        {
            var offsets = new List<int>();
            for (int i = 0; i <= data.Length - target.Length; i++)
            {
                bool ok = true;
                for (int j = 0; j < target.Length; j++)
                {
                    if (data[i + j] != target[j]) { ok = false; break; }
                }
                if (ok) offsets.Add(i);
            }
            return offsets;
        }
    }
}
