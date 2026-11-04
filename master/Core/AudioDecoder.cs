using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TTG_Tools
{
    /// <summary>
    /// Decodes the audio formats found inside Telltale archives to 16-bit PCM for the Archive
    /// Unpacker player:
    ///   .aud        ERTM AudioData + Ogg Vorbis stream (Tales of Monkey Island, Sam &amp; Max...)
    ///   .vox        ERTM VoiceData + Speex packets (BTTF, Poker Night, W&amp;G, console voices)
    ///   .fsb/.wav   FMOD sound banks FSB4 / FSB5 (MP3 or PCM samples; TWD, Batman, BTTF PS4...)
    ///   plain RIFF WAV, Ogg Vorbis and MP3 files.
    /// The .vox layout follows zenderovpaulo95's Telltale-games-vox-modifier.
    /// </summary>
    public static class AudioDecoder
    {
        public class DecodedAudio
        {
            public byte[] Pcm;      // interleaved 16-bit little-endian
            public int SampleRate;
            public int Channels;
            public string Codec;    // short description for the info line
        }

        public static bool IsAudioExtension(string ext)
        {
            switch (ext)
            {
                case ".aud": case ".vox": case ".fsb": case ".wav": case ".ogg": case ".mp3":
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>Returns null (with a reason) when the data is not a supported audio stream.</summary>
        public static DecodedAudio Decode(byte[] data, string ext, byte[] key, out string error)
        {
            error = null;

            try
            {
                if (data == null || data.Length < 16) { error = "empty"; return null; }

                string magic = Encoding.ASCII.GetString(data, 0, 4);

                if (magic == "FSB4") return DecodeFsb4(data, out error);
                if (magic == "FSB5") return DecodeFsb5(data, out error);
                if (magic == "RIFF") return DecodeWithNAudio(data, "WAV");
                if (magic == "OggS") return DecodeVorbis(data, 0);
                if (IsMp3(data, 0)) return DecodeMp3(data, 0, data.Length, "MP3");

                if (ext == ".vox") return DecodeVox(data, key, out error);

                //.aud (and anything else with a meta header): the Ogg stream follows the header.
                int ogg = IndexOf(data, Encoding.ASCII.GetBytes("OggS"), 0);
                if (ogg >= 0) return DecodeVorbis(data, ogg);

                int fsb = IndexOf(data, Encoding.ASCII.GetBytes("FSB5"), 0);
                if (fsb < 0) fsb = IndexOf(data, Encoding.ASCII.GetBytes("FSB4"), 0);
                if (fsb >= 0)
                {
                    byte[] slice = new byte[data.Length - fsb];
                    Array.Copy(data, fsb, slice, 0, slice.Length);
                    return Decode(slice, ext, key, out error);
                }

                error = "unsupported audio format";
                return null;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return null;
            }
        }

        #region Ogg Vorbis

        private static DecodedAudio DecodeVorbis(byte[] data, int offset)
        {
            using (var ms = new MemoryStream(data, offset, data.Length - offset, false))
            using (var reader = new NVorbis.VorbisReader(ms, false))
            {
                int channels = reader.Channels;
                var pcm = new MemoryStream();
                float[] buffer = new float[reader.SampleRate * channels];
                int read;

                while ((read = reader.ReadSamples(buffer, 0, buffer.Length)) > 0)
                {
                    for (int i = 0; i < read; i++) WriteSample(pcm, buffer[i]);
                }

                return new DecodedAudio { Pcm = pcm.ToArray(), SampleRate = reader.SampleRate, Channels = channels, Codec = "Ogg Vorbis" };
            }
        }

        private static void WriteSample(MemoryStream ms, float f)
        {
            int s = (int)Math.Round(f * 32767f);
            if (s > 32767) s = 32767; else if (s < -32768) s = -32768;
            ms.WriteByte((byte)s);
            ms.WriteByte((byte)(s >> 8));
        }

        #endregion

        #region Speex .vox

        private static DecodedAudio DecodeVox(byte[] data, byte[] key, out string error)
        {
            error = null;
            int pos = SkipMetaHeader(data);
            if (pos < 0) { error = "not a Telltale meta stream"; return null; }

            //Optional "is encrypted" bool stored as ASCII '0'/'1'.
            bool encrypted = false;
            if (data[pos] == '0' || data[pos] == '1') { encrypted = data[pos] == '1'; pos++; }

            int start = pos;
            for (int attempt = 0; attempt < 2; attempt++)
            {
                //float length, int dataSize, int frameSize, int sampleRate, int channels,
                //int blockTableSize, int blockCount, int blocks[blockCount], packet data.
                pos = start + attempt;
                if (pos + 28 > data.Length) break;

                int dataSize = BitConverter.ToInt32(data, pos + 4);
                int frameSize = BitConverter.ToInt32(data, pos + 8);
                int sampleRate = BitConverter.ToInt32(data, pos + 12);
                int channels = BitConverter.ToInt32(data, pos + 16);
                int blockCount = BitConverter.ToInt32(data, pos + 24);
                int tableEnd = pos + 28 + blockCount * 4;

                //Some files carry a few trailing bytes after the packets (Sam & Max 201 .vox).
                int slack = data.Length - tableEnd - dataSize;
                if (blockCount <= 0 || tableEnd > data.Length || slack < 0 || slack > 8) continue;
                int dataEnd = tableEnd + dataSize;

                int[] blocks = new int[blockCount];
                for (int i = 0; i < blockCount; i++) blocks[i] = BitConverter.ToInt32(data, pos + 28 + i * 4);

                NSpeex.BandMode band = frameSize == 640 ? NSpeex.BandMode.UltraWide
                    : frameSize == 320 ? NSpeex.BandMode.Wide : NSpeex.BandMode.Narrow;
                var decoder = new NSpeex.SpeexDecoder(band, true);
                var pcm = new MemoryStream();
                short[] outBuf = new short[decoder.FrameSize * 64];

                for (int i = 0; i < blockCount; i++)
                {
                    int bStart = tableEnd + blocks[i];
                    int bEnd = i + 1 < blockCount ? tableEnd + blocks[i + 1] : dataEnd;
                    if (bStart < tableEnd || bEnd > dataEnd || bEnd <= bStart) continue;

                    byte[] packet = new byte[bEnd - bStart];
                    Array.Copy(data, bStart, packet, 0, packet.Length);

                    if (encrypted && i % 64 == 0 && key != null)
                    {
                        var bf = new BlowFishCS.BlowFish(key, 2);
                        packet = bf.Crypt_ECB(packet, 2, true);
                    }

                    //The first packet is the Speex comment header ("Encoded with Speex ...").
                    if (i == 0 && IndexOf(packet, Encoding.ASCII.GetBytes("Encoded with Speex"), 0) >= 0) continue;

                    int samples;
                    try { samples = decoder.Decode(packet, 0, packet.Length, outBuf, 0, false); }
                    catch { continue; }

                    for (int s = 0; s < samples && s < outBuf.Length; s++)
                    {
                        pcm.WriteByte((byte)outBuf[s]);
                        pcm.WriteByte((byte)(outBuf[s] >> 8));
                    }
                }

                //NSpeex has no intensity-stereo decoder, so stereo voices play as mono (the
                //speech itself is unaffected).
                return new DecodedAudio
                {
                    Pcm = pcm.ToArray(),
                    SampleRate = decoder.SampleRate > 0 ? decoder.SampleRate : sampleRate,
                    Channels = 1,
                    Codec = "Speex " + (band == NSpeex.BandMode.UltraWide ? "UWB" : band == NSpeex.BandMode.Wide ? "WB" : "NB")
                        + (channels == 2 ? " (stereo as mono)" : "")
                };
            }

            error = "unrecognized .vox layout";
            return null;
        }

        //ERTM / MBIN header: magic, class count, then per class either an 8-byte CRC + 4-byte
        //version, or (oldest MBIN) a length-prefixed class name + 4-byte version.
        private static int SkipMetaHeader(byte[] data)
        {
            string magic = Encoding.ASCII.GetString(data, 0, 4);
            if (magic != "ERTM" && magic != "NIBM") return -1;

            int count = BitConverter.ToInt32(data, 4);
            if (count < 0 || count > 1000) return -1;

            int pos = 8;
            bool named = data.Length > 24 && (Encoding.ASCII.GetString(data, 12, 12).Contains("class") || Encoding.ASCII.GetString(data, 12, 12).Contains("struct"));

            for (int i = 0; i < count; i++)
            {
                if (named)
                {
                    int len = BitConverter.ToInt32(data, pos);
                    pos += 4 + len + 4;
                }
                else pos += 12;

                if (pos >= data.Length) return -1;
            }

            return pos;
        }

        #endregion

        #region FMOD sound banks

        private static DecodedAudio DecodeFsb4(byte[] d, out string error)
        {
            error = null;
            int numSamples = BitConverter.ToInt32(d, 4);
            int headersSize = BitConverter.ToInt32(d, 8);
            int dataOffset = 48 + headersSize;

            //First sample header: size(2) name(30) samples(4) compressedSize(4) loopStart(4)
            //loopEnd(4) mode(4) frequency(4) volume(2) pan(2) pri(2) channels(2)...
            int h = 48;
            int compSize = BitConverter.ToInt32(d, h + 36);
            uint mode = BitConverter.ToUInt32(d, h + 48);
            int freq = BitConverter.ToInt32(d, h + 52);
            int channels = BitConverter.ToUInt16(d, h + 62);
            if (compSize <= 0 || dataOffset + compSize > d.Length) compSize = d.Length - dataOffset;

            string count = numSamples > 1 ? " (1/" + numSamples + ")" : "";

            if (IsMp3(d, dataOffset)) return DecodeMp3(d, dataOffset, compSize, "FSB4 MP3" + count);

            //FSOUND_16BITS raw PCM.
            if ((mode & 0x10) != 0 && (mode & (0x400000 | 0x01000000 | 0x02000000 | 0x00800000)) == 0)
            {
                byte[] pcm = new byte[compSize];
                Array.Copy(d, dataOffset, pcm, 0, compSize);
                return new DecodedAudio { Pcm = pcm, SampleRate = freq, Channels = Math.Max(1, channels), Codec = "FSB4 PCM16" + count };
            }

            error = "FSB4 codec not supported (mode 0x" + mode.ToString("X") + ")";
            return null;
        }

        private static readonly int[] Fsb5Rates = { 4000, 8000, 11000, 11025, 16000, 22050, 24000, 32000, 44100, 48000, 96000 };

        private static DecodedAudio DecodeFsb5(byte[] d, out string error)
        {
            error = null;
            int version = BitConverter.ToInt32(d, 4);
            int numSamples = BitConverter.ToInt32(d, 8);
            int headersSize = BitConverter.ToInt32(d, 12);
            int namesSize = BitConverter.ToInt32(d, 16);
            int dataSize = BitConverter.ToInt32(d, 20);
            int codec = BitConverter.ToInt32(d, 24);
            int baseSize = version == 0 ? 0x40 : 0x3C;

            int h = baseSize;
            ulong bits = BitConverter.ToUInt64(d, h);
            bool hasChunks = (bits & 1) != 0;
            int freqIdx = (int)((bits >> 1) & 0xF);
            int channels = (int)((bits >> 5) & 1) + 1;
            long offset = (long)((bits >> 6) & 0x0FFFFFFF) * 16;
            int freq = freqIdx < Fsb5Rates.Length ? Fsb5Rates[freqIdx] : 44100;

            //Extra chunks may override channels/frequency; chunk 11 names the Vorbis setup header.
            uint vorbisCrc = 0;
            int c = h + 8;
            while (hasChunks && c + 4 <= d.Length)
            {
                uint chunk = BitConverter.ToUInt32(d, c);
                hasChunks = (chunk & 1) != 0;
                int size = (int)((chunk >> 1) & 0xFFFFFF);
                int type = (int)((chunk >> 25) & 0x7F);
                if (type == 1 && size >= 1) channels = d[c + 4];
                else if (type == 2 && size >= 4) freq = BitConverter.ToInt32(d, c + 4);
                else if (type == 11 && size >= 4) vorbisCrc = BitConverter.ToUInt32(d, c + 4);
                c += 4 + size;
            }

            int dataStart = baseSize + headersSize + namesSize;
            int start = dataStart + (int)offset;
            int length = dataSize - (int)offset;

            if (numSamples > 1)
            {
                //Second sample's offset bounds the first one.
                ulong next = FindFsb5SampleOffset(d, baseSize, 1);
                if (next > (ulong)offset) length = (int)(next - (ulong)offset);
            }

            if (start < 0 || start >= d.Length) { error = "FSB5 data out of range"; return null; }
            if (start + length > d.Length || length <= 0) length = d.Length - start;

            string count = numSamples > 1 ? " (1/" + numSamples + ")" : "";

            switch (codec)
            {
                case 15: //Vorbis (Wolf Among Us, Batman and other FSB5 games)
                {
                    byte[] ogg = Fsb5VorbisToOgg(d, start, length, channels, freq, vorbisCrc, out error);
                    if (ogg == null) return null;
                    DecodedAudio decoded = DecodeVorbis(ogg, 0);
                    decoded.Codec = "FSB5 Vorbis" + count;
                    return decoded;
                }
                case 11: //MPEG
                    return DecodeMp3(d, start, length, "FSB5 MP3" + count);
                case 2: //PCM16
                {
                    byte[] pcm = new byte[length];
                    Array.Copy(d, start, pcm, 0, length);
                    return new DecodedAudio { Pcm = pcm, SampleRate = freq, Channels = channels, Codec = "FSB5 PCM16" + count };
                }
                case 1: //PCM8 (unsigned)
                {
                    byte[] pcm = new byte[length * 2];
                    for (int i = 0; i < length; i++)
                    {
                        short s = (short)((d[start + i] - 128) << 8);
                        pcm[i * 2] = (byte)s; pcm[i * 2 + 1] = (byte)(s >> 8);
                    }
                    return new DecodedAudio { Pcm = pcm, SampleRate = freq, Channels = channels, Codec = "FSB5 PCM8" + count };
                }
                default:
                    error = "FSB5 codec " + Fsb5CodecName(codec) + " not supported";
                    return null;
            }
        }

        #region FSB5 Vorbis -> Ogg

        //FSB5 keeps only the Vorbis audio packets ([u16 size][packet]...) plus the CRC32 of the
        //setup header, which FMOD encoders pick from a fixed set. The 161 known setup headers come
        //from python-fsb5 (MIT, ThirdPartyLicenses\python-fsb5-LICENSE.txt), stored gzip'ed as an
        //embedded resource. The identification header is rebuilt with FMOD's block sizes 256/2048.
        private static Dictionary<uint, byte[]> vorbisSetups;

        private static byte[] FindVorbisSetup(uint crc)
        {
            if (vorbisSetups == null)
            {
                var table = new Dictionary<uint, byte[]>();
                using (Stream res = typeof(AudioDecoder).Assembly.GetManifestResourceStream("TTG_Tools.fsb5_vorbis_headers.gz"))
                using (var gz = new System.IO.Compression.GZipStream(res, System.IO.Compression.CompressionMode.Decompress))
                using (var br = new BinaryReader(gz))
                {
                    int count = br.ReadInt32();
                    for (int i = 0; i < count; i++)
                    {
                        uint key = br.ReadUInt32();
                        int len = br.ReadInt32();
                        table[key] = br.ReadBytes(len);
                    }
                }
                vorbisSetups = table;
            }

            byte[] setup;
            return vorbisSetups.TryGetValue(crc, out setup) ? setup : null;
        }

        private static byte[] Fsb5VorbisToOgg(byte[] d, int start, int length, int channels, int freq, uint crc, out string error)
        {
            error = null;
            byte[] setup = FindVorbisSetup(crc);
            if (setup == null) { error = "FSB5 Vorbis setup header 0x" + crc.ToString("X8") + " unknown"; return null; }

            //Identification header.
            var id = new MemoryStream();
            id.WriteByte(1);
            id.Write(Encoding.ASCII.GetBytes("vorbis"), 0, 6);
            id.Write(BitConverter.GetBytes(0), 0, 4);              //version
            id.WriteByte((byte)channels);
            id.Write(BitConverter.GetBytes(freq), 0, 4);
            id.Write(new byte[12], 0, 12);                          //bitrates
            id.WriteByte(0xB8);                                     //log2 block sizes: 256 | 2048 << 4
            id.WriteByte(1);                                        //framing

            //Empty comment header.
            var comment = new MemoryStream();
            comment.WriteByte(3);
            comment.Write(Encoding.ASCII.GetBytes("vorbis"), 0, 6);
            comment.Write(BitConverter.GetBytes(0), 0, 4);         //vendor length
            comment.Write(BitConverter.GetBytes(0), 0, 4);         //comment count
            comment.WriteByte(1);

            bool[] modeBlockFlags = ReadVorbisModeBlockFlags(setup);
            int modeBits = 0;
            while ((1 << modeBits) < modeBlockFlags.Length) modeBits++;

            var packets = new List<byte[]>();
            int pos = start, end = start + length;

            while (pos + 2 <= end)
            {
                int size = BitConverter.ToUInt16(d, pos);
                pos += 2;
                if (size == 0 || pos + size > end) break;

                byte[] packet = new byte[size];
                Array.Copy(d, pos, packet, 0, size);
                pos += size;
                packets.Add(packet);
            }

            if (packets.Count == 0) { error = "FSB5 Vorbis sample has no packets"; return null; }

            var ogg = new MemoryStream();
            int seq = 0;
            WriteOggPage(ogg, new List<byte[]> { id.ToArray() }, 0x02, 0, ref seq);
            WriteOggPage(ogg, new List<byte[]> { comment.ToArray(), setup }, 0x00, 0, ref seq);

            long granule = 0;
            int prevBlock = 0;

            for (int i = 0; i < packets.Count; i++)
            {
                byte[] packet = packets[i];
                int mode = modeBits > 0 ? (packet[0] >> 1) & ((1 << modeBits) - 1) : 0;
                int block = mode < modeBlockFlags.Length && modeBlockFlags[mode] ? 2048 : 256;
                if (prevBlock != 0) granule += (block + prevBlock) / 4;
                prevBlock = block;

                bool last = i == packets.Count - 1;
                WriteOggPage(ogg, new List<byte[]> { packet }, (byte)(last ? 0x04 : 0x00), granule, ref seq);
            }

            return ogg.ToArray();
        }

        //Vorbis modes sit at the very end of the setup packet: [6-bit count-1] then per mode
        //[blockflag:1][windowtype:16 = 0][transformtype:16 = 0][mapping:8], then the framing bit.
        //Scan them backwards like FFmpeg's vorbis_parser instead of decoding every codebook.
        private static bool[] ReadVorbisModeBlockFlags(byte[] setup)
        {
            int totalBits = setup.Length * 8;
            Func<int, int> bitAt = i => (setup[i >> 3] >> (i & 7)) & 1; //Vorbis packs LSB first
            Func<int, int, int> readField = (from, bits) =>
            {
                int v = 0;
                for (int k = 0; k < bits; k++) v |= bitAt(from + k) << k;
                return v;
            };

            int p = totalBits - 1;
            while (p >= 0 && bitAt(p) == 0) p--;                       //framing bit
            int modesEnd = p;                                            //modes end right before it

            int best = 0;
            for (int n = 1; n <= 64; n++)
            {
                int modeStart = modesEnd - n * 41;
                if (modeStart - 6 < 0) break;

                if (readField(modeStart + 1, 16) != 0 || readField(modeStart + 17, 16) != 0 || readField(modeStart + 33, 8) > 63) break;
                if (readField(modeStart - 6, 6) + 1 == n) best = n;
            }

            if (best == 0) return new[] { false, true };

            var flags = new bool[best];
            int first = modesEnd - best * 41;
            for (int m = 0; m < best; m++) flags[m] = bitAt(first + m * 41) == 1;
            return flags;
        }

        private static readonly uint[] OggCrcTable = BuildOggCrcTable();

        private static uint[] BuildOggCrcTable()
        {
            var t = new uint[256];
            for (uint i = 0; i < 256; i++)
            {
                uint r = i << 24;
                for (int k = 0; k < 8; k++) r = (r & 0x80000000) != 0 ? (r << 1) ^ 0x04C11DB7 : r << 1;
                t[i] = r;
            }
            return t;
        }

        private static void WriteOggPage(MemoryStream ogg, List<byte[]> packets, byte flags, long granule, ref int seq)
        {
            var lacing = new List<byte>();
            foreach (byte[] pk in packets)
            {
                int left = pk.Length;
                while (left >= 255) { lacing.Add(255); left -= 255; }
                lacing.Add((byte)left);
            }

            var page = new MemoryStream();
            page.Write(Encoding.ASCII.GetBytes("OggS"), 0, 4);
            page.WriteByte(0);
            page.WriteByte(flags);
            page.Write(BitConverter.GetBytes(granule), 0, 8);
            page.Write(BitConverter.GetBytes(1), 0, 4);            //serial
            page.Write(BitConverter.GetBytes(seq++), 0, 4);
            page.Write(new byte[4], 0, 4);                          //CRC placeholder
            page.WriteByte((byte)lacing.Count);
            page.Write(lacing.ToArray(), 0, lacing.Count);
            foreach (byte[] pk in packets) page.Write(pk, 0, pk.Length);

            byte[] bytes = page.ToArray();
            uint crc = 0;
            foreach (byte x in bytes) crc = (crc << 8) ^ OggCrcTable[((crc >> 24) & 0xFF) ^ x];
            Array.Copy(BitConverter.GetBytes(crc), 0, bytes, 22, 4);
            ogg.Write(bytes, 0, bytes.Length);
        }

        #endregion

        private static ulong FindFsb5SampleOffset(byte[] d, int baseSize, int index)
        {
            int h = baseSize;
            for (int i = 0; i <= index && h + 8 <= d.Length; i++)
            {
                ulong bits = BitConverter.ToUInt64(d, h);
                if (i == index) return ((bits >> 6) & 0x0FFFFFFF) * 16;

                bool more = (bits & 1) != 0;
                h += 8;
                while (more && h + 4 <= d.Length)
                {
                    uint chunk = BitConverter.ToUInt32(d, h);
                    more = (chunk & 1) != 0;
                    h += 4 + (int)((chunk >> 1) & 0xFFFFFF);
                }
            }
            return 0;
        }

        private static string Fsb5CodecName(int codec)
        {
            switch (codec)
            {
                case 6: return "GCADPCM";
                case 7: return "IMA ADPCM";
                case 8: return "VAG";
                case 9: return "HEVAG";
                case 10: return "XMA";
                case 13: return "ATRAC9";
                case 15: return "Vorbis";
                case 17: return "Opus";
                default: return codec.ToString();
            }
        }

        #endregion

        #region MP3 / WAV through NAudio

        private static bool IsMp3(byte[] d, int o)
        {
            if (o + 3 > d.Length) return false;
            if (d[o] == 'I' && d[o + 1] == 'D' && d[o + 2] == '3') return true;
            return d[o] == 0xFF && (d[o + 1] & 0xE0) == 0xE0;
        }

        //FMOD pads every MP3 frame to 4 bytes (FSB5 BTTF PS4); NAudio then resyncs on the padding,
        //misreads it as a frame header and aborts ("changes to channel count"). Keep only real
        //Layer III frames, back to back.
        private static byte[] CleanMp3Frames(byte[] d, int offset, int length)
        {
            var outMs = new MemoryStream(length);
            int i = offset, end = offset + length;
            int[] v1 = { 0, 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320, 0 };
            int[] v2 = { 0, 8, 16, 24, 32, 40, 48, 56, 64, 80, 96, 112, 128, 144, 160, 0 };

            while (i + 4 <= end)
            {
                if (d[i] == 0xFF && (d[i + 1] & 0xE0) == 0xE0)
                {
                    int ver = (d[i + 1] >> 3) & 3;      //3 = MPEG1, 2 = MPEG2, 0 = MPEG2.5
                    int layer = (d[i + 1] >> 1) & 3;    //1 = Layer III
                    int brIdx = d[i + 2] >> 4;
                    int srIdx = (d[i + 2] >> 2) & 3;
                    int pad = (d[i + 2] >> 1) & 1;

                    if (ver != 1 && layer == 1 && brIdx != 0 && brIdx != 15 && srIdx != 3)
                    {
                        int rate = ver == 3 ? new[] { 44100, 48000, 32000 }[srIdx]
                                 : ver == 2 ? new[] { 22050, 24000, 16000 }[srIdx]
                                 : new[] { 11025, 12000, 8000 }[srIdx];
                        int bitrate = (ver == 3 ? v1[brIdx] : v2[brIdx]) * 1000;
                        int len = (ver == 3 ? 144 : 72) * bitrate / rate + pad;

                        if (len > 4 && i + len <= end)
                        {
                            outMs.Write(d, i, len);
                            i += len;
                            continue;
                        }
                    }
                }

                i++;
            }

            return outMs.ToArray();
        }

        private static DecodedAudio DecodeMp3(byte[] d, int offset, int length, string codec)
        {
            byte[] clean = CleanMp3Frames(d, offset, length);
            if (clean.Length > 0) { d = clean; offset = 0; length = clean.Length; }

            using (var ms = new MemoryStream(d, offset, length, false))
            using (var reader = new NAudio.Wave.Mp3FileReader(ms))
            {
                return ReadAllPcm(reader, codec);
            }
        }

        private static DecodedAudio DecodeWithNAudio(byte[] d, string codec)
        {
            using (var ms = new MemoryStream(d, false))
            using (var reader = new NAudio.Wave.WaveFileReader(ms))
            {
                NAudio.Wave.WaveStream source = reader;
                if (reader.WaveFormat.Encoding != NAudio.Wave.WaveFormatEncoding.Pcm || reader.WaveFormat.BitsPerSample != 16)
                {
                    //ADPCM and friends: let the Windows ACM codecs convert to PCM.
                    source = NAudio.Wave.WaveFormatConversionStream.CreatePcmStream(reader);
                }

                using (source == reader ? null : source)
                {
                    return ReadAllPcm(source, codec + " " + reader.WaveFormat.Encoding);
                }
            }
        }

        private static DecodedAudio ReadAllPcm(NAudio.Wave.WaveStream stream, string codec)
        {
            var fmt = stream.WaveFormat;
            var pcm = new MemoryStream();
            byte[] buffer = new byte[fmt.AverageBytesPerSecond > 0 ? fmt.AverageBytesPerSecond : 65536];
            int read;
            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0) pcm.Write(buffer, 0, read);

            byte[] bytes = pcm.ToArray();

            if (fmt.BitsPerSample == 8)
            {
                byte[] wide = new byte[bytes.Length * 2];
                for (int i = 0; i < bytes.Length; i++)
                {
                    short s = (short)((bytes[i] - 128) << 8);
                    wide[i * 2] = (byte)s; wide[i * 2 + 1] = (byte)(s >> 8);
                }
                bytes = wide;
            }

            return new DecodedAudio { Pcm = bytes, SampleRate = fmt.SampleRate, Channels = fmt.Channels, Codec = codec };
        }

        #endregion

        private static int IndexOf(byte[] data, byte[] pattern, int start)
        {
            for (int i = start; i <= data.Length - pattern.Length; i++)
            {
                int j = 0;
                while (j < pattern.Length && data[i + j] == pattern[j]) j++;
                if (j == pattern.Length) return i;
            }
            return -1;
        }
    }
}
