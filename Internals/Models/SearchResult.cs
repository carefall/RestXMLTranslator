using System.Windows.Controls;

namespace RestXMLTranslator.Internals.Models
{
    public class SearchResult
    {
        public FileTab File { get; set; } = null!;

        public string FileName => File.Name;

        public string Id { get; set; } = "";

        public TextBlock PreviewBlock { get; set; } = null!;

    }
}
