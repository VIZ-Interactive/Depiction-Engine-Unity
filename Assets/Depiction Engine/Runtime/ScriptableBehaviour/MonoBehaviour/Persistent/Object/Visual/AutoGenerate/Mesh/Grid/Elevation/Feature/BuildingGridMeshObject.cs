// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/orgs/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

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
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace DepictionEngine
{
    [AddComponentMenu(SceneManager.NAMESPACE + "/Object/Astro/Grid/" + nameof(BuildingGridMeshObject))]
    [CreateComponent(typeof(AssetReference), typeof(AssetReference))]
    public class BuildingGridMeshObject : FeatureGridMeshObjectBase
    {
        public const string COLORMAP_REFERENCE_DATATYPE = nameof(Texture) + " ColorMap";
        public const string ADDITIONALMAP_REFERENCE_DATATYPE = nameof(Texture) + " AdditionalMap";

        [BeginFoldout("Building")]
        [SerializeField, Tooltip("The path of the material's shader from within the Resources directory.")]
        private string _shaderPath;
        [SerializeField, Tooltip("A fallback vertex color value used by the parser if no other value are present in the feature.")]
        private Color _defaultColor;
        [SerializeField, Tooltip("An override vertex color value used by the parser for all the feature. The alpha channel is used to interpolate between this override color and the regular color.")]
        private Color _overrideColor;
        [SerializeField, Tooltip("A fallback building height value used by the parser if no other value are present in the feature. ")]
        private float _defaultHeight;
        [SerializeField, Tooltip("A fallback level height value used by the parser if no other value are present in the feature. "), EndFoldout]
        private float _defaultLevelHeight;

        private Texture _colorMap;
        private Texture _additionalMap;

        protected override void InitializeSerializedFields(InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            InitValue(value => shaderPath = value, GetDefaultShaderPath(), initializingContext);
            InitValue(value => defaultColor = value, new Color(0.6352941f, 0.5882353f, 0.5411765f), initializingContext);
            InitValue(value => overrideColor = value, new Color(1.0f, 1.0f, 1.0f, 0.0f), initializingContext);
            InitValue(value => defaultHeight = value, 10.0f, initializingContext);
            InitValue(value => defaultLevelHeight = value, 3.0f, initializingContext);
        }

        protected override void CreateAndInitializeDependencies(InitializationContext initializingContext)
        {
            base.CreateAndInitializeDependencies(initializingContext);

            InitializeReferenceDataType(COLORMAP_REFERENCE_DATATYPE, typeof(AssetReference));
            InitializeReferenceDataType(ADDITIONALMAP_REFERENCE_DATATYPE, typeof(AssetReference));
        }

        protected override bool UpdateReferences(bool forceUpdate = false)
        {
            if (base.UpdateReferences(forceUpdate))
            {
                colorMap = GetAssetFromAssetReference<Texture>(colorMapAssetReference);
                additionalMap = GetAssetFromAssetReference<Texture>(additionalMapAssetReference);

                return true;
            }
            return false;
        }

        protected override bool IterateOverAssetReferences(Func<AssetBase, AssetReference, bool, bool> callback)
        {
            if (base.IterateOverAssetReferences(callback))
            {
                if (!callback.Invoke(colorMap, colorMapAssetReference, false))
                    return false;
                if (!callback.Invoke(additionalMap, additionalMapAssetReference, false))
                    return false;

                return true;
            }
            return false;
        }

        protected override string GetDefaultShaderPath()
        {
            return base.GetDefaultShaderPath() + "BuildingGrid";
        }

        protected override bool SetPopupT(float value)
        {
            if (base.SetPopupT(value))
            {
                if (initialized)
                    UpdateChildrenMeshRendererVisualLocalScale();

                return true;
            }
            return false;
        }

        /// <summary>
        /// The path of the material's shader from within the Resources directory.
        /// </summary>
        [Json]
        public string shaderPath
        {
            get => _shaderPath;
            set => SetValue(nameof(shaderPath), value, ref _shaderPath);
        }

        /// <summary>
        /// A fallback vertex color value used by the parser if no other value are present in the feature. 
        /// </summary>
        [Json]
        public Color defaultColor
        {
            get => _defaultColor;
            set => SetValue(nameof(defaultColor), value, ref _defaultColor);
        }

        /// <summary>
        /// An override vertex color value used by the parser for all the feature. The alpha channel is used to interpolate between this override color and the regular color.
        /// </summary>
        [Json]
        public Color overrideColor
        {
            get => _overrideColor;
            set => SetValue(nameof(overrideColor), value, ref _overrideColor);
        }

        /// <summary>
        /// A fallback building height value used by the parser if no other value are present in the feature. 
        /// </summary>
        [Json]
        public float defaultHeight
        {
            get => _defaultHeight;
            set => SetValue(nameof(defaultHeight), value, ref _defaultHeight);
        }

        /// <summary>
        /// A fallback level height value used by the parser if no other value are present in the feature. 
        /// </summary>
        [Json]
        public float defaultLevelHeight
        {
            get => _defaultLevelHeight;
            set => SetValue(nameof(defaultLevelHeight), value, ref _defaultLevelHeight);
        }

        private AssetReference colorMapAssetReference
        {
            get => GetFirstReferenceOfType(COLORMAP_REFERENCE_DATATYPE) as AssetReference;
        }

        public Texture colorMap
        {
            get => _colorMap;
            private set => SetValue(nameof(colorMap), value, ref _colorMap);
        }

        protected override Texture GetColorMap()
        {
            return colorMap;
        }

        private AssetReference additionalMapAssetReference
        {
            get => GetFirstReferenceOfType(ADDITIONALMAP_REFERENCE_DATATYPE) as AssetReference;
        }

        public Texture additionalMap
        {
            get => _additionalMap;
            private set => SetValue(nameof(additionalMap), value, ref _additionalMap); 
        }

        protected override Texture GetAdditionalMap()
        {
            return additionalMap;
        }

        protected override Type GetMeshRendererVisualDirtyFlagType()
        {
            return typeof(BuildingGridMeshObjectVisualDirtyFlags);
        }

        protected override void UpdateMeshRendererDirtyFlags(VisualObjectVisualDirtyFlags meshRendererVisualDirtyFlags)
        {
            base.UpdateMeshRendererDirtyFlags(meshRendererVisualDirtyFlags);

            if (meshRendererVisualDirtyFlags is BuildingGridMeshObjectVisualDirtyFlags)
            {
                BuildingGridMeshObjectVisualDirtyFlags buildingMeshRendererVisualDirtyFlags = meshRendererVisualDirtyFlags as BuildingGridMeshObjectVisualDirtyFlags;

                buildingMeshRendererVisualDirtyFlags.defaultColor = defaultColor;
                buildingMeshRendererVisualDirtyFlags.overrideColor = overrideColor;
                buildingMeshRendererVisualDirtyFlags.defaultLevelHeight = defaultLevelHeight;
                buildingMeshRendererVisualDirtyFlags.defaultHeight = defaultHeight;
            }
        }

        protected override Func<ProcessorOutput, ProcessorParameters, IEnumerator> GetProcessorFunction()
        {
            return BuildingFeatureProcessingFunctions.PopulateMeshes;
        }

        protected override Type GetProcessorParametersType()
        {
            return typeof(BuildingGridMeshObjectParameters);
        }

        protected override void InitializeProcessorParameters(ProcessorParameters parameters)
        {
            base.InitializeProcessorParameters(parameters);

            if (meshRendererVisualDirtyFlags is BuildingGridMeshObjectVisualDirtyFlags)
            {
                BuildingGridMeshObjectVisualDirtyFlags buildingMeshRendererVisualDirtyFlags = meshRendererVisualDirtyFlags as BuildingGridMeshObjectVisualDirtyFlags;

                (parameters as BuildingGridMeshObjectParameters).Init(buildingMeshRendererVisualDirtyFlags.defaultColor, buildingMeshRendererVisualDirtyFlags.overrideColor, buildingMeshRendererVisualDirtyFlags.defaultLevelHeight, buildingMeshRendererVisualDirtyFlags.defaultHeight);
            }
        }

        protected override void OnFeatureClickedHit(RaycastHitDouble hit, int featureIndex)
        {
            base.OnFeatureClickedHit(hit, featureIndex);

            BuildingFeature buildingFeature = feature as BuildingFeature;
            if (buildingFeature != Disposable.NULL)
                Debug.Log(buildingFeature.GetGeoCoordinateGeometries(featureIndex));
        }

        protected override Vector3 GetMeshRendererVisualLocalScale()
        {
            return new Vector3(meshRendererVisualLocalScale.x, meshRendererVisualLocalScale.y, meshRendererVisualLocalScale.z * GetPopupT(PopupType.Scale));
        }

        protected override void ApplyPropertiesToVisual(bool visualsChanged, VisualObjectVisualDirtyFlags meshRendererVisualDirtyFlags)
        {
            base.ApplyPropertiesToVisual(visualsChanged, meshRendererVisualDirtyFlags);

            transform.IterateOverChildren<MeshRendererVisual>((meshRendererVisual) =>
            {
                if (visualsChanged)
                    meshRendererVisual.transform.localRotation = Quaternion.Euler(-90.0f, 0.0f, 0.0f);
                
                return true;
            });
        }

        protected override string GetShaderPath()
        {
            return shaderPath;
        }

        protected class BuildingGridMeshObjectParameters : FeatureParameters
        {
            private Color _defaultColor;
            private Color _overrideColor;
            private float _defaultLevelHeight;
            private float _defaultHeight;

            public override void Recycle()
            {
                base.Recycle();

                _defaultColor = default;
                _overrideColor = default;
                _defaultLevelHeight = default;
                _defaultHeight = default;
            }

            public BuildingGridMeshObjectParameters Init(Color defaultColor, Color overrideColor, float defaultLevelHeight, float defaultHeight)
            {
                _defaultColor = defaultColor;
                _overrideColor = overrideColor;
                _defaultLevelHeight = defaultLevelHeight;
                _defaultHeight = defaultHeight;

                return this;
            }

            protected override bool UseAltitude()
            {
                return false;
            }

            public Color defaultColor
            {
                get => _defaultColor;
            }

            public Color overrideColor
            {
                get => _overrideColor;
            }

            public float defaultLevelHeight
            {
                get => _defaultLevelHeight;
            }

            public float defaultHeight
            {
                get => _defaultHeight;
            }
        }

        protected class BuildingFeatureProcessingFunctions : FeatureGridMeshObjectProcessingFunctions
        {
            // number of windows per horizontal meter of building wall
            private const float WINDOWS_PER_METER = 0.5f;
            private const float PENETRATION_HEIGHT = 100.0f;
            private const int YIELD_FEATURE_INTERVAL = 500;

            public static IEnumerator PopulateMeshes(object data, ProcessorParameters parameters)
            {
                foreach (object enumeration in PopulateMeshes(data as MeshObjectProcessorOutput, parameters as BuildingGridMeshObjectParameters))
                    yield return enumeration;
            }

            private static IEnumerable PopulateMeshes(MeshObjectProcessorOutput meshObjectProcessorOutput, BuildingGridMeshObjectParameters parameters)
            {
                BuildingFeature buildingFeature = parameters.feature as BuildingFeature;
                if (buildingFeature != null)
                {
                    int nextYieldFeatureCount = YIELD_FEATURE_INTERVAL;
                    for (int featureIndex = 0; featureIndex < buildingFeature.featureCount; featureIndex++)
                    {
                        Color wallColor = buildingFeature.GetWallColor(featureIndex, parameters.defaultColor, parameters.overrideColor);
                        Color roofColor = buildingFeature.GetRoofColor(featureIndex, parameters.defaultColor, parameters.overrideColor);

                        GeoCoordinateGeometries geoCoordinateGeometries = buildingFeature.GetGeoCoordinateGeometries(featureIndex);
                        foreach (GeoCoordinateGeometry geoCoordinateGeometry in geoCoordinateGeometries.geometries)
                        {
                            VectorGeometry vectorGeometry = new(new VectorPolygon[geoCoordinateGeometry.polygons.Length]);
                            foreach (GeoCoordinatePolygon polygon in geoCoordinateGeometry.polygons)
                            {
                                VectorPolygon vectorPolygon = new(new List<Vector3>(polygon.Count / 2));
                                polygon.IterateOverGeoCoordinates(
                                    (latitude, longitude) =>
                                    {
                                        Vector3 vector = parameters.TransformGeoCoordinateToVector(latitude, longitude);
                                        float z = vector.y;
                                        vector.y = -vector.z;
                                        vector.z = z;
                                        vectorPolygon.vectors.Add(vector);
                                    });
                                vectorGeometry.Add(vectorPolygon);
                            }

                            bool hasHeight = buildingFeature.GetHasHeight(featureIndex);
                            float height = buildingFeature.GetHeight(featureIndex);
                            float minHeight = buildingFeature.GetMinHeight(featureIndex);
                            float roofHeight = buildingFeature.GetRoofHeight(featureIndex);
                            float wallHeight;
                            float wallZ;
                            float roofZ;
                            float uvTilePerUnit = WINDOWS_PER_METER;

                            //*** roof height *********************************************************
                            roofHeight = roofHeight != 0.0f ? roofHeight : buildingFeature.GetRoofLevels(featureIndex) != 0 ? buildingFeature.GetRoofLevels(featureIndex) * parameters.defaultLevelHeight : 0.0f;

                            switch (buildingFeature.GetRoofShape(featureIndex))
                            {
                                case BuildingFeature.RoofShape.Cone:
                                case BuildingFeature.RoofShape.Pyramid:
                                case BuildingFeature.RoofShape.Dome:
                                case BuildingFeature.RoofShape.Onion:
                                    roofHeight = !Object.Equals(roofHeight, 0.0f) ? roofHeight : vectorGeometry.radius;
                                    break;

                                case BuildingFeature.RoofShape.Gabled:
                                case BuildingFeature.RoofShape.Hipped:
                                case BuildingFeature.RoofShape.HalfHipped:
                                case BuildingFeature.RoofShape.Skillion:
                                case BuildingFeature.RoofShape.Gambrel:
                                case BuildingFeature.RoofShape.Mansard:
                                case BuildingFeature.RoofShape.Round:
                                    roofHeight = !Object.Equals(roofHeight, 0.0f) ? roofHeight : parameters.defaultLevelHeight;
                                    break;

                                case BuildingFeature.RoofShape.Flat:
                                    roofHeight = 0.0f;
                                    break;

                                default:
                                    // roofs we don't handle should not affect wallHeight
                                    roofHeight = 0.0f;
                                    break;
                            }

                            //*** wall height *********************************************************
                            float maxHeight;
                            wallZ = minHeight != 0.0f ? minHeight : buildingFeature.GetHasMinLevel(featureIndex) && buildingFeature.GetMinLevel(featureIndex) != 0.0f ? buildingFeature.GetMinLevel(featureIndex) * parameters.defaultLevelHeight : 0.0f;

                            if (hasHeight)
                            {
                                maxHeight = height;
                                roofHeight = Mathf.Min(roofHeight, maxHeight); // we don't want negative wall heights after subtraction
                                roofZ = maxHeight - roofHeight;
                                wallHeight = maxHeight - roofHeight - wallZ;
                            }
                            else if (buildingFeature.GetHasLevels(featureIndex))
                            {
                                maxHeight = buildingFeature.GetLevels(featureIndex) * parameters.defaultLevelHeight;
                                // dim.roofHeight remains unchanged
                                roofZ = maxHeight;
                                wallHeight = maxHeight - wallZ;
                            }
                            else
                            {
                                switch (buildingFeature.GetShape(featureIndex))
                                {
                                    case BuildingFeature.Shape.Cone:
                                    case BuildingFeature.Shape.Dome:
                                    case BuildingFeature.Shape.Pyramid:
                                        maxHeight = 2.0f * vectorGeometry.radius;
                                        roofHeight = 0.0f;
                                        break;

                                    case BuildingFeature.Shape.Sphere:
                                        maxHeight = 4.0f * vectorGeometry.radius;
                                        roofHeight = 0.0f;
                                        break;

                                    case BuildingFeature.Shape.None: // no walls at all
                                        maxHeight = 0.0f;
                                        break;

                                    // case 'cylinder':
                                    default:
                                        maxHeight = parameters.defaultHeight;
                                        break;
                                }
                                roofZ = maxHeight;
                                wallHeight = maxHeight - wallZ;
                            }

                            //Stretch walls through the Terrain to prevent buildings from floating
                            if (wallZ == 0.0f)
                            {
                                wallHeight += PENETRATION_HEIGHT;
                                wallZ -= PENETRATION_HEIGHT;
                            }

                            //Scale
                            float scale = parameters.scale;
                            float inverseScale = parameters.inverseScale;

                            roofHeight *= inverseScale;
                            wallHeight *= inverseScale;

                            roofZ *= inverseScale;
                            wallZ *= inverseScale;

                            uvTilePerUnit *= scale;

                            //Add Elevation
                            if (GetElevation(parameters, new Vector3Double(vectorGeometry.center.x, 0.0d, vectorGeometry.center.y), out float elevation))
                            {
                                float elevationDelta = elevation - parameters.centerElevation;

                                wallZ += elevationDelta;
                                roofZ += elevationDelta;
                            }

                            AddBuilding(meshObjectProcessorOutput, featureIndex, buildingFeature, vectorGeometry, wallHeight, wallZ, roofHeight, roofZ, wallColor, roofColor, uvTilePerUnit, parameters);

                            parameters.cancellationTokenSource?.ThrowIfCancellationRequested();
                        }

                        if (parameters.processingType == Processor.ProcessingType.AsyncCoroutine && featureIndex > nextYieldFeatureCount)
                        {
                            nextYieldFeatureCount += YIELD_FEATURE_INTERVAL;
                            yield return null;
                        }
                    }

                    foreach (MeshRendererVisualModifier meshRendererVisualModifier in meshObjectProcessorOutput.meshRendererVisualModifiers)
                        meshRendererVisualModifier.meshModifier.CalculateBoundsFromMinMax();
                }
                yield break;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void AddBuilding(MeshObjectProcessorOutput meshObjectProcessorOutput, int index, BuildingFeature buildingFeature, VectorGeometry vectorGeometry, float wallHeight, float wallZ, float roofHeight, float roofZ, Color wallColor, Color roofColor, float uvTilePerUnit, BuildingGridMeshObjectParameters parameters)
            {
                //*** process buildings that don't require a roof *************************
                switch (buildingFeature.GetShape(index))
                {
                    case BuildingFeature.Shape.Cone:
                        AddCylinder(meshObjectProcessorOutput, vectorGeometry.center, vectorGeometry.radius, 0.0f, wallHeight, wallZ, wallColor);
                        return;

                    case BuildingFeature.Shape.Dome:
                        AddDome(meshObjectProcessorOutput, vectorGeometry.center, vectorGeometry.radius, wallHeight, wallZ, wallColor);
                        return;

                    case BuildingFeature.Shape.Pyramid:
                        AddPyramid(meshObjectProcessorOutput, vectorGeometry.polygons, vectorGeometry.center, wallHeight, wallZ, wallColor);
                        return;

                    case BuildingFeature.Shape.Sphere:
                        AddSphere(meshObjectProcessorOutput, vectorGeometry.center, vectorGeometry.radius, wallHeight, wallZ, wallColor);
                        return;
                }

                //*** process roofs *******************************************************
                OSMRoofs.CreateRoof(meshObjectProcessorOutput, index, buildingFeature, vectorGeometry, roofHeight, roofZ, roofColor, wallColor);

                //*** process remaining buildings *****************************************
                switch (buildingFeature.GetShape(index))
                {
                    case BuildingFeature.Shape.None:
                        // no walls at all
                        return;

                    case BuildingFeature.Shape.Cylinder:
                        AddCylinder(meshObjectProcessorOutput, vectorGeometry.center, vectorGeometry.radius, vectorGeometry.radius, wallHeight, wallZ, wallColor);
                        return;

                    default: // extruded polygon
                        float ty1 = 0.0f;

                        float ty2;
                        if (buildingFeature.GetHasLevels(index) && buildingFeature.GetHasMinLevel(index))
                            ty2 = buildingFeature.GetLevels(index) - buildingFeature.GetMinLevel(index);
                        else
                            ty2 = wallHeight / (parameters.defaultLevelHeight * parameters.inverseScale);

                        AddExtrusion(meshObjectProcessorOutput, vectorGeometry.polygons, wallHeight, wallZ, wallColor, new Vector4(0.0f, uvTilePerUnit, ty1 / wallHeight, ty2 / wallHeight));
                        break;
                }

                FeatureMeshModifier featureMeshModifier = meshObjectProcessorOutput.currentMeshRendererVisualModifier != Disposable.NULL ? meshObjectProcessorOutput.currentMeshRendererVisualModifier.meshModifier as FeatureMeshModifier : null;
                if (featureMeshModifier != Disposable.NULL)
                    featureMeshModifier.FeatureComplete();
            }

            private class OSMRoofs
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static void CreateRoof(MeshObjectProcessorOutput meshObjectProcessorOutput, int index, BuildingFeature buildingFeature, VectorGeometry vectorGeometry, float roofHeight, float roofZ, Color32 roofColor, Color32 wallColor)
                {
                    switch (buildingFeature.GetRoofShape(index))
                    {
                        case BuildingFeature.RoofShape.Cone:
                            ConeRoof(meshObjectProcessorOutput, vectorGeometry, roofHeight, roofZ, roofColor);
                            break;
                        case BuildingFeature.RoofShape.Dome:
                            DomeRoof(meshObjectProcessorOutput, vectorGeometry, roofHeight, roofZ, roofColor);
                            break;
                        case BuildingFeature.RoofShape.Pyramid:
                            PyramidRoof(meshObjectProcessorOutput, index, buildingFeature, vectorGeometry, roofHeight, roofZ, roofColor);
                            break;
                        case BuildingFeature.RoofShape.Skillion:
                            SkillionRoof(meshObjectProcessorOutput, index, buildingFeature, vectorGeometry, roofHeight, roofZ, roofColor, wallColor);
                            break;
                        case BuildingFeature.RoofShape.Gabled:
                            RoofWithRidge(meshObjectProcessorOutput, index, buildingFeature, vectorGeometry, roofHeight, roofZ, roofColor, wallColor);
                            break;
                        case BuildingFeature.RoofShape.Hipped:
                            RoofWithRidge(meshObjectProcessorOutput, index, buildingFeature, vectorGeometry, roofHeight, roofZ, roofColor, wallColor);
                            break;
                        case BuildingFeature.RoofShape.HalfHipped:
                            RoofWithRidge(meshObjectProcessorOutput, index, buildingFeature, vectorGeometry, roofHeight, roofZ, roofColor, wallColor);
                            break;
                        case BuildingFeature.RoofShape.Gambrel:
                            RoofWithRidge(meshObjectProcessorOutput, index, buildingFeature, vectorGeometry, roofHeight, roofZ, roofColor, wallColor);
                            break;
                        case BuildingFeature.RoofShape.Mansard:
                            RoofWithRidge(meshObjectProcessorOutput, index, buildingFeature, vectorGeometry, roofHeight, roofZ, roofColor, wallColor);
                            break;
                        case BuildingFeature.RoofShape.Round:
                            RoundRoof(meshObjectProcessorOutput, index, buildingFeature, vectorGeometry, roofZ, roofColor);
                            break;
                        case BuildingFeature.RoofShape.Onion:
                            OnionRoof(meshObjectProcessorOutput, vectorGeometry, roofHeight, roofZ, roofColor);
                            break;
                        case BuildingFeature.RoofShape.Flat:
                        default:
                            FlatRoof(meshObjectProcessorOutput, index, buildingFeature, vectorGeometry, roofZ, roofColor);
                            break;
                    }
                }

                private static bool GetRidgeIntersections(ref RidgeIntersection ridgeIntersection, Vector3 center, Vector2 direction, VectorPolygon polygon)
                {
                    // create polygon intersections
                    for (var i = 0; i < polygon.vectors.Count - 1; i++)
                    {
                        if (RoofRidge.GetVectorSegmentIntersection(out Vector3 point, center, direction, new Segment(polygon.vectors[i], polygon.vectors[i + 1])))
                        {
                            if (ridgeIntersection.index.Count == 2)
                            {
                                // more than 2 intersections: too complex for gabled roof, should be hipped+skeleton anyway
                                return false;
                            }
                            i++;
                            polygon.vectors.Insert(i, point);
                            ridgeIntersection.index.Add(i);
                        }
                    }

                    // requires at least 2 intersections
                    if (ridgeIntersection.index.Count < 2)
                    {
                        return false;
                    }

                    ridgeIntersection.roof = polygon.vectors;
                    return true;
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                private static bool GetRidge(ref RidgeIntersection ridgeIntersection, float direction, Vector3 center, VectorPolygon polygon)
                {
                    float rad = ((direction - 90.0f) / 180.0f - 0.5f) * Mathf.PI;
                    return GetRidgeIntersections(ref ridgeIntersection, center, new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)), polygon);
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                private static void GetRidgeDistances(ref List<float> distances, List<Vector3> polygon, List<int> index)
                {
                    Segment ridge = new(polygon[index[0]], polygon[index[1]]);
                    foreach (Vector3 point in polygon)
                        distances.Add(GetDistanceToLine(point, ridge));
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                private static float GetDistanceToLine(Vector3 point, Segment line)
                {
                    Vector3
                      r1 = line.start,
                      r2 = line.end;
                    if (r1.x == r2.x && r1.y == r2.y)
                        return 0.0f;

                    float m1 = r2.x != r1.x ? (r2.y - r1.y) / (r2.x - r1.x) : 0.0f;
                    float b1 = r1.y - (m1 * r1.x);

                    if (m1 == 0)
                        return Mathf.Abs(b1 - point.y);

                    if (m1 == float.PositiveInfinity)
                        return Mathf.Abs(r1.x - point.x);

                    float m2 = -1.0f / m1;
                    float b2 = point.y - (m2 * point.x);

                    float xs = (b2 - b1) / (m1 - m2);
                    float ys = m1 * xs + b1;

                    float c1 = point.x - xs;
                    float c2 = point.y - ys;

                    return Mathf.Sqrt(c1 * c1 + c2 * c2);
                }

                private static void RoofWithRidge(MeshObjectProcessorOutput meshObjectProcessorOutput, int index, BuildingFeature buildingFeature, VectorGeometry vectorGeometry, float roofHeight, float roofZ, Color32 roofColor, Color32 wallColor)
                {
                    // no gabled roofs for polygons with holes, roof direction required
                    if (vectorGeometry.polygons.Length > 1 || !buildingFeature.GetHasRoofDirection(index))
                    {
                        FlatRoof(meshObjectProcessorOutput, index, buildingFeature, vectorGeometry, roofZ, roofColor);
                        return;
                    }

                    RidgeIntersection ridge = new();
                    if (!GetRidge(ref ridge, buildingFeature.GetRoofDirection(index), vectorGeometry.center, vectorGeometry.polygons[0]))
                    {
                        FlatRoof(meshObjectProcessorOutput, index, buildingFeature, vectorGeometry, roofZ, roofColor);
                        return;
                    }

                    List<int> ridgeIndex = ridge.index;
                    List<Vector3> roofPolygon = ridge.roof;

                    List<float> distances = new();
                    GetRidgeDistances(ref distances, roofPolygon, ridge.index);
                    float maxDistance = float.NegativeInfinity;
                    foreach (float distance in distances)
                        maxDistance = Mathf.Max(distance, maxDistance);

                    // set z of all vertices
                    List<Vector3> roofPolygon3 = new();
                    Vector3 vector;
                    for (int i = 0; i < roofPolygon.Count; i++)
                    {
                        vector = roofPolygon[i];
                        roofPolygon3.Add(new Vector3(vector.x, vector.y, (1 - distances[i] / maxDistance) * roofHeight));// closer to ridge -> closer to roof height
                    }

                    // create roof faces
                    List<Vector3> roof = Slice(roofPolygon3, ridgeIndex[0], ridgeIndex[1] + 1);
                    AddPolygon(meshObjectProcessorOutput, new VectorPolygon[] { new VectorPolygon(roof) }, roofZ, roofColor);

                    roof = Slice(roofPolygon3, ridgeIndex[1], roofPolygon3.Count - 1);
                    roof.AddRange(Slice(roofPolygon3, 0, ridgeIndex[0] + 1));
                    AddPolygon(meshObjectProcessorOutput, new VectorPolygon[] { new VectorPolygon(roof) }, roofZ, roofColor);

                    // create extra wall faces
                    for (int i = 0; i < roofPolygon3.Count - 1; i++)
                    {
                        // skip degenerate quads - could even skip degenerate triangles
                        if (roofPolygon3[i][2] == 0 && roofPolygon3[i + 1][2] == 0)
                        {
                            continue;
                        }
                        AddQuad(
                            meshObjectProcessorOutput,

                            new Vector3(roofPolygon3[i][0], roofPolygon3[i][1], roofZ + roofPolygon3[i][2]),

                            new Vector3(roofPolygon3[i][0], roofPolygon3[i][1], roofZ),

                            new Vector3(roofPolygon3[i + 1][0], roofPolygon3[i + 1][1], roofZ),

                            new Vector3(roofPolygon3[i + 1][0], roofPolygon3[i + 1][1], roofZ + roofPolygon3[i + 1][2]),
                            wallColor
                        );
                    }
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                private static List<T> Slice<T>(List<T> list, int start, int end)
                {
                    return list.GetRange(start, end - start);
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                private static void FlatRoof(MeshObjectProcessorOutput meshObjectProcessorOutput, int index, BuildingFeature buildingFeature, VectorGeometry vectorGeometry, float roofZ, Color32 roofColor)
                {
                    if (buildingFeature.GetShape(index) == BuildingFeature.Shape.Cylinder)
                        AddCircle(meshObjectProcessorOutput, vectorGeometry.center, vectorGeometry.radius, roofZ, roofColor);
                    else
                        AddPolygon(meshObjectProcessorOutput, vectorGeometry.polygons, roofZ, roofColor);
                }

                private static void SkillionRoof(MeshObjectProcessorOutput meshObjectProcessorOutput, int index, BuildingFeature buildingFeature, VectorGeometry vectorGeometry, float roofHeight, float roofZ, Color32 roofColor, Color32 wallColor)
                {
                    // roof direction required
                    if (!buildingFeature.GetHasRoofDirection(index))
                    {
                        FlatRoof(meshObjectProcessorOutput, index, buildingFeature, vectorGeometry, roofZ, roofColor);
                        return;
                    }

                    float
                      rad = buildingFeature.GetRoofDirection(index) / 180.0f * Mathf.PI;
                    Vector3 closestPoint = Vector3.negativeInfinity;
                    Vector3 farthestPoint = Vector3.negativeInfinity;
                    float minY = float.PositiveInfinity, maxY = float.NegativeInfinity;

                    foreach (Vector3 point in vectorGeometry.polygons[0].vectors)
                    {
                        float y = point[1] * Mathf.Cos(-rad) + point[0] * Mathf.Sin(-rad);
                        if (y < minY)
                        {
                            minY = y;
                            closestPoint = point;
                        }
                        if (y > maxY)
                        {
                            maxY = y;
                            farthestPoint = point;
                        }
                    }

                    VectorPolygon outerPolygon = vectorGeometry.polygons[0];
                    Vector2 roofDirection = new(Mathf.Cos(rad), Mathf.Sin(rad));
                    Segment ridge = new(closestPoint, new Vector3(closestPoint[0] + roofDirection[0], closestPoint[1] + roofDirection[1]));
                    float maxDistance = GetDistanceToLine(farthestPoint, ridge);

                    // modify vertical position of all points
                    foreach (VectorPolygon polygon in vectorGeometry.polygons)
                    {
                        for (int i = 0; i < polygon.vectors.Count; i++)
                        {
                            Vector3 point = polygon.vectors[i];
                            float distance = GetDistanceToLine(point, ridge);
                            polygon.vectors[i] = new Vector3(point.x, point.y, (distance / maxDistance) * roofHeight);
                        }
                    }

                    // create roof face
                    AddPolygon(meshObjectProcessorOutput, new VectorPolygon[] { outerPolygon }, roofZ, roofColor);

                    // create extra wall faces
                    foreach (VectorPolygon polygon in vectorGeometry.polygons)
                    {
                        for (var i = 0; i < polygon.vectors.Count - 1; i++)
                        {
                            // skip degenerate quads - could even skip degenerate triangles
                            if (polygon.vectors[i][2] == 0 && polygon.vectors[i + 1][2] == 0)
                                continue;

                            AddQuad(
                              meshObjectProcessorOutput,

                              new Vector3(polygon.vectors[i][0], polygon.vectors[i][1], roofZ + polygon.vectors[i][2]),

                              new Vector3(polygon.vectors[i][0], polygon.vectors[i][1], roofZ),

                              new Vector3(polygon.vectors[i + 1][0], polygon.vectors[i + 1][1], roofZ),

                              new Vector3(polygon.vectors[i + 1][0], polygon.vectors[i + 1][1], roofZ + polygon.vectors[i + 1][2]),
                              wallColor
                            );
                        }
                    }
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                private static void ConeRoof(MeshObjectProcessorOutput meshObjectProcessorOutput, VectorGeometry vectorGeometry, float roofHeight, float roofZ, Color32 roofColor)
                {
                    AddPolygon(meshObjectProcessorOutput, vectorGeometry.polygons, roofZ, roofColor);
                    AddCylinder(meshObjectProcessorOutput, vectorGeometry.center, vectorGeometry.radius, 0, roofHeight, roofZ, roofColor);
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                private static void DomeRoof(MeshObjectProcessorOutput meshObjectProcessorOutput, VectorGeometry vectorGeometry, float roofHeight, float roofZ, Color32 roofColor)
                {
                    AddPolygon(meshObjectProcessorOutput, vectorGeometry.polygons, roofZ, roofColor);
                    AddDome(meshObjectProcessorOutput, vectorGeometry.center, vectorGeometry.radius, roofHeight, roofZ, roofColor);
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                private static void PyramidRoof(MeshObjectProcessorOutput meshObjectProcessorOutput, int index, BuildingFeature buildingFeature, VectorGeometry vectorGeometry, float roofHeight, float roofZ, Color32 roofColor)
                {
                    if (buildingFeature.GetShape(index) == BuildingFeature.Shape.Cylinder)
                        AddCylinder(meshObjectProcessorOutput, vectorGeometry.center, vectorGeometry.radius, 0.0f, roofHeight, roofZ, roofColor);
                    else
                        AddPyramid(meshObjectProcessorOutput, vectorGeometry.polygons, vectorGeometry.center, roofHeight, roofZ, roofColor);
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                private static void RoundRoof(MeshObjectProcessorOutput meshObjectProcessorOutput, int index, BuildingFeature buildingFeature, VectorGeometry vectorGeometry, float roofZ, Color32 roofColor)
                {
                    // no round roofs for polygons with holes
                    if (vectorGeometry.polygons.Length > 1 || !buildingFeature.GetHasRoofDirection(index))
                    {
                        FlatRoof(meshObjectProcessorOutput, index, buildingFeature, vectorGeometry, roofZ, roofColor);
                        return;
                    }

                    FlatRoof(meshObjectProcessorOutput, index, buildingFeature, vectorGeometry, roofZ, roofColor);
                }

                private static readonly Vector2[] VECTORS_CONSTS = new Vector2[8]
                {
                    new Vector2(0.8f, 0.0f),
                    new Vector2(0.9f, 0.18f),
                    new Vector2(0.9f, 0.35f),
                    new Vector2(0.8f, 0.47f),
                    new Vector2(0.6f, 0.59f),
                    new Vector2(0.5f, 0.65f),
                    new Vector2(0.2f, 0.82f),
                    new Vector2(0.0f, 1f),
                };

                private static void OnionRoof(MeshObjectProcessorOutput meshObjectProcessorOutput, VectorGeometry vectorGeometry, float roofZ, float roofHeight, Color32 roofColor)
                {
                    AddPolygon(meshObjectProcessorOutput, vectorGeometry.polygons, roofZ, roofColor);

                    float h1, h2;
                    for (int i = 0, il = VECTORS_CONSTS.Length - 1; i < il; i++)
                    {
                        h1 = roofHeight * VECTORS_CONSTS[i].y;
                        h2 = roofHeight * VECTORS_CONSTS[i + 1].y;
                        AddCylinder(meshObjectProcessorOutput, vectorGeometry.center, vectorGeometry.radius * VECTORS_CONSTS[i].x, vectorGeometry.radius * VECTORS_CONSTS[i + 1].x, h2 - h1, roofZ + h1, roofColor);
                    }
                }

                private class RidgeIntersection
                {
                    public List<int> index;
                    public List<Vector3> roof;

                    public RidgeIntersection()
                    {
                        index = new List<int>();
                        roof = new List<Vector3>();
                    }

                    public void Clear()
                    {
                        index.Clear();
                        roof.Clear();
                    }
                }

                private class RoofRidge
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    private static Vector3 RoundPoint(Vector3 point, double f = 1e12)
                    {
                        return new Vector3(
                          (float)(Math.Round(point.x * f) / f),
                          (float)(Math.Round(point.y * f) / f),
                          point.z
                        );
                    }

                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    private static bool PointOnSegment(Vector3 point, Segment segment)
                    {
                        point = RoundPoint(point);
                        segment.start = RoundPoint(segment.start);
                        segment.end = RoundPoint(segment.end);
                        return
                          point.x >= Math.Min(segment.start.x, segment.end.x) &&
                          point.x <= Math.Max(segment.end.x, segment.start.x) &&
                          point.y >= Math.Min(segment.start.y, segment.end.y) &&
                          point.y <= Math.Max(segment.start.y, segment.end.y)
                        ;
                    }

                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    public static bool GetVectorSegmentIntersection(out Vector3 intersection, Vector3 point1, Vector2 vector1, Segment segment)
                    {
                        intersection = Vector3.zero;

                        Vector3 point2 = segment.start;
                        Vector2 vector2 = new Vector3(segment.end.x - segment.start.x, segment.end.y - segment.start.y);
                        float n1 = 0.0f,
                            n2 = 0.0f,
                            m1 = 0.0f,
                            m2 = 0.0f;
                        Vector3 xy;

                        if (vector1.x == 0 && vector2.x == 0)
                            return false;

                        if (vector1.x != 0)
                        {
                            m1 = vector1.y / vector1.x;
                            n1 = point1.y - m1 * point1.x;
                        }

                        if (vector2.x != 0)
                        {
                            m2 = vector2.y / vector2.x;
                            n2 = point2.y - m2 * point2.x;
                        }

                        if (vector1.x == 0)
                        {
                            xy = new Vector3(point1.x, m2 * point1.x + n2);
                            if (PointOnSegment(xy, segment))
                            {
                                intersection = xy;
                                return true;
                            }
                        }

                        if (vector2.x == 0)
                        {
                            xy = new Vector3(point2.x, m1 * point2.x + n1);
                            if (PointOnSegment(xy, segment))
                            {
                                intersection = xy;
                                return true;
                            }
                        }

                        if (m1 == m2)
                            return false;

                        float x = (n2 - n1) / (m1 - m2);
                        xy = new Vector3(x, m1 * x + n1);
                        if (PointOnSegment(xy, segment))
                        {
                            intersection = xy;
                            return true;
                        }

                        return false;
                    }
                }
            }
        }
    }
}
