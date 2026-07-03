using System;
using System.IO;

namespace TTG_Tools.Graphics.Xbox360
{
    /// <summary>
    /// Microsoft LZX decoder - faithful port of <c>lzxd.c</c> from libmspack
    /// (used by Xenia). Single-shot mode: takes a whole de-framed LZX stream
    /// and returns the decompressed output.
    ///
    /// Telltale "Type 2" Xbox 360 textures are compressed with XMemCompress
    /// (Microsoft's XCompress), which is LZX underneath.
    /// </summary>
    public sealed class LzxDecoder
    {
        // ---- LZX constants ----
        private const int MIN_MATCH = 2;
        private const int MAX_MATCH = 257;
        private const int NUM_CHARS = 256;
        private const int BLOCKTYPE_VERBATIM = 1;
        private const int BLOCKTYPE_ALIGNED = 2;
        private const int BLOCKTYPE_UNCOMPRESSED = 3;
        private const int NUM_PRIMARY_LENGTHS = 7;
        private const int NUM_SECONDARY_LENGTHS = 249;
        private const int PRETREE_MAXSYMBOLS = 20;
        private const int PRETREE_TABLEBITS = 6;
        private const int MAINTREE_MAXSYMBOLS = NUM_CHARS + 290 * 8;
        private const int MAINTREE_TABLEBITS = 12;
        private const int LENGTH_MAXSYMBOLS = NUM_SECONDARY_LENGTHS + 1;
        private const int LENGTH_TABLEBITS = 12;
        private const int ALIGNED_MAXSYMBOLS = 8;
        private const int ALIGNED_TABLEBITS = 7;
        private const int LENTABLE_SAFETY = 64;
        private const int FRAME_SIZE = 32768;
        private const int HUFF_MAXBITS = 16;

        private static readonly uint[] PositionSlots =
            { 30, 32, 34, 36, 38, 42, 50, 66, 98, 162, 290 };
        private static readonly byte[] ExtraBits =
        {
            0,0,0,0,1,1,2,2,3,3,4,4,5,5,6,6,7,7,8,8,
            9,9,10,10,11,11,12,12,13,13,14,14,15,15,16,16
        };
        private static readonly uint[] PositionBase = new uint[290];

        static LzxDecoder()
        {
            uint b = 0;
            for (int i = 0; i < 290; i++)
            {
                PositionBase[i] = b;
                int eb = (i < 36) ? ExtraBits[i] : 17;
                b += 1u << eb;
            }
        }

        // ---- bitstream state ----
        private byte[] _in;
        private int _inPos;
        private uint _bitBuffer;
        private int _bitsLeft;

        // ---- Huffman trees ----
        private readonly byte[] _pretreeLen = new byte[PRETREE_MAXSYMBOLS + LENTABLE_SAFETY];
        private readonly byte[] _maintreeLen = new byte[MAINTREE_MAXSYMBOLS + LENTABLE_SAFETY];
        private readonly byte[] _lengthLen = new byte[LENGTH_MAXSYMBOLS + LENTABLE_SAFETY];
        private readonly byte[] _alignedLen = new byte[ALIGNED_MAXSYMBOLS + LENTABLE_SAFETY];
        private readonly ushort[] _pretreeTable = new ushort[(1 << PRETREE_TABLEBITS) + PRETREE_MAXSYMBOLS * 2];
        private readonly ushort[] _maintreeTable = new ushort[(1 << MAINTREE_TABLEBITS) + MAINTREE_MAXSYMBOLS * 2];
        private readonly ushort[] _lengthTable = new ushort[(1 << LENGTH_TABLEBITS) + LENGTH_MAXSYMBOLS * 2];
        private readonly ushort[] _alignedTable = new ushort[(1 << ALIGNED_TABLEBITS) + ALIGNED_MAXSYMBOLS * 2];
        private bool _lengthEmpty;

        /// <summary>
        /// Decompresses a complete LZX stream.
        /// </summary>
        /// <param name="input">raw LZX stream (already without the XMemCompress frame headers)</param>
        /// <param name="outputLength">expected decompressed size</param>
        /// <param name="windowBits">LZX window bits (15..21)</param>
        public static byte[] Decompress(byte[] input, int outputLength, int windowBits)
        {
            return new LzxDecoder().Run(input, outputLength, windowBits);
        }

        /// <summary>
        /// Tries to decompress by sweeping window sizes 15..21 (XMemCompress
        /// does not store this value in the stream).
        /// </summary>
        public static byte[] DecompressAuto(byte[] input, int outputLength)
        {
            for (int wb = 15; wb <= 21; wb++)
            {
                try
                {
                    byte[] r = Decompress(input, outputLength, wb);
                    if (r != null && r.Length == outputLength) return r;
                }
                catch { }
            }
            throw new InvalidOperationException("LZX: no window in 15..21 decoded the stream.");
        }

        private byte[] Run(byte[] input, int outputLength, int windowBits)
        {
            _in = input;
            _inPos = 0;
            _bitBuffer = 0;
            _bitsLeft = 0;

            uint windowSize = 1u << windowBits;
            byte[] window = new byte[windowSize];
            byte[] output = new byte[outputLength];

            uint numOffsets = PositionSlots[windowBits - 15] << 3;

            uint R0 = 1, R1 = 1, R2 = 1;
            int blockType = 0;
            int blockRemaining = 0;
            int blockLength = 0;
            bool headerRead = false;

            uint windowPosn = 0;
            uint framePosn = 0;
            int outPos = 0;

            int endFrame = outputLength / FRAME_SIZE + 1;
            int frame = 0;

            while (frame < endFrame)
            {
                if (!headerRead)
                {
                    int i = ReadBits(1);
                    if (i != 0) { ReadBits(16); ReadBits(16); }
                    headerRead = true;
                }

                int frameSize = FRAME_SIZE;
                if (outputLength - outPos < frameSize)
                    frameSize = outputLength - outPos;

                int bytesTodo = (int)(framePosn + (uint)frameSize - windowPosn);

                while (bytesTodo > 0)
                {
                    if (blockRemaining == 0)
                    {
                        if (blockType == BLOCKTYPE_UNCOMPRESSED && (blockLength & 1) != 0)
                            _inPos++; // realign after an odd-sized UNCOMPRESSED block

                        blockType = ReadBits(3);
                        int hi = ReadBits(16);
                        int lo = ReadBits(8);
                        blockRemaining = blockLength = (hi << 8) | lo;

                        switch (blockType)
                        {
                            case BLOCKTYPE_ALIGNED:
                                for (int i = 0; i < 8; i++) _alignedLen[i] = (byte)ReadBits(3);
                                BuildTable(ALIGNED_MAXSYMBOLS, ALIGNED_TABLEBITS, _alignedLen, _alignedTable, "ALIGNED");
                                goto case BLOCKTYPE_VERBATIM;

                            case BLOCKTYPE_VERBATIM:
                                ReadLengths(_maintreeLen, 0, 256);
                                ReadLengths(_maintreeLen, 256, (int)(NUM_CHARS + numOffsets));
                                BuildTable(MAINTREE_MAXSYMBOLS, MAINTREE_TABLEBITS, _maintreeLen, _maintreeTable, "MAINTREE");
                                ReadLengths(_lengthLen, 0, NUM_SECONDARY_LENGTHS);
                                BuildTableMaybeEmpty();
                                break;

                            case BLOCKTYPE_UNCOMPRESSED:
                                // align to byte
                                if (_bitsLeft == 0) EnsureBits(16);
                                _bitsLeft = 0; _bitBuffer = 0;
                                R0 = ReadUInt32LE();
                                R1 = ReadUInt32LE();
                                R2 = ReadUInt32LE();
                                break;

                            default:
                                throw new InvalidDataException("LZX: invalid block type.");
                        }
                    }

                    int thisRun = blockRemaining;
                    if (thisRun > bytesTodo) thisRun = bytesTodo;
                    bytesTodo -= thisRun;
                    blockRemaining -= thisRun;

                    if (blockType == BLOCKTYPE_VERBATIM || blockType == BLOCKTYPE_ALIGNED)
                    {
                        bool aligned = blockType == BLOCKTYPE_ALIGNED;
                        while (thisRun > 0)
                        {
                            int mainElement = ReadHuffSym(_maintreeTable, _maintreeLen,
                                                           MAINTREE_MAXSYMBOLS, MAINTREE_TABLEBITS);
                            if (mainElement < NUM_CHARS)
                            {
                                window[windowPosn++] = (byte)mainElement;
                                thisRun--;
                                continue;
                            }

                            mainElement -= NUM_CHARS;
                            int matchLength = mainElement & NUM_PRIMARY_LENGTHS;
                            if (matchLength == NUM_PRIMARY_LENGTHS)
                            {
                                if (_lengthEmpty)
                                    throw new InvalidDataException("LZX: empty LENGTH table.");
                                int lf = ReadHuffSym(_lengthTable, _lengthLen,
                                                     LENGTH_MAXSYMBOLS, LENGTH_TABLEBITS);
                                matchLength += lf;
                            }
                            matchLength += MIN_MATCH;

                            uint matchOffset = (uint)(mainElement >> 3);
                            if (!aligned)
                            {
                                switch (matchOffset)
                                {
                                    case 0: matchOffset = R0; break;
                                    case 1: matchOffset = R1; R1 = R0; R0 = matchOffset; break;
                                    case 2: matchOffset = R2; R2 = R0; R0 = matchOffset; break;
                                    case 3: matchOffset = 1; R2 = R1; R1 = R0; R0 = matchOffset; break;
                                    default:
                                        int eb = (matchOffset >= 36) ? 17 : ExtraBits[matchOffset];
                                        int vb = (eb > 0) ? ReadBits(eb) : 0;
                                        matchOffset = PositionBase[matchOffset] - 2 + (uint)vb;
                                        R2 = R1; R1 = R0; R0 = matchOffset;
                                        break;
                                }
                            }
                            else
                            {
                                switch (matchOffset)
                                {
                                    case 0: matchOffset = R0; break;
                                    case 1: matchOffset = R1; R1 = R0; R0 = matchOffset; break;
                                    case 2: matchOffset = R2; R2 = R0; R0 = matchOffset; break;
                                    default:
                                        int eb = (matchOffset >= 36) ? 17 : ExtraBits[matchOffset];
                                        matchOffset = PositionBase[matchOffset] - 2;
                                        if (eb > 3)
                                        {
                                            int vb = ReadBits(eb - 3);
                                            matchOffset += (uint)(vb << 3);
                                            int ab = ReadHuffSym(_alignedTable, _alignedLen,
                                                                 ALIGNED_MAXSYMBOLS, ALIGNED_TABLEBITS);
                                            matchOffset += (uint)ab;
                                        }
                                        else if (eb == 3)
                                        {
                                            int ab = ReadHuffSym(_alignedTable, _alignedLen,
                                                                 ALIGNED_MAXSYMBOLS, ALIGNED_TABLEBITS);
                                            matchOffset += (uint)ab;
                                        }
                                        else if (eb > 0)
                                        {
                                            int vb = ReadBits(eb);
                                            matchOffset += (uint)vb;
                                        }
                                        else
                                        {
                                            matchOffset = 1;
                                        }
                                        R2 = R1; R1 = R0; R0 = matchOffset;
                                        break;
                                }
                            }

                            if (windowPosn + matchLength > windowSize)
                                throw new InvalidDataException("LZX: match ran past end of window.");

                            // copy the match
                            int dst = (int)windowPosn;
                            int len = matchLength;
                            if (matchOffset > windowPosn)
                            {
                                int j = (int)(matchOffset - windowPosn);
                                if (j > (int)windowSize)
                                    throw new InvalidDataException("LZX: offset beyond window.");
                                int src = (int)(windowSize - j);
                                if (j < len)
                                {
                                    len -= j;
                                    while (j-- > 0) window[dst++] = window[src++];
                                    src = 0;
                                }
                                while (len-- > 0) window[dst++] = window[src++];
                            }
                            else
                            {
                                int src = dst - (int)matchOffset;
                                while (len-- > 0) window[dst++] = window[src++];
                            }

                            thisRun -= matchLength;
                            windowPosn += (uint)matchLength;
                        }
                    }
                    else if (blockType == BLOCKTYPE_UNCOMPRESSED)
                    {
                        for (int k = 0; k < thisRun; k++)
                            window[windowPosn + k] = NextByte();
                        windowPosn += (uint)thisRun;
                    }

                    if (thisRun < 0)
                    {
                        if (-thisRun > blockRemaining)
                            throw new InvalidDataException("LZX: block overrun.");
                        blockRemaining -= -thisRun;
                    }
                }

                if (windowPosn - framePosn != frameSize)
                    throw new InvalidDataException("LZX: frame out of bounds.");

                // realign the bitstream at end of frame
                if (_bitsLeft > 0) EnsureBits(16);
                if ((_bitsLeft & 15) != 0) RemoveBits(_bitsLeft & 15);

                // write the frame to the output
                Array.Copy(window, (int)framePosn, output, outPos, frameSize);
                outPos += frameSize;

                framePosn += (uint)frameSize;
                frame++;

                if (windowPosn == windowSize) windowPosn = 0;
                if (framePosn == windowSize) framePosn = 0;
            }

            return output;
        }

        // ---- bit reader (MSB order, same as mspack's readbits.h) ----
        private void EnsureBits(int n)
        {
            while (_bitsLeft < n)
            {
                int b0 = (_inPos < _in.Length) ? _in[_inPos++] : 0;
                int b1 = (_inPos < _in.Length) ? _in[_inPos++] : 0;
                uint data = (uint)((b1 << 8) | b0);
                _bitBuffer |= data << (32 - 16 - _bitsLeft);
                _bitsLeft += 16;
            }
        }

        private int PeekBits(int n) => n == 0 ? 0 : (int)(_bitBuffer >> (32 - n));

        private void RemoveBits(int n)
        {
            _bitBuffer <<= n;
            _bitsLeft -= n;
        }

        private int ReadBits(int n)
        {
            if (n == 0) return 0;
            EnsureBits(n);
            int v = PeekBits(n);
            RemoveBits(n);
            return v;
        }

        private byte NextByte()
        {
            // used in UNCOMPRESSED blocks (byte-aligned bitstream)
            return (_inPos < _in.Length) ? _in[_inPos++] : (byte)0;
        }

        private uint ReadUInt32LE()
        {
            uint a = NextByte(), b = NextByte(), c = NextByte(), d = NextByte();
            return a | (b << 8) | (c << 16) | (d << 24);
        }

        // ---- Huffman ----
        private int ReadHuffSym(ushort[] table, byte[] lengths, int maxSymbols, int tableBits)
        {
            EnsureBits(HUFF_MAXBITS);
            int sym = table[PeekBits(tableBits)];
            if (sym >= maxSymbols)
            {
                uint i = 1u << (32 - tableBits);
                do
                {
                    i >>= 1;
                    if (i == 0) throw new InvalidDataException("LZX: invalid Huffman symbol.");
                    sym = table[(sym << 1) | (((_bitBuffer & i) != 0) ? 1 : 0)];
                } while (sym >= maxSymbols);
            }
            RemoveBits(lengths[sym]);
            return sym;
        }

        private void ReadLengths(byte[] lens, int first, int last)
        {
            for (int x = 0; x < 20; x++)
                _pretreeLen[x] = (byte)ReadBits(4);
            BuildTable(PRETREE_MAXSYMBOLS, PRETREE_TABLEBITS, _pretreeLen, _pretreeTable, "PRETREE");

            for (int x = first; x < last; )
            {
                int z = ReadHuffSym(_pretreeTable, _pretreeLen, PRETREE_MAXSYMBOLS, PRETREE_TABLEBITS);
                if (z == 17)
                {
                    int y = ReadBits(4) + 4;
                    while (y-- > 0) lens[x++] = 0;
                }
                else if (z == 18)
                {
                    int y = ReadBits(5) + 20;
                    while (y-- > 0) lens[x++] = 0;
                }
                else if (z == 19)
                {
                    int y = ReadBits(1) + 4;
                    int z2 = ReadHuffSym(_pretreeTable, _pretreeLen, PRETREE_MAXSYMBOLS, PRETREE_TABLEBITS);
                    z2 = lens[x] - z2; if (z2 < 0) z2 += 17;
                    while (y-- > 0) lens[x++] = (byte)z2;
                }
                else
                {
                    z = lens[x] - z; if (z < 0) z += 17;
                    lens[x++] = (byte)z;
                }
            }
        }

        private void BuildTableMaybeEmpty()
        {
            _lengthEmpty = false;
            if (!MakeDecodeTable(LENGTH_MAXSYMBOLS, LENGTH_TABLEBITS, _lengthLen, _lengthTable))
            {
                for (int i = 0; i < LENGTH_MAXSYMBOLS; i++)
                    if (_lengthLen[i] > 0)
                        throw new InvalidDataException("LZX: failed to build LENGTH table.");
                _lengthEmpty = true;
            }
        }

        private static void BuildTable(int maxSymbols, int tableBits, byte[] lengths,
                                        ushort[] table, string name)
        {
            if (!MakeDecodeTable(maxSymbols, tableBits, lengths, table))
                throw new InvalidDataException("LZX: failed to build " + name + " table.");
        }

        /// <summary>
        /// make_decode_table from readhuff.h (BITS_ORDER_MSB variant).
        /// Returns true on success, false on error.
        /// </summary>
        private static bool MakeDecodeTable(int nsyms, int nbits, byte[] length, ushort[] table)
        {
            uint pos = 0;
            uint tableMask = 1u << nbits;
            uint bitMask = tableMask >> 1;

            for (int bitNum = 1; bitNum <= nbits; bitNum++)
            {
                for (int sym = 0; sym < nsyms; sym++)
                {
                    if (length[sym] != bitNum) continue;
                    uint leaf = pos;
                    if ((pos += bitMask) > tableMask) return false;
                    for (uint fill = bitMask; fill-- > 0; )
                        table[leaf++] = (ushort)sym;
                }
                bitMask >>= 1;
            }

            if (pos == tableMask) return true;

            for (uint sym = pos; sym < tableMask; sym++)
                table[sym] = 0xFFFF;

            uint nextSymbol = ((tableMask >> 1) < (uint)nsyms) ? (uint)nsyms : (tableMask >> 1);

            pos <<= 16;
            tableMask <<= 16;
            bitMask = 1 << 15;

            for (int bitNum = nbits + 1; bitNum <= HUFF_MAXBITS; bitNum++)
            {
                for (int sym = 0; sym < nsyms; sym++)
                {
                    if (length[sym] != bitNum) continue;
                    if (pos >= tableMask) return false;
                    uint leaf = pos >> 16;
                    for (int fill = 0; fill < (bitNum - nbits); fill++)
                    {
                        if (table[leaf] == 0xFFFF)
                        {
                            table[(nextSymbol << 1)] = 0xFFFF;
                            table[(nextSymbol << 1) + 1] = 0xFFFF;
                            table[leaf] = (ushort)(nextSymbol++);
                        }
                        leaf = (uint)(table[leaf] << 1);
                        if (((pos >> (15 - fill)) & 1) != 0) leaf++;
                    }
                    table[leaf] = (ushort)sym;
                    pos += bitMask;
                }
                bitMask >>= 1;
            }

            return pos == tableMask;
        }
    }
}
