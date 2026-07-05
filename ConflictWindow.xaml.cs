using GitXMLTranslator.Internals;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml;
using System.Xml.Linq;
using static GitXMLTranslator.Internals.StringEntry;

namespace GitXMLTranslator
{
    public partial class ConflictWindow : Window, INotifyPropertyChanged
    {
        private string name;
        private string gamedata;
        public bool AreAllConflictsResolved => AllConflictsResolved();

        public ObservableCollection<DownloadedFile> Files { get; set; } = [];
        public ObservableCollection<Conflict> Conflicts { get; set; } = [];

        private readonly XmlWriterSettings settings = new()
        {
            Encoding = Encoding.GetEncoding(1251),
            Indent = true
        };

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public enum ConflictType
        {
            RU, ENG
        }

        public class Conflict(string id, string local, string server, ConflictType type) : INotifyPropertyChanged
        {
            public string Id { get; set; } = id;
            public string Local { get; set; } = local;
            public string Server { get; set; } = server;

            public readonly ConflictType type = type;
            private bool _result;
            public bool Result
            {
                get => _result;
                set
                {
                    _result = value;
                    OnPropertyChanged(nameof(Result));
                    OnPropertyChanged(nameof(TextResult));
                }
            }

            public string TextResult => Result ? "Берём с ПК" : "Берём с сервера";
            private bool _resolved;
            public bool Resolved
            {
                get => _resolved;
                set
                {
                    _resolved = value;
                    OnPropertyChanged(nameof(Resolved));
                    OnPropertyChanged(nameof(TextResolved));
                    OnPropertyChanged(nameof(NotResolved));
                }
            }
            public bool NotResolved { get => !_resolved; }

            public string TextResolved => Resolved ? "Утверждено" : "В работе";

            public event PropertyChangedEventHandler? PropertyChanged;

            protected void OnPropertyChanged(string? name = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }

        public class DownloadedFile(string name, string absPath, string gamedataPath, Dictionary<string, StringEntry> selfEntries, Dictionary<string, StringEntry> targetEntries)
        {
            public string absPath = absPath, gamedataPath = gamedataPath;
            public Dictionary<string, StringEntry> selfEntries = selfEntries, targetEntries = targetEntries;
            public ObservableCollection<Conflict> conflicts = [];

            public string Name { get; set; } = name;
        }

        public ConflictWindow(string name, string gamedata)
        {
            InitializeComponent();
            this.name = name;
            this.gamedata = gamedata;
            DataContext = this;
        }

        public void RebuildLocalFiles()
        {
            try
            {
                string directory = AppDomain.CurrentDomain.BaseDirectory + "Downloads";
                if (!Directory.Exists(directory))
                {
                    new MainWindow(name).Show();
                    Close();
                    return;
                }
                var filelist = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);
                if (filelist.Length == 0)
                {
                    new MainWindow(name).Show();
                    Close();
                    return;
                }
                foreach (string file in filelist)
                {
                    string filename = file.Replace(directory + "\\", "");
                    string targetName = gamedata + "/gamedata/configs/" + filename;
                    using var sReader = new StreamReader(file, Encoding.GetEncoding(1251));
                    XDocument self = XDocument.Load(sReader);
                    var sEntries = GenerateEntries(self);
                    using var tReader = new StreamReader(targetName, Encoding.GetEncoding(1251));
                    XDocument target = XDocument.Load(tReader);
                    var tEntries = GenerateEntries(target);
                    Files.Add(new DownloadedFile(filename, file, targetName, sEntries, tEntries));
                }
                ManageIds();
                OverrideComments();
                SaveLocalFiles();
                AddMissingTranslations();
                SaveLocalFiles();
                GenerateConflicts();
                OnPropertyChanged(nameof(AreAllConflictsResolved));
                if (AllConflictsResolved())
                {
                    new MainWindow(name).Show();
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Неизвестная ошибка");
                Application.Current.Shutdown();
            }
        }

        private bool FileConflictsResolved(DownloadedFile file)
        {
            foreach (var conflict in file.conflicts)
            {
                if (conflict.NotResolved) return false;
            }
            return true;
        }

        private bool AllConflictsResolved()
        {
            foreach (var file in Files)
            {
                if (!FileConflictsResolved(file)) return false;
            }
            return true;
        }

        private void GenerateConflicts() // Удаляет неконфликтующие файлы 
        {
            List<DownloadedFile> nonConflicting = [];
            foreach (var file in Files)
            {
                foreach (var entry in file.selfEntries)
                {
                    StringEntry s = entry.Value, t = file.targetEntries[entry.Key];
                    if (s.newRu == t.newRu && s.newEn == t.newEn) continue;
                    if (s.newRu != t.newRu)
                    {
                        Conflict c = new(s.id, t.newRu, s.newRu, ConflictType.RU);
                        file.conflicts.Add(c);
                    }
                    if (s.newEn != t.newEn)
                    {
                        Conflict c = new(s.id, t.newEn, s.newEn, ConflictType.ENG);
                        file.conflicts.Add(c);
                    }
                }
                if (file.conflicts.Count == 0)
                {
                    nonConflicting.Add(file);
                }
            }
            foreach (var file in nonConflicting)
            {
                Files.Remove(file);
            }
        }

        private void AddMissingTranslations() // Добавляет отсутствующие англ. переводы
        {
            foreach (var file in Files)
            {
                foreach (var entry in file.targetEntries)
                {
                    if (!string.IsNullOrEmpty(entry.Value.newEn)) continue;
                    if (string.IsNullOrEmpty(file.selfEntries[entry.Key].newEn)) continue;
                    entry.Value.newEn = file.selfEntries[entry.Key].newEn;
                }
            }
        }

        private void SaveLocalFiles() // Перезапись локальных файлов для более точного совпадения
        {
            foreach (var file in Files)
            {
                var doc = new XDocument(
                    new XElement("string_table",
                        file.targetEntries.Select(e =>
                            new XElement("string",
                                new XAttribute("id", e.Key),
                                new XAttribute("comment", e.Value.comment ?? ""),
                                new XElement("rus", e.Value.newRu),
                                new XElement("eng", e.Value.newEn ?? "")
                            )
                        )
                    )
                );
                using var writer = XmlWriter.Create(file.gamedataPath, settings);
                doc.Save(writer);
            }
        }

        private void ManageIds() // Делает targetEntries одинаковым по составу с selfEntries
        {
            foreach (var file in Files)
            {
                var toRemove = file.targetEntries.Keys.Where(id => !file.selfEntries.ContainsKey(id)).ToList();
                foreach (var id in toRemove)
                {
                    file.targetEntries.Remove(id);
                } // Удалили записи, которые больше не актуальны
                foreach (var id in file.selfEntries.Keys)
                {
                    if (!file.targetEntries.ContainsKey(id)) file.targetEntries[id] = file.selfEntries[id];
                } // Добавили записи, которых раньше не было
            }
        }

        private void OverrideComments() // Перезаписывает комментарии на новые
        {
            foreach (var file in Files)
            {
                foreach (var id in file.selfEntries.Keys)
                {
                    file.targetEntries[id].comment = file.selfEntries[id].comment;
                }
            }
        }

        private static Dictionary<string, StringEntry> GenerateEntries(XDocument doc)
        {
            var dict = new Dictionary<string, StringEntry>();
            foreach (var element in doc.Root!.Elements("string"))
            {
                string id = element.Attribute("id")?.Value ?? "";
                string ru = DecodeMultiline(element.Element("rus")?.Value ?? "");
                string newRu = ru;
                string en = DecodeMultiline(element.Element("eng")?.Value ?? "");
                string newEn = en;
                string comment = element.Attribute("comment")?.Value ?? "";
                dict[id] = new StringEntry(id, ru, newRu, en, newEn, comment);
            }
            return dict;
        }

        private void Files_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (FilesList.SelectedItem is DownloadedFile file)
            {
                LoadConflicts(file);
            }
        }

        void LoadConflicts(DownloadedFile file)
        {
            Conflicts.Clear();
            foreach (var c in file.conflicts)
                Conflicts.Add(c);
        }

        private void EditLongText(object sender, MouseButtonEventArgs e)
        {
            if (sender is not TextBox tb)
                return;
            if (tb.IsReadOnly)
                return;
            var dlg = new TextEditWindow(tb.Text)
            {
                Owner = this,
                Title = "Редактирование текста перевода"
            };
            if (dlg.ShowDialog() == true)
            {
                tb.Text = dlg.ResultText;
            }
            e.Handled = true;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Conflict conflict)
            {
                conflict.Result = !conflict.Result;
            }
        }

        private void Resolve_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Conflict conflict)
            {
                conflict.Resolved = !conflict.Resolved;
                OnPropertyChanged(nameof(AreAllConflictsResolved));
            }
        }

        private void Finish_Click(object sender, RoutedEventArgs e)
        {
            foreach (var file in Files)
            {
                foreach (var conflict in file.conflicts)
                {
                    string result = conflict.Result ? conflict.Local : conflict.Server;
                    var t = file.targetEntries[conflict.Id];
                    var s = file.selfEntries[conflict.Id];
                    if (conflict.type == ConflictType.RU)
                    {
                        t.newRu = result;
                        s.newRu = result;
                        s.oldRu = result;
                        t.oldRu = result;
                    } else
                    {
                        t.newEn = result;
                        s.newEn = result;
                        s.oldEn = result;
                        t.oldEn = result;
                    }
                }
            }
            SaveLocalFiles();
            Directory.Delete(AppDomain.CurrentDomain.BaseDirectory + "Downloads");
            new MainWindow(name).Show();
            Close();
            return;
        }
    }
}
