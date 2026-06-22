using System;
using Autodesk.Revit.DB;

namespace ISPG.Conversion.Core
{
    /// <summary>
    /// Handles origin conversion between legacy (center) and UX5 (front-left) coordinate systems
    /// Replicates pyRevit's origin correction math
    /// </summary>
    public static class OriginCalculator
    {
        /// <summary>
        /// Calculate migration factors for converting from legacy center origin to front-left origin
        /// Based on rotation and dimensions
        /// </summary>
        public static (double widthFactor, double depthFactor) CalculateMigrationFactors(
            double rotationDegrees,
            string sourceOrigin,
            string targetOrigin)
        {
            // Only convert if going from center to front-left
            if (sourceOrigin != "legacy_center" || targetOrigin != "front_left")
                return (0.0, 0.0);

            // Normalize rotation to [0, 360)
            double normRot = NormalizeAngle(rotationDegrees);

            // Determine quadrant and calculate offset factors
            // This matches the pyRevit logic for origin conversion
            double widthFactor, depthFactor;

            if (normRot >= 0 && normRot < 90)
            {
                // Quadrant 1: 0-90 degrees
                widthFactor = -0.5;
                depthFactor = -0.5;
            }
            else if (normRot >= 90 && normRot < 180)
            {
                // Quadrant 2: 90-180 degrees
                widthFactor = 0.5;
                depthFactor = -0.5;
            }
            else if (normRot >= 180 && normRot < 270)
            {
                // Quadrant 3: 180-270 degrees
                widthFactor = 0.5;
                depthFactor = 0.5;
            }
            else
            {
                // Quadrant 4: 270-360 degrees
                widthFactor = -0.5;
                depthFactor = 0.5;
            }

            return (widthFactor, depthFactor);
        }

        /// <summary>
        /// Calculate target position from legacy center position
        /// Applies rotation-aware offset to convert from center to front-left origin
        /// </summary>
        public static XYZ CalculateTargetPosition(
            XYZ legacyCenterPosition,
            double rotationRadians,
            double width,
            double depth,
            double widthFactor,
            double depthFactor)
        {
            // Calculate offset vector in local coordinates
            double localOffsetX = width * widthFactor;
            double localOffsetY = depth * depthFactor;

            // Rotate offset vector to match element's rotation
            double offsetX = localOffsetX * Math.Cos(rotationRadians) - localOffsetY * Math.Sin(rotationRadians);
            double offsetY = localOffsetX * Math.Sin(rotationRadians) + localOffsetY * Math.Cos(rotationRadians);

            // Apply offset to center position
            return new XYZ(
                legacyCenterPosition.X + offsetX,
                legacyCenterPosition.Y + offsetY,
                legacyCenterPosition.Z
            );
        }

        /// <summary>
        /// Calculate legacy center position from front-left position
        /// Reverse of CalculateTargetPosition
        /// </summary>
        public static XYZ CalculateLegacyCenterPosition(
            XYZ frontLeftPosition,
            double rotationRadians,
            double width,
            double depth,
            double widthFactor,
            double depthFactor)
        {
            // Calculate offset vector in local coordinates (reversed signs)
            double localOffsetX = -(width * widthFactor);
            double localOffsetY = -(depth * depthFactor);

            // Rotate offset vector to match element's rotation
            double offsetX = localOffsetX * Math.Cos(rotationRadians) - localOffsetY * Math.Sin(rotationRadians);
            double offsetY = localOffsetX * Math.Sin(rotationRadians) + localOffsetY * Math.Cos(rotationRadians);

            // Apply offset to front-left position
            return new XYZ(
                frontLeftPosition.X + offsetX,
                frontLeftPosition.Y + offsetY,
                frontLeftPosition.Z
            );
        }

        /// <summary>
        /// Normalize angle to [0, 360) range
        /// </summary>
        public static double NormalizeAngle(double degrees)
        {
            double normalized = degrees % 360.0;
            if (normalized < 0)
                normalized += 360.0;
            return normalized;
        }

        /// <summary>
        /// Convert radians to degrees
        /// </summary>
        public static double RadiansToDegrees(double radians)
        {
            return radians * (180.0 / Math.PI);
        }

        /// <summary>
        /// Convert degrees to radians
        /// </summary>
        public static double DegreesToRadians(double degrees)
        {
            return degrees * (Math.PI / 180.0);
        }

        /// <summary>
        /// Get rotation from LocationPoint
        /// </summary>
        public static double? GetRotationRadians(LocationPoint locationPoint)
        {
            if (locationPoint == null) return null;

            try
            {
                return locationPoint.Rotation;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get rotation in degrees from LocationPoint
        /// </summary>
        public static double? GetRotationDegrees(LocationPoint locationPoint)
        {
            double? radians = GetRotationRadians(locationPoint);
            return radians.HasValue ? RadiansToDegrees(radians.Value) : (double?)null;
        }
    }
}
