using RestXMLTranslator.Internals.Models;
using RestXMLTranslator.Internals.Program;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
        public ICollectionView FilesView { get; private set; }

        public FileExplorer()
        {
            InitializeComponent();
            FilesView = CollectionViewSource.GetDefaultView(Files);
            FilesView.Filter = FilterFile;
            DataContext = this;
        }

        private bool FilterFile(object obj)
        {
            if (obj is not FileTab file)
                return false;
            if (string.IsNullOrWhiteSpace(_searchText))
                return true;
            return file.RelativePath.Contains(_searchText, StringComparison.OrdinalIgnoreCase);
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
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
                await Task.Delay(250, _searchCancellation.Token);
                FilesView.Refresh();
            }
            catch (TaskCanceledException) { }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Files = App.Current.LocalFiles.ReadLocalFiles();
            FilesView = CollectionViewSource.GetDefaultView(Files);
            FilesView.Filter = FilterFile;
            FilesView.Refresh();
            if (Files.Count == 0)
            {
                MessageBox.Show(Locale.Get("no_files_found"), Locale.Get("translation"), MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
                return;
            }
        }
    }
}
