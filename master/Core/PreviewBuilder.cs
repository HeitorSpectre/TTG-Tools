using System;
using System.Drawing;
using System.IO;
using System.Text;
using TTG_Tools.Graphics.DDS;

namespace TTG_Tools
{
    /// <summary>
    /// Builds an in-memory preview (image or decoded text) for a single file extracted from a
    /// Telltale archive, so Archive Unpacker can show textures, fonts, scripts and localization
    /// files without the user extracting them first.
    ///
    /// Decoding is delegated to the same workers the export tabs use (TextureWorker, LandbWorker,
    /// LangdbWorker, DlogWorker, ExportPROP, decryptLua). Those workers are file based and write
    /// to MainMenu.settings.pathForOutputFolder, so the builder round-trips through a private
    /// temp folder and swaps the global output folder under a lock — the same technique
    /// ForThreads uses — restoring it right after.
    /// </summary>
    public static class PreviewBuilder
    {
        public enum PreviewKind
        {
            None,       // recognized, but no preview could be produced
            Image,
            Text,
            Audio
        }

        public class PreviewResult
        {
            public PreviewKind Kind;
            public Bitmap Image;
            public string Text;
            public string Info; // short technical note, e.g. "1024 x 1024"

            //Containers that hold several images (Wii multi-page textures/fonts). Image is always
            //Pages[0] when set; the panel lets the user flip through the rest.
            public System.Collections.Generic.List<Bitmap> Pages;

            public AudioDecoder.DecodedAudio Audio;
        }

        //Workers write through MainMenu.settings.pathForOutputFolder (a global), so previews must
        //never run concurrently with each other while that global is swapped.
        private static readonly object workerLock = new object();

        private const int MaxTextChars = 1000000;

        public static PreviewResult Build(string fileName, byte[] data, byte[] key, int version)
        {
            return Build(fileName, data, key, version, null);
        }

        //isStale: true once the user has selected another file. Builds waiting for the shared
        //worker lock then give up instead of queueing behind a slow one, which made every later
        //texture/landb preview hang on "Loading preview..." until the app was restarted.
        public static PreviewResult Build(string fileName, byte[] data, byte[] key, int version, Func<bool> isStale)
        {
            currentIsStale = isStale;

            try
            {
                if (data == null || data.Length == 0) return new PreviewResult { Kind = PreviewKind.None };

                string ext = Methods.GetExtension(fileName).ToLower();

                switch (ext)
                {
                    case ".d3dtx":
                        return TexturePreview(fileName, data, key, version);

                    case ".dds":
                        return DdsPreview(data);

                    case ".png":
                    case ".jpg":
                    case ".jpeg":
                    case ".jpe":
                    case ".bmp":
                    case ".gif":
                    case ".tga":
                        return DirectImagePreview(data, ext);

                    case ".font":
                        return FontPreview(fileName, data, key, version);

                    case ".aud":
                    case ".vox":
                    case ".fsb":
                    case ".wav":
                    case ".ogg":
                    case ".mp3":
                        return AudioPreview(data, ext, key);

                    case ".txt":
                    case ".xml":
                    case ".html":
                    case ".htm":
                    case ".js":
                    case ".css":
                    case ".json":
                    case ".ini":
                    case ".yaml":
                    case ".yml":
                    case ".vers":
                    case ".log":
                    case ".cfg":
                    case ".csv":
                    case ".tsv":
                    case ".md":
                    case ".text":
                    case ".srt":
                    case ".bat":
                    case ".cmd":
                    case ".sh":
                    case ".py":
                    case ".inf":
                    case ".nfo":
                    case ".rtf":
                    case ".xsl":
                    case ".svg":
                    case ".shader":
                    case ".fx":
                    case ".hlsl":
                    case ".glsl":
                    case ".h":
                    case ".c":
                    case ".cpp":
                    case ".cs":
                        return TextPreview(data);

                    case ".lua":
                    case ".lenc":
                        return LuaPreview(data, key, version);

                    case ".landb":
                    case ".langdb":
                    case ".dlog":
                    case ".prop":
                        return WorkerTextPreview(fileName, data, ext, key, version);

                    default:
                        //Unknown extensions that are clearly plain text still get a text preview.
                        if (LooksLikePlainText(data)) return TextPreview(data);
                        return new PreviewResult { Kind = PreviewKind.None };
                }
            }
            catch
            {
                return new PreviewResult { Kind = PreviewKind.None };
            }
            finally
            {
                currentIsStale = null;
            }
        }

        [ThreadStatic] private static Func<bool> currentIsStale;

        private static bool IsStale()
        {
            Func<bool> f = currentIsStale;
            return f != null && f();
        }

        //Takes the worker lock, polling so a superseded preview can bail out while it waits.
        private static IDisposable AcquireWorkerLock()
        {
            while (!System.Threading.Monitor.TryEnter(workerLock, 50))
            {
                if (IsStale()) throw new OperationCanceledException();
            }

            if (IsStale())
            {
                System.Threading.Monitor.Exit(workerLock);
                throw new OperationCanceledException();
            }

            return new WorkerLockScope();
        }

        private sealed class WorkerLockScope : IDisposable
        {
            private bool released;

            public void Dispose()
            {
                if (released) return;
                released = true;
                System.Threading.Monitor.Exit(workerLock);
            }
        }

        //Strict sniff for files of unknown type: no NULs and almost only printable ASCII,
        //whitespace or UTF-8 multibyte sequences in the first 4 KB. Telltale binaries all carry
        //NULs or meta headers early, so they never pass.
        private static bool LooksLikePlainText(byte[] data)
        {
            if (data.Length < 2) return false;

            int sample = Math.Min(data.Length, 4096);
            int bad = 0;

            for (int i = 0; i < sample; i++)
            {
                byte b = data[i];
                if (b == 0) return false;
                if (b == 9 || b == 10 || b == 13 || (b >= 0x20 && b < 0x7F)) continue;
                if (b >= 0x80) continue; //UTF-8 / ANSI letters; DecodeText re-validates
                bad++;
            }

            if (bad > sample / 100) return false;

            string header = Encoding.ASCII.GetString(data, 0, Math.Min(4, data.Length));
            return header != "ERTM" && header != "5VSM" && header != "6VSM" && header != "NIBM" && header != "MBES" && header != "MBIN";
        }

        private static PreviewResult AudioPreview(byte[] data, string ext, byte[] key)
        {
            string error;
            AudioDecoder.DecodedAudio audio = AudioDecoder.Decode(data, ext, key, out error);

            if (audio == null || audio.Pcm == null || audio.Pcm.Length == 0)
                return new PreviewResult { Kind = PreviewKind.None, Info = error };

            double seconds = (double)audio.Pcm.Length / (audio.SampleRate * audio.Channels * 2);
            TimeSpan t = TimeSpan.FromSeconds(seconds);

            return new PreviewResult
            {
                Kind = PreviewKind.Audio,
                Audio = audio,
                Info = audio.Codec + "  |  " + audio.SampleRate + " Hz  |  " + (audio.Channels == 1 ? "mono" : audio.Channels + " ch")
                    + "  |  " + (int)t.TotalMinutes + ":" + t.Seconds.ToString("00")
            };
        }

        #region Temp folder plumbing

        private static string CreateTempDir()
        {
            string dir = Path.Combine(Path.GetTempPath(), "TTGTools_Preview", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            return dir;
        }

        private static void TryDeleteDir(string dir)
        {
            try { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
            catch { }
        }

        private static void RunWithTexturePreviewSettings(Action action)
        {
            using (AcquireWorkerLock())
            {
                bool savedExtractPng = MainMenu.settings != null && MainMenu.settings.extractTexturesAsPng;

                try
                {
                    if (MainMenu.settings != null) MainMenu.settings.extractTexturesAsPng = true;
                    action();
                }
                finally
                {
                    if (MainMenu.settings != null) MainMenu.settings.extractTexturesAsPng = savedExtractPng;
                }
            }
        }

        #endregion

        #region Texture (.d3dtx) preview

        private static PreviewResult TexturePreview(string fileName, byte[] data, byte[] key, int version)
        {
            string tempIn = CreateTempDir();
            string tempOut = CreateTempDir();

            try
            {
                string inputPath = Path.Combine(tempIn, Path.GetFileName(fileName.Replace('/', '\\')));
                File.WriteAllBytes(inputPath, data);

                byte[] keyCopy = key != null ? (byte[])key.Clone() : null;

                string workerMessage = null;
                RunWithTexturePreviewSettings(() => workerMessage = Graphics.TextureWorker.DoWork(inputPath, tempOut, true, false, ref keyCopy, version));

                PreviewResult res = LoadImageFromDir(tempOut, Path.GetFileNameWithoutExtension(inputPath));
                if (res != null)
                {
                    string format = ExtractFormatNote(workerMessage);
                    if (format != null) res.Info += "  |  " + format;
                    return res;
                }

                //Worker produced nothing usable; try any embedded DDS texture before giving up.
                res = EmbeddedDdsPreview(data);
                if (res != null) return res;

                //The worker's message explains the failure (shown by the --preview-all sweep).
                return new PreviewResult { Kind = PreviewKind.None, Info = workerMessage };
            }
            finally
            {
                TryDeleteDir(tempIn);
                TryDeleteDir(tempOut);
            }
        }

        //TextureWorker reports "... Texture format: <name>. Mip count: ..." — surface the format
        //name in the preview info line.
        private static string ExtractFormatNote(string workerMessage)
        {
            if (string.IsNullOrEmpty(workerMessage)) return null;

            const string marker = "Texture format: ";
            int start = workerMessage.IndexOf(marker, StringComparison.Ordinal);
            if (start < 0) return null;

            start += marker.Length;
            int end = workerMessage.IndexOf(". ", start, StringComparison.Ordinal);
            if (end < 0) end = workerMessage.Length;

            string format = workerMessage.Substring(start, end - start).Trim().TrimEnd('.');
            return format.Length > 0 && format.Length < 60 ? format : null;
        }

        //Scans a worker output folder for a displayable texture, preferring formats losslessly
        //rendered by GDI+ before the DDS/TGA decoders. Workers that split a container into
        //several images (Wii: "name.tga", "name_1.tga"..., "name_alpha.tga") produce a
        //multi-page result the panel can flip through.
        private static PreviewResult LoadImageFromDir(string dir, string baseName = null)
        {
            if (!Directory.Exists(dir)) return null;

            string[] priority = { ".png", ".bmp", ".jpg", ".dds", ".tga", ".pvr" };

            foreach (string wanted in priority)
            {
                var files = new System.Collections.Generic.List<string>(Directory.GetFiles(dir, "*" + wanted, SearchOption.AllDirectories));
                if (files.Count == 0) continue;

                files.Sort((a, b) => PageOrder(a, baseName).CompareTo(PageOrder(b, baseName)));
                bool hasBase = baseName != null && files.Exists(f => string.Equals(Path.GetFileNameWithoutExtension(f), baseName, StringComparison.OrdinalIgnoreCase));

                var pages = new System.Collections.Generic.List<Bitmap>();

                foreach (string file in files)
                {
                    //Wii fonts write page 0 twice ("name.tga" and "name_0.tga").
                    if (hasBase && string.Equals(Path.GetFileNameWithoutExtension(file), baseName + "_0", StringComparison.OrdinalIgnoreCase)) continue;

                    try
                    {
                        byte[] bytes = File.ReadAllBytes(file);
                        PreviewResult res;

                        if (wanted == ".pvr")
                        {
                            pages.AddRange(PvrSurfaces(bytes));
                            continue;
                        }

                        if (wanted == ".dds") res = DdsPreview(bytes);
                        else if (wanted == ".tga") res = TgaPreview(bytes);
                        else res = DirectImagePreview(bytes, wanted);

                        if (res != null && res.Kind == PreviewKind.Image) pages.Add(res.Image);
                    }
                    catch { }
                }

                if (pages.Count == 0) continue;

                PreviewResult result = ImageResult(pages[0]);
                if (pages.Count > 1) result.Pages = pages;
                return result;
            }

            return null;
        }

        //PVR3 containers: PVRTC/uncompressed through the shared decoder, block-compressed texture
        //arrays (Batman lightmap atlases: DXT5 x 3 array members, PC and Switch) one page per
        //array member. PVR3 stores mip 0 of every surface first, back to back.
        private static System.Collections.Generic.List<Bitmap> PvrSurfaces(byte[] pvr)
        {
            var list = new System.Collections.Generic.List<Bitmap>();
            if (pvr == null || pvr.Length < 52 || pvr[0] != 'P' || pvr[1] != 'V' || pvr[2] != 'R' || pvr[3] != 3) return list;

            ulong pixelFormat = BitConverter.ToUInt64(pvr, 8);
            int height = (int)BitConverter.ToUInt32(pvr, 24);
            int width = (int)BitConverter.ToUInt32(pvr, 28);
            int surfaces = Math.Max(1, (int)BitConverter.ToUInt32(pvr, 36));
            int faces = Math.Max(1, (int)BitConverter.ToUInt32(pvr, 40));
            int dataOffset = 52 + (int)BitConverter.ToUInt32(pvr, 48);

            string fourCc = null;
            int blockBytes = 16;
            switch (pixelFormat)
            {
                case 7: fourCc = "DXT1"; blockBytes = 8; break;
                case 9: fourCc = "DXT3"; break;
                case 11: fourCc = "DXT5"; break;
                case 12: fourCc = "ATI1"; blockBytes = 8; break;
                case 13: fourCc = "ATI2"; break;
            }

            if (fourCc == null)
            {
                int w, h;
                byte[] rgba = Graphics.TextureWorker.DecodePvrContentToRgba(pvr, out w, out h);
                if (rgba != null) list.Add(BitmapFromRgba(rgba, w, h));
                return list;
            }

            int surfaceSize = ((width + 3) / 4) * ((height + 3) / 4) * blockBytes;

            for (int s = 0; s < surfaces * faces; s++)
            {
                int offset = dataOffset + s * surfaceSize;
                if (offset + surfaceSize > pvr.Length) break;

                byte[] dds = new byte[128 + surfaceSize];
                BuildDdsHeader(dds, width, height, fourCc);
                Array.Copy(pvr, offset, dds, 128, surfaceSize);

                PreviewResult res = DdsPreview(dds);
                if (res != null && res.Image != null) list.Add(res.Image);
            }

            return list;
        }

        //Sort key: the base image first, then numbered pages, then the separate alpha page. Only
        //the part after "<baseName>_" counts, so names that end in digits themselves
        //("avantgarde_lt_book_60") are not mistaken for page numbers.
        private static long PageOrder(string file, string baseName)
        {
            string stem = Path.GetFileNameWithoutExtension(file);
            if (baseName == null || !stem.StartsWith(baseName + "_", StringComparison.OrdinalIgnoreCase)) return 0;

            string suffix = stem.Substring(baseName.Length + 1);
            int n;
            if (int.TryParse(suffix, out n) && n >= 0) return 1 + n;
            if (suffix.Equals("alpha", StringComparison.OrdinalIgnoreCase)) return int.MaxValue;
            return 0;
        }

        #endregion

        #region DDS / plain image decoding

        private static PreviewResult DirectImagePreview(byte[] data, string ext)
        {
            if (ext == ".tga") return TgaPreview(data);

            try
            {
                using (MemoryStream ms = new MemoryStream(data))
                using (Image img = Image.FromStream(ms))
                {
                    Bitmap bmp = new Bitmap(img);
                    return ImageResult(bmp);
                }
            }
            catch
            {
                return null;
            }
        }

        private static PreviewResult DdsPreview(byte[] dds)
        {
            //BC1..BC5 (and DXTn fourCC) are handled by the shared converter.
            byte[] png = DdsPngConverter.DdsToPng(dds);
            if (png != null)
            {
                using (MemoryStream ms = new MemoryStream(png))
                using (Image img = Image.FromStream(ms))
                {
                    return ImageResult(new Bitmap(img));
                }
            }

            //Uncompressed DDS (BGRA32/RGB24/16bpp masks) is common in fonts — decode by masks.
            Bitmap bmp = DecodeUncompressedDds(dds);
            if (bmp != null) return ImageResult(bmp);

            return null;
        }

        private static Bitmap DecodeUncompressedDds(byte[] dds)
        {
            try
            {
                if (dds == null || dds.Length < 128) return null;
                if (dds[0] != 'D' || dds[1] != 'D' || dds[2] != 'S' || dds[3] != ' ') return null;

                int height = BitConverter.ToInt32(dds, 12);
                int width = BitConverter.ToInt32(dds, 16);
                uint pfFlags = BitConverter.ToUInt32(dds, 80);
                int bitCount = BitConverter.ToInt32(dds, 88);
                uint rMask = BitConverter.ToUInt32(dds, 92);
                uint gMask = BitConverter.ToUInt32(dds, 96);
                uint bMask = BitConverter.ToUInt32(dds, 100);
                uint aMask = BitConverter.ToUInt32(dds, 104);

                const uint DDPF_ALPHA = 0x2, DDPF_RGB = 0x40, DDPF_LUMINANCE = 0x20000;
                const uint DDPF_BUMPLUMINANCE = 0x40000, DDPF_BUMPDUDV = 0x80000;

                //Old Xbox 360 bump maps (Sam & Max) embed 32-bit DDS flagged as bump luminance/DuDv with
                //plain R/G/B masks; show them like RGB.
                bool isRgb = (pfFlags & (DDPF_RGB | DDPF_BUMPLUMINANCE | DDPF_BUMPDUDV)) != 0 && (rMask | gMask | bMask) != 0;
                bool isLum = (pfFlags & DDPF_LUMINANCE) != 0;
                bool isAlphaOnly = (pfFlags & DDPF_ALPHA) != 0 && !isRgb && !isLum;

                if (!isRgb && !isLum && !isAlphaOnly) return null;
                if (width <= 0 || height <= 0 || width > 16384 || height > 16384) return null;
                if (bitCount != 8 && bitCount != 16 && bitCount != 24 && bitCount != 32) return null;

                int bytesPerPixel = bitCount / 8;
                long needed = 128L + (long)width * height * bytesPerPixel;
                if (dds.Length < needed) return null;

                Bitmap bmp = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                var rect = new Rectangle(0, 0, width, height);
                var bits = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.WriteOnly, bmp.PixelFormat);

                try
                {
                    byte[] row = new byte[width * 4];
                    int src = 128;

                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            uint pixel = 0;
                            for (int b = 0; b < bytesPerPixel; b++) pixel |= (uint)dds[src + b] << (8 * b);
                            src += bytesPerPixel;

                            byte r, g, bl, a;

                            if (isLum)
                            {
                                byte lum = ExtractChannel(pixel, rMask != 0 ? rMask : 0xFFu);
                                r = g = bl = lum;
                                a = aMask != 0 ? ExtractChannel(pixel, aMask) : (byte)255;
                            }
                            else if (isAlphaOnly)
                            {
                                r = g = bl = 255;
                                a = ExtractChannel(pixel, aMask != 0 ? aMask : 0xFFu);
                            }
                            else
                            {
                                r = ExtractChannel(pixel, rMask);
                                g = ExtractChannel(pixel, gMask);
                                bl = ExtractChannel(pixel, bMask);
                                a = aMask != 0 ? ExtractChannel(pixel, aMask) : (byte)255;
                            }

                            int o = x * 4;
                            row[o] = bl; row[o + 1] = g; row[o + 2] = r; row[o + 3] = a;
                        }

                        System.Runtime.InteropServices.Marshal.Copy(row, 0, bits.Scan0 + y * bits.Stride, row.Length);
                    }
                }
                finally
                {
                    bmp.UnlockBits(bits);
                }

                return bmp;
            }
            catch
            {
                return null;
            }
        }

        //Extracts one color channel from a masked pixel value and rescales it to 0..255.
        private static byte ExtractChannel(uint pixel, uint mask)
        {
            if (mask == 0) return 0;

            int shift = 0;
            uint m = mask;
            while ((m & 1) == 0) { m >>= 1; shift++; }

            uint value = (pixel & mask) >> shift;
            if (m == 0xFF) return (byte)value;

            return (byte)(value * 255 / m);
        }

        private static PreviewResult TgaPreview(byte[] tga)
        {
            try
            {
                if (tga == null || tga.Length < 18) return null;

                int imageType = tga[2];
                if (imageType != 2 && imageType != 3) return null; //uncompressed truecolor/grayscale only

                int width = BitConverter.ToUInt16(tga, 12);
                int height = BitConverter.ToUInt16(tga, 14);
                int bpp = tga[16];
                bool topDown = (tga[17] & 0x20) != 0;
                int idLength = tga[0];

                if (width <= 0 || height <= 0 || (bpp != 24 && bpp != 32 && bpp != 8)) return null;

                int bytesPerPixel = bpp / 8;
                int src = 18 + idLength;
                if (tga.Length < src + width * height * bytesPerPixel) return null;

                Bitmap bmp = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                var bits = bmp.LockBits(new Rectangle(0, 0, width, height), System.Drawing.Imaging.ImageLockMode.WriteOnly, bmp.PixelFormat);

                try
                {
                    byte[] row = new byte[width * 4];

                    for (int y = 0; y < height; y++)
                    {
                        int destY = topDown ? y : height - 1 - y;

                        for (int x = 0; x < width; x++)
                        {
                            byte b, g, r, a;

                            if (bytesPerPixel == 1) { b = g = r = tga[src]; a = 255; }
                            else
                            {
                                b = tga[src]; g = tga[src + 1]; r = tga[src + 2];
                                a = bytesPerPixel == 4 ? tga[src + 3] : (byte)255;
                            }

                            src += bytesPerPixel;

                            int o = x * 4;
                            row[o] = b; row[o + 1] = g; row[o + 2] = r; row[o + 3] = a;
                        }

                        System.Runtime.InteropServices.Marshal.Copy(row, 0, bits.Scan0 + destY * bits.Stride, row.Length);
                    }
                }
                finally
                {
                    bmp.UnlockBits(bits);
                }

                return ImageResult(bmp);
            }
            catch
            {
                return null;
            }
        }

        //Transparency is kept: the preview panel paints a checkerboard behind the image.
        private static PreviewResult ImageResult(Bitmap bmp)
        {
            return new PreviewResult
            {
                Kind = PreviewKind.Image,
                Image = bmp,
                Info = bmp.Width + " x " + bmp.Height
            };
        }

        #endregion

        #region Font (.font) preview

        private static PreviewResult FontPreview(string fileName, byte[] data, byte[] key, int version)
        {
            //Parse the embedded glyph atlas the way Font Editor does (T3Texture blocks located by
            //their ".d3dtx" object name) — covers PC/iOS fonts of every era.
            PreviewResult res = FontAtlasPreview(data);
            if (res != null) return res;

            //Very old fonts store the atlas as a plain DDS blob — show that directly.
            res = EmbeddedDdsPreview(data);
            if (res != null) return res;

            //Console fonts (PS2/Wii/Xbox 360...) go through the texture pipeline, which knows how
            //to unswizzle them into PNG/DDS.
            string tempIn = CreateTempDir();
            string tempOut = CreateTempDir();

            try
            {
                string inputPath = Path.Combine(tempIn, Path.GetFileName(fileName.Replace('/', '\\')));
                File.WriteAllBytes(inputPath, data);

                byte[] keyCopy = key != null ? (byte[])key.Clone() : null;

                string workerMessage = null;
                try { RunWithTexturePreviewSettings(() => workerMessage = Graphics.TextureWorker.DoWork(inputPath, tempOut, true, false, ref keyCopy, version)); }
                catch (Exception ex) { workerMessage = ex.Message; }

                res = LoadImageFromDir(tempOut, Path.GetFileNameWithoutExtension(inputPath));
                if (res != null) return res;

                //Vector fonts carry an embedded TTF instead of an atlas — render a type specimen.
                res = VectorFontPreview(inputPath, tempOut);
                if (res != null) return res;

                return new PreviewResult { Kind = PreviewKind.None, Info = workerMessage };
            }
            finally
            {
                TryDeleteDir(tempIn);
                TryDeleteDir(tempOut);
            }
        }

        //Locates T3Texture blocks inside a .font and parses them with the same TextureWorker
        //readers Font Editor uses. The exact block position depends on layout details a full font
        //parse would be needed for, so candidate anchors are scanned instead and every boolean
        //layout variation (someData/flags/AddInfo) is tried — wrong guesses simply fail parsing
        //or the strict validation.
        private static PreviewResult FontAtlasPreview(byte[] data)
        {
            try
            {
                if (data == null || data.Length < 20) return null;

                string header = Encoding.ASCII.GetString(data, 0, 4);

                //Old mobile fonts ship meta-encrypted (like Font Editor, find the key and decrypt
                //before parsing anything).
                if (header != "ERTM" && header != "5VSM" && header != "6VSM" && header != "NIBM")
                {
                    try
                    {
                        byte[] copy = (byte[])data.Clone();
                        byte[] encKey = null;
                        int encVersion = 2;
                        string info = Methods.FindingDecrytKey(copy, "font", ref encKey, ref encVersion);
                        if (info == null) return null;

                        data = copy;
                        header = Encoding.ASCII.GetString(data, 0, 4);
                    }
                    catch
                    {
                        return null;
                    }
                }

                bool newFormat = header == "5VSM" || header == "6VSM" || HasNewTextureSignature(data);

                if (newFormat)
                {
                    //A new-format texture block begins with SomeValue, then two size-prefixed
                    //blocks whose sizes are always 8, the second carrying a known platform id —
                    //a strong 16-byte signature to anchor on.
                    for (int i = 4; i + 16 <= data.Length; i += 4)
                    {
                        if (BitConverter.ToInt32(data, i) != 8) continue;
                        if (BitConverter.ToInt32(data, i + 8) != 8) continue;

                        uint platform = BitConverter.ToUInt32(data, i + 12);
                        if (!IsKnownPlatform(platform)) continue;

                        PreviewResult res = TryNewFontTex(data, i - 4);
                        if (res != null) return res;
                    }

                    //Some layouts aren't 4-aligned relative to the header — rescan unaligned.
                    for (int i = 4; i + 16 <= data.Length; i++)
                    {
                        if (BitConverter.ToInt32(data, i) != 8) continue;
                        if (BitConverter.ToInt32(data, i + 8) != 8) continue;

                        uint platform = BitConverter.ToUInt32(data, i + 12);
                        if (!IsKnownPlatform(platform)) continue;

                        PreviewResult res = TryNewFontTex(data, i - 4);
                        if (res != null) return res;
                    }
                }
                else
                {
                    //Old-format blocks start with the length-prefixed texture object name; the
                    //name isn't predictable (PC uses "*.d3dtx", consoles use the bare font name),
                    //so try every length-prefixed printable string as an anchor.
                    foreach (int lenPos in FindStringPrefixes(data))
                    {
                        PreviewResult res = TryOldFontTex(data, lenPos);
                        if (res != null) return res;
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private static bool IsKnownPlatform(uint platform)
        {
            switch (platform)
            {
                case 2: case 4: case 5: case 7: case 9: case 11: case 13: case 15:
                    return true;
                default:
                    return false;
            }
        }

        private static bool HasNewTextureSignature(byte[] data)
        {
            for (int i = 4; i + 16 <= data.Length; i++)
            {
                if (BitConverter.ToInt32(data, i) != 8) continue;
                if (BitConverter.ToInt32(data, i + 8) != 8) continue;

                if (IsKnownPlatform(BitConverter.ToUInt32(data, i + 12))) return true;
            }

            return false;
        }

        //Positions of 4-byte length prefixes followed by that many printable ASCII characters.
        private static System.Collections.Generic.List<int> FindStringPrefixes(byte[] data)
        {
            var list = new System.Collections.Generic.List<int>();

            for (int i = 4; i + 4 <= data.Length; i++)
            {
                if (!IsNameChar(data[i])) continue;

                int nameStart = i;
                int nameEnd = i;
                while (nameEnd < data.Length && IsNameChar(data[nameEnd])) nameEnd++;

                for (int s = nameStart; s < nameEnd - 2 && s < nameStart + 2; s++)
                {
                    int nameLen = BitConverter.ToInt32(data, s - 4);

                    if (nameLen >= 3 && nameLen <= 96 && s + nameLen <= nameEnd)
                    {
                        list.Add(s - 4);
                    }
                }

                i = nameEnd;
            }

            return list;
        }

        private static bool IsNameChar(byte b)
        {
            return (b >= 'a' && b <= 'z') || (b >= 'A' && b <= 'Z') || (b >= '0' && b <= '9')
                || b == '_' || b == '-' || b == '.' || b == ' ' || b == '(' || b == ')';
        }

        private static PreviewResult TryOldFontTex(byte[] data, int lenPos)
        {
            int[] starts = { lenPos, lenPos - 4, lenPos - 8, lenPos - 12, lenPos - 16 };
            bool[] bools = { false, true };

            foreach (int start in starts)
            {
                if (start < 0) continue;

                foreach (bool someData in bools)
                {
                    foreach (bool flags in bools)
                    {
                        try
                        {
                            int poz = start;
                            var tex = Graphics.TextureWorker.GetOldTextures(data, ref poz, flags, someData);

                            if (tex == null || tex.Content == null || tex.Content.Length == 0) continue;
                            if (tex.Width < 4 || tex.Width > 16384 || tex.Height < 4 || tex.Height > 16384) continue;
                            if (tex.Content.Length > data.Length) continue;

                            PreviewResult res = DecodeFontTexContent(tex.Content, tex.TextureFormat, tex.Width, tex.Height);
                            if (res != null) return res;
                        }
                        catch { }
                    }
                }
            }

            return null;
        }

        private static PreviewResult TryNewFontTex(byte[] data, int start)
        {
            if (start < 0) return null;

            uint basePos = 0;
            string header = Encoding.ASCII.GetString(data, 0, 4);

            if (header == "5VSM" || header == "6VSM")
            {
                int headerSize = BitConverter.ToInt32(data, 4);
                int countElements = BitConverter.ToInt32(data, 16);
                if (headerSize < 0 || countElements < 0 || countElements > 1000) return null;

                basePos = (uint)headerSize + 16 + ((uint)countElements * 12) + 4;
            }
            bool[] bools = { false, true };

            foreach (bool someData in bools)
            {
                foreach (bool addInfo in bools)
                {
                    foreach (bool flags in bools)
                    {
                        try
                        {
                            int poz = start;
                            uint texFontPoz = basePos;
                            string fmt = "";
                            var tex = Graphics.TextureWorker.GetNewTextures(data, ref poz, ref texFontPoz, flags, someData, true, ref fmt, addInfo);

                            if (tex == null || tex.Tex.Content == null || tex.Tex.Content.Length == 0) continue;
                            if (tex.Width < 4 || tex.Width > 16384 || tex.Height < 4 || tex.Height > 16384) continue;

                            PreviewResult res = DecodeFontTexContent(tex.Tex.Content, tex.TextureFormat, tex.Width, tex.Height, tex.platform.platform == 9);
                            if (res != null) return res;
                        }
                        catch { }
                    }
                }
            }

            return null;
        }

        //Decodes a font atlas payload: PVR (iOS), a full DDS blob, headerless BCn (a minimal DDS
        //header is synthesized), raw RGBA or 8-bit luminance/alpha.
        private static PreviewResult DecodeFontTexContent(byte[] content, uint format, int width, int height, bool vita = false)
        {
            try
            {
                int pvrW, pvrH;
                byte[] pvrRgba = Graphics.TextureWorker.DecodePvrContentToRgba(content, out pvrW, out pvrH, false, vita);

                if (pvrRgba != null)
                {
                    return ImageResult(BitmapFromRgba(pvrRgba, pvrW, pvrH));
                }

                if (content.Length >= 4 && content[0] == 'D' && content[1] == 'D' && content[2] == 'S' && content[3] == ' ')
                {
                    return DdsPreview(content);
                }

                string fourCc = FourCcForTelltaleFormat(format);

                if (fourCc != null)
                {
                    byte[] dds = new byte[128 + content.Length];
                    BuildDdsHeader(dds, width, height, fourCc);
                    Array.Copy(content, 0, dds, 128, content.Length);
                    return DdsPreview(dds);
                }

                if (format == 0x16 || format == 0) //DX_ARGB8888 / NewTextureFormat.ARGB8 (R,G,B,A order)
                {
                    long needed = (long)width * height * 4;
                    if (content.Length < needed) return null;
                    return ImageResult(BitmapFromRgba(content, width, height));
                }

                if (format == 0x32 || format == 0x10 || format == 0x11) //DX_L8 / A8 / IL8
                {
                    long needed = (long)width * height;
                    if (content.Length < needed) return null;

                    byte[] rgba = new byte[width * height * 4];

                    for (int i = 0; i < width * height; i++)
                    {
                        byte v = content[i];
                        rgba[i * 4] = v; rgba[i * 4 + 1] = v; rgba[i * 4 + 2] = v; rgba[i * 4 + 3] = 255;
                    }

                    return ImageResult(BitmapFromRgba(rgba, width, height));
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private static string FourCcForTelltaleFormat(uint format)
        {
            switch (format)
            {
                case 0x40: case 0x31545844: return "DXT1";
                case 0x41: case 0x33545844: return "DXT3";
                case 0x42: case 0x35545844: return "DXT5";
                case 0x43: return "ATI1";
                case 0x44: return "ATI2";
                default: return null;
            }
        }

        private static void BuildDdsHeader(byte[] dds, int width, int height, string fourCc)
        {
            dds[0] = (byte)'D'; dds[1] = (byte)'D'; dds[2] = (byte)'S'; dds[3] = (byte)' ';
            WriteInt(dds, 4, 124);                        //dwSize
            WriteInt(dds, 8, 0x1007);                     //caps/height/width/pixelformat flags
            WriteInt(dds, 12, height);
            WriteInt(dds, 16, width);
            WriteInt(dds, 76, 32);                        //pixel format dwSize
            WriteInt(dds, 80, 0x4);                       //DDPF_FOURCC
            dds[84] = (byte)fourCc[0]; dds[85] = (byte)fourCc[1]; dds[86] = (byte)fourCc[2]; dds[87] = (byte)fourCc[3];
            WriteInt(dds, 108, 0x1000);                   //DDSCAPS_TEXTURE
        }

        private static void WriteInt(byte[] buf, int offset, int value)
        {
            byte[] b = BitConverter.GetBytes(value);
            Array.Copy(b, 0, buf, offset, 4);
        }

        //Content is in R,G,B,A byte order (Telltale/DecodePvrContentToRgba convention); the GDI+
        //32bppArgb buffer wants B,G,R,A.
        private static Bitmap BitmapFromRgba(byte[] rgba, int width, int height)
        {
            Bitmap bmp = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            var bits = bmp.LockBits(new Rectangle(0, 0, width, height), System.Drawing.Imaging.ImageLockMode.WriteOnly, bmp.PixelFormat);

            try
            {
                byte[] row = new byte[width * 4];

                for (int y = 0; y < height; y++)
                {
                    int src = y * width * 4;

                    for (int x = 0; x < width; x++)
                    {
                        int s = src + x * 4;
                        int d = x * 4;
                        row[d] = rgba[s + 2];
                        row[d + 1] = rgba[s + 1];
                        row[d + 2] = rgba[s];
                        row[d + 3] = rgba[s + 3];
                    }

                    System.Runtime.InteropServices.Marshal.Copy(row, 0, bits.Scan0 + y * bits.Stride, row.Length);
                }
            }
            finally
            {
                bmp.UnlockBits(bits);
            }

            return bmp;
        }

        //Finds "DDS " magics inside a raw blob and decodes the first one that renders.
        private static PreviewResult EmbeddedDdsPreview(byte[] data)
        {
            int start = 0;

            while (true)
            {
                int off = IndexOfDdsMagic(data, start);
                if (off < 0) return null;

                byte[] slice = new byte[data.Length - off];
                Array.Copy(data, off, slice, 0, slice.Length);

                PreviewResult res = DdsPreview(slice);
                if (res != null) return res;

                start = off + 4;
            }
        }

        private static int IndexOfDdsMagic(byte[] data, int start)
        {
            if (data == null) return -1;

            for (int i = start; i <= data.Length - 4; i++)
            {
                if (data[i] == 'D' && data[i + 1] == 'D' && data[i + 2] == 'S' && data[i + 3] == ' ') return i;
            }

            return -1;
        }

        private static PreviewResult VectorFontPreview(string inputPath, string tempOut)
        {
            try
            {
                using (AcquireWorkerLock())
                {
                    string savedOutput = MainMenu.settings.pathForOutputFolder;

                    try
                    {
                        MainMenu.settings.pathForOutputFolder = tempOut;
                        Graphics.FontWorker.DoWork(inputPath, true);
                    }
                    finally
                    {
                        MainMenu.settings.pathForOutputFolder = savedOutput;
                    }
                }

                string[] ttfs = Directory.GetFiles(tempOut, "*.ttf", SearchOption.AllDirectories);
                if (ttfs.Length == 0) return null;

                using (var fonts = new System.Drawing.Text.PrivateFontCollection())
                {
                    fonts.AddFontFile(ttfs[0]);
                    if (fonts.Families.Length == 0) return null;

                    FontFamily family = fonts.Families[0];

                    Bitmap bmp = new Bitmap(560, 320, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                    using (var g = System.Drawing.Graphics.FromImage(bmp))
                    {
                        g.Clear(Color.White);
                        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                        string sample = "AaBbCcDdEeFf\nGgHhIiJjKkLl\n0123456789\n!?.,:;\"'()[]";
                        FontStyle style = family.IsStyleAvailable(FontStyle.Regular) ? FontStyle.Regular : FontStyle.Bold;

                        using (Font f = new Font(family, 28f, style, GraphicsUnit.Pixel))
                        using (Brush brush = new SolidBrush(Color.Black))
                        {
                            g.DrawString(family.Name, f, brush, 12, 10);
                            g.DrawString(sample, f, brush, 12, 60);
                        }
                    }

                    return new PreviewResult
                    {
                        Kind = PreviewKind.Image,
                        Image = bmp,
                        Info = family.Name + " (TTF)"
                    };
                }
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region Text previews

        private static PreviewResult TextPreview(byte[] data)
        {
            string text = DecodeText(data);
            if (text == null) return new PreviewResult { Kind = PreviewKind.None };

            return new PreviewResult { Kind = PreviewKind.Text, Text = text };
        }

        private static PreviewResult LuaPreview(byte[] data, byte[] key, int version)
        {
            byte[] content = data;

            try
            {
                if (Methods.isLuaEncrypted(content))
                {
                    content = Methods.decryptLua((byte[])data.Clone(), key, version);
                }
            }
            catch
            {
                content = data;
            }

            if (content == null) return new PreviewResult { Kind = PreviewKind.None };
            if (content.Length > 4 && content[0] == 0x1B && content[1] == 'L' && content[2] == 'u' && content[3] == 'a')
            {
                string source = TryDecompileLuaSource(content);
                if (source == null) return new PreviewResult { Kind = PreviewKind.None };

                return new PreviewResult { Kind = PreviewKind.Text, Text = source, Info = "decompiled Lua" };
            }

            string text = DecodeText(content);
            if (text == null) return new PreviewResult { Kind = PreviewKind.None };

            return new PreviewResult { Kind = PreviewKind.Text, Text = text };
        }

        private static string TryDecompileLuaSource(byte[] compiledBytes)
        {
            try
            {
                int versionIndex = DetectLuaVersionFromBytes(compiledBytes);
                string dir = ToolsDirForLuaVersion(versionIndex);
                string jar = Path.Combine(dir, "unluac.jar");
                if (!File.Exists(jar)) return null;

                string tempDir = Path.Combine(Path.GetTempPath(), "TTGTools_PreviewLua_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempDir);

                try
                {
                    string inFile = Path.Combine(tempDir, "in.luac");
                    File.WriteAllBytes(inFile, compiledBytes);

                    int exitCode;
                    string stdout;
                    string stderr = RunProcess("java", "-jar \"" + jar + "\" \"" + inFile + "\"", tempDir, true, out exitCode, out stdout);

                    if (exitCode != 0 || string.IsNullOrWhiteSpace(stdout)) return null;
                    if (stdout.Length > MaxTextChars) stdout = stdout.Substring(0, MaxTextChars) + "\r\n[...]";

                    return stdout.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n");
                }
                finally
                {
                    TryDeleteDir(tempDir);
                }
            }
            catch
            {
                return null;
            }
        }

        private static int DetectLuaVersionFromBytes(byte[] compiledBytes)
        {
            if (compiledBytes != null && compiledBytes.Length >= 5
                && compiledBytes[0] == 0x1B && compiledBytes[1] == 'L' && compiledBytes[2] == 'u' && compiledBytes[3] == 'a')
            {
                switch (compiledBytes[4])
                {
                    case 0x50: return 0;
                    case 0x51: return 1;
                    case 0x52: return 2;
                }
            }

            int saved = MainMenu.settings != null ? MainMenu.settings.luaVersionIndex : 1;
            return saved < 0 || saved > 2 ? 1 : saved;
        }

        private static string ToolsDirForLuaVersion(int versionIndex)
        {
            string folder = versionIndex == 0 ? "LuaP Files" : (versionIndex == 1 ? "LuaQ Files" : "LuaR Files");
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LencTools", folder);
        }

        private static string RunProcess(string exe, string args, string workingDir, bool captureStdout, out int exitCode, out string stdout)
        {
            stdout = "";
            var outSb = new StringBuilder();
            var errSb = new StringBuilder();
            var psi = new System.Diagnostics.ProcessStartInfo(exe, args)
            {
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = captureStdout,
                CreateNoWindow = true,
                WorkingDirectory = workingDir
            };

            using (var p = System.Diagnostics.Process.Start(psi))
            {
                p.ErrorDataReceived += (s, e) => { if (e.Data != null) errSb.AppendLine(e.Data); };
                if (captureStdout) p.OutputDataReceived += (s, e) => { if (e.Data != null) outSb.AppendLine(e.Data); };

                p.BeginErrorReadLine();
                if (captureStdout) p.BeginOutputReadLine();

                //Poll instead of one 30 s wait so a superseded Lua decompile is killed right away.
                var waited = System.Diagnostics.Stopwatch.StartNew();
                while (!p.WaitForExit(100))
                {
                    if (IsStale() || waited.ElapsedMilliseconds > 30000) break;
                }
                exitCode = p.HasExited ? p.ExitCode : -1;
                if (!p.HasExited) try { p.Kill(); } catch { }

                try { p.WaitForExit(); } catch { }

                if (captureStdout) stdout = outSb.ToString();
                return errSb.ToString();
            }
        }

        //Runs the matching text worker (landb/langdb/dlog/prop) against a temp copy and returns
        //the produced .txt, using the same global-output-folder swap ForThreads relies on.
        private static PreviewResult WorkerTextPreview(string fileName, byte[] data, string ext, byte[] key, int version)
        {
            string tempIn = CreateTempDir();
            string tempOut = CreateTempDir();

            try
            {
                string inputPath = Path.Combine(tempIn, Path.GetFileName(fileName.Replace('/', '\\')));
                File.WriteAllBytes(inputPath, data);

                byte[] keyCopy = key != null ? (byte[])key.Clone() : null;

                using (AcquireWorkerLock())
                {
                    string savedOutput = MainMenu.settings.pathForOutputFolder;

                    try
                    {
                        MainMenu.settings.pathForOutputFolder = tempOut;

                        switch (ext)
                        {
                            case ".landb":
                                Texts.LandbWorker.DoWork(inputPath, "", true, keyCopy, version);
                                break;

                            case ".langdb":
                                Texts.LangdbWorker.DoWork(inputPath, "", true, false, ref keyCopy, version);
                                break;

                            case ".dlog":
                                int ver = version;
                                Texts.DlogWorker.DoWork(inputPath, "", true, ref keyCopy, ref ver);
                                break;

                            case ".prop":
                                var worker = new ForThreads();
                                string txtName = Path.GetFileNameWithoutExtension(inputPath) + ".txt";
                                worker.ExportPROP(new FileInfo(inputPath), txtName, tempOut);
                                break;
                        }
                    }
                    finally
                    {
                        MainMenu.settings.pathForOutputFolder = savedOutput;
                    }
                }

                //Workers name their outputs freely (some emit doubled files) — read whatever
                //text they produced.
                var produced = new System.Collections.Generic.List<string>();
                foreach (string file in Directory.GetFiles(tempOut, "*.txt", SearchOption.AllDirectories)) produced.Add(file);

                if (produced.Count == 0)
                {
                    foreach (string file in Directory.GetFiles(tempOut, "*.*", SearchOption.AllDirectories))
                    {
                        if (!string.Equals(file, inputPath, StringComparison.OrdinalIgnoreCase)) produced.Add(file);
                    }
                }

                if (produced.Count == 0) return new PreviewResult { Kind = PreviewKind.None };

                produced.Sort(StringComparer.OrdinalIgnoreCase);

                string text = DecodeText(File.ReadAllBytes(produced[0]));
                if (text == null) return new PreviewResult { Kind = PreviewKind.None };

                return new PreviewResult { Kind = PreviewKind.Text, Text = text };
            }
            catch
            {
                return new PreviewResult { Kind = PreviewKind.None };
            }
        }

        //Best-effort charset detection: BOM first, then UTF-16 heuristics, then strict UTF-8,
        //then the ANSI codepage configured in settings.
        private static string DecodeText(byte[] data)
        {
            try
            {
                if (data == null) return null;
                if (data.Length == 0) return "";

                Encoding enc = null;

                if (data.Length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF) enc = new UTF8Encoding(false);
                else if (data.Length >= 2 && data[0] == 0xFF && data[1] == 0xFE) enc = Encoding.Unicode;
                else if (data.Length >= 2 && data[0] == 0xFE && data[1] == 0xFF) enc = Encoding.BigEndianUnicode;

                if (enc == null)
                {
                    //A text file full of NUL bytes at odd positions is almost surely UTF-16 LE.
                    int sample = Math.Min(data.Length, 512);
                    int oddZeros = 0;
                    for (int i = 1; i < sample; i += 2) if (data[i] == 0) oddZeros++;
                    if (sample >= 8 && oddZeros > sample / 4) enc = Encoding.Unicode;
                }

                string text;

                if (enc != null)
                {
                    text = enc.GetString(data);
                }
                else
                {
                    try
                    {
                        text = new UTF8Encoding(false, true).GetString(data);
                    }
                    catch (DecoderFallbackException)
                    {
                        text = Encoding.GetEncoding(MainMenu.settings.ASCII_N).GetString(data);
                    }
                }

                if (text.Length > 0 && text[0] == '﻿') text = text.Substring(1);

                //Binary disguised as text isn't worth rendering in a TextBox.
                int check = Math.Min(text.Length, 2048);
                int controls = 0;
                for (int i = 0; i < check; i++)
                {
                    char c = text[i];
                    if (c < 0x20 && c != '\r' && c != '\n' && c != '\t') controls++;
                }
                if (check > 0 && controls > check / 20) return null;

                if (text.Length > MaxTextChars) text = text.Substring(0, MaxTextChars) + "\r\n[...]";

                return text.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n");
            }
            catch
            {
                return null;
            }
        }

        #endregion
    }
}
