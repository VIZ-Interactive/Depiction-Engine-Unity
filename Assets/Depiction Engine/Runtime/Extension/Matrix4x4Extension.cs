// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using UnityEngine;

namespace DepictionEngine
{
    public static class Matrix4x4Extension
    {
        public static Vector3 GetDiagonalComponentsOnly(this Matrix4x4 mat)
        {
            return new Vector3(mat.m00, mat.m11, mat.m22);
        }

        public static Matrix4x4 ProjectMatrixOntoAxes(this Matrix4x4 mat, Quaternion axes)
        {
            Matrix4x4 rotationMatrix = Matrix4x4.Rotate(axes);
            float x = Vector3.Dot(rotationMatrix.GetColumn(0), mat.GetColumn(0));
            float y = Vector3.Dot(rotationMatrix.GetColumn(1), mat.GetColumn(1));
            float z = Vector3.Dot(rotationMatrix.GetColumn(2), mat.GetColumn(2));
            return Matrix4x4.TRS(Vector3.zero, axes, new Vector3(x, y, z));
        }

        public static Matrix4x4 Set3x3Rows(this Matrix4x4 mat, Vector3 affectRows, float value)
        {
            if (affectRows.x == 0)
                mat.m00 = mat.m01 = mat.m02 = value;
            if (affectRows.y == 0)
                mat.m10 = mat.m11 = mat.m12 = value;
            if (affectRows.z == 0)
                mat.m20 = mat.m21 = mat.m22 = value;
            return mat;
        }

        public static Matrix4x4 Set3x3Columns(this Matrix4x4 mat, Vector3 affectColumns, float value)
        {
            if (affectColumns.x == 0)
                mat.m00 = mat.m10 = mat.m20 = value;
            if (affectColumns.y == 0)
                mat.m01 = mat.m11 = mat.m21 = value;
            if (affectColumns.z == 0)
                mat.m02 = mat.m12 = mat.m22 = value;
            return mat;
        }

        public static Vector3Double MultiplyDoublePoint3x4(this Matrix4x4 mat, Vector3Double point)
        {
            Vector3Double res;
            res.x = mat.m00 * point.x + mat.m01 * point.y + mat.m02 * point.z + mat.m03;
            res.y = mat.m10 * point.x + mat.m11 * point.y + mat.m12 * point.z + mat.m13;
            res.z = mat.m20 * point.x + mat.m21 * point.y + mat.m22 * point.z + mat.m23;
            return res;
        }

        public static Matrix4x4 Negate3x3Rows(this Matrix4x4 mat, Vector3 affectRows)
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

        public static Matrix4x4 Negate3x3Columns(this Matrix4x4 mat, Vector3 affectColumns)
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
