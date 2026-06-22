using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.Revit.DB;
using Newtonsoft.Json;
using ISPG.Conversion.Models;

namespace ISPG.Conversion.Core
{
    /// <summary>
    /// Exports Revit elements to JSON format matching pyRevit schema version 6
    /// </summary>
    public class JsonExporter
    {
        private readonly Document _doc;
        private readonly string _exportType; // "units", "parking", or "shell"

        public JsonExporter(Document doc, string exportType)
        {
            _doc = doc;
            _exportType = exportType;
        }

        /// <summary>
        /// Export elements to JSON file
        /// </summary>
        public string Export(IEnumerable<FamilyInstance> elements, string filePath)
        {
            var payload = BuildPayload(elements);

            var json = JsonConvert.SerializeObject(payload, Formatting.Indented);
            File.WriteAllText(filePath, json);

            return filePath;
        }

        /// <summary>
        /// Build export payload from elements
        /// </summary>
        private ExportPayload BuildPayload(IEnumerable<FamilyInstance> elements)
        {
            var elementsList = elements.ToList();
            var exportedRecords = new List<UnitRecord>();
            var skippedRecords = new List<SkippedRecord>();
            var matchReasonCounts = new Dictionary<string, int>();
            var sourceOriginCounts = new Dictionary<string, int>();

            foreach (var element in elementsList)
            {
                try
                {
                    var record = ExportElement(element);
                    if (record != null)
                    {
                        exportedRecords.Add(record);

                        // Count match reasons
                        string matchReason = record.Source.MatchReason ?? "unknown";
                        if (!matchReasonCounts.ContainsKey(matchReason))
                            matchReasonCounts[matchReason] = 0;
                        matchReasonCounts[matchReason]++;

                        // Count source origins
                        string sourceOrigin = record.Source.SourceOrigin ?? "unknown";
                        if (!sourceOriginCounts.ContainsKey(sourceOrigin))
                            sourceOriginCounts[sourceOrigin] = 0;
                        sourceOriginCounts[sourceOrigin]++;
                    }
                }
                catch (Exception ex)
                {
                    skippedRecords.Add(new SkippedRecord
                    {
                        ElementId = ParameterHelper.GetElementIdValue(element.Id) ?? -1,
                        FamilyName = element.Symbol?.FamilyName ?? "Unknown",
                        TypeName = element.Symbol?.Name ?? "Unknown",
                        Reason = $"Export failed: {ex.Message}"
                    });
                }
            }

            return new ExportPayload
            {
                Schema = new SchemaInfo
                {
                    Name = $"umx_revit_2022_plus_ux5_unit_extract",
                    Version = 6
                },
                ExportedAt = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
                Document = new DocumentInfo
                {
                    Title = _doc.Title,
                    Path = _doc.PathName
                },
                Filters = new FilterInfo
                {
                    FamilyOrTypeNameContains = GetFilterContains(),
                    FamilyOrTypeNameStartsWith = GetFilterStartsWith(),
                    AlsoMatchesByUnitParameters = true,
                    Ux5UnitOrigin = "front_left"
                },
                Counts = new CountInfo
                {
                    Exported = exportedRecords.Count,
                    SkippedGenericModels = skippedRecords.Count,
                    MatchReasonCounts = matchReasonCounts,
                    SourceOriginCounts = sourceOriginCounts
                },
                Units = exportedRecords,
                Skipped = skippedRecords
            };
        }

        /// <summary>
        /// Export a single element to UnitRecord
        /// </summary>
        private UnitRecord ExportElement(FamilyInstance instance)
        {
            var symbol = instance.Symbol;
            var familyName = symbol?.FamilyName ?? "";
            var typeName = symbol?.Name ?? "";

            // Determine match reason
            string matchReason = DetermineMatchReason(familyName, typeName);

            // Determine source origin (for units, check if it's legacy or UX5)
            string sourceOrigin = DetermineSourceOrigin(familyName, typeName);

            // Get location
            var location = instance.Location as LocationPoint;
            var position = location?.Point;
            var rotationRadians = OriginCalculator.GetRotationRadians(location) ?? 0.0;
            var rotationDegrees = OriginCalculator.RadiansToDegrees(rotationRadians);

            // Get dimensions
            var (widthRaw, widthString, widthSource, widthParam) = GetDimension(instance, symbol, ParameterHelper.WIDTH_PARAMS, typeName);
            var (depthRaw, depthString, depthSource, depthParam) = GetDimension(instance, symbol, ParameterHelper.DEPTH_PARAMS, typeName);
            var (heightRaw, heightString, heightSource, heightParam) = GetDimension(instance, symbol, ParameterHelper.HEIGHT_PARAMS, typeName);
            var (elevationRaw, elevationString, elevationSource, elevationParam) = GetDimension(instance, symbol, ParameterHelper.DEFAULT_ELEVATION_PARAMS, typeName);

            // Calculate migration factors
            var (widthFactor, depthFactor) = OriginCalculator.CalculateMigrationFactors(
                rotationDegrees, sourceOrigin, "front_left");

            // Get level info
            var level = _doc.GetElement(instance.LevelId) as Level;
            double? levelOffset = null;
            string levelOffsetSource = null;
            string levelOffsetString = null;

            if (level != null)
            {
                var offsetParam = instance.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM);
                if (offsetParam != null && offsetParam.HasValue)
                {
                    levelOffset = offsetParam.AsDouble();
                    levelOffsetSource = "instance";
                    levelOffsetString = offsetParam.AsValueString();
                }
                else if (elevationRaw.HasValue)
                {
                    levelOffset = elevationRaw.Value;
                    levelOffsetSource = "default_elevation";
                    levelOffsetString = elevationString;
                }
            }

            return new UnitRecord
            {
                Source = new SourceInfo
                {
                    RevitVersion = _doc.Application.VersionNumber,
                    DocumentTitle = _doc.Title,
                    DocumentPath = _doc.PathName,
                    ElementId = ParameterHelper.GetElementIdValue(instance.Id) ?? -1,
                    UniqueId = instance.UniqueId,
                    FamilyName = familyName,
                    TypeName = typeName,
                    UxRole = _exportType,
                    MatchReason = matchReason,
                    SourceOrigin = sourceOrigin
                },
                MigrationAssumptions = new MigrationAssumptions
                {
                    OldOrigin = sourceOrigin,
                    SourceOrigin = sourceOrigin,
                    TargetOrigin = "front_left",
                    WidthDirectionBasis = "rotation_0_degrees_points_right",
                    DepthDirectionBasis = "rotation_0_degrees_points_up",
                    LegacyCenterToTargetFrontLeftWidthFactor = widthFactor,
                    LegacyCenterToTargetFrontLeftDepthFactor = depthFactor
                },
                Identity = GetIdentity(instance, symbol),
                Dimensions = new DimensionsInfo
                {
                    Width = ParameterHelper.CreateLengthRecord(widthRaw),
                    Depth = ParameterHelper.CreateLengthRecord(depthRaw),
                    Height = ParameterHelper.CreateLengthRecord(heightRaw),
                    DefaultElevation = ParameterHelper.CreateLengthRecord(elevationRaw),
                    WidthRaw = widthRaw,
                    DepthRaw = depthRaw,
                    HeightRaw = heightRaw,
                    DefaultElevationRaw = elevationRaw,
                    WidthString = widthString,
                    DepthString = depthString,
                    HeightString = heightString,
                    DefaultElevationString = elevationString,
                    WidthSource = widthSource,
                    DepthSource = depthSource,
                    HeightSource = heightSource,
                    DefaultElevationSource = elevationSource,
                    WidthParam = widthParam,
                    DepthParam = depthParam,
                    HeightParam = heightParam,
                    DefaultElevationParam = elevationParam,
                    ParsedWidthFeet = widthRaw,
                    ParsedDepthFeet = depthRaw
                },
                Classification = GetClassification(instance, symbol),
                Placement = new PlacementInfo
                {
                    Location = position != null ? new LocationData
                    {
                        Point = new PointData
                        {
                            XFeet = position.X,
                            YFeet = position.Y,
                            ZFeet = position.Z,
                            XInches = position.X * 12,
                            YInches = position.Y * 12,
                            ZInches = position.Z * 12
                        },
                        RotationRadians = rotationRadians,
                        RotationDegrees = rotationDegrees
                    } : null,
                    BoundingBox = GetBoundingBox(instance),
                    Mirrored = instance.Mirrored,
                    HandFlipped = instance.HandFlipped,
                    FacingFlipped = instance.FacingFlipped,
                    LevelId = ParameterHelper.GetElementIdValue(instance.LevelId),
                    LevelName = ParameterHelper.GetElementNameById(_doc, instance.LevelId),
                    LevelOffset = levelOffset.HasValue ? new LevelOffsetData
                    {
                        Feet = levelOffset.Value,
                        Inches = levelOffset.Value * 12,
                        Source = levelOffsetSource,
                        ValueString = levelOffsetString
                    } : null,
                    DesignOption = GetDesignOptionName(instance),
                    Workset = GetWorksetName(instance),
                    PhaseCreated = GetPhaseName(instance.CreatedPhaseId),
                    PhaseDemolished = GetPhaseName(instance.DemolishedPhaseId)
                },
                Parameters = new ParametersInfo
                {
                    Instance = ParameterHelper.CollectParameters(instance),
                    Type = ParameterHelper.CollectParameters(symbol)
                }
            };
        }

        /// <summary>
        /// Get dimension value from parameter or type name
        /// Returns (rawValue, valueString, source, paramName)
        /// </summary>
        private (double? raw, string str, string source, string param) GetDimension(
            FamilyInstance instance, FamilySymbol symbol, string[] paramNames, string typeName)
        {
            // Try parameters first
            var (value, valueString, source, paramName) = ParameterHelper.GetFirstParamValue(instance, symbol, paramNames);

            if (value != null)
            {
                double? rawValue = value as double?;
                if (!rawValue.HasValue && value is int intVal)
                    rawValue = (double)intVal;

                return (rawValue, valueString, source, paramName);
            }

            // Try parsing from type name (e.g., "5x10")
            var (width, depth) = ParameterHelper.ParseSizeFromText(typeName);

            // Return width or depth based on which parameter we're looking for
            if (paramNames == ParameterHelper.WIDTH_PARAMS && width.HasValue)
                return (width.Value, width.Value.ToString("F2"), "type_name", "TypeName");

            if (paramNames == ParameterHelper.DEPTH_PARAMS && depth.HasValue)
                return (depth.Value, depth.Value.ToString("F2"), "type_name", "TypeName");

            return (null, null, null, null);
        }

        /// <summary>
        /// Get identity info (building number, unit number)
        /// </summary>
        private IdentityInfo GetIdentity(FamilyInstance instance, FamilySymbol symbol)
        {
            var (bldgValue, bldgString, bldgSource, bldgParam) = ParameterHelper.GetFirstParamValue(
                instance, symbol, ParameterHelper.BUILDING_NUMBER_PARAMS);

            var (unitValue, unitString, unitSource, unitParam) = ParameterHelper.GetFirstParamValue(
                instance, symbol, ParameterHelper.UNIT_NUMBER_PARAMS);

            return new IdentityInfo
            {
                BuildingNumber = bldgValue,
                BuildingNumberString = bldgString,
                BuildingNumberSource = bldgSource,
                BuildingNumberParam = bldgParam,
                UnitNumber = unitValue,
                UnitNumberString = unitString,
                UnitNumberSource = unitSource,
                UnitNumberParam = unitParam
            };
        }

        /// <summary>
        /// Get classification info (climate, driveup, etc.)
        /// </summary>
        private ClassificationInfo GetClassification(FamilyInstance instance, FamilySymbol symbol)
        {
            var classification = new ClassificationInfo();

            // Climate
            GetBooleanClassification(instance, symbol, ParameterHelper.CLIMATE_PARAMS,
                out classification.ClimateRaw, out classification.ClimateString,
                out classification.Climate, out classification.ClimateSource,
                out classification.ClimateParam);

            // Climate Heat Only
            GetBooleanClassification(instance, symbol, ParameterHelper.CLIMATE_HEAT_ONLY_PARAMS,
                out classification.ClimateHeatOnlyRaw, out classification.ClimateHeatOnlyString,
                out classification.ClimateHeatOnly, out classification.ClimateHeatOnlySource,
                out classification.ClimateHeatOnlyParam);

            // DriveUp
            GetBooleanClassification(instance, symbol, ParameterHelper.DRIVEUP_PARAMS,
                out classification.DriveupRaw, out classification.DriveupString,
                out classification.Driveup, out classification.DriveupSource,
                out classification.DriveupParam);

            // Locker
            GetBooleanClassification(instance, symbol, ParameterHelper.LOCKER_PARAMS,
                out classification.LockerRaw, out classification.LockerString,
                out classification.Locker, out classification.LockerSource,
                out classification.LockerParam);

            // Ground Access
            GetBooleanClassification(instance, symbol, ParameterHelper.GROUND_ACCESS_PARAMS,
                out classification.GroundAccessRaw, out classification.GroundAccessString,
                out classification.GroundAccess, out classification.GroundAccessSource,
                out classification.GroundAccessParam);

            // Accessible
            GetBooleanClassification(instance, symbol, ParameterHelper.ACCESSIBLE_PARAMS,
                out classification.AccessibleRaw, out classification.AccessibleString,
                out classification.Accessible, out classification.AccessibleSource,
                out classification.AccessibleParam);

            // Obstructions
            GetBooleanClassification(instance, symbol, ParameterHelper.OBSTRUCTIONS_PARAMS,
                out classification.ObstructionsRaw, out classification.ObstructionsString,
                out classification.Obstructions, out classification.ObstructionsSource,
                out classification.ObstructionsParam);

            // Offline
            GetBooleanClassification(instance, symbol, ParameterHelper.OFFLINE_PARAMS,
                out classification.OfflineRaw, out classification.OfflineString,
                out classification.Offline, out classification.OfflineSource,
                out classification.OfflineParam);

            // Portable
            GetBooleanClassification(instance, symbol, ParameterHelper.PORTABLE_PARAMS,
                out classification.PortableRaw, out classification.PortableString,
                out classification.Portable, out classification.PortableSource,
                out classification.PortableParam);

            // Stack Bottom
            GetBooleanClassification(instance, symbol, ParameterHelper.STACK_BOTTOM_PARAMS,
                out classification.StackBottomRaw, out classification.StackBottomString,
                out classification.StackBottom, out classification.StackBottomSource,
                out classification.StackBottomParam);

            // Stack Top
            GetBooleanClassification(instance, symbol, ParameterHelper.STACK_TOP_PARAMS,
                out classification.StackTopRaw, out classification.StackTopString,
                out classification.StackTop, out classification.StackTopSource,
                out classification.StackTopParam);

            // WalkUp
            GetBooleanClassification(instance, symbol, ParameterHelper.WALKUP_PARAMS,
                out classification.WalkupRaw, out classification.WalkupString,
                out classification.Walkup, out classification.WalkupSource,
                out classification.WalkupParam);

            return classification;
        }

        /// <summary>
        /// Helper to get boolean classification field
        /// </summary>
        private void GetBooleanClassification(FamilyInstance instance, FamilySymbol symbol, string[] paramNames,
            out object raw, out string str, out bool? boolean, out string source, out string param)
        {
            var (value, valueString, paramSource, paramName) = ParameterHelper.GetFirstParamValue(instance, symbol, paramNames);

            raw = value;
            str = valueString;
            boolean = ParameterHelper.Boolish(value);
            source = paramSource;
            param = paramName;
        }

        /// <summary>
        /// Get bounding box data
        /// </summary>
        private BoundingBoxData GetBoundingBox(FamilyInstance instance)
        {
            try
            {
                var bbox = instance.get_BoundingBox(null);
                if (bbox == null) return null;

                var min = bbox.Min;
                var max = bbox.Max;
                var size = max - min;

                return new BoundingBoxData
                {
                    Min = new CoordData { XFeet = min.X, YFeet = min.Y, ZFeet = min.Z },
                    Max = new CoordData { XFeet = max.X, YFeet = max.Y, ZFeet = max.Z },
                    Size = new SizeData
                    {
                        XFeet = size.X,
                        YFeet = size.Y,
                        ZFeet = size.Z,
                        XInches = size.X * 12,
                        YInches = size.Y * 12,
                        ZInches = size.Z * 12
                    }
                };
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Determine match reason for element
        /// </summary>
        private string DetermineMatchReason(string familyName, string typeName)
        {
            if (ContainsAny(familyName, GetFilterContains()) || ContainsAny(typeName, GetFilterContains()))
                return "family_or_type_name_contains";

            if (StartsWithAny(familyName, GetFilterStartsWith()) || StartsWithAny(typeName, GetFilterStartsWith()))
                return "family_or_type_name_starts_with";

            return "unit_parameters";
        }

        /// <summary>
        /// Determine source origin for element
        /// </summary>
        private string DetermineSourceOrigin(string familyName, string typeName)
        {
            // UX5 units have front_left origin
            if (familyName?.Contains("UX5") == true || typeName?.Contains("UX5") == true)
                return "front_left";

            // Everything else is legacy center origin
            return "legacy_center";
        }

        /// <summary>
        /// Get filter "contains" strings based on export type
        /// </summary>
        private List<string> GetFilterContains()
        {
            switch (_exportType)
            {
                case "units":
                    return new List<string> { "UX", "Unit" };
                case "parking":
                    return new List<string> { "Parking" };
                case "shell":
                    return new List<string> { "Shell" };
                default:
                    return new List<string>();
            }
        }

        /// <summary>
        /// Get filter "starts with" strings based on export type
        /// </summary>
        private List<string> GetFilterStartsWith()
        {
            switch (_exportType)
            {
                case "units":
                    return new List<string> { "UX" };
                default:
                    return new List<string>();
            }
        }

        private bool ContainsAny(string text, List<string> patterns)
        {
            if (string.IsNullOrEmpty(text)) return false;
            return patterns.Any(p => text.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private bool StartsWithAny(string text, List<string> patterns)
        {
            if (string.IsNullOrEmpty(text)) return false;
            return patterns.Any(p => text.StartsWith(p, StringComparison.OrdinalIgnoreCase));
        }

        private string GetDesignOptionName(FamilyInstance instance)
        {
            try
            {
                var param = instance.get_Parameter(BuiltInParameter.DESIGN_OPTION_ID);
                if (param != null && param.HasValue)
                {
                    var id = param.AsElementId();
                    return ParameterHelper.GetElementNameById(_doc, id);
                }
            }
            catch { }
            return null;
        }

        private string GetWorksetName(FamilyInstance instance)
        {
            try
            {
                if (_doc.IsWorkshared)
                {
                    var worksetId = instance.WorksetId;
                    var workset = _doc.GetWorksetTable().GetWorkset(worksetId);
                    return workset.Name;
                }
            }
            catch { }
            return null;
        }

        private string GetPhaseName(ElementId phaseId)
        {
            if (phaseId == null || phaseId == ElementId.InvalidElementId) return null;
            return ParameterHelper.GetElementNameById(_doc, phaseId);
        }
    }
}
