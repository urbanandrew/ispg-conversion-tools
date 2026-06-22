using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ISPG.Conversion.Helpers;

namespace ISPG.Conversion.Commands
{
    [Transaction(TransactionMode.ReadOnly)]
    public class ExportParkingLegacyCommand : IExternalCommand
    {
        private static readonly string[] NUMBER_PARAMS = { "UX_Info_Unit_Number", "Info Unit Number" };
        private static readonly string[] ACCESSIBLE_PARAMS = { "Info Accessible" };

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;

            try
            {
                // Collect parking spaces
                List<Element> parkingSpaces = CollectParkingSpaces(doc);

                if (parkingSpaces.Count == 0)
                {
                    UIHelper.Alert("No parking space elements found to export.", "Export Parking");
                    return Result.Cancelled;
                }

                // Ask for file location
                string defaultFileName = $"parking_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                string filePath = UIHelper.AskForSaveFile("Export Parking to CSV", "CSV files (*.csv)|*.csv", defaultFileName);

                if (string.IsNullOrEmpty(filePath))
                    return Result.Cancelled;

                // Build export data
                List<Dictionary<string, string>> rows = new List<Dictionary<string, string>>();

                foreach (Element parking in parkingSpaces)
                {
                    Dictionary<string, string> row = new Dictionary<string, string>();

                    row["ElementId"] = NumberingHelper.GetElementIdValue(parking.Id).ToString();
                    row["UniqueId"] = parking.UniqueId;
                    row["FamilyName"] = parking is FamilyInstance fi ? fi.Symbol.Family.Name : "";
                    row["TypeName"] = parking is FamilyInstance fi2 ? fi2.Symbol.Name : "";
                    row["Level"] = GetLevelName(parking);

                    row["SpaceNumber"] = CsvHelper.GetParamValue(parking, NUMBER_PARAMS);
                    row["Accessible"] = CsvHelper.GetParamValue(parking, ACCESSIBLE_PARAMS);

                    // Location
                    if (parking is FamilyInstance famInst && famInst.Location is LocationPoint locPt)
                    {
                        row["LocationX"] = locPt.Point.X.ToString("F6");
                        row["LocationY"] = locPt.Point.Y.ToString("F6");
                        row["LocationZ"] = locPt.Point.Z.ToString("F6");
                        row["Rotation"] = locPt.Rotation.ToString("F6");
                    }
                    else
                    {
                        row["LocationX"] = "";
                        row["LocationY"] = "";
                        row["LocationZ"] = "";
                        row["Rotation"] = "";
                    }

                    rows.Add(row);
                }

                // Define column order
                List<string> columns = new List<string>
                {
                    "ElementId", "UniqueId", "FamilyName", "TypeName", "Level",
                    "SpaceNumber", "Accessible",
                    "LocationX", "LocationY", "LocationZ", "Rotation"
                };

                // Write CSV
                CsvHelper.WriteCsv(filePath, rows, columns);

                UIHelper.Alert(
                    $"Export complete!\n\n" +
                    $"Parking spaces exported: {rows.Count}\n" +
                    $"File: {filePath}",
                    "Export Parking"
                );

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.ToString();
                return Result.Failed;
            }
        }

        private List<Element> CollectParkingSpaces(Document doc)
        {
            List<Element> parkingSpaces = new List<Element>();

            FilteredElementCollector collector = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Parking)
                .WhereElementIsNotElementType();

            foreach (Element el in collector)
            {
                if (el is FamilyInstance famInst)
                {
                    // Skip nested instances
                    if (famInst.SuperComponent == null)
                    {
                        parkingSpaces.Add(el);
                    }
                }
            }

            return parkingSpaces;
        }

        private string GetLevelName(Element element)
        {
            try
            {
                if (element is FamilyInstance famInst)
                {
                    Level level = famInst.Document.GetElement(famInst.LevelId) as Level;
                    return level?.Name ?? "";
                }
            }
            catch { }

            return "";
        }
    }
}
