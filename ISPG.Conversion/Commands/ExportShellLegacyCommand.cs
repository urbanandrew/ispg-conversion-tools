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
    public class ExportShellLegacyCommand : IExternalCommand
    {
        private static readonly string[] NUMBER_PARAMS = { "UX_Info_Unit_Number", "Info Unit Number" };
        private static readonly string[] BUILDING_NUMBER_PARAMS = { "Info Building Number" };
        private static readonly string[] WIDTH_PARAMS = { "UX_Room_Width", "Stretch Width" };
        private static readonly string[] DEPTH_PARAMS = { "UX_Room_Depth", "Stretch Depth" };
        private static readonly string[] HEIGHT_PARAMS = { "UX_Room_Height", "Stretch Height" };

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;

            try
            {
                // Collect shell elements
                List<Element> shells = CollectShells(doc);

                if (shells.Count == 0)
                {
                    UIHelper.Alert("No shell elements found to export.", "Export Shell");
                    return Result.Cancelled;
                }

                // Ask for file location
                string defaultFileName = $"shell_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                string filePath = UIHelper.AskForSaveFile("Export Shell to CSV", "CSV files (*.csv)|*.csv", defaultFileName);

                if (string.IsNullOrEmpty(filePath))
                    return Result.Cancelled;

                // Build export data
                List<Dictionary<string, string>> rows = new List<Dictionary<string, string>>();

                foreach (Element shell in shells)
                {
                    Dictionary<string, string> row = new Dictionary<string, string>();

                    row["ElementId"] = NumberingHelper.GetElementIdValue(shell.Id).ToString();
                    row["UniqueId"] = shell.UniqueId;
                    row["FamilyName"] = shell is FamilyInstance fi ? fi.Symbol.Family.Name : "";
                    row["TypeName"] = shell is FamilyInstance fi2 ? fi2.Symbol.Name : "";
                    row["Level"] = GetLevelName(shell);

                    row["ShellNumber"] = CsvHelper.GetParamValue(shell, NUMBER_PARAMS);
                    row["BuildingNumber"] = CsvHelper.GetParamValue(shell, BUILDING_NUMBER_PARAMS);
                    row["Width"] = CsvHelper.GetParamValue(shell, WIDTH_PARAMS);
                    row["Depth"] = CsvHelper.GetParamValue(shell, DEPTH_PARAMS);
                    row["Height"] = CsvHelper.GetParamValue(shell, HEIGHT_PARAMS);

                    // Location
                    if (shell is FamilyInstance famInst && famInst.Location is LocationPoint locPt)
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
                    "ShellNumber", "BuildingNumber",
                    "Width", "Depth", "Height",
                    "LocationX", "LocationY", "LocationZ", "Rotation"
                };

                // Write CSV
                CsvHelper.WriteCsv(filePath, rows, columns);

                UIHelper.Alert(
                    $"Export complete!\n\n" +
                    $"Shells exported: {rows.Count}\n" +
                    $"File: {filePath}",
                    "Export Shell"
                );

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.ToString();
                return Result.Failed;
            }
        }

        private List<Element> CollectShells(Document doc)
        {
            List<Element> shells = new List<Element>();

            FilteredElementCollector collector = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_GenericModel)
                .WhereElementIsNotElementType();

            foreach (Element el in collector)
            {
                if (el is FamilyInstance famInst)
                {
                    string familyName = famInst.Symbol.Family.Name;

                    if (familyName.Contains("Shell") || 
                        familyName.Contains("UX5_Shell") ||
                        familyName.Contains("ISPG Shell"))
                    {
                        // Skip nested instances
                        if (famInst.SuperComponent == null)
                        {
                            shells.Add(el);
                        }
                    }
                }
            }

            return shells;
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
