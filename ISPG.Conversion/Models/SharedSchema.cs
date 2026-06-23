using System.Collections.Generic;
using Newtonsoft.Json;

namespace ISPG.Conversion.Models
{
    /// <summary>
    /// Shared schema types used by Parking, Unit, and Shell exporters
    /// (types not already defined in JsonSchema.cs)
    /// </summary>
    
    public class ExportMetadata
    {
        [JsonProperty("export_type")]
        public string ExportType { get; set; }

        [JsonProperty("export_date")]
        public string ExportDate { get; set; }

        [JsonProperty("revit_version")]
        public string RevitVersion { get; set; }

        [JsonProperty("document_title")]
        public string DocumentTitle { get; set; }

        [JsonProperty("document_path")]
        public string DocumentPath { get; set; }
    }

    public class SkippedElement
    {
        [JsonProperty("element_id")]
        public int? ElementId { get; set; }

        [JsonProperty("family_name")]
        public string FamilyName { get; set; }

        [JsonProperty("type_name")]
        public string TypeName { get; set; }

        [JsonProperty("reason")]
        public string Reason { get; set; }
    }

    public class ExportSummary
    {
        [JsonProperty("total_processed")]
        public int TotalProcessed { get; set; }

        [JsonProperty("successful_exports")]
        public int SuccessfulExports { get; set; }

        [JsonProperty("skipped_count")]
        public int SkippedCount { get; set; }

        [JsonProperty("role_counts")]
        public Dictionary<string, int> RoleCounts { get; set; }

        [JsonProperty("source_kind_counts")]
        public Dictionary<string, int> SourceKindCounts { get; set; }

        [JsonProperty("source_origin_counts")]
        public Dictionary<string, int> SourceOriginCounts { get; set; }
    }

    public class PlacementData
    {
        [JsonProperty("x_feet")]
        public double XFeet { get; set; }

        [JsonProperty("y_feet")]
        public double YFeet { get; set; }

        [JsonProperty("z_feet")]
        public double ZFeet { get; set; }

        [JsonProperty("rotation_degrees")]
        public double RotationDegrees { get; set; }
    }

    public class ParametersData
    {
        [JsonProperty("instance")]
        public Dictionary<string, object> Instance { get; set; }

        [JsonProperty("type")]
        public Dictionary<string, object> Type { get; set; }
    }

    public class SourceData
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

        [JsonProperty("ux_role")]
        public string UxRole { get; set; }

        [JsonProperty("match_reason")]
        public string MatchReason { get; set; }

        [JsonProperty("source_origin")]
        public string SourceOrigin { get; set; }
    }
}

