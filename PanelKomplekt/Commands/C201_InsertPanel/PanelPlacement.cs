using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using PanelKomplekt.Core;

namespace PanelKomplekt.Commands
{
    /// <summary>
    /// Общие вычисления и действия обоих режимов команды C201:
    /// поворот вставки, проекция на плоскость раскладки и сохранение настроек в чертёж.
    /// </summary>
    internal static class PanelPlacement
    {
        /// <summary>
        /// Угол поворота вставки: от оси X системы координат объекта (по нормали) до направления длины.
        /// </summary>
        /// <param name="normal">Нормаль плоскости панели (ось Z ПСК).</param>
        /// <param name="direction">Направление длины панели, МСК.</param>
        public static double GetRotation(Vector3d normal, Vector3d direction)
        {
            var objectXAxis = Vector3d.XAxis.TransformBy(Matrix3d.PlaneToWorld(normal));
            return objectXAxis.GetAngleTo(direction, normal);
        }

        /// <summary>
        /// Проекция вектора на плоскость раскладки: убирает составляющую вдоль нормали.
        /// </summary>
        public static Vector3d Project(Vector3d vector, Vector3d normal) => vector - normal * vector.DotProduct(normal);

        /// <summary>
        /// Сохраняет настройки панелей в чертёж отдельной транзакцией.
        /// </summary>
        public static void SaveSettings(Database db, PanelSettings settings)
        {
            using (var tr = db.TransactionManager.StartTransaction())
            {
                settings.Save(db, tr);
                tr.Commit();
            }
        }
    }
}
