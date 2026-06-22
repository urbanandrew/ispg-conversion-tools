using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace ISPG.Conversion.Commands
{
    /// <summary>
    /// Unit Material Management: assign materials based on rentable area
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class UnitMaterialCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var doc = commandData.Application.ActiveUIDocument.Document;

            try
            {
                // Collect UMX blocks
                var umxBlocks = CollectUmxBlocks(doc);

                if (umxBlocks.Count == 0)
                {
                    TaskDialog.Show("Unit Material Management", 
                        "No UX5_Unit blocks found in the project.");
                    return Result.Cancelled;
                }

                // Update materials
                var (updated, skipped) = UpdateMaterialForUmxBlocks(doc, umxBlocks);

                TaskDialog.Show("Unit Material Management",
                    $"Found UX5_Unit blocks: {umxBlocks.Count}\n\n" +
                    $"Updated materials: {updated}\n" +
                    $"Skipped: {skipped}");

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        private List<ElementId> CollectUmxBlocks(Document doc)
        {
            var umxBlocks = new List<ElementId>();

            var instances = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilyInstance))
                .WhereElementIsNotElementType()
                .Cast<FamilyInstance>();

            foreach (var inst in instances)
            {
                try
                {
                    var familyName = inst.Symbol?.Family?.Name;
                    if (!string.IsNullOrEmpty(familyName) && 
                        familyName.Contains("UX5_Unit") && 
                        inst.SuperComponent == null)
                    {
                        umxBlocks.Add(inst.Id);
                    }
                }
                catch
                {
                    // Ignore failures
                }
            }

            return umxBlocks;
        }

        private (int updated, int skipped) UpdateMaterialForUmxBlocks(Document doc, List<ElementId> umxBlocks)
        {
            // Get all materials indexed by name
            var matsByName = GetUnitMaterialsByName(doc);

            int updated = 0;
            int skipped = 0;

            using (var tr = new Transaction(doc, "Update UMX Block Materials"))
            {
                tr.Start();

                try
                {
                    foreach (var uxId in umxBlocks)
                    {
                        var ux = doc.GetElement(uxId);

                        // Get rentable area
                        var rentableArea = GetAreaValue(ux, "Label Area");
                        if (rentableArea == null)
                        {
                            skipped++;
                            continue;
                        }

                        // Determine material
                        var materialNumber = DetermineMaterialNumber(rentableArea.Value);
                        var materialString = FormatMaterialString(materialNumber);
                        var materialName = $"Units {materialString}";

                        // Find material
                        if (!matsByName.TryGetValue(materialName, out Material unitMaterial))
                        {
                            skipped++;
                            continue;
                        }

                        // Set material parameter
                        var pMat = ux.LookupParameter("Info Material");
                        if (pMat == null || pMat.IsReadOnly)
                        {
                            skipped++;
                            continue;
                        }

                        pMat.Set(unitMaterial.Id);
                        updated++;
                    }

                    tr.Commit();
                }
                catch
                {
                    tr.RollBack();
                    throw;
                }
            }

            return (updated, skipped);
        }

        private Dictionary<string, Material> GetUnitMaterialsByName(Document doc)
        {
            var mats = new Dictionary<string, Material>();

            var allMats = new FilteredElementCollector(doc)
                .OfClass(typeof(Material))
                .Cast<Material>();

            foreach (var mat in allMats)
            {
                try
                {
                    if (!string.IsNullOrEmpty(mat.Name))
                        mats[mat.Name] = mat;
                }
                catch
                {
                    // Ignore failures
                }
            }

            return mats;
        }

        private double? GetAreaValue(Element element, string paramName)
        {
            var p = element.LookupParameter(paramName);
            if (p == null)
                return null;

            try
            {
                if (p.StorageType == StorageType.Double)
                    return p.AsDouble();
            }
            catch
            {
                // Ignore failures
            }

            return null;
        }

        private int DetermineMaterialNumber(double rentableArea)
        {
            if (rentableArea <= 100)
                return (int)(5 * Math.Round(rentableArea / 5.0));
            else if (rentableArea <= 150)
                return (int)(10 * Math.Round(rentableArea / 10.0));
            else
                return (int)(25 * Math.Round(rentableArea / 25.0));
        }

        private string FormatMaterialString(int materialNumber)
        {
            if (materialNumber < 100)
                return $"0{materialNumber}";
            else if (materialNumber <= 275)
                return materialNumber.ToString();
            else
                return "300+";
        }
    }
}
