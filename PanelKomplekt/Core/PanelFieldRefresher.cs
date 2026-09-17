using Autodesk.AutoCAD.DatabaseServices;

namespace PanelKomplekt.Core
{
    /// <summary>
    /// Пересчёт полей в атрибутах одной панели (марка «длина в см» после растягивания).
    /// Пересчитываются только поля этой панели, без РЕГЕН всего чертежа.
    /// </summary>
    public static class PanelFieldRefresher
    {
        /// <summary>
        /// Пересчитывает поля во всех атрибутах вставки. Если поле осталось с неразрешённой ссылкой ?BlockRefId
        /// (вставка создана не командой ВСТАВИТЬ и не плагином), ссылка заменяется на ID вставки.
        /// </summary>
        /// <returns>Количество пересчитанных полей.</returns>
        public static int Refresh(BlockReference reference, Transaction tr)
        {
            var count = 0;
            foreach (ObjectId attributeId in reference.AttributeCollection)
            {
                if (attributeId.IsErased) continue;
                var attribute = (AttributeReference)tr.GetObject(attributeId, OpenMode.ForRead);
                if (!attribute.HasFields) continue;

                var fieldId = attribute.GetField(PanelBlock.TextFieldProperty);
                if (fieldId.IsNull) continue;

                var field = (Field)tr.GetObject(fieldId, OpenMode.ForWrite);
                var code = field.GetFieldCode(FieldCodeFlags.AddMarkers | FieldCodeFlags.FieldCode);
                if (code.Contains(PanelBlock.BlockReferencePlaceholder))
                {
                    PanelInserter.SetFieldCode(attribute, reference, code, tr);
                }
                else
                {
                    // Поле пишет вычисленный текст в атрибут. Только что созданный атрибут уже открыт на запись,
                    // а повторный UpgradeOpen для такого объекта вызывает исключение.
                    if (!attribute.IsWriteEnabled) attribute.UpgradeOpen();
                    field.Evaluate();
                }
                count++;
            }
            return count;
        }
    }
}
