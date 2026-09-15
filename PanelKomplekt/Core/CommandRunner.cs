using System;
using Autodesk.AutoCAD.ApplicationServices;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace PanelKomplekt.Core
{
    /// <summary>
    /// Общая обёртка для тела команд: проверяет наличие активного документа
    /// и перехватывает исключения, чтобы ошибка в команде не приводила к аварийному окну AutoCAD.
    /// </summary>
    public static class CommandRunner
    {
        /// <summary>
        /// Выполняет тело команды для активного документа.
        /// </summary>
        /// <param name="info">Описание команды (используется в тексте ошибки).</param>
        /// <param name="body">Тело команды; получает активный документ.</param>
        public static void Run(CommandInfo info, Action<Document> body)
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return; // нет открытого чертежа — выполнять нечего

            try
            {
                body(doc);
            }
            catch (Exception ex)
            {
                doc.Editor.WriteMessage($"\n[{info.Number}] Ошибка: {ex.Message}\n{ex}\n");
                AcApp.ShowAlertDialog($"Команда {info.Number} завершилась с ошибкой:\n{ex.Message}");
            }
        }
    }
}
