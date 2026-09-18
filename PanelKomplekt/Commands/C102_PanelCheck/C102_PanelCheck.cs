using System.Collections.Generic;
using System.Globalization;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using PanelKomplekt.Core;

// Регистрация класса команд: при наличии ExtensionApplication AutoCAD ищет команды только в перечисленных классах.
[assembly: CommandClass(typeof(PanelKomplekt.Commands.C102_PanelCheck))]

namespace PanelKomplekt.Commands
{
    /// <summary>
    /// C102. Служебная команда проверки панелей: добавляет блок PK_Panel в чертёж, если его нет,
    /// и выводит в командную строку данные выбранных панелей (марка, длина, ширина, тип, цвета).
    /// Документация: C102_PanelCheck.md в папке команды.
    /// </summary>
    public class C102_PanelCheck
    {
        /// <summary>Имя команды в AutoCAD.</summary>
        public const string GlobalName = "PK_C102_PANELCHECK";

        /// <summary>Сколько панелей выводить построчно, чтобы не засорять командную строку.</summary>
        private const int MaxListedPanels = 200;

        /// <summary>Описание команды для ленты и каталога.</summary>
        public static readonly CommandInfo Info = new CommandInfo
        {
            Key = nameof(C102_PanelCheck),
            Number = "C102",
            GlobalName = GlobalName,
            RibbonText = "Проверка",
            RibbonPanel = "Панели",
            Description = "Добавляет блок PK_Panel в чертёж и показывает данные выбранных панелей"
        };

        /// <summary>
        /// Точка входа команды.
        /// </summary>
        [CommandMethod(GlobalName)]
        public void Execute()
        {
            CommandRunner.Run(Info, doc =>
            {
                var ed = doc.Editor;
                var db = doc.Database;

                // 1. Определение блока: есть в чертеже или копируется из шаблона.
                PanelBlock.EnsureDefinition(db, out var imported);
                ed.WriteMessage(imported
                    ? $"\nБлок {PanelBlock.BlockName} добавлен в чертёж из шаблона: {PanelBlock.TemplatePath}"
                    : $"\nБлок {PanelBlock.BlockName} уже есть в чертеже.");

                // 2. Выбор объектов; Enter без выбора — все панели пространства модели.
                var options = new PromptSelectionOptions
                {
                    MessageForAdding = "\nВыберите панели для проверки <Enter — все панели в модели>: "
                };
                var selection = ed.GetSelection(options);
                if (selection.Status == PromptStatus.Cancel) return;

                List<PanelData> panels;
                int selectedCount;
                using (var tr = db.TransactionManager.StartTransaction())
                {
                    if (selection.Status == PromptStatus.OK)
                    {
                        var ids = selection.Value.GetObjectIds();
                        selectedCount = ids.Length;
                        panels = PanelReader.ReadAll(ids, tr);
                    }
                    else
                    {
                        panels = PanelReader.ReadModelSpace(db, tr);
                        selectedCount = panels.Count;
                    }
                    tr.Commit();
                }

                // 3. Отчёт.
                ed.WriteMessage($"\nНайдено панелей: {panels.Count}" +
                                (selectedCount > panels.Count ? $" (прочих объектов пропущено: {selectedCount - panels.Count})" : string.Empty));

                for (var i = 0; i < panels.Count && i < MaxListedPanels; i++)
                {
                    ed.WriteMessage("\n  " + Describe(panels[i]));
                }
                if (panels.Count > MaxListedPanels)
                {
                    ed.WriteMessage($"\n  … и ещё {panels.Count - MaxListedPanels}");
                }
                ed.WriteMessage("\n");
            });
        }

        /// <summary>
        /// Строка с данными панели и предупреждениями для командной строки.
        /// </summary>
        private static string Describe(PanelData panel)
        {
            var culture = CultureInfo.InvariantCulture;
            var text = $"ID {panel.Handle}: {panel.Mark}  длина {panel.Length.ToString("0.##", culture)} × ширина {panel.Width.ToString("0.##", culture)} мм" +
                       $"  тип «{panel.Type}»  снаружи {panel.RalOut}/{panel.SurfaceOut}  внутри {panel.RalIn}/{panel.SurfaceIn}";

            if (panel.Length > PanelBlock.MaxLength)
                text += $"  [!] длиннее {PanelBlock.MaxLength} мм";
            if (string.IsNullOrEmpty(panel.Prefix))
                text += "  [!] пустая буква марки";
            if (string.IsNullOrEmpty(panel.Type))
                text += "  [!] пустой тип";

            return text;
        }
    }
}
