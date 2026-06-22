using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace ISPG.Conversion.Commands
{
    /// <summary>
    /// Unit Numbering QA/QC: find missing numbers, duplicates, skipped numbers
    /// </summary>
    [Transaction(TransactionMode.ReadOnly)]
    [Regeneration(RegenerationOption.Manual)]
    public class UnitNumberingQAQCCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var doc = commandData.Application.ActiveUIDocument.Document;

            try
            {
                var report = new StringBuilder();
                report.AppendLine("UNIT NUMBERING QA/QC REPORT");
                report.AppendLine("==========================================");
                report.AppendLine();

                // Collect all UX5_Unit and UX5 Parking Space instances
                var idList = new List<ElementId>();
                var instances = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilyInstance))
                    .WhereElementIsNotElementType()
                    .Cast<FamilyInstance>();

                foreach (var inst in instances)
                {
                    try
                    {
                        var familyName = inst.Symbol?.Family?.Name;
                        if (string.IsNullOrEmpty(familyName))
                            continue;

                        if ((familyName.Contains("UX5_Unit") || familyName.Contains("UX5 Parking Space")) 
                            && inst.SuperComponent == null)
                        {
                            idList.Add(inst.Id);
                        }
                    }
                    catch
                    {
                        // Ignore failures
                    }
                }

                report.AppendLine($"Total elements found: {idList.Count}");
                report.AppendLine();

                // Check for missing numbers
                var missingList = new List<string>();
                var numberList = new List<string>();

                foreach (var id in idList)
                {
                    var unit = doc.GetElement(id);
                    var numberParam = unit.LookupParameter("Info Unit Number");
                    
                    if (numberParam == null)
                        continue;

                    var numberValue = numberParam.AsString();

                    if (string.IsNullOrEmpty(numberValue))
                    {
                        missingList.Add($"{unit.Name}: {id.IntegerValue}");
                    }
                    else
                    {
                        numberList.Add(numberValue);
                    }
                }

                // Report missing numbers
                report.AppendLine("------------------------------------------");
                report.AppendLine($"Missing unit numbers: {missingList.Count}");
                if (missingList.Count > 0)
                {
                    foreach (var item in missingList)
                    {
                        report.AppendLine($"  {item}");
                    }
                }
                report.AppendLine("------------------------------------------");
                report.AppendLine();

                // Find duplicates
                var duplicates = numberList
                    .GroupBy(x => x)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                report.AppendLine("------------------------------------------");
                report.AppendLine("Unit numbers that exist more than once:");
                if (duplicates.Count > 0)
                {
                    foreach (var dup in duplicates)
                    {
                        report.AppendLine($"  {dup}");
                    }
                }
                else
                {
                    report.AppendLine("  (none)");
                }
                report.AppendLine("------------------------------------------");
                report.AppendLine();

                // Find skipped numbers
                var skippedList = new HashSet<string>();

                foreach (var number in numberList)
                {
                    var nextNumber = IncrementString(number);
                    if (numberList.Contains(nextNumber))
                        continue;

                    var nextNextNumber = IncrementString(nextNumber);
                    if (numberList.Contains(nextNextNumber))
                    {
                        skippedList.Add(nextNumber);
                    }
                }

                var skippedSorted = skippedList.OrderBy(x => x).ToList();

                report.AppendLine("------------------------------------------");
                report.AppendLine("Unit numbers that appear to have been skipped:");
                if (skippedSorted.Count > 0)
                {
                    foreach (var skipped in skippedSorted)
                    {
                        report.AppendLine($"  {skipped}");
                    }
                }
                else
                {
                    report.AppendLine("  (none)");
                }
                report.AppendLine("------------------------------------------");

                // Show report
                TaskDialog.Show("Unit Numbering QA/QC", report.ToString());

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        private string IncrementString(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            var match = Regex.Match(value, @"(\d+)$");
            if (!match.Success)
                return value;

            var numberText = match.Groups[1].Value;
            var prefix = value.Substring(0, match.Groups[1].Index);

            if (!int.TryParse(numberText, out int number))
                return value;

            var nextNumber = number + 1;
            var width = numberText.Length;

            return prefix + nextNumber.ToString().PadLeft(width, '0');
        }
    }
}
