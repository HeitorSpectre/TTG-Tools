using System;
using System.Collections.Generic;
using System.IO;

namespace TTG_Tools.Graphics.Xbox360
{
    /// <summary>
    /// Encodes data into a "Type 2" chunk (XMemCompress / LZX stream).
    ///
    /// Uses LZX UNCOMPRESSED blocks - i.e. it does NOT compress. The original
    /// compressor (Microsoft's XMemCompress) is proprietary and cannot be
    /// reproduced bit-for-bit, so reinsertion stores the data without
    /// compression. The result is a 100% valid LZX chunk: both this viewer's
    /// decoder and the game's XMemDecompress decode it correctly. The only
    /// difference from the original file is that the payload is larger
    /// (uncompressed).
    ///
    /// Layout of the LZX stream produced (1 UNCOMPRESSED block):
    ///   [16-byte header][raw bytes]
    ///   - bits: E8=0 (1) | type=3 (3) | size (24) | padding (4)  -> 32 bits
    ///   - 12 bytes: R0,R1,R2 = 1,1,1 (uint32 LE)
    /// This stream is then sliced into 32768-byte output frames and wrapped
    /// in the XMemCompress framing.
    /// </summary>
    public static class LzxEncoder
    {
        private const int FRAME = 32768;

        /// <summary>Builds the continuous LZX stream (1 UNCOMPRESSED block).</summary>
        private static byte[] BuildLzxStream(byte[] data)
        {
            // 32-bit header, bits MSB-first:
            //   bit 31      = E8 flag (0)
            //   bits 30..28 = block type (3 = UNCOMPRESSED)
            //   bits 27..4  = block size (24 bits)
            //   bits 3..0   = padding (0)
            uint hdr = (3u << 28) | ((uint)data.Length << 4);

            byte[] outp = new byte[16 + data.Length];
            // emitted as 2 16-bit words (high word first), each word written
            // little-endian (the decoder reads b0,b1).
            int w0 = (int)(hdr >> 16);
            int w1 = (int)(hdr & 0xFFFF);
            outp[0] = (byte)(w0 & 0xFF); outp[1] = (byte)(w0 >> 8);
            outp[2] = (byte)(w1 & 0xFF); outp[3] = (byte)(w1 >> 8);
            // 12 bytes of R0/R1/R2 = 1,1,1
            outp[4] = 1; outp[8] = 1; outp[12] = 1;
            // raw data
            Array.Copy(data, 0, outp, 16, data.Length);
            return outp;
        }

        /// <summary>
        /// Encodes 'data' into a complete Type 2 chunk (XMemCompress stream
        /// ready to go inside [4B BE size][chunk]).
        /// </summary>
        public static byte[] EncodeChunk(byte[] data)
        {
            byte[] lzx = BuildLzxStream(data);
            var outp = new MemoryStream();

            int produced = 0;          // output bytes already covered
            int lzxPos = 0;            // position in the LZX stream
            while (produced < data.Length)
            {
                int outSize = Math.Min(FRAME, data.Length - produced);
                // LZX bytes of this frame: the 16-byte header only exists in
                // the first frame; each frame covers 'outSize' output bytes.
                int lzxEnd = 16 + produced + outSize;
                int compLen = lzxEnd - lzxPos;

                if (outSize == FRAME)
                {
                    // normal frame: [2B BE compLen]
                    outp.WriteByte((byte)(compLen >> 8));
                    outp.WriteByte((byte)(compLen & 0xFF));
                }
                else
                {
                    // last partial frame: [FF][2B BE uncSize][2B BE compLen]
                    outp.WriteByte(0xFF);
                    outp.WriteByte((byte)(outSize >> 8));
                    outp.WriteByte((byte)(outSize & 0xFF));
                    outp.WriteByte((byte)(compLen >> 8));
                    outp.WriteByte((byte)(compLen & 0xFF));
                }
                outp.Write(lzx, lzxPos, compLen);
                lzxPos = lzxEnd;
                produced += outSize;
            }

            // terminator
            outp.WriteByte(0);
            outp.WriteByte(0);
            return outp.ToArray();
        }
    }
}
