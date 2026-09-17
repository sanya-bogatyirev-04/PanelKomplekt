using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using PanelKomplekt.Core;

namespace PanelKomplekt.Commands
{
    /// <summary>
    /// Интерактивный предпросмотр панели для команды C201: от начальной точки к курсору рисуется
    /// прямоугольник с маркой. Рисуется лёгкая временная графика (без вставки блока в чертёж),
    /// поэтому предпросмотр не нагружает AutoCAD.
    /// </summary>
    internal sealed class PanelJig : DrawJig
    {
        /// <summary>Высота текста марки в предпросмотре, мм (как у атрибутов блока).</summary>
        private const double MarkHeight = 300;

        /// <summary>Начальная точка, МСК.</summary>
        private readonly Point3d _start;

        /// <summary>Начальная точка, ПСК — базовая точка «резиновой нити».</summary>
        private readonly Point3d _startUcs;

        /// <summary>Нормаль плоскости панели (ось Z ПСК).</summary>
        private readonly Vector3d _normal;

        /// <summary>Направление по умолчанию (ось X ПСК), пока курсор совпадает с начальной точкой.</summary>
        private readonly Vector3d _defaultDirection;

        /// <summary>Настройки панели (ширина, буква).</summary>
        private readonly PanelSettings _settings;

        /// <summary>Последняя точка курсора — чтобы не перерисовывать без движения.</summary>
        private Point3d _lastPoint;

        /// <summary>Длина панели, мм (кратно 10, в пределах блока).</summary>
        public double Length { get; private set; } = PanelBlock.MinLength;

        /// <summary>Направление длины панели, МСК (единичный вектор в плоскости ПСК).</summary>
        public Vector3d Direction { get; private set; }

        /// <summary>
        /// Создание предпросмотра.
        /// </summary>
        /// <param name="start">Начальная точка, МСК.</param>
        /// <param name="startUcs">Начальная точка, ПСК.</param>
        /// <param name="normal">Ось Z ПСК.</param>
        /// <param name="defaultDirection">Ось X ПСК.</param>
        /// <param name="settings">Настройки панели.</param>
        public PanelJig(Point3d start, Point3d startUcs, Vector3d normal, Vector3d defaultDirection, PanelSettings settings)
        {
            _start = start;
            _startUcs = startUcs;
            _normal = normal;
            _defaultDirection = defaultDirection;
            _settings = settings;
            _lastPoint = start;
            Direction = defaultDirection;
        }

        /// <summary>
        /// Опрос курсора: вторая точка панели. Работают привязки, ОРТО и ввод длины с клавиатуры.
        /// </summary>
        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            var options = new JigPromptPointOptions("\nКонечная точка панели (или длина): ")
            {
                BasePoint = _startUcs,
                UseBasePoint = true,
                UserInputControls = UserInputControls.Accept3dCoordinates
                                    | UserInputControls.GovernedByOrthoMode
                                    | UserInputControls.NoZeroResponseAccepted
                                    | UserInputControls.NoNegativeResponseAccepted
            };

            var result = prompts.AcquirePoint(options);
            if (result.Status != PromptStatus.OK) return SamplerStatus.Cancel;
            if (result.Value.IsEqualTo(_lastPoint)) return SamplerStatus.NoChange;

            _lastPoint = result.Value;
            UpdateGeometry(result.Value);
            return SamplerStatus.OK;
        }

        /// <summary>
        /// Длина и направление по точке курсора: проекция на плоскость ПСК, округление до 10 мм.
        /// </summary>
        private void UpdateGeometry(Point3d point)
        {
            var vector = point - _start;
            vector -= _normal * vector.DotProduct(_normal);
            if (vector.Length < Tolerance.Global.EqualPoint)
            {
                Direction = _defaultDirection;
                Length = PanelBlock.MinLength;
                return;
            }
            Direction = vector.GetNormal();
            Length = PanelBlock.NormalizeLength(vector.Length);
        }

        /// <summary>
        /// Отрисовка предпросмотра: контур панели и марка по центру.
        /// </summary>
        protected override bool WorldDraw(WorldDraw draw)
        {
            var across = _normal.CrossProduct(Direction).GetNormal();
            var width = PanelBlock.NormalizeWidth(_settings.Width);
            var alongLength = Direction * Length;
            var alongWidth = across * width;

            var outline = new Point3dCollection
            {
                _start,
                _start + alongLength,
                _start + alongLength + alongWidth,
                _start + alongWidth,
                _start
            };
            draw.Geometry.Polyline(outline, _normal, System.IntPtr.Zero);

            var mark = (_settings.Prefix ?? string.Empty) + (int)System.Math.Round(Length / 10.0);
            var center = _start + alongLength * 0.5 + alongWidth * 0.5;
            // Приблизительное центрирование: ширина символа ≈ 0,6 высоты.
            var textPosition = center - Direction * (mark.Length * MarkHeight * 0.3) - across * (MarkHeight * 0.5);
            draw.Geometry.Text(textPosition, _normal, Direction, MarkHeight, 1.0, 0.0, mark);
            return true;
        }
    }
}
