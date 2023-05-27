// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace DepictionEngine
{
    /// <summary>
    /// A 64 bit double version of the Matrix4x4.
    /// </summary>
    [Serializable, StructLayout(LayoutKind.Sequential)]
    public partial struct Matrix4x4Double : IEquatable<Matrix4x4Double>, IFormattable
    {
        // memory layout:
        //
        //                row no (=vertical)
        //               |  0   1   2   3
        //            ---+----------------
        //            0  | m00 m10 m20 m30
        // column no  1  | m01 m11 m21 m31
        // (=horiz)   2  | m02 m12 m22 m32
        //            3  | m03 m13 m23 m33

        public double m00;
        public double m10;
        public double m20;
        public double m30;

        public double m01;
        public double m11;
        public double m21;
        public double m31;

        public double m02;
        public double m12;
        public double m22;
        public double m32;

        public double m03;
        public double m13;
        public double m23;
        public double m33;

        public Matrix4x4Double(Vector4Double column0, Vector4Double column1, Vector4Double column2, Vector4Double column3)
        {
            this.m00 = column0.x; this.m01 = column1.x; this.m02 = column2.x; this.m03 = column3.x;
            this.m10 = column0.y; this.m11 = column1.y; this.m12 = column2.y; this.m13 = column3.y;
            this.m20 = column0.z; this.m21 = column1.z; this.m22 = column2.z; this.m23 = column3.z;
            this.m30 = column0.w; this.m31 = column1.w; this.m32 = column2.w; this.m33 = column3.w;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4x4Double TRS(Vector3Double translation, QuaternionDouble rotation, Vector3Double scale)
        {
            Matrix3x3Double r = new Matrix3x3Double(rotation);
            return new Matrix4x4Double(new Vector4Double(r.c0 * scale.x, 0.0d),
                              new Vector4Double(r.c1 * scale.y, 0.0d),
                              new Vector4Double(r.c2 * scale.z, 0.0d),
                              new Vector4Double(translation, 1.0d));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4x4Double TRS(ref Matrix4x4Double matrix, Vector3Double translation, QuaternionDouble rotation, Vector3Double scale)
        {
            double x = rotation.x * 2.0d;
            double y = rotation.y * 2.0d;
            double z = rotation.z * 2.0d;
            double xx = rotation.x * x;
            double yy = rotation.y * y;
            double zz = rotation.z * z;
            double xy = rotation.x * y;
            double xz = rotation.x * z;
            double yz = rotation.y * z;
            double wx = rotation.w * x;
            double wy = rotation.w * y;
            double wz = rotation.w * z;

            matrix[0, 0] = 1.0d - (yy + zz) * scale.x;
            matrix[1, 0] = xy + wz * scale.x;
            matrix[2, 0] = xz - wy * scale.x;
            matrix[3, 0] = 0.0d;

            matrix[0, 1] = xy - wz * scale.y;
            matrix[1, 1] = 1.0d - (xx + zz) * scale.y;
            matrix[2, 1] = yz + wx * scale.y;
            matrix[3, 1] = 0.0d;

            matrix[0, 2] = xz + wy * scale.z;
            matrix[1, 2] = yz - wx * scale.z;
            matrix[2, 2] = 1.0d - (xx + yy) * scale.z;
            matrix[3, 2] = 0.0d;
       
            matrix[0, 3] = translation.x;
            matrix[1, 3] = translation.y;
            matrix[2, 3] = translation.z;
            matrix[3, 3] = 1.0d;

            return matrix;
        }

        // Access element at [row, column].
        public double this[int row, int column]
        {
            get
            {
                return this[row + column * 4];
            }

            set
            {
                this[row + column * 4] = value;
            }
        }

        // Access element at sequential index (0..15 inclusive).
        public double this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return m00;
                    case 1: return m10;
                    case 2: return m20;
                    case 3: return m30;
                    case 4: return m01;
                    case 5: return m11;
                    case 6: return m21;
                    case 7: return m31;
                    case 8: return m02;
                    case 9: return m12;
                    case 10: return m22;
                    case 11: return m32;
                    case 12: return m03;
                    case 13: return m13;
                    case 14: return m23;
                    case 15: return m33;
                    default:
                        throw new IndexOutOfRangeException("Invalid matrix index!");
                }
            }

            set
            {
                switch (index)
                {
                    case 0: m00 = value; break;
                    case 1: m10 = value; break;
                    case 2: m20 = value; break;
                    case 3: m30 = value; break;
                    case 4: m01 = value; break;
                    case 5: m11 = value; break;
                    case 6: m21 = value; break;
                    case 7: m31 = value; break;
                    case 8: m02 = value; break;
                    case 9: m12 = value; break;
                    case 10: m22 = value; break;
                    case 11: m32 = value; break;
                    case 12: m03 = value; break;
                    case 13: m13 = value; break;
                    case 14: m23 = value; break;
                    case 15: m33 = value; break;

                    default:
                        throw new IndexOutOfRangeException("Invalid matrix index!");
                }
            }
        }

        // used to allow Matrix4x4s to be used as keys in hash tables
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return GetColumn(0).GetHashCode() ^ (GetColumn(1).GetHashCode() << 2) ^ (GetColumn(2).GetHashCode() >> 2) ^ (GetColumn(3).GetHashCode() >> 1);
        }

        // also required for being able to use Matrix4x4s as keys in hash tables
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object other)
        {
            if (!(other is Matrix4x4Double)) return false;

            return Equals((Matrix4x4Double)other);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Matrix4x4Double other)
        {
            return GetColumn(0).Equals(other.GetColumn(0))
                && GetColumn(1).Equals(other.GetColumn(1))
                && GetColumn(2).Equals(other.GetColumn(2))
                && GetColumn(3).Equals(other.GetColumn(3));
        }

        // Multiplies two matrices.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4x4Double operator *(Matrix4x4Double lhs, Matrix4x4Double rhs)
        {
            Matrix4x4Double res;
            res.m00 = lhs.m00 * rhs.m00 + lhs.m01 * rhs.m10 + lhs.m02 * rhs.m20 + lhs.m03 * rhs.m30;
            res.m01 = lhs.m00 * rhs.m01 + lhs.m01 * rhs.m11 + lhs.m02 * rhs.m21 + lhs.m03 * rhs.m31;
            res.m02 = lhs.m00 * rhs.m02 + lhs.m01 * rhs.m12 + lhs.m02 * rhs.m22 + lhs.m03 * rhs.m32;
            res.m03 = lhs.m00 * rhs.m03 + lhs.m01 * rhs.m13 + lhs.m02 * rhs.m23 + lhs.m03 * rhs.m33;

            res.m10 = lhs.m10 * rhs.m00 + lhs.m11 * rhs.m10 + lhs.m12 * rhs.m20 + lhs.m13 * rhs.m30;
            res.m11 = lhs.m10 * rhs.m01 + lhs.m11 * rhs.m11 + lhs.m12 * rhs.m21 + lhs.m13 * rhs.m31;
            res.m12 = lhs.m10 * rhs.m02 + lhs.m11 * rhs.m12 + lhs.m12 * rhs.m22 + lhs.m13 * rhs.m32;
            res.m13 = lhs.m10 * rhs.m03 + lhs.m11 * rhs.m13 + lhs.m12 * rhs.m23 + lhs.m13 * rhs.m33;

            res.m20 = lhs.m20 * rhs.m00 + lhs.m21 * rhs.m10 + lhs.m22 * rhs.m20 + lhs.m23 * rhs.m30;
            res.m21 = lhs.m20 * rhs.m01 + lhs.m21 * rhs.m11 + lhs.m22 * rhs.m21 + lhs.m23 * rhs.m31;
            res.m22 = lhs.m20 * rhs.m02 + lhs.m21 * rhs.m12 + lhs.m22 * rhs.m22 + lhs.m23 * rhs.m32;
            res.m23 = lhs.m20 * rhs.m03 + lhs.m21 * rhs.m13 + lhs.m22 * rhs.m23 + lhs.m23 * rhs.m33;

            res.m30 = lhs.m30 * rhs.m00 + lhs.m31 * rhs.m10 + lhs.m32 * rhs.m20 + lhs.m33 * rhs.m30;
            res.m31 = lhs.m30 * rhs.m01 + lhs.m31 * rhs.m11 + lhs.m32 * rhs.m21 + lhs.m33 * rhs.m31;
            res.m32 = lhs.m30 * rhs.m02 + lhs.m31 * rhs.m12 + lhs.m32 * rhs.m22 + lhs.m33 * rhs.m32;
            res.m33 = lhs.m30 * rhs.m03 + lhs.m31 * rhs.m13 + lhs.m32 * rhs.m23 + lhs.m33 * rhs.m33;

            return res;
        }

        // Transforms a [[Vector4]] by a matrix.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4Double operator *(Matrix4x4Double lhs, Vector4Double vector)
        {
            Vector4Double res;
            res.x = lhs.m00 * vector.x + lhs.m01 * vector.y + lhs.m02 * vector.z + lhs.m03 * vector.w;
            res.y = lhs.m10 * vector.x + lhs.m11 * vector.y + lhs.m12 * vector.z + lhs.m13 * vector.w;
            res.z = lhs.m20 * vector.x + lhs.m21 * vector.y + lhs.m22 * vector.z + lhs.m23 * vector.w;
            res.w = lhs.m30 * vector.x + lhs.m31 * vector.y + lhs.m32 * vector.z + lhs.m33 * vector.w;
            return res;
        }

        //*undoc*
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Matrix4x4Double lhs, Matrix4x4Double rhs)
        {
            // Returns false in the presence of NaN values.
            return lhs.GetColumn(0) == rhs.GetColumn(0)
                && lhs.GetColumn(1) == rhs.GetColumn(1)
                && lhs.GetColumn(2) == rhs.GetColumn(2)
                && lhs.GetColumn(3) == rhs.GetColumn(3);
        }

        //*undoc*
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Matrix4x4Double lhs, Matrix4x4Double rhs)
        {
            // Returns true in the presence of NaN values.
            return !(lhs == rhs);
        }

        // Get a column of the matrix.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector4Double GetColumn(int index)
        {
            switch (index)
            {
                case 0: return new Vector4Double(m00, m10, m20, m30);
                case 1: return new Vector4Double(m01, m11, m21, m31);
                case 2: return new Vector4Double(m02, m12, m22, m32);
                case 3: return new Vector4Double(m03, m13, m23, m33);
                default:
                    throw new IndexOutOfRangeException("Invalid column index!");
            }
        }

        // Returns a row of the matrix.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector4Double GetRow(int index)
        {
            switch (index)
            {
                case 0: return new Vector4Double(m00, m01, m02, m03);
                case 1: return new Vector4Double(m10, m11, m12, m13);
                case 2: return new Vector4Double(m20, m21, m22, m23);
                case 3: return new Vector4Double(m30, m31, m32, m33);
                default:
                    throw new IndexOutOfRangeException("Invalid row index!");
            }
        }

        // Sets a column of the matrix.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetColumn(int index, Vector4Double column)
        {
            this[0, index] = column.x;
            this[1, index] = column.y;
            this[2, index] = column.z;
            this[3, index] = column.w;
        }

        // Sets a row of the matrix.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetRow(int index, Vector4Double row)
        {
            this[index, 0] = row.x;
            this[index, 1] = row.y;
            this[index, 2] = row.z;
            this[index, 3] = row.w;
        }

        // Transforms a position by this matrix, with a perspective divide. (generic)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3Double MultiplyPoint(Vector3Double point)
        {
            Vector3Double res;
            double w;
            res.x = this.m00 * point.x + this.m01 * point.y + this.m02 * point.z + this.m03;
            res.y = this.m10 * point.x + this.m11 * point.y + this.m12 * point.z + this.m13;
            res.z = this.m20 * point.x + this.m21 * point.y + this.m22 * point.z + this.m23;
            w = this.m30 * point.x + this.m31 * point.y + this.m32 * point.z + this.m33;

            w = 1.0d / w;
            res.x *= w;
            res.y *= w;
            res.z *= w;
            return res;
        }

        // Transforms a position by this matrix, without a perspective divide. (fast)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3Double MultiplyPoint3x4(Vector3Double point)
        {
            Vector3Double res;
            res.x = this.m00 * point.x + this.m01 * point.y + this.m02 * point.z + this.m03;
            res.y = this.m10 * point.x + this.m11 * point.y + this.m12 * point.z + this.m13;
            res.z = this.m20 * point.x + this.m21 * point.y + this.m22 * point.z + this.m23;
            return res;
        }

        // Transforms a direction by this matrix.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3Double MultiplyVector(Vector3Double vector)
        {
            Vector3Double res;
            res.x = this.m00 * vector.x + this.m01 * vector.y + this.m02 * vector.z;
            res.y = this.m10 * vector.x + this.m11 * vector.y + this.m12 * vector.z;
            res.z = this.m20 * vector.x + this.m21 * vector.y + this.m22 * vector.z;
            return res;
        }

        // Creates a scaling matrix.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4x4Double Scale(Vector3Double vector)
        {
            Matrix4x4Double m;
            m.m00 = vector.x; m.m01 = 0.0d; m.m02 = 0.0d; m.m03 = 0.0d;
            m.m10 = 0.0d; m.m11 = vector.y; m.m12 = 0.0d; m.m13 = 0.0d;
            m.m20 = 0.0d; m.m21 = 0.0d; m.m22 = vector.z; m.m23 = 0.0d;
            m.m30 = 0.0d; m.m31 = 0.0d; m.m32 = 0.0d; m.m33 = 1.0d;
            return m;
        }

        // Creates a translation matrix.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4x4Double Translate(Vector3Double vector)
        {
            Matrix4x4Double m;
            m.m00 = 1.0d; m.m01 = 0.0d; m.m02 = 0.0d; m.m03 = vector.x;
            m.m10 = 0.0d; m.m11 = 1.0d; m.m12 = 0.0d; m.m13 = vector.y;
            m.m20 = 0.0d; m.m21 = 0.0d; m.m22 = 1.0d; m.m23 = vector.z;
            m.m30 = 0.0d; m.m31 = 0.0d; m.m32 = 0.0d; m.m33 = 1.0d;
            return m;
        }

        // Creates a rotation matrix. Note: Assumes unit quaternion
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4x4Double Rotate(QuaternionDouble q)
        {
            // Precalculate coordinate products
            double x = q.x * 2.0d;
            double y = q.y * 2.0d;
            double z = q.z * 2.0d;
            double xx = q.x * x;
            double yy = q.y * y;
            double zz = q.z * z;
            double xy = q.x * y;
            double xz = q.x * z;
            double yz = q.y * z;
            double wx = q.w * x;
            double wy = q.w * y;
            double wz = q.w * z;

            // Calculate 3x3 matrix from orthonormal basis
            Matrix4x4Double m;
            m.m00 = 1.0d - (yy + zz); m.m10 = xy + wz; m.m20 = xz - wy; m.m30 = 0.0d;
            m.m01 = xy - wz; m.m11 = 1.0d - (xx + zz); m.m21 = yz + wx; m.m31 = 0.0d;
            m.m02 = xz + wy; m.m12 = yz - wx; m.m22 = 1.0d - (xx + yy); m.m32 = 0.0d;
            m.m03 = 0.0d; m.m13 = 0.0d; m.m23 = 0.0d; m.m33 = 1.0d;
            return m;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Matrix4x4Double(Matrix4x4 mat)
        {
            Matrix4x4Double newMat = new Matrix4x4Double();
            newMat.m00 = mat.m00;
            newMat.m10 = mat.m10;
            newMat.m20 = mat.m20;
            newMat.m30 = mat.m30;
            newMat.m01 = mat.m01;
            newMat.m11 = mat.m11;
            newMat.m21 = mat.m21;
            newMat.m31 = mat.m31;
            newMat.m02 = mat.m02;
            newMat.m12 = mat.m12;
            newMat.m22 = mat.m22;
            newMat.m32 = mat.m32;
            newMat.m03 = mat.m03;
            newMat.m13 = mat.m13;
            newMat.m23 = mat.m23;
            newMat.m33 = mat.m33;
            return newMat;
        }

        public Matrix4x4Double fastinverse
        {
            get
            {
                Vector4Double c0 = new Vector4Double(m00, m10, m20, m30);
                Vector4Double c1 = new Vector4Double(m01, m11, m21, m31);
                Vector4Double c2 = new Vector4Double(m02, m12, m22, m32);
                Vector4Double pos = new Vector4Double(m03, m13, m23, m33);

                Vector4Double zero = Vector4Double.zero;

                Vector4Double t0 = unpacklo(c0, c2);
                Vector4Double t1 = unpacklo(c1, zero);
                Vector4Double t2 = unpackhi(c0, c2);
                Vector4Double t3 = unpackhi(c1, zero);

                Vector4Double r0 = unpacklo(t0, t1);
                Vector4Double r1 = unpackhi(t0, t1);
                Vector4Double r2 = unpacklo(t2, t3);

                pos = -(r0 * pos.x + r1 * pos.y + r2 * pos.z);
                pos.w = 1.0d;

                return new Matrix4x4Double(r0, r1, r2, pos);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector4Double unpacklo(Vector4Double a, Vector4Double b)
        {
            return shuffle(a, b, ShuffleComponent.LeftX, ShuffleComponent.RightX, ShuffleComponent.LeftY, ShuffleComponent.RightY);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector4Double unpackhi(Vector4Double a, Vector4Double b)
        {
            return shuffle(a, b, ShuffleComponent.LeftZ, ShuffleComponent.RightZ, ShuffleComponent.LeftW, ShuffleComponent.RightW);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector4Double shuffle(Vector4Double a, Vector4Double b, ShuffleComponent x, ShuffleComponent y, ShuffleComponent z, ShuffleComponent w)
        {
            return new Vector4Double(
                select_shuffle_component(a, b, x),
                select_shuffle_component(a, b, y),
                select_shuffle_component(a, b, z),
                select_shuffle_component(a, b, w));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double select_shuffle_component(Vector4Double a, Vector4Double b, ShuffleComponent component)
        {
            switch (component)
            {
                case ShuffleComponent.LeftX:
                    return a.x;
                case ShuffleComponent.LeftY:
                    return a.y;
                case ShuffleComponent.LeftZ:
                    return a.z;
                case ShuffleComponent.LeftW:
                    return a.w;
                case ShuffleComponent.RightX:
                    return b.x;
                case ShuffleComponent.RightY:
                    return b.y;
                case ShuffleComponent.RightZ:
                    return b.z;
                case ShuffleComponent.RightW:
                    return b.w;
                default:
                    throw new System.ArgumentException("Invalid shuffle component: " + component);
            }
        }

        // Matrix4x4.zero is of questionable usefulness considering C# sets everything to 0 by default, however:
        //  1. it's consistent with other Math structs in Unity such as Vector2, Vector3 and Vector4,
        //  2. "Matrix4x4.zero" is arguably more readable than "new Matrix4x4()",
        //  3. it's already in the API ..
        static readonly Matrix4x4Double zeroMatrix = new Matrix4x4Double(new Vector4Double(0, 0, 0, 0),
            new Vector4Double(0, 0, 0, 0),
            new Vector4Double(0, 0, 0, 0),
            new Vector4Double(0, 0, 0, 0));

        // Returns a matrix with all elements set to zero (RO).
        public static Matrix4x4Double zero { get { return zeroMatrix; } }

        static readonly Matrix4x4Double identityMatrix = new Matrix4x4Double(new Vector4Double(1, 0, 0, 0),
            new Vector4Double(0, 1, 0, 0),
            new Vector4Double(0, 0, 1, 0),
            new Vector4Double(0, 0, 0, 1));

        // Returns the identity matrix (RO).
        public static Matrix4x4Double identity { get { return identityMatrix; } }

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
                format = "F5";
            return Format("{0}\t{1}\t{2}\t{3}\n{4}\t{5}\t{6}\t{7}\n{8}\t{9}\t{10}\t{11}\n{12}\t{13}\t{14}\t{15}\n",
                m00.ToString(format, formatProvider), m01.ToString(format, formatProvider), m02.ToString(format, formatProvider), m03.ToString(format, formatProvider),
                m10.ToString(format, formatProvider), m11.ToString(format, formatProvider), m12.ToString(format, formatProvider), m13.ToString(format, formatProvider),
                m20.ToString(format, formatProvider), m21.ToString(format, formatProvider), m22.ToString(format, formatProvider), m23.ToString(format, formatProvider),
                m30.ToString(format, formatProvider), m31.ToString(format, formatProvider), m32.ToString(format, formatProvider), m33.ToString(format, formatProvider));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Format(string fmt, params object[] args)
        {
            return String.Format(CultureInfo.InvariantCulture.NumberFormat, fmt, args);
        }

        private struct Matrix3x3Double
        {
            public Vector3Double c0;
            public Vector3Double c1;
            public Vector3Double c2;

            public Matrix3x3Double(QuaternionDouble q)
            {
                double x = q.x * 2.0d;
                double y = q.y * 2.0d;
                double z = q.z * 2.0d;
                double xx = q.x * x;
                double yy = q.y * y;
                double zz = q.z * z;
                double xy = q.x * y;
                double xz = q.x * z;
                double yz = q.y * z;
                double wx = q.w * x;
                double wy = q.w * y;
                double wz = q.w * z;

                c0 = new Vector3Double(1.0d - (yy + zz), xy + wz, xz - wy);
                c1 = new Vector3Double(xy - wz, 1.0d - (xx + zz), yz + wx);
                c2 = new Vector3Double(xz + wy, yz - wx, 1.0d - (xx + yy));
            }
        }

        /// <summary>Specifies a shuffle component.</summary>
        private enum ShuffleComponent : byte
        {
            /// <summary>Specified the x component of the left vector.</summary>
            LeftX,
            /// <summary>Specified the y component of the left vector.</summary>
            LeftY,
            /// <summary>Specified the z component of the left vector.</summary>
            LeftZ,
            /// <summary>Specified the w component of the left vector.</summary>
            LeftW,

            /// <summary>Specified the x component of the right vector.</summary>
            RightX,
            /// <summary>Specified the y component of the right vector.</summary>
            RightY,
            /// <summary>Specified the z component of the right vector.</summary>
            RightZ,
            /// <summary>Specified the w component of the right vector.</summary>
            RightW
        };
    }
} //namespace