using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ISPG.Conversion.Helpers;

namespace ISPG.Conversion.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class ImportShellLegacyCommand : IExternalCommand
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
                // Ask for CSV file
                string filePath = UIHelper.AskForFile("Import Shell from CSV", "CSV files (*.csv)|*.csv");

                if (string.IsNullOrEmpty(filePath))
                    return Result.Cancelled;

                // Read CSV
                List<Dictionary<string, string>> rows = CsvHelper.ReadCsv(filePath);

                if (rows.Count == 0)
                {
                    UIHelper.Alert("CSV file is empty or invalid.", "Import Shell");
                    return Result.Cancelled;
                }

                // Build element lookup by UniqueId
                Dictionary<string, Element> elementsByUniqueId = new Dictionary<string, Element>();
                FilteredElementCollector collector = new FilteredElementCollector(doc)
                    .WhereElementIsNotElementType();

                foreach (Element el in collector)
                {
                    if (!string.IsNullOrEmpty(el.UniqueId))
                    {
                        elementsByUniqueId[el.UniqueId] = el;
                    }
                }

                // Import data
                int updated = 0;
                int skipped = 0;
                List<string> errors = new List<string>();

                using (Transaction tr = new Transaction(doc, "Import Shell"))
                {
                    tr.Start();

                    try
                    {
                        foreach (var row in rows)
                        {
                            if (!row.ContainsKey("UniqueId") || string.IsNullOrEmpty(row["UniqueId"]))
                            {
                                skipped++;
                                continue;
                            }

                            string uniqueId = row["UniqueId"];

                            if (!elementsByUniqueId.ContainsKey(uniqueId))
                            {
                                errors.Add($"Element not found: {uniqueId}");
                                skipped++;
                                continue;
                            }

                            Element element = elementsByUniqueId[uniqueId];

                            // Update parameters
                            bool anyUpdated = false;

                            if (row.ContainsKey("ShellNumber") && !string.IsNullOrEmpty(row["ShellNumber"]))
                                anyUpdated |= CsvHelper.SetParamValue(element, NUMBER_PARAMS, row["ShellNumber"]);

                            if (row.ContainsKey("BuildingNumber") && !string.IsNullOrEmpty(row["BuildingNumber"]))
                                anyUpdated |= CsvHelper.SetParamValue(element, BUILDING_NUMBER_PARAMS, row["BuildingNumber"]);

                            if (row.ContainsKey("Width") && !string.IsNullOrEmpty(row["Width"]))
                                anyUpdated |= CsvHelper.SetParamValue(element, WIDTH_PARAMS, row["Width"]);

                            if (row.ContainsKey("Depth") && !string.IsNullOrEmpty(row["Depth"]))
                                anyUpdated |= CsvHelper.SetParamValue(element, DEPTH_PARAMS, row["Depth"]);

                            if (row.ContainsKey("Height") && !string.IsNullOrEmpty(row["Height"]))
                                anyUpdated |= CsvHelper.SetParamValue(element, HEIGHT_PARAMS, row["Height"]);

                            if (anyUpdated)
                                updated++;
                            else
                                skipped++;
                        }

                        tr.Commit();

                        string errorSummary = "";
                        if (errors.Count > 0 && errors.Count <= 10)
                        {
                            errorSummary = "\n\nErrors:\n" + string.Join("\n", errors);
                        }
                        else if (errors.Count > 10)
                        {
                            errorSummary = $"\n\n{errors.Count} errors occurred";
                        }

                        UIHelper.Alert(
                            $"Import complete!\n\n" +
                            $"Rows processed: {rows.Count}\n" +
                            $"Elements updated: {updated}\n" +
                            $"Skipped: {skipped}" +
                            errorSummary,
                            "Import Shell"
                        );
                    }
                    catch (Exception ex)
                    {
                        tr.RollBack();
                        throw;
                    }
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.ToString();
                return Result.Failed;
            }
        }
    }
}
