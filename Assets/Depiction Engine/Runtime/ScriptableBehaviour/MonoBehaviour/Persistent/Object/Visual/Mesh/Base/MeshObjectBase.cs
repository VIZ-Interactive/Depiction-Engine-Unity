// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace DepictionEngine
{
    public class MeshObjectBase : VisualObject, IProcessing
    {
        protected const int DEFAULT_MISSING_CACHE_HASH = -1;

        [BeginFoldout("Collider")]
        [SerializeField, Tooltip("When enabled the "+nameof(MeshRendererVisual)+"'s will have collider component.")]
        private bool _useCollider;
        [SerializeField, Tooltip("When enabled the "+nameof(MeshCollider)+ "'s will have '"+nameof(MeshCollider.convex)+"' set to true."), EndFoldout]
        private bool _convexCollider;

        [BeginFoldout("Shadow")]
        [SerializeField, Tooltip("When enabled the meshes '"+nameof(MeshRenderer)+ "' will have '"+nameof(Renderer.shadowCastingMode)+ "' set to '"+nameof(ShadowCastingMode.On)+ "', otherwise it will be '"+nameof(ShadowCastingMode.Off)+"'.")]
        private bool _castShadow;
        [SerializeField, Tooltip("When enabled the meshes '"+nameof(MeshRenderer)+ "' will have '"+nameof(Renderer.receiveShadows)+"' set to this value."), EndFoldout]
        private bool _receiveShadows;

        [SerializeField, HideInInspector]
        private MeshRendererVisual.ColliderType _colliderType;

        [SerializeField]
#if UNITY_EDITOR
        [ConditionalShow(nameof(GetDebug))]
#endif
        private List<Mesh> _meshes;
        [SerializeField]
#if UNITY_EDITOR
        [ConditionalShow(nameof(GetDebug))]
#endif
        private List<bool> _isSharedMeshFlags;

        private List<MeshRendererVisualModifier> _meshRendererVisualModifiers;

        private List<MeshParameters> _meshParameters;

        private Processor _meshDataProcessor;

        private Action _processingCompletedEvent;

        public override void Recycle()
        {
            base.Recycle();

            if (_meshes != null)
                _meshes.Clear();
     
            if (_isSharedMeshFlags != null)
                _isSharedMeshFlags.Clear();

            _meshParameters = null;
        }

        protected override void InitializeSerializedFields(InstanceManager.InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            if (initializingContext == InstanceManager.InitializationContext.Editor_Duplicate || initializingContext == InstanceManager.InitializationContext.Programmatically_Duplicate)
            {
                meshes.Clear();
                isSharedMeshFlags.Clear();

                meshRendererVisualDirtyFlags.Recreate();
            }

            InitValue(value => useCollider = value, GetDefaultUseCollider(), initializingContext);
            InitValue(value => convexCollider = value, GetDefaultConvexCollider(), initializingContext);
            InitValue(value => castShadow = value, GetDefaultCastShadow(), initializingContext);
            InitValue(value => receiveShadows = value,GetDefaultReceiveShadows(), initializingContext);
        }

        protected virtual bool GetDefaultUseCollider()
        {
            return true;
        }

        protected virtual bool GetDefaultConvexCollider()
        {
            return false;
        }

        protected virtual bool GetDefaultCastShadow()
        {
            return true;
        }

        protected virtual bool GetDefaultReceiveShadows()
        {
            return true;
        }

        public override void ExplicitOnEnable()
        {
            base.ExplicitOnEnable();

            if (meshDataProcessor != null && meshDataProcessor.ProcessingWasCompromised())
                meshDataProcessor.Dispose();

            if (meshRendererVisualModifiers != null)
            {
                foreach (MeshRendererVisualModifier meshRendererVisualModifier in meshRendererVisualModifiers)
                    meshRendererVisualModifier.DisposeMeshModifierProcessorIfProcessingWasCompromised();
            }
        }

        /// <summary>
        /// When enabled the <see cref="DepictionEngine.MeshRendererVisual"/>'s will have collider component.
        /// </summary>
        [Json]
        public bool useCollider
        {
            get { return _useCollider; }
            set { SetValue(nameof(useCollider), value, ref _useCollider); }
        }

        /// <summary>
        /// When enabled the MeshCollider's will have 'MeshCollider.convex' set to true.
        /// </summary>
        [Json]
        public bool convexCollider
        {
            get { return _convexCollider; }
            set { SetValue(nameof(convexCollider), value, ref _convexCollider); }
        }

        /// <summary>
        /// When enabled the meshes 'MeshRenderer' will have 'Renderer.shadowCastingMode' set to 'ShadowCastingMode.On', otherwise it will be 'ShadowCastingMode.Off'.
        /// </summary>
        [Json]
        public bool castShadow
        {
            get { return _castShadow; }
            set { SetValue(nameof(castShadow), value, ref _castShadow); }
        }

        /// <summary>
        /// When enabled the meshes 'MeshRenderer' will have 'Renderer.receiveShadows' set to this value.
        /// </summary>
        [Json]
        public bool receiveShadows
        {
            get { return _receiveShadows; }
            set { SetValue(nameof(receiveShadows), value, ref _receiveShadows); }
        }

        protected override void DontSaveToSceneChanged(bool newValue, bool oldValue)
        {
            base.DontSaveToSceneChanged(newValue, oldValue);

            UpdateMeshesDontSaveToScene();
        }

        protected override void DontSaveVisualsToSceneChanged(bool newValue, bool oldValue)
        {
            base.DontSaveVisualsToSceneChanged(newValue, oldValue);

            UpdateMeshesDontSaveToScene();
        }

        private void UpdateMeshesDontSaveToScene()
        {
            foreach (Mesh mesh in meshes)
                mesh.dontSaveToScene = dontSaveToScene || dontSaveVisualsToScene;
        }

        /// <summary>
        /// Dispatched after the <see cref="DepictionEngine.MeshObjectBase.meshDataProcessor"/> as completed modifying the <see cref="DepictionEngine.Mesh"/> that will be displayed by the child <see cref="DepictionEngine.MeshRendererVisual"/>.
        /// </summary>
        public Action ProcessingCompletedEvent
        {
            get { return _processingCompletedEvent; }
            set { _processingCompletedEvent = value; }
        }

        protected Processor meshDataProcessor
        {
            get { return _meshDataProcessor; }
            set
            {
                if (Object.ReferenceEquals(_meshDataProcessor, value))
                    return;
                if (_meshDataProcessor != null)
                    _meshDataProcessor.Cancel();
                _meshDataProcessor = value;
            }
        }

        public Processor.ProcessingState processingState
        {
            get { return _meshDataProcessor != null ? _meshDataProcessor.processingState : Processor.ProcessingState.None; }
        }

        protected Processor.ProcessingType GetProcessingType(VisualObjectVisualDirtyFlags meshRendererVisualDirtyFlags)
        {
            return !meshRendererVisualDirtyFlags.disableMultithreading && sceneManager.enableMultithreading ? Processor.ProcessingType.AsyncTask : Processor.ProcessingType.Sync;
        }

        protected virtual MeshRendererVisual.ColliderType GetColliderType()
        {
            return MeshRendererVisual.ColliderType.Mesh;
        }

        private List<Mesh> meshes
        {
            get 
            {
                if (_meshes == null)
                    _meshes = new List<Mesh>();
                return _meshes; 
            }
        }

        private List<bool> isSharedMeshFlags
        {
            get
            {
                if (_isSharedMeshFlags == null)
                    _isSharedMeshFlags = new List<bool>();
                return _isSharedMeshFlags;
            }
        }

        public List<MeshRendererVisualModifier> meshRendererVisualModifiers
        {
            get 
            {
                if (_meshRendererVisualModifiers == null)
                    _meshRendererVisualModifiers = new List<MeshRendererVisualModifier>();
                return _meshRendererVisualModifiers; 
            }
            set 
            {
                if (Object.ReferenceEquals(_meshRendererVisualModifiers, value))
                    return;

                DisposeAllMeshRendererVisualModifiers();

                _meshRendererVisualModifiers = value;
            }
        }

        protected Type GetMeshRendererType(MeshRendererVisual.ColliderType colliderType, Type meshRendererVisualNoCollider, Type meshRendererVisualBoxCollider, Type meshRendererVisualMeshCollider)
        {
            return colliderType == MeshRendererVisual.ColliderType.None ? meshRendererVisualNoCollider : colliderType == MeshRendererVisual.ColliderType.Box ? meshRendererVisualBoxCollider : meshRendererVisualMeshCollider;
        }

        private MeshRendererVisual GetMeshRendererVisual(int index)
        {
            MeshRendererVisual meshRendererVisual = null;
            if (index < transform.children.Count)
                meshRendererVisual = transform.children[index] as MeshRendererVisual;
            return meshRendererVisual;
        }

        private Mesh GetMeshFromCache(MeshRendererVisualModifier meshRendererVisualModifier)
        {
            Mesh mesh = null;
            int hash = GetCacheHash(meshRendererVisualModifier);
            if (hash != -1)
                mesh = renderingManager.GetMeshFromCache(hash);
            return mesh;
        }

        protected virtual int GetCacheHash(MeshRendererVisualModifier meshRendererVisualModifier)
        {
            return DEFAULT_MISSING_CACHE_HASH;
        }

        private bool AddMeshToCache(MeshRendererVisualModifier meshRendererVisualModifier, Mesh mesh)
        {
            int hash = GetCacheHash(meshRendererVisualModifier);
            if (hash != DEFAULT_MISSING_CACHE_HASH)
            {
                renderingManager.AddMeshToCache(hash, mesh);
                return true;
            }
            return false;
        }

        protected override void UpdateMeshRendererDirtyFlags(VisualObjectVisualDirtyFlags meshRendererVisualDirtyFlags)
        {
            base.UpdateMeshRendererDirtyFlags(meshRendererVisualDirtyFlags);

            meshRendererVisualDirtyFlags.colliderType = useCollider ? GetColliderType() : MeshRendererVisual.ColliderType.None;
        }

        protected override void UpdateMeshRendererVisuals(VisualObjectVisualDirtyFlags meshRendererVisualDirtyFlags)
        {
            base.UpdateMeshRendererVisuals(meshRendererVisualDirtyFlags);

            if (meshRendererVisualDirtyFlags.isDirty)
                UpdateMeshRendererVisualModifiers(UpdateMeshRendererVisualModifiersMesh, meshRendererVisualDirtyFlags);

            bool allMeshesReady = false;

            foreach (MeshRendererVisualModifier meshRendererVisualModifier in meshRendererVisualModifiers)
            {
                if (meshRendererVisualModifier.sharedMesh != Disposable.NULL && !meshRendererVisualModifier.sharedMesh.modificationPending)
                    allMeshesReady = true;
                else
                {
                    allMeshesReady = false;
                    break;
                }
            }

            if (allMeshesReady)
            {
                if (meshRendererVisualModifiers.Count < transform.children.Count)
                    DisposeAllChildren();

                for (int i = 0; i < meshRendererVisualModifiers.Count; i++)
                {
                    MeshRendererVisualModifier meshRendererVisualModifier = meshRendererVisualModifiers[i];

                    MeshRendererVisual meshRendererVisual = GetMeshRendererVisual(i);
                    if (meshRendererVisual == Disposable.NULL)
                        meshRendererVisual = CreateMeshRendererVisual(meshRendererVisualDirtyFlags.colliderType, meshRendererVisualModifier);

                    meshRendererVisualModifier.sharedMesh = null;
                }

                meshRendererVisualModifiers = null;
            }

            transform.IterateOverChildren<MeshRendererVisual>((meshRendererVisual) =>
            {
                Mesh mesh = GetMeshFromUnityMesh(meshRendererVisual.sharedMesh, false);
                if (mesh != Disposable.NULL && mesh.NormalsDirty())
                    mesh.RecalculateNormals();
                return true;
            });

            if (meshDataProcessor == null || meshDataProcessor.processingState != Processor.ProcessingState.Processing)
            {
                Mesh.PhysicsBakeMeshType physicsBakeMesh = useCollider ? convexCollider ? Mesh.PhysicsBakeMeshType.Convex : Mesh.PhysicsBakeMeshType.Concave : Mesh.PhysicsBakeMeshType.None;
                transform.IterateOverChildren<MeshRendererVisual>((meshRendererVisual) =>
                {
                    Mesh mesh = GetMeshFromUnityMesh(meshRendererVisual.sharedMesh, false);
                    if (mesh != Disposable.NULL && mesh.PhysicsBakedMeshDirty(physicsBakeMesh))
                    {
                        if (_meshParameters == null)
                            _meshParameters = new List<MeshParameters>();
                        _meshParameters.Add(new MeshParameters(mesh, physicsBakeMesh));
                    }
                    return true;
                });

                if (_meshParameters != null)
                {
                    if (meshDataProcessor == null)
                        meshDataProcessor = InstanceManager.Instance(false).CreateInstance<Processor>();

                    meshDataProcessor.StartProcessing(MeshProcessingFunctions.ModifyMeshes, null, typeof(MeshesParameters), 
                        (parameters) => 
                        { 
                            (parameters as MeshesParameters).Init(_meshParameters); 
                        },
                        (data, errorMsg) => 
                        {
                            if (string.IsNullOrEmpty(errorMsg))
                            {
                                if (ProcessingCompletedEvent != null)
                                    ProcessingCompletedEvent();
                            }
                        }, sceneManager.enableMultithreading ? Processor.ProcessingType.AsyncTask : Processor.ProcessingType.Sync);
                    _meshParameters = null;
                }
            }
        }

        protected Mesh GetMeshFromUnityMesh(UnityEngine.Mesh unityMesh, bool includeSharedMesh = true)
        {
            for (int i = 0; i < meshes.Count; i++)
            {
                if (!isSharedMeshFlags[i] || includeSharedMesh)
                {
                    Mesh mesh = meshes[i];
                    if (mesh != Disposable.NULL && mesh.unityMesh == unityMesh)
                        return mesh;
                }
            }

            return null;
        }

        private MeshRendererVisual CreateMeshRendererVisual(MeshRendererVisual.ColliderType colliderType, MeshRendererVisualModifier meshRendererVisualModifier)
        {
            Type typeNoCollider = meshRendererVisualModifier.typeNoCollider != null ? meshRendererVisualModifier.typeNoCollider : typeof(MeshRendererVisualNoCollider);
            Type typeBoxCollider = meshRendererVisualModifier.typeBoxCollider != null ? meshRendererVisualModifier.typeBoxCollider : typeof(MeshRendererVisualBoxCollider);
            Type typeMeshCollider = meshRendererVisualModifier.typeMeshCollider != null ? meshRendererVisualModifier.typeMeshCollider : typeof(MeshRendererVisualMeshCollider);
            Type type = GetMeshRendererType(colliderType, typeNoCollider, typeBoxCollider, typeMeshCollider);

            return CreateVisual(type, meshRendererVisualModifier.name, null, new List<PropertyModifier>() { meshRendererVisualModifier }) as MeshRendererVisual;
        }

        protected virtual void UpdateMeshRendererVisualModifiers(Action<VisualObjectVisualDirtyFlags> completedCallback, VisualObjectVisualDirtyFlags meshRendererVisualDirtyFlags)
        {
           
        }

        private void UpdateMeshRendererVisualModifiersMesh(VisualObjectVisualDirtyFlags meshRendererVisualDirtyFlags)
        {
            for (int i = 0; i < meshRendererVisualModifiers.Count; i++)
            {
                MeshRendererVisualModifier meshRendererVisualModifier = meshRendererVisualModifiers[i];
                UpdateMeshRendererVisualModifierMesh(meshRendererVisualModifier, i, meshRendererVisualDirtyFlags);
            }
        }

        private void UpdateMeshRendererVisualModifierMesh(MeshRendererVisualModifier meshRendererVisualModifier, int index, VisualObjectVisualDirtyFlags meshRendererVisualDirtyFlags)
        {
            if (IsDisposing())
                return;

            bool isSharedMesh = true;

            Mesh mesh = GetMeshFromCache(meshRendererVisualModifier);

            if (mesh == Disposable.NULL || mesh.IsEmpty())
            {
                if (mesh == Disposable.NULL)
                {
                    mesh = index < meshes.Count ? meshes[index] : null;

                    Type meshType = meshRendererVisualModifier.GetMeshType();
                    if (mesh == Disposable.NULL || mesh.GetType() != meshType)
                        mesh = Mesh.CreateMesh(meshType, GetInitializeContext());

                    isSharedMesh = AddMeshToCache(meshRendererVisualModifier, mesh);
                }

                if (!isSharedMesh || !mesh.modificationPending)
                {
                    mesh.AddModifying(this);
                    ModifyMesh(meshRendererVisualModifier, mesh, () => { mesh.RemoveModifying(this); }, meshRendererVisualDirtyFlags);
                }
            }

            meshRendererVisualModifier.sharedMesh = mesh;

            UpdateMeshAtIndex(index, mesh, isSharedMesh);
        }

        protected virtual void ModifyMesh(MeshRendererVisualModifier meshRendererVisualModifier, Mesh mesh, Action meshModifiedCallback, VisualObjectVisualDirtyFlags meshRendererVisualDirtyFlags, bool disposeMeshModifier = true)
        {
            if (meshRendererVisualModifier.ApplyMeshModifierToMesh(mesh))
            {
                if (disposeMeshModifier)
                    meshRendererVisualModifier.DisposeMeshModifier();

                if (meshModifiedCallback != null)
                    meshModifiedCallback();
            }
        }

        private void UpdateMeshAtIndex(int index, Mesh mesh, bool isSharedMesh)
        {
            if (index + 1 > meshes.Count)
            {
                meshes.AddRange(new Mesh[index + 1 - meshes.Count]);
                isSharedMeshFlags.AddRange(new bool[index + 1 - isSharedMeshFlags.Count]);
            }

            if (mesh != meshes[index])
                DisposeMesh(index);

            meshes[index] = mesh;
            isSharedMeshFlags[index] = isSharedMesh;

            UpdateMeshesDontSaveToScene();
        }

        protected virtual void ApplyCastShadowToMeshRendererVisual(MeshRendererVisual meshRendererVisual, ShadowCastingMode shadowCastingMode)
        {
            meshRendererVisual.shadowCastingMode = shadowCastingMode;
        }

        protected override void ApplyPropertiesToVisual(bool visualsChanged, VisualObjectVisualDirtyFlags meshRendererVisualDirtyFlags)
        {
            base.ApplyPropertiesToVisual(visualsChanged, meshRendererVisualDirtyFlags);

            transform.IterateOverChildren<MeshRendererVisual>((meshRendererVisual) =>
            {
                if (visualsChanged || PropertyDirty(nameof(castShadow)) || PropertyDirty(nameof(alpha)))
                    ApplyCastShadowToMeshRendererVisual(meshRendererVisual, castShadow ? ShadowCastingMode.On : ShadowCastingMode.Off);
                
                if (visualsChanged || PropertyDirty(nameof(receiveShadows)))
                    meshRendererVisual.receiveShadows = receiveShadows;

                UpdateMeshRendererVisualCollider(meshRendererVisual, meshRendererVisualDirtyFlags.colliderType, convexCollider, visualsChanged, meshRendererVisualDirtyFlags.colliderTypeDirty, PropertyDirty(nameof(convexCollider)));

                InitializeMaterial(meshRendererVisual);

                return true;
            });
        }

        [NonSerialized]
        private List<Mesh> _lastMeshes;
        [NonSerialized]
        private List<bool> _lastIsSharedMeshFlags;
        protected override void Saving(Scene scene, string path)
        {
            base.Saving(scene, path);

            _lastMeshes = _meshes;
            _lastIsSharedMeshFlags = _isSharedMeshFlags;
            if (dontSaveVisualsToScene)
            {
                _meshes = null;
                _isSharedMeshFlags = null;
            }
        }

        protected override void Saved(Scene scene)
        {
            base.Saved(scene);

            _meshes = _lastMeshes;
            _isSharedMeshFlags = _lastIsSharedMeshFlags;
        }

        protected void DisposeAllMeshRendererVisualModifiers()
        {
            if (_meshRendererVisualModifiers != null)
            {
                foreach (MeshRendererVisualModifier meshRendererVisualModifier in _meshRendererVisualModifiers)
                    DisposeMeshRendererVisualModifier(meshRendererVisualModifier);
                _meshRendererVisualModifiers.Clear();
            }
        }

        protected void DisposeMeshRendererVisualModifier(MeshRendererVisualModifier meshRendererVisualModifier)
        {
            DisposeManager.Dispose(meshRendererVisualModifier);
        }

        private void DisposeAllMeshes(DisposeManager.DestroyDelay destroyDelay = DisposeManager.DestroyDelay.None)
        {
            if (_meshes != null)
            {
                for (int i = _meshes.Count - 1; i >= 0; i--)
                    DisposeMesh(i, destroyDelay);
            }
        }

        private void DisposeMesh(int index, DisposeManager.DestroyDelay destroyDelay = DisposeManager.DestroyDelay.None)
        {
            if (_meshes != null && _isSharedMeshFlags != null)
            {
                if (!_isSharedMeshFlags[index])
                    Dispose(_meshes[index], destroyDelay);
            }
        }

        public override bool OnDispose()
        {
            if (base.OnDispose())
            {
                DisposeDataProcessor(_meshDataProcessor);

                DisposeAllMeshes(DisposeManager.DestroyDelay.Delayed);

                return true;
            }
            return false;
        }

        protected override bool OnDisposed(DisposeManager.DestroyContext destroyContext)
        {
            if (base.OnDisposed(destroyContext))
            {
                meshRendererVisualModifiers = null;

                ProcessingCompletedEvent = null;

                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// A list of Mesh parameters used by <see cref="DepictionEngine.MeshProcessingFunctions"/>.
    /// </summary>
    public class MeshesParameters : ProcessorParameters
    {
        private List<MeshParameters> _meshesParameters;

        public override void Recycle()
        {
            base.Recycle();

            _meshesParameters.Clear();
        }

        public MeshesParameters Init(List<MeshParameters> meshesParameters)
        {
            foreach (MeshParameters meshParameters in meshesParameters)
                Lock(meshParameters.mesh);
            _meshesParameters = meshesParameters;

            return this;
        }

        public List<MeshParameters> meshesParameters
        {
            get { return _meshesParameters; }
        }

        public IEnumerable ApplyPhysicsBakeMeshToMesh()
        {
            foreach (MeshParameters meshParameters in meshesParameters)
                meshParameters.mesh.PhysicsBakeMesh(meshParameters.physicsBakeMesh);

            yield break;
        }
    }

    /// <summary>
    /// Mesh parameters used by <see cref="DepictionEngine.MeshProcessingFunctions"/>.
    /// </summary>
    public struct MeshParameters
    {
        public Mesh mesh;
        public Mesh.PhysicsBakeMeshType physicsBakeMesh;

        public MeshParameters(Mesh mesh, Mesh.PhysicsBakeMeshType physicsBakeMesh)
        {
            this.mesh = mesh;
            this.physicsBakeMesh = physicsBakeMesh;
        }
    }

    public class MeshProcessingFunctions : ProcessingFunctions
    {
        public static IEnumerator ModifyMeshes(object data, ProcessorParameters parameters)
        {
            foreach (object enumeration in ModifyMeshes(data, parameters as MeshesParameters))
                yield return enumeration;
        }

        protected static IEnumerable ModifyMeshes(object data, MeshesParameters parameters)
        {
            foreach (object enumeration in parameters.ApplyPhysicsBakeMeshToMesh())
                yield return enumeration;
        }
    }

    public class MeshObjectProcessorOutput : ProcessorOutput
    {
        private List<MeshRendererVisualModifier> _meshRendererVisualModifiers;

        public override void Recycle()
        {
            base.Recycle();

            Clear();
        }

        public MeshRendererVisualModifier currentMeshRendererVisualModifier
        {
            get { return meshRendererVisualModifiers.Count == 0 ? null : meshRendererVisualModifiers[meshRendererVisualModifiers.Count - 1]; }
        }

        public int meshRendererVisualModifiersCount
        {
            get { return meshRendererVisualModifiers.Count; }
        }

        public List<MeshRendererVisualModifier> meshRendererVisualModifiers
        {
            get 
            {
                if (_meshRendererVisualModifiers == null)
                    _meshRendererVisualModifiers = new List<MeshRendererVisualModifier>();
                return _meshRendererVisualModifiers; 
            }
            private set
            {
                if (Object.ReferenceEquals(_meshRendererVisualModifiers, value))
                    return;
                _meshRendererVisualModifiers = value;
            }
        }

        public void AddMeshRendererVisualModifier(MeshRendererVisualModifier meshModifier)
        {
            if (meshRendererVisualModifiers.Count > 0)
            {
                FeatureMeshModifier featureMeshModifier = meshRendererVisualModifiers[meshRendererVisualModifiers.Count - 1].meshModifier as FeatureMeshModifier;
                if (featureMeshModifier != Disposable.NULL)
                    featureMeshModifier.FeatureComplete();
            }
            meshRendererVisualModifiers.Add(meshModifier);
        }

        public void Clear()
        {
            _meshRendererVisualModifiers = null;
        }

        protected override bool OnDisposed(DisposeManager.DestroyContext destroyContext)
        {
            if (base.OnDisposed(destroyContext))
            {
                if (_meshRendererVisualModifiers != null)
                {
                    foreach (MeshRendererVisualModifier meshRendererVisualModifier in _meshRendererVisualModifiers)
                        DisposeManager.Dispose(meshRendererVisualModifier);
                }

                return true;
            }
            return false;
        }
    }
}
