using System;
using System.Text;
using System.IO;
using TTG_Tools.Graphics.DDS;
using TTG_Tools.Graphics.PVR;
using TTG_Tools.Graphics.Swizzles;
using System.Linq;

namespace TTG_Tools.Graphics
{
    public class TextureWorker
    {
        static uint[] TexCodes = { 0x00, 0x4, 0x10, 0x11, 0x25, 0x40, 0x41, 0x42, 0x43, 0x43, 0x44, 0x45, 0x46 };
        static string[] FourCC = { "\0\0\0\0", "\0\0\0\0", "\0\0\0\0", "\0\0\0\0", "\x74\0\0\0", "DXT1", "DXT3", "DXT5", "BC4U", "ATI1", "ATI2", "BC6H", "ATI2" };
        static string[] Formats = { "uncompressed 8.8.8.8 ARGB", "uncompressed 4.4.4.4 ARGB", "Alpha 8 bit (A8)", "IL8", "uncompressed 32f.32f.32f.32f ARGB", "DXT1", "DXT3", "DXT5", "BC4", "BC4", "BC5", "BC6", "BC7" };

        private static bool IsVitaPvrFormat(uint textureFormat)
        {
            return textureFormat == 0x51 || textureFormat == 0x52 || textureFormat == 0x53 || textureFormat == 0x70;
        }

        private static void GetVitaSwizzleInfo(uint textureFormat, int width, int height, out int swizzleWidth, out int swizzleHeight, out int bytesPerPixelSet, out int formatBitsPerPixel)
        {
            bool blockCompressed = textureFormat >= 0x40 && textureFormat <= 0x46;

            swizzleWidth = blockCompressed ? Math.Max(1, (width + 3) / 4) : width;
            swizzleHeight = blockCompressed ? Math.Max(1, (height + 3) / 4) : height;

            switch (textureFormat)
            {
                case 0x04:
                    bytesPerPixelSet = 2;
                    break;
                case 0x10:
                case 0x11:
                    bytesPerPixelSet = 1;
                    break;
                case 0x40:
                case 0x43:
                    bytesPerPixelSet = 8;
                    break;
                case 0x41:
                case 0x42:
                case 0x44:
                case 0x45:
                case 0x46:
                    bytesPerPixelSet = 16;
                    break;
                default:
                    bytesPerPixelSet = 4;
                    break;
            }

            formatBitsPerPixel = bytesPerPixelSet * 8;
        }

        private static int GetPvrDataOffset(byte[] pvrContent)
        {
            const int pvrHeaderSize = 0x34;
            const int metaSizeOffset = 0x30;

            if (pvrContent == null || pvrContent.Length < pvrHeaderSize)
            {
                return 0;
            }

            int metaSize = BitConverter.ToInt32(pvrContent, metaSizeOffset);

            if (metaSize < 0)
            {
                metaSize = 0;
            }

            int dataOffset = pvrHeaderSize + metaSize;

            if (dataOffset > pvrContent.Length)
            {
                return pvrHeaderSize;
            }

            return dataOffset;
        }

        public static int ReadDDSHeader(Stream stream, ref int width, ref int height, ref int mip, ref uint textureFormat, bool newFormat)
        {
            BinaryReader br = new BinaryReader(stream);
            try
            {
                dds.header head;
                byte[] tmp = br.ReadBytes(4);
                head.head = Encoding.ASCII.GetString(tmp);
                head.Size = br.ReadUInt32();
                head.Flags = br.ReadUInt32();
                head.Height = br.ReadUInt32();
                head.Width = br.ReadUInt32();
                head.PitchOrLinearSize = br.ReadUInt32();
                head.Depth = br.ReadUInt32();
                head.MipMapCount = br.ReadUInt32();
                head.Reserved1 = new uint[11];

                for (int i = 0; i < 11; i++)
                {
                    head.Reserved1[i] = br.ReadUInt32();
                }

                head.PF.Size = br.ReadUInt32();
                head.PF.Flags = br.ReadUInt32();
                tmp = br.ReadBytes(4);
                head.PF.FourCC = Encoding.ASCII.GetString(tmp);
                head.PF.RgbBitCount = br.ReadUInt32();
                head.PF.RBitMask = br.ReadUInt32();
                head.PF.GBitMask = br.ReadUInt32();
                head.PF.BBitMask = br.ReadUInt32();
                head.PF.ABitMask = br.ReadUInt32();

                head.Caps = br.ReadUInt32();
                head.Caps2 = br.ReadUInt32();
                head.Caps3 = br.ReadUInt32();
                head.Caps4 = br.ReadUInt32();
                head.Reserved2 = br.ReadUInt32();

                width = (int)head.Width;
                height = (int)head.Height;
                mip = head.MipMapCount < 1 ? 1 : (int)head.MipMapCount;

                Flags flags = new Flags();
                bool isDX10 = false;

                if (newFormat)
                {
                    if ((ushort)(head.PF.Flags & flags.DDPF_RGB) == flags.DDPF_RGB)
                    {
                        switch (head.PF.RgbBitCount) {
                            case 16:
                                textureFormat = (uint)ClassesStructs.TextureClass.NewTextureFormat.ARGB4;
                                break;

                            case 32:
                                textureFormat = (uint)ClassesStructs.TextureClass.NewTextureFormat.ARGB8;
                                break;
                    }
                    }
                    else if((ushort)(head.PF.Flags & flags.DDPF_LUMINANCE) == flags.DDPF_LUMINANCE)
                    {
                        if (head.PF.RgbBitCount == 16) textureFormat = (uint)ClassesStructs.TextureClass.NewTextureFormat.IL8;
                    }
                    else if (head.PF.Flags == 2 && head.PF.RgbBitCount == 8)
                    {
                        textureFormat = (uint)ClassesStructs.TextureClass.NewTextureFormat.A8; 
                    }
                    else
                    {
                        switch (head.PF.FourCC)
                        {
                            case "DXT1":
                                textureFormat = (uint)ClassesStructs.TextureClass.NewTextureFormat.BC1;
                                break;

                            case "DXT3":
                                textureFormat = (uint)ClassesStructs.TextureClass.NewTextureFormat.BC2;
                                break;

                            case "DXT5":
                                textureFormat = (uint)ClassesStructs.TextureClass.NewTextureFormat.BC3;
                                break;

                            case "ATI1":
                                textureFormat = (uint)ClassesStructs.TextureClass.NewTextureFormat.BC4;
                                break;

                            case "ATI2":
                            case "BC5U":
                                textureFormat = (uint)ClassesStructs.TextureClass.NewTextureFormat.BC5;
                                break;

                            case "DX10":
                                isDX10 = true;
                                break;

                            default:
                                throw new Exception("Unknown format");
                        }

                        if (isDX10)
                        {
                            int Format = br.ReadInt32();

                            switch (Format)
                            {
                                case (int)dds.DxgiFormat.DXGI_FORMAT_R8G8B8A8_UNORM:
                                    textureFormat = (uint)ClassesStructs.TextureClass.NewTextureFormat.ARGB8;
                                    break;

                                case (int)dds.DxgiFormat.DXGI_FORMAT_B4G4R4A4_UNORM:
                                    textureFormat = (uint)ClassesStructs.TextureClass.NewTextureFormat.ARGB4;
                                    break;

                                case (int)dds.DxgiFormat.DXGI_FORMAT_A8_UNORM:
                                    textureFormat = (uint)ClassesStructs.TextureClass.NewTextureFormat.A8;
                                    break;

                                case (int)dds.DxgiFormat.DXGI_FORMAT_BC1_UNORM:
                                case (int)dds.DxgiFormat.DXGI_FORMAT_BC1_TYPELESS:
                                    textureFormat = (uint)ClassesStructs.TextureClass.NewTextureFormat.BC1;
                                    break;

                                case (int)dds.DxgiFormat.DXGI_FORMAT_BC2_UNORM:
                                case (int)dds.DxgiFormat.DXGI_FORMAT_BC2_TYPELESS:
                                    textureFormat = (uint)ClassesStructs.TextureClass.NewTextureFormat.BC2;
                                    break;

                                case (int)dds.DxgiFormat.DXGI_FORMAT_BC3_UNORM:
                                case (int)dds.DxgiFormat.DXGI_FORMAT_BC3_TYPELESS:
                                    textureFormat = (uint)ClassesStructs.TextureClass.NewTextureFormat.BC3;
                                    break;

                                case (int)dds.DxgiFormat.DXGI_FORMAT_BC4_UNORM:
                                case (int)dds.DxgiFormat.DXGI_FORMAT_BC4_SNORM:
                                case (int)dds.DxgiFormat.DXGI_FORMAT_BC4_TYPELESS:
                                    textureFormat = (uint)ClassesStructs.TextureClass.NewTextureFormat.BC4;
                                    break;

                                case (int)dds.DxgiFormat.DXGI_FORMAT_BC5_UNORM:
                                case (int)dds.DxgiFormat.DXGI_FORMAT_BC5_SNORM:
                                case (int)dds.DxgiFormat.DXGI_FORMAT_BC5_TYPELESS:
                                    textureFormat = (uint)ClassesStructs.TextureClass.NewTextureFormat.BC5;
                                    break;

                                case (int)dds.DxgiFormat.DXGI_FORMAT_BC6H_TYPELESS:
                                case (int)dds.DxgiFormat.DXGI_FORMAT_BC6H_SF16:
                                case (int)dds.DxgiFormat.DXGI_FORMAT_BC6H_UF16:
                                    textureFormat = (uint)ClassesStructs.TextureClass.NewTextureFormat.BC6;
                                    break;

                                case (int)dds.DxgiFormat.DXGI_FORMAT_BC7_UNORM:
                                case (int)dds.DxgiFormat.DXGI_FORMAT_BC7_TYPELESS:
                                    textureFormat = (uint)ClassesStructs.TextureClass.NewTextureFormat.BC7;
                                    break;
                            }

                            Format = br.ReadInt32(); //Read DX10 ResourceDimension
                            head.headDX11.MiscFlag = br.ReadUInt32();
                            head.headDX11.ArraySize = br.ReadUInt32();
                            head.headDX11.MiscFlag2 = br.ReadUInt32();
                        }
                    }
                }
                else
                {
                    if ((uint)(head.PF.Flags & flags.DDPF_RGB) == flags.DDPF_RGB)
                    {
                        switch (head.PF.RgbBitCount) {
                            case 32:
                                textureFormat = (uint)ClassesStructs.TextureClass.OldTextureFormat.DX_ARGB8888;
                                break;
                    }
                    }
                    else if (head.PF.Flags == 2 && head.PF.RgbBitCount == 8)
                    {
                        textureFormat = (uint)ClassesStructs.TextureClass.OldTextureFormat.DX_L8;
                    }
                    else if (((uint)(head.PF.Flags & flags.DDPF_LUMINANCE) == flags.DDPF_LUMINANCE) || ((uint)(head.PF.Flags & flags.DDPF_ALPHA) == flags.DDPF_ALPHA))
                    {
                        textureFormat = (uint)ClassesStructs.TextureClass.OldTextureFormat.DX_L8;
                    }
                    else if((uint)(head.PF.Flags & flags.DDPF_FOURCC) == flags.DDPF_FOURCC)
                    {
                        switch (head.PF.FourCC)
                        {
                            case "DXT1":
                                textureFormat = (uint)ClassesStructs.TextureClass.OldTextureFormat.DX_DXT1;
                                break;

                            case "DXT3":
                                textureFormat = (uint)ClassesStructs.TextureClass.OldTextureFormat.DX_DXT3;
                                break;

                            case "DXT5":
                                textureFormat = (uint)ClassesStructs.TextureClass.OldTextureFormat.DX_DXT5;
                                break;

                            default:
                                throw new Exception("Unknown format");
                        }
                    }
                }

                return 0;
            }
            catch
            {
                if (br != null) br.Close();
                return -1;
            }
        }

        public static int ReadPvrHeader(Stream stream, ref int width, ref int height, ref int mip, ref uint TexFormat, bool NewFormat)
        {
            BinaryReader br = new BinaryReader(stream);
            pvr.header head;
            pvr.metaHeader metaHead;

            head.Version = br.ReadUInt32();
            head.Flags = br.ReadUInt32();
            head.PixelFormat = br.ReadUInt64();
            head.ColorSpace = br.ReadUInt32();
            head.ChannelType = br.ReadUInt32();
            head.Height = br.ReadUInt32();
            head.Width = br.ReadUInt32();
            head.Depth = br.ReadUInt32();
            head.Surface = br.ReadUInt32();
            head.Face = br.ReadUInt32();
            head.Mip = br.ReadUInt32();
            head.MetaSize = br.ReadUInt32();

            if(head.MetaSize > 0)
            {
                metaHead.FourCC = br.ReadUInt32();
                metaHead.Key = br.ReadUInt32();
                metaHead.DataSize = br.ReadUInt32();
                metaHead.Data = br.ReadBytes((int)metaHead.DataSize);
                metaHead.padding = br.ReadBytes((int)head.MetaSize - 12 - (int)metaHead.DataSize);
            }

            TexFormat = 0;

            if (NewFormat) {
                switch (head.PixelFormat)
                {
                    case (ulong)pvr.HeaderFormat.BC1:
                        TexFormat = 0x40;
                        break;

                    case (ulong)pvr.HeaderFormat.DXT2:
                        TexFormat = 0x41;
                        break;

                    case (ulong)pvr.HeaderFormat.DXT3:
                        TexFormat = 0x41;
                        break;

                    case (ulong)pvr.HeaderFormat.DXT5:
                        TexFormat = 0x42;
                        break;

                    case (ulong)pvr.HeaderFormat.BC4:
                        TexFormat = 0x43;
                        break;

                    case (ulong)pvr.HeaderFormat.BC5:
                        TexFormat = 0x44;
                        break;

                    case (ulong)pvr.HeaderFormat.BC6:
                        TexFormat = 0x45;
                        break;

                    case (ulong)pvr.HeaderFormat.BC7:
                        TexFormat = 0x46;
                        break;
                }
            }
            else
            {
                switch (head.PixelFormat)
                {
                    case (ulong)pvr.HeaderFormat.PVRTC4bppRGB:
                        TexFormat = (uint)ClassesStructs.TextureClass.OldTexturePVRFormat.PVRTC4bppRGB_1;
                        break;

                    case (ulong)pvr.HeaderFormat.PVRTC4bppRGBA:
                        TexFormat = (uint)ClassesStructs.TextureClass.OldTexturePVRFormat.PVRTC4bppRGBA_1;
                        break;

                    /*case (ulong)pvr.HeaderFormat.PVRTCII2Bpp:
                        TexFormat = (uint)ClassesStructs.TextureClass.OldTexturePVRFormat.;
                        break;*/

                    /*case (ulong)pvr.HeaderFormat.PVRTCII4Bpp:

                        break;*/

                    case 0x808080861626772: //rgba8888
                        TexFormat = (uint)ClassesStructs.TextureClass.OldTexturePVRFormat.PVRTC_RGBA8;
                        break;

                    case 0x404040461626772: //rgba4444
                        TexFormat = (uint)ClassesStructs.TextureClass.OldTexturePVRFormat.PVRTC_RGBA4;
                        break;

                    case 0x105050561626772: //rgba5551
                        TexFormat = (uint)ClassesStructs.TextureClass.OldTexturePVRFormat.PVRTC_RGBA1555;
                        break;

                    case 0x5060500626772:
                        TexFormat = (uint)ClassesStructs.TextureClass.OldTexturePVRFormat.PVRTC_RGB565_1;
                        break;
                }
            }

            br.Dispose();
            return 0;
        }

        private static byte[] GenHeader(uint Format, int Width, int Height, uint Size, int Faces, int ArrayMember, int MipCount, ref string StrFormat)
        {
            if (FourCC.Length != TexCodes.Length) throw new Exception("Length array FourCC is not equal length array TexCodes");
            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);
            try
            {
                dds.header head;
                head.head = "DDS ";
                byte[] tmp = Encoding.ASCII.GetBytes(head.head);
                bw.Write(tmp);

                head.Size = 124;
                bw.Write(head.Size);

                head.Flags = 0;
                Flags flags = new Flags();
                head.Flags = flags.DDSD_WIDTH | flags.DDSD_HEIGHT | flags.DDSD_PIXELFORMAT | flags.DDSD_CAPS;
                head.Flags |= Format < 0x40 ? flags.DDSD_PITCH : flags.DDSD_LINEARSIZE;

                if (MipCount > 1) head.Flags |= flags.DDSD_MIPMAPCOUNT;

                bw.Write(head.Flags);

                head.Height = (uint)Height;
                bw.Write(head.Height);

                head.Width = (uint)Width;
                bw.Write(head.Width);

                head.PitchOrLinearSize = Size;
                if (Format < 0x40)
                {
                    int Pitch = 0;
                    int sz = 0;
                    Methods.getSizeAndKratnost((int)head.Width, (int)head.Height, (int)Format, ref sz, ref Pitch);
                    head.PitchOrLinearSize = (uint)Pitch;
                }
                bw.Write(head.PitchOrLinearSize);

                head.Depth = 0;
                bw.Write(head.Depth);

                head.MipMapCount = (uint)MipCount;
                bw.Write(head.MipMapCount);

                head.Reserved1 = new uint[11];

                for(int i = 0; i < 11; i++)
                {
                    bw.Write(head.Reserved1[i]);
                }

                head.PF.Size = 32;
                bw.Write(head.PF.Size);

                head.PF.Flags = (Format >= 0x40 || Format == 0x25) ? flags.DDPF_FOURCC : flags.DDPF_RGB;
                
                switch (Format)
                {
                    case 0x00:
                        head.PF.Flags = flags.DDPF_RGB | flags.DDPF_ALPHAPIXELS;
                        break;

                    case 0x10:
                        head.PF.Flags = flags.DDPF_ALPHA;
                        break;

                    case 0x11:
                        head.PF.Flags = flags.DDPF_LUMINANCE;
                        break;
                }

                bw.Write(head.PF.Flags);

                head.PF.FourCC = "\0\0\0\0";

                if (Faces <= 1 && ArrayMember <= 1)
                {
                    for (int i = 0; i < TexCodes.Length; i++)
                    {
                        if (TexCodes[i] == Format)
                        {
                            head.PF.FourCC = FourCC[i];
                            StrFormat = Formats[i];
                            break;
                        }
                    }
                }
                else
                {
                    head.PF.FourCC = "DX10";
                }

                if (Format > 0x42)
                {
                    head.PF.FourCC = "DX10";
                }

                tmp = Encoding.ASCII.GetBytes(head.PF.FourCC);
                bw.Write(tmp);

                head.PF.RgbBitCount = 0;
                if(Format < 0x40)
                {
                    switch (Format)
                    {
                        case 0:
                            head.PF.RgbBitCount = 32;
                            break;
                        case 4:
                            head.PF.RgbBitCount = 16;
                            break;

                        case 0x10:
                        case 0x11:
                            head.PF.RgbBitCount = 8;
                            break;

                        default:
                            head.PF.RgbBitCount = 0;
                            break;
                    }
                }

                bw.Write(head.PF.RgbBitCount);
                head.PF.RBitMask = 0;
                head.PF.GBitMask = 0;
                head.PF.BBitMask = 0;
                head.PF.ABitMask = 0;

                if (Format < 0x40)
                {
                    switch (Format)
                    {
                        case (int)ClassesStructs.TextureClass.NewTextureFormat.ARGB8:
                            head.PF.RBitMask = 0xFF0000;
                            head.PF.GBitMask = 0xFF00;
                            head.PF.BBitMask = 0xFF;
                            head.PF.ABitMask = 0xFF000000;
                            break;

                        case (int)ClassesStructs.TextureClass.NewTextureFormat.ARGB4:
                            head.PF.RBitMask = 0xF00;
                            head.PF.GBitMask = 0xF0;
                            head.PF.BBitMask = 0xF;
                            head.PF.ABitMask = 0xF000;
                            break;

                        case (int)ClassesStructs.TextureClass.NewTextureFormat.IL8:
                            head.PF.RBitMask = 0xFF;
                            head.PF.GBitMask = 0xFF;
                            head.PF.BBitMask = 0xFF;
                            head.PF.ABitMask = 0;
                            break;

                        case (int)ClassesStructs.TextureClass.NewTextureFormat.A8:
                            head.PF.RBitMask = 0;
                            head.PF.GBitMask = 0;
                            head.PF.BBitMask = 0;
                            head.PF.ABitMask = 0xFF;
                            break;
                    }
                }

                bw.Write(head.PF.RBitMask);
                bw.Write(head.PF.GBitMask);
                bw.Write(head.PF.BBitMask);
                bw.Write(head.PF.ABitMask);

                Caps caps = new Caps();

                head.Caps = caps.DDSCAPS_TEXTURE;
                head.Caps2 = 0;
                head.Caps3 = 0;
                head.Caps4 = 0;
                head.Reserved2 = 0;

                if (MipCount > 1) head.Caps |= caps.DDSCAPS_COMPLEX | caps.DDSCAPS_MIPMAP;

                //Need find out how caps2 value works
                head.Caps2 = caps.DDSCAPS2_CUBEMAP_POSITIVEY;

                bw.Write(head.Caps);
                bw.Write(head.Caps2);
                bw.Write(head.Caps3);
                bw.Write(head.Caps4);
                bw.Write(head.Reserved2);

                if(head.PF.FourCC == "DX10")
                {
                    head.headDX11.DF = dds.DxgiFormat.DXGI_FORMAT_UNKNOWN;

                    switch (Format)
                    {
                        case (uint)ClassesStructs.TextureClass.NewTextureFormat.ARGB8:
                            head.headDX11.DF = dds.DxgiFormat.DXGI_FORMAT_R8G8B8A8_UNORM;
                            break;

                        case (uint)ClassesStructs.TextureClass.NewTextureFormat.ARGB4:
                            head.headDX11.DF = dds.DxgiFormat.DXGI_FORMAT_B4G4R4A4_UNORM;
                            break;

                        case (uint)ClassesStructs.TextureClass.NewTextureFormat.A8:
                            head.headDX11.DF = dds.DxgiFormat.DXGI_FORMAT_A8_UNORM;
                            break;

                        case (uint)ClassesStructs.TextureClass.NewTextureFormat.BC1:
                            head.headDX11.DF = dds.DxgiFormat.DXGI_FORMAT_BC1_UNORM;
                            break;

                        case (uint)ClassesStructs.TextureClass.NewTextureFormat.BC2:
                            head.headDX11.DF = dds.DxgiFormat.DXGI_FORMAT_BC2_UNORM;
                            break;

                        case (uint)ClassesStructs.TextureClass.NewTextureFormat.BC3:
                            head.headDX11.DF = dds.DxgiFormat.DXGI_FORMAT_BC3_UNORM;
                            break;

                        case (uint)ClassesStructs.TextureClass.NewTextureFormat.BC4:
                            head.headDX11.DF = dds.DxgiFormat.DXGI_FORMAT_BC4_UNORM;
                            break;

                        case (uint)ClassesStructs.TextureClass.NewTextureFormat.BC5:
                            head.headDX11.DF = dds.DxgiFormat.DXGI_FORMAT_BC5_UNORM;
                            break;

                        case (uint)ClassesStructs.TextureClass.NewTextureFormat.BC6:
                            head.headDX11.DF = dds.DxgiFormat.DXGI_FORMAT_BC6H_TYPELESS;
                            break;

                        case (uint)ClassesStructs.TextureClass.NewTextureFormat.BC7:
                            head.headDX11.DF = dds.DxgiFormat.DXGI_FORMAT_BC7_UNORM;
                            break;
                    }

                    head.headDX11.ResourceDimension = dds.D3D10ResourceDimension.D3D10_RESOURCE_DIMENSION_TEXTURE2D;
                    head.headDX11.MiscFlag = 0;
                    head.headDX11.MiscFlag2 = 0;
                    head.headDX11.ArraySize = (uint)ArrayMember;

                    bw.Write((uint)head.headDX11.DF);
                    bw.Write((uint)head.headDX11.ResourceDimension);
                    bw.Write(head.headDX11.MiscFlag);
                    bw.Write(head.headDX11.ArraySize);
                    bw.Write(head.headDX11.MiscFlag2);
                }
                //NEED THINK ABOUT DX10 FORMAT!

                byte[] header = ms.ToArray();
                bw.Close();
                ms.Close();
                return header;
            }
            catch
            {
                if (bw != null) bw.Close();
                if (ms != null) ms.Close();
                return null;
            }
        }

        private static byte[] GenPvrHeader(int width, int height, int mip, uint format, uint ArrayMembers, uint Faces, bool NewFormat)
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);
            try
            {
                pvr.header head;
                head.Version = BitConverter.ToUInt32(Encoding.ASCII.GetBytes("PVR\x03"), 0);
                bw.Write(head.Version);

                head.Flags = 0;
                bw.Write(head.Flags);

                head.ChannelType = 0; //Unsigned byte normalised
                head.PixelFormat = 0;

                if (!NewFormat)
                {
                    switch (format)
                    {
                        case (uint)ClassesStructs.TextureClass.OldTexturePVRFormat.PVRTC4bppRGBA_1:
                        case (uint)ClassesStructs.TextureClass.OldTexturePVRFormat.PVRTC4bppRGBA_2:
                        case (uint)ClassesStructs.TextureClass.OldTexturePVRFormat.PVRTC4bppRGBA_3:
                        case (uint)ClassesStructs.TextureClass.OldTexturePVRFormat.PVRTC4bppRGBA_4:
                        case (uint)ClassesStructs.TextureClass.OldTexturePVRFormat.PVRTC4bppRGBA_5:
                            head.PixelFormat = (ulong)pvr.HeaderFormat.PVRTC4bppRGBA;
                            break;

                        case (uint)ClassesStructs.TextureClass.OldTexturePVRFormat.PVRTC4bppRGB_1:
                        case (uint)ClassesStructs.TextureClass.OldTexturePVRFormat.PVRTC4bppRGB_2:
                            head.PixelFormat = (ulong)pvr.HeaderFormat.PVRTC4bppRGB;
                            break;

                        case (uint)ClassesStructs.TextureClass.OldTexturePVRFormat.PVRTC_RGB565_1:
                        case (uint)ClassesStructs.TextureClass.OldTexturePVRFormat.PVRTC_RGB565_2:
                            head.PixelFormat = BitConverter.ToUInt64(Encoding.ASCII.GetBytes("rgb\x0\x5\x6\x5\x0"), 0);
                            head.ChannelType = 4; //Unsigned short normalised
                            break;

                        case (uint)ClassesStructs.TextureClass.OldTexturePVRFormat.PVRTC_RGBA1555:
                            head.PixelFormat = BitConverter.ToUInt64(Encoding.ASCII.GetBytes("rgba\x5\x5\x5\x1"), 0);
                            head.ChannelType = 4; //Unsigned short normalised
                            break;

                        case (uint)ClassesStructs.TextureClass.OldTexturePVRFormat.PVRTC_RGBA4:
                            head.PixelFormat = BitConverter.ToUInt64(Encoding.ASCII.GetBytes("rgba\x4\x4\x4\x4"), 0);
                            head.ChannelType = 4; //Unsigned short normalised
                            break;

                        case (uint)ClassesStructs.TextureClass.OldTexturePVRFormat.PVRTC_RGBA8:
                            head.PixelFormat = BitConverter.ToUInt64(Encoding.ASCII.GetBytes("rgba\x8\x8\x8\x8"), 0);
                            break;

                        default:
                            return null;
                    }
                }
                else
                {
                    switch (format)
                    {
                        case 0x00:
                            head.PixelFormat = BitConverter.ToUInt64(Encoding.ASCII.GetBytes("rgba\x8\x8\x8\x8"), 0);
                            break;

                        case 0x04:
                            head.PixelFormat = BitConverter.ToUInt64(Encoding.ASCII.GetBytes("rgba\x4\x4\x4\x4"), 0);
                            head.ChannelType = 4; //Unsigned short normalised
                            break;

                        case 0x52:
                        case 0x53:
                            head.PixelFormat = (ulong)pvr.HeaderFormat.PVRTC4bppRGBA;
                            break;

                        case 0x51:
                            head.PixelFormat = (ulong)pvr.HeaderFormat.PVRTC4bppRGB;
                            break;

                        case 0x70:
                            head.PixelFormat = (ulong)pvr.HeaderFormat.ETC1;
                            break;

                        case 0x40:
                            head.PixelFormat = (ulong)pvr.HeaderFormat.DXT1;
                            break;

                        case 0x41:
                            head.PixelFormat = (ulong)pvr.HeaderFormat.DXT3;
                            break;

                        case 0x42:
                            head.PixelFormat = (ulong)pvr.HeaderFormat.DXT5;
                            break;

                        case 0x43:
                            head.PixelFormat = (ulong)pvr.HeaderFormat.BC4;
                            break;

                        case 0x44:
                            head.PixelFormat = (ulong)pvr.HeaderFormat.BC5;
                            break;

                        case 0x45:
                            head.PixelFormat = (ulong)pvr.HeaderFormat.BC6;
                            break;

                        case 0x46:
                            head.PixelFormat = (ulong)pvr.HeaderFormat.BC7;
                            break;

                        default:
                            return null;
                    }
                }

                bw.Write(head.PixelFormat);

                head.ColorSpace = 0; //Linear RGB
                bw.Write(head.ColorSpace);
                bw.Write(head.ChannelType);

                head.Height = (uint)height;
                bw.Write(head.Height);

                head.Width = (uint)width;
                bw.Write(head.Width);

                head.Depth = 1; //Set default 1
                bw.Write(head.Depth);

                head.Surface = ArrayMembers > 1 ? ArrayMembers : 1; //Default 1
                bw.Write(head.Surface);

                head.Face = Faces > 1 ? Faces : 1; //Default 1
                bw.Write(head.Face);

                head.Mip = mip == 0 ? 1 : (uint)mip;
                bw.Write(head.Mip);

                head.MetaSize = 0; //Default 0 meta size
                bw.Write(head.MetaSize);


                byte[] header = ms.ToArray();

                bw.Close();
                ms.Close();

                return header;
            }
            catch
            {
                if (bw != null) bw.Close();
                if (ms != null) ms.Close();

                return null;
            }
        }

        private static void ReadNewD3dtxHeader(ref ClassesStructs.TextureClass.NewT3Texture tex, int i, ref int poz, byte[] binContent, string checkHeader)
        {
            byte[] tmp = null;

            if (tex.SomeValue >= 5)
            {
                tmp = new byte[4];
                Array.Copy(binContent, poz, tmp, 0, tmp.Length);
                tex.Tex.Textures[i].SubTexNum = BitConverter.ToInt32(tmp, 0);
                poz += 4;
            }

            tmp = new byte[4];
            Array.Copy(binContent, poz, tmp, 0, tmp.Length);
            tex.Tex.Textures[i].CurrentMip = BitConverter.ToInt32(tmp, 0);
            poz += 4;


            tmp = new byte[4];
            Array.Copy(binContent, poz, tmp, 0, tmp.Length);
            if (BitConverter.ToInt32(tmp, 0) == 1 || tex.platform.platform == 4)
            {
                tex.Tex.Textures[i].One = BitConverter.ToInt32(tmp, 0);
                poz += 4;

                tex.HasOneValueTex = true;

                tmp = new byte[4];
                Array.Copy(binContent, poz, tmp, 0, tmp.Length);
            }

            tex.Tex.Textures[i].MipSize = BitConverter.ToInt32(tmp, 0);
            poz += 4;

            tmp = new byte[4];
            Array.Copy(binContent, poz, tmp, 0, tmp.Length);
            tex.Tex.Textures[i].BlockSize = BitConverter.ToInt32(tmp, 0);
            poz += 4;

            //if (checkHeader == "6VSM" && tex.SomeValue == 9)
            if (((checkHeader == "6VSM") || (checkHeader == "5VSM")) && tex.SomeValue >= 8)
            {
                poz += 4; //Skip Mip size
            }
        }

        public static string DoWork(string InputFile, string OutputDir, bool extract, bool FullEncrypt, ref byte[] EncKey, int version)
        {
            string result = null;
            string additionalMessage = null;
            bool NewFormat = false;

            FileInfo fi = new FileInfo(InputFile);

            byte[] binContent = File.ReadAllBytes(InputFile);
            byte[] check_header = new byte[4];
            Array.Copy(binContent, 0, check_header, 0, check_header.Length);
            int poz = 4;
            int headerPos = 0;

            if ((Encoding.ASCII.GetString(check_header) != "5VSM") && (Encoding.ASCII.GetString(check_header) != "ERTM")
            && (Encoding.ASCII.GetString(check_header) != "6VSM")) //Supposed this texture encrypted
            {
                //First trying decrypt probably encrypted font
                try
                {
                    string info = Methods.FindingDecrytKey(binContent, "texture", ref EncKey, ref version);

                    if (info != null)
                    {
                        additionalMessage = "D3dtx file was encrypted, but I decrypted. " + info;
                    }
                }
                catch
                {
                    result = "Maybe that D3DTX file encrypted. Try to decrypt first: " + fi.Name;

                    return result;
                }
            }
            if ((Encoding.ASCII.GetString(check_header) == "5VSM") || (Encoding.ASCII.GetString(check_header) == "6VSM"))
            {
                poz = 16;
                NewFormat = true;
            }

            try
            {
                byte[] tmp = new byte[4];
                Array.Copy(binContent, poz, tmp, 0, tmp.Length);
                int countElements = BitConverter.ToInt32(tmp, 0);
                poz += 4;

                tmp = new byte[8];
                Array.Copy(binContent, poz, tmp, 0, tmp.Length);

                bool flags = false;
                bool someData = false;
                bool PossibleNewFlags = false;
                bool AddInfo = false; //For 01 00 00 00 and 01 02 03 values

                if (BitConverter.ToString(tmp) == "E2-CC-38-6F-7E-9E-24-3E")
                {
                    byte[][] elements = new byte[countElements][];

                    for(int i = 0; i < countElements; i++)
                    {
                        elements[i] = new byte[8];
                        Array.Copy(binContent, poz, elements[i], 0, elements[i].Length);
                        poz += 12;

                        switch (BitConverter.ToString(elements[i]))
                        {
                            case "41-16-D7-79-B9-3C-28-84":
                                flags = true;
                                break;

                            case "E3-88-09-7A-48-5D-7F-93":
                                someData = true;
                                break;

                            case "0F-F4-20-E6-20-BA-A1-EF":
                                NewFormat = true;
                                break;

                            case "D2-15-9F-6B-4F-DC-75-CD":
                                PossibleNewFlags = true;
                                break;

                            case "7A-BA-6E-87-89-88-6C-FA":
                                AddInfo = true;
                                break;
                        }
                    }
                }
                else
                {
                    string[] elements = new string[countElements];
                    int lenStr;

                    for (int i = 0; i < countElements; i++)
                    {
                        tmp = new byte[4];
                        Array.Copy(binContent, poz, tmp, 0, tmp.Length);
                        poz += 4;
                        lenStr = BitConverter.ToInt32(tmp, 0);
                        tmp = new byte[lenStr];
                        Array.Copy(binContent, poz, tmp, 0, tmp.Length);
                        poz += lenStr + 4; //Length element's name and 4 bytes data for Telltale Tool
                        elements[i] = Encoding.ASCII.GetString(tmp);

                        if (elements[i] == "class Flags")
                        {
                            flags = true;
                        }
                    }
                }

                headerPos = poz;

                if (!NewFormat)
                {
                    ClassesStructs.TextureClass.OldT3Texture oldTex = GetOldTextures(binContent, ref poz, flags, someData);

                    if (oldTex == null)
                    {
                        result = "Something wrong with this file: " + fi.Name;
                        return result;
                    }

                    if (extract)
                    {
                        try
                        {
                            result = "File " + fi.Name + " successfully extracted. Texture format " + Convert.ToString(oldTex.TextureFormat) + " (" + Encoding.ASCII.GetString(BitConverter.GetBytes(oldTex.TextureFormat)) + "). ";

                            string format = oldTex.isIOS ? ".pvr" : ".dds";

                            if (File.Exists(OutputDir + Path.DirectorySeparatorChar + fi.Name.Replace(".d3dtx", format))) File.Delete(OutputDir + Path.DirectorySeparatorChar + fi.Name.Replace(".d3dtx", format));
                            File.WriteAllBytes(OutputDir + Path.DirectorySeparatorChar + fi.Name.Replace(".d3dtx", format), oldTex.Content);

                            if (additionalMessage != null) result += additionalMessage;
                        }
                        catch
                        {
                            return "Something wrong with file " + fi.Name;
                        }
                    }
                    else
                    {
                        result = "File " + fi.Name + " successfully imported. ";
                        byte[] NewContent = oldTex.isIOS ? File.ReadAllBytes(fi.FullName.Remove(fi.FullName.Length - 5) + "pvr") : File.ReadAllBytes(fi.FullName.Remove(fi.FullName.Length - 5) + "dds");
                        oldTex.Content = new byte[NewContent.Length];
                        Array.Copy(NewContent, 0, oldTex.Content, 0, oldTex.Content.Length);

                        MemoryStream ms = new MemoryStream(NewContent);

                        if (oldTex.isIOS)
                        {
                            ReadPvrHeader(ms, ref oldTex.Width, ref oldTex.Height, ref oldTex.Mip, ref oldTex.mobTexFormat, false);
                            oldTex.TexSize = 0x34 + 1;
                            int headSize = 0x34;

                            tmp = new byte[4];
                            Array.Copy(NewContent, headSize - 4, tmp, 0, 4);
                            headSize += BitConverter.ToInt32(tmp, 0);
                            oldTex.TexSize += (NewContent.Length - headSize);

                            oldTex.Content = new byte[(NewContent.Length - headSize) + 1];
                            Array.Copy(NewContent, headSize, oldTex.Content, 0, oldTex.Content.Length - 1);
                        }
                        else ReadDDSHeader(ms, ref oldTex.Width, ref oldTex.Height, ref oldTex.Mip, ref oldTex.TextureFormat, false);
                        ms.Close();

                        oldTex.OriginalHeight = oldTex.Height;
                        oldTex.OriginalWidth = oldTex.Width;

                        if(!oldTex.isIOS) oldTex.TexSize = oldTex.Content.Length;

                        if (File.Exists(OutputDir + Path.DirectorySeparatorChar + fi.Name)) File.Delete(OutputDir + Path.DirectorySeparatorChar + fi.Name);

                        ms = new MemoryStream();
                        tmp = new byte[headerPos];
                        Array.Copy(binContent, 0, tmp, 0, tmp.Length);
                        ms.Write(tmp, 0, tmp.Length);

                        bool NeedEncrypt = false;

                        if (Encoding.ASCII.GetString(check_header) != "ERTM") NeedEncrypt = true;

                        ReplaceOldTextures(ms, oldTex, someData, NeedEncrypt, EncKey, version);
                        tmp = ms.ToArray();
                        ms.Close();

                        if (FullEncrypt) Methods.meta_crypt(tmp, EncKey, version, false);

                        FileStream fs = new FileStream(OutputDir + Path.DirectorySeparatorChar + fi.Name, FileMode.CreateNew);
                        fs.Write(tmp, 0, tmp.Length);
                        fs.Close();
                    }

                    return result;
                }

                    uint tmpPoz = 0;

                    ClassesStructs.TextureClass.NewT3Texture tex = GetNewTextures(binContent, ref poz, ref tmpPoz, flags, someData, false, ref additionalMessage, AddInfo);

                    if (tex == null)
                    {
                        result = "Something wrong with this file: " + fi.Name;
                        File.AppendAllText("test.txt", fi.Name + "\r\n");
                        return result;
                    }
                
                if (extract)
                {
                    result = "File " + fi.Name + " successfully extracted. ";

                    string format = tex.isPVR ? ".pvr" : ".dds";

                    if (File.Exists(OutputDir + "\\" + fi.Name.Replace(".d3dtx", format))) File.Delete(OutputDir + "\\" + fi.Name.Replace(".d3dtx", format));
                    File.WriteAllBytes(OutputDir + "\\" + fi.Name.Replace(".d3dtx", format), tex.Tex.Content);

                    if (additionalMessage != null) result += additionalMessage;

                    return result;
                }
                else
                {
                    result = "File " + fi.Name + " successfully imported.";

                    string textureBasePath = fi.FullName.Remove(fi.FullName.Length - 5);
                    bool importedFromPvr = false;
                    string importPath = textureBasePath + "dds";

                    if (tex.isPVR)
                    {
                        if (File.Exists(textureBasePath + "dds"))
                        {
                            importPath = textureBasePath + "dds";
                        }
                        else if (File.Exists(textureBasePath + "pvr"))
                        {
                            importPath = textureBasePath + "pvr";
                            importedFromPvr = true;
                        }
                        else
                        {
                            throw new FileNotFoundException("Could not find .dds or .pvr texture to import.", textureBasePath + "dds");
                        }
                    }
                    else if (!File.Exists(importPath))
                    {
                        throw new FileNotFoundException("Could not find .dds texture to import.", importPath);
                    }

                    byte[] NewContent = File.ReadAllBytes(importPath);
                    tex.Tex.Content = new byte[NewContent.Length];
                    Array.Copy(NewContent, 0, tex.Tex.Content, 0, tex.Tex.Content.Length);

                    MemoryStream ms = new MemoryStream(NewContent);

                    int pos;

                    if (importedFromPvr)
                    {
                        int pvrReadResult = ReadPvrHeader(ms, ref tex.Width, ref tex.Height, ref tex.Mip, ref tex.TextureFormat, true);

                        if (pvrReadResult != 0)
                        {
                            ms.Close();
                            return "Unsupported PVR format";
                        }

                        pos = GetPvrDataOffset(NewContent);
                    }
                    else
                    {
                        int ddsReadResult = ReadDDSHeader(ms, ref tex.Width, ref tex.Height, ref tex.Mip, ref tex.TextureFormat, true);

                        if (ddsReadResult != 0)
                        {
                            ms.Close();
                            return "Unsupported DDS format";
                        }

                        pos = (int)ms.Position;
                    }

                    int checkMip = Methods.CalculateMip(tex.Width, tex.Height, tex.TextureFormat);

                    if (checkMip > 1 && Encoding.ASCII.GetString(check_header) != "ERTM") checkMip -= 2;

                    tex.Mip = checkMip <= tex.Mip ? checkMip : tex.Mip;
                    tex.Tex.MipCount = tex.Mip;
                    tex.Tex.Textures = new ClassesStructs.TextureClass.NewT3Texture.TextureStruct[tex.Mip];

                    int w = tex.Width;
                    int h = tex.Height;

                    ms.Close();

                    tex.textureSize = 0;
                    tex.Tex.TexSize = 0;

                    for (int i = 0; i < tex.Mip; i++)
                    {
                        tex.Tex.Textures[i].CurrentMip = i;
                        Methods.getSizeAndKratnost(w, h, (int)tex.TextureFormat, ref tex.Tex.Textures[i].MipSize, ref tex.Tex.Textures[i].BlockSize);

                        tex.Tex.Textures[i].Block = new byte[tex.Tex.Textures[i].MipSize];

                        Array.Copy(NewContent, pos, tex.Tex.Textures[i].Block, 0, tex.Tex.Textures[i].Block.Length);

                        pos += tex.Tex.Textures[i].MipSize;

                        tex.textureSize += (uint)tex.Tex.Textures[i].MipSize;
                        tex.Tex.TexSize += (uint)tex.Tex.Textures[i].MipSize;

                        if (tex.SomeValue >= 5) tex.Tex.Textures[i].SubTexNum = 0;
                        if (tex.HasOneValueTex) tex.Tex.Textures[i].One = 1;

                        if(w > 1) w /= 2;
                        if(h > 1) h /= 2;
                    }

                    ms = new MemoryStream();
                    tmp = new byte[headerPos];
                    Array.Copy(binContent, 0, tmp, 0, tmp.Length);
                    ms.Write(tmp, 0, tmp.Length);

                    int mode = 1;
                    int res = -1;

                    if (Encoding.ASCII.GetString(check_header) == "ERTM") res = ReplaceNewTextures(ms, mode, Encoding.ASCII.GetString(check_header), tex, false);
                    else
                    {
                        if (MainMenu.settings.swizzleNintendoSwitch) tex.platform.platform = 15;
                        if (MainMenu.settings.swizzlePS4) tex.platform.platform = 11;
                        if (MainMenu.settings.swizzleXbox360) tex.platform.platform = 4;
                        if (MainMenu.settings.swizzlePSVita) tex.platform.platform = 9;

                        for(mode = 2; mode < 4; mode++)
                        {
                            res = ReplaceNewTextures(ms, mode, Encoding.ASCII.GetString(check_header), tex, false);

                            if(res == -1)
                            {
                                ms.Close();
                                return "Unsupported format";
                            }
                        }
                    }

                    NewContent = ms.ToArray();
                    ms.Close();

                    if (res == -1) return "Unsupported format";

                    if (File.Exists(MainMenu.settings.pathForOutputFolder + "\\" + fi.Name)) File.Delete(MainMenu.settings.pathForOutputFolder + "\\" + fi.Name);
                    FileStream fs = new FileStream(MainMenu.settings.pathForOutputFolder + "\\" + fi.Name, FileMode.CreateNew);
                    fs.Write(NewContent, 0, NewContent.Length);
                    fs.Close();
                    
                    return result;
                }
            }
            catch (Exception ex)
            {
                // Esta mensagem mostrará o erro exato, a função e a linha onde ocorreu.
                string detailedError = $"{fi.Name}: An error occurred.\n" +
                                       $"Message: {ex.Message}\n" +
                                       $"Location: {ex.StackTrace}";
                return detailedError;
            }
        }

        public static int ReplaceOldTextures(Stream stream, ClassesStructs.TextureClass.OldT3Texture tex, bool someData, bool NeedEncrypt, byte[] EncKey, int version)
        {
            BinaryWriter bw = new BinaryWriter(stream);

            if (someData)
            {
                bw.Write(tex.sizeBlock);
                bw.Write(tex.someValue);
            }

            byte[] tmp = Encoding.ASCII.GetBytes(tex.ObjectName);
            int len = tmp.Length;
            int addLen = len;

            if (tex.AdditionalSize)
            {
                addLen = len + 8;
                bw.Write(addLen);
            }

            bw.Write(len);
            bw.Write(tmp);

            tmp = Encoding.ASCII.GetBytes(tex.SubobjectName);
            len = tmp.Length;
            addLen = len;

            if (tex.AdditionalSize)
            {
                addLen = len + 8;
                bw.Write(addLen);
            }

            bw.Write(len);
            bw.Write(tmp);

            bw.Write(tex.Flags);
            bw.Write(tex.Mip);
            bw.Write(tex.TextureFormat);
            bw.Write(tex.Width);
            bw.Write(tex.Height);

            if (tex.isIOS)
            {
                tmp = BitConverter.GetBytes(tex.TexSize);
                Array.Copy(tmp, 0, tex.block, tex.block.Length - 0x38, tmp.Length);

                tmp = BitConverter.GetBytes(tex.Width);
                Array.Copy(tmp, 0, tex.block, tex.block.Length - 0x30, tmp.Length);

                tmp = BitConverter.GetBytes(tex.Height);
                Array.Copy(tmp, 0, tex.block, tex.block.Length - 0x2C, tmp.Length);

                int mip = tex.Mip <= 1 ? 0 : tex.Mip;

                tmp = BitConverter.GetBytes(mip);
                Array.Copy(tmp, 0, tex.block, tex.block.Length - 0x28, tmp.Length);

                tmp = BitConverter.GetBytes(tex.mobTexFormat);
                Array.Copy(tmp, 0, tex.block, tex.block.Length - 0x24, tmp.Length);

                tmp = BitConverter.GetBytes(tex.Content.Length);
                Array.Copy(tmp, 0, tex.block, tex.block.Length - 0x20, tmp.Length);
            }
            else if (tex.isPS3)
            {
                int tmpPos = tex.block.Length;

                byte texFormat = 0;

                int texSize = tex.Content.Length;
                int paddedSize = Methods.pad_size(texSize, 128);

                //cut dds header and copy to padded block
                tmp = new byte[paddedSize - 128];
                Array.Copy(tex.Content, 128, tmp, 0, tex.Content.Length - 128);
                tex.Content = new byte[tmp.Length];
                Array.Copy(tmp, 0, tex.Content, 0, tmp.Length);

                switch (tex.TextureFormat)
                {
                    case (uint)ClassesStructs.TextureClass.OldTextureFormat.DX_DXT1:
                        texFormat = 0x86;
                        break;

                    case (uint)ClassesStructs.TextureClass.OldTextureFormat.DX_DXT5:
                        texFormat = 0x88;
                        break;
                }

                tmp = new byte[1];
                tmp[0] = Convert.ToByte(tex.Mip);
                Array.Copy(tmp, 0, tex.block, tmpPos - 103, tmp.Length);

                tmp = new byte[1];
                tmp[0] = texFormat;
                Array.Copy(tmp, 0, tex.block, tmpPos - 104, tmp.Length);

                tmp = new byte[1];
                tmp[0] = Convert.ToByte(tex.Mip);
                Array.Copy(tmp, 0, tex.block, tmpPos - 103, tmp.Length);

                tmp = BitConverter.GetBytes(tex.Width).Reverse().ToArray();
                Array.Copy(tmp, 2, tex.block, tmpPos - 96, 2);

                tmp = BitConverter.GetBytes(tex.Height).Reverse().ToArray();
                Array.Copy(tmp, 2, tex.block, tmpPos - 94, 2);


                tex.TexSize = texSize;

                tmp = BitConverter.GetBytes(texSize - 128).Reverse().ToArray();
                Array.Copy(tmp, 0, tex.block, tmpPos - 124, tmp.Length);

                tmp = BitConverter.GetBytes(paddedSize - 128).Reverse().ToArray();
                Array.Copy(tmp, 0, tex.block, tmpPos - 108, tmp.Length);

                paddedSize += 4; //Add 4 bytes for common size block
                tmp = BitConverter.GetBytes(paddedSize);
                Array.Copy(tmp, 0, tex.block, tmpPos - 132, tmp.Length);
            }

            bw.Write(tex.block);
            if(!tex.isIOS && !tex.isPS3) bw.Write(tex.TexSize);

            if (NeedEncrypt && !tex.isIOS)
            {
                BlowFishCS.BlowFish blowfish = new BlowFishCS.BlowFish(EncKey, version);
                int size = 2048;
                if (size > tex.Content.Length) size = tex.Content.Length;

                tmp = new byte[size];
                Array.Copy(tex.Content, 0, tmp, 0, tmp.Length);
                tmp = blowfish.Crypt_ECB(tmp, version, false);
                Array.Copy(tmp, 0, tex.Content, 0, tmp.Length);

                blowfish = null;
                GC.Collect();
            }

            bw.Write(tex.Content);

            if ((tex.TexFlags != null) && (tex.TexFlags.TexSizes != null))
            {
                for (int j = 0; j < tex.TexFlags.TexSizes.Length; j++)
                {
                    bw.Write(tex.TexFlags.SubTexContent[j]);
                }
            }

            bw.Flush();

            return 0;
        }

        public static int ReplaceNewTextures(Stream stream, int mode, string checkHeader, ClassesStructs.TextureClass.NewT3Texture tex, bool isFont)
        {
            //mode = 1 - ERTM
            //mode = 2 & mode = 3 = files NOT header ERTM
            BinaryWriter bw = new BinaryWriter(stream);
            int pos = 0;

            // ====================================================================================
            // CORREÇÃO DE ALINHAMENTO PS4 (Adicione este bloco)
            // Isso garante que o Header do arquivo tenha o tamanho real com Padding,
            // sincronizando a leitura do jogo com os dados gravados.
            // ====================================================================================
            if (tex.platform.platform == 11) // PS4
            {
                int alignW = tex.Width;
                int alignH = tex.Height;
                // DXT1/BC4 = 8 bytes, DXT3/DXT5/BC5 = 16 bytes
                int alignBlockSize = (tex.TextureFormat == 0x40 || tex.TextureFormat == 0x43) ? 8 : 16;

                // Vamos recalcular o tamanho total
                tex.Tex.TexSize = 0;

                // Simula o loop de mipmaps para calcular o tamanho exato que o Swizzle vai gerar
                for (int i = 0; i < tex.Tex.MipCount; i++)
                {
                    // Lógica de alinhamento idêntica à classe PS4.cs
                    int hTexels = alignH / 4;
                    int wTexels = alignW / 4;

                    // Arredonda para cima para o próximo múltiplo de 8 (blocos de 32 pixels)
                    int hAligned = (hTexels + 7) / 8;
                    int wAligned = (wTexels + 7) / 8;

                    // Tamanho real ocupado em memória no PS4 (incluindo espaços vazios)
                    int alignedSize = hAligned * wAligned * 64 * alignBlockSize;

                    // Se o tamanho alinhado for maior que o tamanho linear original, 
                    // atualizamos o MipSize no objeto para que o Header seja gravado corretamente.
                    if (alignedSize > tex.Tex.Textures[i].MipSize)
                    {
                        tex.Tex.Textures[i].MipSize = alignedSize;
                    }

                    // Acumula o tamanho total
                    tex.Tex.TexSize += (uint)tex.Tex.Textures[i].MipSize;

                    // Reduz as dimensões para o próximo mip (se houver)
                    if (alignW > 1) alignW /= 2;
                    if (alignH > 1) alignH /= 2;
                }
            }
            // ====================================================================================

            // ====================================================================================
            // CORREÇÃO DE ALINHAMENTO PS VITA (NPOT swizzle padding)
            // O Vita pode exigir mips maiores (padded) em memória swizzled para texturas NPOT.
            // Se o header mantiver MipSize linear, a cauda padded é truncada na leitura seguinte.
            // ====================================================================================
            if (tex.platform.platform == 9 && !IsVitaPvrFormat(tex.TextureFormat))
            {
                int vitaW = tex.Width;
                int vitaH = tex.Height;

                tex.Tex.TexSize = 0;

                for (int i = 0; i < tex.Tex.MipCount; i++)
                {
                    int swizzleWidth;
                    int swizzleHeight;
                    int bytesPerPixelSet;
                    int formatBitsPerPixel;
                    GetVitaSwizzleInfo(tex.TextureFormat, vitaW, vitaH, out swizzleWidth, out swizzleHeight, out bytesPerPixelSet, out formatBitsPerPixel);

                    int paddedWidth = 1;
                    while (paddedWidth < swizzleWidth)
                    {
                        paddedWidth <<= 1;
                    }

                    int paddedHeight = 1;
                    while (paddedHeight < swizzleHeight)
                    {
                        paddedHeight <<= 1;
                    }

                    int paddedMipSize = (formatBitsPerPixel * paddedWidth * paddedHeight) / 8;

                    if (paddedMipSize > tex.Tex.Textures[i].MipSize)
                    {
                        tex.Tex.Textures[i].MipSize = paddedMipSize;
                    }

                    tex.Tex.TexSize += (uint)tex.Tex.Textures[i].MipSize;

                    if (vitaW > 1) vitaW /= 2;
                    if (vitaH > 1) vitaH /= 2;
                }
            }
            // ====================================================================================
            // CORREÇÃO DE ALINHAMENTO NINTENDO SWITCH (swizzle padding para NPOT)
            // Alguns mips no Switch exigem stride/alinhamento maior no bloco swizzled.
            // Sem ajustar MipSize/TexSize para o tamanho swizzled real, os bytes finais
            // da borda direita podem ficar fora da leitura no jogo.
            // ====================================================================================
            if (tex.platform.platform == 15)
            {
                int switchW = tex.Width;
                int switchH = tex.Height;

                tex.Tex.TexSize = 0;

                for (int i = 0; i < tex.Tex.MipCount; i++)
                {
                    int switchMipSize = NintendoSwitch.GetSwizzledMipSize(switchW, switchH, (int)tex.TextureFormat);

                    if (switchMipSize > tex.Tex.Textures[i].MipSize)
                    {
                        tex.Tex.Textures[i].MipSize = switchMipSize;
                    }

                    tex.Tex.TexSize += (uint)tex.Tex.Textures[i].MipSize;

                    if (switchW > 1) switchW /= 2;
                    if (switchH > 1) switchH /= 2;
                }
            }
            // ====================================================================================

            if (mode == 1 || mode == 2)
            {
                bw.Write(tex.SomeValue);
                pos += 4;
                bw.Write(tex.unknownFlags.blockSize);
                pos += 4;
                bw.Write(tex.unknownFlags.block);
                pos += 4;
                bw.Write(tex.platform.blockSize);
                pos += 4;
                bw.Write(tex.platform.platform);
                pos += 4;

                byte[] tmp = Encoding.ASCII.GetBytes(tex.ObjectName);
                int len = tmp.Length + 8;
                bw.Write(len);
                pos += 4;
                len = tmp.Length;
                bw.Write(len);
                pos += 4;
                if (tmp.Length > 0)
                {
                    bw.Write(tmp);
                    pos += tmp.Length;
                }

                tmp = Encoding.ASCII.GetBytes(tex.SubObjectName);
                len = tmp.Length + 8;
                bw.Write(len);
                pos += 4;

                len = tmp.Length;
                bw.Write(len);
                pos += 4;
                if (tmp.Length > 0)
                {
                    bw.Write(tmp);
                    pos += tmp.Length;
                }

                bw.Write(tex.OneValue);
                pos += 4;

                if ((checkHeader == "5VSM") && (tex.SomeValue > 4) && (tex.Zero != 48))
                {
                    bw.Write(tex.Zero);
                    pos += 4;
                }

                bw.Write(tex.OneByte);
                pos++;

                if (tex.OneByte == 0x31)
                {
                    bw.Write(tex.UnknownData.count);
                    pos += 4;
                    bw.Write(tex.UnknownData.Unknown1);
                    pos += 4;
                    //bw.Write(tex.UnknownData.len);
                    //pos += 4;
                    bw.Write(tex.UnknownData.someData);
                    pos += tex.UnknownData.len;//tex.UnknownData.someData.Length;
                }

                bw.Write(tex.Mip);
                pos += 4;
                bw.Write(tex.Width);
                pos += 4;
                bw.Write(tex.Height);
                pos += 4;

                //if (checkHeader == "6VSM" && tex.SomeValue == 9)
                if (((checkHeader == "6VSM") || (checkHeader == "5VSM")) && tex.SomeValue >= 8)
                {
                    bw.Write(tex.Faces);
                    pos += 4;
                    bw.Write(tex.ArrayMembers);
                    pos += 4;
                }

                bw.Write(tex.TextureFormat);
                pos += 4;
                bw.Write(tex.Unknown1);
                pos += 4;
                bw.Write(tex.block);
                pos += tex.block.Length;

                //if(tex.SomeValue == 9)
                if (tex.SomeValue >= 8)
                {
                    bw.Write(tex.subBlock2.Block);
                    pos += tex.subBlock2.Size;
                }

                bw.Write(tex.subBlock.Block);
                pos += tex.subBlock.Size;

                bw.Write(tex.Tex.MipCount);
                pos += 4;
                bw.Write(tex.Tex.SomeData);
                pos += 4;
                bw.Write(tex.Tex.TexSize);
                pos += 4;

                if (tex.platform.platform == 15)
                {
                    for (int i = 0; i < tex.Tex.MipCount; i++)
                    {
                        if (tex.SomeValue >= 5)
                        {
                            bw.Write(tex.Tex.Textures[i].SubTexNum);
                            pos += 4;
                        }

                        bw.Write(tex.Tex.Textures[i].CurrentMip);
                        pos += 4;

                        if (tex.HasOneValueTex)
                        {
                            bw.Write(tex.Tex.Textures[i].One);
                            pos += 4;
                        }

                        bw.Write(tex.Tex.Textures[i].MipSize);
                        pos += 4;
                        bw.Write(tex.Tex.Textures[i].BlockSize);
                        pos += 4;

                        //if (checkHeader == "6VSM" && tex.SomeValue == 9)
                        if (((checkHeader == "6VSM") || (checkHeader == "5VSM")) && tex.SomeValue >= 8)
                        {
                            bw.Write(tex.Tex.Textures[i].MipSize);
                            pos += 4;
                        }
                    }
                }
                else
                {
                    for (int i = tex.Tex.MipCount - 1; i >= 0; i--)
                    {
                        if (tex.SomeValue >= 5)
                        {
                            bw.Write(tex.Tex.Textures[i].SubTexNum);
                            pos += 4;
                        }

                        bw.Write(tex.Tex.Textures[i].CurrentMip);
                        pos += 4;

                        if (tex.HasOneValueTex)
                        {
                            bw.Write(tex.Tex.Textures[i].One);
                            pos += 4;
                        }

                        bw.Write(tex.Tex.Textures[i].MipSize);
                        pos += 4;
                        bw.Write(tex.Tex.Textures[i].BlockSize);
                        pos += 4;

                        //if (checkHeader == "6VSM" && tex.SomeValue == 9)
                        if (((checkHeader == "6VSM") || (checkHeader == "5VSM")) && tex.SomeValue >= 8)
                        {
                            bw.Write(tex.Tex.Textures[i].MipSize);
                            pos += 4;
                        }
                    }
                }

                if (tex.Tex.SomeData > 0)
                {
                    for (int i = 0; i < tex.Tex.SomeData; i++)
                    {
                        bw.Write(tex.Tex.SubBlocks[i].Size);
                        pos += 4;
                        bw.Write(tex.Tex.SubBlocks[i].Block);
                        pos += tex.Tex.SubBlocks[i].Block.Length;
                    }
                }

                if (checkHeader != "ERTM" && !isFont)
                {
                    tex.headerSize = pos;
                }
            }


            if(mode == 1 || mode == 3)
            {
                if (!isFont && checkHeader != "ERTM")
                {
                    tex.textureSize = 0;

                    if(tex.platform.platform == 15)
                    {
                        int w = tex.Width;
                        int h = tex.Height;

                        for (int i = 0; i < tex.Tex.MipCount; i++)
                        {
                            int switchMipSize = NintendoSwitch.GetSwizzledMipSize(w, h, (int)tex.TextureFormat);

                            tex.Tex.Textures[i].Block = NintendoSwitch.NintendoSwizzle(tex.Tex.Textures[i].Block, w, h, (int)tex.TextureFormat, false);

                            if (switchMipSize > 0 && tex.Tex.Textures[i].Block.Length >= switchMipSize)
                            {
                                tex.Tex.Textures[i].MipSize = switchMipSize;
                            }

                            bw.Write(tex.Tex.Textures[i].Block, 0, tex.Tex.Textures[i].MipSize);
                            tex.textureSize += (uint)tex.Tex.Textures[i].MipSize;

                            if (w > 1) w /= 2;
                            if (h > 1) h /= 2;
                        }
                    }
                    else
                    {
                        if (tex.platform.platform == 4) // Xbox 360 Swizzle
                        {
                            int w = tex.Width;
                            int h = tex.Height;

                            int texelBytePitch;
                            int blockPixelSize;
                            bool performByteSwap;

                            if (tex.TextureFormat == 0x00) // Formato ARGB 8.8.8.8
                            {
                                texelBytePitch = 4;
                                blockPixelSize = 1;
                                performByteSwap = false;
                            }
                            else if (tex.TextureFormat == 0x40 || tex.TextureFormat == 0x43) // DXT1, BC4
                            {
                                texelBytePitch = 8;
                                blockPixelSize = 4;
                                performByteSwap = true;
                            }
                            else // DXT3, DXT5, BC5
                            {
                                texelBytePitch = 16;
                                blockPixelSize = 4;
                                performByteSwap = true;
                            }

                            for (int i = 0; i < tex.Tex.MipCount; i++)
                            {
                                if (tex.TextureFormat == 0x00)
                                {
                                    tex.Tex.Textures[i].Block = Swizzles.Xbox360.ConvertBGRAtoARGB(tex.Tex.Textures[i].Block);
                                }

                                byte[] swizzledPaddedBlock = Swizzles.Xbox360.Swizzle(tex.Tex.Textures[i].Block, w, h, texelBytePitch, blockPixelSize, performByteSwap);
                                int originalMipSize = tex.Tex.Textures[i].MipSize;

                                if (swizzledPaddedBlock.Length > originalMipSize)
                                {
                                    byte[] truncatedBlock = new byte[originalMipSize];
                                    Array.Copy(swizzledPaddedBlock, 0, truncatedBlock, 0, originalMipSize);
                                    tex.Tex.Textures[i].Block = truncatedBlock;
                                }
                                else
                                {
                                    tex.Tex.Textures[i].Block = swizzledPaddedBlock;
                                }

                                if (w > 1) w /= 2;
                                if (h > 1) h /= 2;
                            }
                        }
                        else if (tex.platform.platform == 11)
                        {
                            int w = tex.Width;
                            int h = tex.Height;

                            int blockSize = tex.TextureFormat == 0x40 || tex.TextureFormat == 0x43 ? 8 : 16;

                            for (int i = 0; i < tex.Tex.MipCount; i++)
                            {
                                if (tex.Tex.Textures[i].Block.Length < blockSize) blockSize = tex.Tex.Textures[i].Block.Length;
                                tex.Tex.Textures[i].Block = PS4.Swizzle(tex.Tex.Textures[i].Block, w, h, blockSize);

                                if (w > 1) w /= 2;
                                if (h > 1) h /= 2;
                            }
                        }
                        else if (tex.platform.platform == 9 && !IsVitaPvrFormat(tex.TextureFormat))
                        {
                            int w = tex.Width;
                            int h = tex.Height;

                            for (int i = 0; i < tex.Tex.MipCount; i++)
                            {
                                int swizzleWidth;
                                int swizzleHeight;
                                int bytesPerPixelSet;
                                int formatBitsPerPixel;
                                GetVitaSwizzleInfo(tex.TextureFormat, w, h, out swizzleWidth, out swizzleHeight, out bytesPerPixelSet, out formatBitsPerPixel);

                                int safeBppSet = bytesPerPixelSet;
                                if (tex.Tex.Textures[i].Block.Length > 0 && safeBppSet > tex.Tex.Textures[i].Block.Length)
                                {
                                    safeBppSet = tex.Tex.Textures[i].Block.Length;
                                }

                                if (safeBppSet > 0)
                                {
                                    tex.Tex.Textures[i].Block = PSVita.Swizzle(tex.Tex.Textures[i].Block, swizzleWidth, swizzleHeight, safeBppSet, formatBitsPerPixel);
                                }

                                if (w > 1) w /= 2;
                                if (h > 1) h /= 2;
                            }
                        }

                        for (int i = tex.Tex.MipCount - 1; i >= 0; i--)
                        {
                            bw.Write(tex.Tex.Textures[i].Block);
                            tex.textureSize += (uint)tex.Tex.Textures[i].Block.Length;
                        }
                    }

                    bw.BaseStream.Seek(4, SeekOrigin.Begin);
                    bw.Write(tex.headerSize);
                    bw.BaseStream.Seek(12, SeekOrigin.Begin);
                    bw.Write(tex.textureSize);
                }
                else
                {
                    for (int i = tex.Tex.MipCount - 1; i >= 0; i--)
                   {
                        bw.Write(tex.Tex.Textures[i].Block);
                   }
                }
            }

            bw.Flush();
            return 0;
        }

        public static ClassesStructs.TextureClass.OldT3Texture GetOldTextures(byte[] binContent, ref int poz, bool flags, bool someData)
        {
            //Try read begin of file
            try
            {
                ClassesStructs.TextureClass.OldT3Texture tex = new ClassesStructs.TextureClass.OldT3Texture();
                tex.TexFlags = null;

                if (flags) tex.TexFlags = new ClassesStructs.FlagsClass();
                tex.BlockPos = 0;

                if (someData)
                {
                    byte[] someBinData = new byte[4];
                    Array.Copy(binContent, poz, someBinData, 0, someBinData.Length);
                    tex.sizeBlock = BitConverter.ToInt32(someBinData, 0);
                    poz += 4;
                    tex.BlockPos += 4;

                    someBinData = new byte[4];
                    Array.Copy(binContent, poz, someBinData, 0, someBinData.Length);
                    tex.someValue = BitConverter.ToInt32(someBinData, 0);
                    poz += 4;
                    tex.BlockPos += 4;

                    someBinData = null;
                }

                byte[] tmp = new byte[4];
                Array.Copy(binContent, poz, tmp, 0, tmp.Length);
                int nameLen = BitConverter.ToInt32(tmp, 0);
                poz += 4;
                tex.BlockPos += 4;

                tmp = new byte[4];
                Array.Copy(binContent, poz, tmp, 0, tmp.Length);
                if (nameLen - BitConverter.ToInt32(tmp, 0) == 8)
                {
                    nameLen = BitConverter.ToInt32(tmp, 0);
                    poz += 4;
                    tex.BlockPos += 4;

                    tex.AdditionalSize = true;
                }

                tmp = new byte[nameLen];
                Array.Copy(binContent, poz, tmp, 0, tmp.Length);
                poz += nameLen;
                tex.BlockPos += nameLen;
                tex.ObjectName = Encoding.ASCII.GetString(tmp);

                tmp = new byte[4];
                Array.Copy(binContent, poz, tmp, 0, tmp.Length);
                nameLen = BitConverter.ToInt32(tmp, 0);
                poz += 4;
                tex.BlockPos += 4;

                tmp = new byte[4];
                Array.Copy(binContent, poz, tmp, 0, tmp.Length);
                if (nameLen - BitConverter.ToInt32(tmp, 0) == 8)
                {
                    nameLen = BitConverter.ToInt32(tmp, 0);
                    poz += 4;
                    tex.BlockPos += 4;
                }

                tmp = new byte[nameLen];
                Array.Copy(binContent, poz, tmp, 0, tmp.Length);
                poz += nameLen;
                tex.BlockPos += nameLen;
                tex.SubobjectName = Encoding.ASCII.GetString(tmp);

                tmp = new byte[8];
                Array.Copy(binContent, poz, tmp, 0, tmp.Length);

                int counter = 0;

                for (int k = 0; k < tmp.Length; k++)
                {
                    if ((tmp[k] == 0x30) || (tmp[k] == 0x31))
                    {
                        counter++;
                    }
                }

                tex.Flags = new byte[counter];
                Array.Copy(binContent, poz, tex.Flags, 0, tex.Flags.Length);
                poz += tex.Flags.Length;
                tex.BlockPos += tex.Flags.Length;

                tmp = new byte[4];
                Array.Copy(binContent, poz, tmp, 0, tmp.Length);
                tex.Mip = BitConverter.ToInt32(tmp, 0);
                poz += 4;
                tex.BlockPos += 4;

                tmp = new byte[4];
                Array.Copy(binContent, poz, tmp, 0, tmp.Length);
                poz += 4;
                tex.BlockPos += 4;
                tex.TextureFormat = BitConverter.ToUInt32(tmp, 0);

                if (tex.TextureFormat == 0) return null;

                tmp = new byte[4];
                Array.Copy(binContent, poz, tmp, 0, tmp.Length);
                tex.OriginalWidth = BitConverter.ToInt32(tmp, 0);
                poz += 4;
                tex.BlockPos += 4;

                tmp = new byte[4];
                Array.Copy(binContent, poz, tmp, 0, tmp.Length);
                tex.OriginalHeight = BitConverter.ToInt32(tmp, 0);
                poz += 4;
                tex.BlockPos += 4;

                int lastPos = poz;

                poz = Methods.FindStartOfStringSomething(binContent, lastPos, "DDS ");
                tex.isIOS = Methods.FindStartOfStringSomething(binContent, lastPos, "PVR!") != -1;
                tex.isPS3 = Methods.FindStartOfStringSomething(binContent, lastPos, "\x02\x01\x01\x00") != -1;

                if ((!tex.isIOS || !tex.isPS3) && poz != -1)
                {
                    poz -= 4;
                    tex.isPS3 = false;
                    tex.isIOS = false;
                }
                else
                {
                    if (tex.isIOS) poz = Methods.FindStartOfStringSomething(binContent, lastPos, "PVR!") + 8;
                    else poz = Methods.FindStartOfStringSomething(binContent, lastPos, "\x02\x01\x01\x00") + 128;
                }

                tex.block = new byte[poz - lastPos];
                Array.Copy(binContent, lastPos, tex.block, 0, tex.block.Length);
                tex.BlockPos += tex.block.Length;
                byte formatTex = 0;
                int paddedSize = 0;

                if(tex.isIOS)
                {
                    tmp = new byte[4];
                    Array.Copy(binContent, poz - 0x38, tmp, 0, tmp.Length);
                    tex.TexSize = BitConverter.ToInt32(tmp, 0) - 0x34;

                    tmp = new byte[4];
                    Array.Copy(binContent, poz - 0x2c, tmp, 0, tmp.Length);
                    tex.Width = BitConverter.ToInt32(tmp, 0);
                    tex.OriginalWidth = tex.Width;

                    tmp = new byte[4];
                    Array.Copy(binContent, poz - 0x30, tmp, 0, tmp.Length);
                    tex.Height = BitConverter.ToInt32(tmp, 0);
                    tex.OriginalHeight = tex.Height;

                    tmp = new byte[4];
                    Array.Copy(binContent, poz - 0x24, tmp, 0, tmp.Length);
                    tex.mobTexFormat = BitConverter.ToUInt32(tmp, 0);

                    tmp = new byte[4];
                    Array.Copy(binContent, poz - 0x28, tmp, 0, tmp.Length);
                    tex.Mip = BitConverter.ToInt32(tmp, 0);
                }
                else if(tex.isPS3)
                {
                    tmp = new byte[4];
                    Array.Copy(binContent, poz - 124, tmp, 0, tmp.Length);
                    Array.Reverse(tmp);
                    tex.TexSize = BitConverter.ToInt32(tmp, 0);

                    tmp = new byte[4];
                    Array.Copy(binContent, poz - 108, tmp, 0, tmp.Length);
                    Array.Reverse(tmp);
                    paddedSize = BitConverter.ToInt32(tmp, 0);

                    tmp = new byte[2];
                    Array.Copy(binContent, poz - 96, tmp, 0, tmp.Length);
                    Array.Reverse(tmp);
                    tex.Width = BitConverter.ToInt16(tmp, 0);

                    tmp = new byte[2];
                    Array.Copy(binContent, poz - 94, tmp, 0, tmp.Length);
                    Array.Reverse(tmp);
                    tex.Height = BitConverter.ToInt16(tmp, 0);

                    tmp = new byte[1];
                    Array.Copy(binContent, poz - 104, tmp, 0, tmp.Length);
                    formatTex = tmp[0];

                    tmp = new byte[1];
                    Array.Copy(binContent, poz - 103, tmp, 0, tmp.Length);
                    tex.Mip = Convert.ToInt32(tmp[0]);
                }
                else
                {
                    tmp = new byte[4];
                    Array.Copy(binContent, poz, tmp, 0, tmp.Length);
                    tex.TexSize = BitConverter.ToInt32(tmp, 0);
                    poz += 4;

                    tex.BlockPos += 4;
                }
                
                tex.Content = new byte[tex.TexSize];
                Array.Copy(binContent, poz, tex.Content, 0, tex.Content.Length);
                poz += tex.isPS3 ? paddedSize : tex.Content.Length;

                if(tex.isIOS)
                {
                    byte[] header = GenPvrHeader(tex.Width, tex.Height, tex.Mip, tex.mobTexFormat, 0, 0, false);

                    if (header == null) return null;

                    tmp = new byte[tex.Content.Length + header.Length];
                    Array.Copy(header, 0, tmp, 0, header.Length);
                    Array.Copy(tex.Content, 0, tmp, header.Length, tex.Content.Length);
                    tex.Content = new byte[tmp.Length];
                    Array.Copy(tmp, 0, tex.Content, 0, tex.Content.Length);
                }
                else if(tex.isPS3)
                {
                    uint format = 0;
                    string texFormat = "";

                    switch(formatTex)
                    {
                        case 0x86: //DXT1
                        case 0xA6:
                            format = 0x40;
                            break;

                        case 0x88: //DXT5
                        case 0xA8: //DXT5
                            format = 0x42;
                            break;

                        case 0x81:
                            format = 4; //ARGB4444?
                            break;
                    }

                    byte[] header = GenHeader(format, tex.Width, tex.Height, (uint)tex.TexSize, 0, 0, tex.Mip, ref texFormat);

                    if (header == null) return null;

                    tmp = new byte[tex.Content.Length + header.Length];
                    Array.Copy(header, 0, tmp, 0, header.Length);
                    Array.Copy(tex.Content, 0, tmp, header.Length, tex.Content.Length);
                    tex.Content = new byte[tmp.Length];
                    Array.Copy(tmp, 0, tex.Content, 0, tex.Content.Length);
                }
                else
                {
                    tmp = new byte[4];
                    Array.Copy(tex.Content, 16, tmp, 0, tmp.Length);
                    tex.Width = BitConverter.ToInt32(tmp, 0);

                    tmp = new byte[4];
                    Array.Copy(tex.Content, 12, tmp, 0, tmp.Length);
                    tex.Height = BitConverter.ToInt32(tmp, 0);
                }

                if ((tex.TexFlags != null) && (tex.TexFlags.TexSizes != null)) 
                {
                    for (int j = 0; j < tex.TexFlags.TexSizes.Length; j++)
                    {
                        tex.TexFlags.SubTexContent[j] = new byte[tex.TexFlags.TexSizes[j]];
                        Array.Copy(binContent, poz, tex.TexFlags.SubTexContent[j], 0, tex.TexFlags.SubTexContent[j].Length);
                        poz += tex.TexFlags.TexSizes[j];
                    }
                }

                return tex;
            }
            catch
            {
                return null;
            }
        }

        //Extract new format textures (Since Poker Night 2)
        public static ClassesStructs.TextureClass.NewT3Texture GetNewTextures(byte[] binContent, ref int poz, ref uint texFontPoz, bool flags, bool someData, bool isFont, ref string AdditionalInfo, bool AddInfo)
        {
            ClassesStructs.TextureClass.NewT3Texture tex = new ClassesStructs.TextureClass.NewT3Texture();
            byte[] tmp = new byte[4];
            Array.Copy(binContent, 0, tmp, 0, tmp.Length);
            string checkHeader = Encoding.ASCII.GetString(tmp);

            if ((checkHeader != "ERTM") && !isFont)
            {
                    tmp = new byte[4];
                    Array.Copy(binContent, 4, tmp, 0, tmp.Length);
                    tex.headerSize = BitConverter.ToInt32(tmp, 0);

                    tmp = new byte[4];
                    Array.Copy(binContent, 12, tmp, 0, tmp.Length);
                    tex.textureSize = BitConverter.ToUInt32(tmp, 0);
            }

            tex.HasOneValueTex = false;
            int beginHeader = poz;
            
            Array.Copy(binContent, poz, tmp, 0, tmp.Length);
            tex.SomeValue = BitConverter.ToInt32(tmp, 0);
            poz += 4;

            tmp = new byte[4];
            Array.Copy(binContent, poz, tmp, 0, tmp.Length);
            tex.unknownFlags.blockSize = BitConverter.ToInt32(tmp, 0);
            poz += 4;

            tmp = new byte[4];
            Array.Copy(binContent, poz, tmp, 0, tmp.Length);
            tex.unknownFlags.block = BitConverter.ToUInt32(tmp, 0);
            poz += 4;

            tmp = new byte[4];
            Array.Copy(binContent, poz, tmp, 0, tmp.Length);
            tex.platform.blockSize = BitConverter.ToInt32(tmp, 0);
            poz += 4;

            tmp = new byte[4];
            Array.Copy(binContent, poz, tmp, 0, tmp.Length);
            tex.platform.platform = BitConverter.ToUInt32(tmp, 0);
            poz += 4;

            tmp = new byte[4];
            Array.Copy(binContent, poz, tmp, 0, tmp.Length);
            int tmpInt = BitConverter.ToInt32(tmp, 0);
            poz += 4;

            tmp = new byte[4];
            Array.Copy(binContent, poz, tmp, 0, tmp.Length);
            tmpInt = BitConverter.ToInt32(tmp, 0);
            poz += 4;

            tmp = new byte[tmpInt];
            Array.Copy(binContent, poz, tmp, 0, tmp.Length);
            tex.ObjectName = Encoding.ASCII.GetString(tmp);
            poz += tmpInt;

            tmp = new byte[4];
            Array.Copy(binContent, poz, tmp, 0, tmp.Length);
            tmpInt = BitConverter.ToInt32(tmp, 0);
            poz += 4;

            tmp = new byte[4];
            Array.Copy(binContent, poz, tmp, 0, tmp.Length);
            tmpInt = BitConverter.ToInt32(tmp, 0);
            poz += 4;

            tmp = new byte[tmpInt];
            Array.Copy(binContent, poz, tmp, 0, tmp.Length);
            tex.SubObjectName = Encoding.ASCII.GetString(tmp);
            poz += tmpInt;

            tmp = new byte[4];
            Array.Copy(binContent, poz, tmp, 0, tmp.Length);
            tex.OneValue = BitConverter.ToSingle(tmp, 0);
            poz += 4;

            tmp = new byte[4];
            Array.Copy(binContent, poz, tmp, 0, tmp.Length);

            if (checkHeader == "5VSM" && tex.SomeValue > 4)
            {
                tex.Zero = 48;
                //if (BitConverter.ToInt32(tmp, 0) == 0)
                if (tmp[0] != 0x30 && tmp[0] != 0x31)
                {
                    tex.Zero = BitConverter.ToInt32(tmp, 0);
                    poz += 4;
                }
                
                tmp = new byte[1];
                Array.Copy(binContent, poz, tmp, 0, tmp.Length);
            }

            tex.OneByte = tmp[0];
            poz++;

            if(tex.OneByte == 0x31)
            {
                tex.UnknownData = new ClassesStructs.FlagsClass.SomeClassData();

                tmp = new byte[4];
                Array.Copy(binContent, poz, tmp, 0, tmp.Length);
                tex.UnknownData.count = BitConverter.ToInt32(tmp, 0);
                poz += 4;

                tmp = new byte[4];
                Array.Copy(binContent, poz, tmp, 0, tmp.Length);
                tex.UnknownData.Unknown1 = BitConverter.ToInt32(tmp, 0);
                poz += 4;

                tmp = new byte[4];
                Array.Copy(binContent, poz, tmp, 0, tmp.Length);
                tex.UnknownData.len = BitConverter.ToInt32(tmp, 0);

                tex.UnknownData.someData = new byte[tex.UnknownData.len];
                Array.Copy(binContent, poz, tex.UnknownData.someData, 0, tex.UnknownData.someData.Length);

                poz += tex.UnknownData.len;
            }

            tmp = new byte[4];
            Array.Copy(binContent, poz, tmp, 0, tmp.Length);
            tex.Mip = BitConverter.ToInt32(tmp, 0);
            poz += 4;

            tmp = new byte[4];
            Array.Copy(binContent, poz, tmp, 0, tmp.Length);
            tex.Width = BitConverter.ToInt32(tmp, 0);
            poz += 4;

            tmp = new byte[4];
            Array.Copy(binContent, poz, tmp, 0, tmp.Length);
            tex.Height = BitConverter.ToInt32(tmp, 0);
            poz += 4;

            //if(checkHeader == "6VSM" && tex.SomeValue == 9)
            if (((checkHeader == "6VSM") || (checkHeader == "5VSM")) && tex.SomeValue >= 8)
            {
                tmp = new byte[4];
                Array.Copy(binContent, poz, tmp, 0, tmp.Length);
                tex.Faces = BitConverter.ToInt32(tmp, 0);
                poz += 4;

                tmp = new byte[4];
                Array.Copy(binContent, poz, tmp, 0, tmp.Length);
                tex.ArrayMembers = BitConverter.ToInt32(tmp, 0);
                poz += 4;

                /*if (tex.Faces > 1 || tex.ArrayMembers > 1)
                {
                    return null; //Need think about it!
                }*/
                if (tex.Faces > 1) return null;
            }

            tmp = new byte[4];
            Array.Copy(binContent, poz, tmp, 0, tmp.Length);
            tex.TextureFormat = BitConverter.ToUInt32(tmp, 0);
            poz += 4;

            tmp = new byte[4];
            Array.Copy(binContent, poz, tmp, 0, tmp.Length);
            tex.Unknown1 = BitConverter.ToInt32(tmp, 0);
            poz += 4;

            int blSize = 0x24;

            switch (tex.SomeValue)
            {
                case 3:
                    blSize = checkHeader == "ERTM" ? 0x24 : 0x28; //I hope this solution will work correct
                    break;
                case 4:
                    blSize = 0x28;
                    break;
                case 5:
                case 7:
                    blSize = 0x34;
                    break;
                case 8:
                case 9:
                    blSize = 0x38;
                    break;
            }

            if (AddInfo) blSize += 4;

            tex.block = new byte[blSize];
            Array.Copy(binContent, poz, tex.block, 0, tex.block.Length);

            poz += tex.block.Length;

            //if(tex.SomeValue == 9)
            if (tex.SomeValue >= 8)
            {
                tmp = new byte[4];
                Array.Copy(binContent, poz, tmp, 0, tmp.Length);
                tex.subBlock2.Size = BitConverter.ToInt32(tmp, 0);
                tex.subBlock2.Block = new byte[tex.subBlock2.Size];
                Array.Copy(binContent, poz, tex.subBlock2.Block, 0, tex.subBlock2.Block.Length);
                poz += tex.subBlock2.Size;
            }

            tmp = new byte[4];
            Array.Copy(binContent, poz, tmp, 0, tmp.Length);
            tex.subBlock.Size = BitConverter.ToInt32(tmp, 0);
            tex.subBlock.Block = new byte[tex.subBlock.Size];
            Array.Copy(binContent, poz, tex.subBlock.Block, 0, tex.subBlock.Block.Length);
            poz += tex.subBlock.Size;

            tmp = new byte[4];
            Array.Copy(binContent, poz, tmp, 0, tmp.Length);
            tex.Tex.MipCount = BitConverter.ToInt32(tmp, 0);
            poz += 4;

            tmp = new byte[4];
            Array.Copy(binContent, poz, tmp, 0, tmp.Length);
            tex.Tex.SomeData = BitConverter.ToInt32(tmp, 0);
            poz += 4;

            tmp = new byte[4];
            Array.Copy(binContent, poz, tmp, 0, tmp.Length);
            tex.Tex.TexSize = BitConverter.ToUInt32(tmp, 0);
            poz += 4;

            tex.Tex.Textures = new ClassesStructs.TextureClass.NewT3Texture.TextureStruct[tex.Tex.MipCount];

            if (tex.platform.platform == 15) //Nintendo Switch format
            {
                for (int i = 0; i < tex.Tex.MipCount; i++)
                {
                    ReadNewD3dtxHeader(ref tex, i, ref poz, binContent, checkHeader);
                }
            }
            else
            {
                for (int i = tex.Tex.MipCount - 1; i >= 0; i--)
                {
                    ReadNewD3dtxHeader(ref tex, i, ref poz, binContent, checkHeader);
                }
            }

            if(tex.Tex.SomeData > 0)
            {
                tex.Tex.SubBlocks = new ClassesStructs.TextureClass.NewT3Texture.SubBlock[tex.Tex.SomeData];

                for(int i = 0; i < tex.Tex.SomeData; i++)
                {
                    tmp = new byte[4];
                    Array.Copy(binContent, poz, tmp, 0, tmp.Length);
                    tex.Tex.SubBlocks[i].Size = BitConverter.ToInt32(tmp, 0);

                    tex.Tex.SubBlocks[i].Block = new byte[tex.Tex.SubBlocks[i].Size];
                    Array.Copy(binContent, poz, tex.Tex.SubBlocks[i].Block, 0, tex.Tex.SubBlocks[i].Block.Length);
                    poz += tex.Tex.SubBlocks[i].Block.Length;
                }
            }

            tex.Tex.headerSize = poz - beginHeader;

            string format = "";

            uint ArrayMembers = tex.ArrayMembers > 1 ? (uint)tex.ArrayMembers : 0;
            uint Faces = tex.Faces > 1 ? (uint)tex.Faces : 0;

            bool vitaIsPvr = tex.platform.platform == 9 && IsVitaPvrFormat(tex.TextureFormat);
            bool usePvrHeader = (tex.platform.platform == 7) || vitaIsPvr || (tex.ArrayMembers > 1);

            byte[] header = usePvrHeader ? GenPvrHeader(tex.Width, tex.Height, tex.Tex.MipCount, (uint)tex.TextureFormat, ArrayMembers, Faces, true) : GenHeader(tex.TextureFormat, tex.Width, tex.Height, tex.Tex.TexSize, tex.Faces, tex.ArrayMembers, tex.Tex.MipCount, ref format);

            tex.isPVR = usePvrHeader;

            if (header == null)
            {
                return null;
            }

            AdditionalInfo = "Platform:" + tex.platform.platform + ". Texture format: " + format + ". Mip count: " + Convert.ToString(tex.Mip);

            if (((checkHeader == "6VSM") || (checkHeader == "5VSM")) && tex.SomeValue >= 8)
            {
                AdditionalInfo += ". Faces: " + Convert.ToString(tex.Faces) + ". Array members: " + Convert.ToString(tex.ArrayMembers);
            }

            tex.Tex.Content = new byte[tex.Tex.TexSize + header.Length];
            Array.Copy(header, 0, tex.Tex.Content, 0, header.Length);
            int texPoz = tex.platform.platform == 15 ? header.Length : tex.Tex.Content.Length;

            uint tmpPoz = (uint)poz;
            if (texFontPoz != 0) tmpPoz = texFontPoz;

            if(tex.platform.platform == 15) //For Nintendo Switch
            {
                int w = tex.Width;
                int h = tex.Height;

                for (int i = 0; i < tex.Tex.MipCount; i++)
                {
                    tex.Tex.Textures[i].Block = new byte[tex.Tex.Textures[i].MipSize];
                    Array.Copy(binContent, tmpPoz, tex.Tex.Textures[i].Block, 0, tex.Tex.Textures[i].Block.Length);
                    tex.Tex.Textures[i].Block = NintendoSwitch.NintendoSwizzle(tex.Tex.Textures[i].Block, w, h, (int)tex.TextureFormat, true);
                    tmpPoz += (uint)tex.Tex.Textures[i].MipSize;

                    Array.Copy(tex.Tex.Textures[i].Block, 0, tex.Tex.Content, texPoz, tex.Tex.Textures[i].Block.Length);
                    texPoz += tex.Tex.Textures[i].MipSize;

                    if (w > 1) w /= 2;
                    if (h > 1) h /= 2;
                }
            }
            else
            {
                if (tex.ArrayMembers > 1)
                {
                    int c = tex.Tex.MipCount - 1;

                    for(int i = tex.Mip - 1; i >= 0; i--)
                    {
                        for(int j = 0; j < tex.ArrayMembers; j++)
                        {
                            texPoz -= tex.Tex.Textures[c].MipSize;
                            tex.Tex.Textures[c].Block = new byte[tex.Tex.Textures[c].MipSize];
                            Array.Copy(binContent, tmpPoz, tex.Tex.Textures[c].Block, 0, tex.Tex.Textures[c].Block.Length);
                            tmpPoz += (uint)tex.Tex.Textures[c].MipSize;

                            Array.Copy(tex.Tex.Textures[c].Block, 0, tex.Tex.Content, texPoz, tex.Tex.Textures[c].Block.Length);
                            c--;
                        }
                    }
                }
                else
                {
                    for (int i = tex.Tex.MipCount - 1; i >= 0; i--)
                    {
                        texPoz -= tex.Tex.Textures[i].MipSize;
                        tex.Tex.Textures[i].Block = new byte[tex.Tex.Textures[i].MipSize];
                        Array.Copy(binContent, tmpPoz, tex.Tex.Textures[i].Block, 0, tex.Tex.Textures[i].Block.Length);

                        tmpPoz += (uint)tex.Tex.Textures[i].MipSize;

                        Array.Copy(tex.Tex.Textures[i].Block, 0, tex.Tex.Content, texPoz, tex.Tex.Textures[i].Block.Length);
                    }
                }

                bool needsReconstruction = false;

                if (tex.platform.platform == 11) // PS4 Unswizzle
                {
                    needsReconstruction = true;
                    int w = tex.Width;
                    int h = tex.Height;
                    int blockSize = tex.TextureFormat == 0x40 || tex.TextureFormat == 0x43 ? 8 : 16;

                    for (int i = 0; i < tex.Tex.MipCount; i++)
                    {
                        if (tex.Tex.Textures[i].Block.Length < blockSize) blockSize = tex.Tex.Textures[i].Block.Length;
                        tex.Tex.Textures[i].Block = PS4.Unswizzle(tex.Tex.Textures[i].Block, w, h, blockSize);

                        if (w > 1) w /= 2;
                        if (h > 1) h /= 2;
                    }
                }
                else if (tex.platform.platform == 4) // Xbox 360 Unswizzle
                {
                    needsReconstruction = true;
                    int w = tex.Width;
                    int h = tex.Height;

                    int texelBytePitch;
                    int blockPixelSize;
                    bool performByteSwap;

                    if (tex.TextureFormat == 0x00) // ARGB 8.8.8.8
                    {
                        texelBytePitch = 4;
                        blockPixelSize = 1;
                        performByteSwap = false;
                    }
                    else if (tex.TextureFormat == 0x40 || tex.TextureFormat == 0x43) // DXT1, BC4
                    {
                        texelBytePitch = 8;
                        blockPixelSize = 4;
                        performByteSwap = true;
                    }
                    else // DXT3, DXT5, BC5
                    {
                        texelBytePitch = 16;
                        blockPixelSize = 4;
                        performByteSwap = true; // FAZER o byte swap de 16-bit
                    }

                    for (int i = 0; i < tex.Tex.MipCount; i++)
                    {
                        tex.Tex.Textures[i].Block = Swizzles.Xbox360.Unswizzle(tex.Tex.Textures[i].Block, w, h, texelBytePitch, blockPixelSize, performByteSwap);

                        if (tex.TextureFormat == 0x00)
                        {
                            tex.Tex.Textures[i].Block = Swizzles.Xbox360.ConvertARGBtoBGRA(tex.Tex.Textures[i].Block);
                        }

                        if (w > 1) w /= 2;
                        if (h > 1) h /= 2;
                    }
                }
                else if (tex.platform.platform == 9 && !IsVitaPvrFormat(tex.TextureFormat)) // PS Vita Unswizzle
                {
                    needsReconstruction = true;
                    int w = tex.Width;
                    int h = tex.Height;

                    for (int i = 0; i < tex.Tex.MipCount; i++)
                    {
                        int swizzleWidth;
                        int swizzleHeight;
                        int bytesPerPixelSet;
                        int formatBitsPerPixel;
                        GetVitaSwizzleInfo(tex.TextureFormat, w, h, out swizzleWidth, out swizzleHeight, out bytesPerPixelSet, out formatBitsPerPixel);

                        int safeBppSet = bytesPerPixelSet;
                        if (tex.Tex.Textures[i].Block.Length > 0 && safeBppSet > tex.Tex.Textures[i].Block.Length)
                        {
                            safeBppSet = tex.Tex.Textures[i].Block.Length;
                        }

                        if (safeBppSet > 0)
                        {
                            tex.Tex.Textures[i].Block = PSVita.Unswizzle(tex.Tex.Textures[i].Block, swizzleWidth, swizzleHeight, safeBppSet, formatBitsPerPixel);
                        }

                        if (w > 1) w /= 2;
                        if (h > 1) h /= 2;
                    }
                }

                if (needsReconstruction)
                {
                    uint newTotalImageSize = 0;
                    for (int i = 0; i < tex.Tex.MipCount; i++)
                    {
                        newTotalImageSize += (uint)tex.Tex.Textures[i].Block.Length;
                    }

                    byte[] newContent = new byte[header.Length + newTotalImageSize];

                    Array.Copy(header, 0, newContent, 0, header.Length);

                    int currentPosition = header.Length;
                    for (int i = 0; i < tex.Tex.MipCount; i++)
                    {
                        Array.Copy(tex.Tex.Textures[i].Block, 0, newContent, currentPosition, tex.Tex.Textures[i].Block.Length);
                        currentPosition += tex.Tex.Textures[i].Block.Length;
                    }

                    tex.Tex.Content = newContent;
                }
            }

            tmp = new byte[4];
            Array.Copy(binContent, 0, tmp, 0, tmp.Length);
            if (Encoding.ASCII.GetString(tmp) == "ERTM") poz = (int)tmpPoz;
            else texFontPoz = tmpPoz;

            return tex;
        }
    }
}
