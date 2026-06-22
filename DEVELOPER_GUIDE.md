# ISPG Conversion Tools - Developer Guide

## Project Structure

```
ISPG.Conversion.Addin/
├── ISPG.Conversion/           # Main C# project
│   ├── Application.cs          # Ribbon initialization
│   ├── Commands/               # All command implementations
│   │   ├── UnitNumberingCommand.cs
│   │   ├── UnitNumberingQAQCCommand.cs
│   │   ├── UnitMaterialCommand.cs
│   │   ├── ImportUnits2027Command.cs
│   │   ├── ImportParking2027Command.cs
│   │   ├── ImportShell2027Command.cs
│   │   ├── ExportUnitsLegacyCommand.cs
│   │   ├── ExportParkingLegacyCommand.cs
│   │   └── ExportShellLegacyCommand.cs
│   ├── Helpers/                # Shared utilities
│   │   ├── NumberingHelper.cs  # Numbering logic
│   │   └── UIHelper.cs         # Dialog helpers
│   ├── Resources/              # Icons (optional)
│   └── ISPG.Conversion.csproj  # Project file
├── ISPG.Conversion.addin      # Revit manifest
├── ISPG.Conversion.sln        # Visual Studio solution
├── build.bat                   # Build script
├── install.bat                 # Installation script
└── README.md                   # User documentation
```

## Build Requirements

### Software
- **Visual Studio 2022** (Community, Professional, or Enterprise)
  - Or **Visual Studio Build Tools 2022**
- **.NET Framework 4.8 SDK**
- **Revit 2025 and/or 2027** (for testing)

### Revit SDK
The Revit API DLLs are referenced from the standard Revit installation paths:
- `C:\Program Files\Autodesk\Revit 2025\RevitAPI.dll`
- `C:\Program Files\Autodesk\Revit 2025\RevitAPIUI.dll`

## Building

### Option 1: Using Visual Studio
1. Open `ISPG.Conversion.sln` in Visual Studio 2022
2. Select configuration:
   - **Release** for Revit 2025
   - **Revit2027** for Revit 2027
3. Build → Build Solution (Ctrl+Shift+B)

### Option 2: Using Command Line
1. Open Command Prompt in the project root
2. Run `build.bat`
3. This will build for both Revit 2025 and 2027

## Installation

### Automated Installation
1. Build the project first
2. Run `install.bat`
3. Choose which Revit version(s) to install for
4. Restart Revit

### Manual Installation
1. Copy `ISPG.Conversion.dll` to:
   - Revit 2025: `%APPDATA%\Autodesk\Revit\Addins\2025\`
   - Revit 2027: `%APPDATA%\Autodesk\Revit\Addins\2027\`
   
2. Copy `ISPG.Conversion.addin` to the same directory

3. Restart Revit

## Development Status

### Fully Implemented ✅
- **Unit Numbering** - Interactive element renumbering with live visual feedback
- **Unit Numbering QA/QC** - Validation for duplicates, missing, and skipped numbers

### Placeholder (TODO) ⚠️
- **Unit Material** - Material assignment to elements
- **Import Units 2027** - Import units from external data
- **Import Parking 2027** - Import parking spaces
- **Import Shell 2027** - Import shell/building data
- **Export Units Legacy** - Export units to legacy format
- **Export Parking Legacy** - Export parking to legacy format
- **Export Shell Legacy** - Export shell to legacy format

## Adding New Functionality

### To Implement a Placeholder Command

1. Open the command file (e.g., `Commands/UnitMaterialCommand.cs`)

2. Replace the placeholder with your implementation:

```csharp
public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
{
    UIDocument uidoc = commandData.Application.ActiveUIDocument;
    Document doc = uidoc.Document;

    try
    {
        // Your implementation here
        
        using (Transaction trans = new Transaction(doc, "Your Transaction Name"))
        {
            trans.Start();
            
            // Modify the document
            
            trans.Commit();
        }

        return Result.Succeeded;
    }
    catch (Exception ex)
    {
        message = ex.ToString();
        return Result.Failed;
    }
}
```

3. Use the helper classes:
   - `NumberingHelper` for element numbering operations
   - `UIHelper` for dialogs and user input

### To Add a New Command

1. Create a new file in `Commands/` folder:

```csharp
using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ISPG.Conversion.Helpers;

namespace ISPG.Conversion.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class MyNewCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Implementation
            return Result.Succeeded;
        }
    }
}
```

2. Register it in `Application.cs`:

```csharp
AddPushButton(
    conversionPanel,
    "MyNewButton",
    "Button\nText",
    "ISPG.Conversion.Commands.MyNewCommand",
    assemblyPath,
    "Tooltip description",
    "icon_name.png"
);
```

3. Rebuild and reinstall

## Common Patterns

### Get/Set Element Parameters
```csharp
// Get parameter
string number = NumberingHelper.GetNumber(element);

// Set parameter
bool success = NumberingHelper.SetNumber(element, "101");
```

### User Dialogs
```csharp
// Simple alert
UIHelper.Alert("Message", "Title");

// Ask for text input
string input = UIHelper.AskForString("Title", "Prompt", "default");

// Ask for selection
var options = new List<OptionItem<string>>
{
    new OptionItem<string>("Option 1", "value1"),
    new OptionItem<string>("Option 2", "value2")
};
string selected = UIHelper.AskForOption("Title", "Message", options);
```

### Element Collection
```csharp
FilteredElementCollector collector = new FilteredElementCollector(doc)
    .OfCategory(BuiltInCategory.OST_GenericModel)
    .WhereElementIsNotElementType();

foreach (Element elem in collector)
{
    // Process elements
}
```

## Porting from pyRevit

When converting Python scripts to C#:

1. **Document/UIDocument access:**
   - Python: `doc = __revit__.ActiveUIDocument.Document`
   - C#: `Document doc = commandData.Application.ActiveUIDocument.Document`

2. **Element ID values:**
   - Python: `element_id.IntegerValue` or `element_id.Value`
   - C#: Use `NumberingHelper.GetElementIdValue(elementId)`

3. **Parameters:**
   - Python: `element.LookupParameter("Name").AsString()`
   - C#: Same syntax works!

4. **Transactions:**
   - Python: `Transaction(doc, "Name")`, then `Start()`, `Commit()`, `RollBack()`
   - C#: Same, but use `using` statements for automatic disposal

5. **UI:**
   - Python: `System.Windows.Forms` imports
   - C#: Use `UIHelper` class methods

## Troubleshooting

### Add-in doesn't load in Revit
- Check Revit version matches the build
- Look at Revit's error log: `%APPDATA%\Autodesk\Revit\Addins\<version>\`
- Make sure Revit was closed when you installed

### Build errors
- Verify Revit installation paths in `.csproj`
- Make sure .NET Framework 4.8 is installed
- Check that Visual Studio has C# desktop development workload

### Icons not showing
- Icons are optional
- The add-in will work without them
- To add icons: place 32x32 PNG files in `Resources/` folder

## Next Steps

To complete this add-in:

1. **Implement Unit Material command** - Read the pyRevit script and port the material assignment logic

2. **Implement Import/Export commands** - These read/write CSV files with unit data
   - Study the pyRevit versions
   - Add CSV reading/writing using `System.IO`
   - Port the parameter mapping logic

3. **Add icons** - Create 32x32 PNG icons for better UX

4. **Create installer EXE** - Use WiX Toolset or NSIS for a proper installer

5. **Add error logging** - Use a logging framework for better diagnostics

## Support

For questions or issues:
- Review the pyRevit source scripts in `~/.hermes/cache/documents/ISPG.Conversion.extension/`
- Check Autodesk Revit API documentation
- Contact the development team

## License

Internal tool for Insite Property Group
