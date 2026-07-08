namespace RestXMLTranslator
{
    internal class DynamicLoc
    {

        public static void Init(string approve, string ttapprove, string decline, string ttdecline)
        {
            LocaleDictionary["btn_approve"] = approve;
            LocaleDictionary["btn_decline"] = decline;
            LocaleDictionary["tip_approve"] = ttapprove;
            LocaleDictionary["tip_decline"] = ttdecline;
        }

        public string this[string key] => LocaleDictionary[key];

        public static Dictionary<string, string> LocaleDictionary { get; set; } = [];
    }
}
