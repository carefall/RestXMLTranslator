namespace RestXMLTranslator.Internals.Models
{
    public interface IEntry
    {
        public bool HasNewLine { get; set; }

        public string Id { get; set; }
    }
}
