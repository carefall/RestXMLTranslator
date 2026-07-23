using RestXMLTranslator.Internals.Models;
using RestXMLTranslator.Internals.Program;
using System.Windows;
using System.Windows.Input;

namespace RestXMLTranslator
{
    public partial class MainWindow : Window
    {

        private Dictionary<string, FileInfo>? files = null;

        public MainWindow(bool online, Dictionary<string, FileInfo>? files)
        {
            InitializeComponent();
            this.files = files;
            Title = Locale.Get("window_title", online ? Locale.Get("connected", GetCurrentTimeHM()) : Locale.Get("not_connected"));
        }


        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            App.Current.MainWindow = this;
            Files.Files.Clear();
            foreach (var file in await App.Current.LocalFiles.ReadLocalFiles())
            {
                Files.Files.Add(file);
            }
            if (files != null)
            {
                foreach (var file in files)
                {
                    FileTab? ft = Files.Files.First(f => f.RelativePath == file.Key);
                    if (ft == null) continue;
                    ft.Finished = file.Value.Finished;
                }
            }
            Files.FilesView?.Refresh();
            Files.FilesList.SelectedIndex = 0;
        }

        public static string GetCurrentTimeHM() => DateTime.Now.ToString("HH:mm");

        public async Task Download()
        {
            WindowBlocker.Visibility = Visibility.Visible;
            int version = await App.Current.SyncService.CompareVersions();
            if (version == -1)
            {
                WindowBlocker.Visibility = Visibility.Hidden;
                MessageBox.Show(Locale.Get("server_unreachable"), Locale.Get("sync"), MessageBoxButton.OK, MessageBoxImage.Error);
                Title = Locale.Get("window_title", Locale.Get("not_connected"));
                return;
            }
            if (version == -2)
            {
                WindowBlocker.Visibility = Visibility.Hidden;
                MessageBox.Show(Locale.Get("server_broken"), Locale.Get("sync"), MessageBoxButton.OK, MessageBoxImage.Error);
                Title = Locale.Get("window_title", Locale.Get("not_connected"));
                return;
            }
            if (version == App.Current.Settings.Version)
            {
                WindowBlocker.Visibility = Visibility.Hidden;
                MessageBox.Show(Locale.Get("sync_up_to_date"), Locale.Get("sync"));
                Title = Locale.Get("window_title", Locale.Get("connected", GetCurrentTimeHM()));
                return;
            }
            await Files.SaveAll(false);
            WindowBlocker.Visibility = Visibility.Visible;
            (SyncResult syncResult, Dictionary<string, FileInfo>? fileResult) = await App.Current.SyncService.EditorSync();
            if (fileResult != null) files = fileResult;
            if (files != null)
            {
                foreach (var file in files)
                {
                    FileTab? ft = Files.Files.First(f => f.RelativePath == file.Key);
                    if (ft == null) continue;
                    ft.Finished = file.Value.Finished;
                }
            }
            if (syncResult == SyncResult.Inactive)
            {
                WindowBlocker.Visibility = Visibility.Hidden;
                MessageBox.Show(Locale.Get("get_not_allowed"), Locale.Get("sync"), MessageBoxButton.OK, MessageBoxImage.Warning);
                Title = Locale.Get("window_title", Locale.Get("not_connected"));
                return;
            }
            if (syncResult == SyncResult.ServerUnavailable)
            {
                WindowBlocker.Visibility = Visibility.Hidden;
                MessageBox.Show(Locale.Get("server_unreachable"), Locale.Get("sync"), MessageBoxButton.OK, MessageBoxImage.Error);
                Title = Locale.Get("window_title", Locale.Get("not_connected"));
                return;
            }
            if (syncResult == SyncResult.OldApp)
            {
                WindowBlocker.Visibility = Visibility.Hidden;
                MessageBox.Show(Locale.Get("old_app_get"), Locale.Get("sync"), MessageBoxButton.OK, MessageBoxImage.Error);
                Title = Locale.Get("window_title", Locale.Get("not_connected"));
                return;
            }
            if (syncResult == SyncResult.Other)
            {
                WindowBlocker.Visibility = Visibility.Hidden;
                MessageBox.Show(Locale.Get("server_update_fail"), Locale.Get("sync"), MessageBoxButton.OK, MessageBoxImage.Error);
                Title = Locale.Get("window_title", Locale.Get("not_connected"));
                return;
            }
            App.Current.Settings.UpdateVersion(version);
            WindowBlocker.Visibility = Visibility.Hidden;
            MessageBox.Show(Locale.Get("synced"), Locale.Get("sync"));
            Title = Locale.Get("window_title", Locale.Get("connected", GetCurrentTimeHM()));
            await Reload();
        }

        public async Task Commit()
        {
            if (Files.FilesList.SelectedItem is not FileTab tab) return;
            if (!tab.HasApprovedChanges) return;
            WindowBlocker.Visibility = Visibility.Visible;
            int version = await App.Current.SyncService.CompareVersions();
            if (version == -1)
            {
                WindowBlocker.Visibility = Visibility.Hidden;
                MessageBox.Show(Locale.Get("server_unreachable"), Locale.Get("sync"), MessageBoxButton.OK, MessageBoxImage.Error);
                Title = Locale.Get("window_title", Locale.Get("not_connected"));
                return;
            }
            if (version == -2)
            {
                WindowBlocker.Visibility = Visibility.Hidden;
                MessageBox.Show(Locale.Get("server_broken"), Locale.Get("sync"), MessageBoxButton.OK, MessageBoxImage.Error);
                Title = Locale.Get("window_title", Locale.Get("not_connected"));
                return;
            }
            if (version > App.Current.Settings.Version)
            {
                WindowBlocker.Visibility = Visibility.Hidden;
                MessageBox.Show(Locale.Get("update_first"), Locale.Get("sync"), MessageBoxButton.OK, MessageBoxImage.Warning);
                Title = Locale.Get("window_title", Locale.Get("not_connected"));
                return;
            }
            switch (await App.Current.SyncService.Commit(tab))
            {
                case SyncResult.OldApp:
                    WindowBlocker.Visibility = Visibility.Hidden;
                    MessageBox.Show(Locale.Get("old_app_post"), Locale.Get("sync"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    Title = Locale.Get("window_title", Locale.Get("not_connected"));
                    return;
                case SyncResult.Inactive:
                    WindowBlocker.Visibility = Visibility.Hidden;
                    MessageBox.Show(Locale.Get("post_not_allowed"), Locale.Get("sync"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    Title = Locale.Get("window_title", Locale.Get("not_connected"));
                    return;
                case SyncResult.ServerUnavailable:
                    WindowBlocker.Visibility = Visibility.Hidden;
                    MessageBox.Show(Locale.Get("commit_fail"), Locale.Get("sync"), MessageBoxButton.OK, MessageBoxImage.Error);
                    Title = Locale.Get("window_title", Locale.Get("not_connected"));
                    return;
            }
            App.Current.LocalFiles.StoreChanges(tab);
            await App.Current.LocalFiles.ApplyApprovedChanges(tab);
            WindowBlocker.Visibility = Visibility.Hidden;
            MessageBox.Show(Locale.Get("synced"), Locale.Get("sync"));
            Title = Locale.Get("window_title", Locale.Get("connected", GetCurrentTimeHM()));
            if (files != null)
            {
                KeyValuePair<string, FileInfo>? info = files.First(f => f.Key == tab.RelativePath);
                info?.Value.Finished = false;
            }
            await Reload();
        }

        private async Task Reload()
        {
            string tabName = "";
            string entryId = "";
            if (Files.FilesList.SelectedItem is FileTab tab)
            {
                tabName = tab.RelativePath;
            }
            if (TranslationGrid.TGrid.SelectedItem is StringEntry entry)
            {
                entryId = entry.Id;
            }
            Files.Files.Clear();
            FileTab? snapTo = null;
            foreach (var file in await App.Current.LocalFiles.ReadLocalFiles())
            {
                Files.Files.Add(file);
                if (file.RelativePath == tabName)
                {
                    file.SelectedEntry = entryId;
                    snapTo = file;
                }
            }
            if (files != null)
            {
                foreach (var file in files)
                {
                    FileTab? ft = Files.Files.First(f => f.RelativePath == file.Key);
                    if (ft == null) continue;
                    ft.Finished = file.Value.Finished;
                }
            }
            Files.FilesView?.Refresh();
            if (snapTo == null)
            {
                Files.FilesList.SelectedIndex = 0;
                return;
            }
            Files.FilesList.SelectedItem = snapTo;
        }

        private async void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
            {
                Buttons.SearchBox.Focus();
                Buttons.SearchBox.SelectAll();
                e.Handled = true;
            }
            if (e.Key == Key.F && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
            {
                e.Handled = true;
                OpenAdvancedSearch();
            }
            if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (!Buttons.SaveFile.IsEnabled) return;
                Buttons.SaveAll.IsEnabled = false;
                Buttons.SaveFile.IsEnabled = false;
                if (Files.SaveFile())
                {
                    await Buttons.ShowStatusAsync(Locale.Get("changes_saved"), true);
                }
                Buttons.SaveAll.IsEnabled = true;
                Buttons.SaveFile.IsEnabled = true;
            }
            if (e.Key == Key.S && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
            {
                if (!Buttons.SaveAll.IsEnabled) return;
                Buttons.SaveAll.IsEnabled = false;
                Buttons.SaveFile.IsEnabled = false;
                if (await Files.SaveAll(true))
                {
                    await Buttons.ShowStatusAsync(Locale.Get("changes_saved"), false);
                }
                Buttons.SaveAll.IsEnabled = true;
                Buttons.SaveFile.IsEnabled = true;
            }
            if (e.Key == Key.Escape)
            {
                TranslationGrid.TGrid.UnselectAll();
            }
        }

        public void OpenAdvancedSearch() => new SearchWindow() { Owner = this }.Show();

        public void NavigateTo(FileTab file, string entry)
        {
            Files.SearchBox.Text = "";
            Buttons.SearchBox.Text = "";
            Files.HideChanged.IsChecked = false;
            Files.HideFinished.IsChecked = false;
            Files.HideUnchanged.IsChecked = false;
            Files.HideUnfinished.IsChecked = false;
            Buttons.HideApproved.IsChecked = false;
            Buttons.HideChanged.IsChecked = false;
            Buttons.HideUnchanged.IsChecked = false;
            Files.FilesView?.Refresh();
            TranslationGrid.EntriesView?.Refresh();
            file.SelectedEntry = entry;
            Files.FilesList.SelectedItem = file;
        }

    }
}