using System.Collections.Generic;

namespace ISPG.Conversion.Core
{
    /// <summary>
    /// Unit-specific parameter names matching Python LegacyExportUnits script
    /// </summary>
    public static class UnitParameterHelper
    {
        // Identity parameters
        public static readonly List<string> BUILDING_NUMBER_PARAMS = new List<string>
        {
            "Info Building Number",
            "Info Building Number (default)"
        };

        public static readonly List<string> UNIT_NUMBER_PARAMS = new List<string>
        {
            "UX_Info_Unit_Number",
            "Info Unit Number",
            "Info Unit Number (default)"
        };

        // Dimension parameters
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

        // Classification parameters (all boolean flags)
        public static readonly List<string> CLIMATE_PARAMS = new List<string>
        {
            "UX_Info_Unit_CC",
            "Info Climate Controlled",
            "Info Climate Controlled (default)"
        };

        public static readonly List<string> CLIMATE_HEAT_ONLY_PARAMS = new List<string>
        {
            "Info Climate Controlled (Heat Only)",
            "Info Climate Controlled (Heat Only) (default)"
        };

        public static readonly List<string> DRIVEUP_PARAMS = new List<string>
        {
            "UX_Info_Unit_DriveUp",
            "Info DriveUp",
            "Info DriveUp (default)"
        };

        public static readonly List<string> LOCKER_PARAMS = new List<string>
        {
            "UX_Info_Locker",
            "Info Locker",
            "Info Locker (default)"
        };

        public static readonly List<string> GROUND_ACCESS_PARAMS = new List<string>
        {
            "UX_Info_Unit_GroundAccess",
            "Info Ground Access",
            "Info Ground Access (default)"
        };

        public static readonly List<string> ACCESSIBLE_PARAMS = new List<string>
        {
            "Info Accessible",
            "Info Accessible (default)"
        };

        public static readonly List<string> OBSTRUCTIONS_PARAMS = new List<string>
        {
            "Info Obstructions",
            "Info Obstructions (default)"
        };

        public static readonly List<string> OFFLINE_PARAMS = new List<string>
        {
            "Info Offline",
            "Info Offline (default)"
        };

        public static readonly List<string> PORTABLE_PARAMS = new List<string>
        {
            "Info Portable",
            "Info Portable (default)"
        };

        public static readonly List<string> STACK_BOTTOM_PARAMS = new List<string>
        {
            "Info StackBottom",
            "Info StackBottom (default)"
        };

        public static readonly List<string> STACK_TOP_PARAMS = new List<string>
        {
            "Info StackTop",
            "Info StackTop (default)"
        };

        public static readonly List<string> WALKUP_PARAMS = new List<string>
        {
            "Info WalkUp",
            "Info WalkUp (default)"
        };
    }
}
