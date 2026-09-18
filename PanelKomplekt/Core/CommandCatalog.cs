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
        /// <summary>
        /// Начало адреса пользовательских страниц команд на GitHub. Страница blob показывает Markdown
        /// с оформлением и открывается без входа в GitHub, пока репозиторий публичный.
        /// Адрес ведёт на ветку master, поэтому у пользователя со старой версией плагина
        /// откроется описание текущей версии.
        /// </summary>
        private const string HelpBaseUrl = "https://github.com/sanya-bogatyirev-04/PanelKomplekt/blob/master/Docs/Commands/";

        /// <summary>Все команды в порядке отображения на ленте.</summary>
        public static IReadOnlyList<CommandInfo> All { get; } = new[]
        {
            // Порядок разделов ленты: Сервис, Элементы, Панели, Спецификации.
            // Команды с одинаковым RibbonPanel должны идти подряд — иначе раздел разделится на два.
            C100_About.Info,
            C101_ShowElementId.Info,
            C201_InsertPanel.Info,
            C102_PanelCheck.Info,
            C301_Specification.Info,
        };

        /// <summary>
        /// Адрес справки для каждой команды: имя страницы совпадает с ключом команды,
        /// поэтому в описании команды адрес не дублируется.
        /// Статические поля инициализируются до тела статического конструктора, поэтому список All уже готов.
        /// </summary>
        static CommandCatalog()
        {
            foreach (var command in All) command.HelpUrl = HelpBaseUrl + command.Key + ".md";
        }
    }
}
