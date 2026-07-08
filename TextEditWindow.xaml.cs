using RestXMLTranslator.Internals;
using System.Windows;
using System.Windows.Input;

namespace RestXMLTranslator
{
    public partial class TextEditWindow : Window
    {
        public string ResultText => Editor.Text;

        public TextEditWindow(string text)
        {
            InitializeComponent();
            Editor.Text = text;
            Title = Locale.Get("enter_text");
            Cancel.Content = Locale.Get("btn_cancel");
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Editor.Focus();
            Editor.CaretIndex = Editor.Text.Length;
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
            {
                DialogResult = true;
            }
        }
    }
}
