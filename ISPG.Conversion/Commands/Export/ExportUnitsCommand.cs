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
        /// Matches legacy families (ISPG UX Shell/Unit, UX3/UX4) and current UX5_Unit families
        /// </summary>
        private IEnumerable<FamilyInstance> FindUnits(Document doc)
        {
            // Family name patterns to match (case-insensitive contains or starts-with)
            string[] containsPatterns = new[] { "ISPG UX Shell", "ISPG UX Unit", "UX4 Unit" };
            string[] startsWithPatterns = new[] { "UX3 Unit", "UX5_Unit" };

            return new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_GenericModel)
                .WhereElementIsNotElementType()
                .Cast<FamilyInstance>()
                .Where(fi => IsUnit(fi, containsPatterns, startsWithPatterns));
        }

        private bool IsUnit(FamilyInstance instance, string[] containsPatterns, string[] startsWithPatterns)
        {
            if (instance == null || instance.Symbol == null) return false;

            string familyName = instance.Symbol.FamilyName ?? "";
            string typeName = instance.Symbol.Name ?? "";
            string combined = $"{familyName} {typeName}".ToLowerInvariant();

            // Check contains patterns
            foreach (var pattern in containsPatterns)
            {
                if (combined.Contains(pattern.ToLowerInvariant()))
                    return true;
            }

            // Check starts-with patterns
            foreach (var pattern in startsWithPatterns)
            {
                string patternLower = pattern.ToLowerInvariant();
                if (familyName.ToLowerInvariant().StartsWith(patternLower) ||
                    typeName.ToLowerInvariant().StartsWith(patternLower))
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
