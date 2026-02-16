using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TTG_Tools.Texts
{
    public sealed class PropEditorDocument
    {
        public string FilePath { get; set; }
        public string Header { get; set; }
        public string Marker { get; set; }
        public List<PropEditorEntry> Entries { get; private set; }

        public PropEditorDocument()
        {
            Entries = new List<PropEditorEntry>();
        }
    }

    public sealed class PropEditorEntry
    {
        public int Index { get; set; }
        public string Value { get; set; }
    }

    public static class PropEditorWorker
    {
        private const string MarkerSimple = "B4-F4-5A-5F-60-6E-9C-CD";
        private const string MarkerNested = "25-03-C6-1F-D8-64-1B-4F";

        public static PropEditorDocument Load(string filePath)
        {
            PropEditorDocument document = new PropEditorDocument();
            document.FilePath = filePath;

            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (BinaryReader br = new BinaryReader(fs))
            {
                byte[] headerBytes = br.ReadBytes(4);
                document.Header = Encoding.ASCII.GetString(headerBytes);

                if ((document.Header == "5VSM") || (document.Header == "6VSM"))
                {
                    br.ReadInt32();
                    br.ReadInt64();
                }

                int countHeaders = br.ReadInt32();
                for (int i = 0; i < countHeaders; i++)
                {
                    br.ReadBytes(8);
                    br.ReadBytes(4);
                }

                br.ReadInt32();
                br.ReadInt32();

                if (document.Header != "6VSM")
                {
                    int blSize1 = br.ReadInt32();
                    br.ReadBytes(blSize1 - 4);
                }

                br.ReadInt32();
                br.ReadInt32();

                if (document.Header == "6VSM")
                {
                    br.ReadInt32();
                }

                byte[] markerBytes = br.ReadBytes(8);
                document.Marker = BitConverter.ToString(markerBytes);

                if (document.Header == "ERTM")
                {
                    br.ReadInt32();
                }

                int countBlocks = br.ReadInt32();
                int idx = 1;
                Encoding txtEncoding = document.Header == "6VSM" ? Encoding.UTF8 : Encoding.GetEncoding(MainMenu.settings.ASCII_N);

                if (document.Marker == MarkerSimple)
                {
                    for (int i = 0; i < countBlocks; i++)
                    {
                        br.ReadBytes(8);
                        if (document.Header == "ERTM") br.ReadInt32();
                        int len = br.ReadInt32();
                        byte[] valueBytes = br.ReadBytes(len);
                        document.Entries.Add(new PropEditorEntry { Index = idx++, Value = txtEncoding.GetString(valueBytes) });
                    }
                }
                else if (document.Marker == MarkerNested)
                {
                    for (int i = 0; i < countBlocks; i++)
                    {
                        br.ReadBytes(8);
                        if (document.Header == "ERTM") br.ReadInt32();

                        int subCount = br.ReadInt32();
                        for (int j = 0; j < subCount * 2; j++)
                        {
                            int len = br.ReadInt32();
                            byte[] valueBytes = br.ReadBytes(len);
                            document.Entries.Add(new PropEditorEntry { Index = idx++, Value = txtEncoding.GetString(valueBytes) });
                        }
                    }
                }
                else
                {
                    throw new InvalidDataException("PROP marker not supported by current editor: " + document.Marker);
                }
            }

            return document;
        }

        public static void Save(PropEditorDocument document, IEnumerable<string> updatedValues, string outputPath)
        {
            List<string> values = updatedValues.Select(v => v ?? string.Empty).ToList();

            byte[] binContent = File.ReadAllBytes(document.FilePath);
            using (MemoryStream ms = new MemoryStream(binContent))
            using (BinaryReader br = new BinaryReader(ms))
            using (FileStream fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                int blockSize = 0;
                int blHeadSize = 0;

                byte[] header = br.ReadBytes(4);
                string headerText = Encoding.ASCII.GetString(header);
                bw.Write(header);

                if ((headerText == "5VSM") || (headerText == "6VSM"))
                {
                    int oldHead = br.ReadInt32();
                    bw.Write(oldHead);
                    byte[] subBlock = br.ReadBytes(8);
                    bw.Write(subBlock);
                }

                int count = br.ReadInt32();
                bw.Write(count);
                for (int i = 0; i < count; i++)
                {
                    bw.Write(br.ReadBytes(8));
                    bw.Write(br.ReadBytes(4));
                }

                int one = br.ReadInt32();
                bw.Write(one);
                blHeadSize += 4;

                int unknown = br.ReadInt32();
                bw.Write(unknown);
                blHeadSize += 4;

                if (headerText != "6VSM")
                {
                    int blLen = br.ReadInt32();
                    bw.Write(blLen);
                    byte[] block = br.ReadBytes(blLen - 4);
                    bw.Write(block);
                    blHeadSize += blLen;
                }

                int posBlSize = (int)br.BaseStream.Position;
                int orBlSize = br.ReadInt32();
                bw.Write(orBlSize);
                blockSize += 4;
                blHeadSize += 4;

                one = br.ReadInt32();
                bw.Write(one);
                blockSize += 4;
                blHeadSize += 4;

                if (headerText == "6VSM")
                {
                    one = br.ReadInt32();
                    bw.Write(one);
                    blockSize += 4;
                    blHeadSize += 4;
                }

                byte[] marker = br.ReadBytes(8);
                string markerText = BitConverter.ToString(marker);
                bw.Write(marker);
                blockSize += 8;
                blHeadSize += 8;

                if (headerText == "ERTM")
                {
                    one = br.ReadInt32();
                    bw.Write(one);
                    blockSize += 4;
                }

                count = br.ReadInt32();
                bw.Write(count);
                blockSize += 4;
                blHeadSize += 4;

                int c = 0;
                Encoding txtEncoding = headerText == "6VSM" ? Encoding.UTF8 : Encoding.GetEncoding(MainMenu.settings.ASCII_N);

                if (markerText == MarkerSimple)
                {
                    for (int i = 0; i < count; i++)
                    {
                        bw.Write(br.ReadBytes(8));
                        blockSize += 8;
                        blHeadSize += 8;

                        if (headerText == "ERTM")
                        {
                            one = br.ReadInt32();
                            bw.Write(one);
                            blockSize += 4;
                        }

                        int oldLen = br.ReadInt32();
                        br.ReadBytes(oldLen);

                        byte[] value = txtEncoding.GetBytes(c < values.Count ? values[c] : string.Empty);
                        bw.Write(value.Length);
                        bw.Write(value);

                        blockSize += 4 + value.Length;
                        blHeadSize += 4 + value.Length;
                        c++;
                    }
                }
                else if (markerText == MarkerNested)
                {
                    for (int i = 0; i < count; i++)
                    {
                        bw.Write(br.ReadBytes(8));
                        blockSize += 8;
                        blHeadSize += 8;

                        if (headerText == "ERTM")
                        {
                            one = br.ReadInt32();
                            bw.Write(one);
                            blockSize += 4;
                        }

                        int subCount = br.ReadInt32();
                        bw.Write(subCount);
                        blockSize += 4;
                        blHeadSize += 4;

                        for (int j = 0; j < subCount * 2; j++)
                        {
                            int oldLen = br.ReadInt32();
                            br.ReadBytes(oldLen);

                            byte[] value = txtEncoding.GetBytes(c < values.Count ? values[c] : string.Empty);
                            bw.Write(value.Length);
                            bw.Write(value);

                            blockSize += 4 + value.Length;
                            blHeadSize += 4 + value.Length;
                            c++;
                        }
                    }
                }
                else
                {
                    throw new InvalidDataException("PROP marker not supported by current editor: " + markerText);
                }

                bw.BaseStream.Seek(posBlSize, SeekOrigin.Begin);
                bw.Write(blockSize);

                if ((headerText == "5VSM") || (headerText == "6VSM"))
                {
                    bw.BaseStream.Seek(4, SeekOrigin.Begin);
                    bw.Write(blHeadSize);
                }
            }
        }
    }
}
