using System;
using System.Text.RegularExpressions;
using Autodesk.Revit.DB;

namespace ISPG.Conversion.Helpers
{
    /// <summary>
    /// Helper methods for working with element numbering
    /// </summary>
    public static class NumberingHelper
    {
        /// <summary>
        /// Priority order for number parameter names
        /// </summary>
        public static readonly string[] NumberParameterNames = new[]
        {
            "Info Unit Number",
            "UX_Info_Unit_Number",
        };

        /// <summary>
        /// Increment a string number (e.g., "001" -> "002", "A-009" -> "A-010")
        /// </summary>
        public static string IncrementString(string value, int step = 1)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            string result = value;

            for (int i = 0; i < step; i++)
            {
                Match match = Regex.Match(result, @"(\d+)$");

                if (!match.Success)
                    return result;

                string numberText = match.Groups[1].Value;
                string prefix = result.Substring(0, match.Groups[1].Index);

                if (!int.TryParse(numberText, out int number))
                    return result;

                int nextNumber = number + 1;
                int width = numberText.Length;

                result = prefix + nextNumber.ToString().PadLeft(width, '0');
            }

            return result;
        }

        /// <summary>
        /// Get the number parameter from an element (tries multiple parameter names)
        /// </summary>
        public static Parameter GetNumberParameter(Element element)
        {
            // Try custom parameters first
            foreach (string paramName in NumberParameterNames)
            {
                Parameter param = element.LookupParameter(paramName);
                if (param != null)
                    return param;
            }

            // Fall back to Mark parameter
            Parameter markParam = element.get_Parameter(BuiltInParameter.ALL_MODEL_MARK);
            return markParam;
        }

        /// <summary>
        /// Get the number value from an element
        /// </summary>
        public static string GetNumber(Element element)
        {
            Parameter param = GetNumberParameter(element);

            if (param == null)
                return null;

            if (param.HasValue)
            {
                if (param.StorageType == StorageType.String)
                    return param.AsString();
                else
                    return param.AsValueString();
            }

            return null;
        }

        /// <summary>
        /// Set the number value on an element
        /// </summary>
        public static bool SetNumber(Element element, string newNumber)
        {
            Parameter param = GetNumberParameter(element);

            if (param == null || param.IsReadOnly)
                return false;

            try
            {
                param.Set(newNumber);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get element ID as integer (compatibility helper for different Revit versions)
        /// </summary>
        public static long GetElementIdValue(ElementId id)
        {
            if (id == null)
                return -1;

#if REVIT2024_OR_GREATER
            return id.Value;
#else
            return id.IntegerValue;
#endif
        }

        /// <summary>
        /// Create ElementId from integer value
        /// </summary>
        public static ElementId CreateElementId(long value)
        {
#if REVIT2024_OR_GREATER
            return new ElementId(value);
#else
            return new ElementId((int)value);
#endif
        }
    }
}
