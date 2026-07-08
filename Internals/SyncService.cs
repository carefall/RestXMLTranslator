using RestXMLTranslator.Internals.Models;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows;
using static RestXMLTranslator.Internals.RestClient;
using static RestXMLTranslator.MainWindow;

namespace RestXMLTranslator.Internals
{
    internal static class SyncService
    {
        public static async Task<Dictionary<string, int>?> GetServerFiles()
        {
            string json = await GetDataAsync("http://127.0.0.1:8000/translator/files");
            if (string.IsNullOrEmpty(json)) return null;
            return JsonSerializer.Deserialize<Dictionary<string, int>>(json, Options);
        }


        public static async Task<List<DownloadedFile>?> Download(int version)
        {
            string json = await GetDataAsync($"http://127.0.0.1:8000/translator/download?version={version}");

            if (string.IsNullOrEmpty(json)) return null;
            return JsonSerializer.Deserialize<List<DownloadedFile>>(json, Options);
        }

        public static async Task<int> CompareVersions()
        {
            string json = await GetDataAsync("http://127.0.0.1:8000/translator/version");
            if (json == "") return -1;
            int version = JsonSerializer.Deserialize<int>(json, Options);
            if (version < Settings.GetInstance().Version)
            {
                Logger.Log("SyncService", $"Client version is higher: {Settings.GetInstance().Version} server {version}");
                Settings.GetInstance().UpdateVersion(0);
                MessageBox.Show(Locale.Get("sync_version_higher"), Locale.Get("sync"), MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return version;
        }

        public static async Task<List<DownloadedFile>?> Update(ObservableCollection<FileTab> tabs)
        {
            int version = Settings.GetInstance().Version;
            var serverFiles = await GetServerFiles();
            if (serverFiles == null)
                return null;
            DeleteRedundantFilesWithTheirTabs(serverFiles, tabs);
            var files = await Download(version);
            if (files == null) return null;
            DownloadedFileWrapper.FillEntries(files);
            return files;
        }

    }
}
