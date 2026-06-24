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
    /// Exports parking spaces to JSON matching Python LegacyExportParking script exactly
    /// </summary>
    public class ParkingExporter
    {
        private readonly Document _doc;

        public ParkingExporter(Document doc)
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

        private ParkingExportPayload BuildPayload(IEnumerable<FamilyInstance> elements)
        {
            var elementsList = elements.ToList();
            var records = new List<ParkingRecord>();
            var skipped = new List<SkippedRecord>();
            var matchReasonCounts = new Dictionary<string, int>();
            var sourceOriginCounts = new Dictionary<string, int>();

            foreach (var element in elementsList)
            {
                try
                {
                    var record = ExportElement(element);
                    if (record != null)
                    {
                        records.Add(record);

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
                    skipped.Add(new SkippedRecord
                    {
                        ElementId = ParameterHelper.GetElementIdValue(element.Id) ?? -1,
                        FamilyName = element.Symbol?.FamilyName ?? "Unknown",
                        TypeName = element.Symbol?.Name ?? "Unknown",
                        Reason = $"Export failed: {ex.Message}"
                    });
                }
            }

            return new ParkingExportPayload
            {
                Schema = new SchemaInfo
                {
                    Name = "umx_revit_2022_plus_ux5_parking_extract",
                    Version = 4
                },
                ExportedAt = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
                Document = new DocumentInfo
                {
                    Title = _doc.Title,
                    Path = _doc.PathName
                },
                Filters = new ParkingFilterInfo
                {
                    ParkingFamilyNameContains = ParkingParameterHelper.PARKING_FAMILY_NAME_CONTAINS.ToList(),
                    MatchByParkingParameters = true,
                    ParkingNumberParamNames = ParkingParameterHelper.PARKING_NUMBER_PARAM_NAMES.ToList(),
                    BuildingNumberParamNames = ParkingParameterHelper.BUILDING_NUMBER_PARAM_NAMES.ToList(),
                    WidthParamNames = ParkingParameterHelper.WIDTH_PARAM_NAMES.ToList(),
                    DepthParamNames = ParkingParameterHelper.DEPTH_PARAM_NAMES.ToList(),
                    ParkingAngleParamNames = ParkingParameterHelper.PARKING_ANGLE_PARAM_NAMES.ToList()
                },
                Counts = new CountInfo
                {
                    Exported = records.Count,
                    SkippedGenericModels = skipped.Count,
                    MatchReasonCounts = matchReasonCounts,
                    SourceOriginCounts = sourceOriginCounts
                },
                Records = records,
                Skipped = skipped
            };
        }

        private ParkingRecord ExportElement(FamilyInstance instance)
        {
            var symbol = instance.Symbol;
            var familyName = symbol?.FamilyName ?? "";
            var typeName = symbol?.Name ?? "";

            // Get all parking parameters using the dedicated parameter helper
            var (parkingNumber, parkingNumberString, parkingNumberSource, parkingNumberParam) =
                ParameterHelper.GetFirstParamValue(instance, symbol, ParkingParameterHelper.PARKING_NUMBER_PARAM_NAMES);

            var (buildingNumber, buildingNumberString, buildingNumberSource, buildingNumberParam) =
                ParameterHelper.GetFirstParamValue(instance, symbol, ParkingParameterHelper.BUILDING_NUMBER_PARAM_NAMES);

            var (width, widthString, widthSource, widthParam) =
                ParameterHelper.GetFirstParamValue(instance, symbol, ParkingParameterHelper.WIDTH_PARAM_NAMES);

            var (depth, depthString, depthSource, depthParam) =
                ParameterHelper.GetFirstParamValue(instance, symbol, ParkingParameterHelper.DEPTH_PARAM_NAMES);

            var (defaultElevation, defaultElevationString, defaultElevationSource, defaultElevationParam) =
                ParameterHelper.GetFirstParamValue(instance, symbol, ParkingParameterHelper.DEFAULT_ELEVATION_PARAM_NAMES);

            var (parkingAngle, parkingAngleString, parkingAngleSource, parkingAngleParam) =
                ParameterHelper.GetFirstParamValue(instance, symbol, ParkingParameterHelper.PARKING_ANGLE_PARAM_NAMES);

            var (parkingCovered, parkingCoveredString, parkingCoveredSource, parkingCoveredParam) =
                ParameterHelper.GetFirstParamValue(instance, symbol, ParkingParameterHelper.PARKING_COVERED_PARAM_NAMES);

            var (parkingRentable, parkingRentableString, parkingRentableSource, parkingRentableParam) =
                ParameterHelper.GetFirstParamValue(instance, symbol, ParkingParameterHelper.PARKING_RENTABLE_PARAM_NAMES);

            var (parkingAccessible, parkingAccessibleString, parkingAccessibleSource, parkingAccessibleParam) =
                ParameterHelper.GetFirstParamValue(instance, symbol, ParkingParameterHelper.PARKING_ACCESSIBLE_PARAM_NAMES);

            var (parkingAccessibleVan, parkingAccessibleVanString, parkingAccessibleVanSource, parkingAccessibleVanParam) =
                ParameterHelper.GetFirstParamValue(instance, symbol, ParkingParameterHelper.PARKING_ACCESSIBLE_VAN_PARAM_NAMES);

            var (parkingCompact, parkingCompactString, parkingCompactSource, parkingCompactParam) =
                ParameterHelper.GetFirstParamValue(instance, symbol, ParkingParameterHelper.PARKING_COMPACT_PARAM_NAMES);

            var (parkingElectricVehicle, parkingElectricVehicleString, parkingElectricVehicleSource, parkingElectricVehicleParam) =
                ParameterHelper.GetFirstParamValue(instance, symbol, ParkingParameterHelper.PARKING_ELECTRIC_VEHICLE_PARAM_NAMES);

            var (infoLocker, infoLockerString, infoLockerSource, infoLockerParam) =
                ParameterHelper.GetFirstParamValue(instance, symbol, ParkingParameterHelper.INFO_LOCKER_PARAM_NAMES);

            var (infoPortable, infoPortableString, infoPortableSource, infoPortableParam) =
                ParameterHelper.GetFirstParamValue(instance, symbol, ParkingParameterHelper.INFO_PORTABLE_PARAM_NAMES);

            // Get level info
            Level level = null;
            string levelName = null;
            try
            {
                var levelId = instance.LevelId;
                level = _doc.GetElement(levelId) as Level;
                levelName = level?.Name;
            }
            catch { }

            // Get design option
            string designOptionName = null;
            try
            {
                designOptionName = instance.DesignOption != null ? instance.DesignOption.Name : null;
            }
            catch { }

            // Determine match reason and source origin
            string matchReason = "family_name_contains_parking_space";
            string sourceOrigin = DetermineSourceOrigin(familyName, typeName);

            // Get location
            var location = instance.Location as LocationPoint;
            var position = location?.Point ?? new XYZ(0, 0, 0);  // Always provide a position, default to origin if null
            var rotationRadians = OriginCalculator.GetRotationRadians(location) ?? 0.0;
            var rotationDegrees = OriginCalculator.RadiansToDegrees(rotationRadians);

            // Get level offset
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
                else if (defaultElevation is double elevValue)
                {
                    levelOffset = elevValue;
                    levelOffsetSource = "default_elevation";
                    levelOffsetString = defaultElevationString;
                }
            }

            // Convert width/depth to doubles
            double? widthRaw = width as double?;
            if (!widthRaw.HasValue && width is int widthInt)
                widthRaw = (double)widthInt;

            double? depthRaw = depth as double?;
            if (!depthRaw.HasValue && depth is int depthInt)
                depthRaw = (double)depthInt;

            double? elevationRaw = defaultElevation as double?;
            if (!elevationRaw.HasValue && defaultElevation is int elevInt)
                elevationRaw = (double)elevInt;

            return new ParkingRecord
            {
                Source = new ParkingSourceInfo
                {
                    RevitVersion = "2022_plus_ux5_parking",
                    DocumentTitle = _doc.Title,
                    DocumentPath = _doc.PathName,
                    ElementId = ParameterHelper.GetElementIdValue(instance.Id),
                    UniqueId = instance.UniqueId,
                    FamilyName = familyName,
                    TypeName = typeName,
                    Role = "parking",
                    MatchReason = matchReason,
                    SourceOrigin = sourceOrigin,
                    TargetFamilyHint = "UX5 Parking Space",
                    TargetTypeHint = "UX5 Parking Space"
                },
                MigrationAssumptions = new MigrationAssumptions
                {
                    SourceOrigin = sourceOrigin,
                    TargetOrigin = "front_left_corner",
                    WidthDirectionBasis = "family_local_x_assumed_width_direction",
                    DepthDirectionBasis = "family_local_y_assumed_depth_direction"
                },
                Identity = new ParkingIdentityInfo
                {
                    ParkingNumber = parkingNumber,
                    ParkingNumberString = parkingNumberString,
                    ParkingNumberSource = parkingNumberSource,
                    ParkingNumberParam = parkingNumberParam,
                    BuildingNumber = buildingNumber,
                    BuildingNumberString = buildingNumberString,
                    BuildingNumberSource = buildingNumberSource,
                    BuildingNumberParam = buildingNumberParam
                },
                Dimensions = new DimensionsInfo
                {
                    Width = ParameterHelper.CreateLengthRecord(widthRaw),
                    Depth = ParameterHelper.CreateLengthRecord(depthRaw),
                    DefaultElevation = ParameterHelper.CreateLengthRecord(elevationRaw),
                    WidthRaw = widthRaw,
                    DepthRaw = depthRaw,
                    DefaultElevationRaw = elevationRaw,
                    WidthString = widthString,
                    DepthString = depthString,
                    DefaultElevationString = defaultElevationString,
                    WidthSource = widthSource,
                    DepthSource = depthSource,
                    DefaultElevationSource = defaultElevationSource,
                    WidthParam = widthParam,
                    DepthParam = depthParam,
                    DefaultElevationParam = defaultElevationParam
                },
                Parking = new ParkingClassificationInfo
                {
                    Angle = parkingAngle,
                    AngleString = parkingAngleString,
                    AngleSource = parkingAngleSource,
                    AngleParam = parkingAngleParam,
                    CoveredRaw = parkingCovered,
                    CoveredString = parkingCoveredString,
                    Covered = ConvertToBool(parkingCovered),
                    CoveredSource = parkingCoveredSource,
                    CoveredParam = parkingCoveredParam,
                    Rentable = CreateBoolRecord(parkingRentable, parkingRentableString, parkingRentableSource, parkingRentableParam),
                    Accessible = CreateBoolRecord(parkingAccessible, parkingAccessibleString, parkingAccessibleSource, parkingAccessibleParam),
                    AccessibleVan = CreateBoolRecord(parkingAccessibleVan, parkingAccessibleVanString, parkingAccessibleVanSource, parkingAccessibleVanParam),
                    Compact = CreateBoolRecord(parkingCompact, parkingCompactString, parkingCompactSource, parkingCompactParam),
                    ElectricVehicle = CreateBoolRecord(parkingElectricVehicle, parkingElectricVehicleString, parkingElectricVehicleSource, parkingElectricVehicleParam),
                    InfoLocker = CreateBoolRecord(infoLocker, infoLockerString, infoLockerSource, infoLockerParam),
                    InfoPortable = CreateBoolRecord(infoPortable, infoPortableString, infoPortableSource, infoPortableParam)
                },
                Placement = new PlacementInfo
                {
                    // ALWAYS create Location with coordinates (never null)
                    Location = new LocationData
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
                    },
                    BoundingBox = GetBoundingBox(instance),
                    Mirrored = instance.Mirrored,
                    HandFlipped = instance.HandFlipped,
                    FacingFlipped = instance.FacingFlipped,
                    LevelId = ParameterHelper.GetElementIdValue(instance.LevelId),
                    LevelName = levelName,
                    LevelOffset = levelOffset.HasValue ? new LevelOffsetData
                    {
                        Feet = levelOffset.Value,
                        Inches = levelOffset.Value * 12,
                        Source = levelOffsetSource,
                        ValueString = levelOffsetString
                    } : null,
                    DesignOption = designOptionName,
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

        private string DetermineSourceOrigin(string familyName, string typeName)
        {
            // Match Python's get_source_origin logic
            if (familyName.Contains("UX5"))
                return "front_left_corner";
            return "center";
        }

        private BoolRecord CreateBoolRecord(object value, string valueString, string source, string param)
        {
            return new BoolRecord
            {
                Value = value,
                ValueString = valueString,
                Bool = ConvertToBool(value),
                Source = source,
                Param = param
            };
        }

        private bool? ConvertToBool(object value)
        {
            if (value == null) return null;
            
            // Handle integers (0/1)
            if (value is int intVal)
                return intVal != 0;
            
            // Handle strings ("Yes"/"No", "True"/"False", "1"/"0")
            if (value is string strVal)
            {
                strVal = strVal.Trim().ToLowerInvariant();
                if (strVal == "yes" || strVal == "true" || strVal == "1")
                    return true;
                if (strVal == "no" || strVal == "false" || strVal == "0")
                    return false;
                return null;
            }
            
            // Try direct boolean conversion
            if (value is bool boolVal)
                return boolVal;
            
            return null;
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
