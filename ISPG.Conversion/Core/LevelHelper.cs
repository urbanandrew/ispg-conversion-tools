using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Autodesk.Revit.DB;

namespace ISPG.Conversion.Core
{
    /// <summary>
    /// Helper methods for level matching and normalization
    /// Replicates pyRevit's level matching logic
    /// </summary>
    public static class LevelHelper
    {
        /// <summary>
        /// Find matching level by name with normalization
        /// Tries exact match first, then normalized, then custom mappings
        /// </summary>
        public static Level FindLevel(Document doc, string levelName, Dictionary<string, string> customMap = null)
        {
            if (string.IsNullOrWhiteSpace(levelName)) return null;

            var levels = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .ToList();

            // Try exact match first
            var exactMatch = levels.FirstOrDefault(l => l.Name == levelName);
            if (exactMatch != null) return exactMatch;

            // Try custom mapping
            if (customMap != null && customMap.ContainsKey(levelName))
            {
                string mappedName = customMap[levelName];
                var mappedLevel = levels.FirstOrDefault(l => l.Name == mappedName);
                if (mappedLevel != null) return mappedLevel;
            }

            // Try normalized match
            string normalizedName = NormalizeLevelName(levelName);
            foreach (var level in levels)
            {
                if (NormalizeLevelName(level.Name) == normalizedName)
                    return level;
            }

            // No match found
            return null;
        }

        /// <summary>
        /// Normalize level name for fuzzy matching
        /// Examples:
        ///   "Level 1" -> "1"
        ///   "02 Floor" -> "2"
        ///   "Ground Floor" -> "ground"
        ///   "Basement" -> "basement"
        /// </summary>
        public static string NormalizeLevelName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "";

            string normalized = name.ToLower().Trim();

            // Remove common prefixes
            normalized = Regex.Replace(normalized, @"^(level|floor|story|storey)\s*", "");

            // Extract number if present
            var numberMatch = Regex.Match(normalized, @"\d+");
            if (numberMatch.Success)
            {
                // Remove leading zeros
                int number = int.Parse(numberMatch.Value);
                return number.ToString();
            }

            // Return cleaned name for non-numeric levels (Ground, Basement, etc.)
            return normalized;
        }

        /// <summary>
        /// Get all levels in document sorted by elevation
        /// </summary>
        public static List<Level> GetSortedLevels(Document doc)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .OrderBy(l => l.Elevation)
                .ToList();
        }

        /// <summary>
        /// Find closest level by elevation
        /// </summary>
        public static Level FindClosestLevel(Document doc, double elevation)
        {
            var levels = GetSortedLevels(doc);
            if (!levels.Any()) return null;

            return levels.OrderBy(l => Math.Abs(l.Elevation - elevation)).First();
        }

        /// <summary>
        /// Get level offset from base level elevation
        /// </summary>
        public static double GetLevelOffset(double elevation, Level baseLevel)
        {
            if (baseLevel == null) return elevation;
            return elevation - baseLevel.Elevation;
        }

        /// <summary>
        /// Get level name safely
        /// </summary>
        public static string GetLevelName(Level level)
        {
            if (level == null) return null;
            try { return level.Name; } catch { return null; }
        }

        /// <summary>
        /// Get level elevation safely
        /// </summary>
        public static double? GetLevelElevation(Level level)
        {
            if (level == null) return null;
            try { return level.Elevation; } catch { return null; }
        }
    }
}
