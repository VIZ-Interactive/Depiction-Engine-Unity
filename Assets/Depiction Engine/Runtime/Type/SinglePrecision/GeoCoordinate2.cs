// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace DepictionEngine
{
    /// <summary>
    /// A GeoCoordinate composed of a latitude and longitude component.
    /// </summary>
    [Serializable]
    public struct GeoCoordinate2
    {
        public static GeoCoordinate2 zero = new GeoCoordinate2();
        public static GeoCoordinate2 empty = new GeoCoordinate2(-1,-1);
        public static GeoCoordinate2 positiveInfinity = new GeoCoordinate2(float.PositiveInfinity, float.PositiveInfinity);

        public float latitude;
        public float longitude;

        public GeoCoordinate2(float latitude = 0.0f, float longitude = 0.0f)
        {
            this.latitude = GeoCoordinate3.ValidateLatitude(latitude);
            this.longitude = GeoCoordinate3.ValidateLongitude(longitude);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Distance(GeoCoordinate2 geoCoordinate, float radius)
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
            GeoCoordinate2 coord = (GeoCoordinate2)otherCoord;
            return latitude.Equals(coord.latitude) && longitude.Equals(coord.longitude);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Approximately(GeoCoordinate2 coord)
        {
            int digits = 14;
            if (coord != null)
                return latitude.Round(digits) == coord.latitude.Round(digits) && longitude.Round(digits) == coord.longitude.Round(digits);
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator GeoCoordinate2(GeoCoordinate3 geo) => new GeoCoordinate2(geo.latitude, geo.longitude);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GeoCoordinate2 Round(GeoCoordinate2 geo, int decimals)
        {
            return new GeoCoordinate2(geo.latitude.Round(decimals), geo.longitude.Round(decimals));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(GeoCoordinate2 coord,GeoCoordinate2 otherCoord)
        {
            return coord.latitude == otherCoord.latitude && coord.longitude == otherCoord.longitude;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(GeoCoordinate2 coord, GeoCoordinate2 otherCoord)
        {
            return coord.latitude != otherCoord.latitude || coord.longitude != otherCoord.longitude;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GeoCoordinate2 Lerp(GeoCoordinate2 value1, GeoCoordinate2 value2, float t)
        {
                return new GeoCoordinate2(value1.latitude + t * (value2.latitude - value1.latitude), value1.longitude + t * (value2.longitude - value1.longitude));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
        {
            return latitude + ", " + longitude;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return 0;
        }
    }
}
