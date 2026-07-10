using RestXMLTranslator.Internals.Models;
using RestXMLTranslator.Internals.Program;
using System.Windows;
using System.Windows.Controls;

namespace RestXMLTranslator
{
    public partial class StartupWindow : Window
    {

        private static readonly HashSet<char> AllowedChars = ['.', ',', '-'];
        private IProgress<string>? _progress;

        public StartupWindow()
        {
            InitializeComponent();
            Title = Locale.Get("window_title", Locale.Get("startup"));
            ContinueButton.Content = Locale.Get("continue");
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string name = NameBox.Text;
            PrepareUpdate(name);
            App.Current.Settings.UpdateName(name);
            StartUpdate();
        }

        private async void StartUpdate()
        {
            var settings = App.Current.Settings;
            SyncText.Visibility = Visibility.Visible;
            Logger.Log("Startup", "Performing update check...");
            SyncResult result = await App.Current.SyncService.StartupSync(settings.GameDataPath, settings.Version, _progress);
            if (result != SyncResult.Success && result != SyncResult.ServerUnavailable)
            {
                MessageBox.Show(Locale.Get(result == SyncResult.ClientVersionHigher ? "sync_version_higher" : "sync_fail"), Locale.Get("sync"));
                Application.Current.Shutdown();
                return;
            }
            if (result == SyncResult.ServerUnavailable)
            {
                MessageBox.Show(Locale.Get("update_server_unreachable"), Locale.Get("sync"));
                Logger.Log("Startup", "Update check failed due to service unavailability. Moving to MainWindow");
                new MainWindow(false).Show();
            }
            else
            {
                Logger.Log("Startup", "Successful update check. Moving to MainWindow");
                new MainWindow(true).Show();
            }
            Close();
            return;
        }

        private void NameBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox textBox) return;
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            string name = App.Current.Settings.Name;
            _progress = new Progress<string>(SetSyncText);
            if (name != "")
            {
                PrepareUpdate(name);
                StartUpdate();
            }
            else
            {
                Text.Text = Locale.Get("enter_name");
            }
        }
    }
}
