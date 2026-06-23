using System.Collections.Generic;
using Newtonsoft.Json;

namespace ISPG.Conversion.Models
{
    /// <summary>
    /// Parking export payload matching Python LegacyExportParking schema
    /// </summary>
    public class ParkingExportPayload
    {
        [JsonProperty("schema")]
        public SchemaInfo Schema { get; set; }

        [JsonProperty("exported_at")]
        public string ExportedAt { get; set; }

        [JsonProperty("document")]
        public DocumentInfo Document { get; set; }

        [JsonProperty("filters")]
        public ParkingFilterInfo Filters { get; set; }

        [JsonProperty("counts")]
        public CountInfo Counts { get; set; }

        [JsonProperty("records")]
        public List<ParkingRecord> Records { get; set; }

        [JsonProperty("skipped")]
        public List<SkippedRecord> Skipped { get; set; }
    }

    public class ParkingFilterInfo
    {
        [JsonProperty("parking_family_name_contains")]
        public List<string> ParkingFamilyNameContains { get; set; }

        [JsonProperty("match_by_parking_parameters")]
        public bool MatchByParkingParameters { get; set; }

        [JsonProperty("parking_number_param_names")]
        public List<string> ParkingNumberParamNames { get; set; }

        [JsonProperty("building_number_param_names")]
        public List<string> BuildingNumberParamNames { get; set; }

        [JsonProperty("width_param_names")]
        public List<string> WidthParamNames { get; set; }

        [JsonProperty("depth_param_names")]
        public List<string> DepthParamNames { get; set; }

        [JsonProperty("parking_angle_param_names")]
        public List<string> ParkingAngleParamNames { get; set; }
    }

    /// <summary>
    /// Single parking space record matching Python schema
    /// </summary>
    public class ParkingRecord
    {
        [JsonProperty("source")]
        public ParkingSourceInfo Source { get; set; }

        [JsonProperty("migration_assumptions")]
        public MigrationAssumptions MigrationAssumptions { get; set; }

        [JsonProperty("identity")]
        public ParkingIdentityInfo Identity { get; set; }

        [JsonProperty("dimensions")]
        public DimensionsInfo Dimensions { get; set; }

        [JsonProperty("parking")]
        public ParkingClassificationInfo Parking { get; set; }

        [JsonProperty("placement")]
        public PlacementInfo Placement { get; set; }

        [JsonProperty("parameters")]
        public ParametersInfo Parameters { get; set; }
    }

    public class ParkingSourceInfo
    {
        [JsonProperty("revit_version")]
        public string RevitVersion { get; set; }

        [JsonProperty("document_title")]
        public string DocumentTitle { get; set; }

        [JsonProperty("document_path")]
        public string DocumentPath { get; set; }

        [JsonProperty("element_id")]
        public long? ElementId { get; set; }

        [JsonProperty("unique_id")]
        public string UniqueId { get; set; }

        [JsonProperty("family_name")]
        public string FamilyName { get; set; }

        [JsonProperty("type_name")]
        public string TypeName { get; set; }

        [JsonProperty("role")]
        public string Role { get; set; } // Always "parking"

        [JsonProperty("match_reason")]
        public string MatchReason { get; set; }

        [JsonProperty("source_origin")]
        public string SourceOrigin { get; set; }

        [JsonProperty("target_family_hint")]
        public string TargetFamilyHint { get; set; }

        [JsonProperty("target_type_hint")]
        public string TargetTypeHint { get; set; }
    }

    public class ParkingIdentityInfo
    {
        [JsonProperty("parking_number")]
        public object ParkingNumber { get; set; }

        [JsonProperty("parking_number_string")]
        public string ParkingNumberString { get; set; }

        [JsonProperty("parking_number_source")]
        public string ParkingNumberSource { get; set; }

        [JsonProperty("parking_number_param")]
        public string ParkingNumberParam { get; set; }

        [JsonProperty("building_number")]
        public object BuildingNumber { get; set; }

        [JsonProperty("building_number_string")]
        public string BuildingNumberString { get; set; }

        [JsonProperty("building_number_source")]
        public string BuildingNumberSource { get; set; }

        [JsonProperty("building_number_param")]
        public string BuildingNumberParam { get; set; }
    }

    /// <summary>
    /// Parking-specific classification fields
    /// </summary>
    public class ParkingClassificationInfo
    {
        [JsonProperty("angle")]
        public object Angle { get; set; }

        [JsonProperty("angle_string")]
        public string AngleString { get; set; }

        [JsonProperty("angle_source")]
        public string AngleSource { get; set; }

        [JsonProperty("angle_param")]
        public string AngleParam { get; set; }

        [JsonProperty("covered_raw")]
        public object CoveredRaw { get; set; }

        [JsonProperty("covered_string")]
        public string CoveredString { get; set; }

        [JsonProperty("covered")]
        public bool? Covered { get; set; }

        [JsonProperty("covered_source")]
        public string CoveredSource { get; set; }

        [JsonProperty("covered_param")]
        public string CoveredParam { get; set; }

        [JsonProperty("rentable")]
        public BoolRecord Rentable { get; set; }

        [JsonProperty("accessible")]
        public BoolRecord Accessible { get; set; }

        [JsonProperty("accessible_van")]
        public BoolRecord AccessibleVan { get; set; }

        [JsonProperty("compact")]
        public BoolRecord Compact { get; set; }

        [JsonProperty("electric_vehicle")]
        public BoolRecord ElectricVehicle { get; set; }

        [JsonProperty("info_locker")]
        public BoolRecord InfoLocker { get; set; }

        [JsonProperty("info_portable")]
        public BoolRecord InfoPortable { get; set; }
    }

    /// <summary>
    /// Boolean record matching Python bool_record() function
    /// </summary>
    public class BoolRecord
    {
        [JsonProperty("value")]
        public object Value { get; set; }

        [JsonProperty("value_string")]
        public string ValueString { get; set; }

        [JsonProperty("bool")]
        public bool? Bool { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("param")]
        public string Param { get; set; }
    }
}
