using Microsoft.Win32;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Windows;

namespace RestXMLTranslator.Internals
{
    internal class Settings
    {
        public static Action? OnUserDeclined;
        public string gamedataPath = "";
        public string name = "";
        public int version = 0;
        private readonly JsonSerializerOptions options = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        private static Settings? instance;

        public static Settings GetInstance()
        {
            instance ??= new();
            return instance;
        }

        public void UpdateVersion(int version)
        {
            string settingsPath = AppDomain.CurrentDomain.BaseDirectory + "settings.json";
            this.version = version;
            var config = new Dictionary<string, object>
            {
                ["gamedata-path"] = gamedataPath,
                ["name"] = name,
                ["version"] = version
            };
            string data = JsonSerializer.Serialize(config, options);
            File.WriteAllText(settingsPath, data);
            Logger.Log("Settings", $"Updated version to {version} after installing update...");
        }

        public Settings()
        {
            try
            {
                Logger.Log("Settings", "Initializing settings...");
                string folderPath = AppDomain.CurrentDomain.BaseDirectory;
                string settingsPath = AppDomain.CurrentDomain.BaseDirectory + "settings.json";
                if (!File.Exists(settingsPath))
                {
                    Logger.Log("Settings", "Creating settings file...");
                    var config = new Dictionary<string, object>
                    {
                        ["gamedata-path"] = "",
                        ["name"] = "",
                        ["version"] = 0
                    };
                    string data = JsonSerializer.Serialize(config, options);
                    File.WriteAllText(settingsPath, data);
                    Logger.Log("Settings", "Settings file created...");
                }
                string json = File.ReadAllText(settingsPath);
                using JsonDocument doc = JsonDocument.Parse(json);
                string? value = doc.RootElement.GetProperty("gamedata-path").GetString();
                name = doc.RootElement.GetProperty("name").GetString() ?? "";
                version = doc.RootElement.GetProperty("version").GetInt32();
                if (value == null || value.Trim().Length == 0)
                {
                    Logger.Log("Settings", "GameData path not found...");
                    var dialog = new OpenFolderDialog
                    {
                        Title = "Выберите папку с gamedata",
                        InitialDirectory = @"C:\"
                    };
                    while (dialog.ShowDialog() != true)
                    {
                        var result = MessageBox.Show("Выберите папку, куда будут размещены файлы(папка gamedata и её содержимое)", "Настройка", MessageBoxButton.YesNo);
                        if (result == MessageBoxResult.No)
                        {
                            OnUserDeclined?.Invoke();
                            return;
                        }
                    }
                    string path = dialog.FolderName;
                    Logger.Log("Settings", $"GameData path selected: {path}");
                    gamedataPath = path.Replace("\\", "/");
                    var config = new Dictionary<string, object>
                    {
                        ["gamedata-path"] = path,
                        ["name"] = "",
                        ["version"] = version,
                    };
                    string data = JsonSerializer.Serialize(config, options);
                    File.WriteAllText(settingsPath, data);
                    Logger.Log("Settings", $"Saved gamedata path. Created /gamedata/configs folder.");
                    Directory.CreateDirectory(path + "/gamedata/configs");
                    return;
                }
                gamedataPath = value.Replace("\\", "/");
            }
            catch (Exception ex)
            {
                Logger.Log("Settings", $"Unhandled exception: {ex}");
                MessageBox.Show("Произошла неизвестная ошибка при обработке папки gamedata. Обратитесь к разработчику. К обращению приложите файл log.txt", "Настройки");
                Application.Current.Shutdown();
            }
        }

        public void UpdateName(string name)
        {
            try
            {
                this.name = name;
                Logger.Log("Settings", $"User selected name: {name}.");
                string settingsPath = AppDomain.CurrentDomain.BaseDirectory + "/settings.json";
                string json = File.ReadAllText(settingsPath);
                using JsonDocument doc = JsonDocument.Parse(json);
                var config = new Dictionary<string, object>
                {
                    ["gamedata-path"] = doc.RootElement.GetProperty("gamedata-path").GetString() ?? "",
                    ["name"] = name,
                    ["version"] = doc.RootElement.GetProperty("version").GetInt32(),
                };
                string data = JsonSerializer.Serialize(config, options);
                File.WriteAllText(settingsPath, data);
                Logger.Log("Settings", "Name saved to settings file.");
            }
            catch (Exception ex)
            {
                Logger.Log("Settings", $"Unhandled exception: {ex}");
                MessageBox.Show("Произошла неизвестная ошибка при обработке имени. Обратитесь к разработчику. К обращению приложите файл log.txt", "Настройки");
                Application.Current.Shutdown();
            }

        }
    }
}
