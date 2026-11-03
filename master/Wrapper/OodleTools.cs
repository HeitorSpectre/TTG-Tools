//Used from here: https://github.com/Tamely/Oodle-Tools/tree/main
//Imported dll from here: https://aluigi.altervista.org/papers/ttarchext.zip

using System;
using System.Runtime.InteropServices;

namespace OodleTools
{

    // These enums were taken directly out of Unreal Engine
    internal enum OodleFormat : uint
    {
        LZH = 0,
        LZHLW = 1,
        LZNIB = 2,
        None = 3,
        LZB16 = 4,
        LZBLW = 5,
        LZA = 6,
        LZNA = 7,
        Kraken = 8,
        Mermaid = 9,
        BitKnit = 10,
        Selkie = 11,
        Hydra = 12,
        Leviathan = 13
    }

    internal enum OodleCompressionLevel : uint
    {
        None = 0,
        SuperFast = 1,
        VeryFast = 2,
        Fast = 3,
        Normal = 4,
        Optimal1 = 5,
        Optimal2 = 6,
        Optimal3 = 7,
        Optimal4 = 8,
        Optimal5 = 9
    }

    public class Imports
    {
        public const string DllName = "oo2core_5_win64.dll";

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string fileName);

        /// <summary>
        /// Checks if the Oodle library can be loaded. It isn't shipped with the tool, so zCTT/eCTT
        /// archives (newer PC games and every console port since Batman) can't be read without it.
        /// </summary>
        public static bool IsAvailable()
        {
            return LoadLibrary(DllName) != IntPtr.Zero;
        }

        /// <summary>
        /// Does a math operation to determine the max size the compressed data can be.
        /// </summary>
        /// <param name="BufferSize">uint: The length of the decompressed buffer.</param>
        /// <returns>uint: The max size the compressed buffer can be.</returns>
        internal static uint GetCompressedBounds(uint BufferSize)
            => BufferSize + 274 * ((BufferSize + 0x3FFFF) / 0x40000) + 0x100; //274 bytes per 256KB block (was /0x400000, leaving no room for incompressible chunks)

        /// <summary>
        /// This should never be called!!! If you are going to compress something, use the Compress method in the Oodle class and don't call it from the library directly!
        /// </summary>
        /// <param name="Format">OodleFormat: The compression format used.</param>
        /// <param name="Buffer">byte[]: The decompressed data.</param>
        /// <param name="BufferSize">long: The size of the decompressed data.</param>
        /// <param name="OutputBuffer">ref byte[]: Where the compressed data will output to.</param>
        /// <param name="Level">OodleCompressionLevel: The compression level used.</param>
        /// <param name="a">uint: unused</param>
        /// <param name="b">uint: unused</param>
        /// <param name="c">uint: unused</param>
        /// <returns>int: The length of the compressed data.</returns>
        [DllImport("oo2core_5_win64.dll")]
        internal static extern long OodleLZ_Compress(OodleFormat Format, byte[] Buffer, long BufferSize, byte[] OutputBuffer, OodleCompressionLevel Level, long options, long dictionaryBase, long lrm, long scratchMem, long scratchSize);

        /// <summary>
        /// This should never be called!!! If you are going to decompress something, use the Decompress method in the Oodle class and don't call it from the library directly!
        /// </summary>
        /// <param name="Buffer">byte[]: The compressed data.</param>
        /// <param name="BufferSize">long: The size of the compressed data.</param>
        /// <param name="OutputBuffer">ref byte[]: Where the decompressed data will output to.</param>
        /// <param name="OutputBufferSize">long: The size of the decompressed data.</param>
        /// <param name="a">uint: unused</param>
        /// <param name="b">uint: unused</param>
        /// <param name="c">uint: unused</param>
        /// <param name="d">uint: unused</param>
        /// <param name="e">uint: unused</param>
        /// <param name="f">uint: unused</param>
        /// <param name="g">uint: unused</param>
        /// <param name="h">uint: unused</param>
        /// <param name="i">uint: unused</param>
        /// <param name="ThreadModule">int: not really used, pass nullptr/void* in cpp or just 0 in C#</param>
        /// <returns>int: The length of the decompressed data.</returns>
        [DllImport("oo2core_5_win64.dll")]
        internal static extern long OodleLZ_Decompress(byte[] Buffer, long BufferSize, byte[] OutputBuffer, long OutputBufferSize, int fuzzSafe, int checkCRC, int verbosity, long decBufBase, long decBufSize, long fpCallback, long callbackUserData, long decoderMemory, long decoderMemorySize, int threadPhase);
    }
}