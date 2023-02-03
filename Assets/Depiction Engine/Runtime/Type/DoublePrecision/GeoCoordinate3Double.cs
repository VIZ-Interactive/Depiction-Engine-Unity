// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Runtime.CompilerServices;

namespace DepictionEngine
{
    /// <summary>
    /// A 64 bit double version of the GeoCoordinate3.
    /// </summary>
    [Serializable]
    public struct GeoCoordinate3Double
    {
        private static readonly GeoCoordinate3Double ZERO_GEOCOORDINATE = new GeoCoordinate3Double();

        public static GeoCoordinate3Double zero { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return ZERO_GEOCOORDINATE; } }

        public double latitude;
        public double longitude;
        public double altitude;

        public GeoCoordinate3Double(double latitude = 0.0d, double longitude = 0.0d, double altitude = 0.0d)
        {
            this.latitude = ValidateLatitude(latitude);
            this.longitude = ValidateLongitude(longitude);
            this.altitude = altitude;
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
            GeoCoordinate3Double coord = (GeoCoordinate3Double) otherCoord;
            if(coord != null)
                return latitude == coord.latitude && longitude == coord.longitude && altitude == coord.altitude;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator GeoCoordinate3Double(GeoCoordinate2Double geo) => new GeoCoordinate3Double(geo.latitude, geo.longitude);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GeoCoordinate3Double Round(GeoCoordinate3Double geo, int decimals)
        {
            return new GeoCoordinate3Double(Math.Round(geo.latitude, decimals), Math.Round(geo.longitude, decimals), Math.Round(geo.altitude, decimals));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GeoCoordinate3Double operator +(GeoCoordinate3Double coord, GeoCoordinate3Double otherCoord)
        {
            return new GeoCoordinate3Double(coord.latitude + otherCoord.latitude, MathPlus.ClampAngle180(coord.longitude + otherCoord.longitude), coord.altitude + otherCoord.altitude);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GeoCoordinate3Double operator +(GeoCoordinate3Double coord, GeoCoordinate2Double otherCoord)
        {
            return new GeoCoordinate3Double(coord.latitude + otherCoord.latitude, MathPlus.ClampAngle180(coord.longitude + otherCoord.longitude), coord.altitude);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(GeoCoordinate3Double coord,GeoCoordinate3Double otherCoord)
        {
            return coord.latitude == otherCoord.latitude && coord.longitude == otherCoord.longitude && coord.altitude == otherCoord.altitude;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(GeoCoordinate3Double coord, GeoCoordinate3Double otherCoord)
        {
            return coord.latitude != otherCoord.latitude || coord.longitude != otherCoord.longitude || coord.altitude != otherCoord.altitude;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GeoCoordinate3Double Lerp(GeoCoordinate3Double value1,GeoCoordinate3Double value2, float t)
        {
            return new GeoCoordinate3Double(MathPlus.Lerp(value1.latitude, value2.latitude, t), MathPlus.Lerp(value1.longitude, value2.longitude, t), MathPlus.Lerp(value1.altitude, value2.altitude, t));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator GeoCoordinate3Double(GeoCoordinate3 geoCoordinate)
        {
            return new GeoCoordinate3Double(geoCoordinate.latitude, geoCoordinate.longitude, geoCoordinate.altitude);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator GeoCoordinate3Double(GeoCoordinate2 geoCoordinate)
        {
            return new GeoCoordinate3Double(geoCoordinate.latitude, geoCoordinate.longitude, 0.0d);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ValidateLatitude(double value)
        {
            return MathPlus.Clamp(value, -90.0d, 90.0d);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ValidateLongitude(double value)
        {
            return MathPlus.ClampAngle180(value);
        }
    }
}
