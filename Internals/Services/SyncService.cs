using RestXMLTranslator.Internals.Models;
using RestXMLTranslator.Internals.Program;
using System.Text.Json;

namespace RestXMLTranslator.Internals.Services
{
    public class SyncService
    {
        public async Task<(Dictionary<string, FileInfo>?, bool)> GetServerFiles()
        {
            string json = await RestClient.GetDataAsync("files");
            if (string.IsNullOrEmpty(json)) return (null, true);
            if (json == "0") return (null, false);
            return (JsonSerializer.Deserialize<Dictionary<string, FileInfo>>(json, App.Current.JsonOptions), true);
        }


        public async Task<List<DownloadedFile>?> DownloadUpdatedFiles(int version)
        {
            string json = await RestClient.GetDataAsync($"download?version={version}");
            if (string.IsNullOrEmpty(json)) return null;
            if (json == "1") return ([]);
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
                    Logger.Log($"Client version is higher: {App.Current.Settings.Version} server {version}", "SyncService");
                    App.Current.Settings.UpdateVersion(0);
                    return 0;
                }
                return version;
            }
            catch (Exception)
            {
                Logger.Log($"Server is broken. It returned {json} instead of int version", "SyncService");
                return -2;
            }
        }

        public async Task<(SyncResult, Dictionary<string, FileInfo>?)> EditorSync()
        {
            int version = App.Current.Settings.Version;
            (var files, bool allowed) = await GetServerFiles();
            if (!allowed) return (SyncResult.Inactive, null);
            if (files == null) return (SyncResult.ServerUnavailable, null);
            App.Current.MWindow.WindowBlocker.Title.Text = Locale.Get("deleting_files");
            App.Current.LocalFiles.DeleteRedundantFiles(files);
            App.Current.LocalFiles.DeleteChanges(files);
            App.Current.MWindow.WindowBlocker.Title.Text = Locale.Get("downloading_updates");
            var updates = await DownloadUpdates(version);
            if (updates == null) return (SyncResult.ServerUnavailable, files);
            if (updates.Count == 0) return (SyncResult.OldApp, files);
            App.Current.MWindow.WindowBlocker.Title.Text = Locale.Get("applying_updates");
            SyncResult result = await App.Current.LocalFiles.ApplyUpdates(updates);
            return (result, files);
        }

        public async Task<(SyncResult, Dictionary<string, FileInfo>?)> StartupSync(string gameDataPath, int version, IProgress<string>? progress = null)
        {
            try
            {
                progress?.Report(Locale.Get("getting_files"));
                (var files, bool allowed) = await GetServerFiles();
                if (!allowed) return (SyncResult.Inactive, null);
                if (files == null) return (SyncResult.ServerUnavailable, null);
                if (files.Count == 0) return (SyncResult.Success, null);
                int targetVersion = files.Values.Max(f => f.Version);
                if (targetVersion <= version) return (SyncResult.Success, files);
                progress?.Report(Locale.Get("deleting_files"));
                await Task.Run(() =>
                {
                    App.Current.LocalFiles.DeleteRedundantFiles(files);
                    App.Current.LocalFiles.DeleteChanges(files);
                });
                progress?.Report(Locale.Get("downloading_updates"));
                var updates = await DownloadUpdates(version);
                if (updates == null) return (SyncResult.ServerUnavailable, files);
                if (updates.Count == 0) return (SyncResult.OldApp, files);
                progress?.Report(Locale.Get("applying_updates"));
                SyncResult result = await App.Current.LocalFiles.ApplyUpdates(updates);
                if (result == SyncResult.Success) App.Current.Settings.UpdateVersion(targetVersion);
                return (result, files);
            }
            catch (Exception ex)
            {
                Logger.Log($"Unhandled exception: {ex}", "SyncService");
                return (SyncResult.Other, null);
            }
        }

        public async Task<List<DownloadedFile>?> DownloadUpdates(int version)
        {
            var files = await DownloadUpdatedFiles(version);
            if (files == null) return null;
            foreach (var file in files)
            {
                var latest = file.HalfEntries.MaxBy(x => x.Uid);
                file.Finished = latest != null? latest.Finished : false;
            }
            return files;
        }

        public async Task<SyncResult> Commit(FileTab file)
        {
            var seq = file.Entries.Where(e => e.IsApproved);
            if (seq == null || !seq.Any())
            {
                return SyncResult.Other;
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
                        EditType = 0,
                        Text = XMLHelper.EncodeMultilineForServer(entry.NewRu),
                        User = App.Current.Settings.Name
                    });
                }
                if (entry.HasEngChanges)
                {
                    request.Entries.Add(new UploadEntry
                    {
                        Id = entry.Id,
                        EditType = 1,
                        Text = XMLHelper.EncodeMultilineForServer(entry.NewEng),
                        User = App.Current.Settings.Name,
                    });
                }
                if (entry.HasCommentChanges)
                {
                    request.Entries.Add(new UploadEntry
                    {
                        Id = entry.Id,
                        EditType = -1,
                        Text = entry.NewComment,
                        User = App.Current.Settings.Name,
                    });
                }
            }
            string body = JsonSerializer.Serialize(request, App.Current.JsonOptions);
            string json = await RestClient.PostDataAsync($"upload?filepath={file.RelativePath.Replace("\\", "/")}", body);
            if (json == "0") return SyncResult.Inactive;
            if (json == "1") return SyncResult.OldApp;
            if (json == "") return SyncResult.ServerUnavailable;
            int version = JsonSerializer.Deserialize<int>(json, App.Current.JsonOptions);
            App.Current.Settings.SetOrAddFileStatus(file.RelativePath, false);
            App.Current.Settings.UpdateVersion(version);
            return SyncResult.Success;
        }
    }
}