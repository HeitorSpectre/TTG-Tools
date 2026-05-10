using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TTG_Tools.ClassesStructs.Text
{
    public struct Landb
    {
        public uint stringNumber; //Current string number
        public uint anmID; //Actor's animation
        public ulong crc64Langres; //Langres's CRC64
        public uint wavID; //Actor's speech
        public int zero1; //Some zero value

        //anm and wav file names
        public int blockAnmNameSize;
        public int anmNameSize;
        public string anmName;
        public int blockWavNameSize;
        public int wavNameSize;
        public string wavName;

        public int blockUnknownNameSize; //I don't know what does it mean
        public int unknownNameSize;
        public string unknownName;

        public int zero2; //Another some zero value

        public int blockLangresSize;
        
        public int blockActorNameSize;
        public int actorNameSize;
        public string actorName;

        public int blockActorSpeechSize;
        public int actorSpeechSize;
        public string actorSpeech;

        public int blockSize;
        public int someValue;

        //For games since Tales from the Borderlands
        public int blockSizeUni;
        public byte[] someDataUni;

        public int flags;
    }

    public struct LastLandbData //16 bytes of some data
    {
        public int Unknown1;
        public int Unknown2;
        public int Unknown3;
        public int Unknown4;
    }

    public class LandbClass
    {
        public int landbFileSize; //For 5VSM and 6VSM formats
        public int newLandbFileSize; //Recount file sizes
        public int landbLastFileSize; //Last block after list landb
        public int newLandbLastFileSize; //Recount last block after list landb

        //public bool isBlockLength; Maybe I'll remove that
        public int blockSize1;
        public int someValue1;
        public int blockSize2;
        public int someValue2;

        public int blockLength;
        public int newBlockLength; //for recount
        public int landbCount;

        public bool isNewFormat;
        public bool hasMetaLangresName; //uses since The Wolf among us
        public bool isUnicode; //Maybe. I named it for a correctly UTF-8 support

        public Landb[] landbs;
        public FlagsClass.LangdbFlagClass[] flags;
        public int commonSomeDataLen;
        public byte[] someData;
        public LastLandbData lastLandbData;
        public byte[] lastNewBlockData; //For games since The Wolf among us
    }
}
