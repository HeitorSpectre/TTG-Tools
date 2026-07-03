using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace TTG_Tools.Graphics.Xbox360
{
    /// <summary>
    /// Entry point for Xbox 360 .d3dtx / .font (Telltale "ERTM") textures.
    /// Auto-detects the two flavors used by Telltale's Xbox 360 builds:
    ///
    ///   - Type 1 "RawTiled": numeric mD3DFormat byte 0x52/0x53/0x54, the
    ///     raw tiled DXT atlas sits at the end of the file. No compression.
    ///
    ///   - Type 2 "Compressed": Xbox 360 fetch constant (signature
    ///     46 AF 00 81) preceding planar LZX (XMemCompress) chunks.
    ///
    /// Anything else returns false and the caller falls back to the normal
    /// .d3dtx pipeline.
    /// </summary>
    internal static class Xbox360Support
    {
        internal static bool TryExtractContainer(string inputPath, string outputDir, out string result)
        {
            result = null;
            D3dtx t;
            if (!TryParse(inputPath, out t)) return false;

            try
            {
                Directory.CreateDirectory(outputDir);
                string outPng = Path.Combine(outputDir,
                    Path.GetFileNameWithoutExtension(inputPath) + ".png");
                using (Bitmap bmp = XboxTexture.Decode(t))
                    bmp.Save(outPng, ImageFormat.Png);

                string flavor = t.Kind == TexKind.Compressed ? "compressed" : "raw tiled";
                result = "File " + Path.GetFileName(inputPath)
                       + " successfully extracted (Xbox 360 " + flavor + " "
                       + t.DxtName + " " + t.Width + "x" + t.Height + ").";
                return true;
            }
            catch (Exception ex)
            {
                result = "Xbox 360 extract failed for " + Path.GetFileName(inputPath)
                       + ": " + ex.Message;
                return true;
            }
        }

        internal static bool TryRepackContainer(string inputPath, string inputDir, string outputDir, out string result)
        {
            result = null;
            D3dtx t;
            if (!TryParse(inputPath, out t)) return false;

            string baseName = Path.GetFileNameWithoutExtension(inputPath);
            string png = Path.Combine(inputDir, baseName + ".png");
            if (!File.Exists(png)) return false; // nothing to repack with

            try
            {
                byte[] rgba = LoadPngAsRgba(png, t.Width, t.Height);
                byte[] rebuilt;
                if (t.Kind == TexKind.Compressed)
                    rebuilt = TelltaleType2.ReinsertImage(t, rgba);
                else
                    rebuilt = XboxTexture.ReinsertRawTiled(t, rgba);

                Directory.CreateDirectory(outputDir);
                string outPath = Path.Combine(outputDir, Path.GetFileName(inputPath));
                File.WriteAllBytes(outPath, rebuilt);

                string flavor = t.Kind == TexKind.Compressed ? "compressed" : "raw tiled";
                result = "File " + Path.GetFileName(inputPath)
                       + " successfully imported (Xbox 360 " + flavor + ").";
                return true;
            }
            catch (Exception ex)
            {
                result = "Xbox 360 repack failed for " + Path.GetFileName(inputPath)
                       + ": " + ex.Message;
                return true;
            }
        }

        /// <summary>
        /// Returns true when the file is an Xbox 360 Telltale texture (Type 1
        /// RawTiled or Type 2 Compressed). Returns false for any other ERTM
        /// file (PC textures, non-Xbox fonts, etc.).
        /// </summary>
        private static bool TryParse(string inputPath, out D3dtx t)
        {
            t = null;
            try
            {
                byte[] bytes = File.ReadAllBytes(inputPath);
                if (bytes.Length < 4 || bytes[0] != 'E' || bytes[1] != 'R'
                    || bytes[2] != 'T' || bytes[3] != 'M')
                    return false;

                D3dtx parsed = null;
                try { parsed = D3dtx.ParseBytes(bytes, inputPath); }
                catch { parsed = null; }

                if (parsed != null && (parsed.Kind == TexKind.RawTiled
                    || (parsed.Kind == TexKind.Compressed && parsed.Type2PayloadOffset >= 0)))
                {
                    t = parsed;
                    return true;
                }

                // Header parsing may misread a .font (different sub-block
                // layout) and miss the Type 2 fetch constant. As a fallback,
                // scan the whole file for the signature; if found, the file
                // is Xbox 360 Type 2 regardless of where the header parser
                // looked.
                long sig = FindFetchConstant(bytes);
                if (sig >= 0 && parsed != null)
                {
                    // Re-classify by forcing the located payload offset.
                    parsed.Kind = TexKind.Compressed;
                    parsed.Type2PayloadOffset = sig;
                    ReadFormatFromFetchConstant(bytes, sig, parsed);
                    t = parsed;
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private static long FindFetchConstant(byte[] d)
        {
            for (int i = 0; i + 24 <= d.Length; i++)
                if (d[i] == 0x46 && d[i + 1] == 0xAF && d[i + 2] == 0x00 && d[i + 3] == 0x81)
                    return i;
            return -1;
        }

        private static void ReadFormatFromFetchConstant(byte[] d, long po, D3dtx t)
        {
            int fmtByte = d[po + 7];
            int dims = (d[po + 9] << 16) | (d[po + 10] << 8) | d[po + 11];
            int w = (dims & 0x1FFF) + 1;
            int h = (dims >> 13) + 1;
            if (fmtByte == 0x91) { t.DxtName = "DXT1"; t.BytesPerBlock = 8; }
            else { t.DxtName = "DXT5"; t.BytesPerBlock = 16; }
            if (w > 0 && h > 0 && w <= 8192 && h <= 8192)
            {
                t.Width = w;
                t.Height = h;
            }
        }

        private static byte[] LoadPngAsRgba(string pngPath, int wExpected, int hExpected)
        {
            using (var bmp = new Bitmap(pngPath))
            {
                if (bmp.Width != wExpected || bmp.Height != hExpected)
                    throw new InvalidDataException("PNG is " + bmp.Width + "x" + bmp.Height
                        + " but the original texture is " + wExpected + "x" + hExpected + ".");

                int w = bmp.Width, h = bmp.Height;
                BitmapData bd = bmp.LockBits(new Rectangle(0, 0, w, h),
                    ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                byte[] bgra = new byte[w * h * 4];
                Marshal.Copy(bd.Scan0, bgra, 0, bgra.Length);
                bmp.UnlockBits(bd);

                byte[] rgba = new byte[w * h * 4];
                for (int i = 0; i < w * h; i++)
                {
                    rgba[i * 4]     = bgra[i * 4 + 2];
                    rgba[i * 4 + 1] = bgra[i * 4 + 1];
                    rgba[i * 4 + 2] = bgra[i * 4];
                    rgba[i * 4 + 3] = bgra[i * 4 + 3];
                }
                return rgba;
            }
        }
    }
}
