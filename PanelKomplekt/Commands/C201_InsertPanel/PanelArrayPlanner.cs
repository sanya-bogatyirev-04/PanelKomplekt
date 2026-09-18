using System;
using System.Collections.Generic;
using PanelKomplekt.Core;

namespace PanelKomplekt.Commands
{
    /// <summary>
    /// Одна панель будущего массива: смещение вдоль линии раскладки и ширина.
    /// </summary>
    internal struct PanelArrayItem
    {
        /// <summary>Смещение начала панели от первой точки вдоль линии раскладки, мм.</summary>
        public double Offset { get; }

        /// <summary>Ширина панели, мм.</summary>
        public double Width { get; }

        /// <summary>true — панель укорочена по ширине, чтобы поместиться в остаток линии.</summary>
        public bool Truncated { get; }

        /// <summary>
        /// Создание описания панели массива.
        /// </summary>
        public PanelArrayItem(double offset, double width, bool truncated)
        {
            Offset = offset;
            Width = width;
            Truncated = truncated;
        }
    }

    /// <summary>
    /// Расчёт раскладки панелей вдоль линии: сколько панелей помещается, с какими смещениями,
    /// и нужна ли последняя укороченная панель. Расчёт не зависит от AutoCAD, поэтому его можно проверять отдельно.
    /// </summary>
    internal static class PanelArrayPlanner
    {
        /// <summary>
        /// Предохранитель от случайно огромной линии: больше этого числа панелей за один раз не создаётся.
        /// </summary>
        public const int MaxCount = 1000;

        /// <summary>
        /// Раскладка панелей по линии.
        /// </summary>
        /// <param name="totalLength">Длина линии раскладки, мм.</param>
        /// <param name="width">Ширина целой панели, мм.</param>
        /// <param name="gap">Зазор между соседними панелями, мм (0 — вплотную).</param>
        /// <returns>Панели в порядке от первой точки; последняя может быть укороченной.</returns>
        public static List<PanelArrayItem> Plan(double totalLength, double width, double gap)
        {
            var items = new List<PanelArrayItem>();
            var panelWidth = PanelBlock.NormalizeWidth(width);
            var step = PanelBlock.NormalizeGap(gap);
            var pitch = panelWidth + step;
            var offset = 0.0;

            while (items.Count < MaxCount)
            {
                // Округление остатка до шага блока гасит погрешность указания точек мышью:
                // остаток 1189,9997 мм считается целой панелью 1190 мм, а не обрезком 1180 мм.
                var available = totalLength - offset;
                var snapped = Math.Round(available / PanelBlock.SizeStep, MidpointRounding.AwayFromZero) * PanelBlock.SizeStep;

                if (snapped >= panelWidth)
                {
                    items.Add(new PanelArrayItem(offset, panelWidth, false));
                    offset += pitch;
                    continue;
                }

                // Остаток меньше целой панели: ставим укороченную, если она не тоньше минимума блока.
                if (snapped >= PanelBlock.MinWidth) items.Add(new PanelArrayItem(offset, snapped, true));
                break;
            }

            return items;
        }
    }
}
