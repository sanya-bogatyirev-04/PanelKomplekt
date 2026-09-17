using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;

namespace PanelKomplekt.Core
{
    /// <summary>
    /// Чтение панелей PK_Panel из чертежа: распознавание вставок блока, длина и ширина
    /// из динамических параметров, данные из атрибутов.
    /// </summary>
    public static class PanelReader
    {
        /// <summary>Класс вставки блока — для быстрой проверки ObjectId без открытия объекта.</summary>
        private static readonly RXClass BlockReferenceClass = RXObject.GetClass(typeof(BlockReference));

        /// <summary>
        /// Является ли вставка блока панелью PK_Panel.
        /// У растянутого динамического блока запись блока анонимная («*U12»),
        /// поэтому имя берётся из DynamicBlockTableRecord.
        /// </summary>
        public static bool IsPanel(BlockReference reference, Transaction tr)
        {
            if (reference == null) return false;
            var record = (BlockTableRecord)tr.GetObject(reference.DynamicBlockTableRecord, OpenMode.ForRead);
            return string.Equals(record.Name, PanelBlock.BlockName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Является ли объект с данным ObjectId панелью PK_Panel.
        /// </summary>
        public static bool IsPanel(ObjectId id, Transaction tr)
        {
            if (id.IsNull || id.IsErased || !id.ObjectClass.IsDerivedFrom(BlockReferenceClass)) return false;
            return IsPanel((BlockReference)tr.GetObject(id, OpenMode.ForRead), tr);
        }

        /// <summary>
        /// Читает данные панели. Вставка должна быть панелью (см. <see cref="IsPanel(BlockReference, Transaction)"/>).
        /// </summary>
        public static PanelData Read(BlockReference reference, Transaction tr)
        {
            var data = new PanelData
            {
                Id = reference.ObjectId,
                Handle = reference.Handle.ToString()
            };

            // Длина и ширина — из динамических параметров блока.
            foreach (DynamicBlockReferenceProperty property in reference.DynamicBlockReferencePropertyCollection)
            {
                if (property.PropertyName == PanelBlock.LengthParameter)
                    data.Length = Convert.ToDouble(property.Value);
                else if (property.PropertyName == PanelBlock.WidthParameter)
                    data.Width = Convert.ToDouble(property.Value);
            }

            // Остальные данные — из атрибутов по тегам.
            foreach (ObjectId attributeId in reference.AttributeCollection)
            {
                var attribute = (AttributeReference)tr.GetObject(attributeId, OpenMode.ForRead);
                var value = attribute.TextString?.Trim() ?? string.Empty;
                switch (attribute.Tag.ToUpperInvariant())
                {
                    case PanelBlock.TagPrefix: data.Prefix = value; break;
                    case PanelBlock.TagType: data.Type = value; break;
                    case PanelBlock.TagRalOut: data.RalOut = value; break;
                    case PanelBlock.TagSurfaceOut: data.SurfaceOut = value; break;
                    case PanelBlock.TagRalIn: data.RalIn = value; break;
                    case PanelBlock.TagSurfaceIn: data.SurfaceIn = value; break;
                }
            }

            return data;
        }

        /// <summary>
        /// Данные всех панелей из набора объектов; объекты, не являющиеся панелями, пропускаются.
        /// </summary>
        public static List<PanelData> ReadAll(IEnumerable<ObjectId> ids, Transaction tr)
        {
            var panels = new List<PanelData>();
            foreach (var id in ids)
            {
                if (!IsPanel(id, tr)) continue;
                panels.Add(Read((BlockReference)tr.GetObject(id, OpenMode.ForRead), tr));
            }
            return panels;
        }

        /// <summary>
        /// Данные всех панелей в пространстве модели чертежа.
        /// </summary>
        public static List<PanelData> ReadModelSpace(Database db, Transaction tr)
        {
            var modelSpace = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForRead);
            var ids = new List<ObjectId>();
            foreach (ObjectId id in modelSpace) ids.Add(id);
            return ReadAll(ids, tr);
        }
    }
}
