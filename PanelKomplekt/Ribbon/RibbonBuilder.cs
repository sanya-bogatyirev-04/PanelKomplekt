using Autodesk.Windows;
using PanelKomplekt.Core;
using System;
using System.Linq;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace PanelKomplekt.Ribbon
{
    /// <summary>
    /// Построение вкладки плагина на ленте AutoCAD.
    /// Кнопки создаются автоматически по <see cref="CommandCatalog"/> и группируются по панелям (CommandInfo.RibbonPanel).
    /// У каждой кнопки своя подсказка <see cref="RibbonToolTip"/> со справкой по F1:
    /// страницу команды открывает встроенная справка AutoCAD, отдельным окном поверх чертежа.
    /// </summary>
    public static class RibbonBuilder
    {
        /// <summary>Идентификатор вкладки: по нему проверяется, что вкладка уже создана.</summary>
        private const string TabId = "PANELKOMPLEKT_TAB";

        /// <summary>Текст в раскрывающейся части подсказки — подсказывает про справку по F1.</summary>
        private const string HelpHint = "Нажмите F1, чтобы открыть инструкцию по команде.";

        /// <summary>
        /// Создаёт вкладку, если лента доступна и вкладки ещё нет.
        /// </summary>
        public static void Create()
        {
            var ribbon = ComponentManager.Ribbon;
            if (ribbon == null || ribbon.FindTab(TabId) != null)
                return;

            var tab = new RibbonTab { Title = "PanelKomplekt", Id = TabId };
            ribbon.Tabs.Add(tab);

            var handler = new RibbonCommandHandler();

            // Порядок панелей и кнопок — как в каталоге команд.
            foreach (var group in CommandCatalog.All.GroupBy(c => c.RibbonPanel))
            {
                var source = new RibbonPanelSource { Title = group.Key };
                tab.Panels.Add(new RibbonPanel { Source = source });

                foreach (var command in group)
                {
                    source.Items.Add(new RibbonButton
                    {
                        Text = command.RibbonText,
                        ShowText = true,
                        // Иконки из папки команды; если их нет — кнопка остаётся текстовой.
                        Image = IconLoader.Load(command.Key, 16),
                        LargeImage = IconLoader.Load(command.Key, 32),
                        ShowImage = true,
                        Size = RibbonItemSize.Large,
                        Orientation = System.Windows.Controls.Orientation.Vertical,
                        // Подсказка — объект, а не строка: со строкой F1 не работает.
                        ToolTip = CreateToolTip(command),
                        CommandParameter = command.GlobalName,
                        CommandHandler = handler
                    });
                }
            }
        }

        /// <summary>
        /// Подсказка кнопки: название, описание, имя команды и справка по F1.
        /// Для каждой кнопки создаётся свой объект: один RibbonToolTip нельзя назначить двум элементам ленты.
        /// </summary>
        private static RibbonToolTip CreateToolTip(CommandInfo command)
        {
            var tip = new RibbonToolTip
            {
                Title = command.RibbonText,
                Content = command.Description,
                Command = command.GlobalName,
                IsHelpEnabled = false // включается только вместе с разобранным адресом справки
            };

            if (string.IsNullOrWhiteSpace(command.HelpUrl))
                return tip;

            // Без проверки адреса неверная строка выбросила бы исключение и оставила ленту без вкладки.
            if (Uri.TryCreate(command.HelpUrl, UriKind.Absolute, out var help))
            {
                tip.HelpSource = help;
                tip.IsHelpEnabled = true;
                tip.ExpandedContent = HelpHint;
            }
            else
            {
                Warn($"[{command.Number}] Неверный адрес справки: {command.HelpUrl}. Кнопка построена без справки по F1.");
            }

            return tip;
        }

        /// <summary>
        /// Предупреждение в командную строку. Документа может ещё не быть — тогда сообщение пропускается.
        /// </summary>
        private static void Warn(string message)
        {
            AcApp.DocumentManager?.MdiActiveDocument?.Editor.WriteMessage($"\n{message}\n");
        }
    }
}
