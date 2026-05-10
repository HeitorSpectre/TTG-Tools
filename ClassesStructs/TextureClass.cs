namespace TTG_Tools.ClassesStructs
{
    //Experiments with oldest format textures
    public class TextureClass
    {
        public enum NewTextureFormat
        {
            ARGB8 = 0,
            ARGB4 = 4,
            A8 = 0x10,
            IL8 = 0x11,
            BC1 = 0x40,
            BC2 = 0x41,
            BC3 = 0x42,
            BC4 = 0x43,
            BC5 = 0x44,
            BC6 = 0x45,
            BC7 = 0x46
        }

        //Enum texture format for old iOS games
        public enum OldTexturePVRFormat
        {
            PVRTC_RGB565_1 = 0x13,
            PVRTC_RGB565_2 = 0x113,
            PVRTC_RGBA8 = 0x8011,
            PVRTC_RGBA4 = 0x8110,
            PVRTC_RGBA1555 = 0x8010,
            PVRTC4bppRGBA_1 = 0x830d,
            PVRTC4bppRGBA_2 = 0x820d,
            PVRTC4bppRGBA_3 = 0x20d,
            PVRTC4bppRGBA_4 = 0x8a0d,
            PVRTC4bppRGBA_5 = 0x8b0d,
            PVRTC4bppRGB_1 = 0xa0d,
            PVRTC4bppRGB_2 = 0x30d
        }

        //ARGB8888, ARGB4444, A8, IL8
        public static uint[] bpps = { 4, 2, 1, 16};

        //Enum texture format for old PC games
        public enum OldTextureFormat
        {
            DX_L8 = 0x32,
            DX_LA8 = 0x33,
            DX_ARGB8888 = 0x16,
            DX_DXT1 = 0x31545844,
            DX_DXT3 = 0x33545844,
            DX_DXT5 = 0x35545844
        }

        public class OldT3Texture
        {
            public bool isIOS;
            public bool isPS3;
            public uint mobTexFormat; //For iOS devices
            public int sizeBlock; //For games since Hector
            public int someValue; //For games since Hector
            public bool AdditionalSize;
            public string ObjectName;
            public string SubobjectName;
            public byte[] Flags;
            public int Mip;
            public uint TextureFormat;
            public int OriginalWidth;
            public int OriginalHeight;
            public int Width;
            public int Height;
            public byte[] UnknownData; //4 bytes
            public byte Zero;
            public FlagsClass TexFlags;
            public byte[] block;
            public int TexSize;
            public int BlockPos; //Need for correct font texture size
            public byte[] Content; //Texture

            public OldT3Texture() { }

            public OldT3Texture(OldT3Texture newClass)
            {
                isIOS = newClass.isIOS;
                isPS3 = newClass.isPS3;
                mobTexFormat = newClass.mobTexFormat;
                sizeBlock = newClass.sizeBlock;
                someValue = newClass.someValue;
                AdditionalSize = newClass.AdditionalSize;
                ObjectName = newClass.ObjectName;
                SubobjectName = newClass.SubobjectName;
                Flags = newClass.Flags;
                Mip = newClass.Mip;
                TextureFormat = newClass.TextureFormat;
                OriginalWidth = newClass.OriginalWidth;
                OriginalHeight = newClass.OriginalHeight;
                Width = newClass.Width;
                Height = newClass.Height;
                UnknownData = newClass.UnknownData;
                Zero = newClass.Zero;
                TexFlags = newClass.TexFlags;
                block = newClass.block;
                TexSize = newClass.TexSize;
                BlockPos = newClass.BlockPos;
                Content = newClass.Content;
            }
        }

        public class NewT3Texture
        {
            public struct UnknownFlags
            {
                public int blockSize; //equals 8 bytes
                public uint block; //Something values (used since Hector)
            }

            public struct Platform
            {
                //2 - PC/Mac
                //7 - iOS/Android (With PowerVR graphic chip)
                //9 - PS Vita
                //11 - PS4
                //15 - Nintendo Switch
                public int blockSize; //equals 8 bytes
                public uint platform;
            }

            public struct TextureStruct
            {
                public int SubTexNum; //Since value 5 (for array members?)
                public int CurrentMip;
                public int One;
                public int MipSize; //Size of one mipmap
                public int BlockSize;
                public byte[] Block;
            }

            public struct SubBlock
            {
                public int Size;
                public byte[] Block;
            }

            public struct TextureInfo
            {
                public int MipCount;
                public int SomeData; //For example in Poker Night 2 sometimes were textures with PNG files
                public uint TexSize;
                public TextureStruct[] Textures;
                public SubBlock[] SubBlocks;
                public int headerSize; //For a comfortable calculate sizes of headers
                public byte[] Content;
            }


            //For headers 5VSM and 6VSM
            public int headerSize; //Size of header
            public uint textureSize; //Size of texture

            public bool isPVR; //For iOS, Android devices with PowerVR chips and PS Vita
            public int SomeValue; //Some value for flags
            public UnknownFlags unknownFlags;
            public Platform platform;
            public string ObjectName;
            public string SubObjectName;
            public int Zero; //Saw in Wolf among Us or Game of Thrones
            public float OneValue;
            public byte OneByte;
            public FlagsClass.SomeClassData UnknownData; //For Walking Dead the New Frontier
            public bool HasOneValueTex; //For newest files
            public int Mip;
            public int Width;
            public int Height;
            public int Faces; //For cubmaps
            public int ArrayMembers; //Saw in PVRTexTool
            public uint TextureFormat;
            public int Unknown1;
            public byte[] block; //temporary solution
            public SubBlock subBlock;
            public SubBlock subBlock2; //For versions 9
            public TextureInfo Tex;

            public NewT3Texture() { }

            public NewT3Texture(NewT3Texture newClass)
            {
                headerSize = newClass.headerSize;
                textureSize = newClass.textureSize;

                isPVR = newClass.isPVR;
                SomeValue = newClass.SomeValue;
                unknownFlags = newClass.unknownFlags;
                platform = newClass.platform;
                ObjectName = newClass.ObjectName;
                SubObjectName = newClass.SubObjectName;
                Zero = newClass.Zero;
                OneValue = newClass.OneValue;
                OneByte = newClass.OneByte;
                UnknownData = newClass.UnknownData;
                HasOneValueTex = newClass.HasOneValueTex;
                Mip = newClass.Mip;
                Width = newClass.Width;
                Height = newClass.Height;
                Faces = newClass.Faces;
                ArrayMembers = newClass.ArrayMembers;
                TextureFormat = newClass.TextureFormat;
                Unknown1 = newClass.Unknown1;
                block = newClass.block;
                subBlock = newClass.subBlock;
                subBlock2 = newClass.subBlock2;
                Tex = newClass.Tex;
            }
        }
    }
}
