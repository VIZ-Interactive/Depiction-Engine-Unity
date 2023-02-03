// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace DepictionEngine
{
    /// <summary>
    /// A 64 bit double version of the Quaternion.
    /// </summary>
    [Serializable]
    public struct QuaternionDouble
    {
        public double x;
        public double y;
        public double z;
        public double w;

        public QuaternionDouble(double x, double y, double z, double w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public QuaternionDouble(Vector3Double xyz, double w)
        {
            this.x = xyz.x;
            this.y = xyz.y;
            this.z = xyz.z;
            this.w = w;
        }

        public Vector3Double xyz
        {
            get { return new Vector3Double(x, y, z); }
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
                    case 3:
                        return w;
                    default:
                        throw new IndexOutOfRangeException("Invalid QuaternionDouble index!");
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
                    case 3:
                        w = value;
                        break;
                    default:
                        throw new IndexOutOfRangeException("Invalid QuaternionDouble index!");
                }
            }
        }

        public static QuaternionDouble identity
        {
            get { return new QuaternionDouble(0, 0, 0, 1); }
        }

        public Vector3Double eulerAngles
        {
            get
            {
                Matrix4x4Double m = QuaternionToMatrix(this);
                return (MatrixToEuler(m) * 180.0d / Math.PI);
            }
        }

        public QuaternionDouble normalized
        {
            get
            {
                double scale = 1.0d / Math.Sqrt(x * x + y * y + z * z + w * w);
                return new QuaternionDouble(xyz * scale, w * scale);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Angle(QuaternionDouble a, QuaternionDouble b)
        {
            double single = Dot(a, b);
            return Math.Acos(Math.Min(Math.Abs(single), 1.0d)) * 2.0d * (180.0d / Math.PI);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static QuaternionDouble AngleAxis(double angle, Vector3Double axis)
        {
            axis = axis.normalized;
            angle = angle / 180.0d * Math.PI;

            QuaternionDouble q = new QuaternionDouble();

            double halfAngle = angle * 0.5d;
            double s = Math.Sin(halfAngle);

            q.w = Math.Cos(halfAngle);
            q.x = s * axis.x;
            q.y = s * axis.y;
            q.z = s * axis.z;

            return q;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Dot(QuaternionDouble a, QuaternionDouble b)
        {
            return a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static QuaternionDouble Euler(Vector3Double euler)
        {
            return Euler(euler.x, euler.y, euler.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static QuaternionDouble Euler(double x, double y, double z)
        {
            double cX = Math.Cos(x * Math.PI / 360.0d);
            double sX = Math.Sin(x * Math.PI / 360.0d);

            double cY = Math.Cos(y * Math.PI / 360.0d);
            double sY = Math.Sin(y * Math.PI / 360.0d);

            double cZ = Math.Cos(z * Math.PI / 360.0d);
            double sZ = Math.Sin(z * Math.PI / 360.0d);

            QuaternionDouble qX = new QuaternionDouble(sX, 0.0d, 0.0d, cX);
            QuaternionDouble qY = new QuaternionDouble(0.0d, sY, 0.0d, cY);
            QuaternionDouble qZ = new QuaternionDouble(0.0d, 0.0d, sZ, cZ);

            QuaternionDouble q = (qY * qX) * qZ;

            return q;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static QuaternionDouble FromToRotation(Vector3Double fromDirection, Vector3Double toDirection)
        {
            throw new IndexOutOfRangeException("Not Available!");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static QuaternionDouble Inverse(QuaternionDouble rotation)
        {
            return new QuaternionDouble(-rotation.x, -rotation.y, -rotation.z, rotation.w);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static QuaternionDouble Lerp(QuaternionDouble a, QuaternionDouble b, double t)
        {
            return LerpUnclamped(a, b, MathPlus.Clamp01(t));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static QuaternionDouble LerpUnclamped(QuaternionDouble a, QuaternionDouble b, double t)
        {
            QuaternionDouble tmpQuat = new QuaternionDouble();
            if (Dot(a, b) < 0.0d)
            {
                tmpQuat.Set(a.x + t * (-b.x - a.x),
                            a.y + t * (-b.y - a.y),
                            a.z + t * (-b.z - a.z),
                            a.w + t * (-b.w - a.w));
            }
            else
            {
                tmpQuat.Set(a.x + t * (b.x - a.x),
                            a.y + t * (b.y - a.y),
                            a.z + t * (b.z - a.z),
                            a.w + t * (b.w - a.w));
            }
            double nor = Math.Sqrt(Dot(tmpQuat, tmpQuat));
            return new QuaternionDouble(tmpQuat.x / nor, tmpQuat.y / nor, tmpQuat.z / nor, tmpQuat.w / nor);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static QuaternionDouble LookRotation(Vector3Double forward)
        {
            return LookRotation(forward, Vector3Double.up);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static QuaternionDouble LookRotation(Vector3Double forward, [DefaultValue("Vector3Double.up")] Vector3Double upwards)
        {
            Matrix4x4Double m = LookRotationToMatrix(forward, upwards);
            return MatrixToQuaternion(m);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static QuaternionDouble RotateTowards(QuaternionDouble from, QuaternionDouble to, double maxDegreesDelta)
        {
            double num = Angle(from, to);
            QuaternionDouble result = new QuaternionDouble();
            if (num == 0.0d)
            {
                result = to;
            }
            else
            {
                double t = Math.Min(1.0d, maxDegreesDelta / num);
                result = SlerpUnclamped(from, to, t);
            }
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static QuaternionDouble Slerp(QuaternionDouble a, QuaternionDouble b, double t)
        {
            if (t > 1.0d)
            {
                t = 1.0d;
            }
            if (t < 0.0d)
            {
                t = 0.0d;
            }
            return SlerpUnclamped(a, b, t);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static QuaternionDouble SlerpUnclamped(QuaternionDouble q1, QuaternionDouble q2, double t)
        {
            double dot = Dot(q1, q2);

            QuaternionDouble tmpQuat = new QuaternionDouble();
            if (dot < 0.0d)
            {
                dot = -dot;
                tmpQuat.Set(-q2.x, -q2.y, -q2.z, -q2.w);
            }
            else
                tmpQuat = q2;


            if (dot < 1.0d)
            {
                double angle = Math.Acos(dot);
                double sinadiv, sinat, sinaomt;
                sinadiv = 1 / Math.Sin(angle);
                sinat = Math.Sin(angle * t);
                sinaomt = Math.Sin(angle * (1.0d - t));
                tmpQuat.Set((q1.x * sinaomt + tmpQuat.x * sinat) * sinadiv,
                         (q1.y * sinaomt + tmpQuat.y * sinat) * sinadiv,
                         (q1.z * sinaomt + tmpQuat.z * sinat) * sinadiv,
                         (q1.w * sinaomt + tmpQuat.w * sinat) * sinadiv);
                return tmpQuat;

            }
            else
            {
                return Lerp(q1, tmpQuat, t);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(double new_x, double new_y, double new_z, double new_w)
        {
            x = new_x;
            y = new_y;
            z = new_z;
            w = new_w;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetFromToRotation(Vector3Double fromDirection, Vector3Double toDirection)
        {
            this = FromToRotation(fromDirection, toDirection);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetLookRotation(Vector3Double view)
        {
            this = LookRotation(view);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetLookRotation(Vector3Double view, [DefaultValue("Vector3Double.up")] Vector3Double up)
        {
            this = LookRotation(view, up);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ToAngleAxis(out double angle, out Vector3Double axis)
        {
            angle = 2.0d * Math.Acos(w);
            if (angle == 0.0d)
            {
                axis = Vector3Double.right;
                return;
            }

            double div = 1.0d / Math.Sqrt(1.0d - w * w);
            axis = new Vector3Double(x * div, y * div, z * div);
            angle = angle * 180.0d / Math.PI;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
        {
            return String.Format("({0}, {1}, {2}, {3})", x, y, z, w);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return x.GetHashCode() ^ y.GetHashCode() << 2 ^ z.GetHashCode() >> 2 ^ w.GetHashCode() >> 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object other)
        {
            return this == (QuaternionDouble)other;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ToString(string format)
        {
            return String.Format("({0}, {1}, {2}, {3})", x.ToString(format), y.ToString(format), z.ToString(format), w.ToString(format));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Vector3Double MatrixToEuler(Matrix4x4Double m)
        {
            Vector3Double v = new Vector3Double();
            if (m[1, 2] < 1.0d)
            {
                if (m[1, 2] > -1.0d)
                {
                    v.x = Math.Asin(-m[1, 2]);
                    v.y = Math.Atan2(m[0, 2], m[2, 2]);
                    v.z = Math.Atan2(m[1, 0], m[1, 1]);
                }
                else
                {
                    v.x = Math.PI * 0.5d;
                    v.y = Math.Atan2(m[0, 1], m[0, 0]);
                    v.z = 0.0d;
                }
            }
            else
            {
                v.x = -Math.PI * 0.5d;
                v.y = Math.Atan2(-m[0, 1], m[0, 0]);
                v.z = 0.0d;
            }

            for (int i = 0; i < 3; i++)
            {
                if (v[i] < 0.0d)
                {
                    v[i] += 2.0d * Math.PI;
                }
                else if (v[i] > 2 * Math.PI)
                {
                    v[i] -= 2.0d * Math.PI;
                }
            }

            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4x4Double QuaternionToMatrix(QuaternionDouble quat)
        {
            Matrix4x4Double m = new Matrix4x4Double();

            double x = quat.x * 2.0d;
            double y = quat.y * 2.0d;
            double z = quat.z * 2.0d;
            double xx = quat.x * x;
            double yy = quat.y * y;
            double zz = quat.z * z;
            double xy = quat.x * y;
            double xz = quat.x * z;
            double yz = quat.y * z;
            double wx = quat.w * x;
            double wy = quat.w * y;
            double wz = quat.w * z;

            m[0] = 1.0d - (yy + zz);
            m[1] = xy + wz;
            m[2] = xz - wy;
            m[3] = 0.0d;

            m[4] = xy - wz;
            m[5] = 1.0d - (xx + zz);
            m[6] = yz + wx;
            m[7] = 0.0d;

            m[8] = xz + wy;
            m[9] = yz - wx;
            m[10] = 1.0d - (xx + yy);
            m[11] = 0.0d;

            m[12] = 0.0d;
            m[13] = 0.0d;
            m[14] = 0.0d;
            m[15] = 1.0d;

            return m;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static QuaternionDouble MatrixToQuaternion(Matrix4x4Double m)
        {
            QuaternionDouble quat = new QuaternionDouble();

            double fTrace = m[0, 0] + m[1, 1] + m[2, 2];
            double root;

            if (fTrace > 0)
            {
                root = Math.Sqrt(fTrace + 1);
                quat.w = 0.5d * root;
                root = 0.5d / root;
                quat.x = (m[2, 1] - m[1, 2]) * root;
                quat.y = (m[0, 2] - m[2, 0]) * root;
                quat.z = (m[1, 0] - m[0, 1]) * root;
            }
            else
            {
                int[] s_iNext = new int[] { 1, 2, 0 };
                int i = 0;
                if (m[1, 1] > m[0, 0])
                {
                    i = 1;
                }
                if (m[2, 2] > m[i, i])
                {
                    i = 2;
                }
                int j = s_iNext[i];
                int k = s_iNext[j];

                root = Math.Sqrt(m[i, i] - m[j, j] - m[k, k] + 1);
                if (root < 0)
                {
                    throw new IndexOutOfRangeException("error!");
                }
                quat[i] = 0.5 * root;
                root = 0.5f / root;
                quat.w = (m[k, j] - m[j, k]) * root;
                quat[j] = (m[j, i] + m[i, j]) * root;
                quat[k] = (m[k, i] + m[i, k]) * root;
            }
            double nor = Math.Sqrt(Dot(quat, quat));
            quat = new QuaternionDouble(quat.x / nor, quat.y / nor, quat.z / nor, quat.w / nor);

            return quat;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Matrix4x4Double LookRotationToMatrix(Vector3Double viewVec, Vector3Double upVec)
        {
            Vector3Double z = viewVec;
            Matrix4x4Double m = new Matrix4x4Double();

            double mag = Vector3Double.Magnitude(z);
            if (mag < 0.0d)
            {
                m = Matrix4x4Double.identity;
            }
            z /= mag;

            Vector3Double x = Vector3Double.Cross(upVec, z);
            mag = Vector3Double.Magnitude(x);
            if (mag < 0.0d)
            {
                m = Matrix4x4Double.identity;
            }
            x /= mag;

            Vector3Double y = Vector3Double.Cross(z, x);

            m[0, 0] = x.x; m[0, 1] = y.x; m[0, 2] = z.x;
            m[1, 0] = x.y; m[1, 1] = y.y; m[1, 2] = z.y;
            m[2, 0] = x.z; m[2, 1] = y.z; m[2, 2] = z.z;

            return m;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static QuaternionDouble operator *(QuaternionDouble lhs, QuaternionDouble rhs)
        {
            return new QuaternionDouble(lhs.w * rhs.x + lhs.x * rhs.w + lhs.y * rhs.z - lhs.z * rhs.y,
                                   lhs.w * rhs.y + lhs.y * rhs.w + lhs.z * rhs.x - lhs.x * rhs.z,
                                   lhs.w * rhs.z + lhs.z * rhs.w + lhs.x * rhs.y - lhs.y * rhs.x,
                                   lhs.w * rhs.w - lhs.x * rhs.x - lhs.y * rhs.y - lhs.z * rhs.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static QuaternionDouble operator *(QuaternionDouble lhs, Quaternion rhs)
        {
            return lhs * (QuaternionDouble)rhs;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3Double operator *(QuaternionDouble rotation, Vector3Double point)
        {
            double num = rotation.x * 2;
            double num2 = rotation.y * 2;
            double num3 = rotation.z * 2;
            double num4 = rotation.x * num;
            double num5 = rotation.y * num2;
            double num6 = rotation.z * num3;
            double num7 = rotation.x * num2;
            double num8 = rotation.x * num3;
            double num9 = rotation.y * num3;
            double num10 = rotation.w * num;
            double num11 = rotation.w * num2;
            double num12 = rotation.w * num3;
            Vector3Double result;
            result.x = (1.0d - (num5 + num6)) * point.x + (num7 - num12) * point.y + (num8 + num11) * point.z;
            result.y = (num7 + num12) * point.x + (1.0d - (num4 + num6)) * point.y + (num9 - num10) * point.z;
            result.z = (num8 - num11) * point.x + (num9 + num10) * point.y + (1.0d - (num4 + num5)) * point.z;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(QuaternionDouble lhs, QuaternionDouble rhs)
        {
            return lhs.x == rhs.x && lhs.y == rhs.y && lhs.z == rhs.z && lhs.w == rhs.w;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(QuaternionDouble lhs, QuaternionDouble rhs)
        {
            return !(lhs == rhs);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator QuaternionDouble(Quaternion quat)
        {
            return new QuaternionDouble(quat.x, quat.y, quat.z, quat.w);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Quaternion(QuaternionDouble quat)
        {
            return new Quaternion((float)quat.x, (float)quat.y, (float)quat.z, (float)quat.w);
        }
    }
}
