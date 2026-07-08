using Microsoft.Win32;
using RestXMLTranslator.Internals;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;
using System.Xml.Linq;
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

            public string Tip { get; set; } = "";

            public ObservableCollection<StringEntry> Entries { get; set; } = [];

            public FileTab(string path, string relativePath, bool read)
            {
                FilePath = path;
                RelativePath = relativePath;
                Tip = RelativePath;
                Name = Path.GetFileName(path);
                if (!read) return;
                string xml = File.ReadAllText(path, Encoding.GetEncoding(1251));
                Entries = LoadStrings(xml);
            }

            public void WriteToDisk(List<StringEntry> entries)
            {
                FilePath = Settings.GetInstance().GameDataPath + "/gamedata/configs/" + RelativePath;
                Entries = new ObservableCollection<StringEntry>(entries);
                Name = Path.GetFileName(FilePath);
                string dir = Path.GetDirectoryName(FilePath)!;
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                XDocument doc = new(new XElement("string_table"));
                if (File.Exists(FilePath)) doc = XDocument.Load(FilePath);
                var index = doc.Root!.Elements("string").ToDictionary(x => (string)x.Attribute("id")!);
                foreach (var entry in entries)
                {
                    if (!index.TryGetValue(entry.Id!, out var stringElement))
                    {
                        stringElement = new XElement("string", new XAttribute("id", entry.Id!));
                        doc.Root.Add(stringElement);
                        index[entry.Id!] = stringElement;
                    }
                    var node = doc.Root!;
                    var rus = node.Element("rus");
                    string text1 = EncodeMultiline(entry.Ru);
                    if (rus == null) node.Add(new XElement("rus", text1));
                    else rus.Value = text1;
                    var eng = node.Element("eng");
                    string text2 = EncodeMultiline(entry.Eng);
                    if (eng == null) node.Add(new XElement("eng", text2));
                    else eng.Value = text2;
                }
                using var writer = XmlWriter.Create(FilePath, settings);
                doc.Save(writer);
            }

            public bool HasApprovedChanges => Entries.Where(e => e.IsApproved).Any();

            public bool HasChanges => Entries.Where(e => e.HasChanges).Any();
        }

        private ScrollViewer? scrollViewer;
        public ObservableCollection<FileTab> Files { get; } = [];
        private ICollectionView? _currentView;
        public ICollectionView FilesView { get; }

        private string searchText = "";

        private readonly static XmlWriterSettings settings = new()
        {
            Encoding = Encoding.GetEncoding(1251),
            Indent = true
        };

        private readonly static JsonSerializerOptions options = new()
        {
            PropertyNameCaseInsensitive = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        };


        public MainWindow(bool online, string gameDataPath)
        {
            InitializeComponent();
            Title = Locale.Get("window_title", online ? Locale.Get("connected", GetCurrentTimeHM()) : Locale.Get("not_connected"));
            DataContext = this;
            string path = gameDataPath + "/gamedata/configs";
            var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                Files.Add(new FileTab(file, file.Replace(path, "")[1..].Replace("\\", "/"), true));
            }
            FilesView = CollectionViewSource.GetDefaultView(Files);
            FilesView.Filter = FilterFile;
            FilesView.Refresh();
            if (Files.Count == 0)
            {
                MessageBox.Show(Locale.Get("no_files_found"), Locale.Get("translation"), MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
                return;
            }
            ApplyChanges();
            Save.Content = Locale.Get("btn_commit");
            SaveLocal.Content = Locale.Get("btn_save_file");
            SaveAllLocal.Content = Locale.Get("btn_save_all");
            LoadTranslation.Content = Locale.Get("btn_load_file");
            LoadBufferTranslation.Content = Locale.Get("btn_load_buffer");
            HideApproved.Content = Locale.Get("btn_hide_approved");
            HideChanged.Content = Locale.Get("btn_hide_changed");
            HideUnchanged.Content = Locale.Get("btn_hide_unchanged");
            SyncTitle.Text = Locale.Get("sync_title");
            SearchPlaceholder.Text = Locale.Get("search_placeholder");
            NewEngColumn.Header = Locale.Get("new_eng");
            NewRuColumn.Header = Locale.Get("new_rus");
            EngColumn.Header = Locale.Get("eng");
            RuColumn.Header = Locale.Get("rus");
            StatusColumn.Header = Locale.Get("translation_status");
            Sync.Content = Locale.Get("btn_sync");
            DynamicLoc.Init(Locale.Get("btn_approve"), Locale.Get("tip_approve"), Locale.Get("btn_decline"), Locale.Get("tip_decline"));
            var dloc = new DynamicLoc();
            Resources["Loc"] = dloc;
        }

        private bool FilterFile(object obj)
        {
            if (obj is not FileTab file)
                return false;
            if (string.IsNullOrWhiteSpace(searchText))
                return true;
            return file.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase);
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
                Filter = "XML files (*.xml)|*.xml"
            };
            if (dialog.ShowDialog() != true)
                return;
            try
            {
                string xml = File.ReadAllText(dialog.FileName, Encoding.GetEncoding(1251));
                if (LoadTranslationFromXML(xml)) MessageBox.Show(Locale.Get("loaded_from_file"), Locale.Get("translation"));
            }
            catch (Exception ex)
            {
                MessageBox.Show(Locale.Get("parse_file_fail"), Locale.Get("file_load_error"));
                Logger.Log("Translator-FileRead", $"Unhandled exception: {ex}");
            }
        }

        private void LoadBufferTranslation_Click(object sender, RoutedEventArgs e)
        {
            if (!Clipboard.ContainsText() || Clipboard.GetText().Trim() == "")
            {
                MessageBox.Show(Locale.Get("clipboard_empty"), Locale.Get("translation"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (LoadTranslationFromXML(Clipboard.GetText())) MessageBox.Show(Locale.Get("clipboard_loaded"), Locale.Get("translation"));
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
            LoadingOverlay.Visibility = Visibility.Visible;
            int version = await RestClient.CompareVersions();
            if (version == -1)
            {
                LoadingOverlay.Visibility = Visibility.Hidden;
                MessageBox.Show(Locale.Get("server_unreachable"), Locale.Get("sync"));
                Title = Locale.Get("window_title", Locale.Get("not_connected"));
                StoreChanges(file, true);
                return;
            }
            if (version == Settings.GetInstance().Version)
            {
                Logger.Log("RestClient-Get", $"Before commit, program is up to date(with version: {Settings.GetInstance().Version})");
                if (!(await RestClient.Upload(file)))
                {
                    LoadingOverlay.Visibility = Visibility.Hidden;
                    MessageBox.Show(Locale.Get("server_load_fail"), Locale.Get("sync"));
                    Title = Locale.Get("window_title", Locale.Get("not_connected"));
                    StoreChanges(file, true);
                    return;
                }
                LoadingOverlay.Visibility = Visibility.Hidden;
                SaveChangesLocally(file);
                Title = Locale.Get("window_title", Locale.Get("connected", GetCurrentTimeHM()));
                return;
            }
            Logger.Log("RestClient-Get", $"Before commit, program was not up to date(with version: {Settings.GetInstance().Version})");
            List<DownloadedFile>? updates = await RestClient.Update(Files);
            if (updates == null)
            {
                LoadingOverlay.Visibility = Visibility.Hidden;
                MessageBox.Show(Locale.Get("server_update_fail"), Locale.Get("sync"));
                Title = Locale.Get("window_title", Locale.Get("not_connected"));
                StoreChanges(file, true);
                return;
            }
            Settings.GetInstance().UpdateVersion(version);
            Update(updates);
            if (!file.HasApprovedChanges)
            {
                LoadingOverlay.Visibility = Visibility.Hidden;
                MessageBox.Show(Locale.Get("all_translations_replaced"), Locale.Get("sync"));
                Title = Locale.Get("window_title", Locale.Get("connected", GetCurrentTimeHM()));
                return;
            }
            if (!(await RestClient.Upload(file)))
            {
                LoadingOverlay.Visibility = Visibility.Hidden;
                MessageBox.Show(Locale.Get("server_load_fail"), Locale.Get("sync"));
                Title = Locale.Get("window_title", Locale.Get("not_connected"));
                StoreChanges(file, true);
                return;
            }
            LoadingOverlay.Visibility = Visibility.Hidden;
            SaveChangesLocally(file);
            Title = Locale.Get("window_title", Locale.Get("connected", GetCurrentTimeHM()));
            return;
        }


        private void Update(List<DownloadedFile> updates)
        {
            foreach (DownloadedFile dfile in updates)
            {
                FileTab file = new FileTab("", dfile.Path, false);
                var seq = Files.Where(f => f.RelativePath == dfile.Path);
                if (seq == null || !seq.Any())
                {
                    file.WriteToDisk(dfile.Entries);
                    Files.Add(file);
                    FilesList.ScrollIntoView(file);
                    FilesList.UpdateLayout();
                    continue;
                }
                FileTab bro = seq.First();
                foreach (StringEntry entry in dfile.Entries)
                {
                    var entrySeq = bro.Entries.Where(e => e.Id == entry.Id);
                    if (entrySeq == null || !seq.Any())
                    {
                        bro.Entries.Add(entry);
                        continue;
                    }
                    StringEntry toChange = entrySeq.First();
                    if (toChange.HasRuChanges && entry.downloadedRu && toChange.HasEngChanges && entry.downloadedEng)
                        toChange.IsApproved = false;
                    else if (toChange.HasRuChanges && entry.downloadedRu)
                        toChange.IsApproved = false;
                    else if (toChange.HasEngChanges && entry.downloadedEng)
                        toChange.IsApproved = false;
                    if (entry.downloadedRu)
                    {
                        toChange.Ru = entry.Ru;
                        toChange.NewRu = entry.NewRu;
                    }
                    if (entry.downloadedEng)
                    {
                        toChange.Eng = entry.Eng;
                        toChange.NewEng = entry.NewEng;
                    }
                }
                WriteFile(bro);
                StoreChanges(bro, true);
            }
        }

        private static void WriteFile(FileTab file)
        {
            XDocument doc = XDocument.Load(file.FilePath);
            var entries = file.Entries;
            foreach (var entry in entries)
            {
                var node = doc.Root!.Elements("string").FirstOrDefault(x => (string?)x.Attribute("id") == entry.Id);
                if (node == null)
                {
                    continue;
                }
                var rus = node.Element("rus");
                string text1 = EncodeMultiline(entry.Ru);
                if (rus == null) node.Add(new XElement("rus", text1));
                else rus.Value = text1;
                var eng = node.Element("eng");
                string text2 = EncodeMultiline(entry.Eng);
                if (eng == null) node.Add(new XElement("eng", text2));
                else eng.Value = text2;

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
                    entry.Ru = entry.NewRu;
                    entry.Eng = entry.NewEng;
                    entry.IsApproved = false;
                }
            }
            WriteFile(file);
            StoreChanges(file, false);
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
            FilesView.Refresh();
        }

        private async void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
            {
                SearchBox.Focus();
                SearchBox.SelectAll();
                e.Handled = true;
            }
            if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (FilesList.SelectedItem is FileTab tab)
                {
                    if (tab.HasChanges) StoreChanges(tab, true);
                }
                e.Handled = true;
                await ShowStatusAsync(Locale.Get("file_saved"));
            }
            if (e.Key == Key.S && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
            {
                foreach (var file in Files)
                {
                    if (!file.HasChanges) continue;
                    StoreChanges(file, true);
                }
                e.Handled = true;
                await ShowStatusAsync(Locale.Get("changes_saved"));
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
            foreach (var item in Grid.SelectedItems)
            {
                if (item is StringEntry entry && entry.HasChanges)
                {
                    entry.IsApproved = !entry.IsApproved;
                }
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

        private async void SaveLocal_Click(object sender, RoutedEventArgs e)
        {
            if (FilesList.SelectedItem is not FileTab tab) return;
            e.Handled = true;
            if (!tab.HasChanges) return;
            StoreChanges(tab, true);
            await ShowStatusAsync(Locale.Get("file_saved"));
        }

        private async void SaveAllLocal_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            MessageBox.Show("1");
            if (!Files.Where(f => f.HasChanges).Any()) return;
            MessageBox.Show("2");
            foreach (var file in Files)
            {
                MessageBox.Show(file.Name);
                if (!file.HasChanges) continue;
                StoreChanges(file, true);
                MessageBox.Show("4");
            }
            await ShowStatusAsync(Locale.Get("changes_saved"));
        }

        private async Task ShowStatusAsync(string text)
        {
            Status.Text = text;
            await Task.Delay(2000);
            Status.Text = string.Empty;
        }

        private void DataGridCell_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGridCell cell && !cell.IsEditing && !cell.IsReadOnly)
            {
                cell.Focus();
                if (Grid.BeginEdit(e))
                {
                    e.Handled = true;
                }
                Grid.SelectedItem = cell.DataContext;
            }
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var tb = (TextBox)sender;
            tb.CaretIndex = tb.Text.Length;
            tb.SelectionLength = 0;
        }

        private async void Sync_Click(object sender, RoutedEventArgs e)
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            int version = await RestClient.CompareVersions();
            if (version == -1)
            {
                LoadingOverlay.Visibility = Visibility.Hidden;
                MessageBox.Show(Locale.Get("server_unreachable"), Locale.Get("sync"));
                Title = Locale.Get("window_title", Locale.Get("not_connected"));
                return;
            }
            if (version == Settings.GetInstance().Version)
            {
                LoadingOverlay.Visibility = Visibility.Hidden;
                MessageBox.Show(Locale.Get("sync_up_to_date"), Locale.Get("sync"));
                Title = Locale.Get("window_title", Locale.Get("connected", GetCurrentTimeHM()));
                return;
            }
            List<DownloadedFile>? updates = await RestClient.Update(Files);
            if (updates == null)
            {
                LoadingOverlay.Visibility = Visibility.Hidden;
                MessageBox.Show(Locale.Get("server_update_fail"), Locale.Get("sync"));
                Title = Locale.Get("window_title", Locale.Get("not_connected"));
                return;
            }
            Settings.GetInstance().UpdateVersion(version);
            Update(updates);
            LoadingOverlay.Visibility = Visibility.Hidden;
            MessageBox.Show(Locale.Get("synced"), Locale.Get("sync"));
            Title = Locale.Get("window_title", Locale.Get("connected", GetCurrentTimeHM()));
        }
    }
}