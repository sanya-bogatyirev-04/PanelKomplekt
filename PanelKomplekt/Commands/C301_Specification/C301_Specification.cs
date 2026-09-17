using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using PanelKomplekt.Core;

// Регистрация класса команд: при наличии ExtensionApplication AutoCAD ищет команды только в перечисленных классах.
[assembly: CommandClass(typeof(PanelKomplekt.Commands.C301_Specification))]

namespace PanelKomplekt.Commands
{
    /// <summary>
    /// C301. Спецификация панелей PK_Panel: выбор панелей → расчёт → Excel и/или таблица в чертеже.
    /// Документация: C301_Specification.md в папке команды.
    /// </summary>
    public class C301_Specification
    {
        /// <summary>Имя команды в AutoCAD.</summary>
        public const string GlobalName = "PK_C301_SPECIFICATION";

        /// <summary>Описание команды для ленты и каталога.</summary>
        public static readonly CommandInfo Info = new CommandInfo
        {
            Key = nameof(C301_Specification),
            Number = "C301",
            GlobalName = GlobalName,
            RibbonText = "Спецификация",
            RibbonPanel = "Спецификации",
            Description = "Спецификация панелей: Excel и/или таблица в чертеже"
        };

        /// <summary>Русские числовые форматы для сообщений.</summary>
        private static readonly CultureInfo Russian = CultureInfo.GetCultureInfo("ru-RU");

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

                // 1. Панели: выбор объектов или все панели модели (Enter).
                var panels = SelectPanels(ed, db, out var cancelled);
                if (cancelled) return;
                if (panels.Count == 0)
                {
                    ed.WriteMessage($"\nПанели {PanelBlock.BlockName} не найдены.\n");
                    return;
                }

                // 2. Расчёт.
                var specification = SpecificationBuilder.Build(panels);
                ed.WriteMessage($"\nПанелей: {specification.TotalCount}, типов: {specification.Groups.Count}, " +
                                $"площадь: {specification.TotalArea.ToString("N2", Russian)} м².");
                foreach (var warning in specification.Warnings) ed.WriteMessage($"\n[!] {warning}");

                // 3. Вид результата.
                var output = AskOutput(ed);
                if (output == null) return;

                if (output == "Excel" || output == "Both") SaveExcel(doc, specification);
                if (output == "Table" || output == "Both") InsertTable(doc, specification);
                ed.WriteMessage("\n");
            });
        }

        /// <summary>
        /// Выбор панелей. Enter без выбора — все панели пространства модели.
        /// Чтение — в транзакции только для чтения (OpenCloseTransaction), это быстрее обычной.
        /// </summary>
        private static List<PanelData> SelectPanels(Editor ed, Database db, out bool cancelled)
        {
            cancelled = false;
            var options = new PromptSelectionOptions
            {
                MessageForAdding = "\nВыберите панели для спецификации <Enter — все панели в модели>: "
            };
            var selection = ed.GetSelection(options);
            if (selection.Status == PromptStatus.Cancel)
            {
                cancelled = true;
                return new List<PanelData>();
            }

            using (var tr = db.TransactionManager.StartOpenCloseTransaction())
            {
                var panels = selection.Status == PromptStatus.OK
                    ? PanelReader.ReadAll(selection.Value.GetObjectIds(), tr)
                    : PanelReader.ReadModelSpace(db, tr);
                tr.Commit();
                return panels;
            }
        }

        /// <summary>
        /// Запрос вида результата. Возвращает глобальное имя варианта (Excel, Table, Both) или null при отмене.
        /// </summary>
        private static string AskOutput(Editor ed)
        {
            var options = new PromptKeywordOptions("\nРезультат [Excel/Таблица/Оба] <Excel>: ", "Excel Table Both")
            {
                AllowNone = true
            };
            var result = ed.GetKeywords(options);
            if (result.Status == PromptStatus.None) return "Excel";
            return result.Status == PromptStatus.OK ? result.StringResult : null;
        }

        /// <summary>
        /// Сохранение в Excel: окно выбора файла (по умолчанию рядом с чертежом), запись, открытие файла.
        /// </summary>
        private static void SaveExcel(Document doc, Specification specification)
        {
            var ed = doc.Editor;
            string path;
            using (var dialog = new SaveFileDialog
            {
                Title = "Сохранить спецификацию",
                Filter = "Книга Excel (*.xlsx)|*.xlsx",
                DefaultExt = "xlsx",
                AddExtension = true,
                OverwritePrompt = true,
                FileName = Path.GetFileNameWithoutExtension(doc.Name) + "_Спецификация.xlsx",
                InitialDirectory = GetDrawingFolder(doc)
            })
            {
                if (dialog.ShowDialog(AcadWindow.Main) != DialogResult.OK)
                {
                    ed.WriteMessage("\nСохранение в Excel отменено.");
                    return;
                }
                path = dialog.FileName;
            }

            try
            {
                SpecificationExcelWriter.Save(specification, path);
            }
            catch (IOException)
            {
                ed.WriteMessage($"\nНе удалось записать файл {path}: он открыт в другой программе. Закройте файл и повторите.");
                return;
            }
            ed.WriteMessage($"\nСпецификация сохранена: {path}");

            try
            {
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch (System.Exception)
            {
                ed.WriteMessage("\nФайл сохранён, но открыть его автоматически не удалось.");
            }
        }

        /// <summary>
        /// Вставка таблицы: запрос точки левого верхнего угла и создание таблицы на текущем слое.
        /// </summary>
        private static void InsertTable(Document doc, Specification specification)
        {
            var ed = doc.Editor;
            var db = doc.Database;
            var point = ed.GetPoint(new PromptPointOptions("\nЛевый верхний угол таблицы спецификации: "));
            if (point.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nВставка таблицы отменена.");
                return;
            }

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var position = point.Value.TransformBy(ed.CurrentUserCoordinateSystem);
                SpecificationTableWriter.Insert(tr, db, db.CurrentSpaceId, position, specification);
                tr.Commit();
            }
            ed.WriteMessage("\nТаблица спецификации вставлена.");
        }

        /// <summary>
        /// Папка чертежа; для несохранённого чертежа — «Документы».
        /// </summary>
        private static string GetDrawingFolder(Document doc)
        {
            try
            {
                if (doc.IsNamedDrawing)
                {
                    var folder = Path.GetDirectoryName(doc.Name);
                    if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder)) return folder;
                }
            }
            catch (ArgumentException) { /* некорректный путь — используем «Документы» */ }
            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }
    }
}
