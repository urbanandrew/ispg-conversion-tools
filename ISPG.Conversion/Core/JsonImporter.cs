using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Newtonsoft.Json;
using ISPG.Conversion.Models;

namespace ISPG.Conversion.Core
{
    /// <summary>
    /// Imports units/parking/shell from JSON files matching pyRevit schema version 6
    /// Handles origin correction, level matching, and parameter mapping
    /// </summary>
    public class JsonImporter
    {
        private readonly Document _doc;
        private readonly string _targetFamilyName;
        private readonly string _targetTypeName;
        private readonly Dictionary<string, string> _customLevelMap;
        private readonly bool _dryRun;
        private readonly int? _maxImport;

        // Constants matching pyRevit
        private const string LEGACY_SOURCE_ORIGIN = "center_of_width_and_depth";
        private const string TARGET_ORIGIN = "front_left_corner";
        private const double CENTER_TO_FRONT_LEFT_WIDTH_FACTOR = -0.5;
        private const double CENTER_TO_FRONT_LEFT_DEPTH_FACTOR = -0.5;

        // Parameter mapping (target family parameter names)
        private static readonly Dictionary<string, string> TARGET_PARAM_MAP = new Dictionary<string, string>
        {
            { "building_number", "UX_Info_Building_Number" },
            { "unit_number", "UX_Info_Unit_Number" },
            { "width", "UX_Room_Width" },
            { "depth", "UX_Room_Depth" },
            { "height", "UX_Room_Height" },
            { "climate", "UX_Info_Unit_CC" },
            { "climate_heat_only", "Info Climate Controlled (Heat Only)" },
            { "driveup", "UX_Info_Unit_DriveUp" },
            { "locker", "UX_Info_Locker" },
            { "ground_access", "UX_Info_Unit_GroundAccess" },
            { "accessible", "Info Accessible" },
            { "obstructions", "Info Obstructions" },
            { "offline", "Info Offline" },
            { "portable", "Info Portable" },
            { "stack_bottom", "Info StackBottom" },
            { "stack_top", "Info StackTop" },
            { "walkup", "Info WalkUp" }
        };

        public JsonImporter(Document doc, string targetFamilyName, string targetTypeName,
            Dictionary<string, string> customLevelMap = null, bool dryRun = false, int? maxImport = null)
        {
            _doc = doc;
            _targetFamilyName = targetFamilyName;
            _targetTypeName = targetTypeName;
            _customLevelMap = customLevelMap ?? new Dictionary<string, string>();
            _dryRun = dryRun;
            _maxImport = maxImport;
        }

        /// <summary>
        /// Import from JSON file
        /// Returns (imported count, skipped count, error messages)
        /// </summary>
        public (int imported, int skipped, List<string> errors) Import(string jsonPath)
        {
            if (!File.Exists(jsonPath))
                throw new FileNotFoundException($"JSON file not found: {jsonPath}");

            // Read and parse JSON
            var json = File.ReadAllText(jsonPath);
            var payload = JsonConvert.DeserializeObject<ExportPayload>(json);

            if (payload == null)
                throw new Exception("Invalid JSON payload");

            // Normalize: Units export uses "units"; Shells uses "shells"; Parking uses "records".
            // Coalesce whichever the JSON file provided into the Units collection.
            if ((payload.Units == null || payload.Units.Count == 0) && payload.Shells != null && payload.Shells.Count > 0)
                payload.Units = payload.Shells;
            if ((payload.Units == null || payload.Units.Count == 0) && payload.Records != null && payload.Records.Count > 0)
                payload.Units = payload.Records;

            if (payload.Units == null || payload.Units.Count == 0)
                throw new Exception("Invalid JSON payload: no units/shells/records found");

            // Normalize per-record schema variations:
            //  - Unit/Shell exports put placement coords FLAT and level_name at record root.
            //  - Parking export uses the nested placement.location.point shape (no normalization needed).
            // After this loop every record exposes the nested shape the rest of the importer expects.
            foreach (var record in payload.Units)
            {
                if (record == null) continue;

                if (record.Placement == null)
                    record.Placement = new PlacementInfo();

                record.Placement.NormalizeLocation();

                if (string.IsNullOrEmpty(record.Placement.LevelName) && !string.IsNullOrEmpty(record.LevelNameTop))
                    record.Placement.LevelName = record.LevelNameTop;

                if (!record.Placement.LevelId.HasValue && record.LevelIdTop.HasValue)
                    record.Placement.LevelId = record.LevelIdTop;

                if (record.Placement.LevelOffset == null && record.LevelOffsetTop != null)
                    record.Placement.LevelOffset = record.LevelOffsetTop;

                if (!record.Placement.Mirrored.HasValue && record.MirroredTop.HasValue)
                    record.Placement.Mirrored = record.MirroredTop;
                if (!record.Placement.HandFlipped.HasValue && record.HandFlippedTop.HasValue)
                    record.Placement.HandFlipped = record.HandFlippedTop;
                if (!record.Placement.FacingFlipped.HasValue && record.FacingFlippedTop.HasValue)
                    record.Placement.FacingFlipped = record.FacingFlippedTop;

                if (string.IsNullOrEmpty(record.Placement.DesignOption) && !string.IsNullOrEmpty(record.DesignOptionTop))
                    record.Placement.DesignOption = record.DesignOptionTop;
                if (string.IsNullOrEmpty(record.Placement.Workset) && !string.IsNullOrEmpty(record.WorksetTop))
                    record.Placement.Workset = record.WorksetTop;

                if (record.Placement.BoundingBox == null && record.BoundingBoxTop != null)
                    record.Placement.BoundingBox = record.BoundingBoxTop;
            }

            // Find target symbol
            var symbol = FindFamilySymbol(_targetFamilyName, _targetTypeName);
            if (symbol == null)
                throw new Exception($"Target family/type not found: {_targetFamilyName} : {_targetTypeName}");

            // Activate symbol if needed
            if (!symbol.IsActive)
            {
                symbol.Activate();
                _doc.Regenerate();
            }

            // Filter and limit units
            var units = payload.Units;
            if (_maxImport.HasValue && units.Count > _maxImport.Value)
                units = units.Take(_maxImport.Value).ToList();

            // Import units
            int importedCount = 0;
            int skippedCount = 0;
            var errors = new List<string>();

            foreach (var unit in units)
            {
                try
                {
                    if (_dryRun)
                    {
                        // Dry run: just validate
                        ValidateUnit(unit);
                        importedCount++;
                    }
                    else
                    {
                        // Real import
                        ImportUnit(unit, symbol);
                        importedCount++;
                    }
                }
                catch (Exception ex)
                {
                    skippedCount++;
                    var unitLabel = GetUnitLabel(unit);
                    errors.Add($"{unitLabel}: {ex.Message}");
                }
            }

            return (importedCount, skippedCount, errors);
        }

        /// <summary>
        /// Import a single unit record
        /// </summary>
        private FamilyInstance ImportUnit(UnitRecord unit, FamilySymbol symbol)
        {
            // Get corrected insertion point
            var point = GetCorrectedInsertPoint(unit);
            if (point == null)
                throw new Exception("Could not determine insertion point");

            // Get rotation
            double rotationRadians = GetRotation(unit);

            // Find level
            string levelName = unit.Placement?.LevelName;
            var level = LevelHelper.FindLevel(_doc, levelName, _customLevelMap);

            if (level == null)
            {
                // Get available levels for diagnostics
                var availableLevels = new FilteredElementCollector(_doc)
                    .OfClass(typeof(Level))
                    .Cast<Level>()
                    .Select(l => l.Name)
                    .OrderBy(n => n)
                    .Take(5);
                
                string availableList = string.Join(", ", availableLevels);
                throw new Exception($"Could not match level '{levelName}'. Available: {availableList}");
            }

            // Get import offset (handles lockers specially)
            double offset = GetImportOffset(unit);

            // Place at level elevation, then apply offset
            var placePoint = new XYZ(point.X, point.Y, level.Elevation);

            var instance = _doc.Create.NewFamilyInstance(
                placePoint,
                symbol,
                level,
                StructuralType.NonStructural
            );

            _doc.Regenerate();

            // Set offset parameters
            SetOffsetParams(instance, offset);

            _doc.Regenerate();

            // Set rotation
            if (Math.Abs(rotationRadians) > 0.0000001)
            {
                RotateInstance(instance, rotationRadians);
                _doc.Regenerate();
            }

            // Set unit parameters
            SetUnitParams(instance, unit);

            return instance;
        }

        /// <summary>
        /// Get corrected insertion point from unit record
        /// Handles origin conversion from legacy center to front-left
        /// </summary>
        private XYZ GetCorrectedInsertPoint(UnitRecord unit)
        {
            var sourcePoint = GetPoint(unit);
            if (sourcePoint == null) return null;

            string sourceOrigin = GetSourceOrigin(unit);
            string targetOrigin = GetTargetOrigin(unit);

            // No correction needed if origins match
            if (sourceOrigin == targetOrigin) return sourcePoint;

            // UX5 units already export at the target insertion point
            if (sourceOrigin == "front_left_corner" && targetOrigin == "front_left_corner")
                return sourcePoint;

            // Get dimensions for correction
            var width = GetLengthFeet(unit, "width");
            var depth = GetLengthFeet(unit, "depth");

            if (!width.HasValue || !depth.HasValue) return sourcePoint;

            // Apply origin correction
            if (sourceOrigin == "center_of_width_and_depth" && targetOrigin == "front_left_corner")
            {
                double rotation = GetRotation(unit);

                var offset = RotatedXYVector(
                    width.Value * CENTER_TO_FRONT_LEFT_WIDTH_FACTOR,
                    depth.Value * CENTER_TO_FRONT_LEFT_DEPTH_FACTOR,
                    rotation
                );

                return sourcePoint + offset;
            }

            // Unknown conversion: keep exported point
            return sourcePoint;
        }

        /// <summary>
        /// Get XYZ point from unit record
        /// </summary>
        private XYZ GetPoint(UnitRecord unit)
        {
            var pointData = unit.Placement?.Location?.Point;
            if (pointData == null) return null;

            try
            {
                return new XYZ(pointData.XFeet, pointData.YFeet, pointData.ZFeet);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get rotation in radians from unit record
        /// </summary>
        private double GetRotation(UnitRecord unit)
        {
            var rotation = unit.Placement?.Location?.RotationRadians;
            return rotation ?? 0.0;
        }

        /// <summary>
        /// Rotate XY vector by angle
        /// </summary>
        private XYZ RotatedXYVector(double x, double y, double angleRadians)
        {
            double ca = Math.Cos(angleRadians);
            double sa = Math.Sin(angleRadians);

            return new XYZ(
                x * ca - y * sa,
                x * sa + y * ca,
                0.0
            );
        }

        /// <summary>
        /// Get source origin from unit record
        /// </summary>
        private string GetSourceOrigin(UnitRecord unit)
        {
            // Try explicit source origin fields (in priority order)
            var explicitOrigin = unit.Source?.SourceOrigin 
                ?? unit.MigrationAssumptions?.SourceOrigin 
                ?? unit.MigrationAssumptions?.OldOrigin;

            if (!string.IsNullOrEmpty(explicitOrigin))
                return NormalizeOrigin(explicitOrigin);

            // Infer from family/type name
            string familyName = unit.Source?.FamilyName ?? "";
            string typeName = unit.Source?.TypeName ?? "";
            string combined = (familyName + " " + typeName).ToLower();

            if (combined.Contains("ux5_unit"))
                return "front_left_corner";

            return LEGACY_SOURCE_ORIGIN;
        }

        /// <summary>
        /// Get target origin from unit record
        /// </summary>
        private string GetTargetOrigin(UnitRecord unit)
        {
            var explicitOrigin = unit.MigrationAssumptions?.TargetOrigin;
            if (!string.IsNullOrEmpty(explicitOrigin))
                return NormalizeOrigin(explicitOrigin);

            return TARGET_ORIGIN;
        }

        /// <summary>
        /// Normalize origin string
        /// </summary>
        private string NormalizeOrigin(string origin)
        {
            if (string.IsNullOrWhiteSpace(origin)) return null;

            var normalized = origin.ToLower().Replace("_", "").Replace("-", "").Trim();

            if (new[] { "frontleftcorner", "frontleft", "fl" }.Contains(normalized))
                return "front_left_corner";

            if (new[] { "centerofwidthanddepth", "center", "centroid", "legacycenter" }.Contains(normalized))
                return "center_of_width_and_depth";

            return origin;
        }

        /// <summary>
        /// Get length in feet from dimensions
        /// </summary>
        private double? GetLengthFeet(UnitRecord unit, string dimension)
        {
            switch (dimension.ToLower())
            {
                case "width": return unit.Dimensions?.WidthRaw;
                case "depth": return unit.Dimensions?.DepthRaw;
                case "height": return unit.Dimensions?.HeightRaw;
                default: return null;
            }
        }

        /// <summary>
        /// Get import height (handles lockers specially)
        /// </summary>
        private double GetImportHeight(UnitRecord unit)
        {
            bool isLocker = unit.Classification?.Locker == true;

            if (isLocker)
                return 3.0; // Lockers are 3' tall

            var height = GetLengthFeet(unit, "height");
            return height ?? 8.0; // Default 8'
        }

        /// <summary>
        /// Get import offset (handles lockers specially)
        /// </summary>
        private double GetImportOffset(UnitRecord unit)
        {
            bool isLocker = unit.Classification?.Locker == true;

            if (isLocker)
                return 5.0; // Lockers offset 5' from level

            var defaultElev = GetLengthFeet(unit, "default_elevation");
            return defaultElev ?? 0.0;
        }

        /// <summary>
        /// Set offset parameters on instance
        /// </summary>
        private bool SetOffsetParams(FamilyInstance instance, double offset)
        {
            bool anySet = false;

            string[] offsetParams = {
                "Offset from Host",
                "Elevation from Level",
                "Offset from Level",
                "Default Elevation"
            };

            foreach (var paramName in offsetParams)
            {
                try
                {
                    var param = instance.LookupParameter(paramName);
                    if (param != null && !param.IsReadOnly)
                    {
                        param.Set(offset);
                        anySet = true;
                    }
                }
                catch { }
            }

            return anySet;
        }

        /// <summary>
        /// Set unit parameters from JSON data
        /// </summary>
        private void SetUnitParams(FamilyInstance instance, UnitRecord unit)
        {
            // Identity
            SetParam(instance, "building_number", unit.Identity?.BuildingNumber);
            SetParam(instance, "unit_number", unit.Identity?.UnitNumber);

            // Dimensions
            SetParam(instance, "width", GetLengthFeet(unit, "width"));
            SetParam(instance, "depth", GetLengthFeet(unit, "depth"));
            SetParam(instance, "height", GetImportHeight(unit));

            // Classification (boolean fields)
            if (unit.Classification != null)
            {
                SetParam(instance, "climate", BoolToRevit(unit.Classification.Climate));
                SetParam(instance, "climate_heat_only", BoolToRevit(unit.Classification.ClimateHeatOnly));
                SetParam(instance, "driveup", BoolToRevit(unit.Classification.Driveup));
                SetParam(instance, "locker", BoolToRevit(unit.Classification.Locker));
                SetParam(instance, "ground_access", BoolToRevit(unit.Classification.GroundAccess));
                SetParam(instance, "accessible", BoolToRevit(unit.Classification.Accessible));
                SetParam(instance, "obstructions", BoolToRevit(unit.Classification.Obstructions));
                SetParam(instance, "offline", BoolToRevit(unit.Classification.Offline));
                SetParam(instance, "portable", BoolToRevit(unit.Classification.Portable));
                SetParam(instance, "stack_bottom", BoolToRevit(unit.Classification.StackBottom));
                SetParam(instance, "stack_top", BoolToRevit(unit.Classification.StackTop));
                SetParam(instance, "walkup", BoolToRevit(unit.Classification.Walkup));
            }
        }

        /// <summary>
        /// Set parameter on element
        /// </summary>
        private bool SetParam(Element element, string key, object value)
        {
            if (!TARGET_PARAM_MAP.ContainsKey(key) || value == null)
                return false;

            string paramName = TARGET_PARAM_MAP[key];

            try
            {
                var param = element.LookupParameter(paramName);
                if (param == null || param.IsReadOnly)
                    return false;

                switch (param.StorageType)
                {
                    case StorageType.String:
                        param.Set(value.ToString());
                        return true;

                    case StorageType.Integer:
                        if (value is bool b)
                            param.Set(b ? 1 : 0);
                        else
                            param.Set(Convert.ToInt32(value));
                        return true;

                    case StorageType.Double:
                        param.Set(Convert.ToDouble(value));
                        return true;

                    case StorageType.ElementId:
                        param.Set((ElementId)value);
                        return true;

                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Convert boolean to Revit integer (1/0)
        /// </summary>
        private int? BoolToRevit(bool? value)
        {
            if (!value.HasValue) return null;
            return value.Value ? 1 : 0;
        }

        /// <summary>
        /// Rotate family instance around Z axis
        /// </summary>
        private void RotateInstance(FamilyInstance instance, double angleRadians)
        {
            if (Math.Abs(angleRadians) < 0.0000001) return;

            try
            {
                var point = (instance.Location as LocationPoint)?.Point;
                if (point == null) return;

                var axis = Line.CreateBound(
                    point,
                    new XYZ(point.X, point.Y, point.Z + 10.0)
                );

                ElementTransformUtils.RotateElement(_doc, instance.Id, axis, angleRadians);
            }
            catch { }
        }

        /// <summary>
        /// Find family symbol by name
        /// </summary>
        private FamilySymbol FindFamilySymbol(string familyName, string typeName)
        {
            return new FilteredElementCollector(_doc)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .FirstOrDefault(s =>
                    s.FamilyName == familyName &&
                    s.Name == typeName);
        }

        /// <summary>
        /// Validate unit without importing
        /// </summary>
        private void ValidateUnit(UnitRecord unit)
        {
            var point = GetCorrectedInsertPoint(unit);
            if (point == null)
                throw new Exception("Could not determine insertion point");

            string levelName = unit.Placement?.LevelName;
            var level = LevelHelper.FindLevel(_doc, levelName, _customLevelMap);

            if (level == null)
                throw new Exception($"Could not match level: {levelName}");
        }

        /// <summary>
        /// Get unit label for error messages
        /// </summary>
        private string GetUnitLabel(UnitRecord unit)
        {
            var unitNumber = unit.Identity?.UnitNumber;
            var elementId = unit.Source?.ElementId;
            var levelName = unit.Placement?.LevelName;

            return $"Unit {unitNumber}, ID {elementId}, Level {levelName}";
        }

        /// <summary>
        /// Get nested value from object using path (e.g., ["placement", "location", "point"])
        /// </summary>
        private T GetNested<T>(object obj, string[] path)
        {
            if (obj == null || path == null || path.Length == 0)
                return default(T);

            object current = obj;

            foreach (var key in path)
            {
                if (current == null)
                    return default(T);

                var prop = current.GetType().GetProperty(
                    key,
                    System.Reflection.BindingFlags.IgnoreCase |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.Instance
                );

                if (prop == null)
                    return default(T);

                current = prop.GetValue(current);
            }

            if (current == null)
                return default(T);

            try
            {
                return (T)current;
            }
            catch
            {
                return default(T);
            }
        }

        /// <summary>
        /// Get first non-null nested value from multiple paths
        /// </summary>
        private T GetFirstNested<T>(object obj, string[][] paths)
        {
            foreach (var path in paths)
            {
                var value = GetNested<T>(obj, path);
                if (value != null && !value.Equals(default(T)))
                    return value;
            }

            return default(T);
        }

        /// <summary>
        /// Get first non-null nested string from multiple paths (non-generic overload to avoid C# parsing issues)
        /// </summary>
        private string GetFirstNestedString(object obj, string[][] paths)
        {
            foreach (var path in paths)
            {
                var value = GetNested<string>(obj, path);
                if (!string.IsNullOrEmpty(value))
                    return value;
            }

            return null;
        }
    }
}
