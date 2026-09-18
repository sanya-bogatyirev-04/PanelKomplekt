using System;
using System.Drawing;
using System.Windows.Forms;
using PanelKomplekt.Core;

namespace PanelKomplekt.Commands
{
    /// <summary>
    /// Окно параметров массива панелей: буква марки, размеры, зазор, тип и цвета.
    /// Значения берутся из настроек чертежа и по нажатию «Создать» возвращаются в них же.
    /// </summary>
    internal sealed class PanelArrayForm : Form
    {
        /// <summary>Настройки, которые заполняет форма.</summary>
        private readonly PanelSettings _settings;

        /// <summary>Буква марки (атрибут PREFIX).</summary>
        private readonly TextBox _prefix = new TextBox();

        /// <summary>Длина панели, мм.</summary>
        private readonly NumericUpDown _length = new NumericUpDown();

        /// <summary>Ширина панели, мм.</summary>
        private readonly NumericUpDown _width = new NumericUpDown();

        /// <summary>Зазор между соседними панелями, мм.</summary>
        private readonly NumericUpDown _gap = new NumericUpDown();

        /// <summary>Тип панели (атрибут TYPE).</summary>
        private readonly TextBox _type = new TextBox();

        /// <summary>RAL снаружи.</summary>
        private readonly TextBox _ralOut = new TextBox();

        /// <summary>Покрытие снаружи.</summary>
        private readonly TextBox _surfaceOut = new TextBox();

        /// <summary>RAL внутри.</summary>
        private readonly TextBox _ralIn = new TextBox();

        /// <summary>Покрытие внутри.</summary>
        private readonly TextBox _surfaceIn = new TextBox();

        /// <summary>Таблица с полями формы.</summary>
        private readonly TableLayoutPanel _layout = new TableLayoutPanel();

        /// <summary>
        /// Создание окна с текущими настройками панелей.
        /// </summary>
        public PanelArrayForm(PanelSettings settings)
        {
            _settings = settings;
            Build();
            Fill();
        }

        /// <summary>
        /// Показывает окно параметров. При нажатии «Создать» настройки обновляются и возвращается true.
        /// </summary>
        public static bool Ask(IWin32Window owner, PanelSettings settings)
        {
            using (var form = new PanelArrayForm(settings))
            {
                if (form.ShowDialog(owner) != DialogResult.OK) return false;
                form.Apply();
                return true;
            }
        }

        /// <summary>
        /// Построение окна: поля, подсказка и кнопки.
        /// </summary>
        private void Build()
        {
            Text = "Создание массива панелей";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            Font = SystemFonts.MessageBoxFont;
            AutoScaleMode = AutoScaleMode.Font;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;

            _layout.ColumnCount = 2;
            _layout.Dock = DockStyle.Fill;
            _layout.AutoSize = true;
            _layout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _layout.Padding = new Padding(16);
            _layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));

            SetupNumber(_length, PanelBlock.MinLength, PanelBlock.MaxLength);
            SetupNumber(_width, PanelBlock.MinWidth, PanelBlock.MaxWidth);
            SetupNumber(_gap, 0, PanelBlock.MaxGap);

            AddRow("Буква марки:", _prefix);
            AddRow("Длина панели, мм:", _length);
            AddRow("Ширина панели, мм:", _width);
            AddRow("Зазор между панелями, мм:", _gap);
            AddRow("Тип панели:", _type);
            AddRow("RAL снаружи:", _ralOut);
            AddRow("Покрытие снаружи:", _surfaceOut);
            AddRow("RAL внутри:", _ralIn);
            AddRow("Покрытие внутри:", _surfaceIn);

            var hint = new Label
            {
                Text = "После нажатия «Создать» укажите начальную и конечную точки линии," + Environment.NewLine +
                       "а затем сторону, в которую уходит длина панелей.",
                AutoSize = true,
                Margin = new Padding(3, 12, 3, 6)
            };
            _layout.Controls.Add(hint);
            _layout.SetColumnSpan(hint, 2);

            var buttons = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(3, 6, 3, 3)
            };
            var cancel = new Button { Text = "Отмена", DialogResult = DialogResult.Cancel, AutoSize = true };
            var create = new Button { Text = "Создать", DialogResult = DialogResult.OK, AutoSize = true };
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(create);
            _layout.Controls.Add(buttons);
            _layout.SetColumnSpan(buttons, 2);

            AcceptButton = create;
            CancelButton = cancel;
            FormClosing += OnFormClosing;

            Controls.Add(_layout);
        }

        /// <summary>
        /// Настройка числового поля: целые миллиметры, шаг блока, допустимый диапазон.
        /// </summary>
        private static void SetupNumber(NumericUpDown number, double minimum, double maximum)
        {
            number.DecimalPlaces = 0;
            number.Increment = (decimal)PanelBlock.SizeStep;
            number.Minimum = (decimal)minimum;
            number.Maximum = (decimal)maximum;
            number.ThousandsSeparator = true;
            number.TextAlign = HorizontalAlignment.Right;
        }

        /// <summary>
        /// Строка формы: подпись слева, поле справа.
        /// </summary>
        private void AddRow(string caption, Control editor)
        {
            var label = new Label
            {
                Text = caption,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(3, 6, 12, 3)
            };
            editor.Dock = DockStyle.Fill;
            _layout.Controls.Add(label);
            _layout.Controls.Add(editor);
        }

        /// <summary>
        /// Заполнение полей текущими настройками чертежа.
        /// </summary>
        private void Fill()
        {
            _prefix.Text = _settings.Prefix;
            _length.Value = (decimal)PanelBlock.NormalizeLength(_settings.ArrayLength);
            _width.Value = (decimal)PanelBlock.NormalizeWidth(_settings.Width);
            _gap.Value = (decimal)PanelBlock.NormalizeGap(_settings.ArrayGap);
            _type.Text = _settings.Type;
            _ralOut.Text = _settings.RalOut;
            _surfaceOut.Text = _settings.SurfaceOut;
            _ralIn.Text = _settings.RalIn;
            _surfaceIn.Text = _settings.SurfaceIn;
        }

        /// <summary>
        /// Перенос значений формы в настройки. Размеры приводятся к правилам блока (шаг 10 мм).
        /// </summary>
        private void Apply()
        {
            _settings.Prefix = _prefix.Text.Trim();
            _settings.ArrayLength = PanelBlock.NormalizeLength((double)_length.Value);
            _settings.Width = PanelBlock.NormalizeWidth((double)_width.Value);
            _settings.ArrayGap = PanelBlock.NormalizeGap((double)_gap.Value);
            _settings.Type = _type.Text.Trim();
            _settings.RalOut = _ralOut.Text.Trim();
            _settings.SurfaceOut = _surfaceOut.Text.Trim();
            _settings.RalIn = _ralIn.Text.Trim();
            _settings.SurfaceIn = _surfaceIn.Text.Trim();
        }

        /// <summary>
        /// Проверка перед закрытием по «Создать»: буква марки обязательна и не содержит пробелов.
        /// </summary>
        private void OnFormClosing(object sender, FormClosingEventArgs args)
        {
            if (DialogResult != DialogResult.OK) return;

            var prefix = _prefix.Text.Trim();
            if (prefix.Length == 0 || prefix.IndexOf(' ') >= 0)
            {
                MessageBox.Show(this,
                    "Буква марки обязательна и не должна содержать пробелов.",
                    "Создание массива панелей",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                args.Cancel = true;
                _prefix.Focus();
            }
        }
    }
}
