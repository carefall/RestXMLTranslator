using System.IO;
using System.Windows;
using System.Text.Json;

namespace RestXMLTranslator.Internals
{
    internal static class Locale
    {

        private static IReadOnlyDictionary<string, string> locales = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public static void Init()
        {
            try
            {
                string json = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "locale.json"));
                locales = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? throw new Exception("Deserialization of locale.json failed...");
            }
            catch (Exception ex)
            {
                Logger.Log("LocaleLoader", ex.ToString());
                MessageBox.Show("Error! No locale found!\nОшибка! Не найдена локализация!", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
        }

        public static string Get(string key)
        {
            return locales.GetValueOrDefault(key, $"{key}");
        }

        internal static string Get(string key, string val)
        {
            return locales.GetValueOrDefault(key, $"{key}").Replace("%value%", val);
        }
    }
}
