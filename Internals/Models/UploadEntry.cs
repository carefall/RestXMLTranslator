namespace RestXMLTranslator.Internals.Models
{

    public class UploadEntry
    {
        public string Id { get; set; } = string.Empty;

        public int EditType { get; set; }

        public string Text { get; set; } = string.Empty;

        public string User { get; set; } = string.Empty;

        public bool NewLine { get; set; }

    }
}
