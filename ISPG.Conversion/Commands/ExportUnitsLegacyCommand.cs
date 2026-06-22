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
    public class ExportUnitsLegacyCommand : IExternalCommand
    {
        private static readonly string[] UNIT_NUMBER_PARAMS = { "UX_Info_Unit_Number", "Info Unit Number" };
        private static readonly string[] BUILDING_NUMBER_PARAMS = { "Info Building Number" };
        private static readonly string[] WIDTH_PARAMS = { "UX_Room_Width", "Stretch Width" };
        private static readonly string[] DEPTH_PARAMS = { "UX_Room_Depth", "Stretch Depth" };
        private static readonly string[] HEIGHT_PARAMS = { "UX_Room_Height", "Stretch Height" };
        private static readonly string[] CLIMATE_PARAMS = { "UX_Info_Unit_CC", "Info Climate Controlled" };
        private static readonly string[] DRIVEUP_PARAMS = { "UX_Info_Unit_DriveUp", "Info DriveUp" };
        private static readonly string[] LOCKER_PARAMS = { "UX_Info_Locker", "Info Locker" };
        private static readonly string[] GROUND_ACCESS_PARAMS = { "UX_Info_Unit_GroundAccess", "Info Ground Access" };

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;

            try
            {
                // Collect units
                List<Element> units = CollectUnits(doc);

                if (units.Count == 0)
                {
                    UIHelper.Alert("No unit elements found to export.", "Export Units");
                    return Result.Cancelled;
                }

                // Ask for file location
                string defaultFileName = $"units_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                string filePath = UIHelper.AskForSaveFile("Export Units to CSV", "CSV files (*.csv)|*.csv", defaultFileName);

                if (string.IsNullOrEmpty(filePath))
                    return Result.Cancelled;

                // Build export data
                List<Dictionary<string, string>> rows = new List<Dictionary<string, string>>();

                foreach (Element unit in units)
                {
                    Dictionary<string, string> row = new Dictionary<string, string>();

                    row["ElementId"] = NumberingHelper.GetElementIdValue(unit.Id).ToString();
                    row["UniqueId"] = unit.UniqueId;
                    row["FamilyName"] = unit is FamilyInstance fi ? fi.Symbol.Family.Name : "";
                    row["TypeName"] = unit is FamilyInstance fi2 ? fi2.Symbol.Name : "";
                    row["Level"] = GetLevelName(unit);

                    row["UnitNumber"] = CsvHelper.GetParamValue(unit, UNIT_NUMBER_PARAMS);
                    row["BuildingNumber"] = CsvHelper.GetParamValue(unit, BUILDING_NUMBER_PARAMS);
                    row["Width"] = CsvHelper.GetParamValue(unit, WIDTH_PARAMS);
                    row["Depth"] = CsvHelper.GetParamValue(unit, DEPTH_PARAMS);
                    row["Height"] = CsvHelper.GetParamValue(unit, HEIGHT_PARAMS);
                    row["ClimateControlled"] = CsvHelper.GetParamValue(unit, CLIMATE_PARAMS);
                    row["DriveUp"] = CsvHelper.GetParamValue(unit, DRIVEUP_PARAMS);
                    row["Locker"] = CsvHelper.GetParamValue(unit, LOCKER_PARAMS);
                    row["GroundAccess"] = CsvHelper.GetParamValue(unit, GROUND_ACCESS_PARAMS);

                    // Location
                    if (unit is FamilyInstance famInst && famInst.Location is LocationPoint locPt)
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
                    "UnitNumber", "BuildingNumber",
                    "Width", "Depth", "Height",
                    "ClimateControlled", "DriveUp", "Locker", "GroundAccess",
                    "LocationX", "LocationY", "LocationZ", "Rotation"
                };

                // Write CSV
                CsvHelper.WriteCsv(filePath, rows, columns);

                UIHelper.Alert(
                    $"Export complete!\n\n" +
                    $"Units exported: {rows.Count}\n" +
                    $"File: {filePath}",
                    "Export Units"
                );

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.ToString();
                return Result.Failed;
            }
        }

        private List<Element> CollectUnits(Document doc)
        {
            List<Element> units = new List<Element>();

            FilteredElementCollector collector = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_GenericModel)
                .WhereElementIsNotElementType();

            foreach (Element el in collector)
            {
                if (el is FamilyInstance famInst)
                {
                    string familyName = famInst.Symbol.Family.Name;

                    if (familyName.Contains("UX5_Unit") || 
                        familyName.Contains("UX4 Unit") || 
                        familyName.Contains("ISPG UX Unit"))
                    {
                        // Skip nested instances
                        if (famInst.SuperComponent == null)
                        {
                            units.Add(el);
                        }
                    }
                }
            }

            return units;
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
