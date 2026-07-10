using RestXMLTranslator.Internals.Models;
using RestXMLTranslator.Internals.Program;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Pipelines;
using System.Text.Json;

namespace RestXMLTranslator.Internals.Services
{
    public class SyncService
    {
        public async Task<Dictionary<string, int>?> GetServerFiles()
        {
            string json = await RestClient.GetDataAsync("files");
            if (string.IsNullOrEmpty(json)) return null;
            return JsonSerializer.Deserialize<Dictionary<string, int>>(json, App.Current.JsonOptions);
        }


        public async Task<List<DownloadedFile>?> DownloadUpdatedFiles(int version)
        {
            string json = await RestClient.GetDataAsync($"download?version={version}");
            if (string.IsNullOrEmpty(json)) return null;
            return await Task.Run(() => JsonSerializer.Deserialize<List<DownloadedFile>>(json, App.Current.JsonOptions));
        }

        public async Task<int> CompareVersions()
        {
            string json = await RestClient.GetDataAsync("version");
            if (json == "") return -1;
            try
            {
                int version = JsonSerializer.Deserialize<int>(json, App.Current.JsonOptions);
                if (version < App.Current.Settings.Version)
                {
                    Logger.Log("SyncService", $"Client version is higher: {App.Current.Settings.Version} server {version}");
                    App.Current.Settings.UpdateVersion(0);
                    return 0;
                }
                return version;
            } catch (Exception)
            {
                Logger.Log("SyncService" , $"Server is broken. It returned {json} instead of int version");
                App.Current.Shutdown();
                return 0;
            }
        }

        public async Task<List<DownloadedFile>?> EditorSync(ObservableCollection<FileTab> tabs)
        {
            int version = App.Current.Settings.Version;
            var serverFiles = await GetServerFiles();
            if (serverFiles == null) return null;
            App.Current.LocalFiles.DeleteRedundantFilesWithTabs(serverFiles, tabs);
            return await DownloadUpdates(version);
        }

        public async Task<SyncResult> StartupSync(string gameDataPath, int version, IProgress<string>? progress = null)
        {
            try
            {
                progress?.Report(Locale.Get("getting_files"));
                var files = await GetServerFiles();
                if (files == null) return SyncResult.ServerUnavailable;
                if (files.Count == 0) return SyncResult.Success;
                int targetVersion = files.Values.Max();
                if (targetVersion <= version) return SyncResult.Success;
                progress?.Report(Locale.Get("deleting_files"));
                await Task.Run(() =>
                {
                    App.Current.LocalFiles.DeleteRedundantFiles(files, gameDataPath);
                });
                var updates = await DownloadUpdates(version);
                if (updates == null) return SyncResult.ServerUnavailable;
                int result = await App.Current.LocalFiles.ApplyUpdates(gameDataPath, updates);
                if (result != 0) return SyncResult.Other;
                await Task.Run(() =>
                {
                    App.Current.Settings.UpdateVersion(targetVersion);
                });
                return SyncResult.Success;
            }
            catch (Exception ex)
            {
                Logger.Log("SyncService", $"Unhandled exception: {ex}");
                return SyncResult.Other;
            }
        }

        public async Task<List<DownloadedFile>?> DownloadUpdates(int version)
        {
            var files = await DownloadUpdatedFiles(version);
            if (files == null) return null;
            DownloadedFileWrapper.FillEntries(files);
            return files;
        }

        public async static Task<bool> Upload(FileTab file)
        {
            var seq = file.Entries.Where(e => e.IsApproved);
            if (seq == null || !seq.Any())
            {
                return true;
            }
            var request = new UploadRequest
            {
                Entries = []
            };
            foreach (var entry in seq)
            {
                if (entry.HasRuChanges)
                {
                    request.Entries.Add(new UploadEntry
                    {
                        Id = entry.Id,
                        Russian = true,
                        Text = entry.NewRu,
                        User = App.Current.Settings.Name
                    });
                }
                if (entry.HasEngChanges)
                {
                    request.Entries.Add(new UploadEntry
                    {
                        Id = entry.Id,
                        Russian = false,
                        Text = entry.NewEng,
                        User = App.Current.Settings.Name
                    });
                }
            }
            string body = JsonSerializer.Serialize(request, App.Current.JsonOptions);
            string json = await RestClient.PostDataAsync($"upload?filepath={file.RelativePath.Replace("\\", "/")}", body);
            if (json == "") return false;
            int version = JsonSerializer.Deserialize<int>(json, App.Current.JsonOptions);
            App.Current.Settings.UpdateVersion(version);
            return true;
        }
    }
}