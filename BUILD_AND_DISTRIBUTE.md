# Building ISPG.Conversion Add-In for Distribution

This guide shows you how to build the ISPG.Conversion Revit add-in as a distributable package that others can install.

## Quick Start (5 minutes)

1. Extract the source code
2. Open in Visual Studio 2022
3. Build → Create installer folder
4. Share the installer folder with users

---

## Prerequisites

### On Your Build Machine (One-Time Setup)

You need:
- **Windows 10/11**
- **Visual Studio 2022** (Community Edition is free)
  - Download: https://visualstudio.microsoft.com/downloads/
  - During install, select: **.NET desktop development** workload
- **Revit 2025 or 2027** installed (for testing, not required for building)

### What Users Need

Users who will install your add-in need:
- **Windows 10/11**
- **Revit 2025 or 2027** (must match the version you build for)
- **Nothing else!** No Visual Studio, no .NET SDK, no pyRevit

---

## Step 1: Extract the Source Code

1. Download `ISPG.Conversion.Addin.tar.gz`
2. Extract using:
   - **7-Zip** (free): Right-click → 7-Zip → Extract Here (twice - once for .gz, once for .tar)
   - **Windows 11**: Right-click → Extract All
   - **WinRAR**: Right-click → Extract Here

You should see a folder: `ISPG.Conversion.Addin/`

---

## Step 2: Build the Add-In

### Option A: Visual Studio GUI (Recommended for first-time)

1. Open `ISPG.Conversion.sln` in Visual Studio 2022
2. At the top, select configuration:
   - **Release** (for Revit 2025)
   - **Revit2027** (for Revit 2027)
3. Click **Build → Build Solution** (or press F7)
4. Wait ~30 seconds for NuGet packages to download and build to complete
5. Look for "Build succeeded" in the Output window

**Output location:**
- Revit 2025: `ISPG.Conversion\bin\Release\net48\ISPG.Conversion.dll`
- Revit 2027: `ISPG.Conversion\bin\Revit2027\net48\ISPG.Conversion.dll`

### Option B: Command Line (Faster for repeat builds)

Open Command Prompt in the `ISPG.Conversion.Addin\` folder:

**For Revit 2025:**
```cmd
build.bat Release
```

**For Revit 2027:**
```cmd
build.bat Revit2027
```

---

## Step 3: Create Distribution Package

### Simple Installer (Recommended)

Create a folder structure that users can simply copy:

1. Create a new folder: `ISPG.Conversion-Installer-v1.0-Revit2025`

2. Copy these files into it:
   ```
   ISPG.Conversion-Installer-v1.0-Revit2025/
   ├── ISPG.Conversion.dll          (from bin\Release\net48\)
   ├── ISPG.Conversion.addin         (from project root)
   ├── install.bat                   (from project root)
   └── README.txt                    (create - see below)
   ```

3. Create a `README.txt`:
   ```
   ISPG.Conversion Add-In for Revit 2025
   Installation Instructions:

   1. Close Revit if it's open
   2. Double-click install.bat
   3. Press any key when prompted
   4. Open Revit
   5. Look for "ISPG" tab in the ribbon

   If you see errors, you may need to:
   - Right-click install.bat → "Run as Administrator"
   - Unblock the DLL: Right-click ISPG.Conversion.dll → Properties → 
     Check "Unblock" → OK

   For support, contact: [your contact info]
   ```

4. **Zip the folder** and share it

   Users extract and run `install.bat` - that's it!

---

## Step 4: Testing Before Distribution

### Test on Your Machine

1. Run `install.bat` from the project folder
2. Open Revit 2025 (or 2027 if you built for that)
3. Look for **ISPG** tab in the ribbon
4. Click each button to verify:
   - Unit Numbering
   - Unit Number QA/QC  
   - Unit Material
   - Export Units / Parking / Shell
   - Import Units / Parking / Shell

### Test on a Clean Machine (Recommended)

Before distributing widely:
1. Find a colleague's computer without the add-in
2. Give them your installer folder
3. Have them run `install.bat`
4. Verify it works in their Revit

---

## Advanced: Manual Installation (For Troubleshooting)

If users can't run `install.bat`, they can install manually:

**For Revit 2025:**
1. Copy `ISPG.Conversion.dll` to:
   ```
   C:\Users\[Username]\AppData\Roaming\Autodesk\Revit\Addins\2025\
   ```

2. Copy `ISPG.Conversion.addin` to the same folder

3. Right-click the DLL → Properties → Check "Unblock" if present → OK

**For Revit 2027:**
Same steps, but use folder: `...\Revit\Addins\2027\`

---

## Building for Both Revit Versions

To support both Revit 2025 AND 2027 users:

1. Build twice:
   ```cmd
   build.bat Release
   build.bat Revit2027
   ```

2. Create two separate installer folders:
   - `ISPG.Conversion-Installer-v1.0-Revit2025`
   - `ISPG.Conversion-Installer-v1.0-Revit2027`

3. Each contains:
   - The DLL from the appropriate build
   - The .addin file (same for both)
   - install.bat (same for both)
   - README.txt (updated with version number)

4. Distribute both, let users pick based on their Revit version

---

## Professional Distribution (Optional)

### Create an MSI Installer

For a polished installation experience:

1. Install **WiX Toolset**: https://wixtoolset.org/
2. Create a WiX project that:
   - Copies the DLL and .addin to the correct folder
   - Detects Revit version
   - Adds uninstall entry to Windows
   - Signs the installer with a code signing certificate

*This is overkill for internal distribution but good for public releases.*

### Code Signing (Optional)

To avoid Windows security warnings:

1. Get a code signing certificate (e.g., from DigiCert, Sectigo)
2. Sign the DLL:
   ```cmd
   signtool sign /f YourCertificate.pfx /p password /tr http://timestamp.digicert.com ISPG.Conversion.dll
   ```

*Only necessary for wide public distribution.*

---

## Distribution Checklist

Before sending to users:

- [ ] Built in **Release** configuration (not Debug)
- [ ] Tested on your machine
- [ ] Tested on at least one other machine
- [ ] Correct Revit version in folder/file names
- [ ] README.txt included with clear instructions
- [ ] DLL is unblocked (right-click → Properties → Unblock)
- [ ] Version number in installer folder name

---

## Troubleshooting for Users

### "Add-in doesn't appear in Revit"

1. Check Revit Addins folder:
   - Revit 2025: `%AppData%\Autodesk\Revit\Addins\2025\`
   - Both files present: `.dll` and `.addin`?

2. Open the `.addin` file in Notepad:
   - Does `<Assembly>` path match the DLL location?
   - Update if needed to: `ISPG.Conversion.dll` (same folder)

3. Check Revit → Add-Ins → Load/Unload:
   - Is "ISPG.Conversion" listed but not loaded?
   - Try loading manually

### "Security warning" or "Blocked DLL"

1. Right-click `ISPG.Conversion.dll`
2. Properties → Check "Unblock" → OK
3. Restart Revit

### "Add-in failed to load"

1. Check Revit version matches DLL build:
   - Revit 2025 needs the Release build
   - Revit 2027 needs the Revit2027 build
   - Wrong version → rebuild for correct version

2. Check `.addin` file `<AddInId>`:
   - Must be: `F8C6D928-9E4B-4B8A-8D3F-5E6C7D8E9F0A`

---

## Version Updates

When releasing updates:

1. Update version number in folder name: `v1.0` → `v1.1`
2. Consider updating `Application.cs` with version display
3. Document changes in a CHANGELOG.txt
4. Test thoroughly before distributing

---

## Support Resources

- **Project folder:** `~/hermes-sandbox/ISPG.Conversion.Addin/`
- **Developer guide:** `DEVELOPER_GUIDE.md`
- **Implementation details:** `IMPLEMENTATION_SUMMARY.md`

---

## Summary: Simplest Distribution Method

**For You (one-time):**
1. Open solution in Visual Studio
2. Build → Release
3. Create folder with DLL + .addin + install.bat + README
4. Zip it

**For Users:**
1. Extract zip
2. Run install.bat
3. Open Revit
4. Done!

**No installers, no complexity, just works.** ✅
