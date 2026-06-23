using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Autodesk.Revit.DB;
using ISPG.Conversion.Models;

namespace ISPG.Conversion.Core
{
    /// <summary>
    /// Helper methods for working with Revit parameters
    /// Replicates pyRevit parameter extraction logic
    /// </summary>
    public static class ParameterHelper
    {
        // Parameter name fallback chains from pyRevit
        public static readonly string[] BUILDING_NUMBER_PARAMS = {
            "Info Building Number",
            "Info Building Number (default)"
        };

        public static readonly string[] UNIT_NUMBER_PARAMS = {
            "UX_Info_Unit_Number",
            "Info Unit Number",
            "Info Unit Number (default)"
        };

        public static readonly string[] WIDTH_PARAMS = {
            // UX5 modern params
            "UX5 Width",
            "UX5 Width (default)",
            "UX5 Width Simple",
            "UX5 Width Simple (default)",
            // Legacy unit params
            "UX_Room_Width",
            "Stretch Width",
            "Stretch Width (default)",
            // Parking params
            "SITE_Parking_Width",
            "Parking Width",
            "Parking Width (default)"
        };

        public static readonly string[] DEPTH_PARAMS = {
            // UX5 modern params
            "UX5 Depth",
            "UX5 Depth (default)",
            "UX5 Depth Simple",
            "UX5 Depth Simple (default)",
            // Legacy unit params
            "UX_Room_Depth",
            "Stretch Depth",
            "Stretch Depth (default)",
            // Parking params
            "SITE_Parking_Depth",
            "Parking Length",
            "Parking Length (default)"
        };

        public static readonly string[] HEIGHT_PARAMS = {
            // UX5 modern params  
            "UX5 Height",
            "UX5 Height (default)",
            // Legacy unit params
            "UX_Room_Height",
            "Stretch Height",
            "Stretch Height (default)"
        };

        public static readonly string[] CLIMATE_PARAMS = {
            "UX_Info_Unit_CC",
            "Info Climate Controlled",
            "Info Climate Controlled (default)"
        };

        public static readonly string[] CLIMATE_HEAT_ONLY_PARAMS = {
            "Info Climate Controlled (Heat Only)",
            "Info Climate Controlled (Heat Only) (default)"
        };

        public static readonly string[] DRIVEUP_PARAMS = {
            "UX_Info_Unit_DriveUp",
            "Info DriveUp",
            "Info DriveUp (default)"
        };

        public static readonly string[] LOCKER_PARAMS = {
            "UX_Info_Locker",
            "Info Locker",
            "Info Locker (default)"
        };

        public static readonly string[] GROUND_ACCESS_PARAMS = {
            "UX_Info_Unit_GroundAccess",
            "Info Ground Access",
            "Info Ground Access (default)"
        };

        public static readonly string[] ACCESSIBLE_PARAMS = {
            // Parking params (check first - more specific)
            "Parking Accessible",
            "Parking Accessible (default)",
            // Unit params
            "Info Accessible",
            "Info Accessible (default)"
        };

        public static readonly string[] OBSTRUCTIONS_PARAMS = {
            "Info Obstructions",
            "Info Obstructions (default)"
        };

        public static readonly string[] OFFLINE_PARAMS = {
            "Info Offline",
            "Info Offline (default)"
        };

        public static readonly string[] PORTABLE_PARAMS = {
            "Info Portable",
            "Info Portable (default)"
        };

        public static readonly string[] STACK_BOTTOM_PARAMS = {
            "Info StackBottom",
            "Info StackBottom (default)"
        };

        public static readonly string[] STACK_TOP_PARAMS = {
            "Info StackTop",
            "Info StackTop (default)"
        };

        public static readonly string[] WALKUP_PARAMS = {
            "Info WalkUp",
            "Info WalkUp (default)"
        };

        public static readonly string[] DEFAULT_ELEVATION_PARAMS = {
            "Default Elevation"
        };

        // Parking-specific parameters
        public static readonly string[] PARKING_COVERED_PARAMS = {
            "Parking Covered",
            "Parking Covered (default)",
            "Parking Coverered",  // typo in some families
            "SITE_Parking_Covered"
        };

        public static readonly string[] PARKING_RENTABLE_PARAMS = {
            "Parking Rentable",
            "Parking Rentable (default)"
        };

        public static readonly string[] PARKING_ACCESSIBLE_VAN_PARAMS = {
            "Parking Accessible Van",
            "Parking Accessible Van (default)"
        };

        public static readonly string[] PARKING_COMPACT_PARAMS = {
            "Parking Compact",
            "Parking Compact (default)"
        };

        public static readonly string[] PARKING_ELECTRIC_VEHICLE_PARAMS = {
            "Parking Electric Vehicle",
            "Parking Electric Vehicle (default)"
        };

        /// <summary>
        /// Get parameter by trying multiple names in order (instance first, then type)
        /// </summary>
        public static Parameter GetParameterByNames(Element instance, Element symbol, string[] names)
        {
            // Try instance first
            if (instance != null)
            {
                foreach (var name in names)
                {
                    var param = instance.LookupParameter(name);
                    if (param != null) return param;
                }
            }

            // Then try type
            if (symbol != null)
            {
                foreach (var name in names)
                {
                    var param = symbol.LookupParameter(name);
                    if (param != null) return param;
                }
            }

            return null;
        }

        /// <summary>
        /// Get parameter value with proper type handling
        /// </summary>
        public static object GetParameterValue(Parameter param)
        {
            if (param == null || !param.HasValue) return null;

            try
            {
                switch (param.StorageType)
                {
                    case StorageType.String:
                        return param.AsString();
                    case StorageType.Integer:
                        return param.AsInteger();
                    case StorageType.Double:
                        return param.AsDouble();
                    case StorageType.ElementId:
                        return GetElementIdValue(param.AsElementId());
                    default:
                        return null;
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get parameter value as display string
        /// </summary>
        public static string GetParameterValueString(Parameter param)
        {
            if (param == null) return null;
            try
            {
                return param.AsValueString();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get first available parameter value from fallback chain
        /// Returns (value, valueString, source ("instance" or "type"), paramName)
        /// </summary>
        public static (object value, string valueString, string source, string paramName) GetFirstParamValue(
            Element instance, Element symbol, string[] paramNames)
        {
            // Try instance first
            if (instance != null)
            {
                foreach (var name in paramNames)
                {
                    var param = instance.LookupParameter(name);
                    if (param != null && param.HasValue)
                    {
                        return (
                            GetParameterValue(param),
                            GetParameterValueString(param),
                            "instance",
                            param.Definition.Name
                        );
                    }
                }
            }

            // Then try type
            if (symbol != null)
            {
                foreach (var name in paramNames)
                {
                    var param = symbol.LookupParameter(name);
                    if (param != null && param.HasValue)
                    {
                        return (
                            GetParameterValue(param),
                            GetParameterValueString(param),
                            "type",
                            param.Definition.Name
                        );
                    }
                }
            }

            return (null, null, null, null);
        }

        /// <summary>
        /// Convert ElementId to long value (handles both Revit 2024- and 2025+)
        /// </summary>
        public static long? GetElementIdValue(ElementId id)
        {
            if (id == null || id == ElementId.InvalidElementId) return null;

            try
            {
                // Revit 2024 and earlier
                return id.IntegerValue;
            }
            catch
            {
                try
                {
                    // Revit 2025+ (.Value property)
                    return id.Value;
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Convert feet to inches
        /// </summary>
        public static double? FeetToInches(double? feet)
        {
            return feet.HasValue ? feet.Value * 12.0 : (double?)null;
        }

        /// <summary>
        /// Create length record with feet and inches
        /// </summary>
        public static LengthRecord CreateLengthRecord(double? feet)
        {
            if (!feet.HasValue) return null;

            return new LengthRecord
            {
                Feet = feet.Value,
                Inches = FeetToInches(feet.Value)
            };
        }

        /// <summary>
        /// Convert parameter value to boolean (handles various string representations)
        /// </summary>
        public static bool? Boolish(object value)
        {
            if (value == null) return null;

            if (value is bool b) return b;
            if (value is int i) return i != 0;

            var str = value.ToString().Trim().ToLower();

            if (new[] { "yes", "true", "1", "y", "cc", "climate", "climate controlled" }.Contains(str))
                return true;

            if (new[] { "no", "false", "0", "n", "ncc", "non climate", "non-climate", "nonclimate" }.Contains(str))
                return false;

            return null;
        }

        /// <summary>
        /// Parse size from type name (e.g., "5x10" -> (5.0, 10.0))
        /// </summary>
        public static (double? width, double? depth) ParseSizeFromText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return (null, null);

            var match = Regex.Match(text.ToLower(), @"(\d+(?:\.\d+)?)\s*[xX]\s*(\d+(?:\.\d+)?)");
            if (match.Success)
            {
                if (double.TryParse(match.Groups[1].Value, out double width) &&
                    double.TryParse(match.Groups[2].Value, out double depth))
                {
                    return (width, depth);
                }
            }

            return (null, null);
        }

        /// <summary>
        /// Collect all parameters from an element into a dictionary
        /// </summary>
        public static Dictionary<string, ParameterRecord> CollectParameters(Element element)
        {
            var result = new Dictionary<string, ParameterRecord>();
            if (element == null) return result;

            try
            {
                foreach (Parameter param in element.Parameters)
                {
                    var record = ParameterToRecord(param);
                    if (record != null && !string.IsNullOrEmpty(record.Name))
                    {
                        result[record.Name] = record;
                    }
                }
            }
            catch
            {
                // Ignore errors
            }

            return result;
        }

        /// <summary>
        /// Convert a parameter to a ParameterRecord
        /// </summary>
        public static ParameterRecord ParameterToRecord(Parameter param)
        {
            if (param == null) return null;

            string name = null;
            string storageType = null;
            bool isShared = false;
            string guid = null;

            try { name = param.Definition.Name; } catch { }
            try { storageType = param.StorageType.ToString(); } catch { }
            try { isShared = param.IsShared; } catch { }

            if (isShared)
            {
                try { guid = param.GUID.ToString(); } catch { }
            }

            var rawValue = GetParameterValue(param);

            var record = new ParameterRecord
            {
                Name = name,
                StorageType = storageType,
                IsShared = isShared,
                Guid = guid,
                ValueRaw = rawValue,
                ValueString = GetParameterValueString(param),
                ValueFeet = null,
                ValueInches = null
            };

            if (param.StorageType == StorageType.Double && rawValue is double dval)
            {
                record.ValueFeet = dval;
                record.ValueInches = FeetToInches(dval);
            }

            return record;
        }

        /// <summary>
        /// Get element name safely
        /// </summary>
        public static string GetElementName(Element element)
        {
            if (element == null) return null;
            try { return element.Name; } catch { return null; }
        }

        /// <summary>
        /// Get element name by ID
        /// </summary>
        public static string GetElementNameById(Document doc, ElementId id)
        {
            try
            {
                var element = doc.GetElement(id);
                return GetElementName(element);
            }
            catch
            {
                return null;
            }
        }
    }
}
