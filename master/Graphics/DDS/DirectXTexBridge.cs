using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace TTG_Tools.Graphics.DDS
{
    /// <summary>
    /// Uses Microsoft's DirectXTex texconv for the BC compression step. The old
    /// in-process encoder remains available as a fallback if the helper cannot run.
    /// </summary>
    internal static class DirectXTexBridge
    {
        private const uint BC1 = 0x40;
        private const uint BC2 = 0x41;
        private const uint BC3 = 0x42;
        private const uint BC4 = 0x43;
        private const uint BC5 = 0x44;

        public static byte[] CompressPngToDds(byte[] png, uint format, int mipCount)
        {
            string dxgiFormat;
            string expectedFourCc;
            if (!TryGetFormat(format, out dxgiFormat, out expectedFourCc)) return null;
            if (png == null || png.Length == 0) return null;
            if (mipCount < 1) mipCount = 1;

            string exe = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "Tools", "DirectXTex", "texconv.exe");
            if (!File.Exists(exe)) return null;

            string tempDir = Path.Combine(Path.GetTempPath(), "TTGTools_DirectXTex_" + Guid.NewGuid().ToString("N"));
            string inputPath = Path.Combine(tempDir, "texture.png");
            string outputPath = Path.Combine(tempDir, "texture.dds");

            try
            {
                Directory.CreateDirectory(tempDir);
                File.WriteAllBytes(inputPath, png);

                // --ignore-srgb preserves the numeric PNG channel values. Telltale stores
                // surface gamma separately in D3DTX, so texconv must not apply a colour-space
                // conversion here. -dx9 emits the legacy FourCC headers expected by TTG Tools.
                string args = "-nologo -y --ignore-srgb -dx9 -m " + mipCount
                    + " -f " + dxgiFormat + " -ft dds -o "
                    + Quote(tempDir) + " " + Quote(inputPath);

                using (Process process = new Process())
                {
                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName = exe,
                        Arguments = args,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        WorkingDirectory = tempDir
                    };

                    process.Start();
                    process.StandardOutput.ReadToEnd();
                    process.StandardError.ReadToEnd();
                    if (!process.WaitForExit(120000))
                    {
                        try { process.Kill(); } catch { }
                        return null;
                    }
                    if (process.ExitCode != 0 || !File.Exists(outputPath)) return null;
                }

                byte[] dds = File.ReadAllBytes(outputPath);
                return ValidateDds(dds, expectedFourCc, mipCount) ? dds : null;
            }
            catch
            {
                return null;
            }
            finally
            {
                try
                {
                    if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
                }
                catch
                {
                    // A locked temporary file must not make texture import fail.
                }
            }
        }

        private static bool TryGetFormat(uint format, out string dxgiFormat, out string fourCc)
        {
            dxgiFormat = null;
            fourCc = null;
            switch (format)
            {
                case BC1: dxgiFormat = "BC1_UNORM"; fourCc = "DXT1"; return true;
                case BC2: dxgiFormat = "BC2_UNORM"; fourCc = "DXT3"; return true;
                case BC3: dxgiFormat = "BC3_UNORM"; fourCc = "DXT5"; return true;
                case BC4: dxgiFormat = "BC4_UNORM"; fourCc = "ATI1"; return true;
                case BC5: dxgiFormat = "BC5_UNORM"; fourCc = "ATI2"; return true;
                default: return false;
            }
        }

        private static bool ValidateDds(byte[] dds, string expectedFourCc, int requestedMipCount)
        {
            if (dds == null || dds.Length < 128) return false;
            if (dds[0] != (byte)'D' || dds[1] != (byte)'D'
                || dds[2] != (byte)'S' || dds[3] != (byte)' ') return false;

            int width = BitConverter.ToInt32(dds, 16);
            int height = BitConverter.ToInt32(dds, 12);
            int mipCount = BitConverter.ToInt32(dds, 28);
            string fourCc = System.Text.Encoding.ASCII.GetString(dds, 84, 4);
            return width > 0 && height > 0
                && mipCount == requestedMipCount
                && string.Equals(fourCc, expectedFourCc, StringComparison.Ordinal);
        }

        private static string Quote(string value)
        {
            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }
    }
}
