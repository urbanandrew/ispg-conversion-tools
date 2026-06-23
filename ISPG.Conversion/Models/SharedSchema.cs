using System.Collections.Generic;
using Newtonsoft.Json;

namespace ISPG.Conversion.Models
{
    /// <summary>
    /// Shared schema types used by Parking, Unit, and Shell exporters
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

    public class LengthRecord
    {
        [JsonProperty("feet")]
        public double? Feet { get; set; }

        [JsonProperty("inches")]
        public double? Inches { get; set; }

        [JsonProperty("feet_decimal")]
        public double? FeetDecimal { get; set; }

        [JsonProperty("inches_decimal")]
        public double? InchesDecimal { get; set; }

        [JsonProperty("formatted")]
        public string Formatted { get; set; }
    }

    public class ParametersData
    {
        [JsonProperty("instance")]
        public Dictionary<string, object> Instance { get; set; }

        [JsonProperty("type")]
        public Dictionary<string, object> Type { get; set; }
    }

    public class BoolRecord
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
