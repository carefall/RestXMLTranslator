using RestXMLTranslator.Internals.Program;
using RestXMLTranslator.Internals.Services;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Windows;
using System.Xml;

namespace RestXMLTranslator
{
    public partial class App : Application
    {

        static App()
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            Locale.Init();
            Logger.Setup();
        }

        public static new App Current => (App)Application.Current;

        public readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        };

        public readonly XmlWriterSettings XmlSettings = new()
        {
            Encoding = Encoding.GetEncoding(1251),
            Indent = true
        };

        public LocalFileService LocalFiles { get; } = new();
        public SyncService SyncService { get; } = new();
        public Settings Settings { get; }

        public App()
        {
            Settings = new Settings();
            new StartupWindow().Show();
        }
    }
}
