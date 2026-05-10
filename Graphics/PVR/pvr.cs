namespace TTG_Tools.Graphics.PVR
{
    public static class pvr
    {
        public enum HeaderFormat : ulong
        {
            PVRTC4bppRGB = 2,
            PVRTC4bppRGBA = 3,
            PVRTCII2Bpp = 4,
            PVRTCII4Bpp = 5,
            ETC1 = 6,
            DXT1 = 7,
            BC1 = 7,
            DXT2 = 8,
            DXT3 = 9,
            BC2 = 9,
            DXT4 = 10,
            DXT5 = 11,
            BC3 = 11,
            BC4 = 12,
            BC5 = 13,
            BC6 = 14,
            BC7 = 15,
            UYVY = 16,
            YUY2 = 17,
            BW1bpp = 18,
            R9G9B9E5SE = 19, //RGBE9995 Shared Exponent
            RGBG8888 = 20,
            GRGB8888 = 21,
            ETC2RGB = 22,
            ETC2RGBA = 23,
            ETC2RGBA1 = 24,
            EACR11 = 25,
            EACRG11 = 26
        }

        public struct header
        {
            public uint Version;
            public uint Flags;
            public ulong PixelFormat;
            public uint ColorSpace;
            public uint ChannelType;
            public uint Height;
            public uint Width;
            public uint Depth;
            public uint Surface;
            public uint Face;
            public uint Mip;
            public uint MetaSize;
        }

        public struct metaHeader
        {
            public uint FourCC;
            public uint Key;
            public uint DataSize;
            public byte[] Data;
            public byte[] padding; //+8 bytes
        }
    }
}
