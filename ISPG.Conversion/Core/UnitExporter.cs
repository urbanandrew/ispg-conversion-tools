using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Autodesk.Revit.DB;
using Newtonsoft.Json;
using ISPG.Conversion.Models;

namespace ISPG.Conversion.Core
{
    /// <summary>
    /// Exports unit spaces to JSON matching Python LegacyExportUnits script exactly
    /// </summary>
    public class UnitExporter
    {
        private readonly Document _doc;

        // Family name filters matching Python UNIT_FAMILY_NAME_CONTAINS and UNIT_FAMILY_NAME_STARTS_WITH
        private static readonly string[] CONTAINS_PATTERNS = { "ISPG UX Shell", "ISPG UX Unit", "UX4 Unit" };
        private static readonly string[] STARTS_WITH_PATTERNS = { "UX3 Unit", "UX5_Unit" };

        public UnitExporter(Document doc)
        {
            _doc = doc;
        }

        public string Export(IEnumerable<FamilyInstance> elements, string filePath)
        {
            var payload = BuildPayload(elements);
            var json = JsonConvert.SerializeObject(payload, Formatting.Indented);

            File.WriteAllText(filePath, json);

            return filePath;
        }

        private UnitExportPayload BuildPayload(IEnumerable<FamilyInstance> elements)
        {
            var records = new List<UnitExportRecord>();
            var skipped = new List<SkippedElement>();
            var matchReasonCounts = new Dictionary<string, int>();
            var sourceOriginCounts = new Dictionary<string, int>();

            foreach (var instance in elements)
            {
                try
                {
                    var familySymbol = instance.Symbol;
                    if (familySymbol == null) continue;

                    var family = familySymbol.Family;
                    if (family == null) continue;

                    string familyName = family.Name ?? "";
                    string typeName = familySymbol.Name ?? "";

                    // Match against Python patterns
                    string matchReason = GetMatchReason(familyName);
                    if (matchReason == null)
                    {
                        skipped.Add(new SkippedElement
                        {
                            ElementId = ParameterHelper.GetElementIdValue(instance.Id),
                            FamilyName = familyName,
                            TypeName = typeName,
                            Reason = "family_type_filter"
                        });
                        continue;
                    }

                    var record = BuildUnitRecord(instance, familyName, typeName);
                    if (record != null)
                    {
                        records.Add(record);

                        // Count match reasons
                        string reason = record.Source.MatchReason ?? "unknown";
                        if (!matchReasonCounts.ContainsKey(reason))
                            matchReasonCounts[reason] = 0;
                        matchReasonCounts[reason]++;

                        // Count source origins
                        string sourceOrigin = record.Source.sourceOrigin ?? "unknown";
                        if (!sourceOriginCounts.ContainsKey(sourceOrigin))
                            sourceOriginCounts[sourceOrigin] = 0;
                        sourceOriginCounts[sourceOrigin]++;
                    }
                }
                catch (Exception ex)
                {
                    skipped.Add(new SkippedElement
                    {
                        ElementId = ParameterHelper.GetElementIdValue(instance.Id),
                        Reason = $"exception: {ex.Message}"
                    });
                }
            }

            return new UnitExportPayload
            {
                ExportMetadata = new ExportMetadata
                {
                    ExportType = "units",
                    ExportDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    RevitVersion = _doc.Application.VersionNumber,
                    DocumentTitle = _doc.Title,
                    DocumentPath = _doc.PathName
                },
                Units = records,
                Skipped = skipped,
                Summary = new ExportSummary
                {
                    TotalProcessed = records.Count + skipped.Count,
                    SuccessfulExports = records.Count,
                    SkippedCount = skipped.Count,
                    MatchReasonCounts = matchReasonCounts,
                    SourceOriginCounts = sourceOriginCounts
                }
            };
        }

        private string GetMatchReason(string familyName)
        {
            foreach (var pattern in CONTAINS_PATTERNS)
            {
                if (familyName.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0)
                    return $"contains_{pattern.Replace(" ", "_")}";
            }

            foreach (var pattern in STARTS_WITH_PATTERNS)
            {
                if (familyName.StartsWith(pattern, StringComparison.OrdinalIgnoreCase))
                    return $"starts_with_{pattern.Replace(" ", "_")}";
            }

            return null;
        }

        private UnitExportRecord BuildUnitRecord(FamilyInstance instance, string familyName, string typeName)
        {
            var symbol = instance.Symbol;
            
            // Determine match reason (simplified for now - full pattern matching logic TBD)
            string matchReason = "unit_parameters";

            // Extract all parameters using Python's first_param_value pattern
            var buildingNumber = ParameterHelper.GetFirstParamValue(instance, symbol, UnitParameterHelper.BUILDING_NUMBER_PARAMS);
            var unitNumber = ParameterHelper.GetFirstParamValue(instance, symbol, UnitParameterHelper.UNIT_NUMBER_PARAMS);
            var width = ParameterHelper.GetFirstParamValue(instance, symbol, UnitParameterHelper.WIDTH_PARAMS);
            var depth = ParameterHelper.GetFirstParamValue(instance, symbol, UnitParameterHelper.DEPTH_PARAMS);
            var height = ParameterHelper.GetFirstParamValue(instance, symbol, UnitParameterHelper.HEIGHT_PARAMS);
            var defaultElevation = ParameterHelper.GetFirstParamValue(instance, symbol, UnitParameterHelper.DEFAULT_ELEVATION_PARAMS);

            // Classification parameters
            var climate = ParameterHelper.GetFirstParamValue(instance, symbol, UnitParameterHelper.CLIMATE_PARAMS);
            var climateHeatOnly = ParameterHelper.GetFirstParamValue(instance, symbol, UnitParameterHelper.CLIMATE_HEAT_ONLY_PARAMS);
            var driveup = ParameterHelper.GetFirstParamValue(instance, symbol, UnitParameterHelper.DRIVEUP_PARAMS);
            var locker = ParameterHelper.GetFirstParamValue(instance, symbol, UnitParameterHelper.LOCKER_PARAMS);
            var groundAccess = ParameterHelper.GetFirstParamValue(instance, symbol, UnitParameterHelper.GROUND_ACCESS_PARAMS);
            var accessible = ParameterHelper.GetFirstParamValue(instance, symbol, UnitParameterHelper.ACCESSIBLE_PARAMS);
            var obstructions = ParameterHelper.GetFirstParamValue(instance, symbol, UnitParameterHelper.OBSTRUCTIONS_PARAMS);
            var offline = ParameterHelper.GetFirstParamValue(instance, symbol, UnitParameterHelper.OFFLINE_PARAMS);
            var portable = ParameterHelper.GetFirstParamValue(instance, symbol, UnitParameterHelper.PORTABLE_PARAMS);
            var stackBottom = ParameterHelper.GetFirstParamValue(instance, symbol, UnitParameterHelper.STACK_BOTTOM_PARAMS);
            var stackTop = ParameterHelper.GetFirstParamValue(instance, symbol, UnitParameterHelper.STACK_TOP_PARAMS);
            var walkup = ParameterHelper.GetFirstParamValue(instance, symbol, UnitParameterHelper.WALKUP_PARAMS);

            // Parse dimensions from type name if not found in parameters
            double? parsedWidth = null;
            double? parsedDepth = null;
            if (width.value == null || depth.value == null)
            {
                var parsed = ParseSizeFromTypeName(typeName);
                parsedWidth = parsed.Item1;
                parsedDepth = parsed.Item2;
            }

            double? finalWidth = (width.value as double?) ?? parsedWidth;
            double? finalDepth = (depth.value as double?) ?? parsedDepth;

            // Get level info
            string levelName = null;
            try
            {
                if (instance.LevelId != null && instance.LevelId != ElementId.InvalidElementId)
                {
                    var level = _doc.GetElement(instance.LevelId) as Level;
                    levelName = level?.Name;
                }
            }
            catch { }

            // Get level offset
            double? levelOffset = null;
            string levelOffsetSource = null;
            string levelOffsetString = null;
            
            if (levelName != null)
            {
                var offsetParam = instance.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM);
                if (offsetParam != null && offsetParam.HasValue)
                {
                    levelOffset = offsetParam.AsDouble();
                    levelOffsetSource = "instance";
                    levelOffsetString = offsetParam.AsValueString();
                }
            }

            // Get design option
            string designOptionName = null;
            try
            {
                var designOption = instance.DesignOption;
                if (designOption != null && designOption.Id != ElementId.InvalidElementId)
                    designOptionName = designOption.Name;
            }
            catch { }

            // Get rotation
            double? rotationDegrees = null;
            try
            {
                var locationPoint = instance.Location as LocationPoint;
                if (locationPoint != null)
                {
                    double rotationRadians = locationPoint.Rotation;
                    rotationDegrees = rotationRadians * (180.0 / Math.PI);
                }
            }
            catch { }

            string sourceOrigin = GetSourceOrigin(familyName, typeName);
            string targetOrigin = GetTargetOrigin(familyName, typeName);

            return new UnitExportRecord
            {
                Source = new SourceData
                {
                    RevitVersion = "2022_plus_ux5_unit",
                    DocumentTitle = _doc.Title,
                    DocumentPath = _doc.PathName,
                    ElementId = ParameterHelper.GetElementIdValue(instance.Id),
                    UniqueId = instance.UniqueId,
                    FamilyName = familyName,
                    TypeName = typeName,
                    UxRole = GetUxRole(familyName, typeName),
                    MatchReason = matchReason,
                    SourceOrigin = sourceOrigin
                },
                MigrationAssumptions = new MigrationAssumptions
                {
                    OldOrigin = sourceOrigin,
                    SourceOrigin = sourceOrigin,
                    TargetOrigin = targetOrigin,
                    WidthDirectionBasis = "family_local_x_assumed_width_direction",
                    DepthDirectionBasis = "family_local_y_assumed_depth_direction",
                    LegacyCenterToTargetFrontLeftWidthFactor = -0.5,
                    LegacyCenterToTargetFrontLeftDepthFactor = -0.5
                },
                Identity = new UnitIdentity
                {
                    BuildingNumber = buildingNumber.value,
                    BuildingNumberString = buildingNumber.valueString,
                    BuildingNumberSource = buildingNumber.source,
                    BuildingNumberParam = buildingNumber.paramName,
                    UnitNumber = unitNumber.valueString,
                    UnitNumberString = unitNumber.valueString,
                    UnitNumberSource = unitNumber.source,
                    UnitNumberParam = unitNumber.paramName
                },
                Dimensions = new UnitDimensions
                {
                    Width = ParameterHelper.CreateLengthRecord(finalWidth),
                    Depth = ParameterHelper.CreateLengthRecord(finalDepth),
                    Height = ParameterHelper.CreateLengthRecord(height.value),
                    DefaultElevation = ParameterHelper.CreateLengthRecord(defaultElevation.value),
                    WidthRaw = width.value,
                    DepthRaw = depth.value,
                    HeightRaw = height.value,
                    DefaultElevationRaw = defaultElevation.value,
                    WidthString = width.valueString,
                    DepthString = depth.valueString,
                    HeightString = height.valueString,
                    DefaultElevationString = defaultElevation.valueString,
                    WidthSource = width.value != null ? width.source : "parsed_type_name",
                    DepthSource = depth.value != null ? depth.source : "parsed_type_name",
                    HeightSource = height.source,
                    DefaultElevationSource = defaultElevation.source,
                    WidthParam = width.paramName,
                    DepthParam = depth.paramName,
                    HeightParam = height.paramName,
                    DefaultElevationParam = defaultElevation.paramName,
                    ParsedWidthFeet = parsedWidth,
                    ParsedDepthFeet = parsedDepth
                },
                Classification = new UnitClassification
                {
                    ClimateRaw = climate.RawValue,
                    ClimateString = climate.valueString,
                    Climate = ParameterHelper.Boolish(climate.RawValue),
                    ClimateSource = climate.source,
                    ClimateParam = climate.paramName,

                    ClimateHeatOnlyRaw = climateHeatOnly.RawValue,
                    ClimateHeatOnlyString = climateHeatOnly.valueString,
                    ClimateHeatOnly = ParameterHelper.Boolish(climateHeatOnly.RawValue),
                    ClimateHeatOnlySource = climateHeatOnly.source,
                    ClimateHeatOnlyParam = climateHeatOnly.paramName,

                    DriveupRaw = driveup.RawValue,
                    DriveupString = driveup.valueString,
                    Driveup = ParameterHelper.Boolish(driveup.RawValue),
                    DriveupSource = driveup.source,
                    DriveupParam = driveup.paramName,

                    LockerRaw = locker.RawValue,
                    LockerString = locker.valueString,
                    Locker = ParameterHelper.Boolish(locker.RawValue),
                    LockerSource = locker.source,
                    LockerParam = locker.paramName,

                    GroundAccessRaw = groundAccess.RawValue,
                    GroundAccessString = groundAccess.valueString,
                    GroundAccess = ParameterHelper.Boolish(groundAccess.RawValue),
                    GroundAccessSource = groundAccess.source,
                    GroundAccessParam = groundAccess.paramName,

                    AccessibleRaw = accessible.RawValue,
                    AccessibleString = accessible.valueString,
                    Accessible = ParameterHelper.Boolish(accessible.RawValue),
                    AccessibleSource = accessible.source,
                    AccessibleParam = accessible.paramName,

                    ObstructionsRaw = obstructions.RawValue,
                    ObstructionsString = obstructions.valueString,
                    Obstructions = ParameterHelper.Boolish(obstructions.RawValue),
                    ObstructionsSource = obstructions.source,
                    ObstructionsParam = obstructions.paramName,

                    OfflineRaw = offline.RawValue,
                    OfflineString = offline.valueString,
                    Offline = ParameterHelper.Boolish(offline.RawValue),
                    OfflineSource = offline.source,
                    OfflineParam = offline.paramName,

                    PortableRaw = portable.RawValue,
                    PortableString = portable.valueString,
                    Portable = ParameterHelper.Boolish(portable.RawValue),
                    PortableSource = portable.source,
                    PortableParam = portable.paramName,

                    StackBottomRaw = stackBottom.RawValue,
                    StackBottomString = stackBottom.valueString,
                    StackBottom = ParameterHelper.Boolish(stackBottom.RawValue),
                    StackBottomSource = stackBottom.source,
                    StackBottomParam = stackBottom.paramName,

                    StackTopRaw = stackTop.RawValue,
                    StackTopString = stackTop.valueString,
                    StackTop = ParameterHelper.Boolish(stackTop.RawValue),
                    StackTopSource = stackTop.source,
                    StackTopParam = stackTop.paramName,

                    WalkupRaw = walkup.RawValue,
                    WalkupString = walkup.valueString,
                    Walkup = ParameterHelper.Boolish(walkup.RawValue),
                    WalkupSource = walkup.source,
                    WalkupParam = walkup.paramName
                },
                Placement = rotationDegrees.HasValue ? new PlacementData
                {
                    XFeet = (instance.Location as LocationPoint)?.Point.X ?? 0,
                    YFeet = (instance.Location as LocationPoint)?.Point.Y ?? 0,
                    ZFeet = (instance.Location as LocationPoint)?.Point.Z ?? 0,
                    RotationDegrees = rotationDegrees.value
                } : null,
                BoundingBox = GetBoundingBox(instance),
                Mirrored = instance.Mirrored,
                HandFlipped = instance.HandFlipped,
                FacingFlipped = instance.FacingFlipped,
                LevelId = ParameterHelper.GetElementIdValue(instance.LevelId),
                LevelName = levelName,
                LevelOffset = levelOffset.HasValue ? new LevelOffsetData
                {
                    Feet = levelOffset.value,
                    Inches = levelOffset.value * 12,
                    Source = levelOffsetSource,
                    ValueString = levelOffsetString
                } : null,
                DesignOption = designOptionName,
                Workset = GetWorksetName(instance),
                CreatedPhaseId = ParameterHelper.GetElementIdValue(instance.CreatedPhaseId),
                CreatedPhase = GetPhaseName(instance.CreatedPhaseId),
                DemolishedPhaseId = ParameterHelper.GetElementIdValue(instance.DemolishedPhaseId),
                DemolishedPhase = GetPhaseName(instance.DemolishedPhaseId),
                Parameters = ParameterHelper.GetAllParameters(instance, symbol)
            };
        }

        private Tuple<double?, double?> ParseSizeFromTypeName(string typeName)
        {
            // Match Python parse_size_from_text: "5x10", "5 x 10", etc.
            var match = Regex.Match(typeName, @"(\d+(?:\.\d+)?)\s*x\s*(\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                if (double.TryParse(match.Groups[1].value, out double w) &&
                    double.TryParse(match.Groups[2].value, out double d))
                {
                    return Tuple.Create<double?, double?>(w, d);
                }
            }
            return Tuple.Create<double?, double?>(null, null);
        }

        private string GetSourceOrigin(string familyName, string typeName)
        {
            // Match Python get_source_origin logic
            if (familyName.Contains("UX5")) return "center";
            if (familyName.Contains("UX4")) return "center";
            if (familyName.Contains("UX3")) return "front_left";
            return "center";
        }

        private string GetTargetOrigin(string familyName, string typeName)
        {
            return "front_left";  // Python always returns this for units
        }

        private string GetUxRole(string familyName, string typeName)
        {
            if (familyName.IndexOf("shell", StringComparison.OrdinalIgnoreCase) >= 0 ||
                typeName.IndexOf("shell", StringComparison.OrdinalIgnoreCase) >= 0)
                return "shell";
            return "unit";
        }

        private BoundingBoxData GetBoundingBox(FamilyInstance instance)
        {
            try
            {
                var bbox = instance.get_BoundingBox(null);
                if (bbox != null)
                {
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
            }
            catch { }
            return null;
        }

        private string GetWorksetName(Element element)
        {
            try
            {
                var worksetId = element.WorksetId;
                if (worksetId != null && worksetId != WorksetId.InvalidWorksetId)
                {
                    var workset = _doc.GetWorksetTable().GetWorkset(worksetId);
                    return workset?.Name;
                }
            }
            catch { }
            return null;
        }

        private string GetPhaseName(ElementId phaseId)
        {
            try
            {
                if (phaseId != null && phaseId != ElementId.InvalidElementId)
                {
                    var phase = _doc.GetElement(phaseId) as Phase;
                    return phase?.Name;
                }
            }
            catch { }
            return null;
        }
    }
}
