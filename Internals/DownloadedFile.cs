namespace RestXMLTranslator.Internals
{
    public class DownloadedFile
    {
        public string Path { get; set; } = string.Empty;

        public List<StringEntry> Entries { get; set; } = [];

        public List<HalfStringEntry> HalfEntries { get; set; } = [];
    }
}
