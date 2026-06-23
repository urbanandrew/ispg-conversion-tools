     1|using System;
     2|using System.Collections.Generic;
     3|using System.IO;
     4|using System.Linq;
     5|using System.Text.RegularExpressions;
     6|using Autodesk.Revit.DB;
     7|using Newtonsoft.Json;
     8|using ISPG.Conversion.Models;
     9|
    10|namespace ISPG.Conversion.Core
    11|{
    12|    /// <summary>
    13|    /// Exports unit spaces to JSON matching Python LegacyExportUnits script exactly
    14|    /// </summary>
    15|    public class UnitExporter
    16|    {
    17|        private readonly Document _doc;
    18|
    19|        // Family name filters matching Python UNIT_FAMILY_NAME_CONTAINS and UNIT_FAMILY_NAME_STARTS_WITH
    20|        private static readonly string[] CONTAINS_PATTERNS = { "ISPG UX Shell", "ISPG UX Unit", "UX4 Unit" };
    21|        private static readonly string[] STARTS_WITH_PATTERNS = { "UX3 Unit", "UX5_Unit" };
    22|
    23|        public UnitExporter(Document doc)
    24|        {
    25|            _doc = doc;
    26|        }
    27|
    28|        public string Export(IEnumerable<FamilyInstance> elements, string filePath)
    29|        {
    30|            var payload = BuildPayload(elements);
    31|            var json = JsonConvert.SerializeObject(payload, Formatting.Indented);
    32|
    33|            File.WriteAllText(filePath, json);
    34|
    35|            return filePath;
    36|        }
    37|
    38|        private UnitExportPayload BuildPayload(IEnumerable<FamilyInstance> elements)
    39|        {
    40|            var records = new List<UnitExportRecord>();
    41|            var skipped = new List<SkippedElement>();
    42|            var matchReasonCounts = new Dictionary<string, int>();
    43|            var sourceOriginCounts = new Dictionary<string, int>();
    44|
    45|            foreach (var instance in elements)
    46|            {
    47|                try
    48|                {
    49|                    var familySymbol = instance.Symbol;
    50|                    if (familySymbol == null) continue;
    51|
    52|                    var family = familySymbol.Family;
    53|                    if (family == null) continue;
    54|
    55|                    string familyName = family.Name ?? "";
    56|                    string typeName = familySymbol.Name ?? "";
    57|
    58|                    // Match against Python patterns
    59|                    string matchReason = GetMatchReason(familyName);
    60|                    if (matchReason == null)
    61|                    {
    62|                        skipped.Add(new SkippedElement
    63|                        {
    64|                            ElementId = ParameterHelper.GetElementIdValue(instance.Id),
    65|                            FamilyName = familyName,
    66|                            TypeName = typeName,
    67|                            Reason = "family_type_filter"
    68|                        });
    69|                        continue;
    70|                    }
    71|
    72|                    var record = BuildUnitRecord(instance, familyName, typeName);
    73|                    if (record != null)
    74|                    {
    75|                        records.Add(record);
    76|
    77|                        // Count match reasons
    78|                        string reason = record.source.MatchReason ?? "unknown";
    79|                        if (!matchReasonCounts.ContainsKey(reason))
    80|                            matchReasonCounts[reason] = 0;
    81|                        matchReasonCounts[reason]++;
    82|
    83|                        // Count source origins
    84|                        string sourceOrigin = record.source.sourceOrigin ?? "unknown";
    85|                        if (!sourceOriginCounts.ContainsKey(sourceOrigin))
    86|                            sourceOriginCounts[sourceOrigin] = 0;
    87|                        sourceOriginCounts[sourceOrigin]++;
    88|                    }
    89|                }
    90|                catch (Exception ex)
    91|                {
    92|                    skipped.Add(new SkippedElement
    93|                    {
    94|                        ElementId = ParameterHelper.GetElementIdValue(instance.Id),
    95|                        Reason = $"exception: {ex.Message}"
    96|                    });
    97|                }
    98|            }
    99|
   100|            return new UnitExportPayload
   101|            {
   102|                ExportMetadata = new ExportMetadata
   103|                {
   104|                    ExportType = "units",
   105|                    ExportDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
   106|                    RevitVersion = _doc.Application.VersionNumber,
   107|                    DocumentTitle = _doc.Title,
   108|                    DocumentPath = _doc.PathName
   109|                },
   110|                Units = records,
   111|                Skipped = skipped,
   112|                Summary = new ExportSummary
   113|                {
   114|                    TotalProcessed = records.Count + skipped.Count,
   115|                    SuccessfulExports = records.Count,
   116|                    SkippedCount = skipped.Count,
   117|                    MatchReasonCounts = matchReasonCounts,
   118|                    SourceOriginCounts = sourceOriginCounts
   119|                }
   120|            };
   121|        }
   122|
   123|        private string GetMatchReason(string familyName)
   124|        {
   125|            foreach (var pattern in CONTAINS_PATTERNS)
   126|            {
   127|                if (familyName.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0)
   128|                    return $"contains_{pattern.Replace(" ", "_")}";
   129|            }
   130|
   131|            foreach (var pattern in STARTS_WITH_PATTERNS)
   132|            {
   133|                if (familyName.StartsWith(pattern, StringComparison.OrdinalIgnoreCase))
   134|                    return $"starts_with_{pattern.Replace(" ", "_")}";
   135|            }
   136|
   137|            return null;
   138|        }
   139|
   140|        private UnitExportRecord BuildUnitRecord(FamilyInstance instance, string familyName, string typeName)
   141|        {
   142|            var symbol = instance.Symbol;
   143|
            // Determine match reason (simplified for now - full pattern matching logic TBD)
            string matchReason = "unit_parameters";

   144|            // Extract all parameters using Python's first_param_value pattern
   145|            var buildingNumber = ParameterHelper.GetFirstParamValue(instance, symbol, UnitParameterHelper.BUILDING_NUMBER_PARAMS);
   146|            var unitNumber = ParameterHelper.GetFirstParamValue(instance, symbol, UnitParameterHelper.UNIT_NUMBER_PARAMS);
   147|            var width = ParameterHelper.GetFirstParamValue(instance, symbol, UnitParameterHelper.WIDTH_PARAMS);
   148|            var depth = ParameterHelper.GetFirstParamValue(instance, symbol, UnitParameterHelper.DEPTH_PARAMS);
   149|            var height = ParameterHelper.GetFirstParamValue(instance, symbol, UnitParameterHelper.HEIGHT_PARAMS);
   150|            var defaultElevation = ParameterHelper.GetFirstParamValue(instance, symbol, UnitParameterHelper.DEFAULT_ELEVATION_PARAMS);
   151|
   152|            // Classification parameters
   153|            var climate = ParameterHelper.GetFirstParamValue(instance, symbol, UnitParameterHelper.CLIMATE_PARAMS);
   154|            var climateHeatOnly = ParameterHelper.GetFirstParamValue(instance, symbol, UnitParameterHelper.CLIMATE_HEAT_ONLY_PARAMS);
   155|            var driveup = ParameterHelper.GetFirstParamValue(instance, symbol, UnitParameterHelper.DRIVEUP_PARAMS);
   156|            var locker = ParameterHelper.GetFirstParamValue(instance, symbol, UnitParameterHelper.LOCKER_PARAMS);
   157|            var groundAccess = ParameterHelper.GetFirstParamValue(instance, symbol, UnitParameterHelper.GROUND_ACCESS_PARAMS);
   158|            var accessible = ParameterHelper.GetFirstParamValue(instance, symbol, UnitParameterHelper.ACCESSIBLE_PARAMS);
   159|            var obstructions = ParameterHelper.GetFirstParamValue(instance, symbol, UnitParameterHelper.OBSTRUCTIONS_PARAMS);
   160|            var offline = ParameterHelper.GetFirstParamValue(instance, symbol, UnitParameterHelper.OFFLINE_PARAMS);
   161|            var portable = ParameterHelper.GetFirstParamValue(instance, symbol, UnitParameterHelper.PORTABLE_PARAMS);
   162|            var stackBottom = ParameterHelper.GetFirstParamValue(instance, symbol, UnitParameterHelper.STACK_BOTTOM_PARAMS);
   163|            var stackTop = ParameterHelper.GetFirstParamValue(instance, symbol, UnitParameterHelper.STACK_TOP_PARAMS);
   164|            var walkup = ParameterHelper.GetFirstParamValue(instance, symbol, UnitParameterHelper.WALKUP_PARAMS);
   165|
   166|            // Parse dimensions from type name if not found in parameters
   167|            double? parsedWidth = null;
   168|            double? parsedDepth = null;
   169|            if (width.value == null || depth.value == null)
   170|            {
   171|                var parsed = ParseSizeFromTypeName(typeName);
   172|                parsedWidth = parsed.Item1;
   173|                parsedDepth = parsed.Item2;
   174|            }
   175|
   176|            double? finalWidth = width.value ?? parsedWidth;
   177|            double? finalDepth = depth.value ?? parsedDepth;
   178|
   179|            // Get level info
   180|            string levelName = null;
   181|            try
   182|            {
   183|                if (instance.LevelId != null && instance.LevelId != ElementId.InvalidElementId)
   184|                {
   185|                    var level = _doc.GetElement(instance.LevelId) as Level;
   186|                    levelName = level?.Name;
   187|                }
   188|            }
   189|            catch { }
   190|
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
   195|
   196|            // Get design option
   197|            string designOptionName = null;
   198|            try
   199|            {
   200|                var designOption = instance.DesignOption;
   201|                if (designOption != null && designOption.Id != ElementId.InvalidElementId)
   202|                    designOptionName = designOption.Name;
   203|            }
   204|            catch { }
   205|
   206|            // Get rotation
   207|            double? rotationDegrees = null;
   208|            try
   209|            {
   210|                var locationPoint = instance.Location as LocationPoint;
   211|                if (locationPoint != null)
   212|                {
   213|                    double rotationRadians = locationPoint.Rotation;
   214|                    rotationDegrees = rotationRadians * (180.0 / Math.PI);
   215|                }
   216|            }
   217|            catch { }
   218|
   219|            string sourceOrigin = GetSourceOrigin(familyName, typeName);
   220|            string targetOrigin = GetTargetOrigin(familyName, typeName);
   221|
   222|            return new UnitExportRecord
   223|            {
   224|                Source = new SourceData
   225|                {
   226|                    RevitVersion = "2022_plus_ux5_unit",
   227|                    DocumentTitle = _doc.Title,
   228|                    DocumentPath = _doc.PathName,
   229|                    ElementId = ParameterHelper.GetElementIdValue(instance.Id),
   230|                    UniqueId = instance.UniqueId,
   231|                    FamilyName = familyName,
   232|                    TypeName = typeName,
   233|                    UxRole = GetUxRole(familyName, typeName),
   234|                    MatchReason = matchReason,
   235|                    SourceOrigin = sourceOrigin
   236|                },
   237|                MigrationAssumptions = new MigrationAssumptions
   238|                {
   239|                    OldOrigin = sourceOrigin,
   240|                    SourceOrigin = sourceOrigin,
   241|                    TargetOrigin = targetOrigin,
   242|                    WidthDirectionBasis = "family_local_x_assumed_width_direction",
   243|                    DepthDirectionBasis = "family_local_y_assumed_depth_direction",
   244|                    LegacyCenterToTargetFrontLeftWidthFactor = -0.5,
   245|                    LegacyCenterToTargetFrontLeftDepthFactor = -0.5
   246|                },
   247|                Identity = new UnitIdentity
   248|                {
   249|                    BuildingNumber = buildingNumber.value,
   250|                    BuildingNumberString = buildingNumber.valueString,
   251|                    BuildingNumberSource = buildingNumber.source,
   252|                    BuildingNumberParam = buildingNumber.paramName,
   253|                    UnitNumber = unitNumber.valueString,
   254|                    UnitNumberString = unitNumber.valueString,
   255|                    UnitNumberSource = unitNumber.source,
   256|                    UnitNumberParam = unitNumber.paramName
   257|                },
   258|                Dimensions = new UnitDimensions
   259|                {
   260|                    Width = ParameterHelper.CreateLengthRecord(finalWidth),
   261|                    Depth = ParameterHelper.CreateLengthRecord(finalDepth),
   262|                    Height = ParameterHelper.CreateLengthRecord(height.value),
   263|                    DefaultElevation = ParameterHelper.CreateLengthRecord(defaultElevation.value),
   264|                    WidthRaw = width.value,
   265|                    DepthRaw = depth.value,
   266|                    HeightRaw = height.value,
   267|                    DefaultElevationRaw = defaultElevation.value,
   268|                    WidthString = width.valueString,
   269|                    DepthString = depth.valueString,
   270|                    HeightString = height.valueString,
   271|                    DefaultElevationString = defaultElevation.valueString,
   272|                    WidthSource = width.value != null ? width.source : "parsed_type_name",
   273|                    DepthSource = depth.value != null ? depth.source : "parsed_type_name",
   274|                    HeightSource = height.source,
   275|                    DefaultElevationSource = defaultElevation.source,
   276|                    WidthParam = width.paramName,
   277|                    DepthParam = depth.paramName,
   278|                    HeightParam = height.paramName,
   279|                    DefaultElevationParam = defaultElevation.paramName,
   280|                    ParsedWidthFeet = parsedWidth,
   281|                    ParsedDepthFeet = parsedDepth
   282|                },
   283|                Classification = new UnitClassification
   284|                {
   285|                    ClimateRaw = climate.RawValue,
   286|                    ClimateString = climate.valueString,
   287|                    Climate = ParameterHelper.Boolish(climate.RawValue),
   288|                    ClimateSource = climate.source,
   289|                    ClimateParam = climate.paramName,
   290|
   291|                    ClimateHeatOnlyRaw = climateHeatOnly.RawValue,
   292|                    ClimateHeatOnlyString = climateHeatOnly.valueString,
   293|                    ClimateHeatOnly = ParameterHelper.Boolish(climateHeatOnly.RawValue),
   294|                    ClimateHeatOnlySource = climateHeatOnly.source,
   295|                    ClimateHeatOnlyParam = climateHeatOnly.paramName,
   296|
   297|                    DriveupRaw = driveup.RawValue,
   298|                    DriveupString = driveup.valueString,
   299|                    Driveup = ParameterHelper.Boolish(driveup.RawValue),
   300|                    DriveupSource = driveup.source,
   301|                    DriveupParam = driveup.paramName,
   302|
   303|                    LockerRaw = locker.RawValue,
   304|                    LockerString = locker.valueString,
   305|                    Locker = ParameterHelper.Boolish(locker.RawValue),
   306|                    LockerSource = locker.source,
   307|                    LockerParam = locker.paramName,
   308|
   309|                    GroundAccessRaw = groundAccess.RawValue,
   310|                    GroundAccessString = groundAccess.valueString,
   311|                    GroundAccess = ParameterHelper.Boolish(groundAccess.RawValue),
   312|                    GroundAccessSource = groundAccess.source,
   313|                    GroundAccessParam = groundAccess.paramName,
   314|
   315|                    AccessibleRaw = accessible.RawValue,
   316|                    AccessibleString = accessible.valueString,
   317|                    Accessible = ParameterHelper.Boolish(accessible.RawValue),
   318|                    AccessibleSource = accessible.source,
   319|                    AccessibleParam = accessible.paramName,
   320|
   321|                    ObstructionsRaw = obstructions.RawValue,
   322|                    ObstructionsString = obstructions.valueString,
   323|                    Obstructions = ParameterHelper.Boolish(obstructions.RawValue),
   324|                    ObstructionsSource = obstructions.source,
   325|                    ObstructionsParam = obstructions.paramName,
   326|
   327|                    OfflineRaw = offline.RawValue,
   328|                    OfflineString = offline.valueString,
   329|                    Offline = ParameterHelper.Boolish(offline.RawValue),
   330|                    OfflineSource = offline.source,
   331|                    OfflineParam = offline.paramName,
   332|
   333|                    PortableRaw = portable.RawValue,
   334|                    PortableString = portable.valueString,
   335|                    Portable = ParameterHelper.Boolish(portable.RawValue),
   336|                    PortableSource = portable.source,
   337|                    PortableParam = portable.paramName,
   338|
   339|                    StackBottomRaw = stackBottom.RawValue,
   340|                    StackBottomString = stackBottom.valueString,
   341|                    StackBottom = ParameterHelper.Boolish(stackBottom.RawValue),
   342|                    StackBottomSource = stackBottom.source,
   343|                    StackBottomParam = stackBottom.paramName,
   344|
   345|                    StackTopRaw = stackTop.RawValue,
   346|                    StackTopString = stackTop.valueString,
   347|                    StackTop = ParameterHelper.Boolish(stackTop.RawValue),
   348|                    StackTopSource = stackTop.source,
   349|                    StackTopParam = stackTop.paramName,
   350|
   351|                    WalkupRaw = walkup.RawValue,
   352|                    WalkupString = walkup.valueString,
   353|                    Walkup = ParameterHelper.Boolish(walkup.RawValue),
   354|                    WalkupSource = walkup.source,
   355|                    WalkupParam = walkup.paramName
   356|                },
   357|                Placement = rotationDegrees.HasValue ? new PlacementData
   358|                {
   359|                    XFeet = (instance.Location as LocationPoint)?.Point.X ?? 0,
   360|                    YFeet = (instance.Location as LocationPoint)?.Point.Y ?? 0,
   361|                    ZFeet = (instance.Location as LocationPoint)?.Point.Z ?? 0,
   362|                    RotationDegrees = rotationDegrees.value
   363|                } : null,
   364|                BoundingBox = GetBoundingBox(instance),
   365|                Mirrored = instance.Mirrored,
   366|                HandFlipped = instance.HandFlipped,
   367|                FacingFlipped = instance.FacingFlipped,
   368|                LevelId = ParameterHelper.GetElementIdValue(instance.LevelId),
   369|                LevelName = levelName,
   370|                LevelOffset = levelOffset.HasValue ? new LevelOffsetData
   371|                {
   372|                    Feet = levelOffset.value,
   373|                    Inches = levelOffset.value * 12,
   374|                    Source = levelOffsetSource,
   375|                    ValueString = levelOffsetString
   376|                } : null,
   377|                DesignOption = designOptionName,
   378|                Workset = GetWorksetName(instance),
   379|                CreatedPhaseId = ParameterHelper.GetElementIdValue(instance.CreatedPhaseId),
   380|                CreatedPhase = GetPhaseName(instance.CreatedPhaseId),
   381|                DemolishedPhaseId = ParameterHelper.GetElementIdValue(instance.DemolishedPhaseId),
   382|                DemolishedPhase = GetPhaseName(instance.DemolishedPhaseId),
   383|                Parameters = ParameterHelper.GetAllParameters(instance, symbol)
   384|            };
   385|        }
   386|
   387|        private Tuple<double?, double?> ParseSizeFromTypeName(string typeName)
   388|        {
   389|            // Match Python parse_size_from_text: "5x10", "5 x 10", etc.
   390|            var match = Regex.Match(typeName, @"(\d+(?:\.\d+)?)\s*x\s*(\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);
   391|            if (match.Success)
   392|            {
   393|                if (double.TryParse(match.Groups[1].value, out double w) &&
   394|                    double.TryParse(match.Groups[2].value, out double d))
   395|                {
   396|                    return Tuple.Create<double?, double?>(w, d);
   397|                }
   398|            }
   399|            return Tuple.Create<double?, double?>(null, null);
   400|        }
   401|
   402|        private string GetSourceOrigin(string familyName, string typeName)
   403|        {
   404|            // Match Python get_source_origin logic
   405|            if (familyName.Contains("UX5")) return "center";
   406|            if (familyName.Contains("UX4")) return "center";
   407|            if (familyName.Contains("UX3")) return "front_left";
   408|            return "center";
   409|        }
   410|
   411|        private string GetTargetOrigin(string familyName, string typeName)
   412|        {
   413|            return "front_left";  // Python always returns this for units
   414|        }
   415|
   416|        private string GetUxRole(string familyName, string typeName)
   417|        {
   418|            if (familyName.IndexOf("shell", StringComparison.OrdinalIgnoreCase) >= 0 ||
   419|                typeName.IndexOf("shell", StringComparison.OrdinalIgnoreCase) >= 0)
   420|                return "shell";
   421|            return "unit";
   422|        }
   423|
   424|        private BoundingBoxData GetBoundingBox(FamilyInstance instance)
   425|        {
   426|            try
   427|            {
   428|                var bbox = instance.get_BoundingBox(null);
   429|                if (bbox != null)
   430|                {
   431|                    var min = bbox.Min;
   432|                    var max = bbox.Max;
   433|                    var size = max - min;
   434|                    
   435|                    return new BoundingBoxData
   436|                    {
   437|                        Min = new CoordData { XFeet = min.X, YFeet = min.Y, ZFeet = min.Z },
   438|                        Max = new CoordData { XFeet = max.X, YFeet = max.Y, ZFeet = max.Z },
   439|                        Size = new SizeData
   440|                        {
   441|                            XFeet = size.X,
   442|                            YFeet = size.Y,
   443|                            ZFeet = size.Z,
   444|                            XInches = size.X * 12,
   445|                            YInches = size.Y * 12,
   446|                            ZInches = size.Z * 12
   447|                        }
   448|                    };
   449|                }
   450|            }
   451|            catch { }
   452|            return null;
   453|        }
   454|
   455|        private string GetWorksetName(Element element)
   456|        {
   457|            try
   458|            {
   459|                var worksetId = element.WorksetId;
   460|                if (worksetId != null && worksetId != WorksetId.InvalidWorksetId)
   461|                {
   462|                    var workset = _doc.GetWorksetTable().GetWorkset(worksetId);
   463|                    return workset?.Name;
   464|                }
   465|            }
   466|            catch { }
   467|            return null;
   468|        }
   469|
   470|        private string GetPhaseName(ElementId phaseId)
   471|        {
   472|            try
   473|            {
   474|                if (phaseId != null && phaseId != ElementId.InvalidElementId)
   475|                {
   476|                    var phase = _doc.GetElement(phaseId) as Phase;
   477|                    return phase?.Name;
   478|                }
   479|            }
   480|            catch { }
   481|            return null;
   482|        }
   483|    }
   484|}
   485|