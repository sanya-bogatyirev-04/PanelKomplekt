using System;
using System.Drawing;
using System.Windows.Forms;

namespace PanelKomplekt.Commands
{
    /// <summary>Режим работы команды C201.</summary>
    internal enum PanelCreationMode
    {
        /// <summary>Панели по одной: начальная и конечная точка для каждой.</summary>
        Single,

        /// <summary>Массив панелей вдоль линии по заданным параметрам.</summary>
        Array
    }

    /// <summary>
    /// Окно выбора режима работы команды «Создание»: массив или по одной панели.
    /// </summary>
    internal sealed class PanelModeForm : Form
    {
        /// <summary>Выбранный режим (действителен при DialogResult.OK).</summary>
        public PanelCreationMode Mode { get; private set; } = PanelCreationMode.Array;

        /// <summary>
        /// Создание окна.
        /// </summary>
        public PanelModeForm()
        {
            Text = "Создание панелей";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            Font = SystemFonts.MessageBoxFont;
            AutoScaleMode = AutoScaleMode.Font;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;

            var layout = new TableLayoutPanel
            {
                ColumnCount = 1,
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(16)
            };

            layout.Controls.Add(new Label
            {
                Text = "Выберите режим работы:",
                AutoSize = true,
                Margin = new Padding(3, 3, 3, 10)
            });

            layout.Controls.Add(CreateModeButton("Создание массива",
                "Панели укладываются вдоль указанной линии по заданным параметрам", PanelCreationMode.Array));
            layout.Controls.Add(CreateModeButton("Создание по одному",
                "Каждая панель указывается двумя точками, как раньше", PanelCreationMode.Single));

            var cancel = new Button
            {
                Text = "Отмена",
                DialogResult = DialogResult.Cancel,
                AutoSize = true,
                Anchor = AnchorStyles.Right,
                Margin = new Padding(3, 10, 3, 3)
            };
            layout.Controls.Add(cancel);
            CancelButton = cancel;

            Controls.Add(layout);
        }

        /// <summary>
        /// Крупная кнопка режима: подпись, пояснение и закрытие окна с результатом OK.
        /// </summary>
        private Button CreateModeButton(string text, string hint, PanelCreationMode mode)
        {
            var button = new Button
            {
                Text = text,
                AutoSize = false,
                Width = 320,
                Height = 44,
                Margin = new Padding(3, 3, 3, 6),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };
            var tip = new ToolTip();
            tip.SetToolTip(button, hint);
            button.Click += (sender, args) =>
            {
                Mode = mode;
                DialogResult = DialogResult.OK;
                Close();
            };
            return button;
        }

        /// <summary>
        /// Показывает окно и возвращает выбранный режим; null — пользователь отказался.
        /// </summary>
        public static PanelCreationMode? Ask(IWin32Window owner)
        {
            using (var form = new PanelModeForm())
            {
                return form.ShowDialog(owner) == DialogResult.OK ? form.Mode : (PanelCreationMode?)null;
            }
        }
    }
}
