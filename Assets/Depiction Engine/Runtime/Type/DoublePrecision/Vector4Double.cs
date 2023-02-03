// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace DepictionEngine
{
    /// <summary>
    /// A 64 bit double version of the Vector4.
    /// </summary>
    [Serializable]
    public partial struct Vector4Double : IEquatable<Vector4Double>, IFormattable
    {
        private const double kEpsilon = 0.000000000001f;

        public double x;
        public double y;
        public double z;
        public double w;

        // Access the x, y, z, w components using [0], [1], [2], [3] respectively.
        public double this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return x;
                    case 1: return y;
                    case 2: return z;
                    case 3: return w;
                    default:
                        throw new IndexOutOfRangeException("Invalid Vector4 index!");
                }
            }

            set
            {
                switch (index)
                {
                    case 0: x = value; break;
                    case 1: y = value; break;
                    case 2: z = value; break;
                    case 3: w = value; break;
                    default:
                        throw new IndexOutOfRangeException("Invalid Vector4 index!");
                }
            }
        }

        // Creates a new vector with given x, y, z, w components.
        public Vector4Double(double x, double y, double z, double w) { this.x = x; this.y = y; this.z = z; this.w = w; }
        // Creates a new vector with given x, y, z components and sets /w/ to zero.
        public Vector4Double(double x, double y, double z) { this.x = x; this.y = y; this.z = z; this.w = 0.0d; }
        // Creates a new vector with given x, y components and sets /z/ and /w/ to zero.
        public Vector4Double(double x, double y) { this.x = x; this.y = y; this.z = 0.0d; this.w = 0.0d; }
        public Vector4Double(Vector3Double xyz, double w) { this.x = xyz.x; this.y = xyz.y; this.z = xyz.z; this.w = w; }

        // Set x, y, z and w components of an existing Vector4.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(double newX, double newY, double newZ, double newW) { x = newX; y = newY; z = newZ; w = newW; }

        // Linearly interpolates between two vectors.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4Double Lerp(Vector4Double a, Vector4Double b, float t)
        {
            t = Mathf.Clamp01(t);
            return new Vector4Double(
                a.x + (b.x - a.x) * t,
                a.y + (b.y - a.y) * t,
                a.z + (b.z - a.z) * t,
                a.w + (b.w - a.w) * t
            );
        }

        // Linearly interpolates between two vectors without clamping the interpolant
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4Double LerpUnclamped(Vector4Double a, Vector4Double b, float t)
        {
            return new Vector4Double(
                a.x + (b.x - a.x) * t,
                a.y + (b.y - a.y) * t,
                a.z + (b.z - a.z) * t,
                a.w + (b.w - a.w) * t
            );
        }

        // Moves a point /current/ towards /target/.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4Double MoveTowards(Vector4Double current, Vector4Double target, float maxDistanceDelta)
        {
            double toVector_x = target.x - current.x;
            double toVector_y = target.y - current.y;
            double toVector_z = target.z - current.z;
            double toVector_w = target.w - current.w;

            double sqdist = (toVector_x * toVector_x +
                toVector_y * toVector_y +
                toVector_z * toVector_z +
                toVector_w * toVector_w);

            if (sqdist == 0 || (maxDistanceDelta >= 0 && sqdist <= maxDistanceDelta * maxDistanceDelta))
                return target;

            double dist = Math.Sqrt(sqdist);

            return new Vector4Double(current.x + toVector_x / dist * maxDistanceDelta,
                current.y + toVector_y / dist * maxDistanceDelta,
                current.z + toVector_z / dist * maxDistanceDelta,
                current.w + toVector_w / dist * maxDistanceDelta);
        }

        // Multiplies two vectors component-wise.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4Double Scale(Vector4Double a, Vector4Double b)
        {
            return new Vector4Double(a.x * b.x, a.y * b.y, a.z * b.z, a.w * b.w);
        }

        // Multiplies every component of this vector by the same component of /scale/.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Scale(Vector4Double scale)
        {
            x *= scale.x;
            y *= scale.y;
            z *= scale.z;
            w *= scale.w;
        }

        // used to allow Vector4s to be used as keys in hash tables
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return x.GetHashCode() ^ (y.GetHashCode() << 2) ^ (z.GetHashCode() >> 2) ^ (w.GetHashCode() >> 1);
        }

        // also required for being able to use Vector4s as keys in hash tables
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object other)
        {
            if (!(other is Vector4Double)) return false;

            return Equals((Vector4Double)other);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Vector4Double other)
        {
            return x == other.x && y == other.y && z == other.z && w == other.w;
        }

        // *undoc* --- we have normalized property now
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4Double Normalize(Vector4Double a)
        {
            double mag = Magnitude(a);
            if (mag > kEpsilon)
                return a / mag;
            else
                return zero;
        }

        // Makes this vector have a ::ref::magnitude of 1.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Normalize()
        {
            double mag = Magnitude(this);
            if (mag > kEpsilon)
                this = this / mag;
            else
                this = zero;
        }

        // Returns this vector with a ::ref::magnitude of 1 (RO).
        public Vector4Double normalized
        {
            get
            {
                return Vector4Double.Normalize(this);
            }
        }

        // Dot Product of two vectors.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Dot(Vector4Double a, Vector4Double b) { return a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w; }

        // Projects a vector onto another vector.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4Double Project(Vector4Double a, Vector4Double b) { return b * (Dot(a, b) / Dot(b, b)); }

        // Returns the distance between /a/ and /b/.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Distance(Vector4Double a, Vector4Double b) { return Magnitude(a - b); }

        // *undoc* --- there's a property now
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Magnitude(Vector4Double a) { return Math.Sqrt(Dot(a, a)); }

        // Returns the length of this vector (RO).
        public double magnitude { get { return Math.Sqrt(Dot(this, this)); } }

        // Returns the squared length of this vector (RO).
        public double sqrMagnitude { get { return Dot(this, this); } }

        // Returns a vector that is made from the smallest components of two vectors.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4Double Min(Vector4Double lhs, Vector4Double rhs)
        {
            return new Vector4Double(Math.Min(lhs.x, rhs.x), Math.Min(lhs.y, rhs.y), Math.Min(lhs.z, rhs.z), Math.Min(lhs.w, rhs.w));
        }

        // Returns a vector that is made from the largest components of two vectors.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4Double Max(Vector4Double lhs, Vector4Double rhs)
        {
            return new Vector4Double(Math.Max(lhs.x, rhs.x), Math.Max(lhs.y, rhs.y), Math.Max(lhs.z, rhs.z), Math.Max(lhs.w, rhs.w));
        }

        static readonly Vector4Double zeroVector = new Vector4Double(0.0d, 0.0d, 0.0d, 0.0d);
        static readonly Vector4Double oneVector = new Vector4Double(1.0d, 1.0d, 1.0d, 1.0d);
        static readonly Vector4Double positiveInfinityVector = new Vector4Double(double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity);
        static readonly Vector4Double negativeInfinityVector = new Vector4Double(double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity);

        // Shorthand for writing @@Vector4(0,0,0,0)@@
        public static Vector4Double zero { get { return zeroVector; } }
        // Shorthand for writing @@Vector4(1,1,1,1)@@
        public static Vector4Double one { get { return oneVector; } }
        // Shorthand for writing @@Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity)@@
        public static Vector4Double positiveInfinity { get { return positiveInfinityVector; } }
        // Shorthand for writing @@Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity)@@
        public static Vector4Double negativeInfinity { get { return negativeInfinityVector; } }

        // Adds two vectors.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4Double operator +(Vector4Double a, Vector4Double b) { return new Vector4Double(a.x + b.x, a.y + b.y, a.z + b.z, a.w + b.w); }
        // Subtracts one vector from another.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4Double operator -(Vector4Double a, Vector4Double b) { return new Vector4Double(a.x - b.x, a.y - b.y, a.z - b.z, a.w - b.w); }
        // Negates a vector.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4Double operator -(Vector4Double a) { return new Vector4Double(-a.x, -a.y, -a.z, -a.w); }
        // Multiplies a vector by a number.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4Double operator *(Vector4Double a, double d) { return new Vector4Double(a.x * d, a.y * d, a.z * d, a.w * d); }
        // Multiplies a vector by a number.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4Double operator *(double d, Vector4Double a) { return new Vector4Double(a.x * d, a.y * d, a.z * d, a.w * d); }
        // Divides a vector by a number.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4Double operator /(Vector4Double a, double d) { return new Vector4Double(a.x / d, a.y / d, a.z / d, a.w / d); }

        // Returns true if the vectors are equal.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Vector4Double lhs, Vector4Double rhs)
        {
            // Returns false in the presence of NaN values.
            double diffx = lhs.x - rhs.x;
            double diffy = lhs.y - rhs.y;
            double diffz = lhs.z - rhs.z;
            double diffw = lhs.w - rhs.w;
            double sqrmag = diffx * diffx + diffy * diffy + diffz * diffz + diffw * diffw;
            return sqrmag < kEpsilon * kEpsilon;
        }

        // Returns true if vectors are different.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Vector4Double lhs, Vector4Double rhs)
        {
            // Returns true in the presence of NaN values.
            return !(lhs == rhs);
        }

        // Converts a [[Vector3]] to a Vector4.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Vector4Double(Vector3Double v)
        {
            return new Vector4Double(v.x, v.y, v.z, 0.0F);
        }

        // Converts a Vector4 to a [[Vector3]].
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Vector3Double(Vector4Double v)
        {
            return new Vector3Double(v.x, v.y, v.z);
        }

        // Converts a [[Vector2]] to a Vector4.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Vector4Double(Vector2Double v)
        {
            return new Vector4Double(v.x, v.y, 0.0F, 0.0F);
        }

        // Converts a Vector4 to a [[Vector2]].
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Vector2Double(Vector4Double v)
        {
            return new Vector2Double(v.x, v.y);
        }

        // Converts a DoubleVector4 to a [[Vector4]].
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Vector4Double(Vector4 v)
        {
            return new Vector4Double(v.x, v.y, v.z, v.w);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
        {
            return ToString(null, CultureInfo.InvariantCulture.NumberFormat);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ToString(string format)
        {
            return ToString(format, CultureInfo.InvariantCulture.NumberFormat);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ToString(string format, IFormatProvider formatProvider)
        {
            if (string.IsNullOrEmpty(format))
                format = "F1";
            return Format("({0}, {1}, {2}, {3})", x.ToString(format, formatProvider), y.ToString(format, formatProvider), z.ToString(format, formatProvider), w.ToString(format, formatProvider));
        }

        // *undoc* --- there's a property now
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double SqrMagnitude(Vector4Double a) { return Vector4Double.Dot(a, a); }
        // *undoc* --- there's a property now
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double SqrMagnitude() { return Dot(this, this); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Format(string fmt, params object[] args)
        {
            return String.Format(CultureInfo.InvariantCulture.NumberFormat, fmt, args);
        }
    }
}