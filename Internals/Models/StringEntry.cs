using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace RestXMLTranslator.Internals.Models
{
    public class StringEntry : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string Id { get; set; } = "";

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

        public bool HasChanges => HasRuChanges || HasEngChanges;

        public bool HasRuChanges => Ru != NewRu;

        public bool HasEngChanges => Eng != NewEng;

        public bool downloadedRu, downloadedEng;

    }
}
