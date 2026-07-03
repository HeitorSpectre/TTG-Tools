using System;
using System.Collections.Generic;
using System.IO;

namespace TTG_Tools.Graphics.Xbox360
{
    /// <summary>
    /// Codec for the Telltale Xbox 360 "Type 2" textures.
    ///
    /// Format (reverse engineered and validated byte-for-byte against the
    /// game's internal decoder via instrumented Xenia):
    ///
    ///   [fetch constant 24B]
    ///   [4B BE: size0][chunk0]   -> plane 0 (e.g. DXT color dwords)
    ///   [4B BE: size1][chunk1]   -> plane 1 (e.g. DXT index dwords)
    ///
    /// Each chunk is an XMemCompress stream (Microsoft's XCompress / LZX):
    ///   - normal frame:  [2B BE comp][comp bytes]        -> 32768 bytes
    ///   - last frame:    [FF][2B BE unc][2B BE comp][comp bytes]
    ///   - terminator:    [00 00]
    /// De-framing means concatenating the payloads; LZX then decompresses
    /// everything.
    ///
    /// The DXT is stored "planar" (de-interleaved): one chunk per byte-range
    /// of the DXT block. Re-interleaving the planes yields the Xbox 360 tiled
    /// DXT buffer - identical to the GPU's texture memory.
    /// </summary>
    public static class TelltaleType2
    {
        /// <summary>
        /// Decodes the Type 2 payload and returns the (interleaved) tiled DXT
        /// buffer, ready for untile + DXT decoding.
        /// </summary>
        public static byte[] DecodeToTiledDxt(D3dtx t)
        {
            if (t.Type2PayloadOffset < 0)
                throw new InvalidDataException("Type 2 d3dtx without a located payload.");

            byte[] file = File.ReadAllBytes(t.FilePath);
            int o = (int)t.Type2PayloadOffset + 24;   // skip the fetch constant

            // split into chunks: [4B BE size][data]...
            var chunks = new List<byte[]>();
            while (o + 4 <= file.Length)
            {
                int z = (file[o] << 24) | (file[o + 1] << 16) | (file[o + 2] << 8) | file[o + 3];
                o += 4;
                if (z <= 0 || o + z > file.Length) break;
                byte[] c = new byte[z];
                Array.Copy(file, o, c, 0, z);
                chunks.Add(c);
                o += z;
            }
            if (chunks.Count == 0)
                throw new InvalidDataException("Type 2: no valid chunks.");

            // decompress each chunk (de-frame XMemCompress + LZX)
            var planes = new byte[chunks.Count][];
            for (int i = 0; i < chunks.Count; i++)
                planes[i] = DecompressChunk(chunks[i]);

            // re-interleave the planes to rebuild the tiled DXT
            return InterleavePlanes(planes, t.BytesPerBlock);
        }

        /// <summary>De-frames an XMemCompress chunk and runs LZX decompression.</summary>
        public static byte[] DecompressChunk(byte[] chunk)
        {
            var payload = new MemoryStream();
            int o = 0;
            int totalUncompressed = 0;
            while (o < chunk.Length)
            {
                int comp;
                int unc;
                if (chunk[o] == 0xFF)
                {
                    if (o + 5 > chunk.Length) break;
                    unc = (chunk[o + 1] << 8) | chunk[o + 2];
                    comp = (chunk[o + 3] << 8) | chunk[o + 4];
                    o += 5;
                }
                else
                {
                    if (o + 2 > chunk.Length) break;
                    comp = (chunk[o] << 8) | chunk[o + 1];
                    unc = 0x8000;
                    o += 2;
                }
                if (comp == 0 || o + comp > chunk.Length) break;
                payload.Write(chunk, o, comp);
                o += comp;
                totalUncompressed += unc;
            }

            return LzxDecoder.DecompressAuto(payload.ToArray(), totalUncompressed);
        }

        /// <summary>
        /// Reassembles DXT blocks from the planes.
        ///
        /// The codec splits each DXT block into N "planes" - consecutive byte
        /// ranges - and stores each plane as a chunk, in block order. Each
        /// plane contributes <c>decompressed_size / number_of_blocks</c> bytes
        /// per block. Reconstruction concatenates the planes in chunk order:
        ///
        ///   DXT1 ( 8 B/block):  2 planes -> [4][4]      (colors, indices)
        ///   DXT5 (16 B/block):  4 planes -> [4][4][2][6]
        /// </summary>
        private static byte[] InterleavePlanes(byte[][] planes, int bytesPerBlock)
        {
            long total = 0;
            foreach (var p in planes) total += p.Length;
            int blocks = (int)(total / bytesPerBlock);
            if (blocks == 0)
                throw new InvalidDataException("Type 2: empty planes.");

            // bytes per block each plane provides
            int[] part = new int[planes.Length];
            int sum = 0;
            for (int i = 0; i < planes.Length; i++)
            {
                part[i] = planes[i].Length / blocks;
                sum += part[i];
            }
            if (sum != bytesPerBlock)
                throw new InvalidDataException(
                    "Type 2: plane sizes (" + sum + ") != block size (" +
                    bytesPerBlock + ").");

            byte[] outp = new byte[blocks * bytesPerBlock];
            for (int b = 0; b < blocks; b++)
            {
                int pos = b * bytesPerBlock;
                for (int i = 0; i < planes.Length; i++)
                {
                    Array.Copy(planes[i], b * part[i], outp, pos, part[i]);
                    pos += part[i];
                }
            }
            return outp;
        }

        // ===================== REINSERTION / REBUILD =====================

        /// <summary>Reads the raw compressed chunks of the Type 2 payload.</summary>
        public static byte[][] ExtractChunks(D3dtx t)
        {
            byte[] file = File.ReadAllBytes(t.FilePath);
            int o = (int)t.Type2PayloadOffset + 24;     // skip the fetch constant
            var list = new List<byte[]>();
            while (o + 4 <= file.Length)
            {
                int z = (file[o] << 24) | (file[o + 1] << 16) | (file[o + 2] << 8) | file[o + 3];
                o += 4;
                if (z <= 0 || o + z > file.Length) break;
                byte[] c = new byte[z];
                Array.Copy(file, o, c, 0, z);
                list.Add(c);
                o += z;
            }
            return list.ToArray();
        }

        /// <summary>Reads the chunks and decompresses them - returns the planes.</summary>
        public static byte[][] ExtractPlanes(D3dtx t)
        {
            byte[][] chunks = ExtractChunks(t);
            var planes = new byte[chunks.Length][];
            for (int i = 0; i < chunks.Length; i++)
                planes[i] = DecompressChunk(chunks[i]);
            return planes;
        }

        /// <summary>
        /// Reassembles the .d3dtx with a new set of chunks. Preserves byte-
        /// for-byte everything that is not the payload: header, fetch constant
        /// and any data after the chunks (present in the _nm variants). The
        /// payload size field (uint32 LE at fc-4) is rewritten.
        /// </summary>
        public static byte[] RebuildFromChunks(D3dtx t, IList<byte[]> chunks)
        {
            byte[] file = File.ReadAllBytes(t.FilePath);
            int fc = (int)t.Type2PayloadOffset;
            if (fc < 4 || fc + 24 > file.Length)
                throw new InvalidDataException("Invalid Type 2 payload for rebuild.");

            // original payload size (fetch constant + framed chunks)
            int origPayloadSize = file[fc - 4] | (file[fc - 3] << 8)
                                | (file[fc - 2] << 16) | (file[fc - 1] << 24);

            // new payload: fetch constant (24B, unchanged) + framed chunks
            var payload = new MemoryStream();
            payload.Write(file, fc, 24);
            foreach (byte[] c in chunks)
            {
                payload.WriteByte((byte)(c.Length >> 24));
                payload.WriteByte((byte)(c.Length >> 16));
                payload.WriteByte((byte)(c.Length >> 8));
                payload.WriteByte((byte)(c.Length & 0xFF));
                payload.Write(c, 0, c.Length);
            }
            byte[] payloadBytes = payload.ToArray();

            var outp = new MemoryStream();
            outp.Write(file, 0, fc - 4);                          // header
            int ps = payloadBytes.Length;                         // size field (LE)
            outp.WriteByte((byte)(ps & 0xFF));
            outp.WriteByte((byte)((ps >> 8) & 0xFF));
            outp.WriteByte((byte)((ps >> 16) & 0xFF));
            outp.WriteByte((byte)((ps >> 24) & 0xFF));
            outp.Write(payloadBytes, 0, payloadBytes.Length);     // fetch + chunks
            int trailStart = fc + origPayloadSize;                // data after the chunks
            if (trailStart < file.Length)
                outp.Write(file, trailStart, file.Length - trailStart);
            return outp.ToArray();
        }

        /// <summary>Reinserts from decompressed planes (re-encodes them in LZX).</summary>
        public static byte[] RebuildFromPlanes(D3dtx t, IList<byte[]> planes)
        {
            var chunks = new byte[planes.Count][];
            for (int i = 0; i < planes.Count; i++)
                chunks[i] = LzxEncoder.EncodeChunk(planes[i]);
            return RebuildFromChunks(t, chunks);
        }

        /// <summary>
        /// Splits a tiled DXT block buffer into the codec planes - exact
        /// inverse of <see cref="InterleavePlanes"/>.
        /// </summary>
        public static byte[][] SplitPlanes(byte[] tiledDxt, int bytesPerBlock)
        {
            int[] parts = bytesPerBlock == 8
                ? new[] { 4, 4 }
                : new[] { 4, 4, 2, 6 };
            int blocks = tiledDxt.Length / bytesPerBlock;
            var planes = new byte[parts.Length][];
            for (int i = 0; i < parts.Length; i++)
                planes[i] = new byte[blocks * parts[i]];
            for (int b = 0; b < blocks; b++)
            {
                int pos = b * bytesPerBlock;
                for (int i = 0; i < parts.Length; i++)
                {
                    Array.Copy(tiledDxt, pos, planes[i], b * parts[i], parts[i]);
                    pos += parts[i];
                }
            }
            return planes;
        }

        /// <summary>
        /// Reinserts a texture: takes the DXT block buffer (interleaved, as
        /// produced by <see cref="DecodeToTiledDxt"/>) and rebuilds the .d3dtx.
        /// </summary>
        public static byte[] RebuildD3dtx(D3dtx t, byte[] tiledDxt)
        {
            byte[][] planes = SplitPlanes(tiledDxt, t.BytesPerBlock);
            return RebuildFromPlanes(t, planes);
        }

        /// <summary>
        /// Reinserts a modified texture from an RGBA image (width x height
        /// equal to the original texture's): DXT-encodes it, generates mips,
        /// applies tiling/byte-swap, and rebuilds the .d3dtx.
        /// </summary>
        public static byte[] ReinsertImage(D3dtx t, byte[] rgba)
        {
            byte[] blocks = XboxTexture.BuildBlockBuffer(
                rgba, t.Width, t.Height, t.DxtName, t.BytesPerBlock);
            return RebuildD3dtx(t, blocks);
        }
    }
}
