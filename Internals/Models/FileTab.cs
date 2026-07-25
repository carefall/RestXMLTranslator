using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;

namespace RestXMLTranslator.Internals.Models
{
    public class FileTab : INotifyPropertyChanged
    {

        public string SelectedEntry { get; set; } = "";

        public string Name { get; set; } = "";
        public string FilePath { get; set; } = "";

        public string RelativePath { get; set; } = "";

        public string Tip { get; set; } = "";

        private bool finished = false;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool Finished {
            get => finished;
            set
            {
                finished = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<StringEntry> Entries { get; set; } = [];

        public FileTab(string path, string relativePath)
        {
            FilePath = path;
            RelativePath = relativePath;
            Tip = RelativePath;
            Name = Path.GetFileName(path);
            Finished = App.Current.Settings.GetFileStatus(relativePath);
        }

        public void Read()
        {
            Entries = App.Current.LocalFiles.Read(FilePath);
        }

        public bool HasApprovedChanges => Entries.Where(e => e.IsApproved).Any();

        public bool HasChanges => Entries.Where(e => e.HasChanges).Any();
    }
}
