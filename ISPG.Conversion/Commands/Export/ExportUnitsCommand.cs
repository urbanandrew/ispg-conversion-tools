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
    public class ExportUnitsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                var uiDoc = commandData.Application.ActiveUIDocument;
                var doc = uiDoc.Document;

                // Find all unit family instances
                var unitInstances = FindUnits(doc);

                if (!unitInstances.Any())
                {
                    TaskDialog.Show("Export Units", "No units found in the current model.");
                    return Result.Cancelled;
                }

                // Generate output file path
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string outputPath = Path.Combine(@"C:\Temp", $"umx_2022_extract_{timestamp}.json");

                // Ensure C:\Temp exists
                Directory.CreateDirectory(@"C:\Temp");

                // Export
                var exporter = new JsonExporter(doc, "units");
                exporter.Export(unitInstances, outputPath);

                // Show success message
                TaskDialog.Show("Export Units",
                    $"Successfully exported {unitInstances.Count()} units to:\n\n{outputPath}");

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
        /// Find all unit family instances in the document
        /// Matches families/types containing "UX" or "Unit"
        /// </summary>
        private IEnumerable<FamilyInstance> FindUnits(Document doc)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(FamilyInstance))
                .Cast<FamilyInstance>()
                .Where(fi => IsUnit(fi));
        }

        private bool IsUnit(FamilyInstance instance)
        {
            if (instance == null || instance.Symbol == null) return false;

            string familyName = instance.Symbol.FamilyName ?? "";
            string typeName = instance.Symbol.Name ?? "";

            // Match families/types containing "UX" or "Unit" (case-insensitive)
            return ContainsAny(familyName, new[] { "UX", "Unit" }) ||
                   ContainsAny(typeName, new[] { "UX", "Unit" });
        }

        private bool ContainsAny(string text, string[] patterns)
        {
            if (string.IsNullOrEmpty(text)) return false;
            return patterns.Any(p => text.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0);
        }
    }
}
