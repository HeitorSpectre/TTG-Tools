using System;
using System.Drawing;
using System.IO;

namespace TTG_Tools.Graphics.DDS
{
    public class SomeActions
    {
        public static byte[] ConvertARGBtoBGRA(byte[] data)
        {
            byte[] result = new byte[data.Length];
            uint offset = 0;

            using (MemoryStream ms = new MemoryStream(data))
            {
                using (BinaryReader br = new BinaryReader(ms))
                {
                    for (int i = 0; i < data.Length; i += 4)
                    {
                        byte[] argb = br.ReadBytes(4);

                        byte[] bgra = new byte[4];
                        bgra[0] = argb[3];
                        bgra[1] = argb[2];
                        bgra[2] = argb[1];
                        bgra[3] = argb[0];

                        Array.Copy(bgra, 0, result, offset, bgra.Length);
                        offset += 4;
                    }
                }
            }

            return result;
        }

        public static byte[] ConvertBGRAtoARGB(byte[] data)
        {
            byte[] result = new byte[data.Length];
            uint offset = 0;

            using (MemoryStream ms = new MemoryStream(data))
            {
                using (BinaryReader br = new BinaryReader(ms))
                {
                    for (int i = 0; i < data.Length; i += 4)
                    {
                        byte[] bgra = br.ReadBytes(4);

                        byte[] argb = new byte[4];
                        argb[0] = bgra[3];
                        argb[1] = bgra[1];
                        argb[2] = bgra[2];
                        argb[3] = bgra[0];

                        Array.Copy(argb, 0, result, offset, argb.Length);
                        offset += 4;
                    }
                }
            }

            return result;
        }
    }
}
