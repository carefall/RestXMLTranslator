using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Xml;
using System.Xml.Linq;
using static RestXMLTranslator.MainWindow;

namespace RestXMLTranslator.Internals
{
    internal class RestClient
    {
        private static readonly XmlWriterSettings settings = new()
        {
            Encoding = Encoding.GetEncoding(1251),
            Indent = true
        };

        private static int targetVersion = 0;

        private static JsonSerializerOptions options = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public class HalfStringEntry
        {
            public int Uid { get; set; }
            public string? Id { get; set; }
            public string? File { get; set; }
            public string? Text { get; set; }
            public bool Russian { get; set; }
        }

        public class UploadRequest
        {
            public List<UploadEntry> Entries { get; set; } = [];
        }

        public class UploadEntry
        {
            public string Id { get; set; } = string.Empty;
            public bool Russian { get; set; }
            public string Text { get; set; } = string.Empty;
            public string User { get; set; } = string.Empty;
        }

        public class DownloadedFile
        {
            public string Path { get; set; } = string.Empty;

            public List<RestXMLTranslator.Internals.XMLHelper.StringEntry> Entries { get; set; } = [];
        }

        public static async Task<string> GetDataAsync(string endpoint)
        {
            try
            {
                using var client = new HttpClient();
                HttpResponseMessage response = await client.GetAsync(endpoint);
                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync();
                return json;
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

                var content = new StringContent(body, Encoding.UTF8, "application/json");
                using var client = new HttpClient();
                HttpResponseMessage response = await client.PostAsync(endpoint, content);
                string json = await response.Content.ReadAsStringAsync();
                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    throw new Exception(json);
                }
                return json;
            }
            catch (Exception ex)
            {
                Logger.Log("RestClient-Post", $"Unable to sync data with server. Exception: {ex}");
                return "";
            }
        }

        public static async Task<int> Check(string gameDataPath, int version)
        {
            try
            {
                string json = await GetDataAsync("https://nukerfall.pythonanywhere.com/translator/files");
                if (json == "") return 1;
                Dictionary<string, int> files = JsonSerializer.Deserialize<Dictionary<string, int>>(json) ?? [];
                if (files.Count != 0)
                {
                    targetVersion = files.Values.Max();
                }
                else
                {
                    targetVersion = 0;
                }
                if (targetVersion <= version)
                {
                    return 0;
                }
                DeleteRedundantFiles(files, gameDataPath);
                return await UpdateLocalFiles(files, gameDataPath, version);
            }
            catch (Exception ex)
            {
                Logger.Log("RestClient-Sync", $"Unhandled exception: {ex}");
                MessageBox.Show("Произошла неизвестная ошибка при синхронизации. Обратитесь к разработчику. К обращению приложите файл log.txt", "Синхронизация");
                return -1;
            }
        }


        public async static Task<int> UpdateLocalFiles(Dictionary<string, int> files, string gameDataPath, int version)
        {
            foreach (var file in files)
            {
                if (file.Value < version) continue;
                string update = await GetDataAsync($"https://nukerfall.pythonanywhere.com/translator/download?version={version}&filepath={file.Key}");
                if (update == "") return 1;
                List<HalfStringEntry>? entries = JsonSerializer.Deserialize<List<HalfStringEntry>>(update, options);
                if (entries == null) continue;
                string path = gameDataPath + "/gamedata/configs/" + file.Key;
                string? dir = Path.GetDirectoryName(path);
                if (dir == null)
                {
                    Logger.Log("RestClient-Drive", $"Unable to create folder {dir}");
                    MessageBox.Show($"Не удалось создать путь {dir}. Обратитесь к разработчику. К обращению приложите файл log.txt", "Синхронизация");
                    Application.Current.Shutdown();
                    return -1;
                }
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                XDocument doc = new(new XElement("string_table"));
                if (File.Exists(path)) doc = XDocument.Load(path);
                var index = doc.Root!.Elements("string").ToDictionary(x => (string)x.Attribute("id")!);
                foreach (var entry in entries)
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
                using var writer = XmlWriter.Create(path, settings);
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
                        User = Settings.GetInstance().name
                    });
                }
                else
                {
                    request.Entries.Add(new UploadEntry
                    {
                        Id = entry.Id,
                        Russian = false,
                        Text = entry.NewEng,
                        User = Settings.GetInstance().name
                    });
                }
            }
            string body = JsonSerializer.Serialize(request, options);
            string json = await PostDataAsync($"https://nukerfall.pythonanywhere.com/translator/upload?filepath={file.RelativePath.Replace("\\", "/")}", body);
            if (json == "") return false;
            int version = JsonSerializer.Deserialize<int>(json);
            Settings.GetInstance().UpdateVersion(version);
            return true;
        }

        public async static Task<List<DownloadedFile>?> Update(ObservableCollection<FileTab> tabs)
        {
            int version = Settings.GetInstance().version;
            string json = await GetDataAsync("https://nukerfall.pythonanywhere.com/translator/files");
            if (json == "") return null;
            Dictionary<string, int> files = JsonSerializer.Deserialize<Dictionary<string, int>>(json) ?? [];
            DeleteRedundantFilesWithTheirTabs(files, tabs);
            List<DownloadedFile> result = [];
            foreach (var file in files)
            {
                if (file.Value < version) continue;
                string update = await GetDataAsync($"https://nukerfall.pythonanywhere.com/translator/download?version={version}&filepath={file.Key}");
                if (update == "") return null;
                List<HalfStringEntry>? entries = JsonSerializer.Deserialize<List<HalfStringEntry>>(update, options);
                if (entries == null) continue;
                List<XMLHelper.StringEntry> sEntries = [];
                foreach (var entry in entries)
                {
                    var seq = sEntries.Where(e => e.Id == entry.Id);
                    if (seq == null || !seq.Any())
                    {
                        sEntries.Add(new XMLHelper.StringEntry()
                        {
                            Id = entry.Id!,
                            Ru = entry.Russian ? entry.Text! : "",
                            NewRu = entry.Russian ? entry.Text! : "",
                            Eng = !entry.Russian ? entry.Text! : "",
                            NewEng = !entry.Russian ? entry.Text! : "",
                        });
                    }
                    else
                    {
                        var pair = seq.First();
                        if (entry.Russian)
                        {
                            pair.Ru = entry.Text!;
                            pair.NewRu = entry.Text!;
                        }
                        else
                        {
                            pair.Eng = entry.Text!;
                            pair.NewEng = entry.Text!;
                        }
                    }
                }
                result.Add(new DownloadedFile()
                {
                    Path = file.Key,
                    Entries = sEntries
                });
            }
            return result;
        }

        public async static Task<bool?> CompareVersions()
        {
            string json = await GetDataAsync("https://nukerfall.pythonanywhere.com/translator/version");
            if (json == "") return null;
            int version = JsonSerializer.Deserialize<int>(json);
            if (version < Settings.GetInstance().version)
            {
                Logger.Log("RestClient-Sync", $"Somehow client version is higher than server version: {Settings.GetInstance().version} against {version}");
                Settings.GetInstance().version = 0;
            }
            return Settings.GetInstance().version == version;
        }


    }
}
