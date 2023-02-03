// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace DepictionEngine
{
    /// <summary>
    /// Additional Math helper methods.
    /// </summary>
    public class MathPlus
    {
        private const double EPSILON_DOUBLE = 0.0000000000001d;
        private const float EPSILON_FLOAT = 0.0000000001f;

        public static readonly Vector2Double HALF_INDEX = new Vector2Double(0.5d, 0.5d);
        public static readonly double DOUBLE_RADIUS = GetRadiusFromCircumference(SIZE);

        public const double RAD2DEG = 180.0d / Math.PI;
        public const double DEG2RAD = Math.PI / 180.0d;

        public const float SIZE = 1.0f;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Lerp(double a, double b, double t)
        {
            return a + t * (b - a);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Approximately(double a, double b, double threshold = EPSILON_DOUBLE)
        {
            return Math.Abs(a - b) < threshold;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Approximately(float a, float b, float threshold = EPSILON_FLOAT)
        {
            return Mathf.Abs(a - b) < threshold;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Clamp<T>(T val, T min, T max) where T : IComparable<T>
        {
            if (val.CompareTo(min) < 0) 
                return min;
            else if (val.CompareTo(max) > 0) 
                return max;
            
            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Clamp01(double val)
        {
            if (val < 0.0d) 
                return 0.0d;
            else if (val > 1.0d) 
                return 1.0d;
            
            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ClampAngle180(float val)
        {
            val %= 360.0f;
            if (val < -180.0f)
                val += 360.0f;
            if (val > 180.0f)
                val -= 360.0f;
            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ClampAngle180(double val)
        {
            val %= 360.0d;
            if (val < -180.0d)
                val += 360.0d;
            if (val > 180.0d)
                val -= 360.0d;
            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ClosestLongitudeDelta(float longitude1, float longitude2)
        {
            float delta1 = (longitude2 + 180.0f) - (longitude1 + 180.0f);
            float delta2 = delta1 < 0.0f ? 360.0f + delta1 : -360.0f + delta1;
            return Mathf.Abs(delta1) < Mathf.Abs(delta2) ? delta1 : delta2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ClosestLongitudeDelta(double longitude1, double longitude2)
        {
            double delta1 = (longitude2 + 180.0d) - (longitude1 + 180.0d);
            double delta2 = delta1 < 0.0d ? 360.0d + delta1 : -360.0d + delta1;
            return Math.Abs(delta1) < Math.Abs(delta2) ? delta1 : delta2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Lerp(double a, double b, float t)
        {
            t = Mathf.Clamp01(t);
            if (t == 0.0f)
                return a;
            if (t == 1.0f)
                return b;
            return a + (b - a) * t;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ExpoEaseIn(float current, float start, float end, float duration)
        {
            return (current == 0.0f) ? start : end * (float)Math.Pow(2.0f, 10.0f * (current / duration - 1.0f)) + start - end * 0.001f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ExpoEaseOut(float current, float start, float end, float duration)
        {
            return (current == duration) ? start + end : end * (-(float)Math.Pow(2.0f, -10.0f * current / duration) + 1.0f) + start;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double GetRadiusFromCircumference(double circumference)
        {
            return (circumference / Math.PI) / 2.0d;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetRadiusFromCircumference(float circumference)
        {
            return (circumference / Mathf.PI) / 2.0f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double GetCircumferenceFromRadius(double radius)
        {
            return 2.0d * Math.PI * radius;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetCircumferenceFromRadius(float radius)
        {
            return 2.0f * Mathf.PI * radius;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double GetCircumferenceAtLatitude(double latitude, double equatorialCircumference)
        {
            return Math.Cos(DEG2RAD * latitude) * equatorialCircumference;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetCircumferenceAtLatitude(float latitude, float equatorialCircumference)
        {
            return Mathf.Cos(Mathf.Deg2Rad * latitude) * equatorialCircumference;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ProjectGrid2DIndex(float x, float y, Vector2Int fromGrid2DIndex, Vector2Int fromGrid2DDimensions, Vector2Int toGrid2DIndex, Vector2Int toGrid2DDimensions)
        {
            float xScale = (float)toGrid2DDimensions.x / (float)fromGrid2DDimensions.x;
            float yScale = (float)toGrid2DDimensions.x / (float)fromGrid2DDimensions.y;
            return new Vector2((fromGrid2DIndex.x - toGrid2DIndex.x / xScale + x) * xScale, (fromGrid2DIndex.y - toGrid2DIndex.y / yScale + y) * yScale);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetZoomFromDistance(double distance, int referenceResolution, double circumference)
        {
            double unitsPerTile = Math.Max(distance, 0.0d) / (Screen.height / referenceResolution);
            return Mathf.Log((float)(circumference / unitsPerTile)) / Mathf.Log(2.0f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double GetDistanceFromZoom(float zoom, int referenceResolution, double circumference)
        {
            return Screen.height / referenceResolution * circumference / Mathf.Pow(2.0f, zoom);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Int GetGrid2DDimensionsFromZoom(int zoom, float xyTilesRatio)
        {
            int numTiles = (int)Mathf.Pow(2.0f, zoom);
            return new Vector2Int(numTiles * (int)(xyTilesRatio > 1.0f ? xyTilesRatio : 1.0f), numTiles * (int)(xyTilesRatio < 1.0f ? 1.0f / xyTilesRatio : 1.0f));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetZoomFromGrid2DDimensions(Vector2Int grid2DDimensions)
        {
            return (int)Mathf.Log(Mathf.Min(grid2DDimensions.x, grid2DDimensions.y), 2.0f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Double GetGrid2DIndexFromGeoCoordinate(GeoCoordinate3Double geoCoordinate, Vector2Int grid2DDimensions)
        {
            int zoom = GetZoomFromGrid2DDimensions(grid2DDimensions);
            float xyTileRatio = GetXYTileRatioFromGrid2DDimensions(grid2DDimensions);
            return GetIndexFromGeoCoordinate(geoCoordinate, GetGrid2DDimensionsFromZoom(zoom, xyTileRatio));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetXYTileRatioFromGrid2DDimensions(Vector2Int grid2DDimensions)
        {
            return (float)grid2DDimensions.x / (float)grid2DDimensions.y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float AngleBetween(float startX, float startY, float endX = 0.0f, float endY = 0.0f)
        {
            return Mathf.Atan2(startY - endY, endX - startX) * Mathf.Rad2Deg;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double AngleBetween(double startX, double startY, double endX = 0.0d, double endY = 0.0d)
        {
            return Math.Atan2(startY - endY, endX - startX) * RAD2DEG;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Double GetIndexFromLocalPoint(Vector3Double localPoint, Vector2Int gridDimensions, bool spherical, double radius, double size, bool clamp = true)
        {
            if (!spherical)
                return GetFlatIndexFromLocalPoint(localPoint, gridDimensions, size, clamp);
            else
                return GetSphericalIndexFromLocalPoint(localPoint, gridDimensions, radius);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Double GetSphericalIndexFromLocalPoint(Vector3Double localPoint, Vector2Int gridDimensions, double radius)
        {
            return GetIndexFromGeoCoordinate(GetSphericalGeoCoordinateFromLocalPoint(localPoint, radius), gridDimensions, false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Double GetFlatIndexFromLocalPoint(Vector3Double localPoint, Vector2Int gridDimensions, double size, bool clamp = true)
        {
            double normalizedX = (localPoint.x / size) + 0.5d;
            double normalizedZ = (-localPoint.z / size) + 0.5d;
            return new Vector2Double((clamp ? normalizedX.Clamp01() : normalizedX) * gridDimensions.x, (clamp ? normalizedZ.Clamp01() : normalizedZ) * gridDimensions.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GeoCoordinate3Double GetGeoCoordinateFromLocalPoint(Vector3Double localPoint, float sphericalRatio, double radius, double size)
        {
            if (sphericalRatio != 0.0f && sphericalRatio != 1.0f)
                return GeoCoordinate3Double.Lerp(GetFlatGeoCoordinateFromLocalPoint(localPoint, size), GetSphericalGeoCoordinateFromLocalPoint(localPoint, radius), sphericalRatio);
            else if (sphericalRatio == 0.0f)
                return GetFlatGeoCoordinateFromLocalPoint(localPoint, size);
            else if (sphericalRatio == 1.0f)
                return GetSphericalGeoCoordinateFromLocalPoint(localPoint, radius);
            return GeoCoordinate3Double.zero;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static GeoCoordinate3Double GetSphericalGeoCoordinateFromLocalPoint(Vector3Double localPoint, double radius)
        {
            Vector3Double localDirectionVector = localPoint.normalized;
            return new GeoCoordinate3Double(RAD2DEG * Math.Asin(localDirectionVector.z), RAD2DEG * Math.Atan2(localDirectionVector.x, localDirectionVector.y), localPoint.magnitude - radius);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static GeoCoordinate3Double GetFlatGeoCoordinateFromLocalPoint(Vector3Double localPoint, double size)
        {
            Vector2Double normalizedPosition = new Vector2Double(Math.Min(Math.Max((localPoint.x / size) + 0.5, 0.0d), 1.0d), Math.Min(Math.Max((-localPoint.z / size) + 0.5, 0.0d), 1.0d));
            GeoCoordinate3Double geoCoord = GetGeoCoordinate3FromIndex(normalizedPosition, Vector2Int.one);
            return new GeoCoordinate3Double(geoCoord.latitude, geoCoord.longitude, localPoint.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GeoCoordinate3Double GetGeoCoordinate3FromIndex(Vector2Double index, Vector2Int gridDimensions)
        {
            return new GeoCoordinate3Double(GetLatFromYIndex(index.y, gridDimensions.y), GetLonFromXIndex(index.x, gridDimensions.x));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GeoCoordinate2Double GetGeoCoordinate2FromIndex(Vector2Double index, Vector2Int gridDimensions)
        {
            return new GeoCoordinate2Double(GetLatFromYIndex(index.y, gridDimensions.y), GetLonFromXIndex(index.x, gridDimensions.x));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double GetLonFromXIndex(double x, int xNumTiles)
        {
            return x / xNumTiles * 360.0d - 180.0d;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double GetLatFromYIndex(double y, int yNumTiles)
        {
            double latitude;

            if (y == 0.0d)
                latitude = 90.0d;
            else if (y == yNumTiles)
                latitude = -90.0d;
            else
                latitude = RAD2DEG * Math.Atan(Math.Sinh(Math.PI * (1.0d - 2.0d * y / yNumTiles)));
            
            return latitude;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3Double GetLocalPointFromGeoCoordinate(GeoCoordinate3Double geoCoordinate, float sphericalRatio, double radius, double size)
        {
            if (sphericalRatio != 0.0f && sphericalRatio != 1.0f)
                return Vector3Double.Lerp(GetLocalPointFromFlatGeoCoordinate(geoCoordinate, size), GetLocalPointFromSphericalGeoCoordinate(geoCoordinate, radius), sphericalRatio);
            else if (sphericalRatio == 0.0f)
                return GetLocalPointFromFlatGeoCoordinate(geoCoordinate, size);
            else if (sphericalRatio == 1.0f)
                return GetLocalPointFromSphericalGeoCoordinate(geoCoordinate, radius);
            return Vector3Double.positiveInfinity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector3Double GetLocalPointFromSphericalGeoCoordinate(GeoCoordinate3Double geoCoordinate, double radius)
        {
            return geoCoordinate.UpVector() * (radius + geoCoordinate.altitude);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector3Double GetLocalPointFromFlatGeoCoordinate(GeoCoordinate3Double geoCoordinate, double size)
        {
            Vector2Double index = GetIndexFromGeoCoordinate(geoCoordinate, Vector2Int.one, false) - HALF_INDEX;
            return new Vector3Double(index.x * size, geoCoordinate.altitude, -index.y * size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Double GetIndexFromGeoCoordinate(GeoCoordinate2Double geoCoordinate, Vector2Int gridDimensions, bool clip = true)
        {
            double latitude = Clamp(geoCoordinate.latitude, -89.0d, 89.0d) * DEG2RAD;
            Vector2Double index = new Vector2Double(Clamp((geoCoordinate.longitude + 180.0d) / 360.0d * gridDimensions.x, 0.0d, gridDimensions.x), Clamp((1.0d - Math.Log(Math.Tan(latitude) + 1.0d / Math.Cos(latitude)) / Math.PI) / 2.0d * gridDimensions.y, 0.0d, gridDimensions.y));
            if (clip)
            {
                index.x = ClipIndex(index.x);
                index.y = ClipIndex(index.y);
            }
            return index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ClipIndex(double index)
        {
            if (index - (int)index < 0.000000000000002d)
            {
                index = (int)index;
                if (index > 0.0d)
                    index -= Math.Pow(0.1d, 15.0d - (int)Math.Floor(Math.Log10(index) + 1.0d));
            }
            return index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3Double GetLocalSurfaceFromLocalPoint(Vector3Double localPoint, float sphericalRatio, double radius, double size, bool clamp = true)
        {
            if (sphericalRatio != 0.0f && sphericalRatio != 1.0f)
                return Vector3Double.Lerp(GetLocalFlatSurfaceFromLocalPoint(localPoint, size, clamp), GetLocalSphericalSurfaceFromLocalPoint(localPoint, radius), sphericalRatio);
            else if (sphericalRatio == 0.0f)
                return GetLocalFlatSurfaceFromLocalPoint(localPoint, size, clamp);
            else if (sphericalRatio == 1.0f)
                return GetLocalSphericalSurfaceFromLocalPoint(localPoint, radius);
            return Vector3Double.positiveInfinity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector3Double GetLocalSphericalSurfaceFromLocalPoint(Vector3Double localPoint, double radius)
        {
            return localPoint.normalized * radius;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector3Double GetLocalFlatSurfaceFromLocalPoint(Vector3Double localPoint, double size, bool clamp = true)
        {
            if (clamp)
            {
                double halfSize = size / 2.0d;
                return new Vector3Double(Math.Min(Math.Max(localPoint.x, -halfSize), halfSize), 0.0d, Math.Min(Math.Max(localPoint.z, -halfSize), halfSize));
            }
            else
                return new Vector3Double(localPoint.x, 0.0d, localPoint.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static QuaternionDouble GetUpVectorFromGeoCoordinate(GeoCoordinate2Double geoCoordinate, float sphericalRatio)
        {
            if (sphericalRatio != 0.0f && sphericalRatio != 1.0f)
                return QuaternionDouble.Lerp(QuaternionDouble.identity, GetUpVectorFromSphericalGeoCoordinate(geoCoordinate), sphericalRatio);
            else if (sphericalRatio == 0.0f)
                return QuaternionDouble.identity;
            else if (sphericalRatio == 1.0f)
                return GetUpVectorFromSphericalGeoCoordinate(geoCoordinate);
            return QuaternionDouble.identity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static QuaternionDouble GetUpVectorFromSphericalGeoCoordinate(GeoCoordinate2Double geoCoordinate)
        {
            double clat = Math.Cos(0.5d * (DEG2RAD * geoCoordinate.latitude));
            double clon = Math.Cos(0.5d * (DEG2RAD * geoCoordinate.longitude));
            double slat = Math.Sin(0.5d * (DEG2RAD * geoCoordinate.latitude));
            double slon = Math.Sin(0.5d * (DEG2RAD * geoCoordinate.longitude));

            double qw = clat * clon;
            double qx = 0.0d - clat * slon;
            double qy = slat * clon;
            double qz = 0.0d - slat * slon;

            return new QuaternionDouble(qy, qz, qx, qw);
        }
    }
}
