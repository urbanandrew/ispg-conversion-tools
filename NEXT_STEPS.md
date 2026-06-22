# Next Steps to Complete Revit 2027 Support

## 1. Extract Revit 2027 DLLs

Run this command in the repository root:
```cmd
extract-revit-dlls-2027.bat
```

This will copy three DLLs from your Revit 2027 installation to `lib/Revit2027/`:
- RevitAPI.dll (~12.9 MB)
- RevitAPIUI.dll (~2.0 MB)  
- AdWindows.dll (~1.4 MB)

## 2. Commit and Push the DLLs

```cmd
git add lib/Revit2027/
git commit -m "Add Revit 2027 API DLLs"
git push
```

## 3. GitHub Actions Will Build Both Versions

Once pushed, GitHub Actions will automatically:
- Build for Revit 2025 (Release configuration)
- Build for Revit 2027 (Revit2027 configuration)
- Create an installer that supports both versions

## 4. Test the New Installer

After the build completes:
1. Download the new `ISPG-Conversion-Installer.zip` from GitHub Actions
2. Extract it
3. Run `INSTALL.bat`
4. It will auto-detect and install for both Revit 2025 and 2027

## 5. Troubleshooting the Missing Toolbar Issue

If the ISPG tab still doesn't appear in Revit 2025:

1. **Check Revit's Add-In Manager:**
   - In Revit, go to: **Add-Ins tab > External Tools > Add-In Manager**
   - Look for "ISPG Conversion Tools" in the list
   - Check for any error messages

2. **Verify Files Are Installed:**
   - Press `Win+R`, type `%APPDATA%\Autodesk\Revit\Addins\2025` and press Enter
   - You should see:
     - `ISPG.Conversion.dll`
     - `ISPG.Conversion.addin`

3. **Check Revit Journal File (Advanced):**
   - Revit logs all add-in loading errors to journal files
   - Location: `%LOCALAPPDATA%\Autodesk\Revit\Autodesk Revit 2025\Journals\`
   - Open the most recent `.txt` file
   - Search for "ISPG" or "Conversion" to see loading errors

4. **Manual Test:**
   - Open the `.addin` file in Notepad
   - Verify it points to `ISPG.Conversion.dll` (not a full path, just the filename)
   - Both files should be in the same directory

---

**Current Status:**
- ✅ Code updated with Revit 2027 support
- ✅ Workflow updated to build both versions
- ✅ Installer updated to auto-detect both versions
- ✅ Better error logging added to Application.cs
- ⏳ Waiting for Revit 2027 DLLs to be extracted and committed
