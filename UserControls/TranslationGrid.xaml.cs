using RestXMLTranslator.Internals.Models;
using RestXMLTranslator.Internals.Program;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace RestXMLTranslator.UserControls
{
    public partial class TranslationGrid : UserControl, INotifyPropertyChanged
    {

        private ObservableCollection<StringEntry> _entries = [];

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<StringEntry> Entries
        {
            get => _entries; set
            {
                _entries = value;
                OnPropertyChanged();
                EntriesView?.Refresh();
            }
        }

        public ICollectionView? EntriesView;

        public TranslationGrid()
        {
            InitializeComponent();
            EntriesView = CollectionViewSource.GetDefaultView(Entries);
            EntriesView.Filter = FilterEntries;
            DataContext = this;
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool FilterEntries(object obj)
        {
            if (obj is not StringEntry entry) return false;
            var buttons = App.Current.MWindow.Buttons;
            if (buttons.HideApproved.IsChecked == true && entry.IsApproved) return false;
            if (buttons.HideChanged.IsChecked == true && entry.HasChanges) return false;
            if (buttons.HideUnchanged.IsChecked == true && !entry.HasChanges) return false;
            if (!string.IsNullOrWhiteSpace(buttons.SearchText))
            {
                string s = buttons.SearchText.ToLowerInvariant();
                return ContainTextAny([entry.Id, entry.NewRu, entry.NewEng, entry.Ru, entry.Eng], s);
            }
            return true;
        }

        private bool ContainTextAny(string[] strings, string s)
        {
            return strings.Where(str => str.Contains(s, StringComparison.OrdinalIgnoreCase)).Any();
        }


        internal void LoadFile(FileTab tab)
        {
            StringEntry? targetEntry = null;
            Entries.Clear();
            foreach (var entry in tab.Entries)
            {
                Entries.Add(entry);
                if (tab.SelectedEntry != "" && tab.SelectedEntry == entry.Id) targetEntry = entry;
            }
            if (targetEntry != null)
            {
                TGrid.SelectedItem = targetEntry;
                TGrid.ScrollIntoView(targetEntry);
            }

        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var tb = (TextBox)sender;
            tb.CaretIndex = tb.Text.Length;
            tb.SelectionLength = 0;
        }

        private void TGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            if (e.Row.Item is StringEntry entry && entry.IsApproved)
            {
                e.Cancel = true;
            }
        }

        private void EditLongText(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;
            if (button.DataContext is not StringEntry entry)
                return;
            if (entry.IsApproved)
                return;
            if (button.Tag is not string propertyName)
                return;
            var property = typeof(StringEntry).GetProperty(propertyName);
            if (property == null || !property.CanWrite)
                return;
            var opposite = propertyName == "NewEng"? entry.NewRu : entry.NewEng;
            var dlg = new TextEditWindow(property.GetValue(entry)?.ToString() ?? "", opposite)
            {
                Owner = Window.GetWindow(this)
            };

            if (dlg.ShowDialog() == true)
            {
                property.SetValue(entry, dlg.ResultText);
            }

            e.Handled = true;
        }

        private void Approve_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in TGrid.SelectedItems)
            {
                if (item is StringEntry entry && entry.HasChanges)
                {
                    entry.IsApproved = !entry.IsApproved;
                }
            }
        }

        public void InsertTranslations(Dictionary<string, StringEntry> translations, bool file)
        {
            if (translations.Count == 0) return;
            var window = new TranslationSelectWindow
            {
                Owner = Application.Current.MainWindow
            };
            if (window.ShowDialog() != true)
                return;
            var type = window.Result;
            foreach (var item in Entries)
            {
                if (!translations.TryGetValue(item.Id!, out var tr)) continue;
                item.HasNewLine = tr.HasNewLine;
                if (!string.IsNullOrWhiteSpace(tr.Eng) && (type == ImportType.English || type == ImportType.All || type == ImportType.Localization))
                {
                    item.NewEng = tr.Eng;
                }
                if (!string.IsNullOrWhiteSpace(tr.Ru) && (type == ImportType.Russian || type == ImportType.All || type == ImportType.Localization))
                {
                    item.NewRu = tr.Ru;
                }
                if (!string.IsNullOrWhiteSpace(tr.Comment) && (type == ImportType.Comments || type == ImportType.All))
                {
                    item.NewComment = tr.Comment;
                }
            }
            EntriesView?.Refresh();
            MessageBox.Show(Locale.Get(file ? "loaded_from_file" : "clipboard_loaded"), Locale.Get("translation"));
        }

        private void TextBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is not TextBox textBox) return;
            if (VisualTreeHelper.GetChild(textBox, 0) is not Decorator decorator || decorator.Child is not ScrollViewer scrollViewer) return;
            bool canScrollUp = scrollViewer.VerticalOffset > 0;
            bool canScrollDown = scrollViewer.VerticalOffset < scrollViewer.ScrollableHeight;
            if ((e.Delta > 0 && canScrollUp) || (e.Delta < 0 && canScrollDown)) return;
            e.Handled = true;
        }

        public static T? FindParent<T>(DependencyObject? child) where T : DependencyObject
        {
            while (child != null)
            {
                child = VisualTreeHelper.GetParent(child);
                if (child is T parent) return parent;
            }
            return null;
        }

        private void ChangeStatus_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox cb) return;
            if (cb.DataContext is not StringEntry entry) return;
            if (entry.IsApproved) return;
            entry.HasNewLine = true;
        }

        private void ChangeStatus_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox cb) return;
            if (cb.DataContext is not StringEntry entry) return;
            if (entry.IsApproved) return;
            entry.HasNewLine = false;
        }
    }
}
