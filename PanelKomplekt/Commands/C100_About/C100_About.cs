using Autodesk.AutoCAD.Runtime;
using PanelKomplekt.Core;

// Регистрация класса команд: при наличии ExtensionApplication AutoCAD ищет команды только в перечисленных классах.
[assembly: CommandClass(typeof(PanelKomplekt.Commands.C100_About))]

namespace PanelKomplekt.Commands
{
    /// <summary>
    /// C100. Служебная команда: выводит название и версию плагина.
    /// Документация: C100_About.md в папке команды.
    /// </summary>
    public class C100_About
    {
        /// <summary>Имя команды в AutoCAD.</summary>
        public const string GlobalName = "PK_C100_ABOUT";

        /// <summary>Описание команды для ленты и каталога.</summary>
        public static readonly CommandInfo Info = new CommandInfo
        {
            Key = nameof(C100_About),
            Number = "C100",
            GlobalName = GlobalName,
            RibbonText = "О плагине",
            RibbonPanel = "Сервис",
            Description = "Название и версия плагина"
        };

        /// <summary>
        /// Точка входа команды: вывод версии плагина в командную строку.
        /// </summary>
        [CommandMethod(GlobalName)]
        public void Execute()
        {
            CommandRunner.Run(Info, doc =>
            {
                var version = typeof(C100_About).Assembly.GetName().Version;
                doc.Editor.WriteMessage($"\nPanelKomplekt {version} — раскладка сэндвич-панелей и спецификация.\n");
            });
        }
    }
}
