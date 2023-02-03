// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using UnityEngine;

namespace DepictionEngine
{
    public static class Matrix4x4DoubleExtension
    {
        public static Vector3Double GetDiagonalComponentsOnly(this Matrix4x4Double mat)
        {
            return new Vector3Double(mat.m00, mat.m11, mat.m22);
        }

        public static Matrix4x4Double ProjectMatrixOntoAxes(this Matrix4x4Double mat, QuaternionDouble axes)
        {
            Matrix4x4Double rotationMatrix = Matrix4x4Double.Rotate(axes);
            double x = Vector3Double.Dot(rotationMatrix.GetColumn(0), mat.GetColumn(0));
            double y = Vector3Double.Dot(rotationMatrix.GetColumn(1), mat.GetColumn(1));
            double z = Vector3Double.Dot(rotationMatrix.GetColumn(2), mat.GetColumn(2));
            return Matrix4x4Double.TRS(Vector3Double.zero, axes, new Vector3Double(x, y, z));
        }

        public static Matrix4x4Double Set3x3Rows(this Matrix4x4Double mat, Vector3Double affectRows, double value)
        {
            if (affectRows.x == 0)
                mat.m00 = mat.m01 = mat.m02 = value;
            if (affectRows.y == 0)
                mat.m10 = mat.m11 = mat.m12 = value;
            if (affectRows.z == 0)
                mat.m20 = mat.m21 = mat.m22 = value;
            return mat;
        }

        public static Matrix4x4Double Set3x3Columns(this Matrix4x4Double mat, Vector3Double affectColumns, double value)
        {
            if (affectColumns.x == 0)
                mat.m00 = mat.m10 = mat.m20 = value;
            if (affectColumns.y == 0)
                mat.m01 = mat.m11 = mat.m21 = value;
            if (affectColumns.z == 0)
                mat.m02 = mat.m12 = mat.m22 = value;
            return mat;
        }

        public static Matrix4x4Double Negate3x3Rows(this Matrix4x4Double mat, Vector3Double affectRows)
        {
            if (affectRows.x < 0)
            {
                mat.m00 = -mat.m00;
                mat.m01 = -mat.m01;
                mat.m02 = -mat.m02;
            }
            if (affectRows.y < 0)
            {
                mat.m10 = -mat.m10;
                mat.m11 = -mat.m11;
                mat.m12 = -mat.m12;
            }
            if (affectRows.z < 0)
            {
                mat.m20 = -mat.m20;
                mat.m21 = -mat.m21;
                mat.m22 = -mat.m22;
            }
            return mat;
        }

        public static Matrix4x4Double Negate3x3Columns(this Matrix4x4Double mat, Vector3Double affectColumns)
        {
            if (affectColumns.x < 0)
            {
                mat.m00 = -mat.m00;
                mat.m10 = -mat.m10;
                mat.m20 = -mat.m20;
            }
            if (affectColumns.y < 0)
            {
                mat.m01 = -mat.m01;
                mat.m11 = -mat.m11;
                mat.m21 = -mat.m21;
            }
            if (affectColumns.z < 0)
            {
                mat.m02 = -mat.m02;
                mat.m12 = -mat.m12;
                mat.m22 = -mat.m22;
            }
            return mat;
        }
    }
}
