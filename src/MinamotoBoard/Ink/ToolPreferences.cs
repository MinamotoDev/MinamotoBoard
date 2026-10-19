using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace MinamotoBoard.Ink
{
    public sealed class InstrumentState
    {
        public BoardTool Tool;
        public bool Positioned;
        public double X, Y, Angle;
        public double Length = 640, Scale = 1, Radius = 110;
        public bool Triangle30;
    }

    public sealed class ToolPreference
    {
        public BoardTool Tool;
        public string Color;
        public double Width;
        public int Style;
    }

    public sealed class ToolPreferences
    {
        public BoardTool Selected;
        public string Language;
        public double WindowWidth = 1280, WindowHeight = 720;
        public List<ToolPreference> Tools = new List<ToolPreference>();
        public List<InstrumentState> Instruments = new List<InstrumentState>();
        private static string FilePath
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "MinamotoDev", "Board", "settings.xml"); }
        }
        public static ToolPreferences Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return new ToolPreferences();
                using (XmlReader reader = XmlReader.Create(FilePath, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null }))
                    return (ToolPreferences)new XmlSerializer(typeof(ToolPreferences)).Deserialize(reader);
            }
            catch (Exception ex) { Trace.WriteLine(ex); return new ToolPreferences(); }
        }
        public void Save()
        {
            try
            {
                string path = FilePath, temporary = path + ".tmp";
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                using (FileStream file = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                using (XmlWriter writer = XmlWriter.Create(file, new XmlWriterSettings { Encoding = new UTF8Encoding(false), Indent = true }))
                    new XmlSerializer(typeof(ToolPreferences)).Serialize(writer, this);
                if (File.Exists(path)) File.Replace(temporary, path, null); else File.Move(temporary, path);
            }
            catch (Exception ex) { Trace.WriteLine(ex); }
        }
        internal static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
    }
}
