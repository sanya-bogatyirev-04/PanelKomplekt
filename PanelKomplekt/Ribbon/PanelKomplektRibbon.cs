using Autodesk.Windows;

namespace PanelKomplekt.Ribbon
{
    /// <summary>
    /// Вкладка плагина на ленте AutoCAD. Создаётся программно при каждой загрузке плагина.
    /// </summary>
    public static class PanelKomplektRibbon
    {
        private const string TabId = "PANELKOMPLEKT_TAB";

        public static void Create()
        {
            var ribbon = ComponentManager.Ribbon;
            if (ribbon == null || ribbon.FindTab(TabId) != null) return;

            var tab = new RibbonTab { Title = "PanelKomplekt", Id = TabId };
            ribbon.Tabs.Add(tab);

            var source = new RibbonPanelSource { Title = "Панели" };
            tab.Panels.Add(new RibbonPanel { Source = source });

            source.Items.Add(new RibbonButton
            {
                Text = "О плагине",
                ShowText = true,
                Size = RibbonItemSize.Large,
                Orientation = System.Windows.Controls.Orientation.Vertical,
                CommandParameter = "PK_INFO",
                CommandHandler = new RibbonCommandHandler()
            });
        }
    }
}
