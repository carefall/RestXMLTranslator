namespace RestXMLTranslator.Internals.Models
{
    public class HalfStringEntry : IEntry
    {
        public int Uid { get; set; }

        public string? Text { get; set; }

        public int EditType { get; set; }

        public bool Finished { get; set; }

        public bool HasNewLine { get; set; }

        public string Id { get; set; } = "";
    }
}
