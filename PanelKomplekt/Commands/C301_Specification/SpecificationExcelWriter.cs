using ClosedXML.Excel;

namespace PanelKomplekt.Commands
{
    /// <summary>
    /// Запись спецификации в Excel (.xlsx) в оформлении по примеру заказчика.
    /// Вынесено в отдельный класс: библиотека ClosedXML загружается в AutoCAD только при первой выгрузке в Excel,
    /// а не при запуске плагина.
    /// </summary>
    public static class SpecificationExcelWriter
    {
        /// <summary>Число столбцов таблицы.</summary>
        private const int Columns = 8;

        /// <summary>Формат площади: разделитель тысяч и два знака после запятой.</summary>
        private const string AreaFormat = "#,##0.00";

        /// <summary>
        /// Сохраняет спецификацию в файл. Если файл открыт в Excel, будет исключение IOException.
        /// </summary>
        public static void Save(Specification specification, string path)
        {
            using (var workbook = new XLWorkbook())
            {
                var sheet = workbook.Worksheets.Add("Спецификация");
                var row = 1;

                foreach (var group in specification.Groups)
                {
                    row = WriteGroup(sheet, group, row);
                    row++; // пустая строка между группами
                }

                if (specification.Groups.Count > 1)
                {
                    WriteTotalRow(sheet, row, "ИТОГО по всем типам:", specification.TotalCount, specification.TotalArea);
                }

                sheet.Column(1).Width = 14;
                for (var column = 2; column <= 5; column++) sheet.Column(column).Width = column % 2 == 0 ? 10 : 14;
                sheet.Column(6).Width = 12;
                sheet.Column(7).Width = 12;
                sheet.Column(8).Width = 14;
                sheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
                sheet.PageSetup.FitToPages(1, 0);

                workbook.SaveAs(path);
            }
        }

        /// <summary>
        /// Группа: заголовок, шапка из трёх строк, строки марок, итог группы. Возвращает номер следующей строки.
        /// </summary>
        private static int WriteGroup(IXLWorksheet sheet, SpecificationGroup group, int row)
        {
            // Заголовок группы.
            var title = sheet.Range(row, 1, row, Columns).Merge();
            title.Value = group.Title;
            title.Style.Font.Bold = true;
            title.Style.Font.Underline = XLFontUnderlineValues.Single;
            row++;

            // Шапка: три уровня, как в примере заказчика.
            var headerTop = row;
            SetHeader(sheet.Range(row, 1, row + 2, 1), "№");
            SetHeader(sheet.Range(row, 2, row, 5), "Стальные поверхности панелей");
            SetHeader(sheet.Range(row + 1, 2, row + 1, 3), "Наружная");
            SetHeader(sheet.Range(row + 1, 4, row + 1, 5), "Внутренняя");
            SetHeader(sheet.Range(row + 2, 2, row + 2, 2), "RAL");
            SetHeader(sheet.Range(row + 2, 3, row + 2, 3), "Вид поверх.");
            SetHeader(sheet.Range(row + 2, 4, row + 2, 4), "RAL");
            SetHeader(sheet.Range(row + 2, 5, row + 2, 5), "Вид поверх.");
            SetHeader(sheet.Range(row, 6, row + 2, 6), "Длина, мм");
            SetHeader(sheet.Range(row, 7, row + 2, 7), "Кол-во, шт.");
            SetHeader(sheet.Range(row, 8, row + 2, 8), "Площадь, м²");
            row += 3;

            // Строки марок.
            foreach (var item in group.Rows)
            {
                // Марка, RAL и покрытия — всегда текст: иначе Excel превратит «3005» в число.
                sheet.Cell(row, 1).SetValue(item.Mark ?? string.Empty);
                sheet.Cell(row, 2).SetValue(group.RalOut ?? string.Empty);
                sheet.Cell(row, 3).SetValue(group.SurfaceOut ?? string.Empty);
                sheet.Cell(row, 4).SetValue(group.RalIn ?? string.Empty);
                sheet.Cell(row, 5).SetValue(group.SurfaceIn ?? string.Empty);
                sheet.Cell(row, 6).Value = item.Length;
                sheet.Cell(row, 7).Value = item.Count;
                sheet.Cell(row, 8).Value = item.Area;
                sheet.Cell(row, 8).Style.NumberFormat.Format = AreaFormat;
                row++;
            }

            WriteTotalRow(sheet, row, "ИТОГО:", group.TotalCount, group.TotalArea);

            // Рамки всей таблицы группы (от шапки до итога).
            var table = sheet.Range(headerTop, 1, row, Columns);
            table.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            table.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            sheet.Range(headerTop + 3, 2, row, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            return row + 1;
        }

        /// <summary>Ячейка шапки: объединение, жирный шрифт, выравнивание по центру, перенос слов.</summary>
        private static void SetHeader(IXLRange range, string text)
        {
            range.Merge();
            range.Value = text;
            range.Style.Font.Bold = true;
            range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            range.Style.Alignment.WrapText = true;
        }

        /// <summary>Строка итога: подпись на столбцы 1–6, количество и площадь.</summary>
        private static void WriteTotalRow(IXLWorksheet sheet, int row, string label, int count, double area)
        {
            var caption = sheet.Range(row, 1, row, 6).Merge();
            caption.Value = label;
            sheet.Cell(row, 7).Value = count;
            sheet.Cell(row, 8).Value = area;
            sheet.Cell(row, 8).Style.NumberFormat.Format = AreaFormat;
            var totals = sheet.Range(row, 1, row, Columns);
            totals.Style.Font.Bold = true;
            totals.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            totals.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        }
    }
}
