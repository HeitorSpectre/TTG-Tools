/*************************************************************************************************
 *                                       List of docs:                                           *
 * https://docs.microsoft.com/en-us/windows/win32/direct3ddds/dds-header                         *
 * https://docs.microsoft.com/en-us/windows/win32/api/d3d10/ne-d3d10-d3d10_resource_dimension    *
 * https://docs.microsoft.com/en-us/windows/win32/api/dxgiformat/ne-dxgiformat-dxgi_format       *
 *************************************************************************************************/

namespace TTG_Tools.Graphics.DDS
{
    public class Flags
    {
        //DDS flags
        public readonly uint DDSD_CAPS = 0x1;
        public readonly uint DDSD_HEIGHT = 0x2;
        public readonly uint DDSD_WIDTH = 0x4;
        public readonly uint DDSD_PITCH = 0x8;
        public readonly uint DDSD_PIXELFORMAT = 0x1000;
        public readonly uint DDSD_MIPMAPCOUNT = 0x20000;
        public readonly uint DDSD_LINEARSIZE = 0x80000;
        public readonly uint DDSD_DEPTH = 0x800000;

        //Pixelformat flags
        public readonly uint DDPF_ALPHAPIXELS = 0x1;
        public readonly uint DDPF_ALPHA = 0x2;
        public readonly uint DDPF_FOURCC = 0x4;
        public readonly uint DDPF_RGB = 0x40;
        public readonly uint DDPF_YUV = 0x200;
        public readonly uint DDPF_LUMINANCE = 0x20000;
    }
}
