using System;
using System.Xml.Serialization;

namespace TTG_Tools
{
    [Serializable()]
    public class Settings
    {
        public static void SaveConfig(Settings settings)
        {
            string xmlPath = System.AppDomain.CurrentDomain.BaseDirectory + "config.xml";
            XmlSerializer xmlS = new XmlSerializer(typeof(Settings));
            System.IO.TextWriter xmlW = new System.IO.StreamWriter(xmlPath);
            xmlS.Serialize(xmlW, settings);

            xmlW.Flush();
            xmlW.Close();
        }

        private string _pathForInputFolder;
        private string _pathForOutputFolder;
        private int _ASCII_N;
        private bool _deleteD3DTXafterImport;
        private bool _deleteDDSafterImport;
        private bool _importingOfName;
        private bool _sortSameString;
        private bool _exportRealID;
        private int _unicodeSettings;
        private bool _clearMessages; //For Auto (De)Packer
        private bool _ignoreEmptyStrings;

        private bool _encLangdb;
        private bool _encDDSonly;
        private bool _encNewLua;
        private bool _iOSsupport; //for PVR textures
        private bool _customKey;
        private bool _tsvFormat;
        private int _encKeyIndex;
        private int _versionEnc;
        private string _encCustomKey;

        private string _inputDirPath; //For Archive Packer
        private string _archivePath;
        private bool _encryptLuaInArchive;
        private bool _compressArchive;
        private bool _oldXmode;
        private bool _encArchive;
        private int _archiveFormat;
        private int _versionArchiveIndex;
        private bool _swizzleNintendoSwitch;
        private bool _newTxtFormat;
        private bool _changeLangFlags;
        private bool _swizzlePS4;
        private bool _swizzleXbox360;
        private bool _swizzlePSVita;
        private bool _supportTwdNintendoSwitch;

        private int _languageIndex;

        [XmlAttribute("pathForInputFolder")]
        public string pathForInputFolder
        {
            get
            {
                return _pathForInputFolder;
            }
            set
            {
                _pathForInputFolder = value;
            }
        }

        [XmlAttribute("pathForOutputFolder")]
        public string pathForOutputFolder
        {
            get
            {
                return _pathForOutputFolder;
            }
            set
            {
                _pathForOutputFolder = value;
            }
        }

        [XmlAttribute("clearMessages")]
        public bool clearMessages
        {
            get
            {
                return _clearMessages;
            }
            set
            {
                _clearMessages = value;
            }
        }
        
        [XmlAttribute("ASCII_N")]
        public int ASCII_N
        {
            get
            {
                return _ASCII_N;
            }
            set
            {
                _ASCII_N = value;
            }
        }
        
        [XmlAttribute("deleteD3DTXafterImport")]
        public bool deleteD3DTXafterImport
        {
            get
            {
                return _deleteD3DTXafterImport;
            }
            set
            {
                _deleteD3DTXafterImport = value;
            }
        }

        [XmlAttribute("deleteDDSafterImport")]
        public bool deleteDDSafterImport
        {
            get
            {
                return _deleteDDSafterImport;
            }
            set
            {
                _deleteDDSafterImport = value;
            }
        }

        [XmlAttribute("importingOfName")]
        public bool importingOfName
        {
            get
            {
                return _importingOfName;
            }
            set
            {
                _importingOfName = value;
            }
        }

        [XmlAttribute("sortSameString")]
        public bool sortSameString
        {
            get
            {
                return _sortSameString;
            }
            set
            {
                _sortSameString = value;
            }
        }

        [XmlAttribute("exportRealID")]
        public bool exportRealID
        {
            get
            {
                return _exportRealID;
            }
            set
            {
                _exportRealID = value;
            }
        }

        [XmlAttribute("unicodeMode")]
        public int unicodeSettings
        {
            get
            {
                return _unicodeSettings;
            }
            set
            {
                _unicodeSettings = value;
            }
        }

        [XmlAttribute("encLangdb")]
        public bool encLangdb
        {
            get
            {
                return _encLangdb;
            }
            set
            {
                _encLangdb = value;
            }
        }

        [XmlAttribute("encDDSonly")]
        public bool encDDSonly
        {
            get
            {
                return _encDDSonly;
            }
            set
            {
                _encDDSonly = value;
            }
        }

        [XmlAttribute("encNewLua")]
        public bool encNewLua
        {
            get
            {
                return _encNewLua;
            }
            set
            {
                _encNewLua = value;
            }
        }

        [XmlAttribute("iOSsupport")]
        public bool iOSsupport
        {
            get
            {
                return _iOSsupport;
            }
            set
            {
                _iOSsupport = value;
            }
        }

        [XmlAttribute("customKey")]
        public bool customKey
        {
            get
            {
                return _customKey;
            }
            set
            {
                _customKey = value;
            }
        }
        
        [XmlAttribute("tsvFormat")]
        public bool tsvFormat
        {
            get
            {
                return _tsvFormat;
            }
            set
            {
                _tsvFormat = value;
            }
        }

        [XmlAttribute("encKeyIndex")]
        public int encKeyIndex
        {
            get
            {
                return _encKeyIndex;
            }
            set
            {
                _encKeyIndex = value;
            }
        }

        [XmlAttribute("versionEnc")]
        public int versionEnc
        {
            get
            {
                return _versionEnc;
            }
            set
            {
                _versionEnc = value;
            }
        }

        [XmlAttribute("encCustomKey")]
        public string encCustomKey
        {
            get
            {
                return _encCustomKey;
            }
            set
            {
                _encCustomKey = value;
            }
        }

        [XmlAttribute("inputDirPath")] //For Archive Packer
        public string inputDirPath
        {
            get
            {
                return _inputDirPath;
            }
            set
            {
                _inputDirPath = value;
            }
        }

        [XmlAttribute("archivePath")]
        public string archivePath
        {
            get
            {
                return _archivePath;
            }
            set
            {
                _archivePath = value;
            }
        }

        [XmlAttribute("encArchive")]
        public bool encArchive
        {
            get
            {
                return _encArchive;
            }
            set
            {
                _encArchive = value;
            }
        }

        [XmlAttribute("encryptLuaInArchive")] //Need for mobile versions
        public bool encryptLuaInArchive
        {
            get
            {
                return _encryptLuaInArchive;
            }
            set
            {
                _encryptLuaInArchive = value;
            }
        }

        [XmlAttribute("compressArchive")]
        public bool compressArchive
        {
            get
            {
                return _compressArchive;
            }
            set
            {
                _compressArchive = value;
            }
        }

        [XmlAttribute("oldXmode")] //For very old Telltale games
        public bool oldXmode
        {
            get
            {
                return _oldXmode;
            }
            set
            {
                _oldXmode = value;
            }
        }

        [XmlAttribute("archiveFormat")] //TTARCH (0) or TTARCH2 (1)
        public int archiveFormat
        {
            get
            {
                return _archiveFormat;
            }
            set
            {
                _archiveFormat = value;
            }
        }

        [XmlAttribute("versionArchiveIndex")]
        public int versionArchiveIndex
        {
            get
            {
                return _versionArchiveIndex;
            }
            set
            {
                _versionArchiveIndex = value;
            }
        }

        [XmlAttribute("swizzleNintendoSwitch")]

        public bool swizzleNintendoSwitch
        {
            get
            {
                return _swizzleNintendoSwitch;
            }
            set
            {
                _swizzleNintendoSwitch = value;
            }
        }

        [XmlAttribute("newTxtFormat")]
        public bool newTxtFormat
        {
            get
            {
                return _newTxtFormat;
            }
            set
            {
                _newTxtFormat = value;
            }
        }

        [XmlAttribute("changeLangFlags")]
        public bool changeLangFlags
        {
            get 
            {
                return _changeLangFlags;
            }
            set 
            {
                _changeLangFlags = value;
            }
        }

        [XmlAttribute("ignoreEmptyStrings")]
        public bool ignoreEmptyStrings
        {
            get
            {
                return _ignoreEmptyStrings;
            }
            set
            {
                _ignoreEmptyStrings = value;
            }
        }

        [XmlAttribute("swizzlePS4")]
        public bool swizzlePS4
        {
            get
            {
                return _swizzlePS4;
            }
            set
            {
                _swizzlePS4 = value;
            }
        }

        [XmlAttribute("swizzleXbox360")]
        public bool swizzleXbox360
        {
            get
            {
                return _swizzleXbox360;
            }
            set
            {
                _swizzleXbox360 = value;
            }
        }

        [XmlAttribute("swizzlePSVita")]
        public bool swizzlePSVita
        {
            get
            {
                return _swizzlePSVita;
            }
            set
            {
                _swizzlePSVita = value;
            }
        }

        [XmlAttribute("ASCIILanguageIndex")]
        public int languageIndex
        {
            get
            {
                return _languageIndex;
            }
            set
            {
                _languageIndex = value;
            }
        }

        [XmlAttribute("supportTwdNintendoSwitch")]
        public bool supportTwdNintendoSwitch
        {
            get
            {
                return _supportTwdNintendoSwitch;
            }
            set
            {
                _supportTwdNintendoSwitch = value;
            }
        }

        public Settings(
            string _pathForInputFolder,
            string _pathForOutputFolder,
            int _ASCII_N,
            bool _deleteD3DTXafterImport,
            bool _deleteDDSafterImport,
            bool _importingOfName,
            bool _sortSameString,
            bool _exportRealID,
            int _unicodeSettings,
            bool _encLangdb,
            bool _encDDSonly,
            bool _encNewLua,
            bool _iOSsupport,
            bool _customKey,
            bool _tsvFormat,
            int _encKeyIndex,
            int _versionEnc,
            string _encCustomKey,
            string _inputDirPath,
            string _archivePath,
            bool _encArchive,
            bool _encryptLuaInArchive,
            bool _compressArchive,
            bool _oldXmode,
            int _archiveFormat,
            int _versionArchiveIndex,
            bool _swizzleNintendoSwitch,
            bool _clearMessages,
            bool _newTxtFormat, 
            bool _changeLangFlags,
            bool _ignoreEmptyStrings,
            bool _swizzlePS4,
            bool _swizzleXbox360,
            bool _swizzlePSVita,
            int _languageIndex,
            bool _supportTwdNintendoSwitch)
        {
            this.ASCII_N = _ASCII_N;
            this.pathForInputFolder = _pathForInputFolder;
            this.pathForOutputFolder = _pathForOutputFolder;
            this.deleteD3DTXafterImport = _deleteD3DTXafterImport;
            this.deleteDDSafterImport = _deleteDDSafterImport;
            this.importingOfName = _importingOfName;
            this.sortSameString = _sortSameString;
            this.exportRealID = _exportRealID;
            this.unicodeSettings = _unicodeSettings;
            this.encLangdb = _encLangdb;
            this.encDDSonly = _encDDSonly;
            this.encNewLua = _encNewLua;
            this.iOSsupport = _iOSsupport;
            this.customKey = _customKey;
            this.tsvFormat = _tsvFormat;
            this.encKeyIndex = _encKeyIndex;
            this.versionEnc = _versionEnc;
            this.encCustomKey = _encCustomKey;
            this.inputDirPath = _inputDirPath;
            this.archivePath = _archivePath;
            this.encArchive = _encArchive;
            this.encryptLuaInArchive = _encryptLuaInArchive;
            this.compressArchive = _compressArchive;
            this.oldXmode = _oldXmode;
            this.archiveFormat = _archiveFormat;
            this.versionArchiveIndex = _versionArchiveIndex;
            this.swizzleNintendoSwitch = _swizzleNintendoSwitch;
            this.clearMessages = _clearMessages;
            this.newTxtFormat = _newTxtFormat;
            this.changeLangFlags = _changeLangFlags;
            this.ignoreEmptyStrings = _ignoreEmptyStrings;
            this.swizzlePS4 = _swizzlePS4;
            this.swizzleXbox360 = _swizzleXbox360;
            this.swizzlePSVita = _swizzlePSVita;
            this.languageIndex = _languageIndex;
            this.supportTwdNintendoSwitch = _supportTwdNintendoSwitch;
        }

        public Settings()
        { }
    }
}
