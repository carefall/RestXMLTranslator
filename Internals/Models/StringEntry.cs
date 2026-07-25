using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RestXMLTranslator.Internals.Models
{
    public class StringEntry : INotifyPropertyChanged, IEntry
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string _ru = "";
        public string Ru
        {
            get => _ru; set
            {
                _ru = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasRuChanges));
                OnPropertyChanged(nameof(HasChanges));
            }
        }

        private string _eng = "";
        public string Eng
        {
            get => _eng; set
            {
                _eng = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasEngChanges));
                OnPropertyChanged(nameof(HasChanges));
            }
        }

        private string _newEng = "";
        public string NewEng
        {
            get => _newEng;
            set
            {
                _newEng = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasEngChanges));
                OnPropertyChanged(nameof(HasChanges));
            }
        }

        private string _newRu = "";

        public string NewRu
        {
            get => _newRu;
            set
            {
                _newRu = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasRuChanges));
                OnPropertyChanged(nameof(HasChanges));
            }
        }

        private bool _isApproved;

        public bool IsApproved
        {
            get => _isApproved;
            set
            {
                _isApproved = value;
                OnPropertyChanged();
            }
        }

        public bool HadNewLine { get; set; } = false;

        private bool hasNewLine;
        
        public bool HasNewLine
        {
            get => hasNewLine;
            set
            {
                hasNewLine = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasNewLineChanges));
                OnPropertyChanged(nameof(HasChanges));
            }
        }

        public bool HasNewLineChanges => HasNewLine != HadNewLine;

        public bool HasChanges => HasRuChanges || HasEngChanges || HasCommentChanges || HasNewLineChanges;

        public bool HasRuChanges => Ru != NewRu;

        public bool HasEngChanges => Eng != NewEng;

        public bool HasCommentChanges => Comment != NewComment;

        public bool downloadedRu, downloadedEng, downloadedComment;

        private string _comment = "";
        public string Comment { get => _comment;
            set
            {
                _comment = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasCommentChanges));
                OnPropertyChanged(nameof(HasChanges));
            } 
        }

        private string _newComment = "";
        public string NewComment
        {
            get => _newComment;
            set
            {
                _newComment = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasCommentChanges));
                OnPropertyChanged(nameof(HasChanges));
            }
        }
        public string Id { get; set; } = "";

        public override string ToString()
        {
            return $"Entry {Id}\n" + $"New Line: {HadNewLine}\n" + $"Comment: {Comment}\n" + $"Ru: {Ru}\n" + $"Eng: {Eng}\n" + $"Approved: {IsApproved}";
        }
    }
}
