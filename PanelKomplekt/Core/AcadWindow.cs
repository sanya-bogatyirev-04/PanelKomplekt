using System;
using System.Windows.Forms;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace PanelKomplekt.Core
{
    /// <summary>
    /// Главное окно AutoCAD как владелец окон WinForms (MessageBox, формы).
    /// С владельцем окно плагина открывается поверх AutoCAD и блокирует его, как стандартные диалоги.
    /// </summary>
    public sealed class AcadWindow : IWin32Window
    {
        /// <summary>Дескриптор главного окна AutoCAD.</summary>
        public IntPtr Handle { get; }

        private AcadWindow(IntPtr handle)
        {
            Handle = handle;
        }

        /// <summary>
        /// Текущее главное окно AutoCAD.
        /// </summary>
        public static AcadWindow Main => new AcadWindow(AcApp.MainWindow.Handle);
    }
}
