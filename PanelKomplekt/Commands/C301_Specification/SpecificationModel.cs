using System.Collections.Generic;

namespace PanelKomplekt.Commands
{
    /// <summary>
    /// Спецификация панелей: группы по типу панели и общий итог.
    /// Модель без зависимостей от AutoCAD — её заполняет <see cref="SpecificationBuilder"/>,
    /// а выводят <see cref="SpecificationExcelWriter"/> и <see cref="SpecificationTableWriter"/>.
    /// </summary>
    public sealed class Specification
    {
        /// <summary>Группы (типы панелей) в порядке вывода.</summary>
        public List<SpecificationGroup> Groups { get; } = new List<SpecificationGroup>();

        /// <summary>Общее количество панелей, шт.</summary>
        public int TotalCount { get; set; }

        /// <summary>Общая площадь, м² (сумма площадей строк).</summary>
        public double TotalArea { get; set; }

        /// <summary>Предупреждения о возможных ошибках в чертеже.</summary>
        public List<string> Warnings { get; } = new List<string>();
    }

    /// <summary>
    /// Группа спецификации: панели одного типа, ширины и цветов.
    /// </summary>
    public sealed class SpecificationGroup
    {
        /// <summary>Порядковый номер группы (1, 2, …).</summary>
        public int Number { get; set; }

        /// <summary>Тип панели, например «ПСБ-120».</summary>
        public string Type { get; set; }

        /// <summary>Ширина панели, мм.</summary>
        public double Width { get; set; }

        /// <summary>RAL снаружи.</summary>
        public string RalOut { get; set; }

        /// <summary>Покрытие снаружи.</summary>
        public string SurfaceOut { get; set; }

        /// <summary>RAL внутри.</summary>
        public string RalIn { get; set; }

        /// <summary>Покрытие внутри.</summary>
        public string SurfaceIn { get; set; }

        /// <summary>Строки группы: одна строка на марку и длину.</summary>
        public List<SpecificationRow> Rows { get; } = new List<SpecificationRow>();

        /// <summary>Количество панелей в группе, шт.</summary>
        public int TotalCount { get; set; }

        /// <summary>Площадь панелей группы, м².</summary>
        public double TotalArea { get; set; }

        /// <summary>Заголовок группы, как в примере заказчика: «1. Панели ПСБ-120 ширина 1190 мм.»</summary>
        public string Title => $"{Number}. Панели {Type} ширина {Width:0} мм.";
    }

    /// <summary>
    /// Строка спецификации: панели одной марки и длины.
    /// </summary>
    public sealed class SpecificationRow
    {
        /// <summary>Марка, например «П598».</summary>
        public string Mark { get; set; }

        /// <summary>Длина, мм.</summary>
        public double Length { get; set; }

        /// <summary>Количество, шт.</summary>
        public int Count { get; set; }

        /// <summary>Площадь всех панелей строки, м² (округлена до 0,01).</summary>
        public double Area { get; set; }
    }
}
