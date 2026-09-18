using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using PanelKomplekt.Core;

namespace PanelKomplekt.Commands
{
    /// <summary>
    /// Режим команды C201 «Создание по одному»: каждая панель указывается начальной и конечной точкой.
    /// Ширина, буква марки, тип и цвета задаются опциями командной строки и запоминаются в чертеже.
    /// </summary>
    internal static class SinglePanelCreator
    {
        /// <summary>
        /// Цикл вставки панелей двумя точками до Enter или Esc.
        /// </summary>
        /// <param name="doc">Активный чертёж.</param>
        /// <param name="definitionId">Определение блока PK_Panel в этом чертеже.</param>
        /// <param name="settings">Настройки панелей; опции команды меняют их и сохраняют в чертёж.</param>
        public static void Run(Document doc, ObjectId definitionId, PanelSettings settings)
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
                    if (ChangeSettings(ed, pointResult.StringResult, settings)) PanelPlacement.SaveSettings(db, settings);
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
                    var rotation = PanelPlacement.GetRotation(ucsAxes.Zaxis, jig.Direction);
                    var reference = PanelInserter.Insert(tr, db, db.CurrentSpaceId, definitionId,
                        start, ucsAxes.Zaxis, rotation, jig.Length, settings);
                    mark = PanelReader.Read(reference, tr).Mark;
                    tr.Commit();
                }
                ed.WriteMessage($"\nПанель {mark}: {jig.Length:0} × {settings.Width:0} мм.");
            }
        }

        /// <summary>
        /// Обработка опций командной строки. Возвращает true, если настройки изменились.
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
    }
}
