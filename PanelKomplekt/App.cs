using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Windows;
using PanelKomplekt.Ribbon;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

// Точка входа плагина. Классы команд регистрируются атрибутом CommandClass в файле каждой команды.
[assembly: ExtensionApplication(typeof(PanelKomplekt.App))]

namespace PanelKomplekt
{
    /// <summary>
    /// Точка входа плагина: вызывается AutoCAD при загрузке сборки (NETLOAD или автозагрузка из .bundle).
    /// Отвечает за создание вкладки на ленте.
    /// </summary>
    public class App : IExtensionApplication
    {
        /// <summary>
        /// Вызывается при загрузке плагина.
        /// </summary>
        public void Initialize()
        {
            // Интерфейс AutoCAD может быть ещё не готов, поэтому сообщение и ленту создаём при первом простое.
            AcApp.Idle += OnFirstIdle;
        }

        /// <summary>
        /// Вызывается при закрытии AutoCAD.
        /// </summary>
        public void Terminate()
        {
        }

        /// <summary>
        /// Первый простой AutoCAD после загрузки: создание ленты и подписка на смену рабочего пространства.
        /// </summary>
        private static void OnFirstIdle(object sender, System.EventArgs e)
        {
            AcApp.Idle -= OnFirstIdle;

            if (ComponentManager.Ribbon != null)
                RibbonBuilder.Create();
            else
                ComponentManager.ItemInitialized += OnRibbonInitialized; // лента появится позже

            // При смене рабочего пространства AutoCAD пересоздаёт ленту — добавляем вкладку заново.
            AcApp.SystemVariableChanged += OnSystemVariableChanged;

            var doc = AcApp.DocumentManager.MdiActiveDocument;
            doc?.Editor.WriteMessage("\nPanelKomplekt загружен. Вкладка «PanelKomplekt» на ленте.\n");
        }

        /// <summary>
        /// Лента создана позже загрузки плагина — добавляем вкладку.
        /// </summary>
        private static void OnRibbonInitialized(object sender, RibbonItemEventArgs e)
        {
            if (ComponentManager.Ribbon == null) return;
            ComponentManager.ItemInitialized -= OnRibbonInitialized;
            RibbonBuilder.Create();
        }

        /// <summary>
        /// Смена рабочего пространства (WSCURRENT) — восстанавливаем вкладку.
        /// </summary>
        private static void OnSystemVariableChanged(object sender, SystemVariableChangedEventArgs e)
        {
            if (e.Name == "WSCURRENT") RibbonBuilder.Create();
        }
    }
}
