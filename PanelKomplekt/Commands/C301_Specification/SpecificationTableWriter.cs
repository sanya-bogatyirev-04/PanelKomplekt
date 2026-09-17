using System.Globalization;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace PanelKomplekt.Commands
{
    /// <summary>
    /// Вставка спецификации в чертёж таблицей AutoCAD (одна таблица на все группы) на текущий слой.
    /// Размеры рассчитаны на чертёж в миллиметрах в масштабе 1:1, как у панелей.
    /// </summary>
    public static class SpecificationTableWriter
    {
        /// <summary>Число столбцов.</summary>
        private const int Columns = 8;

        /// <summary>Высота текста данных и шапки, мм.</summary>
        private const double TextHeight = 250;

        /// <summary>Высота текста заголовков групп и общего итога, мм.</summary>
        private const double TitleTextHeight = 350;

        /// <summary>Высота строки, мм (AutoCAD увеличит при переносе текста).</summary>
        private const double RowHeight = 600;

        /// <summary>Ширина столбцов, мм: №, RAL, вид, RAL, вид, длина, кол-во, площадь.</summary>
        private static readonly double[] ColumnWidths = { 2200, 1400, 2200, 1400, 2200, 1900, 1900, 2300 };

        /// <summary>Русские числовые форматы: пробел между тысячами, запятая в дробях.</summary>
        private static readonly CultureInfo Russian = CultureInfo.GetCultureInfo("ru-RU");

        /// <summary>
        /// Создаёт таблицу в пространстве чертежа. Точка — левый верхний угол таблицы.
        /// </summary>
        public static Table Insert(Transaction tr, Database db, ObjectId spaceId, Point3d position, Specification specification)
        {
            var rows = CountRows(specification);
            var table = new Table();
            table.SetDatabaseDefaults(db); // текущий слой чертежа
            table.TableStyle = db.Tablestyle;
            table.SetSize(rows, Columns);
            table.Position = position;
            table.SetRowHeight(RowHeight);
            for (var column = 0; column < Columns; column++) table.Columns[column].Width = ColumnWidths[column];

            var row = 0;
            foreach (var group in specification.Groups)
            {
                row = WriteGroup(table, group, row);
            }
            if (specification.Groups.Count > 1)
            {
                WriteTotalRow(table, row, "ИТОГО по всем типам:", specification.TotalCount, specification.TotalArea, TitleTextHeight);
            }

            table.GenerateLayout();

            var space = (BlockTableRecord)tr.GetObject(spaceId, OpenMode.ForWrite);
            space.AppendEntity(table);
            tr.AddNewlyCreatedDBObject(table, true);
            return table;
        }

        /// <summary>
        /// Количество строк: на группу — заголовок, 3 строки шапки, строки марок, итог; плюс общий итог при нескольких группах.
        /// </summary>
        private static int CountRows(Specification specification)
        {
            var rows = 0;
            foreach (var group in specification.Groups) rows += 1 + 3 + group.Rows.Count + 1;
            if (specification.Groups.Count > 1) rows++;
            return rows;
        }

        /// <summary>
        /// Группа: заголовок, шапка, строки, итог. Возвращает индекс следующей строки.
        /// </summary>
        private static int WriteGroup(Table table, SpecificationGroup group, int row)
        {
            // Заголовок группы на всю ширину.
            SetRowStyle(table, row, "_TITLE");
            Merge(table, row, 0, row, Columns - 1);
            SetText(table, row, 0, group.Title, TitleTextHeight, CellAlignment.MiddleLeft);
            row++;

            // Шапка из трёх строк.
            for (var i = 0; i < 3; i++) SetRowStyle(table, row + i, "_HEADER");
            Header(table, row, 0, row + 2, 0, "№");
            Header(table, row, 1, row, 4, "Стальные поверхности панелей");
            Header(table, row + 1, 1, row + 1, 2, "Наружная");
            Header(table, row + 1, 3, row + 1, 4, "Внутренняя");
            Header(table, row + 2, 1, row + 2, 1, "RAL");
            Header(table, row + 2, 2, row + 2, 2, "Вид поверх.");
            Header(table, row + 2, 3, row + 2, 3, "RAL");
            Header(table, row + 2, 4, row + 2, 4, "Вид поверх.");
            Header(table, row, 5, row + 2, 5, "Длина, мм");
            Header(table, row, 6, row + 2, 6, "Кол-во, шт.");
            Header(table, row, 7, row + 2, 7, "Площадь, м²");
            row += 3;

            // Строки марок.
            foreach (var item in group.Rows)
            {
                SetRowStyle(table, row, "_DATA");
                SetText(table, row, 0, item.Mark, TextHeight, CellAlignment.MiddleCenter);
                SetText(table, row, 1, group.RalOut, TextHeight, CellAlignment.MiddleCenter);
                SetText(table, row, 2, group.SurfaceOut, TextHeight, CellAlignment.MiddleCenter);
                SetText(table, row, 3, group.RalIn, TextHeight, CellAlignment.MiddleCenter);
                SetText(table, row, 4, group.SurfaceIn, TextHeight, CellAlignment.MiddleCenter);
                SetText(table, row, 5, item.Length.ToString("0", Russian), TextHeight, CellAlignment.MiddleRight);
                SetText(table, row, 6, item.Count.ToString(Russian), TextHeight, CellAlignment.MiddleRight);
                SetText(table, row, 7, item.Area.ToString("N2", Russian), TextHeight, CellAlignment.MiddleRight);
                row++;
            }

            WriteTotalRow(table, row, "ИТОГО:", group.TotalCount, group.TotalArea, TextHeight);
            return row + 1;
        }

        /// <summary>Строка итога: подпись на столбцы 0–5, количество и площадь.</summary>
        private static void WriteTotalRow(Table table, int row, string label, int count, double area, double height)
        {
            SetRowStyle(table, row, "_DATA");
            Merge(table, row, 0, row, 5);
            SetText(table, row, 0, label, height, CellAlignment.MiddleLeft);
            SetText(table, row, 6, count.ToString(Russian), height, CellAlignment.MiddleRight);
            SetText(table, row, 7, area.ToString("N2", Russian), height, CellAlignment.MiddleRight);
        }

        /// <summary>Ячейка шапки (с объединением диапазона).</summary>
        private static void Header(Table table, int row1, int column1, int row2, int column2, string text)
        {
            if (row1 != row2 || column1 != column2) Merge(table, row1, column1, row2, column2);
            SetText(table, row1, column1, text, TextHeight, CellAlignment.MiddleCenter);
        }

        /// <summary>Текст ячейки с высотой и выравниванием.</summary>
        private static void SetText(Table table, int row, int column, string text, double height, CellAlignment alignment)
        {
            var cell = table.Cells[row, column];
            cell.TextString = text ?? string.Empty;
            cell.TextHeight = height;
            cell.Alignment = alignment;
        }

        /// <summary>Объединение ячеек.</summary>
        private static void Merge(Table table, int row1, int column1, int row2, int column2)
        {
            // Стиль таблиц может сам объединить первую строку (заголовок таблицы); повторное объединение вызывает ошибку.
            if (table.Cells[row1, column1].IsMerged == true) return;
            table.MergeCells(CellRange.Create(table, row1, column1, row2, column2));
        }

        /// <summary>
        /// Стиль строки из стиля таблиц чертежа (_TITLE, _HEADER, _DATA). Если такого стиля нет — оставляется как есть.
        /// </summary>
        private static void SetRowStyle(Table table, int row, string style)
        {
            try { table.Rows[row].Style = style; }
            catch (Autodesk.AutoCAD.Runtime.Exception) { /* стиль строки отсутствует в стиле таблиц чертежа */ }
        }
    }
}
