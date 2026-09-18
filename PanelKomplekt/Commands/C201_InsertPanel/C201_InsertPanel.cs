using Autodesk.AutoCAD.Runtime;
using PanelKomplekt.Core;

// Регистрация класса команд: при наличии ExtensionApplication AutoCAD ищет команды только в перечисленных классах.
[assembly: CommandClass(typeof(PanelKomplekt.Commands.C201_InsertPanel))]

namespace PanelKomplekt.Commands
{
    /// <summary>
    /// C201. Создание панелей PK_Panel. При запуске выбирается режим работы:
    /// «Создание массива» — <see cref="ArrayPanelCreator"/>, «Создание по одному» — <see cref="SinglePanelCreator"/>.
    /// Длина кратна 10 мм и не больше 13600 мм; ширина, буква марки, тип и цвета запоминаются в чертеже.
    /// Документация: C201_InsertPanel.md в папке команды.
    /// </summary>
    public class C201_InsertPanel
    {
        /// <summary>Имя команды в AutoCAD.</summary>
        public const string GlobalName = "PK_C201_INSERTPANEL";

        /// <summary>Описание команды для ленты и каталога.</summary>
        public static readonly CommandInfo Info = new CommandInfo
        {
            Key = nameof(C201_InsertPanel),
            Number = "C201",
            GlobalName = GlobalName,
            RibbonText = "Создание",
            RibbonPanel = "Панели",
            Description = "Создание сэндвич-панелей: массив вдоль линии или по одной панели двумя точками"
        };

        /// <summary>
        /// Точка входа команды: блок в чертеже, настройки, выбор режима работы.
        /// Сам сценарий выполняет класс выбранного режима.
        /// </summary>
        [CommandMethod(GlobalName)]
        public void Execute()
        {
            CommandRunner.Run(Info, doc =>
            {
                var db = doc.Database;

                var definitionId = PanelBlock.EnsureDefinition(db, out var imported);
                if (imported) doc.Editor.WriteMessage($"\nБлок {PanelBlock.BlockName} добавлен в чертёж.");

                PanelSettings settings;
                using (var tr = db.TransactionManager.StartTransaction())
                {
                    settings = PanelSettings.Load(db, tr, definitionId);
                    tr.Commit();
                }

                var mode = PanelModeForm.Ask(AcadWindow.Main);
                if (mode == null) return; // окно закрыто или «Отмена»

                if (mode == PanelCreationMode.Array) ArrayPanelCreator.Run(doc, definitionId, settings);
                else SinglePanelCreator.Run(doc, definitionId, settings);
            });
        }
    }
}
