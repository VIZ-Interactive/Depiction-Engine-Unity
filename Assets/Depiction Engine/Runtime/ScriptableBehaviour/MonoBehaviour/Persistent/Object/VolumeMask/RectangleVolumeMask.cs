﻿// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using UnityEngine;

namespace DepictionEngine
{
    /// <summary>
    /// Hide the geometry that lies inside its rectangular volume.
    /// </summary>
    [AddComponentMenu(SceneManager.NAMESPACE + "/Object/VolumeMask/" + nameof(RectangularVolumeMask))]
    public class RectangularVolumeMask : VolumeMaskBase
    {
        public override bool GetCustomEffectComputeBufferDataSize(out int size)
        {
            if (base.GetCustomEffectComputeBufferDataSize(out size))
            {
                size += 13;
                return true;
            }
            return false;
        }

        public override bool AddToComputeBufferData(out int size, int startIndex, float[] computeBufferData)
        {
            if (base.AddToComputeBufferData(out size, startIndex, computeBufferData))
            {
                computeBufferData[startIndex] = (float)RenderingManager.CustomEffectType.RectangleVolumeMask;

                Matrix4x4 mat = gameObject.transform.worldToLocalMatrix;

                computeBufferData[startIndex + 1] = mat.m00;
                computeBufferData[startIndex + 2] = mat.m01;
                computeBufferData[startIndex + 3] = mat.m02;
                computeBufferData[startIndex + 4] = mat.m03;

                computeBufferData[startIndex + 5] = mat.m10;
                computeBufferData[startIndex + 6] = mat.m11;
                computeBufferData[startIndex + 7] = mat.m12;
                computeBufferData[startIndex + 8] = mat.m13;

                computeBufferData[startIndex + 9] = mat.m20;
                computeBufferData[startIndex + 10] = mat.m21;
                computeBufferData[startIndex + 11] = mat.m22;
                computeBufferData[startIndex + 12] = mat.m23;

                return true;
            }
            return false;
        }

        public override bool IsInsideVolume(Vector3Double point)
        {
            Vector3Double localPoint = transform.InverseTransformPoint(point);
            return Math.Abs(localPoint.x) <= 1.0d || Math.Abs(localPoint.y) <= 1.0d || Math.Abs(localPoint.z) <= 1.0d;
        }
    }
}
