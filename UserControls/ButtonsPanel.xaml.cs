using Microsoft.Win32;
using RestXMLTranslator.Internals.Models;
using RestXMLTranslator.Internals.Program;
using System.Windows;
using System.Windows.Controls;

namespace RestXMLTranslator.UserControls
{
    public partial class ButtonsPanel : UserControl
    {
        private CancellationTokenSource? _searchCancellation;

        private string _searchText = "";

        public string SearchText { get => _searchText; set => _searchText = value; }

        public ButtonsPanel()
        {
            InitializeComponent();
        }

        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SearchText = SearchBox.Text;
            _searchCancellation?.Cancel();
            _searchCancellation = new CancellationTokenSource();
            try
            {
                await Task.Delay(250, _searchCancellation.Token);
                App.Current.MWindow.TranslationGrid.EntriesView?.Refresh();
            }
            catch (TaskCanceledException) { }
        }

        private async void Commit_Click(object sender, RoutedEventArgs e)
        {
            SetButtonsState(false);
            await App.Current.MWindow.Commit();
            SetButtonsState(true);
        }

        private void SetButtonsState(bool enabled)
        {
            SaveAll.IsEnabled = enabled;
            SaveFile.IsEnabled = enabled;
            Download.IsEnabled = enabled;
            Commit.IsEnabled = enabled;
            LoadBuffer.IsEnabled = enabled;
            LoadFile.IsEnabled = enabled;
        }

        private async void Download_Click(object sender, RoutedEventArgs e)
        {
            SetButtonsState(false);
            await App.Current.MWindow.Download();
            SetButtonsState(true);
        }

        private async void SaveFile_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            SetButtonsState(false);
            if (App.Current.MWindow.Files.SaveFile())
                await ShowStatusAsync(Locale.Get("changes_saved"), true);
            SetButtonsState(true);
        }

        private async void SaveAll_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            SetButtonsState(false);
            if (await App.Current.MWindow.Files.SaveAll(true))
                await ShowStatusAsync(Locale.Get("changes_saved"), false);
            SetButtonsState(true);
        }

        private void LoadBuffer_Click(object sender, RoutedEventArgs e)
        {
            if (!Clipboard.ContainsText() || Clipboard.GetText().Trim() == "")
            {
                MessageBox.Show(Locale.Get("clipboard_empty"), Locale.Get("translation"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            LoadTranslationFromXML(Clipboard.GetText(), false);
        }

        private void LoadTranslationFromXML(string text, bool file)
        {
            var translations = XMLHelper.LoadStrings(text).ToDictionary(x => x.Id!);
            App.Current.MWindow.TranslationGrid.InsertTranslations(translations, file);
        }

        private void LoadFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new()
            {
                Filter = "XML files (*.xml)|*.xml"
            };
            if (dialog.ShowDialog() != true) return;
            string? xml = App.Current.LocalFiles.LoadFileText(dialog.FileName);
            if (xml == null) return;
            LoadTranslationFromXML(xml, true);
        }

        private void FilterChanged(object sender, RoutedEventArgs e)
        {
            App.Current.MWindow.TranslationGrid.EntriesView?.Refresh();
        }

        public async Task ShowStatusAsync(string text, bool single)
        {
            if (single)
            {
                SaveFile.Content = Locale.Get("all_saved");
            }
            else
            {
                SaveAll.Content = Locale.Get("file_saved");
            }
            await Task.Delay(1000);
            SaveAll.IsEnabled = true;
            SaveFile.IsEnabled = true;
            if (single)
            {
                SaveFile.Content = Locale.Get("btn_save_all");
            }
            else
            {
                SaveAll.Content = Locale.Get("btn_save_file");
            }
        }

        public void Theme_Click(object sender, RoutedEventArgs e)
        {
            App.Current.SwitchTheme();
        }
    }
}
