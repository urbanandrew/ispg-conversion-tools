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
    public class ExportParkingCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                var uiDoc = commandData.Application.ActiveUIDocument;
                var doc = uiDoc.Document;

                // Find all parking family instances
                var parkingInstances = FindParking(doc);

                if (!parkingInstances.Any())
                {
                    TaskDialog.Show("Export Parking", "No parking units found in the current model.");
                    return Result.Cancelled;
                }

                // Generate output file path
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string outputPath = Path.Combine(@"C:\Temp", $"umx_parking_extract_{timestamp}.json");

                // Ensure C:\Temp exists
                Directory.CreateDirectory(@"C:\Temp");

                // Export using dedicated parking exporter
                var exporter = new ParkingExporter(doc);
                exporter.Export(parkingInstances, outputPath);

                // Show success message
                TaskDialog.Show("Export Parking",
                    $"Successfully exported {parkingInstances.Count()} parking units to:\n\n{outputPath}");

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
        /// Find all parking space family instances in the document
        /// Matches legacy families (ISPG Parking Space) and current UX5 Parking Space
        /// </summary>
        private IEnumerable<FamilyInstance> FindParking(Document doc)
        {
            // Match Python PARKING_FAMILY_NAME_CONTAINS exactly
            return new FilteredElementCollector(doc)
                .OfClass(typeof(FamilyInstance))
                .Cast<FamilyInstance>()
                .Where(fi => IsParking(fi));
        }

        private bool IsParking(FamilyInstance instance)
        {
            if (instance == null || instance.Symbol == null) return false;

            string familyName = instance.Symbol.FamilyName ?? "";

            // Match Python: PARKING_FAMILY_NAME_CONTAINS = ["Parking Space"]
            return familyName.IndexOf("Parking Space", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private bool ContainsAny(string text, string[] patterns)
        {
            if (string.IsNullOrEmpty(text)) return false;
            return patterns.Any(p => text.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0);
        }
    }
}
