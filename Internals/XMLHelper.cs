using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Xml;
using System.Xml.Linq;

namespace RestXMLTranslator.Internals
{
    public class XMLHelper
    {
        public static string DecodeMultiline(string text)
        {
            return text.Replace("\\n", Environment.NewLine);
        }

        public static string EncodeMultiline(string text)
        {
            return text.Replace("\r\n", "\\n")
                       .Replace("\n", "\\n");
        }

        public static ObservableCollection<StringEntry> LoadStrings(string xml)
        {
            try
            {
                XDocument doc = XDocument.Parse(xml);
                XElement root = doc.Root!;
                if (root.Name.LocalName == "string_table")
                {
                    return ParseStrings(root.Elements("string"));
                }
                if (root.Name.LocalName == "string")
                {
                    return ParseStrings(new[] { root });
                }
                throw new Exception("Неизвестный формат XML.");
            }
            catch (XmlException)
            {
                try
                {
                    string wrapped = $"<string_table>{xml}</string_table>";
                    XDocument doc = XDocument.Parse(wrapped);
                    if (!doc.Root!.Elements("string").Any())
                    {
                        MessageBox.Show(Locale.Get("data_not_xml"), Locale.Get("xml_load_fail"), MessageBoxButton.OK, MessageBoxImage.Warning);
                        return [];
                    }
                    return ParseStrings(doc.Root!.Elements("string"));
                }
                catch (XmlException ex)
                {
                    MessageBox.Show(Locale.Get("xml_corrupt"), Locale.Get("xml_load_fail"), MessageBoxButton.OK, MessageBoxImage.Error);
                    Logger.Log("XMLParser", $"XML Exception during parsing wrapped XML: {ex}");
                    return [];
                }
                catch (Exception ex)
                {
                    Logger.Log("XMLParser", $"Unhandled exception: {ex}");
                    MessageBox.Show(Locale.Get("xml_fail_two"), Locale.Get("xml_load_fail"), MessageBoxButton.OK, MessageBoxImage.Error);
                    return [];
                }
            }
            catch (Exception ex)
            {
                Logger.Log("XMLParser-Read", $"Unhandled exception: {ex}");
                MessageBox.Show(Locale.Get("xml_fail_one"), Locale.Get("xml_load_fail"), MessageBoxButton.OK, MessageBoxImage.Error);
                return [];
            }
        }

        private static ObservableCollection<StringEntry> ParseStrings(IEnumerable<XElement> strings)
        {
            return [.. strings.Select(x => new StringEntry
            {
                Id = x.Attribute("id")?.Value ?? "",
                Ru = DecodeMultiline(x.Element("rus")?.Value ?? ""),
                NewRu = DecodeMultiline(x.Element("rus")?.Value ?? ""),
                Eng = DecodeMultiline(x.Element("eng")?.Value ?? ""),
                NewEng = DecodeMultiline(x.Element("eng")?.Value ?? "")
            })];
        }

        public class StringEntry : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler? PropertyChanged;

            protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

            public string Id { get; set; } = "";

            private string _ru = "";
            public string Ru
            {
                get => _ru; set
                {
                    _ru = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasRuChanges));
                    OnPropertyChanged(nameof(HasChanges));
                }
            }

            private string _eng = "";
            public string Eng
            {
                get => _eng; set
                {
                    _eng = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasEngChanges));
                    OnPropertyChanged(nameof(HasChanges));
                }
            }

            private string _newEng = "";
            public string NewEng
            {
                get => _newEng;
                set
                {
                    _newEng = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasEngChanges));
                    OnPropertyChanged(nameof(HasChanges));
                }
            }

            private string _newRu = "";


            public string NewRu
            {
                get => _newRu;
                set
                {
                    _newRu = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasRuChanges));
                    OnPropertyChanged(nameof(HasChanges));
                }
            }

            private bool _isApproved;

            public bool IsApproved
            {
                get => _isApproved;
                set
                {
                    _isApproved = value;
                    OnPropertyChanged();
                }
            }

            public bool HasChanges => HasRuChanges || HasEngChanges;

            public bool HasRuChanges => Ru != NewRu;

            public bool HasEngChanges => Eng != NewEng;

        }
    }
}
