// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace DepictionEngine
{
    /// <summary>
    /// A 64 bit double version of the Vector2.
    /// </summary>
    [Serializable]
    public struct Vector2Double
    {
        public static Vector2Double zero = new Vector2Double();
        public static Vector2Double one = new Vector2Double(1, 1);
        public static Vector2Double minusOne = new Vector2Double(-1, -1);
        public static Vector2Double positiveInfinity = new Vector2Double(double.PositiveInfinity, double.PositiveInfinity);
        public static Vector2Double negativeInfinity = new Vector2Double(double.NegativeInfinity, double.NegativeInfinity);

        public double x;
        public double y;

        public Vector2Double(double x, double y)
        {
            this.x = x;
            this.y = y;
        }

        public Vector2Double(Vector2 vec)
        {
            x = vec.x;
            y = vec.y;
        }

        public Vector2Double normalized
        {
            get
            {
                double num = Magnitude(this);
                if (num > 9.99999974737875E-06)
                    return this / num;
                return zero;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Double Round(Vector2Double vec, int decimals)
        {
            return new Vector2Double(Math.Round(vec.x, decimals), Math.Round(vec.y, decimals));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Magnitude(Vector2Double value)
        {
            return Math.Sqrt(value.x * value.x + value.y * value.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Distance(Vector2Double vec1, Vector2Double vec2)
        {
            double deltaX = vec2.x - vec1.x;
            double deltaY = vec2.y - vec1.y;
            return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Double Scale(Vector2Double vec1, Vector2 vec2)
        {
            return new Vector2Double(vec1.x * vec2.x, vec1.y * vec2.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Double Scale(Vector2Double vec1, Vector2Double vec2)
        {
            return new Vector2Double(vec1.x * vec2.x, vec1.y * vec2.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Double operator *(Vector2Double value1, Double value2)
        {
            return new Vector2Double(value1.x * value2, value1.y * value2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Double operator *(Vector2Double value1, Vector2Double value2)
        {
            return new Vector2Double(value1.x * value2.x, value1.y * value2.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Double operator +(Vector2Double value1, Vector2Double value2)
        {
            return new Vector2Double(value1.x + value2.x,value1.y + value2.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Double operator +(Vector2Double value1, Vector2 value2)
        {
            return new Vector2Double(value1.x + value2.x, value1.y + value2.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Double operator +(Vector2Double value1, Vector2Int value2)
        {
            return new Vector2Double(value1.x + value2.x, value1.y + value2.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Double operator -(Vector2Double value1, Vector2Double value2)
        {
            return new Vector2Double(value1.x - value2.x, value1.y - value2.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Double operator -(Vector2Double value1, Vector2 value2)
        {
            return new Vector2Double(value1.x - value2.x, value1.y - value2.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Double operator -(Vector2Double value1, Vector2Int value2)
        {
            return new Vector2Double(value1.x - value2.x, value1.y - value2.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Double operator -(Vector2Double value1)
        {
            return new Vector2Double(-value1.x, -value1.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Double operator /(Vector2Double value1, Double value2)
        {
            return new Vector2Double(value1.x / value2, value1.y / value2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object value)
        {
            if (value != null && value is Vector2Double)
                return false;
            Vector2Double vec2 = (Vector2Double)value;
            return x == vec2.x && y == vec2.y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Vector2Double value1, Vector2Double value2)
        {
            return value1.x == value2.x && value1.y == value2.y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Double operator %(Vector2Double value, double mod)
        {
            return new Vector2Double(value.x % mod, value.y % mod);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Vector2Double value1, Vector2Double value2)
        {
            return value1.x != value2.x || value1.y != value2.y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Vector2Double value1, Vector2 value2)
        {
            return value1.x == value2.x || value1.y == value2.y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Vector2Double value1, Vector2 value2)
        {
            return value1.x != value2.x || value1.y != value2.y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Vector2Double(Vector2 vec) 
        {
            return new Vector2Double(vec.x, vec.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Vector2Double(Vector2Int vec)
        {
            return new Vector2Double(vec.x, vec.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Vector2Double(Vector3Double vec)
        {
            return new Vector2Double(vec.x, vec.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Vector2(Vector2Double vec)
        {
            return new Vector2((float)vec.x, (float)vec.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Vector2Double(Vector3 vec)
        {
            return new Vector2Double(vec.x, vec.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Double Lerp(Vector2Double a, Vector2Double b, double t)
        {
            t = MathPlus.Clamp01(t);
            return new Vector2Double(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Double Lerp(Vector2Double a, Vector2Double b, float t)
        {
            t = Mathf.Clamp01(t);
            if (t == 0)
                return a;
            if (t == 1)
                return b;
            return new Vector2Double(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Double Convert(Vector2 vec)
        {
            return new Vector2Double(double.Parse(vec.x.ToString()), double.Parse(vec.y.ToString()));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
        {
            return "("+x + ", " + y +")";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return 0;
        }
    }
}
