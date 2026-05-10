using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace TTG_Tools.ClassesStructs.Text
{
    //For Sam and Max season 3

    public struct LangresDB
    {
        public uint stringNumber;
        public int blockActorNameSize;
        public int actorNameSize;
        public string actorName;

        public int blockActorSpeechSize;
        public int actorSpeechSize;
        public string actorSpeech;

        public int someValue1;
        public int someValue2;
    }

    public struct DlogLandbs
    {
        public uint anmID;
        public uint wavID;
        public int zero;

        public int blockAnmNameSize;
        public int anmNameSize;
        public string anmName;

        public int blockWavNameSize;
        public int wavNameSize;
        public string wavName;

        public int blockLangresSize;
        public int langresStrsCount; //count of langres strings

        public LangresDB[] lang;

        public byte[] flags; //Will be 000 for a compatible

        public int someValue3;
        public int someValue4;
    }

    public class DlogLandb
    {
        public int blockSize1;
        public int someValue1;
        public int blockSize2;
        public int someValue2;

        public int blockLength;
        public int newBlockLength;
        public int landbCount;

        public DlogLandbs[] landbs;

        public int commonBlockLen;
        public byte[] block;

        public DlogLastLandbData lastLandbData;
    }

    public struct DlogLastLandbData //16 bytes of some data
    {
        public int Unknown1;
        public int Unknown2;
        public int BlockLen;
        public byte[] Block;
    }

    public class DlogClass
    {
        public int unknown1;
        public int unknown2;
        public ulong longUnknown1;
        public int zero;
        public int blockSize; //blockSize - 4
        public byte[] block;
        public int blockFileNameSize;
        public int fileNameSize;
        public string dlogFileName;
        public int someValue1;
        public int someValue2;
        public ulong longUnknown2;
        public int someValue3;
        public int langdbBlockSize;
        public int newLangdbBlockSize;

        public DlogLandb landb;

        public byte[] someDlogData; //I don't want research rest of data
    }
}
