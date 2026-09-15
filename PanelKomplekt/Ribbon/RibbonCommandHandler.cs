using System;
using System.Windows.Input;
using Autodesk.Windows;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace PanelKomplekt.Ribbon
{
    /// <summary>
    /// Обработчик кнопок ленты: запускает команду AutoCAD, имя которой лежит в CommandParameter кнопки.
    /// </summary>
    public class RibbonCommandHandler : ICommand
    {
        public event EventHandler CanExecuteChanged { add { } remove { } }

        public bool CanExecute(object parameter) => true;

        public void Execute(object parameter)
        {
            if (parameter is RibbonButton button && button.CommandParameter is string cmd)
            {
                // Пробел в конце = Enter; "_." — независимость от языка и переопределений команд.
                AcApp.DocumentManager.MdiActiveDocument?
                    .SendStringToExecute("_." + cmd + " ", true, false, true);
            }
        }
    }
}
