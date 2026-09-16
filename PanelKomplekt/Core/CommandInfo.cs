namespace PanelKomplekt.Core
{
    /// <summary>
    /// Описание команды плагина: номер, имя в AutoCAD и данные для кнопки на ленте.
    /// Каждая команда объявляет свой экземпляр в статическом поле Info,
    /// а все экземпляры собираются в <see cref="CommandCatalog"/>.
    /// </summary>
    public sealed class CommandInfo
    {
        /// <summary>
        /// Ключ команды — имя её папки и файлов, например "C101_ShowElementId".
        /// По нему ищутся иконки кнопки: &lt;Key&gt;_16.png и &lt;Key&gt;_32.png.
        /// </summary>
        public string Key { get; set; }

        /// <summary>Номер команды, например "C101".</summary>
        public string Number { get; set; }

        /// <summary>Имя команды в AutoCAD (то, что вводится в командной строке).</summary>
        public string GlobalName { get; set; }

        /// <summary>Подпись кнопки на ленте.</summary>
        public string RibbonText { get; set; }

        /// <summary>Название панели на вкладке ленты, куда попадёт кнопка.</summary>
        public string RibbonPanel { get; set; }

        /// <summary>Краткое описание (всплывающая подсказка кнопки).</summary>
        public string Description { get; set; }
    }
}
