// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DepictionEngine
{
    [AddComponentMenu(SceneManager.NAMESPACE + "/Object/Astro/Grid/" + nameof(MeshGridMeshObject))]
    [RequireScript(typeof(AssetReference))]
    public class MeshGridMeshObject : FeatureGridMeshObjectBase
    {
        [BeginFoldout("Material")]
        [SerializeField, Tooltip("The path of the material's shader from within the Resources directory."), EndFoldout]
        private string _shaderPath;

        [SerializeField, HideInInspector]
        private Material _material;
        [SerializeField, HideInInspector]
        private UnityEngine.Mesh[] _unityMeshes;

        private AssetBase _mesh;

        public override void Recycle()
        {
            base.Recycle();

            unityMeshes = null;
        }

        protected override void InitializeSerializedFields(InstanceManager.InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            if (initializingContext == InstanceManager.InitializationContext.Editor_Duplicate || initializingContext == InstanceManager.InitializationContext.Programmatically_Duplicate)
                _material = null;

            InitValue(value => shaderPath = value, RenderingManager.SHADER_BASE_PATH + "BuildingGrid", initializingContext);
        }

        protected override void Initialized(InstanceManager.InitializationContext initializingContext)
        {
            base.Initialized(initializingContext);

            if (unityMeshes == null || unityMeshes.Length == 0)
                UdateUnityMeshes();
        }

        protected override bool UpdateAllDelegates()
        {
            if (base.UpdateAllDelegates())
            {
                RemoveAssetDelgates(mesh);
                AddAssetDelegates(mesh);

                return true;
            }
            return false;
        }

        private void RemoveAssetDelgates(AssetBase assetBase)
        {
            if (!Object.ReferenceEquals(assetBase, null))
                assetBase.PropertyAssignedEvent -= AssetPropertyAssignedHandler;
        }

        private void AddAssetDelegates(AssetBase assetBase)
        {
            if (!IsDisposing() && assetBase != Disposable.NULL)
                assetBase.PropertyAssignedEvent += AssetPropertyAssignedHandler;
        }

        private void AssetPropertyAssignedHandler(IProperty property, string name, object newValue, object oldValue)
        {
            if (name == nameof(AssetBase.data))
                MeshChanged();
        }

        protected override bool UpdateReferences(bool forceUpdate = false)
        {
            if (base.UpdateReferences(forceUpdate))
            {
                UpdateMesh();

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
            get { return _shaderPath; }
            set { SetValue(nameof(shaderPath), value, ref _shaderPath); }
        }

        public Processor meshRendererVisualModifiersProcessor
        {
            get { return _meshRendererVisualModifiersProcessor; }
            private set
            {
                if (Object.ReferenceEquals(_meshRendererVisualModifiersProcessor, value))
                    return;
                if (_meshRendererVisualModifiersProcessor != null)
                    _meshRendererVisualModifiersProcessor.Cancel();
                _meshRendererVisualModifiersProcessor = value;
            }
        }

        protected override bool AssetLoaded()
        {
            return base.AssetLoaded() && mesh != Disposable.NULL;
        }

        protected override Type GetMeshRendererVisualDirtyFlagType()
        {
            return typeof(BuildingGridMeshObjectVisualDirtyFlags);
        }

        protected override void UpdateMeshRendererDirtyFlags(VisualObjectVisualDirtyFlags meshRendererVisualDirtyFlags)
        {
            base.UpdateMeshRendererDirtyFlags(meshRendererVisualDirtyFlags);

            if (meshRendererVisualDirtyFlags is MeshGridMeshObjectVisualDirtyFlags)
            {
                MeshGridMeshObjectVisualDirtyFlags meshGridMeshObjectRendererVisualDirtyFlags = meshRendererVisualDirtyFlags as MeshGridMeshObjectVisualDirtyFlags;

                meshGridMeshObjectRendererVisualDirtyFlags.unityMeshes = unityMeshes;
            }
        }

        protected override Type GetProcessorParametersType()
        {
            return typeof(MeshGridMeshObjectParameters);
        }

        protected override void InitializeProcessorParameters(ProcessorParameters parameters)
        {
            base.InitializeProcessorParameters(parameters);

            if (meshRendererVisualDirtyFlags is MeshGridMeshObjectVisualDirtyFlags)
            {
                MeshGridMeshObjectVisualDirtyFlags meshGridMeshObjectVisualDirtyFlags = meshRendererVisualDirtyFlags as MeshGridMeshObjectVisualDirtyFlags;

                (parameters as MeshGridMeshObjectParameters).Init(meshGridMeshObjectVisualDirtyFlags.unityMeshes);
            }
        }

        private AssetReference meshAssetReference
        {
            get { return AppendToReferenceComponentName(GetReferenceAt(2), typeof(Mesh).Name) as AssetReference; }
        }

        private void UpdateMesh()
        {
            mesh = meshAssetReference != Disposable.NULL && meshAssetReference.data is IUnityMeshAsset ? featureAssetReference.data as AssetBase: null;
        }

        protected AssetBase mesh
        {
            get { return _mesh; }
            private set
            {
                AssetBase oldValue = _mesh;
                AssetBase newValue = value;

                if (oldValue == newValue)
                    return;

                RemoveAssetDelgates(oldValue);
                AddAssetDelegates(newValue);

                _mesh = newValue;

                MeshChanged();
            }
        }

        private void MeshChanged()
        {
            if (initialized)
            {
                UdateUnityMeshes();

                if (meshRendererVisualDirtyFlags is MeshGridMeshObjectVisualDirtyFlags)
                {
                    MeshGridMeshObjectVisualDirtyFlags featureMeshRendererVisualDirtyFlags = meshRendererVisualDirtyFlags as MeshGridMeshObjectVisualDirtyFlags;

                    featureMeshRendererVisualDirtyFlags.UnityMeshesChanged();
                }
            }
        }

        private void UdateUnityMeshes()
        {
            List<UnityEngine.Mesh> newUnityMeshes = new List<UnityEngine.Mesh>();

            if (mesh != null)
            {
                IUnityMeshAsset iUnityMesh = mesh as IUnityMeshAsset;
                iUnityMesh.IterateOverUnityMesh((unityMesh) =>
                {
                    newUnityMeshes.Add(Object.Instantiate(unityMesh));
                });
            }

            unityMeshes = newUnityMeshes.ToArray();
        }

        protected UnityEngine.Mesh[] unityMeshes
        {
            get { return _unityMeshes; }
            private set 
            {
                if (_unityMeshes == value)
                    return;

                DisposeUnityMeshes();

                _unityMeshes = value; 
            }
        }

        private Processor _meshRendererVisualModifiersProcessor;
        protected override void UpdateMeshRendererVisualModifiers(Action<VisualObjectVisualDirtyFlags> completedCallback, VisualObjectVisualDirtyFlags meshRendererVisualDirtyFlags)
        {
            base.UpdateMeshRendererVisualModifiers(completedCallback, meshRendererVisualDirtyFlags);

            MeshGridMeshObjectVisualDirtyFlags meshGridMeshObjectVisualDirtyFlags = meshRendererVisualDirtyFlags as MeshGridMeshObjectVisualDirtyFlags;

            if (meshGridMeshObjectVisualDirtyFlags != null)
            {
                if (meshRendererVisualModifiersProcessor == null)
                    meshRendererVisualModifiersProcessor = InstanceManager.Instance(false).CreateInstance<Processor>();

                meshGridMeshObjectVisualDirtyFlags.SetProcessing(true, meshRendererVisualModifiersProcessor);

                meshRendererVisualModifiersProcessor.StartProcessing(MeshGridMeshObjectProcessingFunctions.PopulateMeshes, typeof(MeshObjectProcessorOutput), typeof(MeshGridMeshObjectParameters), InitializeProcessorParameters,
                    (data, errorMsg) =>
                    {
                        meshGridMeshObjectVisualDirtyFlags.SetProcessing(false);

                        MeshObjectProcessorOutput meshObjectProcessorOutput = data as MeshObjectProcessorOutput;
                        if (meshObjectProcessorOutput != Disposable.NULL)
                        {
                            meshRendererVisualModifiers = meshObjectProcessorOutput.meshRendererVisualModifiers;

                            meshObjectProcessorOutput.Clear();

                            if (completedCallback != null)
                                completedCallback(meshRendererVisualDirtyFlags);
                        }

                    }, GetProcessingType(meshRendererVisualDirtyFlags));
            }
        }

        protected override void OnFeatureClickedHit(RaycastHitDouble hit, int featureIndex)
        {
            base.OnFeatureClickedHit(hit, featureIndex);

            PointFeature pointFeature = feature as PointFeature;
            if (pointFeature != Disposable.NULL)
                Debug.Log(pointFeature.GetGeoCoordinate(featureIndex));
        }

        protected override void InitializeMaterial(MeshRendererVisual meshRendererVisual, Material material = null)
        {
            base.InitializeMaterial(meshRendererVisual, UpdateMaterial(ref this._material, shaderPath));
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

        private void DisposeUnityMeshes()
        {
            if (_unityMeshes != null)
            {
                for (int i = _unityMeshes.Length; i >= 0; i--)
                    Dispose(_unityMeshes[i]);
            }
        }

        public override bool OnDispose()
        {
            if (base.OnDispose())
            {
                DisposeDataProcessor(_meshRendererVisualModifiersProcessor);

                return true;
            }
            return false;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            Dispose(_material);
            DisposeUnityMeshes();
        }

        protected class MeshGridMeshObjectParameters : FeatureParameters
        {
            private UnityEngine.Mesh[] _meshes;

            public override void Recycle()
            {
                base.Recycle();

                _meshes = null;
            }

            public MeshGridMeshObjectParameters Init(UnityEngine.Mesh[] meshes)
            {
                _meshes = meshes;

                return this;
            }

            protected override bool UseAltitude()
            {
                return false;
            }

            public UnityEngine.Mesh[] meshes
            {
                get { return _meshes; }
            }
        }

        protected class MeshGridMeshObjectProcessingFunctions : FeatureGridMeshObjectProcessingFunctions
        {
            public static IEnumerator PopulateMeshes(object data, ProcessorParameters parameters)
            {
                foreach (object enumeration in PopulateMeshes(data as MeshObjectProcessorOutput, parameters as MeshGridMeshObjectParameters))
                    yield return enumeration;
            }

            private static IEnumerable PopulateMeshes(MeshObjectProcessorOutput meshObjectProcessorOutput, MeshGridMeshObjectParameters parameters)
            {
                PointFeature pointFeature = parameters.feature as PointFeature;
                if (pointFeature != null)
                {
                    List<int> triangles = new List<int>();
                    List<Vector3> vertices = new List<Vector3>();
                    List<Vector3> normals = new List<Vector3>();
                    List<Vector2> uvs = new List<Vector2>();

                    for (int i = 0; i < pointFeature.featureCount; i++)
                    {
                        UnityEngine.Mesh mesh = parameters.meshes[i];

                        if (mesh != null)
                        {
                            mesh.GetTriangles(triangles, 0);
                            mesh.GetVertices(vertices);
                            mesh.GetUVs(0, uvs);

                            GeoCoordinate3Double geoCoordinate = pointFeature.GetGeoCoordinate(i);

                            Vector3 point = parameters.TransformGeoCoordinateToVector(geoCoordinate.latitude, geoCoordinate.longitude);

                            float elevationDelta = 0.0f;
                            double elevation = 0.0d;
                            if (GetElevation(parameters, point, ref elevation))
                                elevationDelta = (float)(elevation - parameters.centerElevation);

                            for (int e = 0; e < vertices.Count; e++)
                            {
                                Vector3 vertex = vertices[e];
                                vertex.x += point.x;
                                vertex.y += elevationDelta;
                                vertex.z += point.z;
                                vertices[e] = vertex;
                            }

                            AddBuffers(meshObjectProcessorOutput, triangles, vertices, normals, uvs);
                        }
                    }

                    foreach (MeshRendererVisualModifier meshRendererVisualModifier in meshObjectProcessorOutput.meshRendererVisualModifiers)
                        meshRendererVisualModifier.meshModifier.CalculateBoundsFromMinMax();
                }
                yield break;
            }
        }
    }
}
