using System.Collections.Generic;
using PanelKomplekt.Commands;

namespace PanelKomplekt.Core
{
    /// <summary>
    /// Единый список всех команд плагина. По нему строится вкладка на ленте.
    /// При добавлении новой команды её Info нужно дописать сюда.
    /// </summary>
    public static class CommandCatalog
    {
        /// <summary>Все команды в порядке отображения на ленте.</summary>
        public static IReadOnlyList<CommandInfo> All { get; } = new[]
        {
            // Рабочие команды — первыми, служебные — в конце вкладки.
            C201_InsertPanel.Info,
            C100_About.Info,
            C101_ShowElementId.Info,
            C102_PanelCheck.Info,
        };
    }
}
