using System;
using System.Collections.Generic;
using System.IO;
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

        private static readonly HashSet<string> LanguageNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "French", "English", "Japanese", "German", "Spanish", "Portuguese", "Italian", "Russian", "Polish",
            "Chinese", "Korean", "Dutch", "Czech", "Arabic", "Turkish", "Thai", "Hungarian"
        };

        public static PropNode Parse(string filePath)
        {
            PropNode root = new PropNode();
            root.Name = Path.GetFileName(filePath);
            root.Value = "Properties";

            List<string> strings = ExtractPropStrings(filePath);

            PropNode properties = new PropNode();
            properties.Name = "Properties";
            properties.Value = string.Empty;
            root.Children.Add(properties);

            if (strings.Count == 0)
            {
                PropNode none = new PropNode();
                none.Name = "(No property strings found)";
                none.Value = string.Empty;
                properties.Children.Add(none);
                return root;
            }

            AddLocalizedView(properties, strings);
            AddFlatEntriesView(properties, strings);

            return root;
        }

        private static void AddLocalizedView(PropNode properties, List<string> strings)
        {
            PropNode localized = new PropNode();
            localized.Name = "Localized Text";
            localized.Value = string.Empty;

            Dictionary<string, List<string>> valuesByLang = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < strings.Count - 1; i++)
            {
                string key = Clean(strings[i]);
                string value = Clean(strings[i + 1]);

                if (LanguageNames.Contains(key) && !string.IsNullOrEmpty(value) && !LanguageNames.Contains(value))
                {
                    if (!valuesByLang.ContainsKey(key)) valuesByLang[key] = new List<string>();
                    valuesByLang[key].Add(value);
                }
            }

            if (valuesByLang.Count == 0)
            {
                return;
            }

            foreach (KeyValuePair<string, List<string>> item in valuesByLang)
            {
                PropNode langNode = new PropNode();
                langNode.Name = item.Key;
                langNode.Value = string.Empty;

                for (int i = 0; i < item.Value.Count; i++)
                {
                    PropNode txtNode = new PropNode();
                    txtNode.Name = "Text " + (i + 1).ToString();
                    txtNode.Value = item.Value[i];
                    langNode.Children.Add(txtNode);
                }

                localized.Children.Add(langNode);
            }

            properties.Children.Add(localized);
        }

        private static void AddFlatEntriesView(PropNode properties, List<string> strings)
        {
            PropNode entries = new PropNode();
            entries.Name = "Entries";
            entries.Value = strings.Count.ToString();

            for (int i = 0; i < strings.Count; i++)
            {
                string val = Clean(strings[i]);
                if (string.IsNullOrEmpty(val)) continue;

                PropNode node = new PropNode();
                node.Name = "Entry " + (i + 1).ToString();
                node.Value = val;
                entries.Children.Add(node);
            }

            properties.Children.Add(entries);
        }

        private static List<string> ExtractPropStrings(string filePath)
        {
            List<string> results = new List<string>();

            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (BinaryReader br = new BinaryReader(fs))
            {
                string header = Encoding.ASCII.GetString(br.ReadBytes(4));

                if (header == "5VSM" || header == "6VSM")
                {
                    br.ReadInt32();
                    br.ReadInt64();
                }

                int countHeaders = SafeReadInt32(br);
                for (int i = 0; i < countHeaders; i++)
                {
                    br.ReadBytes(8);
                    br.ReadBytes(4);
                }

                br.ReadInt32();
                br.ReadInt32();

                if (header != "6VSM")
                {
                    int blSize1 = SafeReadInt32(br);
                    int toSkip = blSize1 - 4;
                    if (toSkip > 0) br.ReadBytes(toSkip);
                }

                br.ReadInt32();
                br.ReadInt32();
                if (header == "6VSM") br.ReadInt32();

                byte[] markerBytes = br.ReadBytes(8);
                string marker = BitConverter.ToString(markerBytes);

                Encoding enc = header == "6VSM" ? Encoding.UTF8 : Encoding.GetEncoding(MainMenu.settings.ASCII_N);

                if (marker == "B4-F4-5A-5F-60-6E-9C-CD")
                {
                    if (header == "ERTM") br.ReadInt32();

                    int countBlocks = SafeReadInt32(br);
                    for (int i = 0; i < countBlocks; i++)
                    {
                        br.ReadBytes(8);
                        if (header == "ERTM") br.ReadInt32();

                        int len = SafeReadInt32(br);
                        if (len <= 0 || len > (br.BaseStream.Length - br.BaseStream.Position)) break;

                        byte[] valueData = br.ReadBytes(len);
                        results.Add(enc.GetString(valueData));
                    }
                }
                else if (marker == "25-03-C6-1F-D8-64-1B-4F")
                {
                    if (header == "ERTM") br.ReadInt32();

                    int countBlocks = SafeReadInt32(br);
                    for (int i = 0; i < countBlocks; i++)
                    {
                        br.ReadBytes(8);
                        if (header == "ERTM") br.ReadInt32();

                        int countSubBlocks = SafeReadInt32(br);
                        for (int j = 0; j < countSubBlocks * 2; j++)
                        {
                            int len = SafeReadInt32(br);
                            if (len <= 0 || len > (br.BaseStream.Length - br.BaseStream.Position)) break;

                            byte[] valueData = br.ReadBytes(len);
                            results.Add(enc.GetString(valueData));
                        }
                    }
                }
                else
                {
                    // Fallback: scan printable strings from remaining file data
                    byte[] tail = br.ReadBytes((int)(br.BaseStream.Length - br.BaseStream.Position));
                    ExtractPrintableStrings(results, tail, enc);
                }
            }

            return results;
        }

        private static void ExtractPrintableStrings(List<string> output, byte[] data, Encoding enc)
        {
            List<byte> current = new List<byte>();
            for (int i = 0; i < data.Length; i++)
            {
                byte b = data[i];
                bool printable = (b >= 32 && b <= 126) || b >= 128;

                if (printable)
                {
                    current.Add(b);
                }
                else
                {
                    FlushCurrent(output, current, enc);
                }
            }

            FlushCurrent(output, current, enc);
        }

        private static void FlushCurrent(List<string> output, List<byte> current, Encoding enc)
        {
            if (current.Count >= 3)
            {
                string s = Clean(enc.GetString(current.ToArray()));
                if (!string.IsNullOrEmpty(s)) output.Add(s);
            }

            current.Clear();
        }

        private static int SafeReadInt32(BinaryReader br)
        {
            if (br.BaseStream.Position + 4 > br.BaseStream.Length) return 0;
            return br.ReadInt32();
        }

        private static string Clean(string s)
        {
            if (s == null) return string.Empty;
            s = s.Replace("\0", string.Empty).Trim();
            return s;
        }
    }
}
