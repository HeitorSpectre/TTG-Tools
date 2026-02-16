using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TTG_Tools
{
    internal static class PropInspectorParser
    {
        internal sealed class PropNode
        {
            public string Name { get; set; }
            public string Value { get; set; }
            public long Offset { get; set; }
            public long Length { get; set; }
            public List<PropNode> Children { get; private set; }

            public PropNode()
            {
                Children = new List<PropNode>();
            }
        }

        public static PropNode Parse(string filePath)
        {
            PropNode root = new PropNode();
            root.Name = Path.GetFileName(filePath);
            root.Value = "PROP file";

            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (BinaryReader br = new BinaryReader(fs))
            {
                ParseCore(br, root);
            }

            return root;
        }

        private static void ParseCore(BinaryReader br, PropNode root)
        {
            PropNode headerNode = ReadAscii(br, "Header", 4);
            root.Children.Add(headerNode);
            string header = headerNode.Value;

            if (header == "5VSM" || header == "6VSM")
            {
                root.Children.Add(ReadInt32(br, "Block Size"));
                root.Children.Add(ReadInt64(br, "Sub Block Size"));
            }

            int countHeaders = ReadInt32(br, "Header Entries Count", root).Item2;

            PropNode headersNode = new PropNode();
            headersNode.Name = "Header Entries";
            headersNode.Value = countHeaders.ToString();
            root.Children.Add(headersNode);

            for (int i = 0; i < countHeaders; i++)
            {
                PropNode entry = new PropNode();
                entry.Name = "Entry " + i;
                headersNode.Children.Add(entry);
                entry.Children.Add(ReadBytesNode(br, "CRC64", 8));
                entry.Children.Add(ReadInt32(br, "Value"));
            }

            root.Children.Add(ReadInt32(br, "One"));
            root.Children.Add(ReadInt32(br, "SomeValue1"));

            if (header != "6VSM")
            {
                int blSize1 = ReadInt32(br, "Block1Size", root).Item2;
                root.Children.Add(ReadBytesNode(br, "Block1Data", Math.Max(0, blSize1 - 4)));
            }

            root.Children.Add(ReadInt32(br, "Block2Size"));
            root.Children.Add(ReadInt32(br, "One1"));
            if (header == "6VSM")
            {
                root.Children.Add(ReadInt32(br, "One2"));
            }

            byte[] typeMarker = ReadBytesNode(br, "Primary Marker", 8).ValueBytes;
            root.Children.Add(CreateBytesNode("Primary Marker", typeMarker, br.BaseStream.Position - typeMarker.Length));

            string marker = BitConverter.ToString(typeMarker);

            if (marker == "B4-F4-5A-5F-60-6E-9C-CD")
            {
                ParseStringBlock(br, root, header);
            }
            else if (marker == "25-03-C6-1F-D8-64-1B-4F")
            {
                ParseNestedStringBlock(br, root, header);
            }

            long remain = br.BaseStream.Length - br.BaseStream.Position;
            if (remain > 0)
            {
                root.Children.Add(ReadBytesNode(br, "Trailing/Hidden Data", (int)remain));
            }
        }

        private static void ParseStringBlock(BinaryReader br, PropNode parent, string header)
        {
            PropNode blockNode = new PropNode { Name = "String Block (Marker B4-F4-5A-5F-60-6E-9C-CD)", Value = string.Empty };
            parent.Children.Add(blockNode);

            if (header == "ERTM")
            {
                blockNode.Children.Add(ReadInt32(br, "Optional One"));
            }

            int countBlocks = ReadInt32(br, "Entries Count", blockNode).Item2;
            for (int i = 0; i < countBlocks; i++)
            {
                PropNode entry = new PropNode { Name = "Entry " + i };
                blockNode.Children.Add(entry);
                entry.Children.Add(ReadBytesNode(br, "Name CRC64", 8));

                if (header == "ERTM")
                {
                    entry.Children.Add(ReadInt32(br, "Optional One"));
                }

                int len = ReadInt32(br, "String Length", entry).Item2;
                entry.Children.Add(ReadStringByHeader(br, "String", len, header));
            }
        }

        private static void ParseNestedStringBlock(BinaryReader br, PropNode parent, string header)
        {
            PropNode blockNode = new PropNode { Name = "Nested Block (Marker 25-03-C6-1F-D8-64-1B-4F)", Value = string.Empty };
            parent.Children.Add(blockNode);

            int count = ReadInt32(br, "Blocks Count", blockNode).Item2;
            for (int i = 0; i < count; i++)
            {
                PropNode block = new PropNode { Name = "Block " + i };
                blockNode.Children.Add(block);
                block.Children.Add(ReadBytesNode(br, "Block CRC64", 8));

                if (header == "ERTM")
                {
                    block.Children.Add(ReadInt32(br, "Optional One"));
                }

                int subCount = ReadInt32(br, "Sub Count", block).Item2;
                for (int j = 0; j < subCount * 2; j++)
                {
                    int len = ReadInt32(br, "Value Length " + j, block).Item2;
                    block.Children.Add(ReadStringByHeader(br, "Value " + j, len, header));
                }
            }
        }

        private static PropNode ReadStringByHeader(BinaryReader br, string name, int length, string header)
        {
            long offset = br.BaseStream.Position;
            byte[] data = br.ReadBytes(length);

            Encoding enc = header == "6VSM" ? Encoding.UTF8 : Encoding.GetEncoding(MainMenu.settings.ASCII_N);
            string value = enc.GetString(data);

            PropNode node = new PropNode();
            node.Name = name;
            node.Value = value;
            node.Offset = offset;
            node.Length = data.Length;
            node.Children.Add(CreateBytesNode("Raw", data, offset));
            return node;
        }

        private sealed class BytesNodeResult : PropNode
        {
            public byte[] ValueBytes { get; set; }
        }

        private static BytesNodeResult ReadBytesNode(BinaryReader br, string name, int length)
        {
            long offset = br.BaseStream.Position;
            byte[] bytes = br.ReadBytes(length);
            return new BytesNodeResult
            {
                Name = name,
                Value = BitConverter.ToString(bytes),
                Offset = offset,
                Length = bytes.Length,
                ValueBytes = bytes
            };
        }

        private static PropNode CreateBytesNode(string name, byte[] bytes, long offset)
        {
            PropNode node = new PropNode();
            node.Name = name;
            node.Value = BitConverter.ToString(bytes);
            node.Offset = offset;
            node.Length = bytes.Length;
            return node;
        }

        private static System.Tuple<PropNode, int> ReadInt32(BinaryReader br, string name, PropNode appendTo)
        {
            System.Tuple<PropNode, int> result = ReadInt32(br, name);
            appendTo.Children.Add(result.Item1);
            return result;
        }

        private static System.Tuple<PropNode, int> ReadInt32(BinaryReader br, string name)
        {
            long offset = br.BaseStream.Position;
            int val = br.ReadInt32();
            PropNode node = new PropNode { Name = name, Value = val.ToString(), Offset = offset, Length = 4 };
            return new System.Tuple<PropNode, int>(node, val);
        }

        private static PropNode ReadInt64(BinaryReader br, string name)
        {
            long offset = br.BaseStream.Position;
            long val = br.ReadInt64();
            return new PropNode { Name = name, Value = val.ToString(), Offset = offset, Length = 8 };
        }

        private static PropNode ReadAscii(BinaryReader br, string name, int length)
        {
            long offset = br.BaseStream.Position;
            byte[] bytes = br.ReadBytes(length);
            return new PropNode { Name = name, Value = Encoding.ASCII.GetString(bytes), Offset = offset, Length = bytes.Length };
        }
    }
}
