using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace ISPG.Conversion.Commands
{
    /// <summary>
    /// Interactive unit renumbering: pick elements one-by-one, renumber sequentially
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class UnitNumberingCommand : IExternalCommand
    {
        private static readonly string[] NUMBER_PARAM_NAMES = new[]
        {
            "Info Unit Number",
            "UX_Info_Unit_Number"
        };

        private static readonly CategoryOption[] CATEGORY_OPTIONS = new[]
        {
            new CategoryOption { Label = "Generic Models", Category = BuiltInCategory.OST_GenericModel },
            new CategoryOption { Label = "Parking", Category = BuiltInCategory.OST_Parking }
        };

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiDoc = commandData.Application.ActiveUIDocument;
            var doc = uiDoc.Document;
            var activeView = doc.ActiveView;

            try
            {
                // Validate view type
                if (!IsModelView(activeView))
                {
                    TaskDialog.Show("Unit Numbering", 
                        "Please run this from a model view, not a sheet, schedule, legend, or browser view.");
                    return Result.Cancelled;
                }

                // Ask user to select category
                var selectedOption = AskForCategoryOption();
                if (selectedOption == null)
                    return Result.Cancelled;

                var categoryName = GetCategoryName(doc, selectedOption.Category, selectedOption.Label);

                // Ask for starting number
                var startingNumber = AskForStartingNumber(categoryName);
                if (string.IsNullOrEmpty(startingNumber))
                    return Result.Cancelled;

                // Ask for numbering step
                var step = AskForNumberingStep();
                if (step == 0)
                    return Result.Cancelled;

                // Instructions
                TaskDialog.Show("Unit Numbering",
                    $"After closing this message, select {categoryName} one by one in order.\n\n" +
                    "Each element will be numbered immediately and temporarily highlighted.\n\n" +
                    "Press ESC when finished.");

                // Interactive renumbering
                PickAndRenumberLive(doc, uiDoc, activeView, categoryName, selectedOption.Category, startingNumber, step);

                return Result.Succeeded;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        private bool IsModelView(View view)
        {
            if (view == null) return false;

            var badTypes = new[]
            {
                ViewType.DrawingSheet,
                ViewType.Schedule,
                ViewType.ProjectBrowser,
                ViewType.SystemBrowser,
                ViewType.Legend
            };

            return !badTypes.Contains(view.ViewType);
        }

        private CategoryOption AskForCategoryOption()
        {
            var dialog = new TaskDialog("Select Category");
            dialog.MainInstruction = "Which category do you want to renumber?";
            
            foreach (var option in CATEGORY_OPTIONS)
            {
                dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1 + Array.IndexOf(CATEGORY_OPTIONS, option), 
                    option.Label);
            }

            dialog.CommonButtons = TaskDialogCommonButtons.Cancel;
            var result = dialog.Show();

            if (result >= TaskDialogResult.CommandLink1 && result <= TaskDialogResult.CommandLink4)
            {
                var index = (int)result - (int)TaskDialogResult.CommandLink1;
                return CATEGORY_OPTIONS[index];
            }

            return null;
        }

        private string AskForStartingNumber(string categoryName)
        {
            var input = new InputDialog($"Starting Number for {categoryName}", "Enter starting number (e.g., 101, A001, P-001):", "101");
            if (input.ShowDialog() == true && !string.IsNullOrWhiteSpace(input.InputText))
            {
                return input.InputText.Trim();
            }
            return null;
        }

        private int AskForNumberingStep()
        {
            var dialog = new TaskDialog("Numbering Mode");
            dialog.MainInstruction = "How do you want to number?";
            dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Every number (1, 2, 3...)");
            dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "Every other number (1, 3, 5...)");
            dialog.CommonButtons = TaskDialogCommonButtons.Cancel;

            var result = dialog.Show();
            
            if (result == TaskDialogResult.CommandLink1) return 1;
            if (result == TaskDialogResult.CommandLink2) return 2;
            
            return 0;
        }

        private string GetCategoryName(Document doc, BuiltInCategory bic, string fallback)
        {
            try
            {
                var cat = doc.Settings.Categories.get_Item(bic);
                return cat?.Name ?? fallback;
            }
            catch
            {
                return fallback;
            }
        }

        private void PickAndRenumberLive(Document doc, UIDocument uiDoc, View activeView, 
            string categoryName, BuiltInCategory bic, string startingNumber, int step)
        {
            var renumberedIds = new List<ElementId>();
            var successCount = 0;
            var failedCount = 0;
            var currentNumber = startingNumber;

            using (var tg = new TransactionGroup(doc, $"Renumber {categoryName}"))
            {
                tg.Start();

                try
                {
                    var selectionFilter = new CategorySelectionFilter(bic);

                    while (true)
                    {
                        // Pick element
                        Reference pickedRef;
                        try
                        {
                            pickedRef = uiDoc.Selection.PickObject(ObjectType.Element, selectionFilter,
                                $"Click {categoryName} to renumber as '{currentNumber}' (ESC to finish)");
                        }
                        catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                        {
                            break; // User pressed ESC
                        }

                        if (pickedRef == null)
                            break;

                        var element = doc.GetElement(pickedRef);
                        if (element == null)
                            continue;

                        // Renumber in its own transaction
                        using (var t = new Transaction(doc, $"Set {currentNumber}"))
                        {
                            try
                            {
                                t.Start();

                                if (SetNumber(element, currentNumber))
                                {
                                    MarkElementAsRenumbered(activeView, element);
                                    renumberedIds.Add(element.Id);
                                    successCount++;
                                    t.Commit();
                                }
                                else
                                {
                                    failedCount++;
                                    t.RollBack();
                                }
                            }
                            catch
                            {
                                failedCount++;
                                t.RollBack();
                            }
                        }

                        // Increment for next pick
                        currentNumber = IncrementString(currentNumber, step);
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
                using (var t = new Transaction(doc, $"Unmark {categoryName}"))
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

            TaskDialog.Show($"Renumber {categoryName}",
                $"Renumber complete.\n\nUpdated: {successCount}\nFailed: {failedCount}");
        }

        private Parameter GetNumberParam(Element element)
        {
            foreach (var paramName in NUMBER_PARAM_NAMES)
            {
                var param = element.LookupParameter(paramName);
                if (param != null)
                    return param;
            }

            return element.get_Parameter(BuiltInParameter.ALL_MODEL_MARK);
        }

        private bool SetNumber(Element element, string newNumber)
        {
            var param = GetNumberParam(element);
            if (param == null || param.IsReadOnly)
                return false;

            try
            {
                param.Set(newNumber);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void MarkElementAsRenumbered(View view, Element element)
        {
            try
            {
                var ogs = new OverrideGraphicSettings();
                ogs.SetHalftone(true);
                ogs.SetSurfaceTransparency(75);
                view.SetElementOverrides(element.Id, ogs);
            }
            catch
            {
                // Ignore failures
            }
        }

        private void UnmarkRenumberedElements(View view, List<ElementId> elementIds)
        {
            var ogs = new OverrideGraphicSettings();
            foreach (var id in elementIds)
            {
                try
                {
                    view.SetElementOverrides(id, ogs);
                }
                catch
                {
                    // Ignore failures
                }
            }
        }

        private string IncrementString(string value, int step)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            var result = value;
            for (int i = 0; i < step; i++)
            {
                var match = Regex.Match(result, @"(\d+)$");
                if (!match.Success)
                    return result;

                var numberText = match.Groups[1].Value;
                var prefix = result.Substring(0, match.Groups[1].Index);

                if (!int.TryParse(numberText, out int number))
                    return result;

                var nextNumber = number + 1;
                var width = numberText.Length;

                result = prefix + nextNumber.ToString().PadLeft(width, '0');
            }

            return result;
        }

        private class CategoryOption
        {
            public string Label { get; set; }
            public BuiltInCategory Category { get; set; }
        }

        private class CategorySelectionFilter : ISelectionFilter
        {
            private readonly BuiltInCategory _targetCategory;

            public CategorySelectionFilter(BuiltInCategory targetCategory)
            {
                _targetCategory = targetCategory;
            }

            public bool AllowElement(Element elem)
            {
                return elem?.Category?.Id.IntegerValue == (int)_targetCategory;
            }

            public bool AllowReference(Reference reference, XYZ position)
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Simple WPF input dialog for text input
    /// </summary>
    public class InputDialog : System.Windows.Window
    {
        private System.Windows.Controls.TextBox _textBox;

        public string InputText => _textBox.Text;

        public InputDialog(string title, string prompt, string defaultText = "")
        {
            Title = title;
            Width = 400;
            Height = 150;
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            ResizeMode = System.Windows.ResizeMode.NoResize;

            var grid = new System.Windows.Controls.Grid();
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });

            var stackPanel = new System.Windows.Controls.StackPanel { Margin = new System.Windows.Thickness(10) };
            
            var label = new System.Windows.Controls.Label { Content = prompt };
            stackPanel.Children.Add(label);

            _textBox = new System.Windows.Controls.TextBox { Text = defaultText, Margin = new System.Windows.Thickness(0, 5, 0, 0) };
            stackPanel.Children.Add(_textBox);

            System.Windows.Controls.Grid.SetRow(stackPanel, 0);
            grid.Children.Add(stackPanel);

            var buttonPanel = new System.Windows.Controls.StackPanel 
            { 
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                Margin = new System.Windows.Thickness(10)
            };

            var okButton = new System.Windows.Controls.Button 
            { 
                Content = "OK", 
                Width = 75, 
                Margin = new System.Windows.Thickness(0, 0, 5, 0),
                IsDefault = true
            };
            okButton.Click += (s, e) => { DialogResult = true; Close(); };
            buttonPanel.Children.Add(okButton);

            var cancelButton = new System.Windows.Controls.Button 
            { 
                Content = "Cancel", 
                Width = 75,
                IsCancel = true
            };
            cancelButton.Click += (s, e) => { DialogResult = false; Close(); };
            buttonPanel.Children.Add(cancelButton);

            System.Windows.Controls.Grid.SetRow(buttonPanel, 1);
            grid.Children.Add(buttonPanel);

            Content = grid;

            _textBox.Focus();
            _textBox.SelectAll();
        }
    }
}
