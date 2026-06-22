using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using ISPG.Conversion.Helpers;

namespace ISPG.Conversion.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class UnitNumberingCommand : IExternalCommand
    {
        private class CategoryOption
        {
            public string Label { get; set; }
            public BuiltInCategory Category { get; set; }

            public CategoryOption(string label, BuiltInCategory category)
            {
                Label = label;
                Category = category;
            }
        }

        private static readonly List<CategoryOption> CategoryOptions = new List<CategoryOption>
        {
            new CategoryOption("Generic Models", BuiltInCategory.OST_GenericModel),
            new CategoryOption("Parking", BuiltInCategory.OST_Parking)
        };

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;
            View activeView = doc.ActiveView;

            try
            {
                // Check if we're in a model view
                if (!IsModelView(activeView))
                {
                    UIHelper.Alert(
                        "Please run this from a model view, not a sheet, schedule, legend, or browser view.",
                        "Unit Numbering"
                    );
                    return Result.Cancelled;
                }

                // Ask for category
                var categoryOptions = CategoryOptions.Select(opt => 
                    new OptionItem<CategoryOption>(GetCategoryName(doc, opt), opt)
                ).ToList();

                var selectedCategory = UIHelper.AskForOption(
                    "Unit Numbering",
                    "Pick element type to renumber:",
                    categoryOptions
                );

                if (selectedCategory == null)
                    return Result.Cancelled;

                string categoryName = GetCategoryName(doc, selectedCategory);
                BuiltInCategory bic = selectedCategory.Category;

                // Ask for starting number
                string startingNumber = UIHelper.AskForString(
                    $"Unit Numbering - {categoryName}",
                    "Enter starting number:",
                    "001"
                );

                if (string.IsNullOrEmpty(startingNumber))
                    return Result.Cancelled;

                // Ask for numbering mode
                var numberingModes = new List<OptionItem<int>>
                {
                    new OptionItem<int>("Consecutive (001, 002, 003, ...)", 1),
                    new OptionItem<int>("Skip Every Other (001, 003, 005, ...)", 2)
                };

                int? step = UIHelper.AskForOption(
                    "Numbering Mode",
                    "Choose numbering mode:",
                    numberingModes
                );

                if (!step.HasValue)
                    return Result.Cancelled;

                UIHelper.Alert(
                    $"After closing this message, select {categoryName.ToLower()} one by one in order.\n\n" +
                    "Each element will be numbered immediately and temporarily highlighted.\n\n" +
                    "Press ESC when finished.",
                    $"Unit Numbering - {categoryName}"
                );

                // Run the interactive numbering
                PickAndRenumberLive(uidoc, doc, activeView, categoryName, bic, startingNumber, step.Value);

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.ToString();
                return Result.Failed;
            }
        }

        private void PickAndRenumberLive(
            UIDocument uidoc,
            Document doc,
            View activeView,
            string categoryName,
            BuiltInCategory bic,
            string startingIndex,
            int step)
        {
            // Collect existing numbers
            Dictionary<string, ElementId> existingNumbers = CollectExistingNumbers(doc, bic);
            List<ElementId> renumberedIds = new List<ElementId>();

            CategorySelectionFilter filter = new CategorySelectionFilter(bic);
            string prompt = $"Select {categoryName.ToLower()} in order. Press ESC when done.";

            int successCount = 0;
            int failedCount = 0;
            string currentIndex = startingIndex;

            using (TransactionGroup tg = new TransactionGroup(doc, $"Renumber {categoryName}"))
            {
                tg.Start();

                try
                {
                    while (true)
                    {
                        Reference pickedRef = null;

                        try
                        {
                            pickedRef = uidoc.Selection.PickObject(
                                ObjectType.Element,
                                filter,
                                prompt
                            );
                        }
                        catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                        {
                            // User pressed ESC
                            break;
                        }

                        if (pickedRef == null)
                            break;

                        Element targetElement = doc.GetElement(pickedRef.ElementId);

                        if (targetElement == null)
                            continue;

                        using (Transaction t = new Transaction(doc, $"Renumber {categoryName}"))
                        {
                            t.Start();

                            try
                            {
                                bool success = RenumberElementNow(
                                    targetElement,
                                    currentIndex,
                                    existingNumbers,
                                    activeView
                                );

                                if (success)
                                {
                                    renumberedIds.Add(targetElement.Id);
                                    successCount++;
                                }
                                else
                                {
                                    failedCount++;
                                }

                                t.Commit();
                            }
                            catch
                            {
                                failedCount++;
                                t.RollBack();
                            }
                        }

                        currentIndex = NumberingHelper.IncrementString(currentIndex, step);
                    }

                    tg.Assimilate();
                }
                catch
                {
                    tg.RollBack();
                    throw;
                }
            }

            // Clear visual marks
            if (renumberedIds.Count > 0)
            {
                using (Transaction t = new Transaction(doc, $"Unmark {categoryName}"))
                {
                    t.Start();

                    try
                    {
                        UnmarkRenumberedElements(activeView, renumberedIds);
                        t.Commit();
                    }
                    catch
                    {
                        t.RollBack();
                    }
                }
            }

            UIHelper.Alert(
                $"Renumber complete.\n\nUpdated: {successCount}\nFailed: {failedCount}",
                $"Unit Numbering - {categoryName}"
            );
        }

        private bool RenumberElementNow(
            Element element,
            string newNumber,
            Dictionary<string, ElementId> existingNumbers,
            View view)
        {
            string oldNumber = NumberingHelper.GetNumber(element);

            if (!string.IsNullOrEmpty(oldNumber) && existingNumbers.ContainsKey(oldNumber))
            {
                existingNumbers.Remove(oldNumber);
            }

            bool success = NumberingHelper.SetNumber(element, newNumber);

            if (success)
            {
                existingNumbers[newNumber] = element.Id;
                MarkElementAsRenumbered(view, element);
            }

            return success;
        }

        private void MarkElementAsRenumbered(View view, Element element)
        {
            try
            {
                OverrideGraphicSettings ogs = new OverrideGraphicSettings();
                ogs.SetHalftone(true);
                ogs.SetSurfaceTransparency(75);
                view.SetElementOverrides(element.Id, ogs);
            }
            catch
            {
                // Silently fail if marking doesn't work
            }
        }

        private void UnmarkRenumberedElements(View view, List<ElementId> elementIds)
        {
            try
            {
                OverrideGraphicSettings ogs = new OverrideGraphicSettings();

                foreach (ElementId id in elementIds)
                {
                    try
                    {
                        view.SetElementOverrides(id, ogs);
                    }
                    catch { }
                }
            }
            catch { }
        }

        private Dictionary<string, ElementId> CollectExistingNumbers(Document doc, BuiltInCategory bic)
        {
            Dictionary<string, ElementId> data = new Dictionary<string, ElementId>();

            FilteredElementCollector collector = new FilteredElementCollector(doc)
                .OfCategory(bic)
                .WhereElementIsNotElementType();

            foreach (Element element in collector)
            {
                string number = NumberingHelper.GetNumber(element);

                if (!string.IsNullOrEmpty(number) && !data.ContainsKey(number))
                {
                    data[number] = element.Id;
                }
            }

            return data;
        }

        private string GetCategoryName(Document doc, CategoryOption option)
        {
            try
            {
                Category cat = doc.Settings.Categories.get_Item(option.Category);
                if (cat != null)
                    return cat.Name;
            }
            catch { }

            return option.Label;
        }

        private bool IsModelView(View view)
        {
            if (view == null)
                return false;

            try
            {
                ViewType[] badViewTypes = new[]
                {
                    ViewType.DrawingSheet,
                    ViewType.Schedule,
                    ViewType.ProjectBrowser,
                    ViewType.SystemBrowser,
                    ViewType.Legend
                };

                return !badViewTypes.Contains(view.ViewType);
            }
            catch
            {
                return false;
            }
        }

        // Selection filter class
        private class CategorySelectionFilter : ISelectionFilter
        {
            private BuiltInCategory _category;

            public CategorySelectionFilter(BuiltInCategory category)
            {
                _category = category;
            }

            public bool AllowElement(Element element)
            {
                try
                {
                    if (element.Category == null)
                        return false;

                    return (BuiltInCategory)element.Category.Id.Value == _category;
                }
                catch
                {
                    return false;
                }
            }

            public bool AllowReference(Reference reference, XYZ position)
            {
                return true;
            }
        }
    }
}
