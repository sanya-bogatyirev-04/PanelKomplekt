using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

namespace PanelKomplekt.Core
{
    /// <summary>
    /// Программная вставка панели PK_Panel: вставка блока, атрибуты с настройками,
    /// поле длины, длина и ширина через динамические параметры.
    /// </summary>
    public static class PanelInserter
    {
        /// <summary>Класс определения атрибута — для отбора атрибутов без открытия остальных объектов блока.</summary>
        private static readonly RXClass AttributeDefinitionClass = RXObject.GetClass(typeof(AttributeDefinition));

        /// <summary>
        /// Вставляет панель в указанное пространство чертежа (текущий слой и свойства чертежа).
        /// </summary>
        /// <param name="tr">Открытая транзакция чертежа.</param>
        /// <param name="db">Чертёж.</param>
        /// <param name="spaceId">Пространство (обычно db.CurrentSpaceId).</param>
        /// <param name="definitionId">Определение блока PK_Panel (см. <see cref="PanelBlock.EnsureDefinition"/>).</param>
        /// <param name="position">Базовая точка — левый нижний угол панели, МСК.</param>
        /// <param name="normal">Нормаль плоскости панели (ось Z текущей ПСК).</param>
        /// <param name="rotation">Угол поворота вокруг нормали, радианы.</param>
        /// <param name="length">Длина, мм (будет приведена к правилам блока).</param>
        /// <param name="settings">Ширина, буква, тип и цвета.</param>
        /// <param name="width">Ширина, мм: перекрывает settings.Width (нужна для последней укороченной панели массива).</param>
        /// <returns>Созданная вставка блока (открыта в транзакции).</returns>
        public static BlockReference Insert(Transaction tr, Database db, ObjectId spaceId, ObjectId definitionId,
            Point3d position, Vector3d normal, double rotation, double length, PanelSettings settings, double? width = null)
        {
            var space = (BlockTableRecord)tr.GetObject(spaceId, OpenMode.ForWrite);
            var definition = (BlockTableRecord)tr.GetObject(definitionId, OpenMode.ForRead);

            var reference = new BlockReference(position, definitionId);
            reference.SetDatabaseDefaults(db); // текущий слой, цвет, тип линий чертежа
            reference.Normal = normal;
            reference.Rotation = rotation;
            space.AppendEntity(reference);
            tr.AddNewlyCreatedDBObject(reference, true);

            // 1. Атрибуты — до изменения динамических параметров: действия блока сами сдвинут марку к центру.
            foreach (ObjectId id in definition)
            {
                if (!id.ObjectClass.IsDerivedFrom(AttributeDefinitionClass)) continue;
                var attributeDefinition = (AttributeDefinition)tr.GetObject(id, OpenMode.ForRead);
                if (attributeDefinition.Constant) continue;

                var attribute = new AttributeReference();
                attribute.SetAttributeFromBlock(attributeDefinition, reference.BlockTransform);
                var value = settings.GetAttributeValue(attributeDefinition.Tag);
                if (value != null) attribute.TextString = value;

                reference.AttributeCollection.AppendAttribute(attribute);
                tr.AddNewlyCreatedDBObject(attribute, true);

                if (attributeDefinition.HasFields)
                    CopyFieldResolved(attributeDefinition, attribute, reference, tr);
            }

            // 2. Размеры через динамические параметры (блок сам растянет прямоугольник и сдвинет марку).
            SetDynamicProperty(reference, PanelBlock.LengthParameter, PanelBlock.NormalizeLength(length));
            SetDynamicProperty(reference, PanelBlock.WidthParameter, PanelBlock.NormalizeWidth(width ?? settings.Width));

            // 3. Поле длины пересчитывается сразу — марка правильная без РЕГЕН.
            PanelFieldRefresher.Refresh(reference, tr);
            return reference;
        }

        /// <summary>
        /// Копирует поле из определения атрибута в атрибут вставки, заменяя ссылку «сам блок» (?BlockRefId)
        /// на ID созданной вставки — так же, как это делает команда ВСТАВИТЬ.
        /// </summary>
        internal static void CopyFieldResolved(DBObject source, AttributeReference attribute, BlockReference reference, Transaction tr)
        {
            var sourceFieldId = source.GetField(PanelBlock.TextFieldProperty);
            if (sourceFieldId.IsNull) return;

            var sourceField = (Field)tr.GetObject(sourceFieldId, OpenMode.ForRead);
            var code = sourceField.GetFieldCode(FieldCodeFlags.AddMarkers | FieldCodeFlags.FieldCode);
            SetFieldCode(attribute, reference, code, tr);
        }

        /// <summary>
        /// Назначает атрибуту поле с кодом, в котором ?BlockRefId заменён на ID вставки, и вычисляет его.
        /// </summary>
        internal static void SetFieldCode(AttributeReference attribute, BlockReference reference, string code, Transaction tr)
        {
            var resolved = code.Replace(PanelBlock.BlockReferencePlaceholder,
                $"%<\\_ObjId {reference.ObjectId.OldIdPtr.ToInt64()}>%");

            if (!attribute.IsWriteEnabled) attribute.UpgradeOpen();
            var field = new Field(resolved);
            attribute.SetField(PanelBlock.TextFieldProperty, field);
            tr.AddNewlyCreatedDBObject(field, true);
            field.Evaluate();
        }

        /// <summary>
        /// Задаёт значение динамического свойства вставки по имени (если свойство есть и доступно для записи).
        /// </summary>
        private static void SetDynamicProperty(BlockReference reference, string name, double value)
        {
            foreach (DynamicBlockReferenceProperty property in reference.DynamicBlockReferencePropertyCollection)
            {
                if (property.PropertyName != name || property.ReadOnly) continue;
                property.Value = value;
                return;
            }
        }
    }
}
