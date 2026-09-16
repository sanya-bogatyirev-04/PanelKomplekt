using System;
using System.Windows.Media.Imaging;

namespace PanelKomplekt.Core
{
    /// <summary>
    /// Загрузка иконок кнопок ленты из ресурсов сборки.
    /// Иконки лежат в папках команд (<c>C101_ShowElementId_16.png</c>, <c>C101_ShowElementId_32.png</c>)
    /// и встраиваются в DLL под именами <c>Icons/&lt;Key&gt;_&lt;размер&gt;.png</c> (см. PanelKomplekt.csproj).
    /// </summary>
    public static class IconLoader
    {
        /// <summary>
        /// Возвращает иконку команды нужного размера или null, если иконки нет.
        /// Отсутствие иконки не считается ошибкой: кнопка просто показывается без картинки.
        /// </summary>
        /// <param name="key">Ключ команды, например "C101_ShowElementId".</param>
        /// <param name="size">Размер в пикселях: 16 или 32.</param>
        public static BitmapImage Load(string key, int size)
        {
            // Встроенный ресурс надёжнее pack://-ссылок: внутри AutoCAD они не всегда разрешаются.
            var stream = typeof(IconLoader).Assembly.GetManifestResourceStream($"Icons/{key}_{size}.png");
            if (stream == null) return null;

            try
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.StreamSource = stream;
                image.CacheOption = BitmapCacheOption.OnLoad; // картинка читается сразу, поток можно закрыть
                image.EndInit();
                image.Freeze();
                return image;
            }
            catch (Exception)
            {
                return null; // повреждённая картинка не должна мешать построению ленты
            }
            finally
            {
                stream.Dispose();
            }
        }
    }
}
