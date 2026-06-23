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
        /// Matches legacy families (UX3/UX4 Shell, ISPG UX Shell) and current UX5_Shell
        /// </summary>
        private IEnumerable<FamilyInstance> FindShell(Document doc)
        {
            // Shell family name patterns (case-insensitive contains)
            string[] patterns = new[] { "UX3 Shell", "UX4 Shell", "ISPG UX Shell", "UX5_Shell" };

            return new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_GenericModel)
                .WhereElementIsNotElementType()
                .Cast<FamilyInstance>()
                .Where(fi => IsShell(fi, patterns));
        }

        private bool IsShell(FamilyInstance instance, string[] patterns)
        {
            if (instance == null || instance.Symbol == null) return false;

            string familyName = instance.Symbol.FamilyName ?? "";
            string typeName = instance.Symbol.Name ?? "";
            string combined = $"{familyName} {typeName}".ToLowerInvariant();

            foreach (var pattern in patterns)
            {
                if (combined.Contains(pattern.ToLowerInvariant()))
                    return true;
            }

            return false;
        }

        private bool ContainsAny(string text, string[] patterns)
        {
            if (string.IsNullOrEmpty(text)) return false;
            return patterns.Any(p => text.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0);
        }
    }
}
