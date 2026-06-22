# Revit API DLLs

This folder contains Revit API DLLs needed for GitHub Actions to build the project without having Revit installed.

## Setup Instructions

Run `extract-revit-dlls.bat` from the project root to copy the required DLLs from your Revit installation:

```
extract-revit-dlls.bat
```

This will copy the following DLLs from `C:\Program Files\Autodesk\Revit 2025\`:
- RevitAPI.dll
- RevitAPIUI.dll  
- AdWindows.dll

## How It Works

The `.csproj` file first tries to find Revit installed locally at:
```
C:\Program Files\Autodesk\Revit 2025\
```

If not found (like on GitHub Actions), it falls back to:
```
lib/Revit2025/
```

This allows the same project to build:
- ✅ Locally (with Revit installed)
- ✅ On GitHub Actions (using DLLs from this folder)

## License Note

These DLLs are **redistributed for build purposes only** under the Autodesk Developer Network terms. They are NOT included in the distributed installer - users must have Revit installed to run the add-in.
