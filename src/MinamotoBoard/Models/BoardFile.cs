using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Windows.Ink;
using System.Windows.Media;
using System.Xml;
using System.Xml.Linq;
using MinamotoBoard.Ink;

namespace MinamotoBoard.Models
{
    // Versioned ZIP container: native ISF plus explicit metadata for our custom stroke types.
    // No executable XAML or CLR type deserialization is accepted from document files.
    internal static class BoardFile
    {
        private const long MaximumEntryBytes = 128L * 1024 * 1024;
        private const long MaximumDocumentBytes = 512L * 1024 * 1024;

        public static void Save(BoardDocument document, string destination, bool overwrite = true)
        {
            string folder = Path.GetDirectoryName(destination);
            Directory.CreateDirectory(folder);
            string temporary = Path.Combine(folder, "." + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                using (FileStream file = new FileStream(temporary, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
                {
                    using (ZipArchive archive = new ZipArchive(file, ZipArchiveMode.Create, true))
                    {
                        XElement root = new XElement("MinamotoBoard", new XAttribute("version", 1),
                            new XAttribute("currentPage", document.CurrentIndex));
                        int pageIndex = 0;
                        foreach (BoardPage page in document.Pages)
                        {
                            string entryName = "pages/" + pageIndex++ + ".isf";
                            XElement node = new XElement("Page", new XAttribute("ink", entryName), SaveBackground(page.Background));
                            using (Stream stream = archive.CreateEntry(entryName, CompressionLevel.Optimal).Open()) page.Strokes.Save(stream);
                            for (int i = 0; i < page.Strokes.Count; i++)
                            {
                                Stroke stroke = page.Strokes[i];
                                RegionFillStroke fill = stroke as RegionFillStroke;
                                LiveHighlighterStroke live = stroke as LiveHighlighterStroke;
                                if (fill != null)
                                {
                                    PathGeometry geometry = PathGeometry.CreateFromGeometry(fill.Region);
                                    node.Add(new XElement("Fill", new XAttribute("index", i),
                                        new XAttribute("color", fill.DrawingAttributes.Color.ToString()),
                                        new XAttribute("matrix", geometry.Transform.Value.ToString(CultureInfo.InvariantCulture)),
                                        geometry.ToString(CultureInfo.InvariantCulture)));
                                }
                                else if (live != null)
                                    node.Add(new XElement("Live", new XAttribute("index", i),
                                        new XAttribute("dashed", live.IsDashed), new XAttribute("color", live.DrawingAttributes.Color.ToString())));
                                else if (stroke.GetType() != typeof(Stroke))
                                    throw new NotSupportedException("Unsupported plugin stroke: " + stroke.GetType().Name);
                            }
                            root.Add(node);
                        }
                        using (Stream stream = archive.CreateEntry("document.xml", CompressionLevel.Optimal).Open())
                        using (XmlWriter writer = XmlWriter.Create(stream, new XmlWriterSettings { Encoding = new UTF8Encoding(false) }))
                            root.Save(writer);
                    }
                    // Do not publish a document that exceeds the reader's supported limits.
                    file.Position = 0;
                    using (ZipArchive check = new ZipArchive(file, ZipArchiveMode.Read, true))
                        if (check.Entries.Any(entry => entry.Length > MaximumEntryBytes) ||
                            check.Entries.Sum(entry => entry.Length) > MaximumDocumentBytes)
                            throw new IOException("Document is too large.");
                    file.Flush(true);
                }
                if (overwrite && File.Exists(destination)) File.Replace(temporary, destination, null);
                else File.Move(temporary, destination);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }

        public static BoardDocument Load(string path)
        {
            using (FileStream file = File.OpenRead(path))
            using (ZipArchive archive = new ZipArchive(file, ZipArchiveMode.Read))
            {
                if (archive.Entries.Count > BoardDocument.MaximumPages + 1 ||
                    archive.Entries.Sum(entry => entry.Length) > MaximumDocumentBytes) throw new InvalidDataException("Document is too large.");
                XElement root;
                using (MemoryStream data = ReadEntry(archive, "document.xml"))
                using (XmlReader reader = XmlReader.Create(data, new XmlReaderSettings {
                    DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = MaximumEntryBytes }))
                    root = XElement.Load(reader);
                if (root.Name != "MinamotoBoard" || (int?)root.Attribute("version") != 1) throw new InvalidDataException("Unsupported document version.");
                List<BoardPage> pages = new List<BoardPage>();
                foreach (XElement node in root.Elements("Page"))
                {
                    if (pages.Count >= BoardDocument.MaximumPages) throw new InvalidDataException("Too many pages.");
                    StrokeCollection strokes;
                    using (MemoryStream ink = ReadEntry(archive, (string)node.Attribute("ink"))) strokes = new StrokeCollection(ink);
                    HashSet<int> restored = new HashSet<int>();
                    foreach (XElement item in node.Elements().Where(element => element.Name != "Background"))
                    {
                        int index = (int)item.Attribute("index");
                        if (index < 0 || index >= strokes.Count || !restored.Add(index)) throw new InvalidDataException("Invalid stroke index.");
                        Stroke source = strokes[index];
                        Color color = (Color)ColorConverter.ConvertFromString((string)item.Attribute("color"));
                        Stroke replacement;
                        if (item.Name == "Fill")
                        {
                            Geometry geometry = Geometry.Parse(item.Value).Clone();
                            geometry.Transform = new MatrixTransform(Matrix.Parse((string)item.Attribute("matrix")));
                            if (geometry.Bounds.IsEmpty) throw new InvalidDataException("Empty fill geometry.");
                            geometry.Freeze(); replacement = new RegionFillStroke(geometry, color);
                        }
                        else if (item.Name == "Live")
                        {
                            DrawingAttributes attributes = source.DrawingAttributes.Clone(); attributes.Color = color;
                            replacement = new LiveHighlighterStroke(source.StylusPoints, attributes, (bool)item.Attribute("dashed"));
                        }
                        else throw new InvalidDataException("Unsupported stroke type.");
                        foreach (Guid id in source.GetPropertyDataIds()) replacement.AddPropertyData(id, source.GetPropertyData(id));
                        strokes[index] = replacement;
                    }
                    pages.Add(new BoardPage(strokes, LoadBackground(node.Element("Background"))));
                }
                if (pages.Count == 0) throw new InvalidDataException("No pages.");
                BoardDocument document = new BoardDocument(pages[0].Strokes, pages[0].Background);
                document.ReplacePages(pages, (int)root.Attribute("currentPage"));
                return document;
            }
        }

        private static MemoryStream ReadEntry(ZipArchive archive, string name)
        {
            ZipArchiveEntry entry = archive.GetEntry(name);
            if (entry == null || entry.Length > MaximumEntryBytes) throw new InvalidDataException("Missing or oversized document entry.");
            MemoryStream result = new MemoryStream();
            try
            {
                using (Stream source = entry.Open())
                {
                    byte[] buffer = new byte[81920]; int read;
                    while ((read = source.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        if (result.Length + read > MaximumEntryBytes) throw new InvalidDataException("Document entry is too large.");
                        result.Write(buffer, 0, read);
                    }
                }
                result.Position = 0; return result;
            }
            catch { result.Dispose(); throw; }
        }

        private static XElement SaveBackground(Brush brush)
        {
            if (brush == null) return new XElement("Background", new XAttribute("kind", "None"));
            SolidColorBrush solid = brush as SolidColorBrush;
            if (solid == null) throw new NotSupportedException("Only solid page backgrounds can be saved.");
            return new XElement("Background", new XAttribute("kind", "Solid"), new XAttribute("color", solid.Color.ToString()),
                new XAttribute("opacity", solid.Opacity));
        }

        private static Brush LoadBackground(XElement node)
        {
            if (node == null) throw new InvalidDataException("Missing page background.");
            string kind = (string)node.Attribute("kind");
            if (kind == "None") return null;
            if (kind != "Solid") throw new InvalidDataException("Unsupported page background.");
            SolidColorBrush brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString((string)node.Attribute("color")));
            brush.Opacity = (double)node.Attribute("opacity"); brush.Freeze(); return brush;
        }
    }
}
