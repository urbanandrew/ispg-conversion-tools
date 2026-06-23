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
    /// Exports shell spaces to JSON matching Python LegacyExportShell script exactly
    /// </summary>
    public class ShellExporter
    {
        private readonly Document _doc;

        public ShellExporter(Document doc)
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

        private ShellExportPayload BuildPayload(IEnumerable<FamilyInstance> elements)
        {
            var records = new List<ShellRecord>();
            var skipped = new List<SkippedElement>();
            var roleCounts = new Dictionary<string, int>();
            var sourceKindCounts = new Dictionary<string, int>();
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

                    // Classify role (shell or wall)
                    string role = ClassifyRole(familyName, typeName, instance, familySymbol);
                    if (role == null)
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

                    // Skip legacy retail/server/corridor
                    if (ShouldSkipRecord(familyName, typeName, role))
                    {
                        skipped.Add(new SkippedElement
                        {
                            ElementId = ParameterHelper.GetElementIdValue(instance.Id),
                            FamilyName = familyName,
                            TypeName = typeName,
                            Reason = "skip_legacy_retail_server_or_corridor"
                        });
                        continue;
                    }

                    var record = BuildShellRecord(instance, familyName, typeName, role);
                    if (record != null)
                    {
                        records.Add(record);

                        // Count roles
                        if (!roleCounts.ContainsKey(role))
                            roleCounts[role] = 0;
                        roleCounts[role]++;

                        // Count source kinds
                        string sourceKind = record.Source.SourceKind ?? "unknown";
                        if (!sourceKindCounts.ContainsKey(sourceKind))
                            sourceKindCounts[sourceKind] = 0;
                        sourceKindCounts[sourceKind]++;

                        // Count source origins
                        string sourceOrigin = record.Source.SourceOrigin ?? "unknown";
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

            return new ShellExportPayload
            {
                ExportMetadata = new ExportMetadata
                {
                    ExportType = "shells",
                    ExportDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    RevitVersion = _doc.Application.VersionNumber,
                    DocumentTitle = _doc.Title,
                    DocumentPath = _doc.PathName
                },
                Shells = records,
                Skipped = skipped,
                Summary = new ExportSummary
                {
                    TotalProcessed = records.Count + skipped.Count,
                    SuccessfulExports = records.Count,
                    SkippedCount = skipped.Count,
                    RoleCounts = roleCounts,
                    SourceKindCounts = sourceKindCounts,
                    SourceOriginCounts = sourceOriginCounts
                }
            };
        }

        private string ClassifyRole(string familyName, string typeName, FamilyInstance instance, FamilySymbol symbol)
        {
            // Match Python classify_role logic
            
            // Check for UX5 Shell
            if (IsUx5Shell(familyName, typeName))
            {
                // Check Type Wall parameter
                var typeWall = ParameterHelper.GetFirstParamValue(instance, symbol, new List<string> { "Type Wall" });
                if (ParameterHelper.Boolish(typeWall.RawValue))
                    return "wall";
                return "shell";
            }

            // Check for UX4 Shell
            if (familyName.Contains("UX4 Shell") || familyName.Contains("UX4_Shell"))
                return "shell";

            // Check for UX3 Shell
            if (familyName.StartsWith("UX3 Shell", StringComparison.OrdinalIgnoreCase))
                return "shell";

            // Check for ISPG UX Shell
            if (familyName.Contains("ISPG UX Shell"))
                return "shell";

            // Check for walls
            if (familyName.StartsWith("UX5_Wall", StringComparison.OrdinalIgnoreCase) ||
                familyName.StartsWith("UX4 Wall", StringComparison.OrdinalIgnoreCase) ||
                familyName.StartsWith("UX3 Wall", StringComparison.OrdinalIgnoreCase))
                return "wall";

            return null;
        }

        private bool IsUx5Shell(string familyName, string typeName)
        {
            return familyName.StartsWith("UX5_Shell", StringComparison.OrdinalIgnoreCase);
        }

        private bool ShouldSkipRecord(string familyName, string typeName, string role)
        {
            // Match Python should_skip_record: skip UX3 Retail, UX3 Server, UX3 Corridor
            if (role == "shell")
            {
                string combined = (familyName + " " + typeName).ToLower();
                if (combined.Contains("retail") || combined.Contains("server") || combined.Contains("corridor"))
                {
                    if (familyName.StartsWith("UX3", StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            return false;
        }

        private ShellRecord BuildShellRecord(FamilyInstance instance, string familyName, string typeName, string role)
        {
            var symbol = instance.Symbol;

            // Extract parameters
            var buildingNumber = ParameterHelper.GetFirstParamValue(instance, symbol, ShellParameterHelper.BUILDING_NUMBER_PARAMS);
            var width = ParameterHelper.GetFirstParamValue(instance, symbol, ShellParameterHelper.WIDTH_PARAMS);
            var depth = ParameterHelper.GetFirstParamValue(instance, symbol, ShellParameterHelper.DEPTH_PARAMS);
            var height = ParameterHelper.GetFirstParamValue(instance, symbol, ShellParameterHelper.HEIGHT_PARAMS);
            var defaultElevation = ParameterHelper.GetFirstParamValue(instance, symbol, ShellParameterHelper.DEFAULT_ELEVATION_PARAMS);

            // Parse dimensions from type name if not found in parameters
            double? parsedWidth = null;
            double? parsedDepth = null;
            if (width.Value == null || depth.Value == null)
            {
                var parsed = ParseSizeFromTypeName(typeName);
                parsedWidth = parsed.Item1;
                parsedDepth = parsed.Item2;
            }

            double? finalWidth = width.Value ?? parsedWidth;
            double? finalDepth = depth.Value ?? parsedDepth;

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
            var levelOffset = ParameterHelper.GetParameterValueAsDouble(instance, "Level Offset");
            string levelOffsetSource = levelOffset.HasValue ? "Level Offset" : null;
            string levelOffsetString = levelOffset.HasValue ? levelOffset.Value.ToString() : null;

            // Get design option
            string designOptionName = null;
            try
            {
                var designOption = instance.DesignOption;
                if (designOption != null && designOption.Id != ElementId.InvalidElementId)
                    designOptionName = designOption.Name;
            }
            catch { }

            // Determine source kind and origin
            string sourceKind = role == "shell" ? GetShellSourceKind(familyName, typeName) : GetWallSourceKind(familyName, typeName);
            string sourceOrigin = GetOriginProfile(role, familyName, typeName);

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

            return new ShellRecord
            {
                Source = new ShellSourceData
                {
                    RevitVersion = "2022_plus_ux5_shell",
                    DocumentTitle = _doc.Title,
                    DocumentPath = _doc.PathName,
                    ElementId = ParameterHelper.GetElementIdValue(instance.Id),
                    UniqueId = instance.UniqueId,
                    FamilyName = familyName,
                    TypeName = typeName,
                    Role = role,
                    SourceKind = sourceKind,
                    SourceOrigin = sourceOrigin,
                    TargetFamilyHint = "UX5_Shell",
                    TargetTypeHint = "UX5_Shell"
                },
                MigrationAssumptions = new ShellMigrationAssumptions
                {
                    SourceOrigin = sourceOrigin,
                    TargetOrigin = "front_left_corner",
                    WidthDirectionBasis = "family_local_x_assumed_width_direction",
                    DepthDirectionBasis = "family_local_y_assumed_depth_direction"
                },
                Identity = new ShellIdentity
                {
                    BuildingNumber = buildingNumber.IntValue,
                    BuildingNumberString = buildingNumber.StringValue,
                    BuildingNumberSource = buildingNumber.Source,
                    BuildingNumberParam = buildingNumber.ParameterName
                },
                Dimensions = new ShellDimensions
                {
                    Width = ParameterHelper.CreateLengthRecord(finalWidth),
                    Depth = ParameterHelper.CreateLengthRecord(finalDepth),
                    Height = ParameterHelper.CreateLengthRecord(height.Value),
                    DefaultElevation = ParameterHelper.CreateLengthRecord(defaultElevation.Value),
                    WidthRaw = width.Value,
                    DepthRaw = depth.Value,
                    HeightRaw = height.Value,
                    DefaultElevationRaw = defaultElevation.Value,
                    WidthString = width.StringValue,
                    DepthString = depth.StringValue,
                    HeightString = height.StringValue,
                    DefaultElevationString = defaultElevation.StringValue,
                    WidthSource = width.Value != null ? width.Source : "parsed_type_name",
                    DepthSource = depth.Value != null ? depth.Source : "parsed_type_name",
                    HeightSource = height.Source,
                    DefaultElevationSource = defaultElevation.Source,
                    WidthParam = width.ParameterName,
                    DepthParam = depth.ParameterName,
                    HeightParam = height.ParameterName,
                    DefaultElevationParam = defaultElevation.ParameterName,
                    ParsedWidthFeet = parsedWidth,
                    ParsedDepthFeet = parsedDepth
                },
                Flags = BuildFlagRecords(instance, symbol, familyName, typeName, role),
                Placement = rotationDegrees.HasValue ? new PlacementData
                {
                    XFeet = (instance.Location as LocationPoint)?.Point.X ?? 0,
                    YFeet = (instance.Location as LocationPoint)?.Point.Y ?? 0,
                    ZFeet = (instance.Location as LocationPoint)?.Point.Z ?? 0,
                    RotationDegrees = rotationDegrees.Value
                } : null,
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
                CreatedPhaseId = ParameterHelper.GetElementIdValue(instance.CreatedPhaseId),
                CreatedPhase = GetPhaseName(instance.CreatedPhaseId),
                DemolishedPhaseId = ParameterHelper.GetElementIdValue(instance.DemolishedPhaseId),
                DemolishedPhase = GetPhaseName(instance.DemolishedPhaseId),
                Parameters = ParameterHelper.GetAllParameters(instance, symbol)
            };
        }

        private Dictionary<string, FlagRecord> BuildFlagRecords(FamilyInstance instance, FamilySymbol symbol, string familyName, string typeName, string role)
        {
            // Match Python build_flag_records logic
            if (IsUx5Shell(familyName, typeName))
                return BuildUx5ShellFlags(instance, symbol, role);

            var flags = new Dictionary<string, FlagRecord>();

            if (role == "wall")
            {
                flags["Type Wall"] = CreateFlagRecord("Type Wall", true, "role_is_wall");
                flags["Type Corridor"] = CreateFlagRecord("Type Corridor", false, "legacy_always_false");
                return flags;
            }

            // role == "shell"
            flags["Type Wall"] = CreateFlagRecord("Type Wall", false, "role_is_shell");
            flags["Type Corridor"] = CreateFlagRecord("Type Corridor", false, "legacy_always_false");

            string sourceKind = GetShellSourceKind(familyName, typeName);

            if (sourceKind == "ux4_shell")
            {
                var sourceFlags = BuildUx4IconShellFlags(instance, symbol);
                foreach (var kvp in sourceFlags)
                    flags[kvp.Key] = kvp.Value;
            }
            else if (sourceKind == "ux3_shell" || sourceKind == "ispg_ux_shell")
            {
                var sourceFlags = BuildNameBasedShellFlags(familyName, typeName);
                foreach (var kvp in sourceFlags)
                    flags[kvp.Key] = kvp.Value;
            }

            return flags;
        }

        private Dictionary<string, FlagRecord> BuildUx5ShellFlags(FamilyInstance instance, FamilySymbol symbol, string role)
        {
            var flags = new Dictionary<string, FlagRecord>();

            foreach (var targetParam in ShellParameterHelper.UX5_SHELL_FLAG_PARAMS)
            {
                var paramValue = ParameterHelper.GetFirstParamValue(instance, symbol, new List<string> { targetParam });
                bool value = ParameterHelper.Boolish(paramValue.RawValue);

                // Special handling for Type Wall based on role
                if (targetParam == "Type Wall" && paramValue.RawValue == null)
                {
                    value = (role == "wall");
                }
                else if (paramValue.RawValue == null)
                {
                    value = false;
                }

                flags[targetParam] = new FlagRecord
                {
                    Name = targetParam,
                    Value = value,
                    Source = paramValue.Source ?? (targetParam == "Type Wall" && role == "wall" ? "role_is_wall" : "default_false"),
                    SourceParam = paramValue.ParameterName,
                    Raw = paramValue.RawValue,
                    ValueString = paramValue.StringValue
                };
            }

            return flags;
        }

        private Dictionary<string, FlagRecord> BuildUx4IconShellFlags(FamilyInstance instance, FamilySymbol symbol)
        {
            var flags = new Dictionary<string, FlagRecord>();

            foreach (var kvp in ShellParameterHelper.UX4_ICON_PARAMS_TO_FLAGS)
            {
                string sourceParam = kvp.Key;
                string targetFlag = kvp.Value;

                var paramValue = ParameterHelper.GetFirstParamValue(instance, symbol, new List<string> { sourceParam });
                bool value = ParameterHelper.Boolish(paramValue.RawValue);

                flags[targetFlag] = new FlagRecord
                {
                    Name = targetFlag,
                    Value = value,
                    Source = paramValue.Source ?? "default_false",
                    SourceParam = sourceParam,
                    Raw = paramValue.RawValue,
                    ValueString = paramValue.StringValue
                };
            }

            return flags;
        }

        private Dictionary<string, FlagRecord> BuildNameBasedShellFlags(string familyName, string typeName)
        {
            // Match Python build_name_based_shell_flags: parse type name for keywords
            var flags = new Dictionary<string, FlagRecord>();
            string combined = (familyName + " " + typeName).ToLower();

            var flagMappings = new Dictionary<string, string[]>
            {
                { "Type Elevator", new[] { "elevator", "elev" } },
                { "Type Stair", new[] { "stair" } },
                { "Type Restroom", new[] { "restroom", "toilet" } },
                { "Type Trash", new[] { "trash" } },
                { "Type Mechanical", new[] { "mechanical", "mech" } },
                { "Type Office", new[] { "office" } }
            };

            foreach (var mapping in flagMappings)
            {
                string flagName = mapping.Key;
                bool found = mapping.Value.Any(keyword => combined.Contains(keyword));
                flags[flagName] = CreateFlagRecord(flagName, found, found ? "parsed_from_name" : "default_false");
            }

            return flags;
        }

        private FlagRecord CreateFlagRecord(string name, bool value, string source, object raw = null, string valueString = null, string sourceParam = null)
        {
            return new FlagRecord
            {
                Name = name,
                Value = value,
                Source = source,
                SourceParam = sourceParam,
                Raw = raw,
                ValueString = valueString
            };
        }

        private string GetShellSourceKind(string familyName, string typeName)
        {
            if (IsUx5Shell(familyName, typeName)) return "ux5_shell";
            if (familyName.Contains("UX4 Shell") || familyName.Contains("UX4_Shell")) return "ux4_shell";
            if (familyName.StartsWith("UX3 Shell", StringComparison.OrdinalIgnoreCase)) return "ux3_shell";
            if (familyName.Contains("ISPG UX Shell")) return "ispg_ux_shell";
            return null;
        }

        private string GetWallSourceKind(string familyName, string typeName)
        {
            if (familyName.StartsWith("UX5_Wall", StringComparison.OrdinalIgnoreCase)) return "ux5_wall";
            if (familyName.StartsWith("UX4 Wall", StringComparison.OrdinalIgnoreCase)) return "ux4_wall";
            if (familyName.StartsWith("UX3 Wall", StringComparison.OrdinalIgnoreCase)) return "ux3_wall";
            return null;
        }

        private string GetOriginProfile(string role, string familyName, string typeName)
        {
            // Match Python get_origin_profile
            if (role == "wall")
            {
                if (familyName.Contains("UX5")) return "length_left_end_depth_front";
                if (familyName.Contains("UX4")) return "length_left_end_depth_front";
                if (familyName.Contains("UX3")) return "length_left_end_depth_front";
                return "length_left_end_depth_front";
            }

            // role == "shell"
            if (familyName.Contains("UX5")) return "front_left_corner";
            if (familyName.Contains("UX4")) return "center";
            if (familyName.Contains("UX3")) return "center";
            if (familyName.Contains("ISPG UX Shell")) return "center";
            return "center";
        }

        private Tuple<double?, double?> ParseSizeFromTypeName(string typeName)
        {
            // Match Python parse_size_from_text: "5x10", "5 x 10", etc.
            var match = Regex.Match(typeName, @"(\d+(?:\.\d+)?)\s*x\s*(\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                if (double.TryParse(match.Groups[1].Value, out double w) &&
                    double.TryParse(match.Groups[2].Value, out double d))
                {
                    return Tuple.Create<double?, double?>(w, d);
                }
            }
            return Tuple.Create<double?, double?>(null, null);
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
