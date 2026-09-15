using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using PanelKomplekt.Core;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

// Регистрация класса команд: при наличии ExtensionApplication AutoCAD ищет команды только в перечисленных классах.
[assembly: CommandClass(typeof(PanelKomplekt.Commands.C101_ShowElementId))]

namespace PanelKomplekt.Commands
{
    /// <summary>
    /// C101. Тестовая команда: пользователь выбирает один элемент,
    /// команда показывает всплывающее окно с его ID (дескриптор, Handle).
    /// Документация: C101_ShowElementId.txt в папке команды.
    /// </summary>
    public class C101_ShowElementId
    {
        /// <summary>Имя команды в AutoCAD.</summary>
        public const string GlobalName = "PK_C101_SHOWELEMENTID";

        /// <summary>Описание команды для ленты и каталога.</summary>
        public static readonly CommandInfo Info = new CommandInfo
        {
            Number = "C101",
            GlobalName = GlobalName,
            RibbonText = "ID элемента",
            RibbonPanel = "Сервис",
            Description = "Показывает ID (дескриптор) выбранного элемента"
        };

        /// <summary>
        /// Точка входа команды: выбор одного элемента и вывод его ID во всплывающем окне.
        /// </summary>
        [CommandMethod(GlobalName)]
        public void Execute()
        {
            CommandRunner.Run(Info, doc =>
            {
                var options = new PromptEntityOptions("\nВыберите элемент: ")
                {
                    AllowNone = false
                };

                var result = doc.Editor.GetEntity(options);
                if (result.Status != PromptStatus.OK) return; // пользователь отменил выбор (Esc)

                // Handle — постоянный ID объекта в чертеже: не меняется при сохранении и повторном открытии,
                // совпадает с «Метка» в команде СПИСОК. ObjectId, наоборот, действует только в текущем сеансе.
                var id = result.ObjectId.Handle.ToString();

                AcApp.ShowAlertDialog($"ID выбранного элемента = {id}");
            });
        }
    }
}
