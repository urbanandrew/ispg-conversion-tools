using System;
using System.Linq;
using System.Windows.Forms;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ISPG.Conversion.Core;

namespace ISPG.Conversion.Commands.Import
{
    [Transaction(TransactionMode.Manual)]
    public class ImportParkingCommand : IExternalCommand
    {
        // Target family/type for import (UX5_Parking)
        private const string TARGET_FAMILY_NAME = "UX5 Parking Space";
        private const string TARGET_TYPE_NAME = "UX5 Parking Space";

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                var uiDoc = commandData.Application.ActiveUIDocument;
                var doc = uiDoc.Document;

                // Prompt for JSON file
                var openDialog = new OpenFileDialog
                {
                    Title = "Select Parking Export JSON",
                    Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                    InitialDirectory = @"C:\Temp",
                    CheckFileExists = true
                };

                if (openDialog.ShowDialog() != DialogResult.OK)
                    return Result.Cancelled;

                string jsonPath = openDialog.FileName;

                // Create importer
                var importer = new JsonImporter(doc, TARGET_FAMILY_NAME, TARGET_TYPE_NAME);

                // Show confirmation dialog
                var confirmResult = MessageBox.Show(
                    $"Import parking from:\n\n{jsonPath}\n\n" +
                    $"Target family: {TARGET_FAMILY_NAME}\n" +
                    $"Target type: {TARGET_TYPE_NAME}\n\n" +
                    "Continue?",
                    "Confirm Import",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (confirmResult != DialogResult.Yes)
                    return Result.Cancelled;

                // Execute import in transaction
                int importedCount = 0;
                int skippedCount = 0;
                var errors = new System.Collections.Generic.List<string>();

                using (var trans = new Transaction(doc, "Import Parking"))
                {
                    trans.Start();

                    try
                    {
                        (importedCount, skippedCount, errors) = importer.Import(jsonPath);
                        trans.Commit();
                    }
                    catch (Exception ex)
                    {
                        trans.RollBack();
                        throw new Exception($"Import failed: {ex.Message}", ex);
                    }
                }

                // Show results
                string resultMessage = $"Import completed:\n\n" +
                                       $"Imported: {importedCount}\n" +
                                       $"Skipped: {skippedCount}";

                if (errors.Any())
                {
                    resultMessage += $"\n\nErrors:\n" + string.Join("\n", errors.Take(10));
                    if (errors.Count > 10)
                        resultMessage += $"\n... and {errors.Count - 10} more";
                }

                TaskDialog.Show("Import Parking", resultMessage);

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.ToString();
                TaskDialog.Show("Error", $"Import failed:\n\n{ex.Message}");
                return Result.Failed;
            }
        }
    }
}
