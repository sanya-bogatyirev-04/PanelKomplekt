using System;
using System.IO;
using Autodesk.AutoCAD.Runtime;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

[assembly: ExtensionApplication(typeof(PanelKomplekt.Loader.LoaderApp))]

namespace PanelKomplekt.Loader
{
    /// <summary>
    /// Точка входа загрузчика установленной версии плагина.
    /// Загружает PanelKomplekt.dll из той же папки, что и сам загрузчик
    /// (C:\Program Files\Autodesk\ApplicationPlugins\PanelKomplekt.bundle\Contents).
    /// Если AutoCAD запущен из Visual Studio (переменная окружения PANELKOMPLEKT_DEV=1),
    /// ничего не загружает: отладочную сборку загрузит start.scr.
    /// </summary>
    public class LoaderApp : IExtensionApplication
    {
        /// <summary>Переменная окружения режима отладки; задаётся в Properties/launchSettings.json основного проекта.</summary>
        public const string DevVariable = "PANELKOMPLEKT_DEV";

        /// <summary>Имя файла основного плагина рядом с загрузчиком.</summary>
        private const string PluginFileName = "PanelKomplekt.dll";

        /// <summary>Сообщение для командной строки; выводится при первом простое AutoCAD.</summary>
        private static string _message;

        /// <summary>
        /// Вызывается AutoCAD при загрузке загрузчика.
        /// </summary>
        public void Initialize()
        {
            try
            {
                if (Environment.GetEnvironmentVariable(DevVariable) == "1")
                {
                    ShowMessage("PanelKomplekt: режим отладки — установленная версия не загружается.");
                    return;
                }

                var folder = Path.GetDirectoryName(typeof(LoaderApp).Assembly.Location);
                var pluginPath = Path.Combine(folder ?? string.Empty, PluginFileName);
                if (!File.Exists(pluginPath))
                {
                    ShowMessage($"PanelKomplekt: не найден файл плагина {pluginPath}. Переустановите плагин.");
                    return;
                }

                // Тот же механизм, что у команды NETLOAD: регистрирует команды и вызывает App.Initialize().
                ExtensionLoader.Load(pluginPath);
            }
            catch (System.Exception ex)
            {
                ShowMessage($"PanelKomplekt: ошибка загрузки плагина: {ex.Message}");
            }
        }

        /// <summary>
        /// Вызывается при закрытии AutoCAD.
        /// </summary>
        public void Terminate()
        {
        }

        /// <summary>
        /// Откладывает вывод сообщения до первого простоя: при загрузке командная строка может быть ещё недоступна.
        /// </summary>
        private static void ShowMessage(string message)
        {
            _message = message;
            AcApp.Idle += OnFirstIdle;
        }

        /// <summary>
        /// Первый простой AutoCAD: вывод отложенного сообщения.
        /// </summary>
        private static void OnFirstIdle(object sender, EventArgs e)
        {
            AcApp.Idle -= OnFirstIdle;
            AcApp.DocumentManager.MdiActiveDocument?.Editor.WriteMessage($"\n{_message}\n");
        }
    }
}
