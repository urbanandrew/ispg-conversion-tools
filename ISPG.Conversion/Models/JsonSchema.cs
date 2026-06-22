using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ISPG.Conversion.Models
{
    /// <summary>
    /// Root export payload matching pyRevit's JSON schema version 6
    /// </summary>
    public class ExportPayload
    {
        [JsonProperty("schema")]
        public SchemaInfo Schema { get; set; }

        [JsonProperty("exported_at")]
        public string ExportedAt { get; set; }

        [JsonProperty("document")]
        public DocumentInfo Document { get; set; }

        [JsonProperty("filters")]
        public FilterInfo Filters { get; set; }

        [JsonProperty("counts")]
        public CountInfo Counts { get; set; }

        [JsonProperty("units")]
        public List<UnitRecord> Units { get; set; }

        [JsonProperty("skipped")]
        public List<SkippedRecord> Skipped { get; set; }
    }

    public class SchemaInfo
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("version")]
        public int Version { get; set; }
    }

    public class DocumentInfo
    {
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("path")]
        public string Path { get; set; }
    }

    public class FilterInfo
    {
        [JsonProperty("family_or_type_name_contains")]
        public List<string> FamilyOrTypeNameContains { get; set; }

        [JsonProperty("family_or_type_name_starts_with")]
        public List<string> FamilyOrTypeNameStartsWith { get; set; }

        [JsonProperty("also_matches_by_unit_parameters")]
        public bool AlsoMatchesByUnitParameters { get; set; }

        [JsonProperty("ux5_unit_origin")]
        public string Ux5UnitOrigin { get; set; }
    }

    public class CountInfo
    {
        [JsonProperty("exported")]
        public int Exported { get; set; }

        [JsonProperty("skipped_generic_models")]
        public int SkippedGenericModels { get; set; }

        [JsonProperty("match_reason_counts")]
        public Dictionary<string, int> MatchReasonCounts { get; set; }

        [JsonProperty("source_origin_counts")]
        public Dictionary<string, int> SourceOriginCounts { get; set; }
    }

    public class SkippedRecord
    {
        [JsonProperty("element_id")]
        public long ElementId { get; set; }

        [JsonProperty("family_name")]
        public string FamilyName { get; set; }

        [JsonProperty("type_name")]
        public string TypeName { get; set; }

        [JsonProperty("reason")]
        public string Reason { get; set; }
    }

    public class UnitRecord
    {
        [JsonProperty("source")]
        public SourceInfo Source { get; set; }

        [JsonProperty("migration_assumptions")]
        public MigrationAssumptions MigrationAssumptions { get; set; }

        [JsonProperty("identity")]
        public IdentityInfo Identity { get; set; }

        [JsonProperty("dimensions")]
        public DimensionsInfo Dimensions { get; set; }

        [JsonProperty("classification")]
        public ClassificationInfo Classification { get; set; }

        [JsonProperty("placement")]
        public PlacementInfo Placement { get; set; }

        [JsonProperty("parameters")]
        public ParametersInfo Parameters { get; set; }
    }

    public class SourceInfo
    {
        [JsonProperty("revit_version")]
        public string RevitVersion { get; set; }

        [JsonProperty("document_title")]
        public string DocumentTitle { get; set; }

        [JsonProperty("document_path")]
        public string DocumentPath { get; set; }

        [JsonProperty("element_id")]
        public long ElementId { get; set; }

        [JsonProperty("unique_id")]
        public string UniqueId { get; set; }

        [JsonProperty("family_name")]
        public string FamilyName { get; set; }

        [JsonProperty("type_name")]
        public string TypeName { get; set; }

        [JsonProperty("ux_role")]
        public string UxRole { get; set; }

        [JsonProperty("match_reason")]
        public string MatchReason { get; set; }

        [JsonProperty("source_origin")]
        public string SourceOrigin { get; set; }
    }

    public class MigrationAssumptions
    {
        [JsonProperty("old_origin")]
        public string OldOrigin { get; set; }

        [JsonProperty("source_origin")]
        public string SourceOrigin { get; set; }

        [JsonProperty("target_origin")]
        public string TargetOrigin { get; set; }

        [JsonProperty("width_direction_basis")]
        public string WidthDirectionBasis { get; set; }

        [JsonProperty("depth_direction_basis")]
        public string DepthDirectionBasis { get; set; }

        [JsonProperty("legacy_center_to_target_front_left_width_factor")]
        public double LegacyCenterToTargetFrontLeftWidthFactor { get; set; }

        [JsonProperty("legacy_center_to_target_front_left_depth_factor")]
        public double LegacyCenterToTargetFrontLeftDepthFactor { get; set; }
    }

    public class IdentityInfo
    {
        [JsonProperty("building_number")]
        public object BuildingNumber { get; set; }

        [JsonProperty("building_number_string")]
        public string BuildingNumberString { get; set; }

        [JsonProperty("building_number_source")]
        public string BuildingNumberSource { get; set; }

        [JsonProperty("building_number_param")]
        public string BuildingNumberParam { get; set; }

        [JsonProperty("unit_number")]
        public object UnitNumber { get; set; }

        [JsonProperty("unit_number_string")]
        public string UnitNumberString { get; set; }

        [JsonProperty("unit_number_source")]
        public string UnitNumberSource { get; set; }

        [JsonProperty("unit_number_param")]
        public string UnitNumberParam { get; set; }
    }

    public class DimensionsInfo
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

    public class LengthRecord
    {
        [JsonProperty("feet")]
        public double? Feet { get; set; }

        [JsonProperty("inches")]
        public double? Inches { get; set; }
    }

    public class ClassificationInfo
    {
        // Climate
        [JsonProperty("climate_raw")]
        public object ClimateRaw { get; set; }

        [JsonProperty("climate_string")]
        public string ClimateString { get; set; }

        [JsonProperty("climate")]
        public bool? Climate { get; set; }

        [JsonProperty("climate_source")]
        public string ClimateSource { get; set; }

        [JsonProperty("climate_param")]
        public string ClimateParam { get; set; }

        // Climate Heat Only
        [JsonProperty("climate_heat_only_raw")]
        public object ClimateHeatOnlyRaw { get; set; }

        [JsonProperty("climate_heat_only_string")]
        public string ClimateHeatOnlyString { get; set; }

        [JsonProperty("climate_heat_only")]
        public bool? ClimateHeatOnly { get; set; }

        [JsonProperty("climate_heat_only_source")]
        public string ClimateHeatOnlySource { get; set; }

        [JsonProperty("climate_heat_only_param")]
        public string ClimateHeatOnlyParam { get; set; }

        // DriveUp
        [JsonProperty("driveup_raw")]
        public object DriveupRaw { get; set; }

        [JsonProperty("driveup_string")]
        public string DriveupString { get; set; }

        [JsonProperty("driveup")]
        public bool? Driveup { get; set; }

        [JsonProperty("driveup_source")]
        public string DriveupSource { get; set; }

        [JsonProperty("driveup_param")]
        public string DriveupParam { get; set; }

        // Locker
        [JsonProperty("locker_raw")]
        public object LockerRaw { get; set; }

        [JsonProperty("locker_string")]
        public string LockerString { get; set; }

        [JsonProperty("locker")]
        public bool? Locker { get; set; }

        [JsonProperty("locker_source")]
        public string LockerSource { get; set; }

        [JsonProperty("locker_param")]
        public string LockerParam { get; set; }

        // Ground Access
        [JsonProperty("ground_access_raw")]
        public object GroundAccessRaw { get; set; }

        [JsonProperty("ground_access_string")]
        public string GroundAccessString { get; set; }

        [JsonProperty("ground_access")]
        public bool? GroundAccess { get; set; }

        [JsonProperty("ground_access_source")]
        public string GroundAccessSource { get; set; }

        [JsonProperty("ground_access_param")]
        public string GroundAccessParam { get; set; }

        // Accessible
        [JsonProperty("accessible_raw")]
        public object AccessibleRaw { get; set; }

        [JsonProperty("accessible_string")]
        public string AccessibleString { get; set; }

        [JsonProperty("accessible")]
        public bool? Accessible { get; set; }

        [JsonProperty("accessible_source")]
        public string AccessibleSource { get; set; }

        [JsonProperty("accessible_param")]
        public string AccessibleParam { get; set; }

        // Obstructions
        [JsonProperty("obstructions_raw")]
        public object ObstructionsRaw { get; set; }

        [JsonProperty("obstructions_string")]
        public string ObstructionsString { get; set; }

        [JsonProperty("obstructions")]
        public bool? Obstructions { get; set; }

        [JsonProperty("obstructions_source")]
        public string ObstructionsSource { get; set; }

        [JsonProperty("obstructions_param")]
        public string ObstructionsParam { get; set; }

        // Offline
        [JsonProperty("offline_raw")]
        public object OfflineRaw { get; set; }

        [JsonProperty("offline_string")]
        public string OfflineString { get; set; }

        [JsonProperty("offline")]
        public bool? Offline { get; set; }

        [JsonProperty("offline_source")]
        public string OfflineSource { get; set; }

        [JsonProperty("offline_param")]
        public string OfflineParam { get; set; }

        // Portable
        [JsonProperty("portable_raw")]
        public object PortableRaw { get; set; }

        [JsonProperty("portable_string")]
        public string PortableString { get; set; }

        [JsonProperty("portable")]
        public bool? Portable { get; set; }

        [JsonProperty("portable_source")]
        public string PortableSource { get; set; }

        [JsonProperty("portable_param")]
        public string PortableParam { get; set; }

        // Stack Bottom
        [JsonProperty("stack_bottom_raw")]
        public object StackBottomRaw { get; set; }

        [JsonProperty("stack_bottom_string")]
        public string StackBottomString { get; set; }

        [JsonProperty("stack_bottom")]
        public bool? StackBottom { get; set; }

        [JsonProperty("stack_bottom_source")]
        public string StackBottomSource { get; set; }

        [JsonProperty("stack_bottom_param")]
        public string StackBottomParam { get; set; }

        // Stack Top
        [JsonProperty("stack_top_raw")]
        public object StackTopRaw { get; set; }

        [JsonProperty("stack_top_string")]
        public string StackTopString { get; set; }

        [JsonProperty("stack_top")]
        public bool? StackTop { get; set; }

        [JsonProperty("stack_top_source")]
        public string StackTopSource { get; set; }

        [JsonProperty("stack_top_param")]
        public string StackTopParam { get; set; }

        // WalkUp
        [JsonProperty("walkup_raw")]
        public object WalkupRaw { get; set; }

        [JsonProperty("walkup_string")]
        public string WalkupString { get; set; }

        [JsonProperty("walkup")]
        public bool? Walkup { get; set; }

        [JsonProperty("walkup_source")]
        public string WalkupSource { get; set; }

        [JsonProperty("walkup_param")]
        public string WalkupParam { get; set; }
    }

    public class PlacementInfo
    {
        [JsonProperty("location")]
        public LocationData Location { get; set; }

        [JsonProperty("bounding_box")]
        public BoundingBoxData BoundingBox { get; set; }

        [JsonProperty("mirrored")]
        public bool? Mirrored { get; set; }

        [JsonProperty("hand_flipped")]
        public bool? HandFlipped { get; set; }

        [JsonProperty("facing_flipped")]
        public bool? FacingFlipped { get; set; }

        [JsonProperty("level_id")]
        public long? LevelId { get; set; }

        [JsonProperty("level_name")]
        public string LevelName { get; set; }

        [JsonProperty("level_offset")]
        public LevelOffsetData LevelOffset { get; set; }

        [JsonProperty("design_option")]
        public string DesignOption { get; set; }

        [JsonProperty("workset")]
        public string Workset { get; set; }

        [JsonProperty("phase_created")]
        public string PhaseCreated { get; set; }

        [JsonProperty("phase_demolished")]
        public string PhaseDemolished { get; set; }
    }

    public class LocationData
    {
        [JsonProperty("point")]
        public PointData Point { get; set; }

        [JsonProperty("rotation_radians")]
        public double? RotationRadians { get; set; }

        [JsonProperty("rotation_degrees")]
        public double? RotationDegrees { get; set; }
    }

    public class PointData
    {
        [JsonProperty("x_feet")]
        public double XFeet { get; set; }

        [JsonProperty("y_feet")]
        public double YFeet { get; set; }

        [JsonProperty("z_feet")]
        public double ZFeet { get; set; }

        [JsonProperty("x_inches")]
        public double XInches { get; set; }

        [JsonProperty("y_inches")]
        public double YInches { get; set; }

        [JsonProperty("z_inches")]
        public double ZInches { get; set; }
    }

    public class BoundingBoxData
    {
        [JsonProperty("min")]
        public CoordData Min { get; set; }

        [JsonProperty("max")]
        public CoordData Max { get; set; }

        [JsonProperty("size")]
        public SizeData Size { get; set; }
    }

    public class CoordData
    {
        [JsonProperty("x_feet")]
        public double XFeet { get; set; }

        [JsonProperty("y_feet")]
        public double YFeet { get; set; }

        [JsonProperty("z_feet")]
        public double ZFeet { get; set; }
    }

    public class SizeData
    {
        [JsonProperty("x_feet")]
        public double XFeet { get; set; }

        [JsonProperty("y_feet")]
        public double YFeet { get; set; }

        [JsonProperty("z_feet")]
        public double ZFeet { get; set; }

        [JsonProperty("x_inches")]
        public double XInches { get; set; }

        [JsonProperty("y_inches")]
        public double YInches { get; set; }

        [JsonProperty("z_inches")]
        public double ZInches { get; set; }
    }

    public class LevelOffsetData
    {
        [JsonProperty("feet")]
        public double Feet { get; set; }

        [JsonProperty("inches")]
        public double Inches { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("value_string")]
        public string ValueString { get; set; }
    }

    public class ParametersInfo
    {
        [JsonProperty("instance")]
        public Dictionary<string, ParameterRecord> Instance { get; set; }

        [JsonProperty("type")]
        public Dictionary<string, ParameterRecord> Type { get; set; }
    }

    public class ParameterRecord
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("storage_type")]
        public string StorageType { get; set; }

        [JsonProperty("is_shared")]
        public bool IsShared { get; set; }

        [JsonProperty("guid")]
        public string Guid { get; set; }

        [JsonProperty("value_raw")]
        public object ValueRaw { get; set; }

        [JsonProperty("value_string")]
        public string ValueString { get; set; }

        [JsonProperty("value_feet")]
        public double? ValueFeet { get; set; }

        [JsonProperty("value_inches")]
        public double? ValueInches { get; set; }
    }
}
