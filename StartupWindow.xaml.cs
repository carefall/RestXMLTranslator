using RestXMLTranslator.Internals;
using System.Windows;
using System.Windows.Controls;

namespace RestXMLTranslator
{
    public partial class StartupWindow : Window
    {

        private static readonly HashSet<char> AllowedChars = ['.', ',', '-'];
        private readonly IProgress<string> _progress;

        public StartupWindow()
        {
            InitializeComponent();
            Logger.Setup();
            Locale.Init();
            _progress = new Progress<string>(SetSyncText);
            Title = Locale.Get("window_title", Locale.Get("startup"));
            ContinueButton.Content = Locale.Get("continue");
            string name = Settings.GetInstance().Name;
            if (name != "")
            {
                PrepareUpdate(name);
                _ = StartUpdate();
            }
            else
            {
                Text.Text = Locale.Get("enter_name");
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string name = NameBox.Text;
            PrepareUpdate(name);
            Settings.GetInstance().UpdateName(name);
            _ = StartUpdate();
        }

        private async Task StartUpdate()
        {
            SyncText.Visibility = Visibility.Visible;
            Logger.Log("Startup", "Performing update check...");
            var settings = Settings.GetInstance();
            int result = await RestClient.Check(settings.GameDataPath, settings.Version, _progress);
            if (result == -1)
            {
                Application.Current.Shutdown();
                return;
            }
            if (result == 1)
            {
                MessageBox.Show(Locale.Get("update_server_unreachable"), Locale.Get("sync"));
                Logger.Log("Startup", "Update check failed due to service unavailability. Moving to MainWindow");
                new MainWindow(false, settings.GameDataPath).Show();
            }
            else
            {
                Logger.Log("Startup", "Successful update check. Moving to MainWindow");
                new MainWindow(true, settings.GameDataPath).Show();
            }
            Close();
            return;
        }

        private void NameBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox textBox)
                return;
            var clean = new string([.. textBox.Text.Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c) || AllowedChars.Contains(c))]);
            if (textBox.Text != clean)
            {
                int caret = textBox.CaretIndex;
                textBox.Text = clean;
                textBox.CaretIndex = Math.Clamp(caret - 1, 0, clean.Length);
            }
            ContinueButton.IsEnabled = clean.Length > 2;
        }

        private void SetSyncText(string text)
        {
            SyncText.Text = text;
        }

        private void PrepareUpdate(string name)
        {
            ContinueButton.IsEnabled = false;
            ContinueButton.Visibility = Visibility.Hidden;
            NameBox.Visibility = Visibility.Hidden;
            Circle.Visibility = Visibility.Visible;
            Text.Text = Locale.Get("welcome", name);
        }

    }
}
