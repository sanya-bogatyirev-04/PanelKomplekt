using System.Text;
using System.Windows.Forms;
using Autodesk.AutoCAD.Runtime;
using PanelKomplekt.Core;

// Регистрация класса команд: при наличии ExtensionApplication AutoCAD ищет команды только в перечисленных классах.
[assembly: CommandClass(typeof(PanelKomplekt.Commands.C100_About))]

namespace PanelKomplekt.Commands
{
    /// <summary>
    /// C100. Служебная команда: окно со сведениями о плагине —
    /// версия, заказчик, разработчик и применяемые технологии.
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
            Description = "Сведения о плагине: версия, заказчик, разработчик, технологии"
        };

        /// <summary>
        /// Точка входа команды: всплывающее окно со сведениями о плагине.
        /// </summary>
        [CommandMethod(GlobalName)]
        public void Execute()
        {
            CommandRunner.Run(Info, doc =>
            {
                // Версия дублируется в командную строку: её удобно скопировать в сообщение об ошибке.
                doc.Editor.WriteMessage($"\n{PluginInfo.Name} {PluginInfo.Version}\n");

                MessageBox.Show(
                    AcadWindow.Main,
                    BuildText(),
                    $"О плагине {PluginInfo.Name}",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            });
        }

        /// <summary>
        /// Текст окна «О плагине».
        /// </summary>
        private static string BuildText()
        {
            var text = new StringBuilder();
            text.AppendLine($"{PluginInfo.Name}, версия {PluginInfo.Version}");
            text.AppendLine(PluginInfo.Purpose);
            text.AppendLine();
            text.AppendLine($"Создан для: {PluginInfo.Customer}");
            text.AppendLine($"Разработчик: {PluginInfo.Author}");
            text.AppendLine();
            text.AppendLine("Технологии:");
            foreach (var technology in PluginInfo.Technologies)
            {
                text.AppendLine($"  • {technology}");
            }
            return text.ToString().TrimEnd();
        }
    }
}
