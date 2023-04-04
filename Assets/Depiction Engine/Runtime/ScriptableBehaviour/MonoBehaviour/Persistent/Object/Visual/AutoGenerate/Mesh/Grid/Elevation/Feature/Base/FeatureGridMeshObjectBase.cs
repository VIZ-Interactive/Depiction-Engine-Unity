// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

/*
OSM Buildings https://github.com/OSMBuildings/OSMBuildings Copyright (c) 2018, OSM Buildings

Qolor https://github.com/kekscom/Color.js Copyright (c) 2018, Jan Marsch

Triangulate.js https://github.com/OSMBuildings/Triangulation Copyright (c) 2018, Jan Marsch, OSM Buildings

All rights reserved.

Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.

Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

===============================================================================

Suncalc https://github.com/mourner/suncalc/ Copyright (c) 2014, Vladimir Agafonkin

Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.

Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

===============================================================================

Clockwise winding check https://github.com/Turfjs/turf-rewind Abel Vázquez

Uses Shoelace Formula (http://en.wikipedia.org/wiki/Shoelace_formula)

The MIT License (MIT)

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

===============================================================================

Code fragments from osmstreetview https://github.com/rbuch703/osmstreetview/ Robert Buchholz

Copyright 2014, Robert Buchholz rbuch703@gmail.com.

Licensed under the GNU General Public License version 3.

===============================================================================

Inspiration of roof processing from OSM2World https://github.com/tordanik/OSM2World Tobias Knerr
*/

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace DepictionEngine
{
    [CreateComponent(typeof(AssetReference))]
    public class FeatureGridMeshObjectBase : ElevationGridMeshObjectBase
    {
        private const string FEATURE_REFERENCE_DATATYPE = nameof(Feature);

        [Serializable]
        private class MeshRendererMaterialDictionary : SerializableDictionary<int, Material> { };

        private static readonly Vector2 ZERO_ONE = new(0.0f, 1.0f);
        private static readonly Vector2 ONE_ZERO = new(1.0f, 0.0f);

        private Dictionary<int, Vector2> _meshRendererHighlightIndexRange;

        private Feature _feature;

        [SerializeField, HideInInspector]
        private MeshRendererMaterialDictionary _materialsDictionary;

        public override void Recycle()
        {
            base.Recycle();

            _meshRendererHighlightIndexRange?.Clear();

            _materialsDictionary?.Clear();
        }

        protected override void InitializeSerializedFields(InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            if (initializingContext == InitializationContext.Editor_Duplicate || initializingContext == InitializationContext.Programmatically_Duplicate)
            {
                if (_meshRendererHighlightIndexRange != null)
                    _meshRendererHighlightIndexRange.Clear();

                if (_materialsDictionary != null)
                    _materialsDictionary.Clear();
            }
        }

        protected override void CreateComponents(InitializationContext initializingContext)
        {
            base.CreateComponents(initializingContext);

            InitializeReferenceDataType(FEATURE_REFERENCE_DATATYPE, typeof(AssetReference));
        }

        protected override bool UpdateAllDelegates()
        {
            if (base.UpdateAllDelegates())
            {
                RemoveFeatureDelgates(feature);
                AddFeatureDelegates(feature);

                return true;
            }
            return false;
        }

        private void RemoveFeatureDelgates(Feature feature)
        {
            if (feature is not null)
                feature.PropertyAssignedEvent -= FeaturePropertyAssignedHandler;
        }

        private void AddFeatureDelegates(Feature feature)
        {
            if (!IsDisposing() && feature != Disposable.NULL)
                feature.PropertyAssignedEvent += FeaturePropertyAssignedHandler;
        }

        private void FeaturePropertyAssignedHandler(IProperty property, string name, object newValue, object oldValue)
        {
            if (name == nameof(AssetBase.data))
                FeatureChanged();
        }

        protected override bool UpdateReferences(bool forceUpdate = false)
        {
            if (base.UpdateReferences(forceUpdate))
            {
                UpdateFeature();

                return true;
            }
            return false;
        }

        public override void OnMouseMoveHit(RaycastHitDouble hit)
        {
            base.OnMouseMoveHit(hit);

            UpdateHighlightIndexRange(hit);
        }

        public override void OnMouseExitHit(RaycastHitDouble hit)
        {
            base.OnMouseExitHit(hit);

            UpdateHighlightIndexRange(hit);
        }

        private void UpdateHighlightIndexRange(RaycastHitDouble hit)
        {
            _meshRendererHighlightIndexRange ??= new Dictionary<int, Vector2>();

            _meshRendererHighlightIndexRange.Clear();

            if (hit != null && GetFeatureIndex(hit, out int featureIndex))
            {
                MeshRendererVisual hitMeshRendererVisual = hit.meshRendererVisual;

                FeatureMesh featureMesh = GetMeshFromUnityMesh(hitMeshRendererVisual.sharedMesh) as FeatureMesh;
                if (featureMesh != Disposable.NULL)
                {
                    int firstIndex = 0;

                    if (featureIndex != 0)
                        firstIndex = featureMesh.GetLastIndex(featureIndex - 1);
                 
                    _meshRendererHighlightIndexRange[hitMeshRendererVisual.meshRenderer.GetInstanceID()] = new Vector2(firstIndex, featureMesh.GetLastIndex(featureIndex));
                }
            }
        }

        protected bool GetFeatureIndex(RaycastHitDouble hit, out int featureIndex)
        {
            FeatureMesh featureMesh = hit != null ? GetMeshFromUnityMesh(hit.meshRendererVisual.sharedMesh) as FeatureMesh : null;
            if (featureMesh != Disposable.NULL)
            {
                featureIndex = featureMesh.GetFeatureIndex(hit.triangleIndex);
                return true;
            }

            featureIndex = -1;
            return false;
        }

        public override void OnMouseClickedHit(RaycastHitDouble hit)
        {
            base.OnMouseClickedHit(hit);

            if (GetFeatureIndex(hit, out int featureIndex))
                OnFeatureClickedHit(hit, featureIndex);
        }

        protected virtual void OnFeatureClickedHit(RaycastHitDouble hit, int featureIndex)
        {
           
        }

        protected AssetReference featureAssetReference
        {
            get { return GetFirstReferenceOfType(FEATURE_REFERENCE_DATATYPE) as AssetReference; }
        }

        private void UpdateFeature()
        {
            feature = featureAssetReference != Disposable.NULL ? featureAssetReference.data as Feature : null;
        }

        protected Feature feature
        {
            get { return _feature; }
            private set 
            {
                Feature oldValue = _feature;
                Feature newValue = value;

                if (Object.ReferenceEquals(oldValue, newValue))
                    return;

                RemoveFeatureDelgates(oldValue);
                AddFeatureDelegates(newValue);

                _feature = newValue;

                FeatureChanged();
            }
        }

        private void FeatureChanged()
        {
            if (initialized)
            {
                if (meshRendererVisualDirtyFlags is not null)
                    meshRendererVisualDirtyFlags.Recreate();
            }
        }

        protected override bool AssetLoaded()
        {
            return base.AssetLoaded() && feature != Disposable.NULL;
        }

        protected override void UpdateGridMeshRendererVisualModifier()
        {
           
        }

        protected override void ApplyPropertiesToMaterial(MeshRenderer meshRenderer, Material material, MaterialPropertyBlock materialPropertyBlock, double cameraAtmosphereAltitudeRatio, Camera camera, GeoAstroObject closestGeoAstroObject, Star star)
        {
            base.ApplyPropertiesToMaterial(meshRenderer, material, materialPropertyBlock, cameraAtmosphereAltitudeRatio, camera, closestGeoAstroObject, star);

            if (_meshRendererHighlightIndexRange == null || !_meshRendererHighlightIndexRange.TryGetValue(meshRenderer.GetInstanceID(), out Vector2 highlightIndexRange))
                highlightIndexRange = Vector2.negativeInfinity;

            SetVectorToMaterial("_HighlightIndexRange", highlightIndexRange, material, materialPropertyBlock);

            if (renderingManager.highlightColor.a != 0.0f)
                SetVectorToMaterial("_HighlightColor", renderingManager.highlightColor, material, materialPropertyBlock);
        }

        protected override Type GetMeshRendererVisualDirtyFlagType()
        {
            return typeof(FeatureGridMeshObjectVisualDirtyFlags);
        }

        protected override void UpdateMeshRendererDirtyFlags(VisualObjectVisualDirtyFlags meshRendererVisualDirtyFlags)
        {
            base.UpdateMeshRendererDirtyFlags(meshRendererVisualDirtyFlags);

            if (meshRendererVisualDirtyFlags is FeatureGridMeshObjectVisualDirtyFlags)
            {
                FeatureGridMeshObjectVisualDirtyFlags featureMeshRendererVisualDirtyFlags = meshRendererVisualDirtyFlags as FeatureGridMeshObjectVisualDirtyFlags;

                featureMeshRendererVisualDirtyFlags.feature = feature;

                //Processing was probably interrupted by Recompile or Play so we start it again
                if (featureMeshRendererVisualDirtyFlags.ProcessingWasCompromised())
                    featureMeshRendererVisualDirtyFlags.AllDirty();
            }
        }

        protected virtual string GetShaderPath()
        {
            return null;
        }

        protected override void InitializeMaterial(MeshRenderer meshRenderer, Material material = null)
        {
            string shaderPath = GetShaderPath();
            
            if (!string.IsNullOrEmpty(shaderPath))
            {
                int meshRendererInstanceId = meshRenderer.GetInstanceID();

                _materialsDictionary ??= new MeshRendererMaterialDictionary();
                _materialsDictionary.TryGetValue(meshRendererInstanceId, out material);
                _materialsDictionary[meshRendererInstanceId] = UpdateMaterial(ref material, shaderPath);
            }

            base.InitializeMaterial(meshRenderer, material);
        }

        protected override Type GetProcessorParametersType()
        {
            return typeof(FeatureParameters);
        }

        protected override void InitializeProcessorParameters(ProcessorParameters parameters)
        {
            base.InitializeProcessorParameters(parameters);

            if (meshRendererVisualDirtyFlags is FeatureGridMeshObjectVisualDirtyFlags)
            {
                FeatureGridMeshObjectVisualDirtyFlags featureGridMeshObjectVisualDirtyFlags = meshRendererVisualDirtyFlags as FeatureGridMeshObjectVisualDirtyFlags;

                (parameters as FeatureParameters).Init(featureGridMeshObjectVisualDirtyFlags.feature);
            }
        }

        public override bool OnDispose(DisposeContext disposeContext)
        {
            if (base.OnDispose(disposeContext))
            {
                if (_materialsDictionary != null)
                {
                    foreach (Material material in _materialsDictionary.Values)
                        DisposeManager.Dispose(material, disposeContext);
                }

                return true;
            }
            return false;
        }

        protected class FeatureParameters : ElevationGridMeshObjectParameters
        {
            private Feature _feature;

            public override void Recycle()
            {
                base.Recycle();

                _feature = default;
            }

            public FeatureParameters Init(Feature feature)
            {
                Lock(feature);
                _feature = feature;

                return this;
            }

            public Feature feature
            {
                get { return _feature; }
            }
        }

        protected class FeatureGridMeshObjectProcessingFunctions : ProcessingFunctions
        {
            private static readonly int NUM_Y_SEGMENTS = 24;
            private static readonly int NUM_X_SEGMENTS = 32;

            private static readonly int MAX_TRIANGLE = 65535;

            private static MeshRendererVisualModifier GetMeshRendererVisualModifier(MeshObjectProcessorOutput meshObjectProcessorOutput, int addTriangleCount)
            {
                if (meshObjectProcessorOutput.meshRendererVisualModifiersCount == 0 || meshObjectProcessorOutput.currentMeshRendererVisualModifier.meshModifier.triangles.Count + addTriangleCount > MAX_TRIANGLE)
                    meshObjectProcessorOutput.AddMeshRendererVisualModifier(CreateMeshRendererVisualModifier());

                return meshObjectProcessorOutput.currentMeshRendererVisualModifier;
            }

            private static MeshRendererVisualModifier CreateMeshRendererVisualModifier()
            {
                MeshRendererVisualModifier meshRendererVisualModifier = MeshRendererVisual.CreateMeshRendererVisualModifier();
                
                meshRendererVisualModifier.CreateMeshModifier<FeatureMeshModifier>().Init(0, 0, 0, 0, 0);
                
                return meshRendererVisualModifier;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected static MeshRendererVisualModifier AddQuad(MeshObjectProcessorOutput meshObjectProcessorOutput, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color32 color, bool autoGenerateUVs = true)
            {
                Vector3 n = GetNormal(a, b, c);
                if (n == Vector3.zero)
                    return null;

                MeshRendererVisualModifier meshRendererVisualModifier = GetMeshRendererVisualModifier(meshObjectProcessorOutput, 4);

                int verticesCount = meshRendererVisualModifier.meshModifier.vertices.Count;
                meshRendererVisualModifier.meshModifier.triangles.Add(verticesCount + 2);
                meshRendererVisualModifier.meshModifier.triangles.Add(verticesCount + 1);
                meshRendererVisualModifier.meshModifier.triangles.Add(verticesCount);
                meshRendererVisualModifier.meshModifier.triangles.Add(verticesCount + 1);
                meshRendererVisualModifier.meshModifier.triangles.Add(verticesCount + 3);
                meshRendererVisualModifier.meshModifier.triangles.Add(verticesCount);

                meshRendererVisualModifier.meshModifier.vertices.Add(a);
                meshRendererVisualModifier.meshModifier.UpdateMinMaxBounds(a);
                meshRendererVisualModifier.meshModifier.vertices.Add(c);
                meshRendererVisualModifier.meshModifier.UpdateMinMaxBounds(c);
                meshRendererVisualModifier.meshModifier.vertices.Add(b);
                meshRendererVisualModifier.meshModifier.UpdateMinMaxBounds(b);
                meshRendererVisualModifier.meshModifier.vertices.Add(d);
                meshRendererVisualModifier.meshModifier.UpdateMinMaxBounds(d);

                meshRendererVisualModifier.meshModifier.normals.Add(n);
                meshRendererVisualModifier.meshModifier.normals.Add(n);
                meshRendererVisualModifier.meshModifier.normals.Add(n);
                meshRendererVisualModifier.meshModifier.normals.Add(n);

                meshRendererVisualModifier.meshModifier.colors.Add(color);
                meshRendererVisualModifier.meshModifier.colors.Add(color);
                meshRendererVisualModifier.meshModifier.colors.Add(color);
                meshRendererVisualModifier.meshModifier.colors.Add(color);

                if (autoGenerateUVs)
                {
                    meshRendererVisualModifier.meshModifier.uvs.Add(ONE_ZERO);
                    meshRendererVisualModifier.meshModifier.uvs.Add(ZERO_ONE);
                    meshRendererVisualModifier.meshModifier.uvs.Add(Vector2.zero);
                    meshRendererVisualModifier.meshModifier.uvs.Add(Vector2.one);
                }

                return meshRendererVisualModifier;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected static MeshRendererVisualModifier AddTriangle(MeshObjectProcessorOutput meshObjectProcessorOutput, Vector3 a, Vector3 b, Vector3 c, Color32 color, bool autoGenerateUVs = true)
            {
                Vector3 n = GetNormal(a, b, c);
                if (n == Vector3.zero)
                    return null;

                MeshRendererVisualModifier meshRendererVisualModifier = GetMeshRendererVisualModifier(meshObjectProcessorOutput, 3);

                int verticesCount = meshRendererVisualModifier.meshModifier.vertices.Count;
                meshRendererVisualModifier.meshModifier.triangles.Add(verticesCount + 2);
                meshRendererVisualModifier.meshModifier.triangles.Add(verticesCount + 1);
                meshRendererVisualModifier.meshModifier.triangles.Add(verticesCount);

                meshRendererVisualModifier.meshModifier.vertices.Add(a);
                meshRendererVisualModifier.meshModifier.UpdateMinMaxBounds(a);
                meshRendererVisualModifier.meshModifier.vertices.Add(c);
                meshRendererVisualModifier.meshModifier.UpdateMinMaxBounds(c);
                meshRendererVisualModifier.meshModifier.vertices.Add(b);
                meshRendererVisualModifier.meshModifier.UpdateMinMaxBounds(b);

                meshRendererVisualModifier.meshModifier.normals.Add(n);
                meshRendererVisualModifier.meshModifier.normals.Add(n);
                meshRendererVisualModifier.meshModifier.normals.Add(n);

                meshRendererVisualModifier.meshModifier.colors.Add(color);
                meshRendererVisualModifier.meshModifier.colors.Add(color);
                meshRendererVisualModifier.meshModifier.colors.Add(color);

                if (autoGenerateUVs)
                {
                    meshRendererVisualModifier.meshModifier.uvs.Add(Vector2.zero);
                    meshRendererVisualModifier.meshModifier.uvs.Add(ONE_ZERO);
                    meshRendererVisualModifier.meshModifier.uvs.Add(ZERO_ONE);
                }

                return meshRendererVisualModifier;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected static MeshRendererVisualModifier AddBuffers(MeshObjectProcessorOutput meshObjectProcessorOutput, List<int> triangles, List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, List<Color32> colors = null)
            {
                MeshRendererVisualModifier meshRendererVisualModifier = GetMeshRendererVisualModifier(meshObjectProcessorOutput, triangles.Count);

                int verticesCount = meshRendererVisualModifier.meshModifier.vertices.Count;
                foreach (int triangle in triangles)
                    meshRendererVisualModifier.meshModifier.triangles.Add(verticesCount + triangle);
                
                foreach (Vector3 vertex in vertices)
                {
                    meshRendererVisualModifier.meshModifier.vertices.Add(vertex);
                    meshRendererVisualModifier.meshModifier.UpdateMinMaxBounds(vertex);
                }

                meshRendererVisualModifier.meshModifier.normals.AddRange(normals);

                meshRendererVisualModifier.meshModifier.uvs.AddRange(uvs);

                if (colors != null)
                    meshRendererVisualModifier.meshModifier.colors.AddRange(colors);

                return meshRendererVisualModifier;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector3 GetNormal(Vector3 a, Vector3 b, Vector3 c)
            {
                // Find vectors corresponding to two of the sides of the triangle.
                Vector3 side1 = b - a;
                Vector3 side2 = c - a;

                Vector3 cross = Vector3.Cross(side1, side2);

                float magnitude = cross.magnitude;

                // Cross the vectors to get a perpendicular vector, then normalize it.
                return magnitude != 0.0f ? cross / magnitude : Vector3.zero;
            }

            protected static void AddCircle(MeshObjectProcessorOutput meshObjectProcessorOutput, Vector3 center, float radius, float zPos, Color32 color)
            {
                float u, v;
                for (int i = 0; i < NUM_X_SEGMENTS; i++)
                {
                    u = (float)i / NUM_X_SEGMENTS;
                    v = (i + 1.0f) / NUM_X_SEGMENTS;
                    AddTriangle(
                    meshObjectProcessorOutput,

                    new Vector3(center[0] + radius * Mathf.Sin(u * Mathf.PI * 2.0f), center[1] + radius * Mathf.Cos(u * Mathf.PI * 2.0f), zPos),

                    new Vector3(center[0], center[1], zPos),

                    new Vector3(center[0] + radius * Mathf.Sin(v * Mathf.PI * 2.0f), center[1] + radius * Mathf.Cos(v * Mathf.PI * 2.0f), zPos),
                    color
                    );
                }
            }

            protected static void AddPolygon(MeshObjectProcessorOutput meshObjectProcessorOutput, VectorPolygon[] polygons, float zPos, Color32 color)
            {
                List<float> vertexBuffer = new();
                List<int> vectorIndex = new();

                int index = 0;
                for (int i = 0; i < polygons.Length; i++)
                {
                    VectorPolygon polygon = polygons[i];
                    foreach (Vector3 vector in polygon.vectors)
                    {
                        vertexBuffer.Add(vector.x);
                        vertexBuffer.Add(vector.y);
                        vertexBuffer.Add(zPos + vector.z);
                    }
                    if (i != 0)
                    {
                        index += polygons[i - 1].vectors.Count;
                        vectorIndex.Add(index);
                    }
                }

                List<int> vertices = Earcut.Tessellate(vertexBuffer, vectorIndex, 3);

                for (int i = 0; i < vertices.Count - 2; i += 3)
                {
                    int v1 = vertices[i] * 3;
                    int v2 = vertices[i + 1] * 3;
                    int v3 = vertices[i + 2] * 3;
                    AddTriangle(
                    meshObjectProcessorOutput,

                    new Vector3(vertexBuffer[v1], vertexBuffer[v1 + 1], vertexBuffer[v1 + 2]),

                    new Vector3(vertexBuffer[v2], vertexBuffer[v2 + 1], vertexBuffer[v2 + 2]),

                    new Vector3(vertexBuffer[v3], vertexBuffer[v3 + 1], vertexBuffer[v3 + 2]),
                    color
                    );
                }
            }

            protected static void AddCube(MeshObjectProcessorOutput meshObjectProcessorOutput, float sizeX, float sizeY, float sizeZ, float X, float Y, float zPos, Color32 color)
            {
                Vector3 a = new(X, Y, zPos);
                Vector3 b = new(X + sizeX, Y, zPos);
                Vector3 c = new(X + sizeX, Y + sizeY, zPos);
                Vector3 d = new(X, Y + sizeY, zPos);
                Vector3 A = new(X, Y, zPos + sizeZ);
                Vector3 B = new(X + sizeX, Y, zPos + sizeZ);
                Vector3 C = new(X + sizeX, Y + sizeY, zPos + sizeZ);
                Vector3 D = new(X, Y + sizeY, zPos + sizeZ);

                AddQuad(meshObjectProcessorOutput, b, a, d, c, color);
                AddQuad(meshObjectProcessorOutput, A, B, C, D, color);
                AddQuad(meshObjectProcessorOutput, a, b, B, A, color);
                AddQuad(meshObjectProcessorOutput, b, c, C, B, color);
                AddQuad(meshObjectProcessorOutput, c, d, D, C, color);
                AddQuad(meshObjectProcessorOutput, d, a, A, D, color);
            }

            protected static void AddCylinder(MeshObjectProcessorOutput meshObjectProcessorOutput, Vector3 center, float radius1, float radius2, float height, float zPos, Color32 color)
            {
                int num = NUM_X_SEGMENTS;
                float doublePI = Mathf.PI * 2.0f;

                float currAngle, nextAngle,
                currSin, currCos,
                nextSin, nextCos;

                for (int i = 0; i < num; i++)
                {
                    currAngle = ((float)i / num) * doublePI;
                    nextAngle = ((i + 1.0f) / num) * doublePI;

                    currSin = Mathf.Sin(currAngle);
                    currCos = Mathf.Cos(currAngle);

                    nextSin = Mathf.Sin(nextAngle);
                    nextCos = Mathf.Cos(nextAngle);

                    AddTriangle(
                    meshObjectProcessorOutput,

                    new Vector3(center[0] + radius1 * currSin, center[1] + radius1 * currCos, zPos),

                    new Vector3(center[0] + radius2 * nextSin, center[1] + radius2 * nextCos, zPos + height),

                    new Vector3(center[0] + radius1 * nextSin, center[1] + radius1 * nextCos, zPos),
                    color
                    );

                    if (!Object.Equals(radius2, 0.0f))
                    {
                        AddTriangle(
                        meshObjectProcessorOutput,

                        new Vector3(center[0] + radius2 * currSin, center[1] + radius2 * currCos, zPos + height),

                        new Vector3(center[0] + radius2 * nextSin, center[1] + radius2 * nextCos, zPos + height),

                        new Vector3(center[0] + radius1 * currSin, center[1] + radius1 * currCos, zPos),
                        color
                        );
                    }
                }
            }

            protected static void AddDome(MeshObjectProcessorOutput meshObjectProcessorOutput, Vector3 center, float radius, float height, float zPos, Color32 color, bool flip = false)
            {
                float yNum = NUM_Y_SEGMENTS / 2.0f;
                float quarterCircle = Mathf.PI / 2.0f;
                float circleOffset = flip ? 0.0f : -quarterCircle;

                float currYAngle, nextYAngle,
                x1, y1,
                x2, y2,
                radius1, radius2,
                newHeight, newZPos;

                // goes top-down
                for (int i = 0; i < yNum; i++)
                {
                    currYAngle = (i / yNum) * quarterCircle + circleOffset;
                    nextYAngle = ((i + 1.0f) / yNum) * quarterCircle + circleOffset;

                    x1 = Mathf.Cos(currYAngle);
                    y1 = Mathf.Sin(currYAngle);

                    x2 = Mathf.Cos(nextYAngle);
                    y2 = Mathf.Sin(nextYAngle);

                    radius1 = x1 * radius;
                    radius2 = x2 * radius;

                    newHeight = (y2 - y1) * height;
                    newZPos = zPos - y2 * height;

                    AddCylinder(meshObjectProcessorOutput, center, radius2, radius1, newHeight, newZPos, color);
                }
            }

            protected static void AddSphere(MeshObjectProcessorOutput meshObjectProcessorOutput, Vector3 center, float radius, float height, float zPos, Color32 color)
            {
                AddDome(meshObjectProcessorOutput, center, radius, height / 2.0f, zPos + height / 2.0f, color, true);
                AddDome(meshObjectProcessorOutput, center, radius, height / 2.0f, zPos + height / 2.0f, color);
            }

            protected static void AddPyramid(MeshObjectProcessorOutput meshObjectProcessorOutput, VectorPolygon[] polygons, Vector3 center, float height, float zPos, Color32 color)
            {
                VectorPolygon polygon = polygons[0];
                for (int i = 0, il = polygon.vectors.Count - 1; i < il; i++)
                {
                    AddTriangle(
                    meshObjectProcessorOutput,

                    new Vector3(polygon.vectors[i].x, polygon.vectors[i].y, zPos),

                    new Vector3(polygon.vectors[i + 1].x, polygon.vectors[i + 1].y, zPos),

                    new Vector3(center[0], center[1], zPos + height),
                    color
                    );
                }
            }

            protected static void AddExtrusion(MeshObjectProcessorOutput meshObjectProcessorOutput, VectorPolygon[] polygons, float height, float zPos, Color32 color, Vector4 texCoord)
            {
                Vector2 a, b;
                float L;
                Vector3 v0, v1, v2, v3;
                float tx1, tx2;
                float ty1 = texCoord[2] * height, ty2 = texCoord[3] * height;
                int r, rl;

                foreach (VectorPolygon polygon in polygons)
                {
                    for (r = 0, rl = polygon.vectors.Count - 1; r < rl; r++)
                    {
                        a = b = Vector2.zero;
                        L = 0;

                        a = polygon.vectors[r];
                        b = polygon.vectors[r + 1];
                        L = (a - b).magnitude;

                        v0 = new Vector3(a[0], a[1], zPos);
                        v1 = new Vector3(b[0], b[1], zPos);
                        v2 = new Vector3(b[0], b[1], zPos + height);
                        v3 = new Vector3(a[0], a[1], zPos + height);

                        MeshRendererVisualModifier meshRendererVisualModifier = AddQuad(meshObjectProcessorOutput, v0, v1, v2, v3, color, false);

                        if (meshRendererVisualModifier != null)
                        {
                            tx1 = (float)Math.Truncate(texCoord[0] * L);
                            tx2 = (float)Math.Truncate(texCoord[1] * L);

                            meshRendererVisualModifier.meshModifier.uvs.Add(new Vector2(tx2, ty1));
                            meshRendererVisualModifier.meshModifier.uvs.Add(new Vector2(tx1, ty2));
                            meshRendererVisualModifier.meshModifier.uvs.Add(new Vector2(tx1, ty1));
                            meshRendererVisualModifier.meshModifier.uvs.Add(new Vector2(tx2, ty2));
                        }
                    }
                }
            }

            protected static bool GetElevation(FeatureParameters parameters, Vector3Double center, ref double elevation)
            {
                if (parameters.elevation != null)
                {
                    Vector2Double centerIndex = MathPlus.GetIndexFromLocalPoint(parameters.centerPoint + (parameters.centerRotation * center), parameters.grid2DDimensions, parameters.sphericalRatio == 1.0f, parameters.normalizedRadiusSize, parameters.normalizedCircumferenceSize);
                    elevation = parameters.GetElevation((float)(centerIndex.x - parameters.grid2DIndex.x), (float)(centerIndex.y - parameters.grid2DIndex.y), true);
                    return true;
                }

                elevation = 0.0d;
                return false;
            }
        }
    }
}
