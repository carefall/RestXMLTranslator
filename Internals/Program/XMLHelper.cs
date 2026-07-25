using RestXMLTranslator.Internals.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Xml;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace RestXMLTranslator.Internals.Program
{
    public static class XMLHelper
    {
        public static string DecodeMultiline(string text)
        {
            text = Regex.Replace(text, @"[\r\n]+[ \t]*", "");
            return text.Replace("\\n", Environment.NewLine);
        }

        public static string EncodeMultilineForXML(string text)
        {
            return text.Replace("\\n", "\n\\n");
        }

        public static string EncodeMultilineForJSON(string text)
        {
            return text.Replace("\r\n", "\n").Replace("\n", "\\n");
        }

        public static string EncodeMultilineForServer(string text)
        {
            return text.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\\n");
        }

        public static string EncodeMultilineFromInput(string text)
        {
            text = text.Replace("\r\n", "\n").Replace("\r", "\n");
            return text.Replace("\n", "\n\\n");
        }

        public static ObservableCollection<StringEntry> LoadStrings(string xml, bool file)
        {
            if (file && !File.Exists(xml))
            {
                Logger.Log($"File not found: {xml}", "XMLParser");
                return [];
            }
            string[] text;
            if (file) text = File.ReadAllLines(xml, Encoding.GetEncoding(1251));
            else text = xml.Split(["\r\n", "\n"], StringSplitOptions.None);
            List<string> whitespaces = GetWhitespaces(text);
            try
            {
                XDocument? doc = null;
                if (!file) doc = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
                else doc = XDocument.Load(xml, LoadOptions.None);
                XElement root = doc.Root ?? throw new Exception("XML has no root element");
                if (root.Name.LocalName == "string_table")
                {
                    return ParseStrings(root.Elements("string"), whitespaces);
                }
                if (root.Name.LocalName == "string")
                {
                    return ParseStrings(new[] { root }, whitespaces);
                }
                throw new Exception("Неизвестный формат XML.");
            }
            catch (XmlException)
            {
                try
                {
                    string wrapped = $"<string_table>{xml}</string_table>";
                    XDocument doc = XDocument.Parse(wrapped, LoadOptions.None);
                    if (!doc.Root!.Elements("string").Any())
                    {
                        MessageBox.Show(Locale.Get("data_not_xml"), Locale.Get("xml_load_fail"), MessageBoxButton.OK, MessageBoxImage.Warning);
                        return [];
                    }
                    return ParseStrings(doc.Root!.Elements("string"), whitespaces);
                }
                catch (XmlException ex)
                {
                    MessageBox.Show(Locale.Get("xml_corrupt"), Locale.Get("xml_load_fail"), MessageBoxButton.OK, MessageBoxImage.Error);
                    Logger.Log($"XML Exception during parsing wrapped XML: {ex}", "XMLParser");
                    return [];
                }
                catch (Exception ex)
                {
                    Logger.Log($"Unhandled exception: {ex}", "XMLParser");
                    MessageBox.Show(Locale.Get("xml_fail_two"), Locale.Get("xml_load_fail"), MessageBoxButton.OK, MessageBoxImage.Error);
                    return [];
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Unhandled exception: {ex}", "XMLParser-Read");
                MessageBox.Show(Locale.Get("xml_fail_one"), Locale.Get("xml_load_fail"), MessageBoxButton.OK, MessageBoxImage.Error);
                return [];
            }
        }

        private static ObservableCollection<StringEntry> ParseStrings(IEnumerable<XElement> strings, List<string> blankStrings)
        {
            return [..strings.Select(x => {
                string ru = DecodeMultiline(x.Element("rus")?.Value ?? "");
                string eng = DecodeMultiline(x.Element("eng")?.Value ?? "");
                string comment = FindComment(x);
                string id = x.Attribute("id")?.Value ?? "";
                bool newLine = blankStrings.Contains(id);
                return new StringEntry {
                    Id = id,
                    Ru = ru,
                    NewRu = ru,
                    Eng = eng,
                    NewEng = eng,
                    Comment = comment,
                    NewComment = comment,
                    HadNewLine = newLine,
                    HasNewLine = newLine
                };
            })];
        }

        private static string FindComment(XElement element)
        {
            var inside = element.Nodes().OfType<XComment>().FirstOrDefault();
            if (inside != null) return inside.Value.Trim();
            for (XNode? node = element.PreviousNode; node != null; node = node.PreviousNode)
            {
                switch (node)
                {
                    case XComment comment:
                        return comment.Value.Trim();
                    case XElement:
                        return "";
                }
            }
            return "";
        }

        private static List<string> GetWhitespaces(string[] lines)
        {
            List<string> result = [];
            bool hasBlankLine = false;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0)
                {
                    hasBlankLine = true;
                    continue;
                }
                Match match = Regex.Match(line, @"<string\s+id=""([^""]+)""");
                if (match.Success)
                {
                    if (hasBlankLine) result.Add(match.Groups[1].Value);
                    hasBlankLine = false;
                    continue;
                }
                if (line.StartsWith("<!--")) continue;
                hasBlankLine = false;
            }
            return result;
        }
    }
}
