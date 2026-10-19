using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Xml;

namespace MinamotoBoard.Localization
{
    internal static class Localizer
    {
        public static readonly string[] Codes = { "en-US", "ja-JP", "zh-CN", "zh-TW" };
        public static readonly string[] Names = { "English", "日本語", "简体中文", "正體中文" };
        private static Dictionary<string, string> strings = new Dictionary<string, string>();
        public static string CurrentLanguage { get; private set; }
        public static event Action LanguageChanged;

        public static void Initialize(string language)
        {
            if (Array.IndexOf(Codes, language) < 0)
            {
                string system = CultureInfo.CurrentUICulture.Name;
                language = system.StartsWith("ja", StringComparison.OrdinalIgnoreCase) ? "ja-JP" :
                    system.StartsWith("zh", StringComparison.OrdinalIgnoreCase) ?
                    (system.IndexOf("TW", StringComparison.OrdinalIgnoreCase) >= 0 || system.IndexOf("HK", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     system.IndexOf("MO", StringComparison.OrdinalIgnoreCase) >= 0 || system.IndexOf("Hant", StringComparison.OrdinalIgnoreCase) >= 0 ? "zh-TW" : "zh-CN") : "en-US";
            }
            SetLanguage(language);
        }

        public static void SetLanguage(string language)
        {
            if (Array.IndexOf(Codes, language) < 0 || CurrentLanguage == language) return;
            Dictionary<string, string> next = new Dictionary<string, string>();
            using (Stream stream = typeof(Localizer).Assembly.GetManifestResourceStream("MinamotoBoard.Localization." + language + ".xml"))
            using (XmlReader reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null }))
            {
                while (reader.Read())
                    if (reader.NodeType == XmlNodeType.Element && reader.Name == "text")
                    {
                        string key = reader.GetAttribute("key");
                        string value = reader.ReadElementContentAsString();
                        next.Add(key, value);
                    }
            }
            strings = next;
            CurrentLanguage = language;
            CultureInfo culture = CultureInfo.GetCultureInfo(language);
            Thread.CurrentThread.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            Action changed = LanguageChanged;
            if (changed != null) changed();
        }

        public static string T(string key)
        {
            string value;
            return strings.TryGetValue(key, out value) ? value : key;
        }
        public static string Format(string key, params object[] args)
        { return string.Format(CultureInfo.GetCultureInfo(CurrentLanguage ?? "en-US"), T(key), args); }
    }
}
