using RestXMLTranslator.Internals.Models;
using RestXMLTranslator.Internals.Program;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace RestXMLTranslator
{
    public partial class MainWindow : Window
    {
        public MainWindow(bool online)
        {
            InitializeComponent();
            Title = Locale.Get("window_title", online ? Locale.Get("connected", GetCurrentTimeHM()) : Locale.Get("not_connected"));
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            
        }

        private bool FilterFile(object obj)
        {
            // if (obj is not FileTab file) return false;
            // if (string.IsNullOrWhiteSpace(searchText)) return true;
            // return file.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase);
            return true;
        }

        private void ApplyChanges()
        {
            //foreach (var file in Files)
            //{
            //    string filePath = AppDomain.CurrentDomain.BaseDirectory + "Changes/" + file.RelativePath.Replace(".xml", ".json");
            //    string directory = Path.GetDirectoryName(filePath)!;
            //    if (!Directory.Exists(directory))
            //    {
            //        continue;
            //    }
            //    if (!File.Exists(filePath))
            //    {
            //        continue;
            //    }
            //    string json = File.ReadAllText(filePath, Encoding.GetEncoding(1251));
            //    List<Change>? changes = JsonSerializer.Deserialize<List<Change>>(json, App.Current.JsonOptions);
            //    changes ??= [];
            //    foreach (Change change in changes)
            //    {
            //        var seq = file.Entries.Where(e => e.Id == change.Id);
            //        if (seq == null || !seq.Any()) continue;
            //        StringEntry bro = seq.First();
            //        bro.NewEng = change.Eng;
            //        bro.NewRu = change.Ru;
            //        bro.IsApproved = change.IsApproved;
            //    }
            //}
        }

        private void FilesList_Loaded(object sender, RoutedEventArgs e)
        {
            //scrollViewer = GetScrollViewer(FilesList);
            //if (FilesList != null && FilesList.HasItems)
            //{
            //    FilesList.SelectedIndex = 0;
            //    FilesList.Focus();
            //}
        }

        private static string GetCurrentTimeHM() => DateTime.Now.ToString("HH:mm");

        private void FilesList_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            //scrollViewer!.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - e.Delta);
            //e.Handled = true;
        }

        private static ScrollViewer? GetScrollViewer(DependencyObject o)
        {
            //if (o is ScrollViewer s) return s;
            //for (int i = 0; i < VisualTreeHelper.GetChildrenCount(o); i++)
            //{
            //    var child = VisualTreeHelper.GetChild(o, i);
            //    var result = GetScrollViewer(child);
            //    if (result != null) return result;
            //}
            return null;
        }


        private void LoadTranslation_Click(object sender, RoutedEventArgs e)
        {
            //OpenFileDialog dialog = new()
            //{
            //    Filter = "XML files (*.xml)|*.xml"
            //};
            //if (dialog.ShowDialog() != true)
            //    return;
            //try
            //{
            //    string xml = File.ReadAllText(dialog.FileName, Encoding.GetEncoding(1251));
            //    if (LoadTranslationFromXML(xml)) MessageBox.Show(Locale.Get("loaded_from_file"), Locale.Get("translation"));
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show(Locale.Get("parse_file_fail"), Locale.Get("file_load_error"));
            //    Logger.Log("Translator-FileRead", $"Unhandled exception: {ex}");
            //}
        }

        private void LoadBufferTranslation_Click(object sender, RoutedEventArgs e)
        {
            //if (!Clipboard.ContainsText() || Clipboard.GetText().Trim() == "")
            //{
            //    MessageBox.Show(Locale.Get("clipboard_empty"), Locale.Get("translation"), MessageBoxButton.OK, MessageBoxImage.Warning);
            //    return;
            //}
            //if (LoadTranslationFromXML(Clipboard.GetText())) MessageBox.Show(Locale.Get("clipboard_loaded"), Locale.Get("translation"));
        }

        private bool LoadTranslationFromXML(string text)
        {
            //var translations = LoadStrings(text).ToDictionary(x => x.Id!);
            //if (translations.Count == 0)
            //{
            //    return false;
            //}
            //foreach (var item in (FilesList.SelectedItem as FileTab)!.Entries)
            //{
            //    if (translations.TryGetValue(item.Id!, out var tr) &&
            //        !string.IsNullOrWhiteSpace(tr.Eng))
            //    {
            //        item.NewEng = tr.Eng;
            //    }
            //    if (translations.TryGetValue(item.Id, out var tr2) &&
            //        !string.IsNullOrWhiteSpace(tr2.Ru))
            //    {
            //        item.NewRu = tr2.Ru;
            //    }
            //}
            //_currentView?.Refresh();
            return true;
        }

        private async void Commit_Click(object sender, RoutedEventArgs e)
        {
            //if (FilesList.SelectedItem is not FileTab file) return;
            //if (!file.HasApprovedChanges) return;
            //LoadingOverlay.Visibility = Visibility.Visible;
            //int version = await App.Current.SyncService.CompareVersions();
            //if (version == -1)
            //{
            //    LoadingOverlay.Visibility = Visibility.Hidden;
            //    MessageBox.Show(Locale.Get("server_unreachable"), Locale.Get("sync"));
            //    Title = Locale.Get("window_title", Locale.Get("not_connected"));
            //    StoreChanges(file, true);
            //    return;
            //}
            //if (version == -2)
            //{
            //    MessageBox.Show(Locale.Get("sync_version_higher"), Locale.Get("sync"));
            //}
            //if (version == App.Current.Settings.Version)
            //{
            //    Logger.Log("RestClient-Get", $"Before commit, program is up to date(with version: {App.Current.Settings.Version})");
            //    if (!(await SyncService.Upload(file)))
            //    {
            //        LoadingOverlay.Visibility = Visibility.Hidden;
            //        MessageBox.Show(Locale.Get("server_load_fail"), Locale.Get("sync"));
            //        Title = Locale.Get("window_title", Locale.Get("not_connected"));
            //        StoreChanges(file, true);
            //        return;
            //    }
            //    LoadingOverlay.Visibility = Visibility.Hidden;
            //    SaveChangesLocally(file);
            //    Title = Locale.Get("window_title", Locale.Get("connected", GetCurrentTimeHM()));
            //    return;
            //}
            //Logger.Log("RestClient-Get", $"Before commit, program was not up to date(with version: {App.Current.Settings.Version})");
            //List<DownloadedFile>? updates = await App.Current.SyncService.EditorSync(Files);
            //if (updates == null)
            //{
            //    LoadingOverlay.Visibility = Visibility.Hidden;
            //    MessageBox.Show(Locale.Get("server_update_fail"), Locale.Get("sync"));
            //    Title = Locale.Get("window_title", Locale.Get("not_connected"));
            //    StoreChanges(file, true);
            //    return;
            //}
            //App.Current.Settings.UpdateVersion(version);
            //Update(updates);
            //if (!file.HasApprovedChanges)
            //{
            //    LoadingOverlay.Visibility = Visibility.Hidden;
            //    MessageBox.Show(Locale.Get("all_translations_replaced"), Locale.Get("sync"));
            //    Title = Locale.Get("window_title", Locale.Get("connected", GetCurrentTimeHM()));
            //    return;
            //}
            //if (!(await SyncService.Upload(file)))
            //{
            //    LoadingOverlay.Visibility = Visibility.Hidden;
            //    MessageBox.Show(Locale.Get("server_load_fail"), Locale.Get("sync"));
            //    Title = Locale.Get("window_title", Locale.Get("not_connected"));
            //    StoreChanges(file, true);
            //    return;
            //}
            //LoadingOverlay.Visibility = Visibility.Hidden;
            //SaveChangesLocally(file);
            //Title = Locale.Get("window_title", Locale.Get("connected", GetCurrentTimeHM()));
            //return;
        }


        private void Update(List<DownloadedFile> updates)
        {
            //foreach (DownloadedFile dfile in updates)
            //{
            //    FileTab file = new FileTab("", dfile.Path, false);
            //    var seq = Files.Where(f => f.RelativePath == dfile.Path);
            //    if (seq == null || !seq.Any())
            //    {
            //        file.WriteToDisk(dfile.Entries);
            //        Files.Add(file);
            //        FilesList.ScrollIntoView(file);
            //        FilesList.UpdateLayout();
            //        continue;
            //    }
            //    FileTab bro = seq.First();
            //    foreach (StringEntry entry in dfile.Entries)
            //    {
            //        var entrySeq = bro.Entries.Where(e => e.Id == entry.Id);
            //        if (entrySeq == null || !seq.Any())
            //        {
            //            bro.Entries.Add(entry);
            //            continue;
            //        }
            //        StringEntry toChange = entrySeq.First();
            //        if (toChange.HasRuChanges && entry.downloadedRu && toChange.HasEngChanges && entry.downloadedEng)
            //            toChange.IsApproved = false;
            //        else if (toChange.HasRuChanges && entry.downloadedRu)
            //            toChange.IsApproved = false;
            //        else if (toChange.HasEngChanges && entry.downloadedEng)
            //            toChange.IsApproved = false;
            //        if (entry.downloadedRu)
            //        {
            //            toChange.Ru = entry.Ru;
            //            toChange.NewRu = entry.NewRu;
            //        }
            //        if (entry.downloadedEng)
            //        {
            //            toChange.Eng = entry.Eng;
            //            toChange.NewEng = entry.NewEng;
            //        }
            //    }
            //    WriteFile(bro);
            //    StoreChanges(bro, true);
            //}
        }

        private static void WriteFile(FileTab file)
        {
            //XDocument doc = XDocument.Load(file.FilePath);
            //var entries = file.Entries;
            //foreach (var entry in entries)
            //{
            //    var node = doc.Root!.Elements("string").FirstOrDefault(x => (string?)x.Attribute("id") == entry.Id);
            //    if (node == null)
            //    {
            //        continue;
            //    }
            //    var rus = node.Element("rus");
            //    string text1 = EncodeMultiline(entry.Ru);
            //    if (rus == null) node.Add(new XElement("rus", text1));
            //    else rus.Value = text1;
            //    var eng = node.Element("eng");
            //    string text2 = EncodeMultiline(entry.Eng);
            //    if (eng == null) node.Add(new XElement("eng", text2));
            //    else eng.Value = text2;

            //}
            //using var writer = XmlWriter.Create(file.FilePath, App.Current.XmlSettings);
            //doc.Save(writer);
        }


        private void SaveChangesLocally(FileTab file)
        {
            //foreach (StringEntry entry in file.Entries)
            //{
            //    if (entry.IsApproved)
            //    {
            //        entry.Ru = entry.NewRu;
            //        entry.Eng = entry.NewEng;
            //        entry.IsApproved = false;
            //    }
            //}
            //WriteFile(file);
            //StoreChanges(file, false);
        }

        private void StoreChanges(FileTab file, bool allowApprove)
        {
            //string filePath = AppDomain.CurrentDomain.BaseDirectory + "Changes/" + file.RelativePath.Replace(".xml", ".json");
            //string directory = Path.GetDirectoryName(filePath)!;
            //if (!Directory.Exists(directory))
            //{
            //    Directory.CreateDirectory(directory);
            //}
            //if (!File.Exists(filePath))
            //{
            //    File.WriteAllText(filePath, "[]", Encoding.GetEncoding(1251));
            //}
            //string json = File.ReadAllText(filePath, Encoding.GetEncoding(1251));
            //List<Change>? changes = JsonSerializer.Deserialize<List<Change>>(json, App.Current.JsonOptions);
            //changes ??= [];
            //foreach (StringEntry entry in file.Entries)
            //{
            //    var seq = changes.Where(c => c.Id == entry.Id);
            //    if (seq == null || !seq.Any())
            //    {
            //        if (!entry.HasChanges) continue;
            //        changes.Add(new Change(entry.Id, entry.NewRu, entry.NewEng, false));
            //        continue;
            //    }
            //    else
            //    {
            //        Change bro = seq.First();
            //        if (entry.HasChanges)
            //        {
            //            bro.Eng = entry.NewEng;
            //            bro.Ru = entry.NewRu;
            //            bro.IsApproved = allowApprove;
            //            continue;
            //        }
            //        changes.Remove(bro);
            //    }
            //}
            //if (changes.Count == 0)
            //{
            //    File.Delete(filePath);
            //}
            //else
            //{
            //    File.WriteAllText(filePath, JsonSerializer.Serialize(changes, App.Current.JsonOptions), Encoding.GetEncoding(1251));
            //}
        }


        private void FilterChanged(object sender, RoutedEventArgs e)
        {
            //_currentView?.Refresh();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            //searchText = SearchBox.Text;
            //_currentView?.Refresh();
            //FilesView.Refresh();
        }

        private async void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            //if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
            //{
            //    SearchBox.Focus();
            //    SearchBox.SelectAll();
            //    e.Handled = true;
            //}
            //if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
            //{
            //    if (FilesList.SelectedItem is FileTab tab)
            //    {
            //        if (tab.HasChanges) StoreChanges(tab, true);
            //    }
            //    e.Handled = true;
            //    await ShowStatusAsync(Locale.Get("file_saved"));
            //}
            //if (e.Key == Key.S && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
            //{
            //    foreach (var file in Files)
            //    {
            //        if (!file.HasChanges) continue;
            //        StoreChanges(file, true);
            //    }
            //    e.Handled = true;
            //    await ShowStatusAsync(Locale.Get("changes_saved"));
            //}
        }

        private void EditLongText(object sender, MouseButtonEventArgs e)
        {
            //if (sender is not DataGridCell cell)
            //    return;
            //if (cell.DataContext is not StringEntry entry)
            //    return;
            //if (entry.IsApproved)
            //    return;
            //if (cell.Column is not DataGridTextColumn column ||
            //    column.Binding is not Binding binding)
            //    return;

            //var property = typeof(StringEntry).GetProperty(binding.Path.Path);
            //if (property == null)
            //    return;
            //if (!property.CanWrite)
            //    return;

            //var dlg = new TextEditWindow(property.GetValue(entry)?.ToString() ?? "")
            //{
            //    Owner = this
            //};
            //if (dlg.ShowDialog() == true)
            //{
            //    property.SetValue(entry, dlg.ResultText);
            //}
            //e.Handled = true;
        }

        private void Approve_Click(object sender, RoutedEventArgs e)
        {
            //foreach (var item in Grid.SelectedItems)
            //{
            //    if (item is StringEntry entry && entry.HasChanges)
            //    {
            //        entry.IsApproved = !entry.IsApproved;
            //    }
            //}
        }

        private void FilesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //if (FilesList.SelectedItem is not FileTab file) return;
            //_currentView = CollectionViewSource.GetDefaultView(file.Entries);
            //_currentView.Filter = FilterItems;
            //Grid.ItemsSource = _currentView;
        }
        private bool FilterItems(object obj)
        {
            //if (obj is not StringEntry entry)
            //    return false;
            //if (HideApproved.IsChecked == true && entry.IsApproved)
            //    return false;
            //if (HideChanged.IsChecked == true && entry.HasChanges)
            //    return false;
            //if (HideUnchanged.IsChecked == true && !entry.HasChanges)
            //    return false;
            //if (!string.IsNullOrWhiteSpace(searchText))
            //{
            //    string s = searchText.ToLowerInvariant();
            //    if (!(entry.Id?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) &&
            //        !(entry.Ru?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) &&
            //        !(entry.Eng?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) &&
            //        !(entry.NewRu?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) &&
            //        !(entry.NewEng?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false))
            //    {
            //        return false;
            //    }
            //}

            return true;
        }

        private void Grid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            //if (e.Row.Item is StringEntry entry && entry.IsApproved)
            //{
            //    e.Cancel = true;
            //}
        }

        private async void SaveLocal_Click(object sender, RoutedEventArgs e)
        {
            //if (FilesList.SelectedItem is not FileTab tab) return;
            //e.Handled = true;
            //if (!tab.HasChanges) return;
            //StoreChanges(tab, true);
            //await ShowStatusAsync(Locale.Get("file_saved"));
        }

        private async void SaveAllLocal_Click(object sender, RoutedEventArgs e)
        {
            //e.Handled = true;
            //if (!Files.Where(f => f.HasChanges).Any()) return;
            //foreach (var file in Files)
            //{
            //    if (!file.HasChanges) continue;
            //    StoreChanges(file, true);
            //}
            //await ShowStatusAsync(Locale.Get("changes_saved"));
        }

        private async Task ShowStatusAsync(string text)
        {
            //Status.Text = text;
            //await Task.Delay(2000);
            //Status.Text = string.Empty;
        }

        private void DataGridCell_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //if (sender is DataGridCell cell && !cell.IsEditing && !cell.IsReadOnly)
            //{
            //    cell.Focus();
            //    if (Grid.BeginEdit(e))
            //    {
            //        e.Handled = true;
            //    }
            //    Grid.SelectedItem = cell.DataContext;
            //}
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            //var tb = (TextBox)sender;
            //tb.CaretIndex = tb.Text.Length;
            //tb.SelectionLength = 0;
        }

        private async void Download_Click(object sender, RoutedEventArgs e)
        {
            //LoadingOverlay.Visibility = Visibility.Visible;
            //int version = await App.Current.SyncService.CompareVersions();
            //if (version == -1)
            //{
            //    LoadingOverlay.Visibility = Visibility.Hidden;
            //    MessageBox.Show(Locale.Get("server_unreachable"), Locale.Get("sync"));
            //    Title = Locale.Get("window_title", Locale.Get("not_connected"));
            //    return;
            //}
            //if (version == App.Current.Settings.Version)
            //{
            //    LoadingOverlay.Visibility = Visibility.Hidden;
            //    MessageBox.Show(Locale.Get("sync_up_to_date"), Locale.Get("sync"));
            //    Title = Locale.Get("window_title", Locale.Get("connected", GetCurrentTimeHM()));
            //    return;
            //}
            //List<DownloadedFile>? updates = await App.Current.SyncService.EditorSync(Files);
            //if (updates == null)
            //{
            //    LoadingOverlay.Visibility = Visibility.Hidden;
            //    MessageBox.Show(Locale.Get("server_update_fail"), Locale.Get("sync"));
            //    Title = Locale.Get("window_title", Locale.Get("not_connected"));
            //    return;
            //}
            //App.Current.Settings.UpdateVersion(version);
            //Update(updates);
            //LoadingOverlay.Visibility = Visibility.Hidden;
            //MessageBox.Show(Locale.Get("synced"), Locale.Get("sync"));
            //Title = Locale.Get("window_title", Locale.Get("connected", GetCurrentTimeHM()));
        }
    }
}