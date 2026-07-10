namespace RestXMLTranslator.Internals.Program
{
    internal class XamlLocaleAccessor
    {
        public string this[string key] => Locale.Get(key);
    }
}
