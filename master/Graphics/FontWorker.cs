using System;
using System.IO;
using System.Text;

namespace TTG_Tools.Graphics
{
    public class FontWorker
    {
        public static string DoWork(string inputFile, bool extract)
        {
            FileInfo fi = new FileInfo(inputFile);

            string ps2Result;
            if (extract && Swizzles.PS2.TryExtractContainer(inputFile, MainMenu.settings.pathForOutputFolder, out ps2Result))
            {
                return ps2Result;
            }

            if (MainMenu.settings.swizzlePS2 && !extract && Swizzles.PS2.TryRepackContainer(inputFile, fi.DirectoryName, MainMenu.settings.pathForOutputFolder, out ps2Result))
            {
                return ps2Result;
            }

            string wiiResult;
            if (extract && WiiSupport.TryExtractWiiContainer(inputFile, MainMenu.settings.pathForOutputFolder, out wiiResult))
            {
                return wiiResult;
            }

            if (MainMenu.settings.swizzleNintendoWii && !extract && WiiSupport.TryRepackWiiContainer(inputFile, fi.DirectoryName, MainMenu.settings.pathForOutputFolder, out wiiResult))
            {
                return wiiResult;
            }

            byte[] vectorFont = null;
            int vecFontSize = -1;
            string modFile = Methods.GetNameOfFileOnly(inputFile, ".font") + ".ttf";

            if (File.Exists(modFile))
            {
                FileInfo fi2 = new FileInfo(modFile);
                vectorFont = File.ReadAllBytes(fi2.FullName);
                vecFontSize = vectorFont.Length;
            }

            FileStream fs = new FileStream(fi.FullName, FileMode.Open);
            BinaryReader br = new BinaryReader(fs);

            try
            {
                byte[] header = br.ReadBytes(4);
                br.BaseStream.Seek(16, SeekOrigin.Begin);
                int version = br.ReadInt32(); //element count; a vector font has exactly one element

                //A vector .font embeds a TTF/OTF and is identified by a single header element
                //carrying the class marker 81-53-37-63-9E-4A-3A-9A. Normal bitmap/atlas fonts
                //(e.g. Poker Night Remastered's default.font, which has several elements) and
                //non-6VSM files have no vector payload, so skip them gracefully instead of
                //misreading the structure and crashing with EndOfStreamException.
                byte[] firstElement = br.ReadBytes(8);
                if (Encoding.ASCII.GetString(header) != "6VSM" || version != 1 || !IsVectorFontMarker(firstElement))
                {
                    return "This file doesn't have vector fonts: " + fi.Name;
                }

                br.BaseStream.Seek(4, SeekOrigin.Begin);
                int blockSize = br.ReadInt32();
                ulong someValue = br.ReadUInt64();

                br.BaseStream.Seek(4, SeekOrigin.Current);

                ulong crcFontClass = br.ReadUInt64();
                uint someData = br.ReadUInt32();

                int blockNameLen = br.ReadInt32();

                byte[] fontName = br.ReadBytes(blockNameLen - 4); //Skip block of font name
                byte val = br.ReadByte();
                float fontBaseLine = br.ReadSingle(); //Base line?
                float fontCharSize = br.ReadSingle();

                int blockLen1 = br.ReadInt32();
                byte[] block1 = br.ReadBytes(blockLen1 - 4);
                int blockLen2 = br.ReadInt32();
                byte[] block2 = br.ReadBytes(blockLen2 - 4);

                byte[] boolVals = br.ReadBytes(3);

                int blockFontSize = br.ReadInt32();
                int fontSize = br.ReadInt32();

                byte[] font = br.ReadBytes(fontSize);

                byte[] endBlock = br.ReadBytes((int)(fs.Length - br.BaseStream.Position));

                br.Close();
                fs.Close();

                if (extract)
                {
                    string outputFile = MainMenu.settings.pathForOutputFolder + Path.DirectorySeparatorChar + fi.Name.Remove(fi.Name.Length - 4, 4) + "ttf";
                    File.WriteAllBytes(outputFile, font);
                    return "File " + fi.Name + " successfully extracted";
                }

                int diff = vecFontSize - fontSize;

                blockSize += diff;
                blockFontSize += diff;
                fontSize += diff;

                if (File.Exists(MainMenu.settings.pathForOutputFolder + "\\" + fi.Name)) File.Delete(MainMenu.settings.pathForOutputFolder + "\\" + fi.Name);

                using (FileStream outFs = new FileStream(MainMenu.settings.pathForOutputFolder + "\\" + fi.Name, FileMode.CreateNew))
                using (BinaryWriter bw = new BinaryWriter(outFs))
                {
                    bw.Write(header);
                    bw.Write(blockSize);
                    bw.Write(someValue);
                    bw.Write(version);
                    bw.Write(crcFontClass);
                    bw.Write(someData);
                    bw.Write(blockNameLen);
                    bw.Write(fontName);
                    bw.Write(val);
                    bw.Write(fontBaseLine);
                    bw.Write(fontCharSize);
                    bw.Write(blockLen1);
                    bw.Write(block1);
                    bw.Write(blockLen2);
                    bw.Write(block2);
                    bw.Write(boolVals);
                    bw.Write(blockFontSize);
                    bw.Write(fontSize);
                    bw.Write(vectorFont);
                    bw.Write(endBlock);
                }

                return "File " + fi.Name + " successfully imported";
            }
            catch (Exception ex)
            {
                //Never crash the whole tool on an unexpected font layout; report and move on.
                return "Couldn't process vector font " + fi.Name + ": " + ex.Message;
            }
            finally
            {
                br.Close();
                fs.Close();
            }
        }

        //The 8-byte class marker every vector .font carries as its single header element.
        private static readonly byte[] VectorFontMarker = { 0x81, 0x53, 0x37, 0x63, 0x9E, 0x4A, 0x3A, 0x9A };

        private static bool IsVectorFontMarker(byte[] element)
        {
            if (element == null || element.Length < 8) return false;
            for (int i = 0; i < 8; i++)
            {
                if (element[i] != VectorFontMarker[i]) return false;
            }
            return true;
        }
    }
}
