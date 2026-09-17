using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PanelKomplekt.Core;

namespace PanelKomplekt.Commands
{
    /// <summary>
    /// Расчёт спецификации по данным панелей (без обращения к AutoCAD).
    /// Группа — тип + ширина + цвета и покрытия; строка — марка + длина; площадь = кол-во × длина × ширина.
    /// </summary>
    public static class SpecificationBuilder
    {
        /// <summary>Округление площади, знаков после запятой.</summary>
        private const int AreaDecimals = 2;

        /// <summary>
        /// Строит спецификацию.
        /// </summary>
        /// <param name="panels">Данные панелей (см. <see cref="PanelReader"/>).</param>
        public static Specification Build(IReadOnlyCollection<PanelData> panels)
        {
            var specification = new Specification();
            AddWarnings(panels, specification);

            var groups = panels
                .GroupBy(p => new GroupKey(p))
                .OrderBy(g => g.Key.Type, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(g => g.Key.Width)
                .ThenBy(g => g.Key.RalOut, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(g => g.Key.RalIn, StringComparer.CurrentCultureIgnoreCase);

            var number = 0;
            foreach (var groupPanels in groups)
            {
                var key = groupPanels.Key;
                var group = new SpecificationGroup
                {
                    Number = ++number,
                    Type = key.Type,
                    Width = key.Width,
                    RalOut = key.RalOut,
                    SurfaceOut = key.SurfaceOut,
                    RalIn = key.RalIn,
                    SurfaceIn = key.SurfaceIn
                };

                // Строки: марка + длина (до 1 мм); сначала длинные панели.
                var rows = groupPanels
                    .GroupBy(p => new { p.Mark, Length = Math.Round(p.Length) })
                    .OrderByDescending(r => r.Key.Length)
                    .ThenBy(r => r.Key.Mark, StringComparer.CurrentCultureIgnoreCase);

                foreach (var rowPanels in rows)
                {
                    var count = rowPanels.Count();
                    var row = new SpecificationRow
                    {
                        Mark = rowPanels.Key.Mark,
                        Length = rowPanels.Key.Length,
                        Count = count,
                        Area = Math.Round(count * rowPanels.Key.Length / 1000.0 * key.Width / 1000.0, AreaDecimals, MidpointRounding.AwayFromZero)
                    };
                    group.Rows.Add(row);
                    group.TotalCount += row.Count;
                    group.TotalArea += row.Area;
                }

                group.TotalArea = Math.Round(group.TotalArea, AreaDecimals, MidpointRounding.AwayFromZero);
                specification.Groups.Add(group);
                specification.TotalCount += group.TotalCount;
                specification.TotalArea += group.TotalArea;
            }

            specification.TotalArea = Math.Round(specification.TotalArea, AreaDecimals, MidpointRounding.AwayFromZero);
            return specification;
        }

        /// <summary>
        /// Предупреждения: слишком длинные панели, пустые буква и тип, одна марка при разной длине.
        /// </summary>
        private static void AddWarnings(IReadOnlyCollection<PanelData> panels, Specification specification)
        {
            var culture = CultureInfo.InvariantCulture;

            var tooLong = panels.Where(p => p.Length > PanelBlock.MaxLength).ToList();
            if (tooLong.Count > 0)
                specification.Warnings.Add($"Панели длиннее {PanelBlock.MaxLength.ToString("0", culture)} мм ({tooLong.Count} шт.), ID: {JoinHandles(tooLong)}");

            var noPrefix = panels.Where(p => string.IsNullOrWhiteSpace(p.Prefix)).ToList();
            if (noPrefix.Count > 0)
                specification.Warnings.Add($"Панели без буквы марки ({noPrefix.Count} шт.), ID: {JoinHandles(noPrefix)}");

            var noType = panels.Where(p => string.IsNullOrWhiteSpace(p.Type)).ToList();
            if (noType.Count > 0)
                specification.Warnings.Add($"Панели без типа ({noType.Count} шт.), ID: {JoinHandles(noType)}");

            // Одна марка при разной длине в мм (например, 5980 и 5984 → обе «П598»): в спецификации будут разные строки.
            foreach (var sameMark in panels.GroupBy(p => p.Mark))
            {
                var lengths = sameMark.Select(p => Math.Round(p.Length)).Distinct().OrderBy(l => l).ToList();
                if (lengths.Count > 1)
                    specification.Warnings.Add($"Марка {sameMark.Key} у панелей разной длины: {string.Join(", ", lengths.Select(l => l.ToString("0", culture)))} мм");
            }
        }

        /// <summary>ID панелей через запятую (не больше 20, чтобы не засорять командную строку).</summary>
        private static string JoinHandles(IReadOnlyCollection<PanelData> panels)
        {
            const int limit = 20;
            var text = string.Join(", ", panels.Take(limit).Select(p => p.Handle));
            return panels.Count > limit ? text + $" … (+{panels.Count - limit})" : text;
        }

        /// <summary>
        /// Ключ группы: тип, ширина, цвета и покрытия (без учёта регистра и лишних пробелов).
        /// </summary>
        private sealed class GroupKey : IEquatable<GroupKey>
        {
            public GroupKey(PanelData panel)
            {
                Type = Clean(panel.Type);
                Width = Math.Round(panel.Width);
                RalOut = Clean(panel.RalOut);
                SurfaceOut = Clean(panel.SurfaceOut);
                RalIn = Clean(panel.RalIn);
                SurfaceIn = Clean(panel.SurfaceIn);
            }

            public string Type { get; }
            public double Width { get; }
            public string RalOut { get; }
            public string SurfaceOut { get; }
            public string RalIn { get; }
            public string SurfaceIn { get; }

            /// <summary>Сравнение ключей без учёта регистра.</summary>
            public bool Equals(GroupKey other) =>
                other != null
                && Width.Equals(other.Width)
                && Same(Type, other.Type) && Same(RalOut, other.RalOut) && Same(SurfaceOut, other.SurfaceOut)
                && Same(RalIn, other.RalIn) && Same(SurfaceIn, other.SurfaceIn);

            public override bool Equals(object obj) => Equals(obj as GroupKey);

            public override int GetHashCode()
            {
                unchecked
                {
                    var comparer = StringComparer.CurrentCultureIgnoreCase;
                    var hash = Width.GetHashCode();
                    hash = hash * 31 + comparer.GetHashCode(Type);
                    hash = hash * 31 + comparer.GetHashCode(RalOut);
                    hash = hash * 31 + comparer.GetHashCode(SurfaceOut);
                    hash = hash * 31 + comparer.GetHashCode(RalIn);
                    hash = hash * 31 + comparer.GetHashCode(SurfaceIn);
                    return hash;
                }
            }

            private static string Clean(string value) => (value ?? string.Empty).Trim();

            private static bool Same(string a, string b) => string.Equals(a, b, StringComparison.CurrentCultureIgnoreCase);
        }
    }
}
