namespace TTG_Tools.ClassesStructs
{
    //I DON'T KNOW ABOUT FLAGS BUT I HAVE TO DO WITH THAT!
    //In some files I saw class flags and it does something in other classes.
    public class FlagsClass
    {
        //flags for Textures
        public int Unknown1; //Before 0x30 byte

        //after 0x30 byte
        public int Unknown2;
        public int TextureCount; //Strong Bad has value 3 and after DDS shows some texture, in another files it's just one
        public int[] TexSizes; //Sizes another textures
        public byte[][] SubTexContent; //From Strong Bad

        public class SomeClassData //For Walking Dead The New Frontier
        {
            public int count;
            public int Unknown1;
            public int len;
            public byte[] someData;
            public SomeClassData() { }
        }

        public class LangdbFlagClass
        {
            public byte[] flags;

            public LangdbFlagClass() { }
        }
    }
}
