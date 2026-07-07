using Microsoft.Win32;
using RestXMLTranslator.Internals;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Channels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;
using System.Xml.Linq;
using static RestXMLTranslator.Internals.JSONHelper;
using static RestXMLTranslator.Internals.RestClient;
using static RestXMLTranslator.Internals.XMLHelper;

namespace RestXMLTranslator
{
    public partial class MainWindow : Window
    {

        public class FileTab
        {
            public string Name { get; set; } = "";
            public string FilePath { get; set; } = "";

            public string RelativePath { get; set; } = "";

            public ObservableCollection<StringEntry> Entries { get; set; } = [];

            public FileTab(string path, string relativePath, bool read)
            {
                FilePath = path;
                RelativePath = relativePath;
                Name = Path.GetFileName(path);
                if (!read) return;
                string xml = File.ReadAllText(path, Encoding.GetEncoding(1251));
                Entries = LoadStrings(xml);
            }

            public bool HasApprovedChanges => Entries.Where(e => e.IsApproved).Any();

            public bool HasChanges => Entries.Where(e => e.HasChanges).Any();
        }

        private ScrollViewer? scrollViewer;
        public ObservableCollection<FileTab> Files { get; } = [];
        private ICollectionView? _currentView;

        private string searchText = "";

        public static Action? OnShutdown;

        private readonly XmlWriterSettings settings = new()
        {
            Encoding = Encoding.GetEncoding(1251),
            Indent = true
        };

        private readonly JsonSerializerOptions options = new()
        {
            PropertyNameCaseInsensitive = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        };


        public MainWindow(bool online, string gameDataPath)
        {
            InitializeComponent();
            Title = "RestXMLTranslator - " + (online ? $"Подключено (последняя синхронизация: {GetCurrentTimeHM()})" : "Нет соединения с сервером");
            DataContext = this;
            string path = gameDataPath + "/gamedata/configs";
            var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                Files.Add(new FileTab(file, file.Replace(path, "")[1..], true));
            }
            if (Files.Count == 0)
            {
                MessageBox.Show("Не найдено ни одного файла.", "Перевод");
                OnShutdown?.Invoke();
                return;
            }
            if (!online)
            {
                ApplyChanges();
            }
        }

        private void ApplyChanges()
        {
            foreach (var file in Files)
            {
                string filePath = AppDomain.CurrentDomain.BaseDirectory + "Changes/" + file.RelativePath.Replace(".xml", ".json");
                string directory = Path.GetDirectoryName(filePath)!;
                if (!Directory.Exists(directory))
                {
                    continue;
                }
                if (!File.Exists(filePath))
                {
                    continue;
                }
                string json = File.ReadAllText(filePath, Encoding.GetEncoding(1251));
                List<Change>? changes = JsonSerializer.Deserialize<List<Change>>(json, options);
                changes ??= [];
                foreach (Change change in changes)
                {
                    var seq = file.Entries.Where(e => e.Id == change.Id);
                    if (seq == null || !seq.Any()) continue;
                    StringEntry bro = seq.First();
                    bro.NewEng = change.Eng;
                    bro.NewRu = change.Ru;
                    bro.IsApproved = change.IsApproved;
                }
            }
        }

        private void FilesList_Loaded(object sender, RoutedEventArgs e)
        {
            scrollViewer = GetScrollViewer(FilesList);
            FilesList.SelectedIndex = 0;
            FilesList.Focus();
        }

        private static string GetCurrentTimeHM() => DateTime.Now.ToString("HH:mm");

        private void FilesList_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            scrollViewer!.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - e.Delta);
            e.Handled = true;
        }

        private static ScrollViewer? GetScrollViewer(DependencyObject o)
        {
            if (o is ScrollViewer s) return s;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(o); i++)
            {
                var child = VisualTreeHelper.GetChild(o, i);
                var result = GetScrollViewer(child);
                if (result != null) return result;
            }
            return null;
        }


        private void LoadTranslation_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new()
            {
                Filter = "XML файлы (*.xml)|*.xml"
            };
            if (dialog.ShowDialog() != true)
                return;
            try
            {
                string xml = File.ReadAllText(dialog.FileName, Encoding.GetEncoding(1251));
                if (LoadTranslationFromXML(xml)) MessageBox.Show("Переводы загружены из файла.", "Перевод");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось обработать файл. Обратитесь к разработчику. Приложите файл log.txt.", "Ошибка загрузки файла");
                Logger.Log("Translator-FileRead", $"Unhandled exception: {ex}");
            }
        }

        private void LoadBufferTranslation_Click(object sender, RoutedEventArgs e)
        {
            if (!Clipboard.ContainsText())
            {
                MessageBox.Show("Буфер обмена не содержит текста.");
                return;
            }
            if (LoadTranslationFromXML(Clipboard.GetText())) MessageBox.Show("Переводы загружены из буфера.", "Перевод");
        }

        private bool LoadTranslationFromXML(string text)
        {
            var translations = LoadStrings(text).ToDictionary(x => x.Id!);
            if (translations.Count == 0)
            {
                return false;
            }
            foreach (var item in (FilesList.SelectedItem as FileTab)!.Entries)
            {
                if (translations.TryGetValue(item.Id!, out var tr) &&
                    !string.IsNullOrWhiteSpace(tr.Eng))
                {
                    item.NewEng = tr.Eng;
                }
                if (translations.TryGetValue(item.Id, out var tr2) &&
                    !string.IsNullOrWhiteSpace(tr2.Ru))
                {
                    item.NewRu = tr2.Ru;
                }
            }
            _currentView?.Refresh();
            return true;
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (FilesList.SelectedItem is not FileTab file) return;
            if (!file.HasApprovedChanges) return;
            bool? upToDate = await RestClient.CompareVersions();
            if (upToDate == null)
            {
                MessageBox.Show("Соединение с сервером не установлено. Обновления будут сохранены локально до следующей успешной синхронизации.", "Синхронизация");
                Title = "Нет соединения с сервером";
                StoreChanges(file, true);
                return;
            }
            if (upToDate == true)
            {
                if (!(await RestClient.Upload(file)))
                {
                    MessageBox.Show("Соединение с сервером разорвано во время отправки изменений. Обновления будут сохранены локально до следующей успешной синхронизации.", "Синхронизация");
                    Title = "Нет соединения с сервером";
                    StoreChanges(file, true);
                    return;
                }
                SaveChangesLocally(file);
                Title = $"Подключено (последняя синхронизация: {GetCurrentTimeHM()})";
                return;
            }
            List<DownloadedFile>? updates = await RestClient.Update(Files);
            if (updates == null)
            {
                MessageBox.Show("Соединение с сервером разорвано во время синхронизации. Обновления будут сохранены локально до следующей успешной синхронизации.", "Синхронизация");
                Title = "Нет соединения с сервером";
                StoreChanges(file, true);
                return;
            }
            Update(updates);
            if (!file.HasApprovedChanges)
            {
                MessageBox.Show("В результате синхронизации АБСОЛЮТНО КАЖДЫЙ ваш перевод был заменён. Ваши изменения не будут применены.", "Синхронизация");
                Title = $"Подключено (последняя синхронизация: {GetCurrentTimeHM()})";
                return;
            }
            if (!(await RestClient.Upload(file)))
            {
                MessageBox.Show("Соединение с сервером разорвано во время отправки изменений. Обновления будут сохранены локально до следующей успешной синхронизации.", "Синхронизация");
                Title = "Нет соединения с сервером";
                StoreChanges(file, true);
                return;
            }
            SaveChangesLocally(file);
            Title = $"Подключено (последняя синхронизация: {GetCurrentTimeHM()})";
            return;
        }


        private void Update(List<DownloadedFile> updates)
        {
            foreach (DownloadedFile dfile in updates)
            {
                FileTab file = new FileTab("", dfile.Path, false);
                foreach (var dEntry  in dfile.Entries)
                {

                }
                var seq = Files.Where(f => f.RelativePath == file.RelativePath);
                if (seq == null || !seq.Any())
                {
                    Files.Add(file);
                    continue;
                }
                FileTab bro = seq.First();
                foreach (StringEntry entry in file.Entries)
                {
                    var entrySeq = bro.Entries.Where(e => e.Id == entry.Id);
                    if (entrySeq == null || !seq.Any())
                    {
                        bro.Entries.Add(entry);
                        continue;
                    }
                    StringEntry toChange = entrySeq.First();
                    toChange.IsApproved = false;
                    toChange.Ru = entry.Ru;
                    toChange.NewRu = entry.NewRu;
                    toChange.Eng = entry.Eng;
                    toChange.NewEng = entry.NewEng;
                }
                WriteFile(file);
                StoreChanges(file, true);
            }
        }

        private void WriteFile(FileTab file)
        {
            XDocument doc = XDocument.Load(file.FilePath);
            var entries = file.Entries;
            foreach (var entry in entries)
            {
                var node = doc.Root!.Elements("string").FirstOrDefault(x => (string?)x.Attribute("id") == entry.Id);
                if (node == null)
                    continue;
                if (!entry.HasRuChanges)
                {
                    var rus = node.Element("rus");
                    string text = EncodeMultiline(entry.Ru);
                    if (rus == null) node.Add(new XElement("rus", text));
                    else rus.Value = text;
                }
                if (entry.HasEngChanges)
                {
                    var eng = node.Element("eng");
                    string text = EncodeMultiline(entry.Eng);
                    if (eng == null) node.Add(new XElement("eng", text));
                    else eng.Value = text;
                }
            }
            using var writer = XmlWriter.Create(file.FilePath, settings);
            doc.Save(writer);
        }


        private void SaveChangesLocally(FileTab file)
        {
            foreach (StringEntry entry in file.Entries)
            {
                if (entry.IsApproved)
                {
                    entry.IsApproved = false;
                    entry.Ru = entry.NewRu;
                    entry.Eng = entry.NewEng;
                }
            }
            StoreChanges(file, false);
            WriteFile(file);
        }

        private void StoreChanges(FileTab file, bool allowApprove)
        {
            string filePath = AppDomain.CurrentDomain.BaseDirectory + "Changes/" + file.RelativePath.Replace(".xml", ".json");
            string directory = Path.GetDirectoryName(filePath)!;
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            if (!File.Exists(filePath))
            {
                File.WriteAllText(filePath, "[]", Encoding.GetEncoding(1251));
            }
            string json = File.ReadAllText(filePath, Encoding.GetEncoding(1251));
            List<Change>? changes = JsonSerializer.Deserialize<List<Change>>(json, options);
            changes ??= [];
            foreach (StringEntry entry in file.Entries)
            {
                var seq = changes.Where(c => c.Id == entry.Id);
                if (seq == null || !seq.Any())
                {
                    if (!entry.HasChanges) continue;
                    changes.Add(new Change(entry.Id, entry.NewRu, entry.NewEng, false));
                    continue;
                }
                else
                {
                    Change bro = seq.First();
                    if (entry.HasChanges)
                    {
                        bro.Eng = entry.NewEng;
                        bro.Ru = entry.NewRu;
                        bro.IsApproved = allowApprove;
                        continue;
                    }
                    changes.Remove(bro);
                }
            }
            if (changes.Count == 0)
            {
                File.Delete(filePath);
            }
            else
            {
                File.WriteAllText(filePath, JsonSerializer.Serialize(changes, options), Encoding.GetEncoding(1251));
            }
        }


        private void FilterChanged(object sender, RoutedEventArgs e)
        {
            _currentView?.Refresh();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            searchText = SearchBox.Text;
            _currentView?.Refresh();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
            {
                SearchBox.Focus();
                SearchBox.SelectAll();
                e.Handled = true;
            }
        }

        private void EditLongText(object sender, MouseButtonEventArgs e)
        {
            if (sender is not DataGridCell cell)
                return;
            if (cell.DataContext is not StringEntry entry)
                return;
            if (entry.IsApproved)
                return;
            if (cell.Column is not DataGridTextColumn column ||
                column.Binding is not Binding binding)
                return;

            var property = typeof(StringEntry).GetProperty(binding.Path.Path);
            if (property == null)
                return;
            if (!property.CanWrite)
                return;

            var dlg = new TextEditWindow(property.GetValue(entry)?.ToString() ?? "")
            {
                Owner = this
            };
            if (dlg.ShowDialog() == true)
            {
                property.SetValue(entry, dlg.ResultText);
            }
            e.Handled = true;
        }

        private void Approve_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { DataContext: StringEntry entry })
            {
                if (!entry.HasChanges) return;
                entry.IsApproved = !entry.IsApproved;
            }
        }

        private void FilesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FilesList.SelectedItem is not FileTab file) return;
            _currentView = CollectionViewSource.GetDefaultView(file.Entries);
            _currentView.Filter = FilterItems;
            Grid.ItemsSource = _currentView;
        }
        private bool FilterItems(object obj)
        {
            if (obj is not StringEntry entry)
                return false;
            if (HideApproved.IsChecked == true && entry.IsApproved)
                return false;
            if (HideChanged.IsChecked == true && entry.HasChanges)
                return false;
            if (HideUnchanged.IsChecked == true && !entry.HasChanges)
                return false;
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                string s = searchText.ToLowerInvariant();
                if (!(entry.Id?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) &&
                    !(entry.Ru?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) &&
                    !(entry.Eng?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) &&
                    !(entry.NewRu?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) &&
                    !(entry.NewEng?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false))
                {
                    return false;
                }
            }

            return true;
        }

        private void Grid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            if (e.Row.Item is StringEntry entry && entry.IsApproved)
            {
                e.Cancel = true;
            }
        }
    }
}