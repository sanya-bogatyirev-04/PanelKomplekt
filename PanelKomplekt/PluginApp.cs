using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Windows;
using PanelKomplekt.Ribbon;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

[assembly: ExtensionApplication(typeof(PanelKomplekt.PluginApp))]
[assembly: CommandClass(typeof(PanelKomplekt.Commands.InfoCommands))]

namespace PanelKomplekt
{
    /// <summary>
    /// Точка входа плагина: вызывается AutoCAD при загрузке сборки (NETLOAD или автозагрузка из .bundle).
    /// </summary>
    public class PluginApp : IExtensionApplication
    {
        public void Initialize()
        {
            // Интерфейс AutoCAD может быть ещё не готов, поэтому сообщение и ленту создаём при первом простое.
            AcApp.Idle += OnFirstIdle;
        }

        public void Terminate()
        {
        }

        private static void OnFirstIdle(object sender, System.EventArgs e)
        {
            AcApp.Idle -= OnFirstIdle;

            if (ComponentManager.Ribbon != null)
                PanelKomplektRibbon.Create();
            else
                ComponentManager.ItemInitialized += OnRibbonInitialized; // лента появится позже

            // При смене рабочего пространства AutoCAD пересоздаёт ленту — добавляем вкладку заново.
            AcApp.SystemVariableChanged += OnSystemVariableChanged;

            var doc = AcApp.DocumentManager.MdiActiveDocument;
            doc?.Editor.WriteMessage("\nPanelKomplekt загружен. Команда PK_INFO — сведения о плагине.\n");
        }

        private static void OnRibbonInitialized(object sender, RibbonItemEventArgs e)
        {
            if (ComponentManager.Ribbon == null) return;
            ComponentManager.ItemInitialized -= OnRibbonInitialized;
            PanelKomplektRibbon.Create();
        }

        private static void OnSystemVariableChanged(object sender, SystemVariableChangedEventArgs e)
        {
            if (e.Name == "WSCURRENT") PanelKomplektRibbon.Create();
        }
    }
}
