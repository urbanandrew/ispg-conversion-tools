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

            if (payload == null || payload.Units == null)
                throw new Exception("Invalid JSON payload");

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
            string levelName = GetNested<string>(unit, new[] { "placement", "level_name" });
            var level = LevelHelper.FindLevel(_doc, levelName, _customLevelMap);

            if (level == null)
                throw new Exception($"Could not match level: {levelName}");

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
            var pointData = GetNested<PointData>(unit, new[] { "placement", "location", "point" });
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
            var rotation = GetNested<double?>(unit, new[] { "placement", "location", "rotation_radians" });
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
            // Try explicit source origin fields
            var paths = new[]
            {
                new[] { "source", "source_origin" },
                new[] { "migration_assumptions", "source_origin" },
                new[] { "migration_assumptions", "old_origin" }
            };
            var explicitOrigin = GetFirstNestedString(unit, paths);

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
            var explicitOrigin = GetNested<string>(unit, new[] { "migration_assumptions", "target_origin" });
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
            var path = new[] { "dimensions", $"{dimension}_raw" };
            return GetNested<double?>(unit, path);
        }

        /// <summary>
        /// Get import height (handles lockers specially)
        /// </summary>
        private double GetImportHeight(UnitRecord unit)
        {
            bool isLocker = GetNested<bool?>(unit, new[] { "classification", "locker" }) == true;

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
            bool isLocker = GetNested<bool?>(unit, new[] { "classification", "locker" }) == true;

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
            SetParam(instance, "building_number", GetNested<object>(unit, new[] { "identity", "building_number" }));
            SetParam(instance, "unit_number", GetNested<object>(unit, new[] { "identity", "unit_number" }));

            // Dimensions
            SetParam(instance, "width", GetLengthFeet(unit, "width"));
            SetParam(instance, "depth", GetLengthFeet(unit, "depth"));
            SetParam(instance, "height", GetImportHeight(unit));

            // Classification (boolean fields)
            string[] boolKeys = {
                "climate", "climate_heat_only", "driveup", "locker",
                "ground_access", "accessible", "obstructions", "offline",
                "portable", "stack_bottom", "stack_top", "walkup"
            };

            foreach (var key in boolKeys)
            {
                var value = GetNested<bool?>(unit, new[] { "classification", key });
                SetParam(instance, key, BoolToRevit(value));
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

            string levelName = GetNested<string>(unit, new[] { "placement", "level_name" });
            var level = LevelHelper.FindLevel(_doc, levelName, _customLevelMap);

            if (level == null)
                throw new Exception($"Could not match level: {levelName}");
        }

        /// <summary>
        /// Get unit label for error messages
        /// </summary>
        private string GetUnitLabel(UnitRecord unit)
        {
            var unitNumber = GetNested<object>(unit, new[] { "identity", "unit_number" });
            var elementId = unit.Source?.ElementId;
            var levelName = GetNested<string>(unit, new[] { "placement", "level_name" });

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
