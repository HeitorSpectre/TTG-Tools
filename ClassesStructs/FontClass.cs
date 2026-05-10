namespace TTG_Tools.ClassesStructs
{
    public class FontClass
    {
            public class ClassFont
            {
                //For games since The Wolf Among Us
                public int headerSize; //Calculation of coordinates and texture headers
                public uint texSize; //Size of textures

                public string[] elements;
                public byte[][] binElements;

                //Before Poker Night 2 game
                public class TRect
                {
                    public int TexNum; //CurrentTexture
                    public float XStart;
                    public float XEnd;
                    public float YStart;
                    public float YEnd;
                    public float CharWidth;
                    public float CharHeight;

                    public TRect() { }
                }

                //Since Poker Night 2 game
                public class TRectNew
                {
                    public uint charId;
                    public int TexNum;
                    public int Channel;
                    public float XStart;
                    public float XEnd;
                    public float YStart;
                    public float YEnd;
                    public float CharWidth;
                    public float CharHeight;
                    public float XOffset;
                    public float YOffset;
                    public float XAdvance;

                    public TRectNew() { }
                }

                public struct GlyphInfo
                {
                    public int BlockCoordSize; //Size of block class TRect
                    public int CharCount; //Count characters (before Poker Night 2 default was 256)
                    public TRect[] chars; //Table of characters
                    public TRectNew[] charsNew; //For Poker Night 2 and newer games
                }

                public bool blockSize;
                public bool hasScaleValue; //Use since Hector games
                public bool NewFormat;
                public bool hasOneFloatValue; //Stupid fix for newest Telltale's games
                public bool hasLineHeight;

                public string FontName;
                public float halfValue; //Shows in some fonts
                public float oneValue; //Scale font. Use since Hector and newer games
                public byte One;
                public float NewSomeValue; //Since Game of Thrones if 0x31 byte
                public byte[] feedFace; //some data from WD: Michonne
                public float BaseSize; //Common char size in text line
                public float lineHeight; //For Back to the Future for consoles PS4 and Xbox360
                public int BlockTexSize; //Size of block Textures
                public int TexCount; //Count textures
                public byte LastZero; //Something zero value byte after textures header

                public GlyphInfo glyph;
                public TextureClass.OldT3Texture[] tex;
                public TextureClass.NewT3Texture[] NewTex;

                public ClassFont() { }
            }
    }
}
