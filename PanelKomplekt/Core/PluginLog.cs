using System;
using System.IO;
using System.Text;

namespace PanelKomplekt.Core
{
    /// <summary>
    /// Журнал ошибок плагина: текстовый файл в папке пользователя.
    /// Нужен, чтобы в командную строку AutoCAD выводилось короткое понятное сообщение,
    /// а подробности (стек вызовов) оставались для разбора разработчиком.
    /// Запись в журнал никогда не прерывает работу команды: все ошибки самого журнала подавляются.
    /// </summary>
    public static class PluginLog
    {
        /// <summary>Размер, после которого журнал начинается заново, байт.</summary>
        private const long MaxSize = 1024 * 1024;

        /// <summary>Замок на случай одновременной записи из нескольких чертежей.</summary>
        private static readonly object Lock = new object();

        /// <summary>
        /// Путь к файлу журнала: %LOCALAPPDATA%\PanelKomplekt\PanelKomplekt.log.
        /// Папка пользователя выбрана намеренно: запись туда не требует прав администратора.
        /// </summary>
        public static string FilePath { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PanelKomplekt", "PanelKomplekt.log");

        /// <summary>
        /// Записывает ошибку команды: время, версия плагина, номер команды и полный текст исключения.
        /// </summary>
        /// <param name="source">Источник — обычно номер команды, например «C301».</param>
        /// <param name="exception">Перехваченное исключение.</param>
        /// <returns>Путь к файлу журнала или null, если записать не удалось.</returns>
        public static string WriteError(string source, Exception exception)
        {
            var text = new StringBuilder();
            text.AppendLine(new string('-', 70));
            text.AppendLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  {PluginInfo.Name} {PluginInfo.Version}  [{source}]");
            text.AppendLine(exception?.ToString() ?? "Исключение отсутствует.");
            return Append(text.ToString());
        }

        /// <summary>
        /// Дописывает текст в журнал, создавая папку и обрезая разросшийся файл.
        /// </summary>
        /// <returns>Путь к файлу журнала или null при любой ошибке записи.</returns>
        private static string Append(string text)
        {
            try
            {
                lock (Lock)
                {
                    var folder = Path.GetDirectoryName(FilePath);
                    if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);

                    // Журнал не должен расти бесконечно: предыдущий файл сохраняется рядом с расширением .old.
                    var file = new FileInfo(FilePath);
                    if (file.Exists && file.Length > MaxSize)
                    {
                        var previous = FilePath + ".old";
                        if (File.Exists(previous)) File.Delete(previous);
                        File.Move(FilePath, previous);
                    }

                    File.AppendAllText(FilePath, text, Encoding.UTF8);
                    return FilePath;
                }
            }
            catch (Exception)
            {
                // Нет прав, диск занят, путь недоступен — команда всё равно должна завершиться сообщением.
                return null;
            }
        }
    }
}
