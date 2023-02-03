// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace DepictionEngine
{
    /// <summary>
    /// A 64 bit double version of the Vector3.
    /// </summary>
    [Serializable]
    public struct Vector3Double : IComparable, IComparable<Vector3Double>, IEquatable<Vector3Double>
    {
        public static readonly Vector3Double zero = new Vector3Double();
        public static readonly Vector3Double one = new Vector3Double(1.0d, 1.0d, 1.0d);
        public static readonly Vector3Double up = new Vector3Double(0.0d, 1.0d, 0.0d);
        public static readonly Vector3Double down = new Vector3Double(0.0d, -1.0d, 0.0d);
        public static readonly Vector3Double forward = new Vector3Double(0.0d, 0.0d, 1.0d);
        public static readonly Vector3Double back = new Vector3Double(0.0d, 0.0d, -1.0d);
        public static readonly Vector3Double right = new Vector3Double(1.0d, 0.0d, 0.0d);
        public static readonly Vector3Double left = new Vector3Double(-1.0d, 0.0d, 0.0d);
        public static readonly Vector3Double positiveInfinity = new Vector3Double(double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity);
        public static readonly Vector3Double negativeInfinity = new Vector3Double(double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity);

        public const double kEpsilonNormalSqrt = 1e-15d;

        public double x;
        public double y;
        public double z;

        public Vector3Double(double x, double y, double z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Vector3Double(double x, double y)
        {
            this.x = x;
            this.y = y;
            this.z = 0.0d;
        }

        public double this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return x;
                    case 1:
                        return y;
                    case 2:
                        return z;
                    default:
                        throw new IndexOutOfRangeException("Invalid Vector3Double index!");
                }
            }
            set
            {
                switch (index)
                {
                    case 0:
                        x = value;
                        break;
                    case 1:
                        y = value;
                        break;
                    case 2:
                        z = value;
                        break;
                    default:
                        throw new IndexOutOfRangeException("Invalid Vector3Double index!");
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Normalize()
        {
            double num = Magnitude(this);
            if (num > 9.99999974737875E-06)
            {
                x /= num;
                y /= num;
                z /= num;
            }
            else
                x = y = z = 0.0d;
        }

        public Vector3Double normalized
        {
            get
            {
                double num = Magnitude(this);
                if (num > 9.99999974737875E-06)
                    return this / num;
                return zero;
            }
        }

        public double magnitude
        {
            get { return Magnitude(this); }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3Double Reflect(Vector3Double inDirection, Vector3Double inNormal)
        {
            double factor = -2.0d * Dot(inNormal, inDirection);
            return new Vector3Double(factor * inNormal.x + inDirection.x,
                factor * inNormal.y + inDirection.y,
                factor * inNormal.z + inDirection.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3Double Cross(Vector3Double vec1, Vector3Double vec2)
        {
            return new Vector3Double(vec1.y * vec2.z - vec1.z * vec2.y, vec1.z * vec2.x - vec1.x * vec2.z, vec1.x * vec2.y - vec1.y * vec2.x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Dot(Vector3Double vec1, Vector3Double vec2)
        {
            return vec1.x * vec2.x + vec1.y * vec2.y + vec1.z * vec2.z;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3Double Round(Vector3Double vec, int decimals)
        {
            return new Vector3Double(Math.Round(vec.x, decimals), Math.Round(vec.y, decimals), Math.Round(vec.z, decimals));
        }

        public double sqrMagnitude { get { return x * x + y * y + z * z; } }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Magnitude(Vector3Double value)
        {
            return Math.Sqrt(value.x * value.x + value.y * value.y + value.z * value.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3Double Normalize(Vector3Double value)
        {
            double num = Magnitude(value);
            if (num > 9.99999974737875E-06)
                return value / num;
            return zero;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Distance(Vector3Double vec1, Vector3Double vec2)
        {
            double deltaX = vec2.x - vec1.x;
            double deltaY = vec2.y - vec1.y;
            double deltaZ = vec2.z - vec1.z;
            return Math.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Angle(Vector3Double from, Vector3Double to)
        {
            // sqrt(a) * sqrt(b) = sqrt(a * b) -- valid for real numbers
            double denominator = Math.Sqrt(from.sqrMagnitude * to.sqrMagnitude);
            if (denominator < kEpsilonNormalSqrt)
                return 0.0d;

            double dot = MathPlus.Clamp(Dot(from, to) / denominator, -1.0d, 1.0d);
            return (Math.Acos(dot)) * MathPlus.RAD2DEG;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double SignedAngle(Vector3Double from, Vector3Double to, Vector3Double axis)
        {
            double unsignedAngle = Angle(from, to);

            double cross_x = from.y * to.z - from.z * to.y;
            double cross_y = from.z * to.x - from.x * to.z;
            double cross_z = from.x * to.y - from.y * to.x;
            double sign = Math.Sign(axis.x * cross_x + axis.y * cross_y + axis.z * cross_z);
            return unsignedAngle * sign;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3Double Scale(Vector3Double value1, Vector3Double value2)
        {
            return new Vector3Double(value1.x * value2.x, value1.y * value2.y, value1.z * value2.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3Double Scale(Vector3Double value1, Vector3 value2)
        {
            return new Vector3Double(value1.x * value2.x, value1.y * value2.y, value1.z * value2.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3Double operator *(Quaternion rotation, Vector3Double point)
        {
            double num1 = rotation.x * 2.0d;
            double num2 = rotation.y * 2.0d;
            double num3 = rotation.z * 2.0d;
            double num4 = rotation.x * num1;
            double num5 = rotation.y * num2;
            double num6 = rotation.z * num3;
            double num7 = rotation.x * num2;
            double num8 = rotation.x * num3;
            double num9 = rotation.y * num3;
            double num10 = rotation.w * num1;
            double num11 = rotation.w * num2;
            double num12 = rotation.w * num3;
            return new Vector3Double((1.0d - (num5 + num6)) * point.x + (num7 - num12) * point.y + (num8 + num11) * point.z, (num7 + num12) * point.x + (1.0d - (num4 + num6)) * point.y + (num9 - num10) * point.z, (num8 - num11) * point.x + (num9 + num10) * point.y + (1.0d - (num4 + num5)) * point.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3Double operator *(Vector3Double value, Matrix4x4Double matrix)
        {
            return new Vector3Double(matrix.m00 * value.x + matrix.m01 * value.y + matrix.m02 * value.z, matrix.m10 * value.x + matrix.m11 * value.y + matrix.m12 * value.z, matrix.m20 * value.x + matrix.m21 * value.y + matrix.m22 * value.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3Double operator *(Matrix4x4Double matrix, Vector3Double value)
        {
            return new Vector3Double(matrix.m00 * value.x + matrix.m01 * value.y + matrix.m02 * value.z, matrix.m10 * value.x + matrix.m11 * value.y + matrix.m12 * value.z, matrix.m20 * value.x + matrix.m21 * value.y + matrix.m22 * value.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3Double operator *(double value1, Vector3Double value2)
        {
            return new Vector3Double(value2.x * value1, value2.y * value1, value2.z * value1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3Double operator *(Vector3Double value1, double value2)
        {
            return new Vector3Double(value1.x * value2, value1.y * value2, value1.z * value2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3Double operator *(Vector3Double value1, Vector3Double value2)
        {
            return new Vector3Double(value1.x * value2.x, value1.y * value2.y, value1.z * value2.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3Double operator +(Vector3Double value1, Vector3Double value2)
        {
            return new Vector3Double(value1.x + value2.x, value1.y + value2.y, value1.z + value2.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3Double operator +(Vector3Double value1, Vector3 value2)
        {
            return new Vector3Double(value1.x + value2.x, value1.y + value2.y, value1.z + value2.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3Double operator +(Vector3Double value1, Vector2Double value2)
        {
            return new Vector3Double(value1.x + value2.x, value1.y + value2.y, value1.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3Double operator -(Vector3Double value1, Vector3Double value2)
        {
            return new Vector3Double(value1.x - value2.x, value1.y - value2.y, value1.z - value2.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3Double operator -(Vector3Double value1, Vector3 value2)
        {
            return new Vector3Double(value1.x - value2.x, value1.y - value2.y, value1.z - value2.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3Double operator -(Vector3Double value1)
        {
            return new Vector3Double(-value1.x, -value1.y, -value1.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3Double operator /(Vector3Double value1, double value2)
        {
            return new Vector3Double(value1.x / value2, value1.y / value2, value1.z / value2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object value)
        {
            Vector3Double vec = (Vector3Double)value;
            if (vec != null)
                return x == vec.x && y == vec.y && z == vec.z;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Vector3Double other)
        {
            return x == other.x && y == other.y && z == other.z;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3Double operator /(Vector3Double value, Vector3Double value2)
        {
            return new Vector3Double(value.x / value2.x, value.y / value2.y, value.z / value2.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3Double operator /(Vector3Double value, Vector3 value2)
        {
            return new Vector3Double(value.x / value2.x, value.y / value2.y, value.z / value2.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Vector3Double value1, Vector3Double value2)
        {
            return value1.x == value2.x && value1.y == value2.y && value1.z == value2.z;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3Double operator %(Vector3Double value, double mod)
        {
            return new Vector3Double(value.x % mod, value.y % mod, value.z % mod);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Vector3Double value1, Vector3Double value2)
        {
            return value1.x != value2.x || value1.y != value2.y || value1.z != value2.z;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Vector3Double value1, Vector3 value2)
        {
            return value1.x == value2.x && value1.y == value2.y && value1.z == value2.z;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Vector3Double value1, Vector3 value2)
        {
            return value1.x != value2.x || value1.y != value2.y || value1.z != value2.z;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(Vector3Double other)
        {
            return Equals(other) ? 1 : 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(object other)
        {
            return other is Vector3Double && Equals((Vector3Double)other) ? 1 : 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Vector3Double(Vector2Double vec)
        {
            return new Vector3Double(vec.x, vec.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Vector3Double(Vector2 vec)
        {
            return new Vector3Double(vec.x, vec.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Vector3Double(Vector3 vec)
        {
            return new Vector3Double(vec.x, vec.y, vec.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Vector3(Vector3Double vec)
        {
            return new Vector3((float)vec.x, (float)vec.y, (float)vec.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3Double Lerp(Vector3Double a, Vector3Double b, double t)
        {
            t = MathPlus.Clamp01(t);
            return new Vector3Double(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3Double Lerp(Vector3Double a, Vector3Double b, float t)
        {
            t = Mathf.Clamp01(t);
            if (t == 0)
                return a;
            if (t == 1)
                return b;
            return new Vector3Double(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ToString(string format)
        {
            return "(" + x.ToString(format) + ", " + y.ToString(format) + ", " + z.ToString(format) + ")";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
        {
            return "(" + x + ", " + y + ", " + z + ")";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return 0;
        }
    }
}
