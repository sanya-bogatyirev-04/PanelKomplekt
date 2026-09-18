using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using PanelKomplekt.Core;

// Регистрация класса команд: при наличии ExtensionApplication AutoCAD ищет команды только в перечисленных классах.
[assembly: CommandClass(typeof(PanelKomplekt.Commands.C201_InsertPanel))]

namespace PanelKomplekt.Commands
{
    /// <summary>
    /// C201. Создание панелей PK_Panel. При запуске выбирается режим работы:
    /// «Создание массива» — параметры задаются в форме, панели укладываются перпендикулярно линии из двух точек;
    /// «Создание по одному» — каждая панель указывается начальной и конечной точкой.
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
        /// Точка входа команды: выбор режима работы, затем создание панелей.
        /// </summary>
        [CommandMethod(GlobalName)]
        public void Execute()
        {
            CommandRunner.Run(Info, doc =>
            {
                var ed = doc.Editor;
                var db = doc.Database;

                var definitionId = PanelBlock.EnsureDefinition(db, out var imported);
                if (imported) ed.WriteMessage($"\nБлок {PanelBlock.BlockName} добавлен в чертёж.");

                PanelSettings settings;
                using (var tr = db.TransactionManager.StartTransaction())
                {
                    settings = PanelSettings.Load(db, tr, definitionId);
                    tr.Commit();
                }

                var mode = PanelModeForm.Ask(AcadWindow.Main);
                if (mode == null) return; // окно закрыто или «Отмена»

                if (mode == PanelCreationMode.Array) CreateArray(doc, definitionId, settings);
                else CreateOneByOne(doc, definitionId, settings);
            });
        }

        /// <summary>
        /// Режим «Создание по одному»: цикл вставки панелей двумя точками до Enter или Esc.
        /// </summary>
        private static void CreateOneByOne(Document doc, ObjectId definitionId, PanelSettings settings)
        {
            var ed = doc.Editor;
            var db = doc.Database;

            while (true)
            {
                var pointOptions = new PromptPointOptions(
                    $"\nНачальная точка панели «{settings.Prefix}», ширина {settings.Width:0} мм [Ширина/Буква/Тип]: ",
                    "Width Prefix Type")
                {
                    AllowNone = true // Enter — завершение команды
                };

                var pointResult = ed.GetPoint(pointOptions);
                if (pointResult.Status == PromptStatus.Keyword)
                {
                    if (ChangeSettings(ed, pointResult.StringResult, settings)) SaveSettings(db, settings);
                    continue;
                }
                if (pointResult.Status != PromptStatus.OK) break;

                var ucs = ed.CurrentUserCoordinateSystem;
                var ucsAxes = ucs.CoordinateSystem3d;
                var start = pointResult.Value.TransformBy(ucs);

                var jig = new PanelJig(start, pointResult.Value, ucsAxes.Zaxis, ucsAxes.Xaxis, settings);
                var dragResult = ed.Drag(jig);
                if (dragResult.Status == PromptStatus.Cancel) break;
                if (dragResult.Status != PromptStatus.OK) continue;

                string mark;
                using (var tr = db.TransactionManager.StartTransaction())
                {
                    var rotation = GetRotation(ucsAxes.Zaxis, jig.Direction);
                    var reference = PanelInserter.Insert(tr, db, db.CurrentSpaceId, definitionId,
                        start, ucsAxes.Zaxis, rotation, jig.Length, settings);
                    mark = PanelReader.Read(reference, tr).Mark;
                    tr.Commit();
                }
                ed.WriteMessage($"\nПанель {mark}: {jig.Length:0} × {settings.Width:0} мм.");
            }
        }

        /// <summary>
        /// Режим «Создание массива»: параметры в форме, затем линия раскладки (две точки) и сторона (третья точка).
        /// Панели укладываются вдоль линии вплотную или с зазором, длина каждой панели перпендикулярна линии.
        /// </summary>
        private static void CreateArray(Document doc, ObjectId definitionId, PanelSettings settings)
        {
            var ed = doc.Editor;
            var db = doc.Database;

            if (!PanelArrayForm.Ask(AcadWindow.Main, settings)) return;
            SaveSettings(db, settings);

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
            var along = Project(secondResult.Value.TransformBy(ucs) - start, normal);
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
            if (Project(sideResult.Value.TransformBy(ucs) - start, normal).DotProduct(direction) < 0)
                direction = direction.Negate();

            var items = PanelArrayPlanner.Plan(along.Length, settings.Width, settings.ArrayGap);
            if (items.Count == 0)
            {
                ed.WriteMessage($"\nПанели не созданы: линия короче минимальной ширины панели ({PanelBlock.MinWidth:0} мм).");
                ShowResult(doc, "Панели не созданы: линия короче минимальной ширины панели.", MessageBoxIcon.Warning);
                return;
            }

            InsertItems(db, definitionId, items, start, stack, direction, normal, settings);

            var truncated = items[items.Count - 1].Truncated;
            var report = new StringBuilder();
            report.AppendLine(truncated ? "Последняя панель была урезана." : "Все панели созданы целыми.");
            report.AppendLine();
            report.Append($"Создано панелей: {items.Count}");
            if (truncated) report.Append($" (последняя шириной {items[items.Count - 1].Width:0} мм)");
            report.Append('.');

            ed.WriteMessage($"\nСоздано панелей: {items.Count}, длина {settings.ArrayLength:0} мм, ширина {settings.Width:0} мм, зазор {settings.ArrayGap:0} мм.");
            ShowResult(doc, report.ToString(), truncated ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        }

        /// <summary>
        /// Вставка рассчитанных панелей массива одной транзакцией.
        /// </summary>
        /// <param name="start">Первая точка линии раскладки, МСК.</param>
        /// <param name="stack">Направление укладки вдоль линии (единичный вектор).</param>
        /// <param name="direction">Направление длины панели (единичный вектор).</param>
        /// <param name="normal">Нормаль плоскости раскладки (ось Z ПСК).</param>
        private static void InsertItems(Database db, ObjectId definitionId, IReadOnlyList<PanelArrayItem> items,
            Point3d start, Vector3d stack, Vector3d direction, Vector3d normal, PanelSettings settings)
        {
            var rotation = GetRotation(normal, direction);

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
        private static void ShowResult(Document doc, string text, MessageBoxIcon icon)
        {
            MessageBox.Show(
                AcadWindow.Main,
                $"{Info.RibbonText} выполнена.\n{text}",
                $"Панели — {Info.RibbonText}",
                MessageBoxButtons.OK,
                icon);
        }

        /// <summary>
        /// Проекция вектора на плоскость раскладки (убирает составляющую вдоль нормали).
        /// </summary>
        private static Vector3d Project(Vector3d vector, Vector3d normal) => vector - normal * vector.DotProduct(normal);

        /// <summary>
        /// Угол поворота вставки: от оси X системы координат объекта (по нормали) до направления длины.
        /// </summary>
        private static double GetRotation(Vector3d normal, Vector3d direction)
        {
            var objectXAxis = Vector3d.XAxis.TransformBy(Matrix3d.PlaneToWorld(normal));
            return objectXAxis.GetAngleTo(direction, normal);
        }

        /// <summary>
        /// Обработка опций. Возвращает true, если настройки изменились.
        /// </summary>
        private static bool ChangeSettings(Editor ed, string keyword, PanelSettings settings)
        {
            switch (keyword)
            {
                case "Width":
                {
                    var options = new PromptIntegerOptions($"\nШирина панели, мм ({PanelBlock.MinWidth:0}–{PanelBlock.MaxWidth:0})")
                    {
                        DefaultValue = (int)settings.Width,
                        UseDefaultValue = true,
                        AllowNone = true,
                        LowerLimit = (int)PanelBlock.MinWidth,
                        UpperLimit = (int)PanelBlock.MaxWidth
                    };
                    var result = ed.GetInteger(options);
                    if (result.Status != PromptStatus.OK) return false;
                    var width = PanelBlock.NormalizeWidth(result.Value);
                    if (width != result.Value) ed.WriteMessage($"\nШирина округлена до {width:0} мм (шаг {PanelBlock.SizeStep:0} мм).");
                    settings.Width = width;
                    return true;
                }
                case "Prefix":
                {
                    var value = AskText(ed, "Буква марки", settings.Prefix, allowSpaces: false);
                    if (value == null || value.Length == 0) return false;
                    settings.Prefix = value;
                    return true;
                }
                case "Type":
                {
                    var type = AskText(ed, "Тип панели", settings.Type, allowSpaces: true);
                    if (type == null) return false;
                    settings.Type = type;
                    var ralOut = AskText(ed, "RAL снаружи", settings.RalOut, allowSpaces: true);
                    if (ralOut == null) return true;
                    settings.RalOut = ralOut;
                    var surfaceOut = AskText(ed, "Покрытие снаружи", settings.SurfaceOut, allowSpaces: true);
                    if (surfaceOut == null) return true;
                    settings.SurfaceOut = surfaceOut;
                    var ralIn = AskText(ed, "RAL внутри", settings.RalIn, allowSpaces: true);
                    if (ralIn == null) return true;
                    settings.RalIn = ralIn;
                    var surfaceIn = AskText(ed, "Покрытие внутри", settings.SurfaceIn, allowSpaces: true);
                    if (surfaceIn == null) return true;
                    settings.SurfaceIn = surfaceIn;
                    return true;
                }
                default:
                    return false;
            }
        }

        /// <summary>
        /// Запрос текста со значением по умолчанию. Enter — оставить текущее значение, Esc — null.
        /// </summary>
        private static string AskText(Editor ed, string label, string current, bool allowSpaces)
        {
            var options = new PromptStringOptions($"\n{label} <{current}>: ")
            {
                AllowSpaces = allowSpaces,
                DefaultValue = current ?? string.Empty,
                UseDefaultValue = true
            };
            var result = ed.GetString(options);
            if (result.Status != PromptStatus.OK) return null;
            return string.IsNullOrWhiteSpace(result.StringResult) ? current : result.StringResult.Trim();
        }

        /// <summary>
        /// Сохранение настроек в чертёж.
        /// </summary>
        private static void SaveSettings(Database db, PanelSettings settings)
        {
            using (var tr = db.TransactionManager.StartTransaction())
            {
                settings.Save(db, tr);
                tr.Commit();
            }
        }
    }
}
