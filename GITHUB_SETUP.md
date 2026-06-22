## GitHub Setup Instructions

I've prepared everything - you just need to create the GitHub repo and push.

### Step 1: Create the GitHub Repo

Go to: https://github.com/new

Fill in:
- **Repository name:** `ispg-conversion-tools`
- **Description:** `Revit add-in for unit numbering, QA/QC, and data management`
- **Visibility:** Public
- **DON'T** initialize with README, .gitignore, or license (we already have these)

Click "Create repository"

### Step 2: Push the Code

Copy and run these commands in your terminal (Git Bash or PowerShell):

```bash
cd ~/hermes-sandbox/ISPG.Conversion.Addin
git remote add origin https://github.com/urbanandrew/ispg-conversion-tools.git
git push -u origin main
```

### Step 3: Wait for the Build

1. Go to your repo: https://github.com/urbanandrew/ispg-conversion-tools
2. Click the "Actions" tab
3. Watch the build run (takes ~2-3 minutes)
4. When it's done, go to the "Actions" tab → click the workflow run → scroll down to "Artifacts"
5. Download `ISPG-Conversion-Installer.zip`

### Step 4: Install

1. Extract the ZIP
2. Double-click `INSTALL.bat`
3. Launch Revit 2025
4. Look for "ISPG Conversion Tools" in the ribbon

---

## For Future Releases

To create a new release (optional):

```bash
git tag v1.0.0
git push origin v1.0.0
```

This will automatically:
- Build the add-in
- Create a GitHub Release
- Attach the installer ZIP to the release

You can then share the Release link with others for easy downloads.

---

## What GitHub Actions Will Do

Every time you push code:
1. Spin up a Windows build server
2. Install MSBuild and .NET Framework 4.8
3. Compile your Revit add-in
4. Create an installer package with:
   - ISPG.Conversion.dll
   - ISPG.Conversion.addin
   - INSTALL.bat (one-click installer)
   - README.txt
5. Package it all as a ZIP
6. Upload as a downloadable artifact (or Release if you tagged it)

**You'll never need to build locally again** - just push your code and download the installer from GitHub.
