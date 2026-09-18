using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;

namespace PanelKomplekt.Core
{
    /// <summary>
    /// Настройки новых панелей: ширина, буква марки, тип, цвета и покрытия.
    /// Хранятся в самом чертеже (словарь PanelKomplekt → запись PanelSettings),
    /// поэтому при повторном открытии чертежа команда «Панель» продолжает с последних значений.
    /// </summary>
    public sealed class PanelSettings
    {
        /// <summary>Имя словаря плагина в словаре именованных объектов чертежа.</summary>
        private const string DictionaryName = "PanelKomplekt";

        /// <summary>Имя записи с настройками панелей.</summary>
        private const string RecordName = "PanelSettings";

        /// <summary>
        /// Версия формата записи. 1 — восемь значений (без параметров массива), 2 — добавлены длина и зазор.
        /// Записи версии 1 читаются по-прежнему, поэтому чертежи прежних версий плагина не теряют настройки.
        /// </summary>
        private const short FormatVersion = 2;

        /// <summary>Ширина панели, мм.</summary>
        public double Width { get; set; } = PanelBlock.DefaultWidth;

        /// <summary>Буква марки.</summary>
        public string Prefix { get; set; } = string.Empty;

        /// <summary>Тип панели, например «ПСБ-120».</summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>RAL снаружи.</summary>
        public string RalOut { get; set; } = string.Empty;

        /// <summary>Покрытие снаружи.</summary>
        public string SurfaceOut { get; set; } = string.Empty;

        /// <summary>RAL внутри.</summary>
        public string RalIn { get; set; } = string.Empty;

        /// <summary>Покрытие внутри.</summary>
        public string SurfaceIn { get; set; } = string.Empty;

        /// <summary>Длина панели для режима «массив», мм (в режиме «по одному» длина задаётся точками).</summary>
        public double ArrayLength { get; set; } = PanelBlock.DefaultLength;

        /// <summary>Зазор между соседними панелями в массиве, мм (0 — панели вплотную).</summary>
        public double ArrayGap { get; set; }

        /// <summary>
        /// Значение атрибута для тега; для тегов без настройки (например, LENGTH_CM) — null.
        /// </summary>
        public string GetAttributeValue(string tag)
        {
            switch (tag.ToUpperInvariant())
            {
                case PanelBlock.TagPrefix: return Prefix;
                case PanelBlock.TagType: return Type;
                case PanelBlock.TagRalOut: return RalOut;
                case PanelBlock.TagSurfaceOut: return SurfaceOut;
                case PanelBlock.TagRalIn: return RalIn;
                case PanelBlock.TagSurfaceIn: return SurfaceIn;
                default: return null;
            }
        }

        /// <summary>
        /// Загружает настройки: сначала значения по умолчанию из атрибутов определения блока,
        /// затем сохранённые в чертеже значения (если есть).
        /// </summary>
        public static PanelSettings Load(Database db, Transaction tr, ObjectId definitionId)
        {
            var settings = FromDefinition(definitionId, tr);

            var namedObjects = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead);
            if (!namedObjects.Contains(DictionaryName)) return settings;
            var dictionary = tr.GetObject(namedObjects.GetAt(DictionaryName), OpenMode.ForRead) as DBDictionary;
            if (dictionary == null || !dictionary.Contains(RecordName)) return settings;
            var record = tr.GetObject(dictionary.GetAt(RecordName), OpenMode.ForRead) as Xrecord;
            var values = record?.Data?.AsArray();
            if (values == null || values.Length < 8) return settings;
            var version = Convert.ToInt16(values[0].Value);
            if (version != 1 && version != FormatVersion) return settings;

            settings.Width = PanelBlock.NormalizeWidth(Convert.ToDouble(values[1].Value));
            settings.Prefix = Convert.ToString(values[2].Value);
            settings.Type = Convert.ToString(values[3].Value);
            settings.RalOut = Convert.ToString(values[4].Value);
            settings.SurfaceOut = Convert.ToString(values[5].Value);
            settings.RalIn = Convert.ToString(values[6].Value);
            settings.SurfaceIn = Convert.ToString(values[7].Value);

            // Параметры массива появились в версии 2: в записях версии 1 их нет, остаются значения по умолчанию.
            if (version >= 2 && values.Length >= 10)
            {
                settings.ArrayLength = PanelBlock.NormalizeLength(Convert.ToDouble(values[8].Value));
                settings.ArrayGap = PanelBlock.NormalizeGap(Convert.ToDouble(values[9].Value));
            }
            return settings;
        }

        /// <summary>
        /// Сохраняет настройки в чертёж (создаёт словарь и запись при необходимости).
        /// </summary>
        public void Save(Database db, Transaction tr)
        {
            var namedObjects = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead);
            DBDictionary dictionary;
            if (namedObjects.Contains(DictionaryName))
            {
                dictionary = (DBDictionary)tr.GetObject(namedObjects.GetAt(DictionaryName), OpenMode.ForWrite);
            }
            else
            {
                namedObjects.UpgradeOpen();
                dictionary = new DBDictionary();
                namedObjects.SetAt(DictionaryName, dictionary);
                tr.AddNewlyCreatedDBObject(dictionary, true);
            }

            var data = new ResultBuffer(
                new TypedValue((int)DxfCode.Int16, FormatVersion),
                new TypedValue((int)DxfCode.Real, Width),
                new TypedValue((int)DxfCode.Text, Prefix ?? string.Empty),
                new TypedValue((int)DxfCode.Text, Type ?? string.Empty),
                new TypedValue((int)DxfCode.Text, RalOut ?? string.Empty),
                new TypedValue((int)DxfCode.Text, SurfaceOut ?? string.Empty),
                new TypedValue((int)DxfCode.Text, RalIn ?? string.Empty),
                new TypedValue((int)DxfCode.Text, SurfaceIn ?? string.Empty),
                new TypedValue((int)DxfCode.Real, ArrayLength),
                new TypedValue((int)DxfCode.Real, ArrayGap));

            if (dictionary.Contains(RecordName))
            {
                var record = (Xrecord)tr.GetObject(dictionary.GetAt(RecordName), OpenMode.ForWrite);
                record.Data = data;
            }
            else
            {
                var record = new Xrecord { Data = data };
                dictionary.SetAt(RecordName, record);
                tr.AddNewlyCreatedDBObject(record, true);
            }
        }

        /// <summary>
        /// Значения по умолчанию из атрибутов определения блока PK_Panel.
        /// </summary>
        private static PanelSettings FromDefinition(ObjectId definitionId, Transaction tr)
        {
            var settings = new PanelSettings();
            var attributeClass = RXObject.GetClass(typeof(AttributeDefinition));
            var definition = (BlockTableRecord)tr.GetObject(definitionId, OpenMode.ForRead);
            foreach (ObjectId id in definition)
            {
                if (!id.ObjectClass.IsDerivedFrom(attributeClass)) continue;
                var attribute = (AttributeDefinition)tr.GetObject(id, OpenMode.ForRead);
                var value = attribute.TextString ?? string.Empty;
                switch (attribute.Tag.ToUpperInvariant())
                {
                    case PanelBlock.TagPrefix: settings.Prefix = value; break;
                    case PanelBlock.TagType: settings.Type = value; break;
                    case PanelBlock.TagRalOut: settings.RalOut = value; break;
                    case PanelBlock.TagSurfaceOut: settings.SurfaceOut = value; break;
                    case PanelBlock.TagRalIn: settings.RalIn = value; break;
                    case PanelBlock.TagSurfaceIn: settings.SurfaceIn = value; break;
                }
            }
            return settings;
        }
    }
}
