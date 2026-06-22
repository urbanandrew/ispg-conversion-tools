@echo off
REM Extract Revit 2027 API DLLs for GitHub Actions builds
REM Run this from the repository root

echo Extracting Revit 2027 API DLLs...

set REVIT_PATH=C:\Program Files\Autodesk\Revit 2027
set TARGET_DIR=lib\Revit2027

if not exist "%REVIT_PATH%" (
    echo ERROR: Revit 2027 not found at %REVIT_PATH%
    exit /b 1
)

if not exist "%TARGET_DIR%" mkdir "%TARGET_DIR%"

copy "%REVIT_PATH%\RevitAPI.dll" "%TARGET_DIR%\"
copy "%REVIT_PATH%\RevitAPIUI.dll" "%TARGET_DIR%\"
copy "%REVIT_PATH%\AdWindows.dll" "%TARGET_DIR%\"

echo Done! DLLs extracted to %TARGET_DIR%
