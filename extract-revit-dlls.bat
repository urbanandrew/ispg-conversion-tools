@echo off
REM Copy Revit API DLLs to lib folder for GitHub Actions

echo ================================================================
echo Revit API DLL Extractor for GitHub Actions
echo ================================================================
echo.

set REVIT_2025="C:\Program Files\Autodesk\Revit 2025"

if not exist %REVIT_2025% (
    echo ERROR: Revit 2025 not found at %REVIT_2025%
    echo.
    pause
    exit /b 1
)

echo Found Revit 2025!
echo.
echo Copying required DLLs...

if not exist "lib\Revit2025" mkdir "lib\Revit2025"

copy /Y "%REVIT_2025%\RevitAPI.dll" "lib\Revit2025\"
copy /Y "%REVIT_2025%\RevitAPIUI.dll" "lib\Revit2025\"
copy /Y "%REVIT_2025%\AdWindows.dll" "lib\Revit2025\"

echo.
echo ================================================================
echo DLLs copied successfully!
echo ================================================================
echo.
echo Next step: Commit and push these files to GitHub
echo.
echo Run these commands:
echo   git add lib/
echo   git commit -m "Add Revit 2025 API DLLs for GitHub Actions"
echo   git push
echo.
pause
