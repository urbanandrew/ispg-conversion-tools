@echo off
REM Installer script for ISPG Conversion Add-in
REM This will copy the built DLL and .addin file to the Revit addins folder

echo ================================================
echo ISPG Conversion Tools - Installer
echo ================================================
echo.

REM Check if running as administrator
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo WARNING: This script may need administrator privileges
    echo If installation fails, try running as administrator
    echo.
)

REM Ask which Revit version to install for
echo Which Revit version do you want to install for?
echo.
echo 1. Revit 2025
echo 2. Revit 2027
echo 3. Both
echo.
set /p choice="Enter choice (1-3): "

set INSTALL_2025=0
set INSTALL_2027=0

if "%choice%"=="1" set INSTALL_2025=1
if "%choice%"=="2" set INSTALL_2027=1
if "%choice%"=="3" (
    set INSTALL_2025=1
    set INSTALL_2027=1
)

if %INSTALL_2025%==0 if %INSTALL_2027%==0 (
    echo Invalid choice
    pause
    exit /b 1
)

echo.

REM Install for Revit 2025
if %INSTALL_2025%==1 (
    echo Installing for Revit 2025...
    
    set "ADDIN_DIR=%APPDATA%\Autodesk\Revit\Addins\2025"
    set "DLL_SOURCE=ISPG.Conversion\bin\x64\Release\ISPG.Conversion.dll"
    
    if not exist "%DLL_SOURCE%" (
        echo ERROR: DLL not found at %DLL_SOURCE%
        echo Please build the project first using build.bat
        pause
        exit /b 1
    )
    
    if not exist "%ADDIN_DIR%" (
        echo Creating directory: %ADDIN_DIR%
        mkdir "%ADDIN_DIR%"
    )
    
    echo Copying DLL...
    copy /Y "%DLL_SOURCE%" "%ADDIN_DIR%\ISPG.Conversion.dll"
    
    echo Creating .addin manifest...
    copy /Y "ISPG.Conversion.addin" "%ADDIN_DIR%\ISPG.Conversion.addin"
    
    echo Revit 2025 installation complete
    echo.
)

REM Install for Revit 2027
if %INSTALL_2027%==1 (
    echo Installing for Revit 2027...
    
    set "ADDIN_DIR=%APPDATA%\Autodesk\Revit\Addins\2027"
    set "DLL_SOURCE=ISPG.Conversion\bin\x64\Revit2027\ISPG.Conversion.dll"
    
    if not exist "%DLL_SOURCE%" (
        echo ERROR: DLL not found at %DLL_SOURCE%
        echo Please build the project first using build.bat
        pause
        exit /b 1
    )
    
    if not exist "%ADDIN_DIR%" (
        echo Creating directory: %ADDIN_DIR%
        mkdir "%ADDIN_DIR%"
    )
    
    echo Copying DLL...
    copy /Y "%DLL_SOURCE%" "%ADDIN_DIR%\ISPG.Conversion.dll"
    
    echo Creating .addin manifest...
    copy /Y "ISPG.Conversion.addin" "%ADDIN_DIR%\ISPG.Conversion.addin"
    
    echo Revit 2027 installation complete
    echo.
)

echo ================================================
echo Installation complete!
echo ================================================
echo.
echo The ISPG Conversion Tools add-in has been installed.
echo.
echo Next steps:
echo 1. Launch Revit
echo 2. Look for the "ISPG" tab in the ribbon
echo 3. You should see all the conversion tools
echo.
echo If you don't see the tab:
echo - Check the Revit error log
echo - Make sure Revit is closed before installing
echo - Try running this script as administrator
echo.

pause
