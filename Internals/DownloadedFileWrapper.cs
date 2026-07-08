using RestXMLTranslator.Internals.Models;

namespace RestXMLTranslator.Internals
{
    internal class DownloadedFileWrapper
    {
        public static void FillEntries(DownloadedFile file)
        {
            List<StringEntry> entries = [];
            foreach (var entry in file.HalfEntries)
            {
                var existing = entries.FirstOrDefault(e => e.Id == entry.Id);
                if (existing == null)
                {
                    entries.Add(new StringEntry()
                    {
                        Id = entry.Id!,
                        Ru = entry.Russian ? entry.Text! : "",
                        NewRu = entry.Russian ? entry.Text! : "",
                        Eng = entry.Russian ? "" : entry.Text!,
                        NewEng = entry.Russian ? "" : entry.Text!,
                        downloadedRu = entry.Russian,
                        downloadedEng = !entry.Russian
                    });

                    continue;
                }
                if (entry.Russian)
                {
                    existing.downloadedRu = true;
                    existing.Ru = entry.Text!;
                    existing.NewRu = entry.Text!;
                }
                else
                {
                    existing.downloadedEng = true;
                    existing.Eng = entry.Text!;
                    existing.NewEng = entry.Text!;
                }
            }

            file.Entries = entries;
        }

        public static void FillEntries(IEnumerable<DownloadedFile> files)
        {
            foreach (var file in files)
            {
                FillEntries(file);
            }
        }
    }
}
