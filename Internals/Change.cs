namespace RestXMLTranslator.Internals
{
    public class Change(string Id, string Ru, string Eng, bool IsApproved)
    {
        public string Id { get; set; } = Id;

        public string Ru { get; set; } = Ru;

        public string Eng { get; set; } = Eng;

        public bool IsApproved { get; set; } = IsApproved;
    }
}
