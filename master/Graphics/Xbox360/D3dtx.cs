using System;
using System.IO;
using System.Text;

namespace TTG_Tools.Graphics.Xbox360
{
    public enum TexKind
    {
        RawTiled,    // numeric mD3DFormat (Xbox 360): raw tiled, byte-swapped DXT
        Compressed,  // ASCII FourCC mD3DFormat ("DXT1"/"DXT5"): "Type 2" XMemCompress (LZX) payload
        Unknown
    }

    /// <summary>
    /// Header parser for a .d3dtx file from the Telltale Tool ("ERTM"
    /// meta-stream), as used in The Walking Dead on Xbox 360.
    /// </summary>
    public sealed class D3dtx
    {
        public string FilePath;
        public string Name;
        public uint NumMipLevels;
        public byte[] FormatRaw = new byte[4];
        public int Width;
        public int Height;
        public TexKind Kind;
        public string DxtName;       // "DXT1" / "DXT3" / "DXT5"
        public int BytesPerBlock;    // 8 (DXT1) or 16 (DXT3/5)
        public long FileLength;
        public long PixelDataOffset; // start of the pixel data (RawTiled only)
        public long PixelDataSize;
        public long Type2PayloadOffset = -1; // start of the "Type 2" payload (Compressed)
        public string Notes = "";

        public static D3dtx Parse(string path)
        {
            return ParseBytes(File.ReadAllBytes(path), path);
        }

        public static D3dtx ParseBytes(byte[] d, string path)
        {
            var t = new D3dtx { FilePath = path, FileLength = d.Length };

            if (d.Length < 16 || d[0] != 'E' || d[1] != 'R' || d[2] != 'T' || d[3] != 'M')
                throw new InvalidDataException("Not a valid Telltale 'ERTM' file.");

            int o = 4;
            uint classCount = U32(d, ref o);
            o += (int)classCount * 12;          // class table (crc64 + version)

            // mSamplerState: variable-size block. The block size includes the
            // 4 size bytes themselves, so the payload is (blockSize - 4) bytes.
            // For .d3dtx the block is 8 bytes (4 + 4). For .font it is larger
            // because the font header carries the font name + metrics there.
            int samplerBlockSize = (int)U32(d, ref o);
            int samplerPayload = samplerBlockSize - 4;
            if (samplerPayload < 0 || o + samplerPayload > d.Length)
                throw new InvalidDataException("Invalid mSamplerState block size.");
            o += samplerPayload;

            U32(d, ref o);                      // mName block size
            uint nameLen = U32(d, ref o);
            t.Name = Encoding.ASCII.GetString(d, o, (int)nameLen); o += (int)nameLen;

            U32(d, ref o);                      // mImportName block size
            uint impLen = U32(d, ref o);
            o += (int)impLen;                   // mImportName (usually empty)

            o += 3;                             // mToolProps, mbHasTextureData, mbIsMipMapped (ASCII '0'/'1')

            t.NumMipLevels = U32(d, ref o);
            Array.Copy(d, o, t.FormatRaw, 0, 4); o += 4;
            int pWidth = o;                       // offset of the mWidth field
            t.Width = (int)U32(d, ref o);
            t.Height = (int)U32(d, ref o);

            Classify(t, d, pWidth);
            return t;
        }

        private static void Classify(D3dtx t, byte[] d, int pWidth)
        {
            byte[] f = t.FormatRaw;
            bool ascii = f[0] >= 0x20 && f[0] < 0x7F && f[1] >= 0x20 && f[1] < 0x7F
                       && f[2] >= 0x20 && f[2] < 0x7F && f[3] >= 0x20 && f[3] < 0x7F;

            // Numeric mD3DFormat 0x52/0x53/0x54 = raw tiled DXT atlas (at end of file).
            if (!ascii && (f[0] == 0x52 || f[0] == 0x53 || f[0] == 0x54))
            {
                switch (f[0])
                {
                    case 0x52: t.DxtName = "DXT1"; t.BytesPerBlock = 8; break;
                    case 0x53: t.DxtName = "DXT3"; t.BytesPerBlock = 16; break;
                    default:   t.DxtName = "DXT5"; t.BytesPerBlock = 16; break;
                }
                t.Kind = TexKind.RawTiled;
                int padW = Align(t.Width, 128);
                int padH = Align(t.Height, 128);
                long size = (long)(padW / 4) * (padH / 4) * t.BytesPerBlock;
                t.PixelDataSize = size;
                t.PixelDataOffset = t.FileLength - size;
                if (t.PixelDataOffset < 0)
                {
                    t.Kind = TexKind.Unknown;
                    t.Notes = "File length smaller than the expected pixel data.";
                }
                return;
            }

            // Anything else: try the "Type 2" texture by locating the fetch
            // constant. Works for ASCII mD3DFormat ("DXT1"/"DXT5") and also
            // for files whose mD3DFormat field is unreliable (the _nm
            // variants, value 0x16): the real format and dimensions come
            // from the Xbox 360 fetch constant.
            if (TryClassifyType2(t, d, pWidth))
                return;

            t.Kind = TexKind.Unknown;
            t.DxtName = ascii ? Encoding.ASCII.GetString(f) : "0x" + f[0].ToString("X2");
            t.Notes = "Unrecognized format (mD3DFormat 0x" +
                      f[0].ToString("X2") + ") and no Type 2 payload.";
        }

        /// <summary>
        /// Locates the "Type 2" payload (fetch constant 46 AF 00 81) and reads
        /// the REAL format and dimensions from it. The d3dtx header field
        /// mD3DFormat is ignored because it is inconsistent for _nm variants.
        /// </summary>
        private static bool TryClassifyType2(D3dtx t, byte[] d, int pWidth)
        {
            long po = -1;
            for (int s = pWidth; s < pWidth + 1024 && s + 24 <= d.Length; s++)
                if (HasSig(d, s)) { po = s; break; }
            if (po < 0)
                return false;

            // Xbox 360 fetch constant (24 bytes): byte +7 = format,
            // bytes +9..+11 = packed dimensions (BE 24 bits).
            int fmtByte = d[po + 7];
            int dims = (d[po + 9] << 16) | (d[po + 10] << 8) | d[po + 11];
            int w = (dims & 0x1FFF) + 1;
            int h = (dims >> 13) + 1;

            string dxt;
            int bpb;
            if (fmtByte == 0x91) { dxt = "DXT1"; bpb = 8; }
            else if (fmtByte == 0xA1) { dxt = "DXT5"; bpb = 16; }
            else
            {
                // unknown fetch format: fall back to chunk count
                // (2 = DXT1, 4 = DXT5).
                int nc = CountChunks(d, (int)po + 24);
                if (nc == 2) { dxt = "DXT1"; bpb = 8; }
                else { dxt = "DXT5"; bpb = 16; }
            }

            t.Kind = TexKind.Compressed;
            t.Type2PayloadOffset = po;
            t.DxtName = dxt;
            t.BytesPerBlock = bpb;
            if (w > 0 && h > 0 && w <= 8192 && h <= 8192)
            {
                t.Width = w;
                t.Height = h;
            }
            t.Notes = "Type 2 texture (XMemCompress/LZX) @0x" + po.ToString("X") +
                      " - format " + dxt + " (from fetch constant).";
            return true;
        }

        // Counts the Type 2 chunks of the payload ([4B BE size][data]...).
        private static int CountChunks(byte[] d, int start)
        {
            int o = start, n = 0;
            while (o + 4 <= d.Length)
            {
                int z = (d[o] << 24) | (d[o + 1] << 16) | (d[o + 2] << 8) | d[o + 3];
                o += 4;
                if (z <= 0 || o + z > d.Length) break;
                o += z;
                n++;
            }
            return n;
        }

        // Checks the Xbox 360 fetch constant signature (46 AF 00 81).
        private static bool HasSig(byte[] d, long o)
        {
            return o >= 0 && o + 4 <= d.Length &&
                   d[o] == 0x46 && d[o + 1] == 0xAF && d[o + 2] == 0x00 && d[o + 3] == 0x81;
        }

        public static int Align(int v, int a) => (v + a - 1) / a * a;

        private static uint U32(byte[] d, ref int o)
        {
            uint v = (uint)(d[o] | d[o + 1] << 8 | d[o + 2] << 16 | d[o + 3] << 24);
            o += 4;
            return v;
        }
    }
}
