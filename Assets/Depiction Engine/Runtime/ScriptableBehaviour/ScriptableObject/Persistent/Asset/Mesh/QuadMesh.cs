// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System.Collections.Generic;
using UnityEngine;

namespace DepictionEngine
{
    public class QuadMesh : Mesh
    {
        private static Vector3[] _vertices = new Vector3[4]
                        {
                            new Vector3(-0.5f, 0.0f, -0.5f),
                            new Vector3(-0.5f, 0.0f, 0.5f),
                            new Vector3(0.5f, 0.0f, 0.5f),
                            new Vector3(0.5f, 0.0f, -0.5f)
                        };

        private static int[] _triangles = new int[6]
                        {
                            0, 1, 2,
                            0, 2, 3
                        };

        private static Vector3[] _normals = new Vector3[4]
                        {
                            Vector3.up,
                            Vector3.up,
                            Vector3.up,
                            Vector3.up
                        };

        private static List<Vector2> _uvs = new List<Vector2>
                        {
                            new Vector2(0.0f, 0.0f),
                            new Vector2(0.0f, 1.0f),
                            new Vector2(1.0f, 1.0f),
                            new Vector2(1.0f, 0.0f)
                        };

        protected override bool Initialize(InstanceManager.InitializationContext initializationState)
        {
            if (base.Initialize(initializationState))
            {
                if (unityMesh == null)
                {
                    SetData(_triangles, _vertices, _normals, _uvs, calculateBounds: false);
                    bounds = new Bounds(Vector3.zero, new Vector3(1.0f, 0.0f, 1.0f));
                }

                return true;
            }
            return false;
        }
    }
}
