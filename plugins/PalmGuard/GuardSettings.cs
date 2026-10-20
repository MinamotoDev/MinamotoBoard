using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace MinamotoBoard.Plugins.PalmGuard
{
    public sealed class GuardSettings
    {
        public bool Enabled = true;
        public bool PauseOnMultiple = true;
        public bool ShieldEnabled;
        public double X = 0.65, Y = 0.58, Width = 0.27, Height = 0.28;

        private static string FilePath
        {
            get { return Path.Combine(Path.GetDirectoryName(typeof(GuardSettings).Assembly.Location), "PalmGuard.settings.xml"); }
        }
        public static GuardSettings Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return new GuardSettings();
                using (XmlReader reader = XmlReader.Create(FilePath,
                    new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null }))
                {
                    GuardSettings settings = (GuardSettings)new XmlSerializer(typeof(GuardSettings)).Deserialize(reader);
                    settings.Normalize(); return settings;
                }
            }
            catch (Exception ex) { Trace.WriteLine(ex); return new GuardSettings(); }
        }
        public void Normalize()
        {
            Width = Clamp(Width, 0.06, 0.95, 0.27); Height = Clamp(Height, 0.06, 0.95, 0.28);
            X = Clamp(X, 0, 1 - Width, 0.65); Y = Clamp(Y, 0, 1 - Height, 0.58);
        }
        private static double Clamp(double value, double min, double max, double fallback)
        { return double.IsNaN(value) || double.IsInfinity(value) ? Math.Min(max, fallback) : Math.Max(min, Math.Min(max, value)); }
        public void Save()
        {
            Normalize();
            string path = FilePath;
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (XmlWriter writer = XmlWriter.Create(temporary,
                    new XmlWriterSettings { Encoding = new UTF8Encoding(false), Indent = true }))
                    new XmlSerializer(typeof(GuardSettings)).Serialize(writer, this);
                if (File.Exists(path)) File.Replace(temporary, path, null); else File.Move(temporary, path);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
    }
}
