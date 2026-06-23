using System.Collections.Generic;
using Newtonsoft.Json;

namespace ISPG.Conversion.Models
{
    /// <summary>
    /// Shell export payload matching Python LegacyExportShell schema
    /// </summary>
    public class ShellExportPayload
    {
        [JsonProperty("export_metadata")]
        public ExportMetadata ExportMetadata { get; set; }

        [JsonProperty("shells")]
        public List<ShellRecord> Shells { get; set; }

        [JsonProperty("skipped")]
        public List<SkippedElement> Skipped { get; set; }

        [JsonProperty("summary")]
        public ExportSummary Summary { get; set; }
    }

    public class ShellRecord
    {
        [JsonProperty("source")]
        public ShellSourceData Source { get; set; }

        [JsonProperty("migration_assumptions")]
        public ShellMigrationAssumptions MigrationAssumptions { get; set; }

        [JsonProperty("identity")]
        public ShellIdentity Identity { get; set; }

        [JsonProperty("dimensions")]
        public ShellDimensions Dimensions { get; set; }

        [JsonProperty("flags")]
        public Dictionary<string, FlagRecord> Flags { get; set; }

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

    public class ShellSourceData
    {
        [JsonProperty("revit_version")]
        public string RevitVersion { get; set; }

        [JsonProperty("document_title")]
        public string DocumentTitle { get; set; }

        [JsonProperty("document_path")]
        public string DocumentPath { get; set; }

        [JsonProperty("element_id")]
        public int? ElementId { get; set; }

        [JsonProperty("unique_id")]
        public string UniqueId { get; set; }

        [JsonProperty("family_name")]
        public string FamilyName { get; set; }

        [JsonProperty("type_name")]
        public string TypeName { get; set; }

        [JsonProperty("role")]
        public string Role { get; set; }

        [JsonProperty("source_kind")]
        public string SourceKind { get; set; }

        [JsonProperty("source_origin")]
        public string SourceOrigin { get; set; }

        [JsonProperty("target_family_hint")]
        public string TargetFamilyHint { get; set; }

        [JsonProperty("target_type_hint")]
        public string TargetTypeHint { get; set; }
    }

    public class ShellMigrationAssumptions
    {
        [JsonProperty("source_origin")]
        public string SourceOrigin { get; set; }

        [JsonProperty("target_origin")]
        public string TargetOrigin { get; set; }

        [JsonProperty("width_direction_basis")]
        public string WidthDirectionBasis { get; set; }

        [JsonProperty("depth_direction_basis")]
        public string DepthDirectionBasis { get; set; }
    }

    public class ShellIdentity
    {
        [JsonProperty("building_number")]
        public int? BuildingNumber { get; set; }

        [JsonProperty("building_number_string")]
        public string BuildingNumberString { get; set; }

        [JsonProperty("building_number_source")]
        public string BuildingNumberSource { get; set; }

        [JsonProperty("building_number_param")]
        public string BuildingNumberParam { get; set; }
    }

    public class ShellDimensions
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

    public class FlagRecord
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("value")]
        public bool Value { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("source_param")]
        public string SourceParam { get; set; }

        [JsonProperty("raw")]
        public object Raw { get; set; }

        [JsonProperty("value_string")]
        public string ValueString { get; set; }
    }
}
