using System.Linq;
using Autodesk.Windows;
using PanelKomplekt.Core;

namespace PanelKomplekt.Ribbon
{
    /// <summary>
    /// Построение вкладки плагина на ленте AutoCAD.
    /// Кнопки создаются автоматически по <see cref="CommandCatalog"/> и группируются по панелям (CommandInfo.RibbonPanel).
    /// </summary>
    public static class RibbonBuilder
    {
        /// <summary>Идентификатор вкладки: по нему проверяется, что вкладка уже создана.</summary>
        private const string TabId = "PANELKOMPLEKT_TAB";

        /// <summary>
        /// Создаёт вкладку, если лента доступна и вкладки ещё нет.
        /// </summary>
        public static void Create()
        {
            var ribbon = ComponentManager.Ribbon;
            if (ribbon == null || ribbon.FindTab(TabId) != null) return;

            var tab = new RibbonTab { Title = "PanelKomplekt", Id = TabId };
            ribbon.Tabs.Add(tab);

            var handler = new RibbonCommandHandler();

            // Порядок панелей и кнопок — как в каталоге команд.
            foreach (var group in CommandCatalog.All.GroupBy(c => c.RibbonPanel))
            {
                var source = new RibbonPanelSource { Title = group.Key };
                tab.Panels.Add(new RibbonPanel { Source = source });

                foreach (var command in group)
                {
                    source.Items.Add(new RibbonButton
                    {
                        Text = command.RibbonText,
                        ShowText = true,
                        Size = RibbonItemSize.Large,
                        Orientation = System.Windows.Controls.Orientation.Vertical,
                        ToolTip = $"{command.Description}\nКоманда: {command.GlobalName}",
                        CommandParameter = command.GlobalName,
                        CommandHandler = handler
                    });
                }
            }
        }
    }
}
