using System;
using System.Reflection;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;

namespace ISPG.Conversion
{
    /// <summary>
    /// Main application entry point - creates ribbon tab and buttons
    /// </summary>
    public class Application : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication app)
        {
            try
            {
                CreateRibbonTab(app);
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("ISPG Conversion Startup Error", ex.ToString());
                return Result.Failed;
            }
        }

        public Result OnShutdown(UIControlledApplication app)
        {
            return Result.Succeeded;
        }

        private void CreateRibbonTab(UIControlledApplication app)
        {
            string tabName = "ISPG";
            
            // Create tab (ignore error if already exists)
            try
            {
                app.CreateRibbonTab(tabName);
            }
            catch (Exception ex)
            {
                // Tab already exists - this is fine
                System.Diagnostics.Debug.WriteLine($"Tab creation: {ex.Message}");
            }

            // Create panel
            RibbonPanel conversionPanel = app.CreateRibbonPanel(tabName, "Conversion");

            string assemblyPath = Assembly.GetExecutingAssembly().Location;

            // Unit Numbering button
            AddPushButton(
                conversionPanel,
                "UnitNumbering",
                "Unit\nNumbering",
                "ISPG.Conversion.Commands.UnitNumberingCommand",
                assemblyPath,
                "Interactive renumbering of Generic Models and Parking elements",
                "icon_unit_numbering.png"
            );

            // Unit Numbering QA/QC button
            AddPushButton(
                conversionPanel,
                "UnitNumberingQAQC",
                "Unit Number\nQA/QC",
                "ISPG.Conversion.Commands.UnitNumberingQAQCCommand",
                assemblyPath,
                "Validate unit numbering for duplicates and gaps",
                "icon_unit_qaqc.png"
            );

            // Unit Material button
            AddPushButton(
                conversionPanel,
                "UnitMaterial",
                "Unit\nMaterial",
                "ISPG.Conversion.Commands.UnitMaterialCommand",
                assemblyPath,
                "Assign materials to unit elements",
                "icon_unit_material.png"
            );

            conversionPanel.AddSeparator();

            // Export buttons (NEW JSON-based)
            AddPushButton(
                conversionPanel,
                "ExportUnits",
                "Export\nUnits",
                "ISPG.Conversion.Commands.Export.ExportUnitsCommand",
                assemblyPath,
                "Export units to JSON",
                "icon_export_units.png"
            );

            AddPushButton(
                conversionPanel,
                "ExportParking",
                "Export\nParking",
                "ISPG.Conversion.Commands.Export.ExportParkingCommand",
                assemblyPath,
                "Export parking to JSON",
                "icon_export_parking.png"
            );

            AddPushButton(
                conversionPanel,
                "ExportShell",
                "Export\nShell",
                "ISPG.Conversion.Commands.Export.ExportShellCommand",
                assemblyPath,
                "Export shell data to JSON",
                "icon_export_shell.png"
            );

            conversionPanel.AddSeparator();

            // Import buttons (NEW JSON-based)
            AddPushButton(
                conversionPanel,
                "ImportUnits",
                "Import\nUnits",
                "ISPG.Conversion.Commands.Import.ImportUnitsCommand",
                assemblyPath,
                "Import units from JSON export",
                "icon_import_units.png"
            );

            AddPushButton(
                conversionPanel,
                "ImportParking",
                "Import\nParking",
                "ISPG.Conversion.Commands.Import.ImportParkingCommand",
                assemblyPath,
                "Import parking from JSON export",
                "icon_import_parking.png"
            );

            AddPushButton(
                conversionPanel,
                "ImportShell",
                "Import\nShell",
                "ISPG.Conversion.Commands.Import.ImportShellCommand",
                assemblyPath,
                "Import shell data from JSON export",
                "icon_import_shell.png"
            );
        }

        private void AddPushButton(
            RibbonPanel panel,
            string name,
            string text,
            string className,
            string assemblyPath,
            string tooltip,
            string iconName)
        {
            PushButtonData buttonData = new PushButtonData(
                name,
                text,
                assemblyPath,
                className
            );

            buttonData.ToolTip = tooltip;

            // Try to load icon from embedded resources
            try
            {
                buttonData.LargeImage = GetEmbeddedImage(iconName);
            }
            catch
            {
                // Icon loading failed, button will show without icon
            }

            panel.AddItem(buttonData);
        }

        private BitmapImage GetEmbeddedImage(string imageName)
        {
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                string resourceName = $"ISPG.Conversion.Resources.{imageName}";
                
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        BitmapImage image = new BitmapImage();
                        image.BeginInit();
                        image.StreamSource = stream;
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        image.EndInit();
                        return image;
                    }
                }
            }
            catch { }

            return null;
        }
    }
}
