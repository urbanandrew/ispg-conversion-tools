using System.Collections.Generic;

namespace ISPG.Conversion.Core
{
    /// <summary>
    /// Parking-specific parameter names matching Python LegacyExportParking script
    /// </summary>
    public static class ParkingParameterHelper
    {
        // Width parameters for parking spaces
        public static readonly string[] WIDTH_PARAM_NAMES = new[]
        {
            "SITE_Parking_Width",
            "Parking Width",
            "Parking Width (default)",
            "UX5 Width",
            "UX5 Width (default)",
            "UX5 Width Simple",
            "UX5 Width Simple (default)"
        };

        // Depth/Length parameters for parking spaces
        public static readonly string[] DEPTH_PARAM_NAMES = new[]
        {
            "SITE_Parking_Depth",
            "Parking Length",
            "Parking Length (default)",
            "UX5 Depth",
            "UX5 Depth (default)",
            "UX5 Depth Simple",
            "UX5 Depth Simple (default)"
        };

        // Default elevation parameters
        public static readonly string[] DEFAULT_ELEVATION_PARAM_NAMES = new[]
        {
            "Default Elevation"
        };

        // Parking number (ID) parameters
        public static readonly string[] PARKING_NUMBER_PARAM_NAMES = new[]
        {
            "UX_Info_Unit_Number",
            "Info Unit Number",
            "Info Unit Number (default)"
        };

        // Building number parameters
        public static readonly string[] BUILDING_NUMBER_PARAM_NAMES = new[]
        {
            "Info Building Number"
        };

        // Parking angle parameters
        public static readonly string[] PARKING_ANGLE_PARAM_NAMES = new[]
        {
            "SITE_Parking_Angle",
            "Parking Angle",
            "Parking Angle (default)"
        };

        // Parking covered parameters
        public static readonly string[] PARKING_COVERED_PARAM_NAMES = new[]
        {
            "Parking Covered",
            "Parking Covered (default)",
            "Parking Coverered", // Python includes this typo
            "SITE_Parking_Covered"
        };

        // Parking rentable parameters
        public static readonly string[] PARKING_RENTABLE_PARAM_NAMES = new[]
        {
            "Parking Rentable",
            "Parking Rentable (default)"
        };

        // Parking accessible parameters
        public static readonly string[] PARKING_ACCESSIBLE_PARAM_NAMES = new[]
        {
            "Parking Accessible",
            "Parking Accessible (default)"
        };

        // Parking accessible van parameters
        public static readonly string[] PARKING_ACCESSIBLE_VAN_PARAM_NAMES = new[]
        {
            "Parking Accessible Van",
            "Parking Accessible Van (default)"
        };

        // Parking compact parameters
        public static readonly string[] PARKING_COMPACT_PARAM_NAMES = new[]
        {
            "Parking Compact",
            "Parking Compact (default)"
        };

        // Parking electric vehicle parameters
        public static readonly string[] PARKING_ELECTRIC_VEHICLE_PARAM_NAMES = new[]
        {
            "Parking Electric Vehicle",
            "Parking Electric Vehicle (default)"
        };

        // Info locker parameters
        public static readonly string[] INFO_LOCKER_PARAM_NAMES = new[]
        {
            "Info Locker"
        };

        // Info portable parameters
        public static readonly string[] INFO_PORTABLE_PARAM_NAMES = new[]
        {
            "Info Portable"
        };

        // Family name patterns to match parking spaces (from Python PARKING_FAMILY_NAME_CONTAINS)
        public static readonly string[] PARKING_FAMILY_NAME_CONTAINS = new[]
        {
            "Parking Space"
        };
    }
}
