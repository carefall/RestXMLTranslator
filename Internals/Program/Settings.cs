using Microsoft.Win32;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace RestXMLTranslator.Internals.Program
{
    public class Settings
    {
        public string GameDataPath { get; private set; } = "";
        public string Name { get; private set; } = "";
        public int Version { get; private set; }

        private readonly string SettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

        public Settings()
        {
            try
            {
                Logger.Log("Settings", "Initializing settings...");
                if (!File.Exists(SettingsPath))
                {
                    Logger.Log("Settings", "Settings file not found. Creating...");
                    Save();
                }
                Load();
                if (string.IsNullOrWhiteSpace(GameDataPath))
                {
                    SelectGameDataFolder();
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Settings", $"Unhandled exception: {ex}");
                MessageBox.Show(Locale.Get("gamedata_exception"), Locale.Get("settings"), MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
        }

        private void Load()
        {
            string json = File.ReadAllText(SettingsPath);
            try
            {
                using JsonDocument doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                GameDataPath = root.TryGetProperty("gamedata-path", out var path) ? path.GetString() ?? "" : "";
                Name = root.TryGetProperty("name", out var name) ? name.GetString() ?? "" : "";
                Version = root.TryGetProperty("version", out var version) ? version.GetInt32() : 0;
            }
            catch (JsonException ex)
            {
                Logger.Log("Settings", $"Invalid settings JSON: {ex}");
                try
                {
                    File.Delete(SettingsPath);
                }
                catch (Exception deleteEx)
                {
                    Logger.Log("Settings", $"Unable to delete invalid settings: {deleteEx}");
                }
                GameDataPath = "";
                Name = "";
                Version = 0;
                Save();
            }
        }

        public void UpdateVersion(int version)
        {
            Version = version;
            Save();
            Logger.Log("Settings", $"Updated version to {version} after installing update...");
        }


        public void UpdateName(string name)
        {
            Name = name;
            Save();
            Logger.Log("Settings", $"User selected name: {name}.");
        }

        private void SelectGameDataFolder()
        {
            Logger.Log("Settings", "GameData path not found...");
            var dialog = new OpenFolderDialog
            {
                Title = Locale.Get("select_gamedata"),
                InitialDirectory = @"C:\"
            };
            MessageBox.Show("START");
            while (dialog.ShowDialog() != true)
            {
                var result = MessageBox.Show(Locale.Get("select_gamedata_dialog"), Locale.Get("settings"), MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
                if (result == MessageBoxResult.No)
                {
                    Application.Current.Shutdown();
                    return;
                }
            }
            MessageBox.Show("MID");
            GameDataPath = Path.GetFullPath(dialog.FolderName);
            MessageBox.Show(GameDataPath);
            try
            {
                Directory.CreateDirectory(Path.Combine(GameDataPath, "gamedata", "configs"));
                Save();
            }
            catch (Exception ex)
            {
                Logger.Log("Settings", $"Unhandled exception on gamedata folder creation: {ex}");
                MessageBox.Show(Locale.Get("gamedata_creation_failed"), Locale.Get("settings"));
                Application.Current.Shutdown();
            }
            Logger.Log("Settings", $"GameData path selected: {GameDataPath}");
            return;
        }

        private void Save()
        {
            var config = new Dictionary<string, object>
            {
                ["gamedata-path"] = GameDataPath,
                ["name"] = Name,
                ["version"] = Version
            };
            string json = JsonSerializer.Serialize(config, App.Current.JsonOptions);
            File.WriteAllText(SettingsPath, json);
        }
    }
}