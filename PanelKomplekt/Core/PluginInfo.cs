namespace PanelKomplekt.Core
{
    /// <summary>
    /// Сведения о плагине: название, версия, заказчик, разработчик и технологии.
    /// Единое место для этих данных — их используют окно «О плагине» и сообщение при загрузке.
    /// </summary>
    public static class PluginInfo
    {
        /// <summary>Название плагина.</summary>
        public const string Name = "PanelKomplekt";

        /// <summary>Краткое назначение плагина.</summary>
        public const string Purpose = "Плагин создан для оптимизации и упрощения работы сотрудников ООО «Панелькомплект»";

        /// <summary>Для кого создан плагин.</summary>
        public const string Customer = "ООО «Панелькомплект»";

        /// <summary>Кем создан плагин.</summary>
        public const string Author = "Богатырев Александр Дмитриевич";

        /// <summary>
        /// Для каких версий AutoCAD собрана загруженная копия плагина. Из одного кода собираются две сборки:
        /// net48 — для AutoCAD 2021–2024, net47 — для AutoCAD 2020 (символы NET48 и NET47 задаёт .NET SDK).
        /// Показывается в окне «О плагине»: при обращении заказчика сразу видно, какая сборка у него работает.
        /// </summary>
#if NET48
        public const string Platform = "AutoCAD 2021–2024";
#else
        public const string Platform = "AutoCAD 2020";
#endif

        /// <summary>Применяемые технологии (по одной на строку).</summary>
        public static readonly string[] Technologies =
        {
            "C# и .NET Framework 4.7 / 4.8",
            "AutoCAD .NET API (AutoCAD 2020, 2021, 2022)",
            "WPF — вкладка и кнопки на ленте",
            "Visual Studio 2022, Git, GitHub Actions"
        };

        /// <summary>
        /// Версия плагина в формате A.B.C (из &lt;Version&gt; в PanelKomplekt.csproj).
        /// </summary>
        public static string Version => typeof(PluginInfo).Assembly.GetName().Version.ToString(3);
    }
}
