using System;
using Autodesk.AutoCAD.DatabaseServices;

namespace PanelKomplekt.Core
{
    /// <summary>
    /// Данные одной панели PK_Panel, прочитанные из чертежа.
    /// Используются спецификацией и служебными командами.
    /// </summary>
    public sealed class PanelData
    {
        /// <summary>ObjectId вставки блока в чертеже.</summary>
        public ObjectId Id { get; set; }

        /// <summary>Постоянный ID (дескриптор) вставки — для сообщений пользователю.</summary>
        public string Handle { get; set; }

        /// <summary>Длина панели, мм (динамический параметр Length).</summary>
        public double Length { get; set; }

        /// <summary>Ширина панели, мм (динамический параметр Width).</summary>
        public double Width { get; set; }

        /// <summary>Буква марки (атрибут PREFIX).</summary>
        public string Prefix { get; set; }

        /// <summary>Тип панели, например «ПСБ-120» (атрибут TYPE).</summary>
        public string Type { get; set; }

        /// <summary>RAL снаружи (атрибут RAL_OUT).</summary>
        public string RalOut { get; set; }

        /// <summary>Покрытие снаружи (атрибут SURFACE_OUT).</summary>
        public string SurfaceOut { get; set; }

        /// <summary>RAL внутри (атрибут RAL_IN).</summary>
        public string RalIn { get; set; }

        /// <summary>Покрытие внутри (атрибут SURFACE_IN).</summary>
        public string SurfaceIn { get; set; }

        /// <summary>Длина в сантиметрах, как в марке: 5980 мм → 598.</summary>
        public int LengthCm => (int)Math.Round(Length / 10.0, MidpointRounding.AwayFromZero);

        /// <summary>Марка панели: буква + длина в сантиметрах, например «П598».</summary>
        public string Mark => (Prefix ?? string.Empty) + LengthCm;

        /// <summary>Площадь одной панели, м².</summary>
        public double AreaM2 => Length / 1000.0 * (Width / 1000.0);
    }
}
