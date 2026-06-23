using System.Collections.Generic;
using Newtonsoft.Json;

namespace ISPG.Conversion.Models
{
    /// <summary>
    /// Unit export payload matching Python LegacyExportUnits schema
    /// </summary>
    public class UnitExportPayload
    {
        [JsonProperty("export_metadata")]
        public ExportMetadata ExportMetadata { get; set; }

        [JsonProperty("units")]
        public List<UnitExportRecord> Units { get; set; }

        [JsonProperty("skipped")]
        public List<SkippedElement> Skipped { get; set; }

        [JsonProperty("summary")]
        public ExportSummary Summary { get; set; }
    }

    public class UnitExportRecord
    {
        [JsonProperty("source")]
        public SourceData Source { get; set; }

        [JsonProperty("migration_assumptions")]
        public MigrationAssumptions MigrationAssumptions { get; set; }

        [JsonProperty("identity")]
        public UnitIdentity Identity { get; set; }

        [JsonProperty("dimensions")]
        public UnitDimensions Dimensions { get; set; }

        [JsonProperty("classification")]
        public UnitClassification Classification { get; set; }

        [JsonProperty("placement")]
        public PlacementData Placement { get; set; }

        [JsonProperty("bounding_box")]
        public BoundingBoxData BoundingBox { get; set; }

        [JsonProperty("mirrored")]
        public bool Mirrored { get; set; }

        [JsonProperty("hand_flipped")]
        public bool HandFlipped { get; set; }

        [JsonProperty("facing_flipped")]
        public bool FacingFlipped { get; set; }

        [JsonProperty("level_id")]
        public int? LevelId { get; set; }

        [JsonProperty("level_name")]
        public string LevelName { get; set; }

        [JsonProperty("level_offset")]
        public LevelOffsetData LevelOffset { get; set; }

        [JsonProperty("design_option")]
        public string DesignOption { get; set; }

        [JsonProperty("workset")]
        public string Workset { get; set; }

        [JsonProperty("created_phase_id")]
        public int? CreatedPhaseId { get; set; }

        [JsonProperty("created_phase")]
        public string CreatedPhase { get; set; }

        [JsonProperty("demolished_phase_id")]
        public int? DemolishedPhaseId { get; set; }

        [JsonProperty("demolished_phase")]
        public string DemolishedPhase { get; set; }

        [JsonProperty("parameters")]
        public ParametersData Parameters { get; set; }
    }

    public class UnitIdentity
    {
        [JsonProperty("building_number")]
        public int? BuildingNumber { get; set; }

        [JsonProperty("building_number_string")]
        public string BuildingNumberString { get; set; }

        [JsonProperty("building_number_source")]
        public string BuildingNumberSource { get; set; }

        [JsonProperty("building_number_param")]
        public string BuildingNumberParam { get; set; }

        [JsonProperty("unit_number")]
        public string UnitNumber { get; set; }

        [JsonProperty("unit_number_string")]
        public string UnitNumberString { get; set; }

        [JsonProperty("unit_number_source")]
        public string UnitNumberSource { get; set; }

        [JsonProperty("unit_number_param")]
        public string UnitNumberParam { get; set; }
    }

    public class UnitDimensions
    {
        [JsonProperty("width")]
        public LengthRecord Width { get; set; }

        [JsonProperty("depth")]
        public LengthRecord Depth { get; set; }

        [JsonProperty("height")]
        public LengthRecord Height { get; set; }

        [JsonProperty("default_elevation")]
        public LengthRecord DefaultElevation { get; set; }

        [JsonProperty("width_raw")]
        public double? WidthRaw { get; set; }

        [JsonProperty("depth_raw")]
        public double? DepthRaw { get; set; }

        [JsonProperty("height_raw")]
        public double? HeightRaw { get; set; }

        [JsonProperty("default_elevation_raw")]
        public double? DefaultElevationRaw { get; set; }

        [JsonProperty("width_string")]
        public string WidthString { get; set; }

        [JsonProperty("depth_string")]
        public string DepthString { get; set; }

        [JsonProperty("height_string")]
        public string HeightString { get; set; }

        [JsonProperty("default_elevation_string")]
        public string DefaultElevationString { get; set; }

        [JsonProperty("width_source")]
        public string WidthSource { get; set; }

        [JsonProperty("depth_source")]
        public string DepthSource { get; set; }

        [JsonProperty("height_source")]
        public string HeightSource { get; set; }

        [JsonProperty("default_elevation_source")]
        public string DefaultElevationSource { get; set; }

        [JsonProperty("width_param")]
        public string WidthParam { get; set; }

        [JsonProperty("depth_param")]
        public string DepthParam { get; set; }

        [JsonProperty("height_param")]
        public string HeightParam { get; set; }

        [JsonProperty("default_elevation_param")]
        public string DefaultElevationParam { get; set; }

        [JsonProperty("parsed_width_feet")]
        public double? ParsedWidthFeet { get; set; }

        [JsonProperty("parsed_depth_feet")]
        public double? ParsedDepthFeet { get; set; }
    }

    public class UnitClassification
    {
        [JsonProperty("climate_raw")]
        public object ClimateRaw { get; set; }

        [JsonProperty("climate_string")]
        public string ClimateString { get; set; }

        [JsonProperty("climate")]
        public bool Climate { get; set; }

        [JsonProperty("climate_source")]
        public string ClimateSource { get; set; }

        [JsonProperty("climate_param")]
        public string ClimateParam { get; set; }

        [JsonProperty("climate_heat_only_raw")]
        public object ClimateHeatOnlyRaw { get; set; }

        [JsonProperty("climate_heat_only_string")]
        public string ClimateHeatOnlyString { get; set; }

        [JsonProperty("climate_heat_only")]
        public bool ClimateHeatOnly { get; set; }

        [JsonProperty("climate_heat_only_source")]
        public string ClimateHeatOnlySource { get; set; }

        [JsonProperty("climate_heat_only_param")]
        public string ClimateHeatOnlyParam { get; set; }

        [JsonProperty("driveup_raw")]
        public object DriveupRaw { get; set; }

        [JsonProperty("driveup_string")]
        public string DriveupString { get; set; }

        [JsonProperty("driveup")]
        public bool Driveup { get; set; }

        [JsonProperty("driveup_source")]
        public string DriveupSource { get; set; }

        [JsonProperty("driveup_param")]
        public string DriveupParam { get; set; }

        [JsonProperty("locker_raw")]
        public object LockerRaw { get; set; }

        [JsonProperty("locker_string")]
        public string LockerString { get; set; }

        [JsonProperty("locker")]
        public bool Locker { get; set; }

        [JsonProperty("locker_source")]
        public string LockerSource { get; set; }

        [JsonProperty("locker_param")]
        public string LockerParam { get; set; }

        [JsonProperty("ground_access_raw")]
        public object GroundAccessRaw { get; set; }

        [JsonProperty("ground_access_string")]
        public string GroundAccessString { get; set; }

        [JsonProperty("ground_access")]
        public bool GroundAccess { get; set; }

        [JsonProperty("ground_access_source")]
        public string GroundAccessSource { get; set; }

        [JsonProperty("ground_access_param")]
        public string GroundAccessParam { get; set; }

        [JsonProperty("accessible_raw")]
        public object AccessibleRaw { get; set; }

        [JsonProperty("accessible_string")]
        public string AccessibleString { get; set; }

        [JsonProperty("accessible")]
        public bool Accessible { get; set; }

        [JsonProperty("accessible_source")]
        public string AccessibleSource { get; set; }

        [JsonProperty("accessible_param")]
        public string AccessibleParam { get; set; }

        [JsonProperty("obstructions_raw")]
        public object ObstructionsRaw { get; set; }

        [JsonProperty("obstructions_string")]
        public string ObstructionsString { get; set; }

        [JsonProperty("obstructions")]
        public bool Obstructions { get; set; }

        [JsonProperty("obstructions_source")]
        public string ObstructionsSource { get; set; }

        [JsonProperty("obstructions_param")]
        public string ObstructionsParam { get; set; }

        [JsonProperty("offline_raw")]
        public object OfflineRaw { get; set; }

        [JsonProperty("offline_string")]
        public string OfflineString { get; set; }

        [JsonProperty("offline")]
        public bool Offline { get; set; }

        [JsonProperty("offline_source")]
        public string OfflineSource { get; set; }

        [JsonProperty("offline_param")]
        public string OfflineParam { get; set; }

        [JsonProperty("portable_raw")]
        public object PortableRaw { get; set; }

        [JsonProperty("portable_string")]
        public string PortableString { get; set; }

        [JsonProperty("portable")]
        public bool Portable { get; set; }

        [JsonProperty("portable_source")]
        public string PortableSource { get; set; }

        [JsonProperty("portable_param")]
        public string PortableParam { get; set; }

        [JsonProperty("stack_bottom_raw")]
        public object StackBottomRaw { get; set; }

        [JsonProperty("stack_bottom_string")]
        public string StackBottomString { get; set; }

        [JsonProperty("stack_bottom")]
        public bool StackBottom { get; set; }

        [JsonProperty("stack_bottom_source")]
        public string StackBottomSource { get; set; }

        [JsonProperty("stack_bottom_param")]
        public string StackBottomParam { get; set; }

        [JsonProperty("stack_top_raw")]
        public object StackTopRaw { get; set; }

        [JsonProperty("stack_top_string")]
        public string StackTopString { get; set; }

        [JsonProperty("stack_top")]
        public bool StackTop { get; set; }

        [JsonProperty("stack_top_source")]
        public string StackTopSource { get; set; }

        [JsonProperty("stack_top_param")]
        public string StackTopParam { get; set; }

        [JsonProperty("walkup_raw")]
        public object WalkupRaw { get; set; }

        [JsonProperty("walkup_string")]
        public string WalkupString { get; set; }

        [JsonProperty("walkup")]
        public bool Walkup { get; set; }

        [JsonProperty("walkup_source")]
        public string WalkupSource { get; set; }

        [JsonProperty("walkup_param")]
        public string WalkupParam { get; set; }
    }
}
