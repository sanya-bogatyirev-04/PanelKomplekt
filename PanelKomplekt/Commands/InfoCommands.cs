using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;

namespace PanelKomplekt.Commands
{
    public class InfoCommands
    {
        [CommandMethod("PK_INFO")]
        public void Info()
        {
            var ed = Application.DocumentManager.MdiActiveDocument.Editor;
            var version = typeof(InfoCommands).Assembly.GetName().Version;
            ed.WriteMessage($"\nPanelKomplekt {version} — раскладка сэндвич-панелей и спецификация.\n");
        }
    }
}
