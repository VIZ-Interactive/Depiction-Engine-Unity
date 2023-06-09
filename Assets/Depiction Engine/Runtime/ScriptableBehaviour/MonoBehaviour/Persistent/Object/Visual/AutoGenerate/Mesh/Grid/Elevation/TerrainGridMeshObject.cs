// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace DepictionEngine
{
    [AddComponentMenu(SceneManager.NAMESPACE + "/Object/Astro/Grid/" + nameof(TerrainGridMeshObject))]
    [CreateComponent(typeof(AssetReference), typeof(AssetReference), typeof(AssetReference))]
    public class TerrainGridMeshObject : ElevationGridMeshObjectBase
    {
        public const string COLORMAP_REFERENCE_DATATYPE = nameof(Texture) + " ColorMap";
        public const string ADDITIONALMAP_REFERENCE_DATATYPE = nameof(Texture) + " AdditionalMap";
        public const string SURFACETYPEMAP_REFERENCE_DATATYPE = nameof(Texture) + " SurfaceTypeMap";

        /// <summary>
        /// The different types of normals that can be generated for terrains. <br/><br/>
        /// <b><see cref="DerivedFromElevation"/>:</b> <br/>
        /// The normal will be derived from neighboring elevation values <br/><br/>
        /// <b><see cref="SurfaceUp"/>:</b> <br/>
        /// The planet up vector will be used as a normal irregardless of the terrain elevation <br/><br/>
        /// <b><see cref="GPU"/>:</b> <br/>
        /// The normals will be calculated in realtime in the Terrain vertex shader <br/><br/>
        /// <b><see cref="UnityCalculateNormals"/>:</b> <br/>
        /// The normals will be calculated by Unity's 'UnityEngine.Mesh.RecalculateNormals' function <br/><br/>
        /// <b><see cref="None"/>:</b> <br/>
        /// Normals will not be used
        /// </summary>
        public enum NormalsType
        {
            DerivedFromElevation,
            SurfaceUp,
            GPU,
            UnityCalculateNormals,
            None
        };

        /// <summary>
        /// The different types of terrain tile geometry that can be generated. <br/><br/>
        /// <b><see cref="Surface"/>:</b> <br/>
        /// Only the surface will be generated <br/><br/>
        /// <b><see cref="Sides"/>:</b> <br/>
        /// Only the sides will be generated <br/><br/>
        /// <b><see cref="SurfaceSides"/>:</b> <br/>
        /// Both the surface and the sides will be generated <br/><br/>
        /// <b><see cref="SurfaceSidesSeparateMesh"/>:</b> <br/>
        /// Both the surface and the sides will be generated but as part of two separate <see cref="DepictionEngine.MeshRendererVisual"/>  <br/><br/>
        /// </summary>
        public enum TerrainGeometryType
        {
            Surface,
            Sides,
            SurfaceSides,
            SurfaceSidesSeparateMesh
        };

        private const float MIN_SUBDIVISION_ZOOM_FACTOR = 1.0f;
        private const float MAX_SUBDIVISION_ZOOM_FACTOR = 3.0f;

        [BeginFoldout("Terrain")]
        [SerializeField, Tooltip("The path of the material's shader from within the Resources directory.")]
        private string _shaderPath;
        [SerializeField, Range(1, 127), Tooltip("The minimum number of subdivisions the tile geometry will have when in spherical mode.")]
        private int _sphericalSubdivision;
        [SerializeField, Range(1, 127), Tooltip("The minimum number of subdivisions the tile geometry will have when in flat mode.")]
        private int _flatSubdivision;
        [SerializeField, Range(MIN_SUBDIVISION_ZOOM_FACTOR, MAX_SUBDIVISION_ZOOM_FACTOR), Tooltip("A factor by which the number of subdivisions will be increased as the zoom level decreases according to the following formula. Zoom level 23 or higher will always have the minimum amount of subdivisions.")]
        private float _subdivisionZoomFactor;
        [SerializeField, Tooltip("A factor by which the geometry will be scaled along the longitudinal and latitudinal axis to overlap with other tiles of a similar zoom level.")]
        private float _overlapFactor;
        [SerializeField, Range(0.0f, 1.0f), Tooltip("A value passed to the shader to determine how much edge overlap should exist between the tile and other higher zoom level tiles covering part or all of its surface.")]
        private float _edgeOverlapThickness;
        [SerializeField, Tooltip("When enabled the sides of the terrain geometry will be capped by extending the edges. Extending the edges can help avoid visible gaps between the tiles when elevation is used.")]
        private TerrainGeometryType _generateTerrainGeometry;
        [SerializeField, Tooltip("When enabled the terrain geometry will be shaped on the GPU using vertex shader. This can be useful to limit the load on the CPU.")]
        private bool _GPUTerrain;
        [SerializeField, Tooltip("The type of normals to generate for the terrain mesh."), EndFoldout]
        private NormalsType _normalsType;

        [SerializeField, HideInInspector]
        private Material _material;

        private Texture _colorMap;
        private Texture _additionalMap;
        private Texture _surfaceTypeMap;

        private int _subdivision;
        private float _subdivisionSize;

        private int _cameraCount;

        private TerrainGridCache _terrainGridCache;

        public override void Recycle()
        {
            base.Recycle();

            RecycleMaterial(_material);

            _subdivision = default;
            _subdivisionSize = default;

            _cameraCount = default;

            _terrainGridCache?.Clear();
        }

        protected override void InitializeSerializedFields(InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            if (initializingContext == InitializationContext.Editor_Duplicate || initializingContext == InitializationContext.Programmatically_Duplicate)
                _material = null;

            InitValue(value => shaderPath = value, GetDefaultShaderPath(), initializingContext);
            InitValue(value => sphericalSubdivision = value, 6, initializingContext);
            InitValue(value => flatSubdivision = value, 6, initializingContext);
            InitValue(value => subdivisionZoomFactor = value, 1.2f, initializingContext);
            InitValue(value => overlapFactor = value, 1.0f, initializingContext);
            InitValue(value => edgeOverlapThickness = value, 0.0f, initializingContext);
            InitValue(value => generateTerrainGeometry = value, GetDefaultGenerateTerrainGeometry(), initializingContext);
            InitValue(value => GPUTerrain = value, true, initializingContext);
            InitValue(value => normalsType = value, NormalsType.GPU, initializingContext);
        }

        protected override void CreateAndInitializeDependencies(InitializationContext initializingContext)
        {
            base.CreateAndInitializeDependencies(initializingContext);

            InitializeReferenceDataType(COLORMAP_REFERENCE_DATATYPE, typeof(AssetReference));
            InitializeReferenceDataType(ADDITIONALMAP_REFERENCE_DATATYPE, typeof(AssetReference));
            InitializeReferenceDataType(SURFACETYPEMAP_REFERENCE_DATATYPE, typeof(AssetReference));
        }

        protected override bool UpdateReferences(bool forceUpdate = false)
        {
            if (base.UpdateReferences(forceUpdate))
            {
                colorMap = GetAssetFromAssetReference<Texture>(colorMapAssetReference);
                additionalMap = GetAssetFromAssetReference<Texture>(additionalMapAssetReference);
                surfaceTypeMap = GetAssetFromAssetReference<Texture>(surfaceTypeMapAssetReference);

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
                if (!callback.Invoke(surfaceTypeMap, surfaceTypeMapAssetReference, false))
                    return false;

                return true;
            }
            return false;
        }

        protected override string GetDefaultShaderPath()
        {
            return base.GetDefaultShaderPath() + "TerrainGrid";
        }

        protected virtual TerrainGeometryType GetDefaultGenerateTerrainGeometry()
        {
            return TerrainGeometryType.SurfaceSides;
        }

        protected override bool UpdateAllDelegates()
        {
            if (base.UpdateAllDelegates())
            {
                terrainGridCache.UpdateAllDelegates(IsDisposing());

                return true;
            }
            return false;
        }

        protected override void InitializeMaterial(MeshRenderer meshRenderer, Material material = null)
        {
            base.InitializeMaterial(meshRenderer, UpdateMaterial(ref _material, shaderPath));
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
        /// The minimum number of subdivisions the tile geometry will have when in spherical mode.
        /// </summary>
        [Json]
        public int sphericalSubdivision
        {
            get => _sphericalSubdivision;
            set => SetValue(nameof(sphericalSubdivision), ValidateSubdivision(value), ref _sphericalSubdivision);
        }

        /// <summary>
        /// The minimum number of subdivisions the tile geometry will have when in flat mode.
        /// </summary>
        [Json]
        public int flatSubdivision
        {
            get => _flatSubdivision;
            set => SetValue(nameof(flatSubdivision), ValidateSubdivision(value), ref _flatSubdivision);
        }

        /// <summary>
        /// A factor by which the number of subdivisions will be increased as the zoom level decreases according to the following formula.
        /// <code>
        /// zoom -= 24;
        /// if (zoom >= 1)
        ///     newSubdivision *= Mathf.Pow(subdivisionZoomFactor, zoom);
        /// </code>
        /// </summary>
        /// <remarks>Zoom level 23 or higher will always have the minimum amount of subdivisions.</remarks>
        [Json]
        public float subdivisionZoomFactor
        {
            get => _subdivisionZoomFactor;
            set => SetValue(nameof(subdivisionZoomFactor), Mathf.Clamp(value, MIN_SUBDIVISION_ZOOM_FACTOR, MAX_SUBDIVISION_ZOOM_FACTOR), ref _subdivisionZoomFactor);
        }

        /// <summary>
        /// A factor by which the geometry will be scaled along the longitudinal and latitudinal axis to overlap with other tiles of a similar zoom level.
        /// </summary>
        [Json]
        public float overlapFactor
        {
            get => _overlapFactor;
            set => SetValue(nameof(overlapFactor), Mathf.Clamp(value, 0.5f, 1.5f), ref _overlapFactor);
        }

        /// <summary>
        /// A value passed to the shader to determine how much edge overlap should exist between the tile and other higher zoom level tiles covering part or all of its surface. 
        /// </summary>
        [Json]
        public float edgeOverlapThickness
        {
            get => _edgeOverlapThickness;
            set
            {
                SetValue(nameof(edgeOverlapThickness), Mathf.Clamp01(value), ref _edgeOverlapThickness, (newValue, oldValue) =>
                {
                    terrainGridCache.Dirty();
                });
            }
        }

        /// <summary>
        /// How deep the tile edges should extend below the ground, in local units. Extending the edges can help avoid gaps between the tiles when elevation is used. Set to zero to deactivate.
        /// </summary>
        [Json]
        public TerrainGeometryType generateTerrainGeometry
        {
            get => _generateTerrainGeometry;
            set => SetValue(nameof(generateTerrainGeometry), value, ref _generateTerrainGeometry);
        }

        /// <summary>
        /// When enabled the terrain geometry will be shaped on the GPU using vertex shader. This can be useful to limit the load on the CPU.
        /// </summary>
        [Json]
        public bool GPUTerrain
        {
            get => _GPUTerrain;
            set => SetValue(nameof(GPUTerrain), value, ref _GPUTerrain);
        }

        /// <summary>
        /// The type of normals to generate for the terrain mesh.
        /// </summary>
        [Json]
        public NormalsType normalsType
        {
            get => _normalsType;
            set => SetValue(nameof(normalsType), value, ref _normalsType);
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

        private void UpdateAdditionalMap()
        {
            additionalMap = additionalMapAssetReference != Disposable.NULL ? additionalMapAssetReference.data as Texture : null;
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

        private AssetReference surfaceTypeMapAssetReference
        {
            get => GetFirstReferenceOfType(SURFACETYPEMAP_REFERENCE_DATATYPE) as AssetReference;
        }

        private void UpdateSurfaceTypeMap()
        {
            surfaceTypeMap = surfaceTypeMapAssetReference != Disposable.NULL ? surfaceTypeMapAssetReference.data as Texture : null;
        }

        public Texture surfaceTypeMap
        {
            get => _surfaceTypeMap;
            private set => SetValue(nameof(surfaceTypeMap), value, ref _surfaceTypeMap);
        }

        private TerrainGridCache terrainGridCache
        {
            get { _terrainGridCache ??= new TerrainGridCache().Initialize(); return _terrainGridCache; }
        }

        /// <summary>
        /// The actual number of subdivisions after calculations.
        /// </summary>
        public int subdivision
        {
            get => _subdivision;
            private set => _subdivision = ValidateSubdivision(value);
        }

        private float subdivisionSize
        {
            get => _subdivisionSize;
            set => _subdivisionSize = value;
        }

        private int cameraCount
        {
            get => _cameraCount;
            set => SetCameraCount(value);
        }

        private int ValidateSubdivision(int value)
        {
            return (int)Mathf.Clamp(value, 1.0f, 127.0f);
        } 

        private bool SetCameraCount(int value)
        {
            if (_cameraCount == value)
                return false;
            _cameraCount = value;
            return true;
        }

        protected override MeshRendererVisual.ColliderType GetColliderType()
        {
            return IsFlat() && elevation == Disposable.NULL ? MeshRendererVisual.ColliderType.Box : base.GetColliderType();
        }

        private float GetSubdivisionSize(int subdivision)
        {
            return 1.0f / subdivision;
        }

        protected override void ColorChanged(Color newValue, Color oldValue)
        {
            base.ColorChanged(newValue, oldValue);

            terrainGridCache.Dirty();
        }

        protected override bool SetPopupT(float value)
        {
            if (base.SetPopupT(value))
            {
                terrainGridCache.Dirty();
                return true;
            }
            return false;
        }

        protected override void UpdateVisualProperties()
        {
            base.UpdateVisualProperties();
            
            float newSubdivision = 1.0f;

            if (IsSpherical())
                newSubdivision = sphericalSubdivision;
            if (IsFlat())
                newSubdivision = flatSubdivision;

            //Zoom level 23 and above should have the minimum number of subdivision and no more
            int zoom = 24 - MathPlus.GetZoomFromGrid2DDimensions(grid2DDimensions);
            if (zoom >= 1)
                newSubdivision *= Mathf.Pow(subdivisionZoomFactor, zoom);

            subdivision = (int)newSubdivision;

            subdivisionSize = GetSubdivisionSize(subdivision);
        }

        protected override bool EnableRecalculateNormals()
        {
            return normalsType == NormalsType.UnityCalculateNormals;
        }

        protected override Func<ProcessorOutput, ProcessorParameters, IEnumerator> GetProcessorFunction()
        {
            return TerrainGridMeshObjectProcessingFunctions.InitPopulateEdgeAndGrid;
        }

        protected override Type GetProcessorParametersType()
        {
            return typeof(TerrainGridMeshObjectParameters);
        }

        protected override void InitializeProcessorParameters(ProcessorParameters parameters)
        {
            base.InitializeProcessorParameters(parameters);

            if (meshRendererVisualDirtyFlags is TerrainGridMeshObjectVisualDirtyFlags)
            {
                TerrainGridMeshObjectVisualDirtyFlags terrainRendererVisualDirtyFlags = meshRendererVisualDirtyFlags as TerrainGridMeshObjectVisualDirtyFlags;

                (parameters as TerrainGridMeshObjectParameters).Init(terrainRendererVisualDirtyFlags.subdivision, terrainRendererVisualDirtyFlags.subdivisionSize, terrainRendererVisualDirtyFlags.overlapFactor, terrainRendererVisualDirtyFlags.generateTerrainGeometry, terrainRendererVisualDirtyFlags.normalsType, terrainRendererVisualDirtyFlags.trianglesDirty, terrainRendererVisualDirtyFlags.uvsDirty, terrainRendererVisualDirtyFlags.verticesNormalsDirty);
            }
        }

        protected override int GetCacheHash(VisualObjectVisualDirtyFlags meshRendererVisualDirtyFlags)
        {
            int hash = base.GetCacheHash(meshRendererVisualDirtyFlags);

            if (IsFlat() && elevation == Disposable.NULL)
            {
                hash = 17;
                hash *= 31 + subdivision.GetHashCode();
                hash *= 31 + overlapFactor.GetHashCode();
                hash *= 31 + generateTerrainGeometry.GetHashCode();
                hash *= 31 + normalsType.GetHashCode();
                hash *= 31 + (grid2DDimensions.x / grid2DDimensions.y).GetHashCode();
            }

            return hash;
        }

        protected override Type GetMeshRendererVisualDirtyFlagType()
        {
            return typeof(TerrainGridMeshObjectVisualDirtyFlags);
        }

        protected override void UpdateMeshRendererDirtyFlags(VisualObjectVisualDirtyFlags meshRendererVisualDirtyFlags)
        {
            base.UpdateMeshRendererDirtyFlags(meshRendererVisualDirtyFlags);

            if (meshRendererVisualDirtyFlags is TerrainGridMeshObjectVisualDirtyFlags)
            {
                TerrainGridMeshObjectVisualDirtyFlags terrainMeshRendererVisualDirtyFlags = meshRendererVisualDirtyFlags as TerrainGridMeshObjectVisualDirtyFlags;

                terrainMeshRendererVisualDirtyFlags.subdivision = subdivision;
                terrainMeshRendererVisualDirtyFlags.subdivisionSize = subdivisionSize;
                terrainMeshRendererVisualDirtyFlags.overlapFactor = overlapFactor;
                terrainMeshRendererVisualDirtyFlags.generateTerrainGeometry = generateTerrainGeometry;
                terrainMeshRendererVisualDirtyFlags.normalsType = normalsType;
            }
        }

        protected override void ApplyCastShadowToMeshRendererVisual(MeshRendererVisual meshRendererVisual, ShadowCastingMode shadowCastingMode)
        {
            base.ApplyCastShadowToMeshRendererVisual(meshRendererVisual, color.a != 1.0f ? ShadowCastingMode.Off : shadowCastingMode);
        }

        protected virtual bool GetEnableGPUTerrain()
        {
            return GPUTerrain;
        }

        protected virtual bool GetEnableGPUNormals()
        {
            return normalsType == NormalsType.GPU;
        }

        protected override void ApplyPropertiesToMaterial(MeshRenderer meshRenderer, Material material, MaterialPropertyBlock materialPropertyBlock, double cameraAtmosphereAltitudeRatio, Camera camera, GeoAstroObject closestGeoAstroObject, Star star)
        {
            base.ApplyPropertiesToMaterial(meshRenderer, material, materialPropertyBlock, cameraAtmosphereAltitudeRatio, camera, closestGeoAstroObject, star);

            SetTextureToMaterial("_SurfaceTypeMap", surfaceTypeMap, Texture2D.whiteTexture, material, materialPropertyBlock);

            SetIntToMaterial("_Subdivision", subdivision, material, materialPropertyBlock);

            int beginSideVertexID = 0;

            if (GetEnableGPUTerrain())
            {
                int vertexCount = subdivision + 1;
                beginSideVertexID = vertexCount * 4;

                if (generateTerrainGeometry == TerrainGeometryType.Surface || generateTerrainGeometry == TerrainGeometryType.SurfaceSides || (generateTerrainGeometry == TerrainGeometryType.SurfaceSidesSeparateMesh && managedMeshRenderers[0] == meshRenderer))
                    beginSideVertexID += (int)Mathf.Pow(vertexCount, 2);
            }

            SetIntToMaterial("_BeginSideVertexID", beginSideVertexID, material, materialPropertyBlock);

            if (GetEnableGPUTerrain())
                material.EnableKeyword("ENABLE_GPU_TERRAIN");
            else
                material.DisableKeyword("ENABLE_GPU_TERRAIN");

            if (GetEnableGPUNormals())
                material.EnableKeyword("ENABLE_GPU_NORMALS");
            else
                material.DisableKeyword("ENABLE_GPU_NORMALS");
        }

        protected override void ApplyAlphaToMaterial(Material material, MaterialPropertyBlock materialPropertyBlock, GeoAstroObject closestGeoAstroObject, Camera camera, float alpha)
        {
            int zoom = MathPlus.GetZoomFromGrid2DDimensions(grid2DDimensions);

            SetFloatToMaterial("_InvertDitherAlpha", zoom % 2 == 0 ? 1 : 0, material, materialPropertyBlock);

            SetFloatToMaterial("_EdgeOverlapThickness", edgeOverlapThickness / 2.0f, material, materialPropertyBlock);

            bool cameraCountChanged = SetCameraCount(instanceManager.camerasCount);
            if (terrainGridCache.Update(parentGeoAstroObject, grid2DDimensions, grid2DIndex) || cameraCountChanged || cameraCount > 1)
            {
                for (int y = 0; y < 2; y++)
                {
                    for (int x = 0; x < 2; x++)
                    {
                        Vector2Int quadrantIndex = new(x, y);
                        float alphaQuadrant = GetTerrainGridMeshObjectAlpha(terrainGridCache, quadrantIndex, Vector2Int.zero, camera);
                        for (int row = -1; row <= 1; row++)
                        {
                            Vector3 alphaRow = Vector3.one * alpha;

                            if (alphaQuadrant != 0.0f)
                            {
                                for (int column = -1; column <= 1; column++)
                                    alphaRow[column + 1] = 1.0f - Mathf.Min(alphaQuadrant, GetTerrainGridMeshObjectAlpha(terrainGridCache, quadrantIndex, new Vector2Int(column, row), camera));
                            }

                            SetVectorToMaterial("_AlphaQuadrant" + ((y * 2) + x + 1) + "Row" + (row + 2), alphaRow, material, materialPropertyBlock);
                        }
                    }
                }
            }
        }

        private float GetTerrainGridMeshObjectAlpha(TerrainGridCache terrainGridCache, Vector2Int quadrantIndex, Vector2Int offset, Camera camera)
        {
            float alpha = GetGrid2DIndexTerrainGridMeshObjectAlpha(terrainGridCache.GetTerrainGridMeshObject(GetIndexFromQuadrantOffset(quadrantIndex, offset)), camera);

            if (offset != Vector2Int.zero)
            {
                if (alpha != 0.0f)
                    alpha = Mathf.Min(alpha, GetGrid2DIndexTerrainGridMeshObjectAlpha(terrainGridCache.GetTerrainGridMeshObject(GetIndexFromQuadrantOffset(quadrantIndex, new Vector2Int(offset.x, 0))), camera));
                if (alpha != 0.0f)
                    alpha = Mathf.Min(alpha, GetGrid2DIndexTerrainGridMeshObjectAlpha(terrainGridCache.GetTerrainGridMeshObject(GetIndexFromQuadrantOffset(quadrantIndex, new Vector2Int(0, offset.y))), camera));
            }

            return alpha;
        }

        private float GetGrid2DIndexTerrainGridMeshObjectAlpha(Grid2DIndexTerrainGridMeshObjects grid2DIndexTerrainGridMeshObject, Camera camera)
        {
            return grid2DIndexTerrainGridMeshObject != Disposable.NULL ? grid2DIndexTerrainGridMeshObject.GetHighestAlpha(camera) : 0.0f;
        }

        private int GetIndexFromQuadrantOffset(Vector2Int quadrantIndex, Vector2Int offset)
        {
            return 5 + (quadrantIndex.y + offset.y) * 4 + (quadrantIndex.x + offset.x);
        }

        protected override void ApplyReflectionTextureToMaterial(MeshRenderer meshRenderer, Material material, MaterialPropertyBlock materialPropertyBlock, double cameraAtmosphereAltitudeRatio, Camera camera, GeoAstroObject closestGeoAstroObject, RTTCamera rttCamera, ScriptableRenderContext? context)
        {
            base.ApplyReflectionTextureToMaterial(meshRenderer, material, materialPropertyBlock, cameraAtmosphereAltitudeRatio, camera, closestGeoAstroObject, rttCamera, context);

            if (closestGeoAstroObject != Disposable.NULL)
            {
                closestGeoAstroObject.IterateOverEffects<TerrainSurfaceReflectionEffect>((effect) =>
                {
                    float terrainSurfaceReflectionAlpha = effect.isActiveAndEnabled && context.HasValue && rttCamera != Disposable.NULL ? effect.GetAlpha() * (float)Math.Max(0.0d, cameraAtmosphereAltitudeRatio * 10.0d - 9.0d) : 0.0f;
                    SetFloatToMaterial("_TerrainSurfaceReflectionAlpha", terrainSurfaceReflectionAlpha, material, materialPropertyBlock);
                    SetTextureToMaterial("_TerrainSurfaceReflectionMap", terrainSurfaceReflectionAlpha != 0.0f ? effect.GetTexture(rttCamera, camera, context.Value) : Texture2D.blackTexture, material, materialPropertyBlock);
                    return false;
                });
            }
        }

        public override bool OnDispose(DisposeContext disposeContext)
        {
            if (base.OnDispose(disposeContext))
            {
                if (disposeContext != DisposeContext.Programmatically_Pool)
                    DisposeManager.Dispose(_material, disposeContext);

                return true;
            }
            return false;
        }

        protected class TerrainGridMeshObjectParameters : ElevationGridMeshObjectParameters
        {
            private int _subdivision;
            private float _subdivisionSize;
            private float _overlapFactor;
            private TerrainGeometryType _terrainGeometryType;
            private NormalsType _normalsType;

            private bool _trianglesDirty;
            private bool _uvsDirty;
            private bool _verticesDirty;
            private bool _normalsDirty;

            public override void Recycle()
            {
                base.Recycle();

                _subdivision = default;
                _subdivisionSize = default;
                _overlapFactor = default;
                _terrainGeometryType = default;

                _trianglesDirty = default;
                _uvsDirty = default;
                _verticesDirty = default;
                _normalsDirty = default;
            }

            public TerrainGridMeshObjectParameters Init(int subdivision, float subdivisionSize, float overlapFactor, TerrainGeometryType terrainGeometryType, NormalsType normalsType, bool trianglesDirty, bool uvsDirty, bool verticesNormalsDirty)
            {
                _subdivision = subdivision;
                _subdivisionSize = subdivisionSize;
                _overlapFactor = overlapFactor;
                _terrainGeometryType = terrainGeometryType;
                _normalsType = normalsType;

                _trianglesDirty = trianglesDirty;
                _uvsDirty = uvsDirty;
                _verticesDirty = verticesNormalsDirty;
                _normalsDirty = verticesNormalsDirty;

                return this;
            }

            public int GetSubdivision()
            {
                return _subdivision;
            }

            public float GetSubdivisionSize()
            {
                return _subdivisionSize;
            }

            public float GetOverlapFactor()
            {
                return _overlapFactor;
            }

            public bool GetTrianglesDirty()
            {
                return _trianglesDirty;
            }

            public bool GetUVsDirty()
            {
                return _uvsDirty;
            }

            public bool GetVerticesDirty()
            {
                return _verticesDirty;
            }

            public bool GetNormalsDirty()
            {
                return _normalsDirty;
            }

            public TerrainGeometryType terrainGeometryType
            {
                get => _terrainGeometryType;
            }

            public NormalsType normalsType
            {
                get => _normalsType;
            }

            public bool trianglesDirty
            {
                get => _trianglesDirty;
            }

            public bool uvsDirty
            {
                get => _uvsDirty;
            }

            public bool verticesDirty
            {
                get => _verticesDirty;
            }

            public bool normalsDirty
            {
                get => _normalsDirty;
            }
        }

        private class TerrainGridCache
        {
            private const int GRID_SIZE = 16;

            Vector2Int _grid2DDimensions;
            Vector2Int _grid2DIndex;

            private GeoAstroObject _parentGeoAstroObject;

            private Vector2Int _upperLeft;
            private Vector2Int _bottomRight;

            private Grid2DIndexTerrainGridMeshObjects[] _grid2DIndexTerrainGridMeshObjects;
            private int _terrainGridMeshObjectsCount;

            private bool _changed;

            private bool _dirty;

            public TerrainGridCache Initialize()
            {
                Dirty();

                _grid2DIndexTerrainGridMeshObjects ??= new Grid2DIndexTerrainGridMeshObjects[GRID_SIZE];

                return this;
            }

            public void UpdateAllDelegates(bool isDisposing)
            {
                foreach (Grid2DIndexTerrainGridMeshObjects grid2DIndexTerrainGridMeshObject in _grid2DIndexTerrainGridMeshObjects)
                {
                    RemoveGrid2DIndexTerrainGridMeshObjectDelegate(grid2DIndexTerrainGridMeshObject);
                    if (!isDisposing)
                        AddGrid2DIndexTerrainGridMeshObjectDelegate(grid2DIndexTerrainGridMeshObject);
                }

                RemoveParentGeoAstroObjectDelegate(_parentGeoAstroObject);
                if (!isDisposing)
                    AddParentGeoAstroObjectDelegate(_parentGeoAstroObject);
            }

            private void RemoveGrid2DIndexTerrainGridMeshObjectDelegate(Grid2DIndexTerrainGridMeshObjects grid2DIndexTerrainGridMeshObject)
            {
                if (grid2DIndexTerrainGridMeshObject is not null)
                {
                    grid2DIndexTerrainGridMeshObject.TerrainGridMeshObjectPropertyAssignedEvent -= TerrainGridMeshObjectPropertyAssignedHandler;
                    grid2DIndexTerrainGridMeshObject.TerrainGridMeshObjectAddedEvent -= TerrainGridMeshObjectAddedHandler;
                    grid2DIndexTerrainGridMeshObject.TerrainGridMeshObjectRemovedEvent -= TerrainGridMeshObjectRemovedHandler;
                    grid2DIndexTerrainGridMeshObject.TerrainGridMeshObjectChildAddedEvent -= TerrainGridMeshObjectChildAddedHandler;
                    grid2DIndexTerrainGridMeshObject.TerrainGridMeshObjectChildRemovedEvent -= TerrainGridMeshObjectChildRemovedHandler;
                }
            }

            private void AddGrid2DIndexTerrainGridMeshObjectDelegate(Grid2DIndexTerrainGridMeshObjects grid2DIndexTerrainGridMeshObject)
            {
                if (grid2DIndexTerrainGridMeshObject != Disposable.NULL)
                {
                    grid2DIndexTerrainGridMeshObject.TerrainGridMeshObjectPropertyAssignedEvent += TerrainGridMeshObjectPropertyAssignedHandler;
                    grid2DIndexTerrainGridMeshObject.TerrainGridMeshObjectAddedEvent += TerrainGridMeshObjectAddedHandler;
                    grid2DIndexTerrainGridMeshObject.TerrainGridMeshObjectRemovedEvent += TerrainGridMeshObjectRemovedHandler;
                    grid2DIndexTerrainGridMeshObject.TerrainGridMeshObjectChildAddedEvent += TerrainGridMeshObjectChildAddedHandler;
                    grid2DIndexTerrainGridMeshObject.TerrainGridMeshObjectChildRemovedEvent += TerrainGridMeshObjectChildRemovedHandler;
                }
            }

            private void TerrainGridMeshObjectPropertyAssignedHandler(Grid2DIndexTerrainGridMeshObjects grid2DIndexTerrainGridMeshObject, string name, object newValue, object oldValue)
            {
                if (name == nameof(name) || name == nameof(popupT))
                    _changed = true;
            }

            private void TerrainGridMeshObjectAddedHandler(TerrainGridMeshObject terrainGridMeshObject)
            {
                _changed = true;
            }

            private void TerrainGridMeshObjectRemovedHandler(TerrainGridMeshObject terrainGridMeshObject)
            {
                _changed = true;
            }

            private void TerrainGridMeshObjectChildAddedHandler(Grid2DIndexTerrainGridMeshObjects grid2DIndexTerrainGridMeshObjects, Object objectBase, PropertyMonoBehaviour child)
            {
                if (child is MeshRendererVisual)
                    _changed = true;
            }

            private void TerrainGridMeshObjectChildRemovedHandler(Grid2DIndexTerrainGridMeshObjects grid2DIndexTerrainGridMeshObjects, Object objectBase, PropertyMonoBehaviour child)
            {
                if (child is MeshRendererVisual)
                    _changed = true;
            }

            private void RemoveParentGeoAstroObjectDelegate(GeoAstroObject parentGeoAstroObject)
            {
                if (parentGeoAstroObject is not null)
                {
                    parentGeoAstroObject.TerrainGridMeshObjectAddedEvent -= ParentGeoAstroObjectTerrainGridMeshAddedHandler;
                    parentGeoAstroObject.TerrainGridMeshObjectRemovedEvent -= ParentGeoAstroObjectTerrainGridMeshRemovedHandler;
                }
            }

            private void AddParentGeoAstroObjectDelegate(GeoAstroObject parentGeoAstroObject)
            {
                if (parentGeoAstroObject != Disposable.NULL)
                {
                    parentGeoAstroObject.TerrainGridMeshObjectAddedEvent += ParentGeoAstroObjectTerrainGridMeshAddedHandler;
                    parentGeoAstroObject.TerrainGridMeshObjectRemovedEvent += ParentGeoAstroObjectTerrainGridMeshRemovedHandler;
                }
            }

            private void ParentGeoAstroObjectTerrainGridMeshAddedHandler(Grid2DIndexTerrainGridMeshObjects grid2DIndexTerrainGridMeshObject, Vector2Int grid2DDimensions, Vector2Int grid2DIndex)
            {
                if (_grid2DIndexTerrainGridMeshObjects != null && _terrainGridMeshObjectsCount != 16)
                {
                    if (IsWithinCache(grid2DDimensions, grid2DIndex))
                        AddTerrain(grid2DIndexTerrainGridMeshObject, GetIndexFromGrid2DIndex(grid2DIndex));
                }
            }

            private void ParentGeoAstroObjectTerrainGridMeshRemovedHandler(Grid2DIndexTerrainGridMeshObjects grid2DIndexTerrainGridMeshObject, Vector2Int grid2DDimensions, Vector2Int grid2DIndex)
            {
                if (_grid2DIndexTerrainGridMeshObjects != null)
                {
                    if (IsWithinCache(grid2DDimensions, grid2DIndex))
                        RemoveTerrain(grid2DIndexTerrainGridMeshObject, GetIndexFromGrid2DIndex(grid2DIndex));
                }
            }

            private bool IsWithinCache(Vector2Int grid2DDimensions, Vector2Int grid2DIndex)
            {
                return grid2DDimensions == _grid2DDimensions && (grid2DIndex.x >= _upperLeft.x && grid2DIndex.x <= _bottomRight.x) && (grid2DIndex.y >= _upperLeft.y && grid2DIndex.y <= _bottomRight.y);
            }

            private bool SetParentGeoAstroObject(GeoAstroObject parentGeoAstroObject)
            {
                if (_parentGeoAstroObject == parentGeoAstroObject)
                    return false;

                RemoveParentGeoAstroObjectDelegate(_parentGeoAstroObject);
                    
                _parentGeoAstroObject = parentGeoAstroObject;
                    
                AddParentGeoAstroObjectDelegate(_parentGeoAstroObject);

                return true;
            }

            private bool SetGrid2DDimensionsIndex(Vector2Int grid2DDimensions, Vector2Int grid2DIndex)
            {
                Vector2Int zoomPlusOneGrid2DDimensions = grid2DDimensions * new Vector2Int(2, 2);
                if (_grid2DDimensions == zoomPlusOneGrid2DDimensions && _grid2DIndex == grid2DIndex)
                    return false;

                _grid2DDimensions = zoomPlusOneGrid2DDimensions;
                _grid2DIndex = grid2DIndex;

                _upperLeft = new Vector2Int((int)((float)grid2DIndex.x / (float)grid2DDimensions.x * (float)zoomPlusOneGrid2DDimensions.x) - 1, (int)((float)grid2DIndex.y / (float)grid2DDimensions.y * (float)zoomPlusOneGrid2DDimensions.y) - 1);
                _bottomRight = _upperLeft + new Vector2Int(3, 3);

                return true;
            }

            public void Dirty()
            {
                _dirty = true;
            }

            public bool Update(GeoAstroObject parentGeoAstroObject, Vector2Int grid2DDimensions, Vector2Int grid2DIndex)
            {
                bool findExistingTerrainGridMeshObject = false;

                if (SetParentGeoAstroObject(parentGeoAstroObject))
                    findExistingTerrainGridMeshObject = true;

                if (SetGrid2DDimensionsIndex(grid2DDimensions, grid2DIndex))
                    findExistingTerrainGridMeshObject = true;

                bool changed = false;

                if (_dirty || findExistingTerrainGridMeshObject)
                {
                    _dirty = false;
                    changed = true;

                    if (_parentGeoAstroObject != Disposable.NULL)
                    {
                        int zoom = MathPlus.GetZoomFromGrid2DDimensions(_grid2DDimensions);
                        for (int y = 0; y < 4; y++)
                        {
                            for (int x = 0; x < 4; x++)
                            {
                                Vector2Int relativeGrid2DIndex = new(x, y);
                                AddTerrain(_parentGeoAstroObject.GetGrid2DIndexTerrainGridMeshObject(zoom, _upperLeft + relativeGrid2DIndex), GetIndexFromRelativeGrid2DIndex(relativeGrid2DIndex));
                            }
                        }
                    }
                    else
                        RemoveAllTerrain();
                }

                if (_changed)
                {
                    changed = true;
                    _changed = false;
                }

                return changed;
            }

            private void AddTerrain(Grid2DIndexTerrainGridMeshObjects terrainGridMeshObject, int index)
            {
                if (SetTerrainGridMeshObject(index, terrainGridMeshObject))
                {
                    AddGrid2DIndexTerrainGridMeshObjectDelegate(terrainGridMeshObject);
                    _terrainGridMeshObjectsCount++;
                    _changed = true;
                }
            }

            private void RemoveTerrain(Grid2DIndexTerrainGridMeshObjects terrainGridMeshObject, int index)
            {
                if (SetTerrainGridMeshObject(index, null))
                {
                    RemoveGrid2DIndexTerrainGridMeshObjectDelegate(terrainGridMeshObject);
                    _terrainGridMeshObjectsCount--;
                    _changed = true;
                }
            }

            private void RemoveAllTerrain()
            {
                if (_grid2DIndexTerrainGridMeshObjects != null)
                {
                    for (int i = 0; i < GRID_SIZE; i++)
                        RemoveTerrain(_grid2DIndexTerrainGridMeshObjects[i], i);
                }
            }

            public bool SetTerrainGridMeshObject(int index, Grid2DIndexTerrainGridMeshObjects terrainGridMeshObject)
            {
                if (!Object.ReferenceEquals(_grid2DIndexTerrainGridMeshObjects[index], terrainGridMeshObject))
                {
                    _grid2DIndexTerrainGridMeshObjects[index] = terrainGridMeshObject;
                    return true;
                }
                return false;
            }

            public Grid2DIndexTerrainGridMeshObjects GetTerrainGridMeshObject(int index)
            {
                return _grid2DIndexTerrainGridMeshObjects?[index];
            }

            private int GetIndexFromGrid2DIndex(Vector2Int grid2DIndex)
            {
                Vector2Int relativeGrid2DIndex = grid2DIndex - _upperLeft;
                return GetIndexFromRelativeGrid2DIndex(relativeGrid2DIndex);
            }

            private int GetIndexFromRelativeGrid2DIndex(Vector2Int relativeGrid2DIndex)
            {
                return relativeGrid2DIndex.y * 4 + relativeGrid2DIndex.x;
            }

            public void Clear()
            {
                _grid2DDimensions = Vector2Int.zero;
                _upperLeft = Vector2Int.zero;
                _bottomRight = Vector2Int.zero;
                SetParentGeoAstroObject(null);
                RemoveAllTerrain();
                _changed = false;
                _dirty = false;
            }
        }

        protected class TerrainGridMeshObjectProcessingFunctions : ProcessingFunctions
        {
            private const int SIDE_DEPTH_MULTIPLIER = 1;
            private static int[] _sides = new int[] { 0, 2, 4, 6, 1, 3, 5, 7 };

            public static IEnumerator InitPopulateEdgeAndGrid(ProcessorOutput data, ProcessorParameters parameters)
            {
                foreach (object enumeration in InitPopulateEdgeAndGrid(data as MeshObjectProcessorOutput, parameters as TerrainGridMeshObjectParameters))
                    yield return enumeration;
            }

            protected static IEnumerable InitPopulateEdgeAndGrid(MeshObjectProcessorOutput meshObjectProcessorOutput, TerrainGridMeshObjectParameters parameters)
            {
                TerrainGeometryType terrainGeometryType = parameters.terrainGeometryType;

                int subdivision = parameters.GetSubdivision();
                int vertexCount = subdivision + 1;

                bool verticesDirty = parameters.GetVerticesDirty();
                bool normalsDirty = parameters.GetNormalsDirty();
                bool trianglesDirty = parameters.GetTrianglesDirty();
                bool uvsDirty = parameters.GetUVsDirty();

                int grid2DVerticesNormalsCount = terrainGeometryType != TerrainGeometryType.Sides ? GetVerticesNormalsCount(vertexCount) : 0;
                int grid2DTrianglesCount = terrainGeometryType != TerrainGeometryType.Sides ? GetTrianglesCount(subdivision) : 0;
                int grid2DUvsCount = terrainGeometryType != TerrainGeometryType.Sides ? GetUVsCount(vertexCount) : 0;

                int sidesVerticesCount = -1;
                int verticesCount = -1;
                if (verticesDirty)
                {
                    sidesVerticesCount = terrainGeometryType == TerrainGeometryType.Sides || terrainGeometryType == TerrainGeometryType.SurfaceSides || terrainGeometryType == TerrainGeometryType.SurfaceSidesSeparateMesh ? GetEdgeVerticesNormalsCount(vertexCount) : 0;
                    verticesCount = grid2DVerticesNormalsCount;
                    if (terrainGeometryType == TerrainGeometryType.SurfaceSides)
                        verticesCount += sidesVerticesCount;
                }

                int sidesNormalsCount = -1;
                int normalsCount = -1;
                if (normalsDirty)
                {
                    if (parameters.normalsType == NormalsType.SurfaceUp || parameters.normalsType == NormalsType.DerivedFromElevation)
                    {
                        sidesNormalsCount = terrainGeometryType == TerrainGeometryType.Sides || terrainGeometryType == TerrainGeometryType.SurfaceSides || terrainGeometryType == TerrainGeometryType.SurfaceSidesSeparateMesh ? GetEdgeVerticesNormalsCount(vertexCount) : 0;
                        normalsCount = grid2DVerticesNormalsCount;
                        if (terrainGeometryType == TerrainGeometryType.SurfaceSides)
                            normalsCount += sidesNormalsCount;
                    }
                    else
                    {
                        sidesNormalsCount = 0;
                        normalsCount = 0;
                    }
                }

                int sidesTrianglesCount = -1;
                int trianglesCount = -1;
                if (trianglesDirty)
                {
                    sidesTrianglesCount = terrainGeometryType == TerrainGeometryType.Sides || terrainGeometryType == TerrainGeometryType.SurfaceSides || terrainGeometryType == TerrainGeometryType.SurfaceSidesSeparateMesh ? GetEdgeTrianglesCount(subdivision) : 0;
                    trianglesCount = grid2DTrianglesCount;
                    if (terrainGeometryType == TerrainGeometryType.SurfaceSides)
                        trianglesCount += sidesTrianglesCount;
                }

                int sidesUvsCount = -1;
                int uvsCount = -1;
                if (uvsDirty)
                {
                    sidesUvsCount = terrainGeometryType == TerrainGeometryType.Sides || terrainGeometryType == TerrainGeometryType.SurfaceSides || terrainGeometryType == TerrainGeometryType.SurfaceSidesSeparateMesh ? GetEdgeUVsCount(vertexCount) : 0;
                    uvsCount = grid2DUvsCount;
                    if (terrainGeometryType == TerrainGeometryType.SurfaceSides)
                        uvsCount += sidesUvsCount;
                }

                MeshRendererVisualModifier meshRendererVisualModifier = null;

                if (terrainGeometryType != TerrainGeometryType.Sides)
                {
                    meshRendererVisualModifier = CreateMeshRendererVisualModifier(verticesCount, normalsCount, trianglesCount, uvsCount);
                    meshObjectProcessorOutput.AddMeshRendererVisualModifier(meshRendererVisualModifier);

                    foreach (object enumeration in PopulateGrid(meshRendererVisualModifier.meshModifier, parameters))
                        yield return enumeration;
                }

                if (terrainGeometryType == TerrainGeometryType.Sides || terrainGeometryType == TerrainGeometryType.SurfaceSides || terrainGeometryType == TerrainGeometryType.SurfaceSidesSeparateMesh)
                {
                    if (meshRendererVisualModifier == null || terrainGeometryType == TerrainGeometryType.SurfaceSidesSeparateMesh)
                    {
                        meshRendererVisualModifier = CreateMeshRendererVisualModifier(sidesVerticesCount, sidesNormalsCount, sidesTrianglesCount, sidesUvsCount);
                        meshRendererVisualModifier.SetTypes(typeof(TerrainEdgeMeshRendererVisualNoCollider), typeof(TerrainEdgeMeshRendererVisualBoxCollider), typeof(TerrainEdgeMeshRendererVisualMeshCollider));
                        meshObjectProcessorOutput.AddMeshRendererVisualModifier(meshRendererVisualModifier);

                        if (terrainGeometryType == TerrainGeometryType.SurfaceSidesSeparateMesh)
                            grid2DVerticesNormalsCount = grid2DTrianglesCount = grid2DUvsCount = 0;
                    }

                    foreach (object enumeration in PopulateEdge(meshRendererVisualModifier.meshModifier, parameters, grid2DVerticesNormalsCount, grid2DTrianglesCount, grid2DUvsCount))
                        yield return enumeration;
                }
            }

            private static MeshRendererVisualModifier CreateMeshRendererVisualModifier(int verticesCount = -1, int normalsCount = -1, int trianglesCount = -1, int uvsCount = -1, int colorsCount = -1)
            {
                MeshRendererVisualModifier meshRendererVisualModifier = MeshRendererVisual.CreateMeshRendererVisualModifier();
                meshRendererVisualModifier.CreateMeshModifier<MeshModifier>().Init(verticesCount, normalsCount, trianglesCount, uvsCount);
                return meshRendererVisualModifier;
            }

            public static IEnumerable PopulateGrid(MeshModifier meshModifier, TerrainGridMeshObjectParameters parameters)
            {
                bool verticesDirty = parameters.GetVerticesDirty();
                bool normalsDirty = parameters.GetNormalsDirty() && (parameters.normalsType == NormalsType.SurfaceUp || parameters.normalsType == NormalsType.DerivedFromElevation);
                bool trianglesDirty = parameters.GetTrianglesDirty();
                bool uvsDirty = parameters.GetUVsDirty();

                if (verticesDirty || normalsDirty || trianglesDirty || uvsDirty)
                {
                    int subdivision = parameters.GetSubdivision();
                    float subdivisionSize = parameters.GetSubdivisionSize();
                    int vertexCount = subdivision + 1;

                    if (verticesDirty)
                    {
                        int startIndex = 0;
                        for (int y = 0; y < vertexCount; y++)
                        {
                            for (int x = 0; x < vertexCount; x++)
                                SetVertices(parameters, meshModifier, startIndex + y * vertexCount + x, x * subdivisionSize, y * subdivisionSize, parameters.GetOverlapFactor(), 0.0f);

                            parameters.cancellationTokenSource?.ThrowIfCancellationRequested();
                        }
                    }

                    if (trianglesDirty)
                    {
                        int startIndex = 0;
                        int i = 0;
                        for (int y = 0; y < subdivision; y++)
                        {
                            for (int x = 0; x < subdivision; x++)
                            {
                                int bottomLeft = startIndex + (y + 1) * vertexCount + x;
                                int topLeft = startIndex + (y * vertexCount) + x;
                                int topRight = topLeft + 1;
                                int bottomRight = bottomLeft + 1;

                                bool diagonalOrientation = GetDiagonalOrientation(x, y);

                                if (diagonalOrientation)
                                {
                                    meshModifier.triangles[i] = bottomLeft;
                                    meshModifier.triangles[i + 1] = topLeft;
                                    meshModifier.triangles[i + 2] = topRight;
                                    meshModifier.triangles[i + 3] = bottomLeft;
                                    meshModifier.triangles[i + 4] = topRight;
                                    meshModifier.triangles[i + 5] = bottomRight;
                                }
                                else
                                {
                                    meshModifier.triangles[i] = bottomLeft;
                                    meshModifier.triangles[i + 1] = topLeft;
                                    meshModifier.triangles[i + 2] = bottomRight;
                                    meshModifier.triangles[i + 3] = bottomRight;
                                    meshModifier.triangles[i + 4] = topLeft;
                                    meshModifier.triangles[i + 5] = topRight;
                                }

                                i += 6;
                            }

                            parameters.cancellationTokenSource?.ThrowIfCancellationRequested();
                        }

                        if (parameters.flipTriangles)
                            meshModifier.FlipTriangles();
                    }

                    if (normalsDirty)
                    {
                        int startIndex = 0;
                        for (int y = 0; y < vertexCount; y++)
                        {
                            for (int x = 0; x < vertexCount; x++)
                                SetNormals(parameters, meshModifier, startIndex + y * vertexCount + x, x * subdivisionSize, y * subdivisionSize);

                            parameters.cancellationTokenSource?.ThrowIfCancellationRequested();
                        }
                    }

                    if (uvsDirty)
                    {
                        int startIndex = 0;
                        for (int y = 0; y < vertexCount; y++)
                        {
                            for (int x = 0; x < vertexCount; x++)
                                meshModifier.uvs[startIndex + y * vertexCount + x] = new Vector2((float)(subdivisionSize * x), (float)(1.0f - (subdivisionSize * y)));

                            parameters.cancellationTokenSource?.ThrowIfCancellationRequested();
                        }
                    }

                    meshModifier.CalculateBoundsFromMinMax();
                }

                yield break;
            }

            protected static IEnumerable PopulateEdge(MeshModifier meshModifier, TerrainGridMeshObjectParameters parameters, int verticesNormalsStartIndex = 0, int trianglesStartIndex = 0, int uvsStartIndex = 0)
            {
                bool verticesDirty = parameters.GetVerticesDirty();
                bool normalsDirty = parameters.GetNormalsDirty() && (parameters.normalsType == NormalsType.SurfaceUp || parameters.normalsType == NormalsType.DerivedFromElevation);
                bool trianglesDirty = parameters.GetTrianglesDirty();
                bool uvsDirty = parameters.GetUVsDirty();

                if (verticesDirty || normalsDirty || trianglesDirty || uvsDirty)
                {
                    int subdivision = parameters.GetSubdivision();
                    float subdivisionSize = parameters.GetSubdivisionSize();
                    int vertexCount = subdivision + 1;

                    if (verticesDirty)
                    {
                        int startIndex = verticesNormalsStartIndex;
                        int sideCount = 0;
                        foreach (int side in _sides)
                        {
                            double sideAltitudeOffset = side % 2 == 0 ? 0.0f : Mathf.Clamp01(subdivisionSize * SIDE_DEPTH_MULTIPLIER);

                            for (int index = 0; index < vertexCount; index++)
                                SetVertices(parameters, meshModifier, startIndex + sideCount * vertexCount + index, GetEdgeX(side, index, vertexCount) * subdivisionSize, GetEdgeY(side, index, vertexCount) * subdivisionSize, parameters.GetOverlapFactor(), -sideAltitudeOffset);

                            sideCount++;

                            parameters.cancellationTokenSource?.ThrowIfCancellationRequested();
                        }
                    }

                    if (trianglesDirty)
                    {
                        int startIndex = verticesNormalsStartIndex;
                        int i = trianglesStartIndex;
                        for (int side = 0; side < 4; side++)
                        {
                            for (int index = 0; index < subdivision; index++)
                            {
                                int bottomLeft = startIndex + (side + 4) * vertexCount + index;
                                int topLeft = startIndex + side * vertexCount + index;
                                int topRight = topLeft + 1;
                                int bottomRight = bottomLeft + 1;

                                meshModifier.triangles[i] = bottomLeft;
                                meshModifier.triangles[i + 1] = topLeft;
                                meshModifier.triangles[i + 2] = topRight;
                                meshModifier.triangles[i + 3] = bottomLeft;
                                meshModifier.triangles[i + 4] = topRight;
                                meshModifier.triangles[i + 5] = bottomRight;

                                i += 6;
                            }

                            parameters.cancellationTokenSource?.ThrowIfCancellationRequested();
                        }
                    }

                    if (normalsDirty)
                    {
                        int startIndex = verticesNormalsStartIndex;
                        int sideCount = 0;
                        foreach (int side in _sides)
                        {
                            for (int index = 0; index < vertexCount; index++)
                            {
                                Vector2Int normalizedCoordinate = GetNormalizedCoordinate(side, index);

                                SetNormals(parameters, meshModifier, startIndex + sideCount * vertexCount + index, Mathf.Clamp01(subdivisionSize * normalizedCoordinate.x), Mathf.Clamp01(subdivisionSize * normalizedCoordinate.y));
                            }

                            sideCount++;

                            parameters.cancellationTokenSource?.ThrowIfCancellationRequested();
                        }
                    }

                    if (uvsDirty)
                    {
                        int startIndex = uvsStartIndex;
                        int sideCount = 0;
                        foreach (int side in _sides)
                        {
                            for (int index = 0; index < vertexCount; index++)
                            {
                                Vector2Int normalizedCoordinate = GetNormalizedCoordinate(side, index);

                                meshModifier.uvs[startIndex + sideCount * vertexCount + index] = new Vector2(Mathf.Clamp01(subdivisionSize * normalizedCoordinate.x), Mathf.Clamp01(1.0f - (subdivisionSize * normalizedCoordinate.y)));
                            }

                            sideCount++;

                            parameters.cancellationTokenSource?.ThrowIfCancellationRequested();
                        }
                    }

                    Vector2Int GetNormalizedCoordinate(int side, int index)
                    {
                        Vector2Int normalizedCoordinate = new Vector2Int(GetEdgeX(side, index, vertexCount), GetEdgeY(side, index, vertexCount));

                        bool edgeBottom = side % 2 != 0;
                        if (edgeBottom)
                        {
                            if (side == 0 || side == 1)//Bottom
                                normalizedCoordinate.y -= SIDE_DEPTH_MULTIPLIER;
                            if (side == 2 || side == 3)//Left
                                normalizedCoordinate.x += SIDE_DEPTH_MULTIPLIER;
                            if (side == 4 || side == 5)//Top
                                normalizedCoordinate.y += SIDE_DEPTH_MULTIPLIER;
                            if (side == 6 || side == 7)//Right
                                normalizedCoordinate.x -= SIDE_DEPTH_MULTIPLIER;
                        }

                        return normalizedCoordinate;
                    }

                    meshModifier.CalculateBoundsFromMinMax();
                }
              
                yield break;
            }

            public static int GetVerticesNormalsCount(int vertexCount)
            {
                return vertexCount * vertexCount;
            }

            public static int GetTrianglesCount(int subdivision)
            {
                return 6 * subdivision * subdivision;
            }

            public static int GetUVsCount(int vertexCount)
            {
                return vertexCount * vertexCount;
            }

            public static int GetEdgeVerticesNormalsCount(int vertexCount)
            {
                return vertexCount * 8;
            }

            public static int GetEdgeTrianglesCount(int subdivision)
            {
                return 6 * subdivision * 4;
            }

            public static int GetEdgeUVsCount(int vertexCount)
            {
                return vertexCount * 8;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static bool GetDiagonalOrientation(int x, int y)
            {
                return (x + (y % 2 == 0 ? 1 : 0)) % 2 == 0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static int GetEdgeX(int side, int index, int vertexCount)
            {
                if (side == 0 || side == 1)//Bottom
                    return index;
                if (side == 2 || side == 3)//Left
                    return 0;
                if (side == 4 || side == 5)//Top
                    return vertexCount - index - 1;
                if (side == 6 || side == 7)//Right
                    return vertexCount - 1;
                return 0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static int GetEdgeY(int side, int index, int vertexCount)
            {
                if (side == 0 || side == 1)//Bottom
                    return vertexCount - 1;
                if (side == 2 || side == 3)//Left
                    return index;
                if (side == 4 || side == 5)//Top
                    return 0;
                if (side == 6 || side == 7)//Right
                    return vertexCount - index - 1;
                return 0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected static GeoCoordinate3Double GetGeoCoordinate(TerrainGridMeshObjectParameters parameters, float normalizedX, float normalizedY)
            {
                GeoCoordinate3Double geoCoordinate = MathPlus.GetGeoCoordinate3FromIndex(new Vector2Double(parameters.grid2DIndex.x + normalizedX, parameters.grid2DIndex.y + normalizedY), parameters.grid2DDimensions);

                if (parameters.GetElevation(new Vector2(normalizedX, 1.0f - normalizedY), out float elevation))
                    geoCoordinate.altitude = elevation;

                return geoCoordinate;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void SetVertices(TerrainGridMeshObjectParameters parameters, MeshModifier meshModifier, int bufferIndex, float normalizedX, float normalizedY, float overlapFactor, double altitudeOffset)
            {
                GeoCoordinate3Double geoCoordinate = GetGeoCoordinate(parameters, normalizedX, normalizedY);

                Vector3 vertex = parameters.TransformGeoCoordinateToVector(geoCoordinate.latitude, geoCoordinate.longitude, geoCoordinate.altitude + altitudeOffset) * overlapFactor;
                meshModifier.vertices[bufferIndex] = vertex;
                meshModifier.UpdateMinMaxBounds(vertex);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void SetNormals(TerrainGridMeshObjectParameters parameters, MeshModifier meshModifier, int bufferIndex, float normalizedX, float normalizedY)
            {
                GeoCoordinate3Double geoCoordinate = GetGeoCoordinate(parameters, normalizedX, normalizedY);

                meshModifier.normals[bufferIndex] = parameters.GetUpVector(geoCoordinate) * GetNormal(parameters, normalizedX, normalizedY, 0.05f);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected static Vector3Double GetNormal(TerrainGridMeshObjectParameters parameters, float normalizedX, float normalizedY, float normalSamplingDistance)
            {
                Vector3Double normal = Vector3Double.up;

                if (parameters.normalsType == NormalsType.DerivedFromElevation && parameters.elevation != Disposable.NULL)
                {
                    normalizedY = 1.0f - normalizedY;

                    parameters.GetElevation(new Vector2(normalizedX, normalizedY + normalSamplingDistance), out float upElevation);
                    parameters.GetElevation(new Vector2(normalizedX, normalizedY - normalSamplingDistance), out float downElevation);
                    parameters.GetElevation(new Vector2(normalizedX + normalSamplingDistance, normalizedY), out float rightElevation);
                    parameters.GetElevation(new Vector2(normalizedX - normalSamplingDistance, normalizedY), out float leftElevation);

                    normal = Vector3Double.Cross(
                        new Vector3Double(0.0d, upElevation - downElevation, normalSamplingDistance * 2.0d).normalized,
                        new Vector3Double(normalSamplingDistance * 2.0d, rightElevation - leftElevation, 0.0d).normalized);
                }

                return normal;
            }
        }
    }
}
