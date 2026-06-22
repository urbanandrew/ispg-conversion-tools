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
    public class UnitNumberingQAQCCommand : IExternalCommand
    {
        private const string FAMILY_NAME_CONTAINS_UNIT = "UX5_Unit";
        private const string FAMILY_NAME_CONTAINS_PARKING = "UX5 Parking Space";

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;

            try
            {
                List<ElementId> idList = new List<ElementId>();
                List<string> numberList = new List<string>();
                int missingCount = 0;

                // Collect family instances
                FilteredElementCollector collector = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilyInstance))
                    .WhereElementIsNotElementType();

                foreach (FamilyInstance famInst in collector)
                {
                    string familyName = famInst.Symbol.FamilyName;

                    if (familyName.Contains(FAMILY_NAME_CONTAINS_UNIT) || 
                        familyName.Contains(FAMILY_NAME_CONTAINS_PARKING))
                    {
                        // Skip nested instances
                        if (famInst.SuperComponent == null)
                        {
                            idList.Add(famInst.Id);
                        }
                    }
                }

                // Check for missing and collect numbers
                List<string> missingElements = new List<string>();

                foreach (ElementId id in idList)
                {
                    Element unit = doc.GetElement(id);
                    string number = NumberingHelper.GetNumber(unit);

                    if (string.IsNullOrEmpty(number))
                    {
                        missingElements.Add($"{unit.Name}: {NumberingHelper.GetElementIdValue(unit.Id)}");
                        missingCount++;
                    }
                    else
                    {
                        numberList.Add(number);
                    }
                }

                // Find duplicates
                var duplicates = numberList.GroupBy(x => x)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                // Find skipped numbers
                List<string> skippedNumbers = new List<string>();
                numberList.Sort();

                foreach (string number in numberList.ToList())
                {
                    string nextNumber = NumberingHelper.IncrementString(number, 1);

                    if (!numberList.Contains(nextNumber))
                    {
                        string nextNextNumber = NumberingHelper.IncrementString(nextNumber, 1);

                        if (numberList.Contains(nextNextNumber))
                        {
                            if (!skippedNumbers.Contains(nextNumber))
                            {
                                skippedNumbers.Add(nextNumber);
                            }
                        }
                    }
                }

                // Build report
                string report = "UNIT NUMBERING QA/QC REPORT\n";
                report += "==========================================\n\n";

                report += $"Total elements checked: {idList.Count}\n\n";

                report += $"Missing unit numbers: {missingCount}\n";
                if (missingElements.Count > 0)
                {
                    report += "Elements without numbers:\n";
                    foreach (string elem in missingElements.Take(20))
                    {
                        report += $"  - {elem}\n";
                    }
                    if (missingElements.Count > 20)
                    {
                        report += $"  ... and {missingElements.Count - 20} more\n";
                    }
                }
                report += "\n";

                report += "Duplicate unit numbers:\n";
                if (duplicates.Count > 0)
                {
                    foreach (string dup in duplicates)
                    {
                        report += $"  - {dup}\n";
                    }
                }
                else
                {
                    report += "  None found\n";
                }
                report += "\n";

                report += "Skipped unit numbers:\n";
                if (skippedNumbers.Count > 0)
                {
                    foreach (string skipped in skippedNumbers.Take(20))
                    {
                        report += $"  - {skipped}\n";
                    }
                    if (skippedNumbers.Count > 20)
                    {
                        report += $"  ... and {skippedNumbers.Count - 20} more\n";
                    }
                }
                else
                {
                    report += "  None found\n";
                }

                // Show report
                TaskDialog td = new TaskDialog("Unit Numbering QA/QC");
                td.MainContent = report;
                td.Show();

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
