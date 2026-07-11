using System.Windows;

namespace RestXMLTranslator
{
    public partial class LanguageWindow : Window
    {

        public bool IsEnglish { get; set; } = false;

        public LanguageWindow()
        {
            InitializeComponent();
        }

        private void Russian_Click(object sender, RoutedEventArgs e)
        {
            IsEnglish = false;
            DialogResult = true;
            Close();
        }

        private void English_Click(object sender, RoutedEventArgs e)
        {
            IsEnglish = true;
            DialogResult = true;
            Close();
        }

    }
}
