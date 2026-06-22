using System;
using System.Collections.Generic;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ISPG.Conversion.Helpers;

namespace ISPG.Conversion.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class UnitMaterialCommand : IExternalCommand
    {
        private const string FAMILY_NAME_CONTAINS = "UX5_Unit";
        private const string AREA_PARAM_NAME = "Label Area";
        private const string MATERIAL_PARAM_NAME = "Info Material";

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;

            try
            {
                // Collect UX5_Unit blocks
                List<ElementId> umxBlocks = CollectUmxBlocks(doc);

                UIHelper.Alert(
                    $"Found {umxBlocks.Count} UX5_Unit blocks.\n\n" +
                    "Materials will be assigned based on rentable area.",
                    "Unit Material Assignment"
                );

                if (umxBlocks.Count == 0)
                {
                    UIHelper.Alert("No UX5_Unit blocks found in the project.", "Unit Material");
                    return Result.Cancelled;
                }

                // Update materials
                var result = UpdateMaterialForUmxBlocks(doc, umxBlocks);

                UIHelper.Alert(
                    $"Material Assignment Complete\n\n" +
                    $"Updated: {result.Updated}\n" +
                    $"Skipped: {result.Skipped}\n\n" +
                    $"Skipped elements either had no Label Area, " +
                    $"no matching material, or read-only material parameter.",
                    "Unit Material"
                );

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.ToString();
                return Result.Failed;
            }
        }

        private List<ElementId> CollectUmxBlocks(Document doc)
        {
            List<ElementId> umxBlocks = new List<ElementId>();

            FilteredElementCollector collector = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilyInstance))
                .WhereElementIsNotElementType();

            foreach (FamilyInstance inst in collector)
            {
                try
                {
                    string familyName = inst.Symbol.Family.Name;

                    if (familyName.Contains(FAMILY_NAME_CONTAINS) && inst.SuperComponent == null)
                    {
                        umxBlocks.Add(inst.Id);
                    }
                }
                catch { }
            }

            return umxBlocks;
        }

        private int DetermineMaterialNumber(double rentableArea)
        {
            if (rentableArea <= 100)
            {
                return (int)(5 * Math.Round(rentableArea / 5.0));
            }
            else if (rentableArea <= 150)
            {
                return (int)(10 * Math.Round(rentableArea / 10.0));
            }
            else
            {
                return (int)(25 * Math.Round(rentableArea / 25.0));
            }
        }

        private string FormatMaterialString(int materialNumber)
        {
            if (materialNumber < 100)
            {
                return $"0{materialNumber}";
            }
            else if (materialNumber <= 275)
            {
                return materialNumber.ToString();
            }
            else
            {
                return "300+";
            }
        }

        private Dictionary<string, Material> GetUnitMaterialsByName(Document doc)
        {
            Dictionary<string, Material> mats = new Dictionary<string, Material>();

            FilteredElementCollector collector = new FilteredElementCollector(doc)
                .OfClass(typeof(Material));

            foreach (Material mat in collector)
            {
                try
                {
                    mats[mat.Name] = mat;
                }
                catch { }
            }

            return mats;
        }

        private double? GetAreaValue(Element element, string paramName)
        {
            Parameter p = element.LookupParameter(paramName);

            if (p == null)
                return null;

            try
            {
                if (p.StorageType == StorageType.Double)
                {
                    return p.AsDouble();
                }
            }
            catch { }

            return null;
        }

        private class UpdateResult
        {
            public int Updated { get; set; }
            public int Skipped { get; set; }
        }

        private UpdateResult UpdateMaterialForUmxBlocks(Document doc, List<ElementId> umxBlocks)
        {
            var result = new UpdateResult();
            Dictionary<string, Material> matsByName = GetUnitMaterialsByName(doc);

            using (Transaction tr = new Transaction(doc, "Update UMX Block Materials"))
            {
                tr.Start();

                try
                {
                    foreach (ElementId uxId in umxBlocks)
                    {
                        Element ux = doc.GetElement(uxId);

                        double? rentableArea = GetAreaValue(ux, AREA_PARAM_NAME);

                        if (!rentableArea.HasValue)
                        {
                            result.Skipped++;
                            continue;
                        }

                        int materialNumber = DetermineMaterialNumber(rentableArea.Value);
                        string materialString = FormatMaterialString(materialNumber);
                        string materialName = $"Units {materialString}";

                        if (!matsByName.ContainsKey(materialName))
                        {
                            result.Skipped++;
                            continue;
                        }

                        Material unitMaterial = matsByName[materialName];

                        Parameter pMat = ux.LookupParameter(MATERIAL_PARAM_NAME);

                        if (pMat == null || pMat.IsReadOnly)
                        {
                            result.Skipped++;
                            continue;
                        }

                        pMat.Set(unitMaterial.Id);
                        result.Updated++;
                    }

                    tr.Commit();
                }
                catch
                {
                    tr.RollBack();
                    throw;
                }
            }

            return result;
        }
    }
}
