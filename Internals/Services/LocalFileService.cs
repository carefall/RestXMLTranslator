using RestXMLTranslator.Internals.Models;
using RestXMLTranslator.Internals.Program;
using System.Collections;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;


namespace RestXMLTranslator.Internals.Services
{
    public class LocalFileService
    {
        public void DeleteRedundantFiles(Dictionary<string, Models.FileInfo> files)
        {
            string path = Path.Combine(App.Current.Settings.GameDataPath, "gamedata", "configs", "text"); // change path later when smth happens
            if (!Directory.Exists(path)) return;
            List<string> localFiles = GetLocalFiles(path);
            if (localFiles.Count == 0) return;
            if (files.Count == 0) return;
            foreach (string item in localFiles)
            {
                if (files.ContainsKey(item)) continue;
                try
                {
                    App.Current.Settings.TryDeleteStatus(item);
                    File.Delete(Path.Combine(path, item));
                }
                catch (Exception ex)
                {
                    Logger.Log($"Unhandle exception on file {item} deletion: {ex}", "LocalFileService");
                    continue;
                }
            }
        }

        public void DeleteChanges(Dictionary<string, Models.FileInfo> files)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Changes");
            if (!Directory.Exists(path)) return;
            List<string> localFiles = GetLocalFiles(path);
            localFiles = localFiles.ConvertAll(file =>
            {
                int index = file.IndexOf("text/");
                return index >= 0 ? file.Remove(index, "text/".Length) : file;
            });
            if (localFiles.Count == 0) return;
            if (files.Count == 0) return;
            if (!Directory.Exists(path)) return;
            foreach (string item in localFiles)
            {
                if (files.ContainsKey(item.Replace(".json", ".xml"))) continue;
                try
                {
                    File.Delete(Path.Combine(path, item.Replace("text\\", "")));
                }
                catch (Exception ex)
                {
                    Logger.Log($"Unhandle exception on file {item} deletion: {ex}", "LocalFileService");
                    continue;
                }
            }
        }

        private XElement GetOrCreateString(XElement root, Dictionary<string, XElement> index, string id)
        {
            if (index.TryGetValue(id, out var element))
                return element;
            element = new XElement("string", new XAttribute("id", id));
            root.Add(element);
            index[id] = element;
            return element;
        }

        private XElement GetOrCreateLanguage(XElement stringElement, string tag)
        {
            XElement? element = stringElement.Element(tag);
            if (element != null) return element;
            element = new XElement(tag);
            stringElement.Add(element);
            return element;
        }

        public async Task ApplyHalfEntries(string path, DownloadedFile file)
        {
            XDocument doc = new(new XElement("string_table"));
            if (File.Exists(path)) doc = XDocument.Load(path);
            var index = doc.Root!.Elements("string").ToDictionary(x => (string)x.Attribute("id")!);
            HashSet<string> validIds = new(file.Ids);
            foreach (var pair in index.ToList())
            {
                if (validIds.Contains(pair.Key))
                    continue;
                XElement element = pair.Value;
                if (element.PreviousNode is XComment comment)
                    comment.Remove();
                element.Remove();
                index.Remove(pair.Key);
            }
            foreach (var entry in file.HalfEntries)
            {
                XElement stringElement = GetOrCreateString(doc.Root!, index, entry.Id!);
                if (entry.EditType == -1)
                {
                    if (stringElement.PreviousNode is XComment oldComment) oldComment.Remove();
                    if (!string.IsNullOrWhiteSpace(entry.Text))
                    {
                        stringElement.AddBeforeSelf(new XComment(entry.Text));
                    }
                    continue;
                }
                string tag = entry.EditType == 0 ? "rus" : "eng";
                XElement lang = GetOrCreateLanguage(stringElement, tag);
                lang.Value = XMLHelper.EncodeMultilineForXML(entry.Text!);
            }
            using var writer = XmlWriter.Create(path, App.Current.XmlSettings);
            doc.Save(writer);
            writer.Close();
            App.Current.Settings.SetOrAddFileStatus(file.Path, file.Finished);
            await WriteWhiteSpaces(path, file.HalfEntries);
        }

        public async Task<SyncResult> ApplyUpdates(List<DownloadedFile> files)
        {
            try
            {
                foreach (var file in files)
                {
                    string path = Path.Combine(App.Current.Settings.GameDataPath, "gamedata", "configs", file.Path);
                    string? dir = Path.GetDirectoryName(path);
                    Directory.CreateDirectory(dir!);
                    await ApplyHalfEntries(path, file);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Unhandled exception during changes applying: {ex}", "LocalFileService");
                return SyncResult.Other;
            }
            return SyncResult.Success;
        }

        public List<string> GetLocalFiles(string folderPath)
        {
            try
            {
                return [..Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories).Select(f =>
                {
                    var relative = Path.GetRelativePath(folderPath, f);
                    return "text/" + relative.Replace("\\", "/");
                })];
            }
            catch (Exception ex)
            {
                Logger.Log($"Unhandled exception during local files collection: {ex}", "LocalFileService");
                return [];
            }
        }

        public async Task<ObservableCollection<FileTab>> ReadLocalFiles()
        {
            string fullPath = Path.Combine(App.Current.Settings.GameDataPath, "gamedata", "configs", "text");
            string path = Path.Combine(App.Current.Settings.GameDataPath, "gamedata", "configs");
            var files = Directory.GetFiles(fullPath, "*", SearchOption.AllDirectories);
            ObservableCollection<FileTab> tabs = [];
            foreach (var file in files)
            {
                var relativePath = file.Replace(path, "")[1..].Replace("\\", "/");
                var tab = new FileTab(file, relativePath);
                await Task.Run(() =>
                {
                    tab.Read();
                    ApplyChanges(tab);
                });
                if (tab.Entries.Count == 0) continue;
                tabs.Add(tab);
            }
            return tabs;
        }

        private void ApplyChanges(FileTab file)
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Changes", file.RelativePath.Replace(".xml", ".json"));
            if (!File.Exists(filePath)) return;
            try
            {
                string json = File.ReadAllText(filePath, Encoding.GetEncoding(1251));
                List<Change>? changes = JsonSerializer.Deserialize<List<Change>>(json, App.Current.JsonOptions);
                changes ??= [];
                foreach (Change change in changes)
                {
                    var seq = file.Entries.Where(e => e.Id == change.Id);
                    if (!seq.Any()) continue;
                    StringEntry bro = seq.First();
                    bro.NewEng = XMLHelper.DecodeMultiline(change.Eng ?? "");
                    bro.NewRu = XMLHelper.DecodeMultiline(change.Ru ?? "");
                    bro.NewComment = XMLHelper.DecodeMultiline(change.Comment ?? "");
                    bro.HasNewLine = change.NewLine;
                    if (!change.IsApproved) bro.IsApproved = false;
                    if (bro.HasChanges && change.IsApproved) bro.IsApproved = true;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Unhandled exception during changes load: {ex}", "LocalFileService");
            }
        }

        public void StoreChanges(FileTab file, bool allowApprove)
        {
            StoreChanges(file, allowApprove, false);
        }

        private void StoreChanges(FileTab file, bool allowApprove, bool ignoreApproved)
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Changes", file.RelativePath.Replace(".xml", ".json"));
            string directory = Path.GetDirectoryName(filePath)!;
            Directory.CreateDirectory(directory);
            List<Change>? changes = [];
            foreach (StringEntry entry in file.Entries)
            {
                if (!entry.HasChanges) continue;
                if (ignoreApproved && entry.IsApproved) continue;
                changes.Add(new Change(entry.Id, XMLHelper.EncodeMultilineForJSON(entry.NewRu), XMLHelper.EncodeMultilineForJSON(entry.NewEng), allowApprove ? entry.IsApproved : false, entry.NewComment, entry.HasNewLine));
            }
            if (changes.Count == 0 && File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            else
            {
                File.WriteAllText(filePath, JsonSerializer.Serialize(changes, App.Current.JsonOptions), Encoding.GetEncoding(1251));
            }
        }

        public void StoreChanges(FileTab file)
        {
            StoreChanges(file, false, true);
        }

        public ObservableCollection<StringEntry> Read(string filePath)
        {
            string xml = File.ReadAllText(filePath, Encoding.GetEncoding(1251));
            return XMLHelper.LoadStrings(xml, false);
        }

        public async Task ApplyApprovedChanges(FileTab tab)
        {
            XDocument doc = new(new XElement("string_entry"));
            if (File.Exists(tab.FilePath)) doc = XDocument.Load(tab.FilePath, LoadOptions.None);
            var index = doc.Root!.Elements("string").ToDictionary(x => (string)x.Attribute("id")!);
            foreach (var entry in tab.Entries)
            {
                XElement stringElement = GetOrCreateString(doc.Root!, index, entry.Id!);
                if (stringElement.PreviousNode is XComment oldComment) oldComment.Remove();
                if (!string.IsNullOrWhiteSpace(entry.NewComment))
                {
                    stringElement.AddBeforeSelf(new XComment(entry.NewComment));
                }
                XElement rus = GetOrCreateLanguage(stringElement, "rus");
                rus.Value = XMLHelper.EncodeMultilineFromInput(entry.IsApproved ? entry.NewRu : entry.Ru);
                XElement eng = GetOrCreateLanguage(stringElement, "eng");
                eng.Value = XMLHelper.EncodeMultilineFromInput(entry.IsApproved ? entry.NewEng : entry.Eng);
                entry.IsApproved = false;
            }
            using var writer = XmlWriter.Create(tab.FilePath, App.Current.XmlSettings);
            doc.Save(writer);
            writer.Close();
            await WriteWhiteSpaces(tab.FilePath, tab.Entries);
        }

        private async Task WriteWhiteSpaces<T>(string path, IEnumerable<T> entries) where T : IEntry
        {
            var encoding = Encoding.GetEncoding(1251);
            var lines = File.ReadAllLines(path, encoding).ToList();
            var result = new List<string>(lines.Count);
            var needBlank = entries.Where(x => x.HasNewLine).Select(x => x.Id!).ToHashSet();
            for (int i = 0; i < lines.Count;)
            {
                bool isComment = lines[i].TrimStart().StartsWith("<!--");
                bool isString = lines[i].Contains("<string id=\"");
                if (!isComment && !isString)
                {
                    result.Add(lines[i++]);
                    continue;
                }
                var block = new List<string>();
                if (isComment)
                {
                    do
                    {
                        block.Add(lines[i]);
                        while (!lines[i].Contains("-->"))
                        {
                            i++;
                            block.Add(lines[i]);
                        }
                        i++;
                    }
                    while (i < lines.Count && lines[i].Trim().Length == 0);
                    if (i < lines.Count && lines[i].Contains("<string id=\""))
                    {
                        block.Add(lines[i]);
                        i++;
                    }
                }
                else
                {
                    block.Add(lines[i]);
                    i++;
                }
                var match = Regex.Match(block.Last(), @"<string id=""([^""]+)""");
                string? id = match.Success ? match.Groups[1].Value : null;
                while (result.Count > 0 && string.IsNullOrWhiteSpace(result[^1])) result.RemoveAt(result.Count - 1);
                if (id != null && needBlank.Contains(id)) result.Add("");
                result.AddRange(block);
            }
            File.WriteAllText(path, string.Join("\r\n", result), encoding);
        }
    }
}
