@echo off
REM Build script for ISPG Conversion Add-in
REM Requires: .NET SDK 4.8 and Visual Studio Build Tools

echo ================================================
echo ISPG Conversion Tools - Build Script
echo ================================================
echo.

REM Try to find MSBuild
set "MSBUILD="

REM Try Visual Studio 2022
if exist "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" (
    set "MSBUILD=C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
)

if exist "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe" (
    set "MSBUILD=C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"
)

if exist "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe" (
    set "MSBUILD=C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
)

if "%MSBUILD%"=="" (
    echo ERROR: MSBuild not found!
    echo Please install Visual Studio 2022 or Visual Studio Build Tools
    pause
    exit /b 1
)

echo Found MSBuild: %MSBUILD%
echo.

REM Build for Revit 2025
echo Building for Revit 2025...
"%MSBUILD%" ISPG.Conversion\ISPG.Conversion.csproj /p:Configuration=Release /p:Platform=x64 /t:Rebuild /v:minimal
if errorlevel 1 (
    echo Build failed for Revit 2025
    pause
    exit /b 1
)
echo Revit 2025 build complete
echo.

REM Build for Revit 2027
echo Building for Revit 2027...
"%MSBUILD%" ISPG.Conversion\ISPG.Conversion.csproj /p:Configuration=Revit2027 /p:Platform=x64 /t:Rebuild /v:minimal
if errorlevel 1 (
    echo Build failed for Revit 2027
    pause
    exit /b 1
)
echo Revit 2027 build complete
echo.

echo ================================================
echo Build complete!
echo ================================================
echo.
echo Output files:
echo   - ISPG.Conversion\bin\x64\Release\ISPG.Conversion.dll (Revit 2025)
echo   - ISPG.Conversion\bin\x64\Revit2027\ISPG.Conversion.dll (Revit 2027)
echo.

pause
