# ISPG Conversion Tools - Revit Add-in

Revit add-in for unit numbering, QA/QC, and data management workflows.

## Features

- **Unit Numbering** - Automated unit numbering system
- **Unit Numbering QA/QC** - Quality assurance checks
- **Unit Material Management** - Material assignment and tracking
- **Legacy Import/Export** - CSV-based import/export for units, parking, and shell elements

## Installation

### Option 1: Download Pre-built Installer (Easiest)

1. Go to [Releases](../../releases)
2. Download the latest `ISPG-Conversion-Installer.zip`
3. Extract the ZIP
4. Double-click `INSTALL.bat`
5. Launch Revit 2025

### Option 2: Build from Source

Requirements:
- Visual Studio 2022
- .NET Framework 4.8
- Revit 2025 installed

Steps:
1. Clone this repository
2. Open `ISPG.Conversion.sln` in Visual Studio
3. Build → Rebuild Solution
4. Run `install.bat` from the solution directory

## Compatibility

- Revit 2025
- .NET Framework 4.8
- Windows 10/11

## Development

This project uses GitHub Actions for automated builds. Every push to `main` triggers a build that produces a ready-to-install package.

To create a new release:
```bash
git tag v1.0.0
git push origin v1.0.0
```

This will automatically build and publish a GitHub release with the installer.

## License

Internal tool for Insite Property Group

## Support

For issues or questions, contact the development team.
