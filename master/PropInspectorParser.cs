using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TTG_Tools
{
    internal static class PropInspectorParser
    {
        internal class PropNode
        {
            public string Name { get; set; }
            public string Value { get; set; }
            public List<PropNode> Children { get; private set; }

            public PropNode()
            {
                Children = new List<PropNode>();
            }
        }

        private sealed class PropPair
        {
            public string Key;
            public string Value;
        }

        private sealed class PropBlock
        {
            public string Label;
            public List<PropPair> Pairs = new List<PropPair>();
            public List<string> Values = new List<string>();
        }

        private static readonly HashSet<string> LanguageNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "French", "English", "Japanese", "German", "Spanish", "Portuguese", "Italian", "Russian", "Polish",
            "Chinese", "Korean", "Dutch", "Czech", "Arabic", "Turkish", "Thai", "Hungarian", "Brazilian Portuguese"
        };

        public static PropNode Parse(string filePath)
        {
            PropNode root = new PropNode();
            root.Name = Path.GetFileName(filePath);
            root.Value = "Properties";

            List<PropBlock> blocks = ExtractPropBlocks(filePath);

            PropNode properties = new PropNode();
            properties.Name = "Properties";
            properties.Value = string.Empty;
            root.Children.Add(properties);

            if (blocks.Count == 0)
            {
                properties.Children.Add(new PropNode { Name = "(No properties found)", Value = string.Empty });
                return root;
            }

            for (int i = 0; i < blocks.Count; i++)
            {
                PropNode blockNode = BuildBlockNode(blocks[i], i + 1);
                if (blockNode != null) properties.Children.Add(blockNode);
            }

            if (properties.Children.Count == 0)
            {
                properties.Children.Add(new PropNode { Name = "(No readable properties found)", Value = string.Empty });
            }

            return root;
        }

        private static PropNode BuildBlockNode(PropBlock block, int index)
        {
            string title = string.IsNullOrEmpty(block.Label) ? ("Property " + index.ToString()) : block.Label;
            PropNode blockNode = new PropNode { Name = title, Value = string.Empty };

            if (block.Pairs.Count > 0)
            {
                AddPairsTree(blockNode, block.Pairs);
            }
            else
            {
                int shown = 0;
                for (int i = 0; i < block.Values.Count; i++)
                {
                    if (!IsUsefulString(block.Values[i])) continue;
                    blockNode.Children.Add(new PropNode { Name = "Value " + (shown + 1).ToString(), Value = Clean(block.Values[i]) });
                    shown++;
                }
            }

            if (blockNode.Children.Count == 0)
                return null;

            return blockNode;
        }

        private static void AddPairsTree(PropNode parent, List<PropPair> pairs)
        {
            int i = 0;
            while (i < pairs.Count)
            {
                PropPair pair = pairs[i];
                string key = Clean(pair.Key);
                string value = Clean(pair.Value);

                if (!IsUsefulString(key) && !IsUsefulString(value))
                {
                    i++;
                    continue;
                }

                if (key.Equals("Description", StringComparison.OrdinalIgnoreCase))
                {
                    PropNode descNode = new PropNode { Name = "Description", Value = string.Empty };
                    int j = i + 1;
                    bool hasLang = false;

                    while (j < pairs.Count)
                    {
                        string lk = Clean(pairs[j].Key);
                        string lv = Clean(pairs[j].Value);

                        if (!LanguageNames.Contains(lk)) break;
                        if (IsUsefulString(lv))
                        {
                            descNode.Children.Add(new PropNode { Name = lk, Value = lv });
                            hasLang = true;
                        }

                        j++;
                    }

                    if (hasLang)
                    {
                        parent.Children.Add(descNode);
                        i = j;
                        continue;
                    }
                }

                if (LanguageNames.Contains(key) && IsUsefulString(value))
                {
                    PropNode localized = parent.Children.FirstOrDefault(n => n.Name == "Localized Text");
                    if (localized == null)
                    {
                        localized = new PropNode { Name = "Localized Text", Value = string.Empty };
                        parent.Children.Add(localized);
                    }

                    localized.Children.Add(new PropNode { Name = key, Value = value });
                    i++;
                    continue;
                }

                if (IsUsefulString(key))
                {
                    parent.Children.Add(new PropNode { Name = key, Value = IsUsefulString(value) ? value : string.Empty });
                }
                else if (IsUsefulString(value))
                {
                    parent.Children.Add(new PropNode { Name = "Value", Value = value });
                }

                i++;
            }
        }

        private static List<PropBlock> ExtractPropBlocks(string filePath)
        {
            List<PropBlock> blocks = new List<PropBlock>();

            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (BinaryReader br = new BinaryReader(fs))
            {
                string header = Encoding.ASCII.GetString(br.ReadBytes(4));

                if (header == "5VSM" || header == "6VSM")
                {
                    SafeReadInt32(br);
                    SafeReadInt64(br);
                }

                int countHeaders = SafeReadInt32(br);
                for (int i = 0; i < countHeaders; i++)
                {
                    br.ReadBytes(8);
                    br.ReadBytes(4);
                }

                SafeReadInt32(br);
                SafeReadInt32(br);

                if (header != "6VSM")
                {
                    int blSize1 = SafeReadInt32(br);
                    int toSkip = blSize1 - 4;
                    if (toSkip > 0 && toSkip <= br.BaseStream.Length - br.BaseStream.Position) br.ReadBytes(toSkip);
                }

                SafeReadInt32(br);
                SafeReadInt32(br);
                if (header == "6VSM") SafeReadInt32(br);

                byte[] markerBytes = br.ReadBytes(8);
                string marker = BitConverter.ToString(markerBytes);

                if (marker == "B4-F4-5A-5F-60-6E-9C-CD")
                {
                    if (header == "ERTM") SafeReadInt32(br);

                    int countBlocks = SafeReadInt32(br);
                    for (int i = 0; i < countBlocks; i++)
                    {
                        br.ReadBytes(8);
                        if (header == "ERTM") SafeReadInt32(br);

                        int len = SafeReadInt32(br);
                        if (len <= 0 || len > (br.BaseStream.Length - br.BaseStream.Position)) break;

                        byte[] valueData = br.ReadBytes(len);
                        string value = DecodeBest(valueData);

                        PropBlock block = new PropBlock();
                        block.Label = "Property " + (i + 1).ToString();
                        block.Values.Add(value);
                        blocks.Add(block);
                    }
                }
                else if (marker == "25-03-C6-1F-D8-64-1B-4F")
                {
                    if (header == "ERTM") SafeReadInt32(br);

                    int countBlocks = SafeReadInt32(br);
                    for (int i = 0; i < countBlocks; i++)
                    {
                        br.ReadBytes(8);
                        if (header == "ERTM") SafeReadInt32(br);

                        int countSubBlocks = SafeReadInt32(br);
                        List<string> values = new List<string>();

                        for (int j = 0; j < countSubBlocks * 2; j++)
                        {
                            int len = SafeReadInt32(br);
                            if (len <= 0 || len > (br.BaseStream.Length - br.BaseStream.Position)) break;

                            byte[] valueData = br.ReadBytes(len);
                            values.Add(DecodeBest(valueData));
                        }

                        PropBlock block = new PropBlock();
                        block.Label = InferBlockName(values, i + 1);

                        for (int j = 0; j + 1 < values.Count; j += 2)
                        {
                            PropPair pair = new PropPair();
                            pair.Key = values[j];
                            pair.Value = values[j + 1];
                            block.Pairs.Add(pair);
                        }

                        if (block.Pairs.Count == 0)
                            block.Values.AddRange(values);

                        blocks.Add(block);
                    }
                }
                else
                {
                    byte[] tail = br.ReadBytes((int)(br.BaseStream.Length - br.BaseStream.Position));
                    List<string> strs = ExtractPrintableStrings(tail);
                    if (strs.Count > 0)
                    {
                        PropBlock block = new PropBlock();
                        block.Label = "Property 1";
                        block.Values.AddRange(strs);
                        blocks.Add(block);
                    }
                }
            }

            return blocks;
        }

        private static string InferBlockName(List<string> values, int index)
        {
            for (int i = 0; i < values.Count; i += 2)
            {
                string k = Clean(values[i]);
                if (IsUsefulString(k) && !LanguageNames.Contains(k) && k.Length <= 40)
                    return k;
            }

            return "Property " + index.ToString();
        }

        private static List<string> ExtractPrintableStrings(byte[] data)
        {
            List<string> result = new List<string>();
            List<byte> current = new List<byte>();

            for (int i = 0; i < data.Length; i++)
            {
                byte b = data[i];
                bool printable = (b >= 32 && b <= 126) || b >= 128;
                if (printable) current.Add(b);
                else FlushCurrent(result, current);
            }

            FlushCurrent(result, current);
            return result;
        }

        private static void FlushCurrent(List<string> result, List<byte> current)
        {
            if (current.Count >= 3)
            {
                string decoded = DecodeBest(current.ToArray());
                if (IsUsefulString(decoded)) result.Add(Clean(decoded));
            }

            current.Clear();
        }

        private static string DecodeBest(byte[] data)
        {
            if (data == null || data.Length == 0) return string.Empty;

            List<Encoding> candidates = new List<Encoding>
            {
                Encoding.UTF8,
                Encoding.GetEncoding(MainMenu.settings.ASCII_N),
                Encoding.GetEncoding(1252),
                Encoding.Unicode,
                Encoding.BigEndianUnicode
            };

            string best = string.Empty;
            double bestScore = double.MinValue;

            for (int i = 0; i < candidates.Count; i++)
            {
                string text;
                try { text = candidates[i].GetString(data); }
                catch { continue; }

                text = Clean(text);
                double score = ScoreText(text);

                if (score > bestScore)
                {
                    bestScore = score;
                    best = text;
                }
            }

            return best;
        }

        private static double ScoreText(string text)
        {
            if (string.IsNullOrEmpty(text)) return -1000;

            int printable = 0;
            int letters = 0;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (!char.IsControl(c)) printable++;
                if (char.IsLetterOrDigit(c) || char.IsWhiteSpace(c) || char.IsPunctuation(c)) letters++;
            }

            double ratioPrintable = (double)printable / Math.Max(1, text.Length);
            double ratioLetters = (double)letters / Math.Max(1, text.Length);
            return (ratioPrintable * 2.0) + ratioLetters;
        }

        private static bool IsUsefulString(string text)
        {
            text = Clean(text);
            if (text.Length == 0) return false;
            if (text.Length > 2048) return false;
            return ScoreText(text) > 1.4;
        }

        private static int SafeReadInt32(BinaryReader br)
        {
            if (br.BaseStream.Position + 4 > br.BaseStream.Length) return 0;
            return br.ReadInt32();
        }

        private static long SafeReadInt64(BinaryReader br)
        {
            if (br.BaseStream.Position + 8 > br.BaseStream.Length) return 0;
            return br.ReadInt64();
        }

        private static string Clean(string s)
        {
            if (s == null) return string.Empty;
            return s.Replace("\0", string.Empty).Trim();
        }
    }
}
