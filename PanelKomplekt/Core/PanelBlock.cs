using System;
using System.IO;
using Autodesk.AutoCAD.DatabaseServices;

namespace PanelKomplekt.Core
{
    /// <summary>
    /// Блок сэндвич-панели PK_Panel: имена параметров и атрибутов, путь к файлу-шаблону,
    /// добавление определения блока в чертёж.
    /// Описание блока — Docs/DeveloperGuide.md, раздел «Блок PK_Panel».
    /// </summary>
    public static class PanelBlock
    {
        /// <summary>Имя блока в чертеже.</summary>
        public const string BlockName = "PK_Panel";

        /// <summary>Имя динамического параметра длины (мм).</summary>
        public const string LengthParameter = "Length";

        /// <summary>Имя динамического параметра ширины (мм).</summary>
        public const string WidthParameter = "Width";

        /// <summary>Тег атрибута «Буква марки».</summary>
        public const string TagPrefix = "PREFIX";

        /// <summary>Тег атрибута «Длина, см» (поле от параметра Length).</summary>
        public const string TagLengthCm = "LENGTH_CM";

        /// <summary>Тег скрытого атрибута «Тип панели».</summary>
        public const string TagType = "TYPE";

        /// <summary>Тег скрытого атрибута «RAL снаружи».</summary>
        public const string TagRalOut = "RAL_OUT";

        /// <summary>Тег скрытого атрибута «Покрытие снаружи».</summary>
        public const string TagSurfaceOut = "SURFACE_OUT";

        /// <summary>Тег скрытого атрибута «RAL внутри».</summary>
        public const string TagRalIn = "RAL_IN";

        /// <summary>Тег скрытого атрибута «Покрытие внутри».</summary>
        public const string TagSurfaceIn = "SURFACE_IN";

        /// <summary>Максимальная длина панели, мм (ограничение заказчика).</summary>
        public const double MaxLength = 13600;

        /// <summary>Минимальная длина панели, мм (как у параметра Length в блоке).</summary>
        public const double MinLength = 10;

        /// <summary>Минимальная ширина панели, мм (как у параметра Width в блоке).</summary>
        public const double MinWidth = 100;

        /// <summary>Максимальная ширина панели, мм (как у параметра Width в блоке).</summary>
        public const double MaxWidth = 2000;

        /// <summary>Ширина панели по умолчанию, мм (как в файле-шаблоне).</summary>
        public const double DefaultWidth = 1190;

        /// <summary>Шаг изменения длины и ширины, мм (приращение параметров в блоке).</summary>
        public const double SizeStep = 10;

        /// <summary>
        /// Ссылка «сам блок» в коде поля атрибута. Команда ВСТАВИТЬ заменяет её на ID вставки;
        /// при вставке из кода это делает <see cref="PanelInserter"/>.
        /// </summary>
        public const string BlockReferencePlaceholder = "?BlockRefId";

        /// <summary>Имя свойства текста, к которому привязано поле атрибута.</summary>
        public const string TextFieldProperty = "TEXT";

        /// <summary>
        /// Длина панели по правилам блока: кратно 10 мм, в пределах 10…13600 мм.
        /// </summary>
        public static double NormalizeLength(double length) => Normalize(length, MinLength, MaxLength);

        /// <summary>
        /// Ширина панели по правилам блока: кратно 10 мм, в пределах 100…2000 мм.
        /// </summary>
        public static double NormalizeWidth(double width) => Normalize(width, MinWidth, MaxWidth);

        /// <summary>
        /// Округление до шага <see cref="SizeStep"/> и ограничение диапазоном.
        /// </summary>
        private static double Normalize(double value, double min, double max)
        {
            var rounded = Math.Round(value / SizeStep, MidpointRounding.AwayFromZero) * SizeStep;
            return Math.Max(min, Math.Min(max, rounded));
        }

        /// <summary>Имя файла-шаблона с определением блока.</summary>
        private const string TemplateFileName = "PK_Panel.dwg";

        /// <summary>
        /// Путь к файлу-шаблону: папка Blocks рядом с PanelKomplekt.dll
        /// (bin\Debug\Blocks при отладке, Contents\Blocks в установленном плагине).
        /// </summary>
        public static string TemplatePath =>
            Path.Combine(Path.GetDirectoryName(typeof(PanelBlock).Assembly.Location) ?? string.Empty, "Blocks", TemplateFileName);

        /// <summary>
        /// Возвращает определение блока PK_Panel в чертеже; если его нет — копирует из файла-шаблона.
        /// Уже существующее определение не перезаписывается, чтобы не менять чертежи заказчика.
        /// Вызывать в контексте команды (документ заблокирован).
        /// </summary>
        /// <param name="db">Чертёж.</param>
        /// <param name="imported">true — определение скопировано из шаблона; false — уже было в чертеже.</param>
        /// <returns>ObjectId записи таблицы блоков PK_Panel.</returns>
        public static ObjectId EnsureDefinition(Database db, out bool imported)
        {
            imported = false;
            var existing = FindDefinition(db);
            if (!existing.IsNull) return existing;

            if (!File.Exists(TemplatePath))
                throw new FileNotFoundException($"Не найден файл блока {TemplatePath}. Переустановите плагин.", TemplatePath);

            using (var source = new Database(false, true))
            {
                source.ReadDwgFile(TemplatePath, FileOpenMode.OpenForReadAndAllShare, true, string.Empty);

                var sourceId = FindDefinition(source);
                if (sourceId.IsNull)
                    throw new InvalidOperationException($"В файле {TemplatePath} нет блока {BlockName}.");

                // Вместе с записью блока копируются связанные объекты динамического блока (параметры, действия, поля).
                var ids = new ObjectIdCollection { sourceId };
                source.WblockCloneObjects(ids, db.BlockTableId, new IdMapping(), DuplicateRecordCloning.Ignore, false);
            }

            imported = true;
            var result = FindDefinition(db);
            if (result.IsNull)
                throw new InvalidOperationException($"Не удалось добавить блок {BlockName} в чертёж.");
            return result;
        }

        /// <summary>
        /// Ищет определение блока PK_Panel в чертеже.
        /// </summary>
        /// <returns>ObjectId записи блока или ObjectId.Null, если блока нет.</returns>
        public static ObjectId FindDefinition(Database db)
        {
            using (var tr = db.TransactionManager.StartOpenCloseTransaction())
            {
                var blockTable = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                var id = blockTable.Has(BlockName) ? blockTable[BlockName] : ObjectId.Null;
                tr.Commit();
                return id;
            }
        }
    }
}
