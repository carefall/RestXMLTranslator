using RestXMLTranslator.Internals.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Windows;
using System.Xml;
using System.Xml.Linq;
using static RestXMLTranslator.MainWindow;

namespace RestXMLTranslator.Internals
{
    internal static class RestClient
    {
        private static readonly HttpClient Client = new()
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        private static readonly XmlWriterSettings XmlSettings = new()
        {
            Encoding = Encoding.GetEncoding(1251),
            Indent = true
        };

        internal static readonly JsonSerializerOptions Options = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public static async Task<string> GetDataAsync(string endpoint)
        {
            try
            {
                HttpResponseMessage response = await Client.GetAsync(endpoint);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                Logger.Log("RestClient-Get", $"Unable to sync data with server. Exception: {ex}");
                return "";
            }
        }

        public static async Task<string> PostDataAsync(string endpoint, string body)
        {
            try
            {
                using var content = new StringContent(body, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await Client.PostAsync(endpoint, content);
                string json = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                    throw new Exception(json);
                return json;
            }
            catch (Exception ex)
            {
                Logger.Log("RestClient-Post", $"Unable to sync data with server. Exception: {ex}");
                return "";
            }
        }

        public static async Task<int> Check(string gameDataPath, int version, IProgress<string>? progress = null)
        {
            try
            {
                Logger.Log("RestClient-Get", $"Sending files request");
                progress?.Report(Locale.Get("getting_files"));
                var files = await SyncService.GetServerFiles();
                if (files == null) return 1;
                int targetVersion = files.Count > 0 ? files.Values.Max() : 0;
                if (targetVersion <= version)
                {
                    return 0;
                }
                progress?.Report(Locale.Get("deleting_files"));
                DeleteRedundantFiles(files, gameDataPath);
                var result = await UpdateLocalFiles(gameDataPath, version, targetVersion, progress);
                return result;
            }
            catch (Exception ex)
            {
                Logger.Log("RestClient-Sync", $"Unhandled exception: {ex}");
                MessageBox.Show(Locale.Get("sync_fail"), Locale.Get("sync"));
                return -1;
            }
        }


        public async static Task<int> UpdateLocalFiles(string gameDataPath, int version, int targetVersion, IProgress<string>? progress = null)
        {
            Stopwatch watch = new Stopwatch();
            Logger.Log("RestClient-Get", "Sending download request");
            progress?.Report(Locale.Get("downloading"));
            watch.Start();
            string update = await GetDataAsync($"http://127.0.0.1:8000/translator/download?version={version}");
            watch.Stop();
            Logger.Log("RestClient-Get", "Download responce received.");
            if (update == "") return 1;
            List<DownloadedFile>? files = JsonSerializer.Deserialize<List<DownloadedFile>>(update, Options);
            if (files == null) return 0;
            for (int i = 0; i < files.Count; i++)
            {
                var file = files[i];
                string path = Path.Combine(gameDataPath, "gamedata", "configs", file.Path);
                string? dir = Path.GetDirectoryName(path);
                if (dir == null)
                {
                    Logger.Log("RestClient-Drive", $"Unable to create folder {dir}");
                    MessageBox.Show(Locale.Get("sync_fail", dir ?? "UNDEFINED"), Locale.Get("sync"));
                    Application.Current.Shutdown();
                    return -1;
                }
                Logger.Log("RestClient-Drive", $"Creating file {file.Path}");
                Directory.CreateDirectory(dir);
                XDocument doc = new(new XElement("string_table"));
                if (File.Exists(path)) doc = XDocument.Load(path);
                var index = doc.Root!.Elements("string").ToDictionary(x => (string)x.Attribute("id")!);
                foreach (var entry in file.HalfEntries)
                {
                    if (!index.TryGetValue(entry.Id!, out var stringElement))
                    {
                        stringElement = new XElement("string", new XAttribute("id", entry.Id!));
                        doc.Root.Add(stringElement);
                        index[entry.Id!] = stringElement;
                    }
                    var langTag = entry.Russian ? "rus" : "eng";
                    var langElement = stringElement.Element(langTag);
                    if (langElement == null)
                    {
                        langElement = new XElement(langTag);
                        stringElement.Add(langElement);
                    }
                    langElement.Value = entry.Text!;
                }
                Logger.Log("RestClient-Drive", $"Writing file {file.Path}");
                using var writer = XmlWriter.Create(path, XmlSettings);
                doc.Save(writer);
            }
            Settings.GetInstance().UpdateVersion(targetVersion);
            return 0;
        }

        public static void DeleteRedundantFilesWithTheirTabs(Dictionary<string, int> files, ObservableCollection<FileTab> tabs)
        {
            var filesToDelete = tabs.Where(t => !files.ContainsKey(t.RelativePath.Replace("\\", "/")));
            foreach (FileTab tab in filesToDelete)
            {
                tabs.Remove(tab);
                File.Delete(tab.FilePath);
                string changesPath = AppDomain.CurrentDomain.BaseDirectory + "Changes/" + tab.RelativePath.Replace("\\", "/").Replace(".xml", ".json");
                if (File.Exists(changesPath))
                {
                    File.Delete(changesPath);
                }
            }

        }

        public static void DeleteRedundantFiles(Dictionary<string, int> files, string gameDataPath)
        {
            List<string> localFiles = GetLocalFiles(gameDataPath + "/gamedata/configs");
            if (localFiles.Count == 0) return;
            if (files.Count == 0)
            {
                Directory.Delete(gameDataPath + "/gamedata/configs", true);
                return;
            }
            foreach (string item in localFiles)
            {
                if (files.ContainsKey(item)) continue;
                File.Delete(gameDataPath + "/gamedata/configs/" + item);
            }
        }

        public static List<string> GetLocalFiles(string folderPath)
        {
            try
            {
                return [.. Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories).Select(f => {
                    var relative = Path.GetRelativePath(folderPath, f);
                    return relative.Replace("\\", "/");
                })];
            }
            catch (Exception ex)
            {
                Logger.Log("RestClient-Drive", $"Unhandled exception: {ex}");
                return [];
            }
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
                        User = Settings.GetInstance().Name
                    });
                }
                if (entry.HasEngChanges)
                {
                    request.Entries.Add(new UploadEntry
                    {
                        Id = entry.Id,
                        Russian = false,
                        Text = entry.NewEng,
                        User = Settings.GetInstance().Name
                    });
                }
            }
            string body = JsonSerializer.Serialize(request, Options);
            string json = await PostDataAsync($"http://127.0.0.1:8000/translator/upload?filepath={file.RelativePath.Replace("\\", "/")}", body);
            if (json == "") return false;
            int version = JsonSerializer.Deserialize<int>(json, Options);
            Settings.GetInstance().UpdateVersion(version);
            return true;
        }

        
    }
}
