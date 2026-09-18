using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using PanelKomplekt.Core;

namespace PanelKomplekt.Commands
{
    /// <summary>
    /// Режим команды C201 «Создание массива»: параметры задаются в форме, затем указываются
    /// линия раскладки (две точки) и сторона (третья точка). Панели укладываются вдоль линии
    /// перпендикулярно ей, вплотную или с зазором; последняя панель при нехватке места делается уже.
    /// </summary>
    internal static class ArrayPanelCreator
    {
        /// <summary>
        /// Полный сценарий режима: форма параметров, три точки, вставка панелей и итоговое уведомление.
        /// </summary>
        /// <param name="doc">Активный чертёж.</param>
        /// <param name="definitionId">Определение блока PK_Panel в этом чертеже.</param>
        /// <param name="settings">Настройки панелей; форма заполняет их и они сохраняются в чертёж.</param>
        public static void Run(Document doc, ObjectId definitionId, PanelSettings settings)
        {
            var ed = doc.Editor;
            var db = doc.Database;

            if (!PanelArrayForm.Ask(AcadWindow.Main, settings)) return;
            PanelPlacement.SaveSettings(db, settings);

            var ucs = ed.CurrentUserCoordinateSystem;
            var normal = ucs.CoordinateSystem3d.Zaxis;

            var firstResult = ed.GetPoint(new PromptPointOptions("\nНачальная точка линии раскладки: "));
            if (firstResult.Status != PromptStatus.OK) return;

            var secondResult = ed.GetPoint(new PromptPointOptions("\nКонечная точка линии раскладки: ")
            {
                BasePoint = firstResult.Value,
                UseBasePoint = true,
                UseDashedLine = true
            });
            if (secondResult.Status != PromptStatus.OK) return;

            var start = firstResult.Value.TransformBy(ucs);
            var along = PanelPlacement.Project(secondResult.Value.TransformBy(ucs) - start, normal);
            if (along.Length < Tolerance.Global.EqualPoint)
            {
                ed.WriteMessage("\nТочки совпадают: линия раскладки не задана.");
                return;
            }

            var sideResult = ed.GetPoint(new PromptPointOptions("\nСторона, в которую уходит длина панелей: ")
            {
                BasePoint = firstResult.Value,
                UseBasePoint = true,
                UseDashedLine = true
            });
            if (sideResult.Status != PromptStatus.OK) return;

            // Направление укладки — вдоль линии; длина панели — перпендикуляр к линии в сторону третьей точки.
            var stack = along.GetNormal();
            var direction = normal.CrossProduct(stack).GetNormal();
            if (PanelPlacement.Project(sideResult.Value.TransformBy(ucs) - start, normal).DotProduct(direction) < 0)
                direction = direction.Negate();

            var items = PanelArrayPlanner.Plan(along.Length, settings.Width, settings.ArrayGap);
            if (items.Count == 0)
            {
                ed.WriteMessage($"\nПанели не созданы: линия короче минимальной ширины панели ({PanelBlock.MinWidth:0} мм).");
                ShowResult("Панели не созданы: линия короче минимальной ширины панели.", MessageBoxIcon.Warning);
                return;
            }

            Insert(db, definitionId, items, start, stack, direction, normal, settings);

            var truncated = items[items.Count - 1].Truncated;
            var report = new StringBuilder();
            report.AppendLine(truncated ? "Последняя панель была урезана." : "Все панели созданы целыми.");
            report.AppendLine();
            report.Append($"Создано панелей: {items.Count}");
            if (truncated) report.Append($" (последняя шириной {items[items.Count - 1].Width:0} мм)");
            report.Append('.');

            ed.WriteMessage($"\nСоздано панелей: {items.Count}, длина {settings.ArrayLength:0} мм, " +
                            $"ширина {settings.Width:0} мм, зазор {settings.ArrayGap:0} мм.");
            ShowResult(report.ToString(), truncated ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        }

        /// <summary>
        /// Вставка рассчитанных панелей массива одной транзакцией.
        /// </summary>
        /// <param name="start">Первая точка линии раскладки, МСК.</param>
        /// <param name="stack">Направление укладки вдоль линии (единичный вектор).</param>
        /// <param name="direction">Направление длины панели (единичный вектор).</param>
        /// <param name="normal">Нормаль плоскости раскладки (ось Z ПСК).</param>
        private static void Insert(Database db, ObjectId definitionId, IReadOnlyList<PanelArrayItem> items,
            Point3d start, Vector3d stack, Vector3d direction, Vector3d normal, PanelSettings settings)
        {
            var rotation = PanelPlacement.GetRotation(normal, direction);

            // Ширина панели откладывается от базовой точки в сторону normal × direction. Если это направление
            // противоположно укладке, базовая точка берётся у дальнего края полосы, иначе панель уйдёт за линию.
            var forward = normal.CrossProduct(direction).DotProduct(stack) > 0;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                foreach (var item in items)
                {
                    var offset = forward ? item.Offset : item.Offset + item.Width;
                    PanelInserter.Insert(tr, db, db.CurrentSpaceId, definitionId,
                        start + stack * offset, normal, rotation, settings.ArrayLength, settings, item.Width);
                }
                tr.Commit();
            }
        }

        /// <summary>
        /// Итоговое уведомление команды.
        /// </summary>
        private static void ShowResult(string text, MessageBoxIcon icon)
        {
            var name = C201_InsertPanel.Info.RibbonText;
            MessageBox.Show(
                AcadWindow.Main,
                $"{name} выполнена.\n{text}",
                $"Панели — {name}",
                MessageBoxButtons.OK,
                icon);
        }
    }
}
