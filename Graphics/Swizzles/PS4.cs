/***************************************************************
 * Special thanks to daemon1 and tge for PS4 swizzle algorithm *
 * Fixed destination array size bug for imports                *
 ***************************************************************/

using System;

namespace TTG_Tools.Graphics.Swizzles
{
    public class PS4
    {
        public static byte[] Swizzle(byte[] data, int width, int height, int blockSize)
        {
            return DoSwizzle(data, width, height, blockSize, false);
        }

        public static byte[] Unswizzle(byte[] data, int width, int height, int blockSize)
        {
            return DoSwizzle(data, width, height, blockSize, true);
        }

        private static byte[] DoSwizzle(byte[] data, int width, int height, int blockSize, bool unswizzle)
        {
            // 1. Calcula as dimensões em "Texels" (Blocos de compressão 4x4 pixels)
            var heightTexels = height / 4;
            var widthTexels = width / 4;

            // 2. Calcula o alinhamento necessário para o PS4 (Blocos de 8x8 Texels = 32x32 Pixels)
            var heightTexelsAligned = (heightTexels + 7) / 8;
            var widthTexelsAligned = (widthTexels + 7) / 8;

            // 3. Calcula o tamanho total necessário (incluindo o Padding do PS4)
            int alignedSize = heightTexelsAligned * widthTexelsAligned * 64 * blockSize;

            byte[] processed;

            // CORREÇÃO AQUI:
            // Se estivermos fazendo Swizzle (Importando: Linear -> PS4), o destino deve ter o tamanho alinhado (maior).
            // Se estivermos fazendo Unswizzle (Exportando: PS4 -> Linear), usamos o tamanho original ou calculado.
            if (!unswizzle)
            {
                // Garante que o array de destino seja grande o suficiente para conter o padding
                int targetSize = (alignedSize > data.Length) ? alignedSize : data.Length;
                processed = new byte[targetSize];
            }
            else
            {
                processed = new byte[data.Length];
            }

            var dataIndex = 0;

            for (int y = 0; y < heightTexelsAligned; ++y)
            {
                for (int x = 0; x < widthTexelsAligned; ++x)
                {
                    for (int t = 0; t < 64; ++t)
                    {
                        int pixelIndex = SwizzleUtilities.Morton(t, 8, 8);
                        int num8 = pixelIndex / 8;
                        int num9 = pixelIndex % 8;
                        var yOffset = (y * 8) + num8;
                        var xOffset = (x * 8) + num9;

                        // Verifica se estamos dentro da área válida da imagem (não no padding)
                        if ((xOffset < widthTexels) && (yOffset < heightTexels))
                        {
                            var destPixelIndex = yOffset * widthTexels + xOffset;
                            int destIndex = blockSize * destPixelIndex;

                            // Verificações de segurança adicionais para evitar crash se o arquivo DDS estiver corrompido ou incompleto
                            if (unswizzle)
                            {
                                // PS4 (data) -> Linear (processed)
                                if (dataIndex + blockSize <= data.Length && destIndex + blockSize <= processed.Length)
                                {
                                    Array.Copy(data, dataIndex, processed, destIndex, blockSize);
                                }
                            }
                            else
                            {
                                // Linear (data) -> PS4 (processed)
                                if (destIndex + blockSize <= data.Length && dataIndex + blockSize <= processed.Length)
                                {
                                    Array.Copy(data, destIndex, processed, dataIndex, blockSize);
                                }
                            }
                        }

                        // O índice do PS4 avança sempre, mesmo que seja área de padding
                        dataIndex += blockSize;
                    }
                }
            }

            return processed;
        }
    }
}