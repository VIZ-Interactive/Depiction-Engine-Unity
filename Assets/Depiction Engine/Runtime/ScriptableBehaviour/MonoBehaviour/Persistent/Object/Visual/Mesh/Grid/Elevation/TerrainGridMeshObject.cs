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
        /// <summary>
        /// The different types of normals that can be generated for terrains. <br/><br/>
        /// <b><see cref="DerivedFromElevation"/>:</b> <br/>
        /// The normal will be derived from neighbouring elevation values <br/><br/>
        /// <b><see cref="SurfaceUp"/>:</b> <br/>
        /// The planet up vector will be used as a normal irregardless of the terrain elevation <br/><br/>
        /// <b><see cref="Auto"/>:</b> <br/>
        /// The normals will be automatically calculated by Unity's <see cref="DepictionEngine.Mesh.RecalculateNormals"/> function
        /// </summary>
        public enum NormalsType
        {
            DerivedFromElevation,
            SurfaceUp,
            Auto
        };

        private const float MIN_SUBDIVISION_ZOOM_FACTOR = 1.0f;
        private const float MAX_SUBDIVISION_ZOOM_FACTOR = 3.0f;

        [BeginFoldout("Terrain Mesh")]
        [SerializeField, Range(1, 127), Tooltip("The minimum number of subdivisions the tile geometry will have when in spherical mode.")]
        private int _sphericalSubdivision;
        [SerializeField, Range(1, 127), Tooltip("The minimum number of subdivisions the tile geometry will have when in flat mode.")]
        private int _flatSubdivision;
        [SerializeField, Range(MIN_SUBDIVISION_ZOOM_FACTOR, MAX_SUBDIVISION_ZOOM_FACTOR), Tooltip("A factor by which the number of subdivisions will be increased as the zoom level decreases according to the following formula. Zoom level 23 or higher will always have the minimum amount of subdivions.")]
        private float _subdivisionZoomFactor;
        [SerializeField, Tooltip("A factor by which the geometry will be scaled along the longitudinal and latitudinal axis to overlap with other tiles of a similar zoom level.")]
        private float _overlapFactor;
        [SerializeField, Tooltip("How deep the tile edges should extend below the ground, in local units. Extending the edges can help avoid gaps between the tiles when elevation is used. Set to zero to deactivate.")]
        private float _edgeDepth;
        [SerializeField, Tooltip("The type of normals to generate for the terrain mesh."), EndFoldout]
        private NormalsType _normalsType;

        [BeginFoldout("Material")]
        [SerializeField, Tooltip("The path of the material's shader from within the Resources directory.")]
        private string _shaderPath;
        [SerializeField, Range(0.0f, 1.0f), Tooltip("A value passed to the shader to determine how much edge overlap should exist between the tile and other higher zoom level tiles covering part or all of its surface."), EndFoldout]
        private float _edgeOverlapThickness;

        [BeginFoldout("Color")]
        [SerializeField, Tooltip("A color value which will override texture color depending on alpha value."), EndFoldout]
        private Color _color;

        [SerializeField, HideInInspector]
        private Material _material;

        private Texture _colorMap;
        private Texture _additionalMap;
        private Texture _surfaceTypeMap;

        private int _subdivision;
        private float _subdivisionSize;

        private int _cameraCount;

        private bool _generateEdgeInSeperateMesh;

        private TerrainGridCache _terrainGridCache;

        public override void Recycle()
        {
            base.Recycle();

            _subdivision = 0;
            _subdivisionSize = 0.0f;

            _cameraCount = 0;

            _generateEdgeInSeperateMesh = false;

            if (_terrainGridCache != null)
                _terrainGridCache.Clear();
        }

        protected override void InitializeSerializedFields(InstanceManager.InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            if (initializingContext == InstanceManager.InitializationContext.Editor_Duplicate || initializingContext == InstanceManager.InitializationContext.Programmatically_Duplicate)
                _material = null;

            InitValue(value => sphericalSubdivision = value, 2, initializingContext);
            InitValue(value => flatSubdivision = value, 2, initializingContext);
            InitValue(value => subdivisionZoomFactor = value, 1.2f, initializingContext);
            InitValue(value => overlapFactor = value, 1.0f, initializingContext);
            InitValue(value => edgeDepth = value, GetDefaultEdgeDepth(), initializingContext);
            InitValue(value => normalsType = value, NormalsType.DerivedFromElevation, initializingContext);
            InitValue(value => shaderPath = value, GetDefaultShaderPath(), initializingContext);
            InitValue(value => edgeOverlapThickness = value, 0.12f, initializingContext);
            InitValue(value => color = value, Color.clear, initializingContext);
        }

        protected override bool UpdateReferences(bool forceUpdate = false)
        {
            if (base.UpdateReferences(forceUpdate))
            {
                UpdateColorMap();
                UpdateAdditionalMap();
                UpdateSurfaceTypeMap();

                return true;
            }
            return false;
        }

        protected virtual string GetDefaultShaderPath()
        {
            return RenderingManager.SHADER_BASE_PATH + "TerrainGrid";
        }

        protected virtual float GetDefaultEdgeDepth()
        {
            return -1.0f;
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

        protected override void InitializeMaterial(MeshRendererVisual meshRendererVisual, Material material = null)
        {
            base.InitializeMaterial(meshRendererVisual, UpdateMaterial(ref _material, shaderPath));
        }

        /// <summary>
        /// The minimum number of subdivisions the tile geometry will have when in spherical mode.
        /// </summary>
        [Json]
        public int sphericalSubdivision
        {
            get { return _sphericalSubdivision; }
            set { SetValue(nameof(sphericalSubdivision), ValidateSubdivision(value), ref _sphericalSubdivision); }
        }

        /// <summary>
        /// The minimum number of subdivisions the tile geometry will have when in flat mode.
        /// </summary>
        [Json]
        public int flatSubdivision
        {
            get { return _flatSubdivision; }
            set { SetValue(nameof(flatSubdivision), ValidateSubdivision(value), ref _flatSubdivision); }
        }

        /// <summary>
        /// A factor by which the number of subdivisions will be increased as the zoom level decreases according to the following formula.
        /// <code>
        /// zoom -= 24;
        /// if (zoom >= 1)
        ///     newSubdivision *= Mathf.Pow(subdivisionZoomFactor, zoom);
        /// </code>
        /// </summary>
        /// <remarks>Zoom level 23 or higher will always have the minimum amount of subdivions.</remarks>
        [Json]
        public float subdivisionZoomFactor
        {
            get { return _subdivisionZoomFactor; }
            set { SetValue(nameof(subdivisionZoomFactor), Mathf.Clamp(value, MIN_SUBDIVISION_ZOOM_FACTOR, MAX_SUBDIVISION_ZOOM_FACTOR), ref _subdivisionZoomFactor); }
        }

        /// <summary>
        /// A factor by which the geometry will be scaled along the longitudinal and latitudinal axis to overlap with other tiles of a similar zoom level.
        /// </summary>
        [Json]
        public float overlapFactor
        {
            get { return _overlapFactor; }
            set { SetValue(nameof(overlapFactor), Mathf.Clamp(value, 0.5f, 1.5f), ref _overlapFactor); }
        }

        /// <summary>
        /// How deep the tile edges should extend below the ground, in local units. Extending the edges can help avoid gaps between the tiles when elevation is used. Set to zero to deactivate.
        /// </summary>
        [Json]
        public float edgeDepth
        {
            get { return _edgeDepth; }
            set { SetValue(nameof(edgeDepth), value, ref _edgeDepth); }
        }

        /// <summary>
        /// The type of normals to generate for the terrain mesh.
        /// </summary>
        [Json]
        public NormalsType normalsType
        {
            get { return _normalsType; }
            set { SetValue(nameof(normalsType), value, ref _normalsType); }
        }

        /// <summary>
        /// The path of the material's shader from within the Resources directory.
        /// </summary>
        [Json]
        public string shaderPath
        {
            get { return _shaderPath; }
            set { SetValue(nameof(shaderPath), value, ref _shaderPath); }
        }

        /// <summary>
        /// A value passed to the shader to determine how much edge overlap should exist between the tile and other higher zoom level tiles covering part or all of its surface. 
        /// </summary>
        [Json]
        public float edgeOverlapThickness
        {
            get { return _edgeOverlapThickness; }
            set
            {
                SetValue(nameof(edgeOverlapThickness), Mathf.Clamp01(value), ref _edgeOverlapThickness, (newValue, oldValue) =>
                {
                    terrainGridCache.Dirty();
                });
            }
        }

        /// <summary>
        /// A color value which will override texture color depending on alpha value.
        /// </summary>
        [Json]
        public Color color
        {
            get { return _color; }
            set { SetValue(nameof(color), value, ref _color); }
        }

        protected override Color GetColor()
        {
            return color;
        }

        private AssetReference colorMapAssetReference
        {
            get { return AppendToReferenceComponentName(GetReferenceAt(1), typeof(Texture).Name + " ColorMap") as AssetReference; }
        }

        private void UpdateColorMap()
        {
            colorMap = colorMapAssetReference != Disposable.NULL ? colorMapAssetReference.data as Texture : null;
        }

        private Texture colorMap
        {
            get { return _colorMap; }
            set
            {
                if (_colorMap == value)
                    return;

                _colorMap = value;
            }
        }

        protected override Texture GetColorMap()
        {
            return colorMap;
        }

        private AssetReference additionalMapAssetReference
        {
            get { return AppendToReferenceComponentName(GetReferenceAt(2), typeof(Texture).Name + " AdditionalMap") as AssetReference; }
        }

        private void UpdateAdditionalMap()
        {
            additionalMap = additionalMapAssetReference != Disposable.NULL ? additionalMapAssetReference.data as Texture : null;
        }

        private Texture additionalMap
        {
            get { return _additionalMap; }
            set
            {
                if (_additionalMap == value)
                    return;

                _additionalMap = value;
            }
        }

        protected override Texture GetAdditionalMap()
        {
            return additionalMap;
        }

        private AssetReference surfaceTypeMapAssetReference
        {
            get { return AppendToReferenceComponentName(GetReferenceAt(3), typeof(Texture).Name + " SurfaceTypeMap") as AssetReference; }
        }

        private void UpdateSurfaceTypeMap()
        {
            surfaceTypeMap = surfaceTypeMapAssetReference != Disposable.NULL ? surfaceTypeMapAssetReference.data as Texture : null;
        }

        private Texture surfaceTypeMap
        {
            get { return _surfaceTypeMap; }
            set
            {
                if (_surfaceTypeMap == value)
                    return;

                _surfaceTypeMap = value;
            }
        }

        private TerrainGridCache terrainGridCache
        {
            get
            {
                _terrainGridCache ??= new TerrainGridCache().Initialize();
                return _terrainGridCache;
            }
        }

        private bool generateEdgeInSeperateMesh
        {
            get { return _generateEdgeInSeperateMesh; }
            set { _generateEdgeInSeperateMesh = value; }
        }

        /// <summary>
        /// The actual number of subdivisions after calculations.
        /// </summary>
        public int subdivision
        {
            get { return _subdivision; }
            private set { SetValue(nameof(subdivision), ValidateSubdivision(value), ref _subdivision); }
        }

        private float subdivisionSize
        {
            get { return _subdivisionSize; }
            set { SetValue(nameof(subdivisionSize), value, ref _subdivisionSize); }
        }

        private int cameraCount
        {
            get { return _cameraCount; }
            set { SetCameraCount(value); }
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

        protected override bool AssetLoaded()
        {
            return base.AssetLoaded() && (colorMapAssetReference == Disposable.NULL || colorMapAssetReference.IsLoaded()) && (additionalMapAssetReference == Disposable.NULL || additionalMapAssetReference.IsLoaded()) && (surfaceTypeMapAssetReference == Disposable.NULL || surfaceTypeMapAssetReference.IsLoaded());
        }

        protected override MeshRendererVisual.ColliderType GetColliderType()
        {
            return IsFlat() && elevation == Disposable.NULL ? MeshRendererVisual.ColliderType.Box : base.GetColliderType();
        }

        private float GetEdgeDepth()
        {
            return edgeDepth / 10.0f;
        }

        private float GetSubdivisionSize(int subdivision)
        {
            return 1.0f / subdivision;
        }

        protected override bool SetAlpha(float value)
        {
            if (base.SetAlpha(value))
            {
                terrainGridCache.Dirty();

                return true;
            }
            return false;
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

        protected Func<ProcessorOutput, ProcessorParameters, IEnumerator> GetGridProcessingFunction()
        {
            if (!generateEdgeInSeperateMesh)
                return TerrainGridMeshObjectProcessingFunctions.InitPopulateEdgeAndGrid;
            else
                return Grid2DMeshObjectProcessingFunctions.InitPopulateGrid;
        }

        protected override void ModifyMesh(MeshRendererVisualModifier meshRendererVisualModifier, Mesh mesh, Action meshModified, VisualObjectVisualDirtyFlags meshRendererVisualDirtyFlags, bool disposeMeshModifier = true)
        {
            Func<ProcessorOutput, ProcessorParameters, IEnumerator> processingFunction = TerrainGridMeshObjectProcessingFunctions.InitPopulateEdgeAndGrid;

            if (generateEdgeInSeperateMesh)
                processingFunction = Grid2DMeshObjectProcessingFunctions.InitPopulateGrid;

            meshRendererVisualModifier.StartProcessing(processingFunction, GetProcessorParametersType(), InitializeProcessorParameters, GetProcessingType(meshRendererVisualDirtyFlags),
                (data) =>
                {
                    meshRendererVisualModifier.meshModifier = data.meshModifier;
                    data.meshModifier = null;
                    base.ModifyMesh(meshRendererVisualModifier, mesh, meshModified, meshRendererVisualDirtyFlags, disposeMeshModifier);
                });
        }

        protected override void UpdateMeshRendererVisualModifiers(Action<VisualObjectVisualDirtyFlags> completedCallback, VisualObjectVisualDirtyFlags meshRendererVisualDirtyFlags)
        {
            base.UpdateMeshRendererVisualModifiers(completedCallback, meshRendererVisualDirtyFlags);

            if (generateEdgeInSeperateMesh)
            {
                if (meshRendererVisualModifiers.Count < 2)
                    meshRendererVisualModifiers.Add(MeshRendererVisual.CreateMeshRendererVisualModifier());

                meshRendererVisualModifiers[1].SetTypes(typeof(TerrainEdgeMeshRendererVisualNoCollider), typeof(TerrainEdgeMeshRendererVisualBoxCollider), typeof(TerrainEdgeMeshRendererVisualMeshCollider));
            }

            completedCallback?.Invoke(meshRendererVisualDirtyFlags);
        }

        protected override int GetCacheHash(MeshRendererVisualModifier meshRendererVisualModifier)
        {
            int hash = base.GetCacheHash(meshRendererVisualModifier);

            if (sphericalRatio == 0.0f && elevation == Disposable.NULL)
            {
                hash = 17;
                hash *= 31 + GetEdgeDepth().GetHashCode();
                hash *= 31 + subdivision.GetHashCode();
                hash *= 31 + overlapFactor.GetHashCode();
                hash *= 31 + (grid2DDimensions.x / grid2DDimensions.y).GetHashCode();

                //0 == terrain + edge
                //1 == terrain
                //2 == edge
                hash *= 31 + (!generateEdgeInSeperateMesh ? 0 : !meshRendererVisualModifier.typeNoCollider.IsSubclassOf(typeof(TerrainEdgeMeshRendererVisual)) ? 1 : 2);
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
                terrainMeshRendererVisualDirtyFlags.generateEdgeInSeperateMesh = generateEdgeInSeperateMesh;
                terrainMeshRendererVisualDirtyFlags.edgeDepth = GetEdgeDepth();
                terrainMeshRendererVisualDirtyFlags.normalsType = normalsType;
            }
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
                
                (parameters as TerrainGridMeshObjectParameters).Init(terrainRendererVisualDirtyFlags.subdivision, terrainRendererVisualDirtyFlags.subdivisionSize, terrainRendererVisualDirtyFlags.overlapFactor, terrainRendererVisualDirtyFlags.edgeDepth, terrainRendererVisualDirtyFlags.normalsType, terrainRendererVisualDirtyFlags.trianglesDirty, terrainRendererVisualDirtyFlags.uvsDirty, terrainRendererVisualDirtyFlags.verticesNormalsDirty, terrainRendererVisualDirtyFlags.generateEdgeInSeperateMesh);
            }
        }

        protected override void ApplyCastShadowToMeshRendererVisual(MeshRendererVisual meshRendererVisual, ShadowCastingMode shadowCastingMode)
        {
            base.ApplyCastShadowToMeshRendererVisual(meshRendererVisual, alpha != 1.0f ? ShadowCastingMode.Off : shadowCastingMode);
        }

        protected override void ApplyPropertiesToMaterial(MeshRenderer meshRenderer, Material material, MaterialPropertyBlock materialPropertyBlock, double cameraAtmosphereAltitudeRatio, Camera camera, GeoAstroObject closestGeoAstroObject, Star star)
        {
            base.ApplyPropertiesToMaterial(meshRenderer, material, materialPropertyBlock, cameraAtmosphereAltitudeRatio, camera, closestGeoAstroObject, star);

            SetTextureToMaterial("_SurfaceTypeMap", surfaceTypeMap, Texture2D.whiteTexture, material, materialPropertyBlock);
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
                                for (int collumn = -1; collumn <= 1; collumn++)
                                    alphaRow[collumn + 1] = 1.0f - Mathf.Min(alphaQuadrant, GetTerrainGridMeshObjectAlpha(terrainGridCache, quadrantIndex, new Vector2Int(collumn, row), camera));
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

        protected override bool OnDisposed(DisposeManager.DisposeContext disposeContext, bool pooled)
        {
            if (base.OnDisposed(disposeContext, pooled))
            {
                if (!pooled)
                    Dispose(_material, disposeContext);

                return true;
            }
            return false;
        }

        protected class TerrainGridMeshObjectParameters : ElevationGridMeshObjectParameters
        {
            private int _subdivision;
            private float _subdivisionSize;
            private float _overlapFactor;
            private float _edgeDepth;
            private NormalsType _normalsType;

            private bool _generateEdgeInSeperateMesh;

            private bool _trianglesDirty;
            private bool _uvsDirty;
            private bool _verticesDirty;
            private bool _normalsDirty;

            public TerrainGridMeshObjectParameters Init(int subdivision, float subdivisionSize, float overlapFactor = 1.0f, float edgeDepth = 0.0f, NormalsType normalsType = NormalsType.DerivedFromElevation, bool trianglesDirty = true, bool uvsDirty = true, bool verticesNormalsDirty = true, bool generateEdgeInSeperateMesh = false)
            {
                _subdivision = subdivision;
                _subdivisionSize = subdivisionSize;
                _overlapFactor = overlapFactor;
                _edgeDepth = edgeDepth;
                _normalsType = normalsType;

                _generateEdgeInSeperateMesh = generateEdgeInSeperateMesh;

                _trianglesDirty = trianglesDirty;
                _uvsDirty = uvsDirty;
                _verticesDirty = verticesNormalsDirty;
                _normalsDirty = verticesNormalsDirty;

                return this;
            }

            public bool generateEdgeInSeperateMesh
            {
                get { return _generateEdgeInSeperateMesh; }
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
                return uvsDirty;
            }

            public bool GetVerticesDirty()
            {
                return verticesDirty;
            }

            public bool GetNormalsDirty()
            {
                return normalsDirty;
            }

            public float edgeDepth
            {
                get { return _edgeDepth; }
            }

            public NormalsType normalsType
            {
                get { return _normalsType; }
            }

            public bool trianglesDirty
            {
                get { return _trianglesDirty; }
            }

            public bool uvsDirty
            {
                get { return _uvsDirty; }
            }

            public bool verticesDirty
            {
                get { return _verticesDirty; }
            }

            public bool normalsDirty
            {
                get { return _normalsDirty; }
            }

            public override bool OnDisposing(DisposeManager.DisposeContext disposeContext)
            {
                if (base.OnDisposing(disposeContext))
                {
                    _subdivision = 0;
                    _subdivisionSize = 0.0f;
                    _overlapFactor = 0.0f;
                    _edgeDepth = 0.0f;

                    _generateEdgeInSeperateMesh = false;

                    _trianglesDirty = false;
                    _uvsDirty = false;
                    _verticesDirty = false;
                    _normalsDirty = false;

                    return true;
                }
                return false;
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

            private void TerrainGridMeshObjectChildAddedHandler(Object objectBase, PropertyMonoBehaviour child)
            {
                if (child is MeshRendererVisual)
                    _changed = true;
            }

            private void TerrainGridMeshObjectChildRemovedHandler(Object objectBase, PropertyMonoBehaviour child)
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

        protected class Grid2DMeshObjectProcessingFunctions : ProcessingFunctions
        {
            public static IEnumerator InitPopulateGrid(ProcessorOutput data, ProcessorParameters parameters)
            {
                foreach (object enumeration in InitPopulateGrid(data as MeshRendererVisualProcessorOutput, parameters as TerrainGridMeshObjectParameters))
                    yield return enumeration;
            }

            private static IEnumerable InitPopulateGrid(MeshRendererVisualProcessorOutput meshRendererVisualProcessorOutput, TerrainGridMeshObjectParameters parameters)
            {
                int subdivision = parameters.GetSubdivision();
                int vertexCount = subdivision + 1;

                bool verticesDirty = parameters.GetVerticesDirty();
                bool normalsDirty = parameters.GetNormalsDirty();
                bool trianglesDirty = parameters.GetTrianglesDirty();
                bool uvsDirty = parameters.GetUVsDirty();

                meshRendererVisualProcessorOutput.meshModifier.Init(verticesDirty ? GetVerticesNormalsCount(vertexCount) : -1, normalsDirty ? parameters.normalsType != NormalsType.Auto ? GetVerticesNormalsCount(vertexCount) : 0 : -1, trianglesDirty ? GetTrianglesCount(subdivision) : -1, uvsDirty ? GetUVsCount(vertexCount) : -1);

                foreach (object enumeration in PopulateGrid(meshRendererVisualProcessorOutput, parameters))
                    yield return enumeration;
            }

            public static IEnumerable PopulateGrid(MeshRendererVisualProcessorOutput meshRendererVisualProcessorOutput, TerrainGridMeshObjectParameters parameters)
            {
                int subdivision = parameters.GetSubdivision();
                float subdivisionSize = parameters.GetSubdivisionSize();
                int vertexCount = subdivision + 1;

                bool verticesDirty = parameters.GetVerticesDirty();
                bool normalsDirty = parameters.GetNormalsDirty() && parameters.normalsType != NormalsType.Auto;
                bool trianglesDirty = parameters.GetTrianglesDirty();
                bool uvsDirty = parameters.GetUVsDirty();

                if (verticesDirty || normalsDirty || trianglesDirty || uvsDirty)
                {
                    MeshModifier meshModifier = meshRendererVisualProcessorOutput.meshModifier;

                    if (verticesDirty)
                    {
                        int startIndex = 0;
                        for (int y = 0; y < vertexCount; y++)
                        {
                            for (int x = 0; x < vertexCount; x++)
                                SetVertices(parameters, meshModifier, startIndex + y * vertexCount + x, x * subdivisionSize, y * subdivisionSize, parameters.GetOverlapFactor(), 0.0f);

                            if (parameters.cancellationTokenSource != null)
                                parameters.cancellationTokenSource.ThrowIfCancellationRequested();
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

                            if (parameters.cancellationTokenSource != null)
                                parameters.cancellationTokenSource.ThrowIfCancellationRequested();
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

                            if (parameters.cancellationTokenSource != null)
                                parameters.cancellationTokenSource.ThrowIfCancellationRequested();
                        }
                    }

                    if (uvsDirty)
                    {
                        int startIndex = 0;
                        for (int y = 0; y < vertexCount; y++)
                        {
                            for (int x = 0; x < vertexCount; x++)
                                meshModifier.uvs[startIndex + y * vertexCount + x] = new Vector2((float)(subdivisionSize * x), (float)(1.0d - (subdivisionSize * y)));

                            if (parameters.cancellationTokenSource != null)
                                parameters.cancellationTokenSource.ThrowIfCancellationRequested();
                        }
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

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static bool GetDiagonalOrientation(int x, int y)
            {
                return (x + (y % 2 == 0 ? 1 : 0)) % 2 == 0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected static GeoCoordinate3Double GetGeoCoordinate(TerrainGridMeshObjectParameters parameters, float normalizedX, float normalizedY)
            {
                GeoCoordinate3Double geoCoordinate = MathPlus.GetGeoCoordinate3FromIndex(new Vector2Double(parameters.grid2DIndex.x + normalizedX, parameters.grid2DIndex.y + normalizedY), parameters.grid2DDimensions);

                if (parameters.GetElevation(out double elevation, normalizedX, normalizedY))
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

                if (parameters.GetNormalsDirty())
                    meshModifier.normals[bufferIndex] = parameters.GetUpVector(geoCoordinate) * GetNormal(parameters, normalizedX, normalizedY, 0.05f);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected static Vector3Double GetNormal(TerrainGridMeshObjectParameters parameters, float normalizedX, float normalizedY, float subdivisionSize)
            {
                Vector3Double normal = Vector3Double.up;

                if (parameters.normalsType == NormalsType.DerivedFromElevation && parameters.elevation != Disposable.NULL)
                {
                    double leftElevation = parameters.GetElevation(normalizedX - subdivisionSize, normalizedY, true);
                    double rightElevation = parameters.GetElevation(normalizedX + subdivisionSize, normalizedY, true);
                    double downElevation = parameters.GetElevation(normalizedX, normalizedY - subdivisionSize, true);
                    double upElevation = parameters.GetElevation(normalizedX, normalizedY + subdivisionSize, true);
                    normal = Vector3Double.Cross(
                        new Vector3Double(0.0d, downElevation - upElevation, subdivisionSize * 2.0d).normalized,
                        new Vector3Double(subdivisionSize * 2.0d, rightElevation - leftElevation, 0.0d).normalized);
                }

                return normal;
            }
        }

        protected class TerrainGridMeshObjectProcessingFunctions : ProcessingFunctions
        {
            public static IEnumerator InitPopulateEdgeAndGrid(ProcessorOutput data, ProcessorParameters parameters)
            {
                foreach (object enumeration in InitPopulateEdgeAndGrid(data as MeshRendererVisualProcessorOutput, parameters as TerrainGridMeshObjectParameters))
                    yield return enumeration;
            }

            protected static IEnumerable InitPopulateEdgeAndGrid(MeshRendererVisualProcessorOutput meshRendererVisualProcessorOutput, TerrainGridMeshObjectParameters parameters)
            {
                bool addEdge = parameters.edgeDepth != 0.0f;

                int subdivision = parameters.GetSubdivision();
                int vertexCount = subdivision + 1;

                bool verticesDirty = parameters.GetVerticesDirty();
                bool normalsDirty = parameters.GetNormalsDirty();
                bool trianglesDirty = parameters.GetTrianglesDirty();
                bool uvsDirty = parameters.GetUVsDirty();

                int grid2DVerticesNormalsCount = Grid2DMeshObjectProcessingFunctions.GetVerticesNormalsCount(vertexCount);
                int grid2DTrianglesCount = Grid2DMeshObjectProcessingFunctions.GetTrianglesCount(subdivision);
                int grid2DUvsCount = Grid2DMeshObjectProcessingFunctions.GetUVsCount(vertexCount);

                int verticesCount = -1;
                if (verticesDirty)
                    verticesCount = grid2DVerticesNormalsCount + (addEdge ? GetVerticesNormalsCount(vertexCount) : 0);

                int normalsCount = -1;
                if (normalsDirty)
                    normalsCount = parameters.normalsType != NormalsType.Auto ? grid2DVerticesNormalsCount + (addEdge ? GetVerticesNormalsCount(vertexCount) : 0) : 0;

                int trianglesCount = -1;
                if (trianglesDirty)
                    trianglesCount = grid2DTrianglesCount + (addEdge ? GetTrianglesCount(subdivision * 4) : 0);
                
                int uvsCount = -1;
                if (uvsDirty)
                    uvsCount = grid2DUvsCount + (addEdge ? GetUVsCount(vertexCount) : 0);

                meshRendererVisualProcessorOutput.meshModifier.Init(verticesCount, normalsCount, trianglesCount, uvsCount);

                foreach (object enumeration in Grid2DMeshObjectProcessingFunctions.PopulateGrid(meshRendererVisualProcessorOutput, parameters))
                    yield return enumeration;

                if (addEdge)
                {
                    foreach (object enumeration in PopulateEdge(meshRendererVisualProcessorOutput, parameters, grid2DVerticesNormalsCount, grid2DTrianglesCount, grid2DUvsCount))
                        yield return enumeration;
                }
            }

            public static IEnumerator InitPopulateEdge(ProcessorOutput data, ProcessorParameters parameters)
            {
                foreach (object enumeration in InitPopulateEdge(data as MeshRendererVisualProcessorOutput, parameters as TerrainGridMeshObjectParameters))
                    yield return enumeration;
            }

            protected static IEnumerable InitPopulateEdge(MeshRendererVisualProcessorOutput meshRendererVisualProcessorOutput, TerrainGridMeshObjectParameters parameters)
            {
                int subdivision = parameters.GetSubdivision();
                int vertexCount = subdivision + 1;

                bool verticesDirty = parameters.GetVerticesDirty();
                bool normalsDirty = parameters.GetNormalsDirty();
                bool trianglesDirty = parameters.GetTrianglesDirty();
                bool uvsDirty = parameters.GetUVsDirty();

                meshRendererVisualProcessorOutput.meshModifier.Init(verticesDirty ? GetVerticesNormalsCount(vertexCount) : -1, normalsDirty ? parameters.normalsType != NormalsType.Auto ? GetVerticesNormalsCount(vertexCount) : 0 : -1, trianglesDirty ? GetTrianglesCount(subdivision) : -1, uvsDirty ? GetUVsCount(vertexCount) : -1);

                foreach (object enumeration in PopulateEdge(meshRendererVisualProcessorOutput, parameters))
                    yield return enumeration;
            }

            protected static IEnumerable PopulateEdge(MeshRendererVisualProcessorOutput meshRendererVisualProcessorOutput, TerrainGridMeshObjectParameters parameters, int verticesNormalsStartIndex = 0, int trianglesStartIndex = 0, int uvsStartIndex = 0)
            {
                int subdivision = parameters.GetSubdivision();
                float subdivisionSize = parameters.GetSubdivisionSize();
                int vertexCount = subdivision + 1;

                bool verticesDirty = parameters.GetVerticesDirty();
                bool normalsDirty = parameters.GetNormalsDirty() && parameters.normalsType != NormalsType.Auto;
                bool trianglesDirty = parameters.GetTrianglesDirty();
                bool uvsDirty = parameters.GetUVsDirty();

                if (verticesDirty || normalsDirty || trianglesDirty || uvsDirty)
                {
                    MeshModifier meshModifier = meshRendererVisualProcessorOutput.meshModifier;

                    if (verticesDirty)
                    {
                        int startIndex = verticesNormalsStartIndex;
                        for (int side = 0; side < 8; side++)
                        {
                            double edgeAltitudeOffset = side % 2 == 0 ? 0.0f : parameters.edgeDepth;

                            for (int index = 0; index < vertexCount; index++)
                                Grid2DMeshObjectProcessingFunctions.SetVertices(parameters, meshModifier, startIndex + side * vertexCount + index, GetX(side, index, vertexCount) * subdivisionSize, GetY(side, index, vertexCount) * subdivisionSize, parameters.GetOverlapFactor(), edgeAltitudeOffset);
                            
                            if (parameters.cancellationTokenSource != null)
                                parameters.cancellationTokenSource.ThrowIfCancellationRequested();
                        }
                    }

                    if (trianglesDirty)
                    {
                        int startIndex = verticesNormalsStartIndex;
                        int i = trianglesStartIndex;
                        for (int side = 0; side < 8; side += 2)
                        {
                            for (int index = 0; index < subdivision; index++)
                            {
                                int bottomLeft = startIndex + (side + 1) * vertexCount + index;
                                int topLeft = startIndex + (side * vertexCount) + index;
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

                            if (parameters.cancellationTokenSource != null)
                                parameters.cancellationTokenSource.ThrowIfCancellationRequested();
                        }
                    }

                    if (normalsDirty)
                    {
                        int startIndex = verticesNormalsStartIndex;
                        for (int side = 0; side < 8; side++)
                        {
                            for (int index = 0; index < vertexCount; index++)
                                Grid2DMeshObjectProcessingFunctions.SetNormals(parameters, meshModifier, startIndex + side * vertexCount + index, GetX(side, index, vertexCount) * subdivisionSize, GetY(side, index, vertexCount) * subdivisionSize);

                            if (parameters.cancellationTokenSource != null)
                                parameters.cancellationTokenSource.ThrowIfCancellationRequested();
                        }
                    }

                    if (uvsDirty)
                    {
                        int startIndex = uvsStartIndex;
                        for (int side = 0; side < 8; side++)
                        {
                            for (int index = 0; index < vertexCount; index++)
                                meshModifier.uvs[startIndex + side * vertexCount + index] = new Vector2((float)(subdivisionSize * GetX(side, index, vertexCount)), (float)(1.0d - (subdivisionSize * GetY(side, index, vertexCount))));
                            
                            if (parameters.cancellationTokenSource != null)
                                parameters.cancellationTokenSource.ThrowIfCancellationRequested();
                        }
                    }

                    meshModifier.CalculateBoundsFromMinMax();
                }

                yield break;
            }

            public static int GetVerticesNormalsCount(int vertexCount)
            {
                return vertexCount * 8;
            }

            public static int GetTrianglesCount(int subdivision)
            {
                return 6 * subdivision * 4;
            }

            public static int GetUVsCount(int vertexCount)
            {
                return vertexCount * 8;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static int GetX(int side, int index, int vertexCount)
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
            private static int GetY(int side, int index, int vertexCount)
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
        }
    }
}
