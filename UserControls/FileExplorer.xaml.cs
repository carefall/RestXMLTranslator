using RestXMLTranslator.Internals.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace RestXMLTranslator.UserControls
{
    public partial class FileExplorer : UserControl
    {

        private CancellationTokenSource? _searchCancellation;
        private string _searchText = "";
        public ObservableCollection<FileTab> Files { get; } = [];

        public ICollectionView? FilesView { get; private set; }

        public FileExplorer()
        {
            InitializeComponent();
            FilesView = CollectionViewSource.GetDefaultView(Files);
            FilesView.Filter = FilterFile;
            DataContext = this;
        }

        private bool FilterFile(object obj)
        {
            if (obj is not FileTab file) return false;
            if (HideChanged.IsChecked == true && file.HasChanges) return false;
            if (HideUnchanged.IsChecked == true && !file.HasChanges) return false;
            if (HideFinished.IsChecked == true && file.Finished) return false;
            if (HideUnfinished.IsChecked == true && !file.Finished) return false;
            if (string.IsNullOrWhiteSpace(_searchText)) return true;
            return file.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase);
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchText = SearchBox.Text;
            RefreshSearch();
        }

        private async void RefreshSearch()
        {
            _searchCancellation?.Cancel();
            _searchCancellation = new CancellationTokenSource();
            try
            {
                await Task.Delay(150, _searchCancellation.Token);
                FilesView?.Refresh();
            }
            catch (TaskCanceledException) { }
        }

        private void FilesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FilesList.SelectedItem is not FileTab tab) return;
            App.Current.MWindow.TranslationGrid.LoadFile(tab);
        }

        public async Task<bool> SaveAll(bool allowApprove)
        {
            if (!Files.Any(f => f.HasChanges)) return false;
            App.Current.MWindow.WindowBlocker.Visibility = Visibility.Visible;
            foreach (var file in Files)
            {
                if (!file.HasChanges) continue;
                await App.Current.LocalFiles.StoreChanges(file, allowApprove);
            }
            App.Current.MWindow.WindowBlocker.Visibility = Visibility.Hidden;
            return true;
        }

        public async Task<bool> SaveFile()
        {
            if (FilesList.SelectedItem is not FileTab tab) return false;
            if (!tab.HasChanges) return false;
            await App.Current.LocalFiles.StoreChanges(tab, true);
            return true;
        }

        private void MenuItem_Click_Text(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem) return;
            if (menuItem.Parent is not ContextMenu contextMenu) return;
            if (contextMenu.PlacementTarget is not FrameworkElement element) return;
            if (element.DataContext is not FileTab file) return;
            Process.Start(new ProcessStartInfo
            {
                FileName = file.FilePath,
                UseShellExecute = true
            });
        }

        private void FileFilterChanged(object sender, RoutedEventArgs e)
        {
            FilesView?.Refresh();
        }

        private void MenuItem_Click_File(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem) return;
            if (menuItem.Parent is not ContextMenu contextMenu) return;
            if (contextMenu.PlacementTarget is not FrameworkElement element) return;
            if (element.DataContext is not FileTab file) return;
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{file.FilePath}\"",
                UseShellExecute = true
            });
        }
    }
}
