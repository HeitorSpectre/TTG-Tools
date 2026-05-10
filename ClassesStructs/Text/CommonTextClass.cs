using System.Collections.Generic;

namespace TTG_Tools.ClassesStructs.Text
{
    public struct CommonText
    { 
        public bool isBothSpeeches; //have txt file both original and translation or not?
        public uint strNumber;
        public string actorName;
        public string actorSpeechOriginal;
        public string actorSpeechTranslation;
        public string flags;
    }

    public class CommonTextClass
    {
        public List<CommonText> txtList;
    }
}
