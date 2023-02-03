// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace DepictionEngine
{
    /// <summary>
    /// A GeoCoordinate composed of a latitude, longitude and altitude component.
    /// </summary>
    [Serializable]
    public struct GeoCoordinate3
    {
        public static GeoCoordinate3 zero = new GeoCoordinate3();
        public static GeoCoordinate3 positiveInfinity = new GeoCoordinate3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        public static GeoCoordinate3 negativeInfinity = new GeoCoordinate3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

        public float latitude;
        public float longitude;
        public float altitude;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GeoCoordinate3(float latitude = 0, float longitude = 0, float altitude = 0)
        {
            this.latitude = ValidateLatitude(latitude);
            this.longitude = ValidateLongitude(longitude);
            this.altitude = altitude;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Distance(GeoCoordinate3 geoCoordinate, float radius)
        {
            float dLat = (geoCoordinate.latitude - latitude) * Mathf.PI / 180.0f;
            float dLon = (geoCoordinate.longitude - longitude) * Mathf.PI / 180.0f;

            float lat1inRadians = latitude * Mathf.PI / 180.0f;
            float lat2inRadians = geoCoordinate.latitude * Mathf.PI / 180.0f;

            float a = Mathf.Sin(dLat / 2.0f) * Mathf.Sin(dLat / 2.0f) +
                    Mathf.Sin(dLon / 2.0f) * Mathf.Sin(dLon / 2.0f) *
                    Mathf.Cos(lat1inRadians) * Mathf.Cos(lat2inRadians);
            float c = 2.0f * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1.0f - a));
            float d = radius * c;

            return d;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 UpVector()
        {
            float latRad = latitude * Mathf.Deg2Rad;
            float lonRad = longitude * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(latRad) * Mathf.Sin(lonRad), Mathf.Cos(latRad) * Mathf.Cos(lonRad), Mathf.Sin(latRad));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object otherCoord)
        {
            if (otherCoord == null)
                return false;
            GeoCoordinate3 coord = (GeoCoordinate3)otherCoord;
            return latitude.Equals(coord.latitude) && longitude.Equals(coord.longitude) && altitude.Equals(coord.altitude);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Approximately(GeoCoordinate3 coord)
        {
            int digits = 14;
            if (coord != null)
                return latitude.Round(digits) == coord.latitude.Round(digits) && longitude.Round(digits) == coord.longitude.Round(digits) && altitude.Round(digits) == coord.altitude.Round(digits);
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator GeoCoordinate3(GeoCoordinate2 geo) => new GeoCoordinate3(geo.latitude, geo.longitude, 0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GeoCoordinate3 Round(GeoCoordinate3 geo, int decimals)
        {
            return new GeoCoordinate3(geo.latitude.Round(decimals), geo.longitude.Round(decimals), geo.altitude.Round(decimals));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GeoCoordinate3 operator +(GeoCoordinate3 coord, GeoCoordinate3 otherCoord)
        {
            return new GeoCoordinate3(coord.latitude + otherCoord.latitude, MathPlus.ClampAngle180(coord.longitude + otherCoord.longitude), coord.altitude + otherCoord.altitude);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GeoCoordinate3 operator +(GeoCoordinate3 coord, GeoCoordinate2 otherCoord)
        {
            return new GeoCoordinate3(coord.latitude + otherCoord.latitude, MathPlus.ClampAngle180(coord.longitude + otherCoord.longitude), coord.altitude);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(GeoCoordinate3 coord,GeoCoordinate3 otherCoord)
        {
            return coord.latitude == otherCoord.latitude && coord.longitude == otherCoord.longitude && coord.altitude == otherCoord.altitude;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(GeoCoordinate3 coord, GeoCoordinate3 otherCoord)
        {
            return coord.latitude != otherCoord.latitude || coord.longitude != otherCoord.longitude || coord.altitude != otherCoord.altitude;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator GeoCoordinate3(GeoCoordinate3Double geoCoordinate)
        {
            return new GeoCoordinate3((float)geoCoordinate.latitude, (float)geoCoordinate.longitude, (float)geoCoordinate.altitude);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GeoCoordinate3 Lerp(GeoCoordinate3 value1, GeoCoordinate3 value2, float t)
        {
                return new GeoCoordinate3(value1.latitude + t * (value2.latitude - value1.latitude), value1.longitude + t * (value2.longitude - value1.longitude), value1.altitude + t * (value2.altitude - value1.altitude));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ValidateLatitude(float value)
        {
            return Mathf.Clamp(value, -90, 90);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ValidateLongitude(float value)
        {
            return Mathf.Clamp(value, -180, 180);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
        {
            return latitude + ", " + longitude + ", " + altitude;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return 0;
        }
    }
}
