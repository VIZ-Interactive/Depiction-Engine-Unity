// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Runtime.CompilerServices;

namespace DepictionEngine
{
    /// <summary>
    /// A 64 bit double version of the GeoCoordinate2.
    /// </summary>
    [Serializable]
    public struct GeoCoordinate2Double
    {
        private static readonly GeoCoordinate2Double ZERO_GEOCOORDINATE = new GeoCoordinate2Double();

        public static GeoCoordinate2Double zero { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return ZERO_GEOCOORDINATE; } }

        public double latitude;
        public double longitude;

        public GeoCoordinate2Double(double latitude = 0.0d, double longitude = 0.0d)
        {
            this.latitude = GeoCoordinate3Double.ValidateLatitude(latitude);
            this.longitude = GeoCoordinate3Double.ValidateLongitude(longitude);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3Double UpVector()
        {
            double latRad = latitude * MathPlus.DEG2RAD;
            double lonRad = longitude * MathPlus.DEG2RAD;
            return new Vector3Double(Math.Cos(latRad) * Math.Sin(lonRad), Math.Cos(latRad) * Math.Cos(lonRad), Math.Sin(latRad));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object otherCoord)
        {
            int digits = 14;
            GeoCoordinate2Double coord = (GeoCoordinate2Double) otherCoord;
            if(coord != null)
                return Math.Round(latitude, digits) == Math.Round(coord.latitude, digits) && Math.Round(longitude, digits) == Math.Round(coord.longitude, digits);
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator GeoCoordinate2Double(GeoCoordinate3Double geo) => new GeoCoordinate2Double(geo.latitude, geo.longitude);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GeoCoordinate2Double Round(GeoCoordinate2Double geo, int decimals)
        {
            return new GeoCoordinate2Double(Math.Round(geo.latitude, decimals), Math.Round(geo.longitude, decimals));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(GeoCoordinate2Double coord,GeoCoordinate2Double otherCoord)
        {
            return coord.latitude == otherCoord.latitude && coord.longitude == otherCoord.longitude;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(GeoCoordinate2Double coord, GeoCoordinate2Double otherCoord)
        {
            return coord.latitude != otherCoord.latitude || coord.longitude != otherCoord.longitude;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GeoCoordinate2Double Lerp(GeoCoordinate2Double value1,GeoCoordinate2Double value2, float t)
        {
            return new GeoCoordinate2Double(MathPlus.Lerp(value1.latitude, value2.latitude, t), MathPlus.Lerp(value1.longitude, value2.longitude, t));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator GeoCoordinate2Double(GeoCoordinate2 geoCoordinate)
        {
            return new GeoCoordinate2Double(geoCoordinate.latitude, geoCoordinate.longitude);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator GeoCoordinate2(GeoCoordinate2Double geoCoordinate)
        {
            return new GeoCoordinate2((float)geoCoordinate.latitude, (float)geoCoordinate.longitude);
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
