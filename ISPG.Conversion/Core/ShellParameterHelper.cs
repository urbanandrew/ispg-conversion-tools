using System.Collections.Generic;

namespace ISPG.Conversion.Core
{
    /// <summary>
    /// Shell-specific parameter names matching Python LegacyExportShell script
    /// </summary>
    public static class ShellParameterHelper
    {
        // Identity parameters
        public static readonly List<string> BUILDING_NUMBER_PARAMS = new List<string>
        {
            "Info Building Number",
            "Info Building Number (default)"
        };

        // Dimension parameters (same as Units)
        public static readonly List<string> WIDTH_PARAMS = new List<string>
        {
            "UX_Room_Width",
            "Stretch Width",
            "Stretch Width (default)"
        };

        public static readonly List<string> DEPTH_PARAMS = new List<string>
        {
            "UX_Room_Depth",
            "Stretch Depth",
            "Stretch Depth (default)"
        };

        public static readonly List<string> HEIGHT_PARAMS = new List<string>
        {
            "UX_Room_Height",
            "Stretch Height",
            "Stretch Height (default)"
        };

        public static readonly List<string> DEFAULT_ELEVATION_PARAMS = new List<string>
        {
            "Default Elevation"
        };

        // UX5 Shell flag parameters (from Python UX5_SHELL_FLAG_PARAMS list)
        public static readonly List<string> UX5_SHELL_FLAG_PARAMS = new List<string>
        {
            "Type Wall",
            "Type Corridor",
            "Type Elevator",
            "Type Stair",
            "Type Restroom",
            "Type Trash",
            "Type Mechanical",
            "Type Office",
            "Type Retail",
            "Type Residential",
            "Type Amenity",
            "Type Parking"
        };

        // UX4 Icon Shell flag parameters (from Python ICON_SHELL_PARAMS_TO_FLAGS mapping)
        public static readonly Dictionary<string, string> UX4_ICON_PARAMS_TO_FLAGS = new Dictionary<string, string>
        {
            { "Type Elevator", "Type Elevator" },
            { "Type Restroom", "Type Restroom" },
            { "Type Stair", "Type Stair" },
            { "Type Trash", "Type Trash" },
            { "Type Mech", "Type Mechanical" }
        };
    }
}
