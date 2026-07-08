using System.IO;
using System.Windows;

namespace RestXMLTranslator.Internals
{
    internal static class Logger
    {

        private static readonly string LogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.txt");
        private static readonly object Lock = new();

        public static void Log(string thrower, string message)
        {
            if (!File.Exists(LogPath)) return;
            try
            {
                lock (Lock)
                {
                    using StreamWriter writer = new(LogPath, true);
                    writer.WriteLine($"[{DateTime.Now}] [{thrower}]: {message}");
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Не удалось записать информацию в файл log.txt.\nUnable to write data to log.txt file.", "Логирование / Logging", MessageBoxButton.OK);
            }
        }

        internal static void Setup()
        {
            try
            {
                if (!File.Exists(LogPath)) File.Create("log.txt").Close();
                Log("Logger", "Logging initialized");
            }
            catch (Exception)
            {
                MessageBox.Show("Не удалось записать информацию в файл log.txt.\nUnable to write data to log.txt file.", "Логирование / Logging", MessageBoxButton.OK);
            }
        }
    }
}
