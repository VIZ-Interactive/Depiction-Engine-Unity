// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using UnityEngine;

namespace DepictionEngine
{
    public static class CameraExtension
    {
        public static Ray ScreenPointToRaySafe(this UnityEngine.Camera unityCamera, Vector2 pos)
        {
            return unityCamera.ScreenPointToRaySafe((Vector3)pos);
        }

        public static Ray ScreenPointToRaySafe(this UnityEngine.Camera unityCamera, Vector3 pos)
        {
            Ray ray = new Ray();

            float z = 500.0f;

            unityCamera.SetNearFarClipPlane(() =>
            {
                ray = unityCamera.ScreenPointToRay(new Vector3(pos.x, pos.y, z));
            }, z);

            return ray;
        }

        public static void CalculateFrustumCornersSafe(this UnityEngine.Camera unityCamera, Rect viewport, float z, UnityEngine.Camera.MonoOrStereoscopicEye eye, Vector3[] outCorners)
        {
            unityCamera.SetNearFarClipPlane(() =>
            {
                unityCamera.CalculateFrustumCorners(viewport, z, eye, outCorners);
            }, z);
        }

        public static void SetNearFarClipPlane(this UnityEngine.Camera unityCamera, Action callback, float distance)
        {
            float lastNearClipPlane = unityCamera.nearClipPlane;
            float lastFarClipPlane = unityCamera.farClipPlane;

            unityCamera.nearClipPlane = distance - 1.0f;
            unityCamera.farClipPlane = distance + 1.0f;

            callback();

            unityCamera.nearClipPlane = lastNearClipPlane;
            unityCamera.farClipPlane = lastFarClipPlane;
        }
    }
}
