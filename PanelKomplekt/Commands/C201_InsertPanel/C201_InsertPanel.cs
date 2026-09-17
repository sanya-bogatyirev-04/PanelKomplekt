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
    /// C201. Вставка панели PK_Panel: начальная точка → конечная точка (или длина с клавиатуры).
    /// Направление панели — по двум точкам, длина кратна 10 мм и не больше 13600 мм.
    /// Ширина, буква марки, тип и цвета задаются опциями и запоминаются в чертеже.
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
            RibbonText = "Панель",
            RibbonPanel = "Панели",
            Description = "Вставка сэндвич-панели: начальная и конечная точка, марка «буква + длина в см»"
        };

        /// <summary>
        /// Точка входа команды: цикл вставки панелей до Enter или Esc.
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
            });
        }

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
