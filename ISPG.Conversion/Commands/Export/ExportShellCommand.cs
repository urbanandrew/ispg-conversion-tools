using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ISPG.Conversion.Core;

namespace ISPG.Conversion.Commands.Export
{
    [Transaction(TransactionMode.ReadOnly)]
    public class ExportShellCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                var uiDoc = commandData.Application.ActiveUIDocument;
                var doc = uiDoc.Document;

                // Find all shell family instances
                var shellInstances = FindShell(doc);

                if (!shellInstances.Any())
                {
                    TaskDialog.Show("Export Shell", "No shell elements found in the current model.");
                    return Result.Cancelled;
                }

                // Generate output file path
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string outputPath = Path.Combine(@"C:\Temp", $"umx_shell_extract_{timestamp}.json");

                // Ensure C:\Temp exists
                Directory.CreateDirectory(@"C:\Temp");

                // Export
                var exporter = new JsonExporter(doc, "shell");
                exporter.Export(shellInstances, outputPath);

                // Show success message
                TaskDialog.Show("Export Shell",
                    $"Successfully exported {shellInstances.Count()} shell elements to:\n\n{outputPath}");

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.ToString();
                TaskDialog.Show("Error", $"Export failed:\n\n{ex.Message}");
                return Result.Failed;
            }
        }

        /// <summary>
        /// Find all shell family instances in the document
        /// Matches families/types containing "Shell"
        /// </summary>
        private IEnumerable<FamilyInstance> FindShell(Document doc)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(FamilyInstance))
                .Cast<FamilyInstance>()
                .Where(fi => IsShell(fi));
        }

        private bool IsShell(FamilyInstance instance)
        {
            if (instance == null || instance.Symbol == null) return false;

            string familyName = instance.Symbol.FamilyName ?? "";

            // Match ONLY UX5_Shell family
            return familyName.Equals("UX5_Shell", StringComparison.OrdinalIgnoreCase);
        }

        private bool ContainsAny(string text, string[] patterns)
        {
            if (string.IsNullOrEmpty(text)) return false;
            return patterns.Any(p => text.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0);
        }
    }
}
