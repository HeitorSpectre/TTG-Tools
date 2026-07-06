using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TTG_Tools
{
    /// <summary>
    /// Lightweight, dependency-free localization engine for TTG Tools.
    ///
    /// The whole UI is in English in the source (Designer text and inline
    /// <c>englishDefault</c> arguments). This engine loads plain-text
    /// translation files (see <c>Languages/*.lang</c>) and, when a translated
    /// key exists, swaps the English text for the localized one. If a key is
    /// missing in the active language it falls back to English, so partial
    /// community translations never break the interface.
    ///
    /// Translation files use a simple "key = value" format (INI-like), so any
    /// contributor can add a new language by dropping a new .lang file into the
    /// Languages folder next to the executable. No recompilation needed.
    /// </summary>
    internal static class Localization
    {
        /// <summary>Describes one available language file (used to fill the settings dropdown).</summary>
        public sealed class LanguageInfo
        {
            public string Code;        // e.g. "en", "pt-BR", "es"
            public string DisplayName; // e.g. "English", "Português do Brasil"
            public string FilePath;

            //So a ComboBox shows the language name even if DisplayMember isn't honored.
            public override string ToString()
            {
                return DisplayName;
            }
        }

        //Reserved metadata keys that live in the file header, not real UI strings.
        private const string KeyLanguageName = "LanguageName";
        private const string KeyLanguageCode = "LanguageCode";
        private const string KeyAuthor = "Author";

        private static readonly Dictionary<string, string> _active =
            new Dictionary<string, string>(StringComparer.Ordinal);

        private static readonly List<LanguageInfo> _available = new List<LanguageInfo>();

        public static string ActiveLanguageCode { get; private set; }

        /// <summary>All language files found in the Languages folder (English first).</summary>
        public static List<LanguageInfo> AvailableLanguages
        {
            get { return _available; }
        }

        /// <summary>The folder shipped next to the executable that holds the .lang files.</summary>
        public static string LanguagesDirectory
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Languages"); }
        }

        /// <summary>
        /// Scans the Languages folder and loads the dictionary for <paramref name="languageCode"/>.
        /// Call once at startup, before any form is constructed. English ("en") is treated as the
        /// implicit baseline: selecting it (or an unknown/empty code) simply uses the in-source text.
        /// Never throws.
        /// </summary>
        public static void Init(string languageCode)
        {
            try
            {
                ScanAvailableLanguages();
            }
            catch { /* localization is optional; never block startup */ }

            LoadLanguage(languageCode);
        }

        /// <summary>Loads (or reloads) the active dictionary for the given code.</summary>
        public static void LoadLanguage(string languageCode)
        {
            _active.Clear();
            ActiveLanguageCode = string.IsNullOrEmpty(languageCode) ? "en" : languageCode;

            //English is the source of truth already baked into the code, so there is
            //nothing to load for it (and an en.lang, if present, would be identical).
            if (string.Equals(ActiveLanguageCode, "en", StringComparison.OrdinalIgnoreCase))
                return;

            LanguageInfo info = _available.Find(l =>
                string.Equals(l.Code, ActiveLanguageCode, StringComparison.OrdinalIgnoreCase));

            if (info == null || !File.Exists(info.FilePath))
                return;

            try
            {
                ParseFile(info.FilePath, _active, null);
            }
            catch
            {
                //A malformed file must not crash the app; fall back to English.
                _active.Clear();
            }
        }

        /// <summary>
        /// Returns the translated text for <paramref name="key"/> in the active language,
        /// or <paramref name="englishDefault"/> when there is no translation (or the key itself
        /// if no default was supplied). Never throws.
        /// </summary>
        public static string T(string key, string englishDefault = null)
        {
            if (!string.IsNullOrEmpty(key))
            {
                string value;
                if (_active.TryGetValue(key, out value))
                    return value;
            }

            return englishDefault ?? key;
        }

        /// <summary>
        /// Tries to get a translation for <paramref name="key"/>. Used by <see cref="Localizer"/>
        /// so Designer text is only replaced when an actual translation exists (otherwise the
        /// original English text on the control is kept untouched).
        /// </summary>
        public static bool TryGet(string key, out string value)
        {
            if (!string.IsNullOrEmpty(key) && _active.TryGetValue(key, out value))
                return true;

            value = null;
            return false;
        }

        #region File scanning / parsing
        private static void ScanAvailableLanguages()
        {
            _available.Clear();

            //English is always offered even without a file (it's the in-source baseline).
            _available.Add(new LanguageInfo { Code = "en", DisplayName = "English", FilePath = null });

            string dir = LanguagesDirectory;
            if (!Directory.Exists(dir))
                return;

            foreach (string file in Directory.GetFiles(dir, "*.lang"))
            {
                var header = new Dictionary<string, string>(StringComparer.Ordinal);
                try
                {
                    ParseFile(file, null, header);
                }
                catch { continue; }

                string name, code;
                header.TryGetValue(KeyLanguageName, out name);
                header.TryGetValue(KeyLanguageCode, out code);

                if (string.IsNullOrEmpty(code))
                    code = Path.GetFileNameWithoutExtension(file);
                if (string.IsNullOrEmpty(name))
                    name = code;

                //An en.lang overrides the built-in English entry; otherwise add/replace by code.
                LanguageInfo existing = _available.Find(l =>
                    string.Equals(l.Code, code, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    existing.DisplayName = name;
                    existing.FilePath = file;
                }
                else
                {
                    _available.Add(new LanguageInfo { Code = code, DisplayName = name, FilePath = file });
                }
            }
        }

        /// <summary>
        /// Parses a .lang file. When <paramref name="strings"/> is provided, real UI keys go there;
        /// when <paramref name="header"/> is provided, metadata (LanguageName/Code/Author) goes there.
        /// Either may be null. Lines starting with '#' or ';' are comments; blank lines are ignored.
        /// The first '=' splits key and value; values support \n \r \t \\ escapes.
        /// </summary>
        private static void ParseFile(string path, Dictionary<string, string> strings, Dictionary<string, string> header)
        {
            foreach (string rawLine in File.ReadAllLines(path, Encoding.UTF8))
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line[0] == '#' || line[0] == ';')
                    continue;

                int eq = line.IndexOf('=');
                if (eq <= 0)
                    continue;

                string key = line.Substring(0, eq).Trim();
                //'line' is already trimmed, so real trailing spaces can't survive here (and text
                //editors/git strip them anyway). Some strings are concatenated with data
                //(e.g. "File " + name), so a trailing space is represented explicitly with the
                //\s escape (handled in Unescape).
                string value = Unescape(line.Substring(eq + 1).TrimStart());

                if (key.Length == 0)
                    continue;

                if (key == KeyLanguageName || key == KeyLanguageCode || key == KeyAuthor)
                {
                    if (header != null) header[key] = value;
                }
                else if (strings != null)
                {
                    strings[key] = value;
                }
            }
        }

        private static string Unescape(string s)
        {
            if (s.IndexOf('\\') < 0)
                return s;

            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '\\' && i + 1 < s.Length)
                {
                    char next = s[++i];
                    switch (next)
                    {
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 's': sb.Append(' '); break; // explicit (usually trailing) space
                        case '\\': sb.Append('\\'); break;
                        default: sb.Append('\\').Append(next); break;
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }
        #endregion
    }

    /// <summary>Short alias so call sites read as <c>Loc.T("key", "English")</c>.</summary>
    internal static class Loc
    {
        public static string T(string key, string englishDefault = null)
        {
            return Localization.T(key, englishDefault);
        }
    }
}
