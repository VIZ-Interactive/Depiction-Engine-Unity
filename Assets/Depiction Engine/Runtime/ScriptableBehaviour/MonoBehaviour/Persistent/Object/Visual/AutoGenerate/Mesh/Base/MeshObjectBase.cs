﻿// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace DepictionEngine
{
    public class MeshObjectBase : AutoGenerateVisualObject, IProcessing
    {
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

        [SerializeField]
#if UNITY_EDITOR
        [ConditionalShow(nameof(GetShowDebug))]
#endif
        private List<Mesh> _meshes;
        [SerializeField]
#if UNITY_EDITOR
        [ConditionalShow(nameof(GetShowDebug))]
#endif
        private List<bool> _isSharedMeshFlags;

        private List<MeshRendererVisualModifier> _meshRendererVisualModifiers;

        private List<MeshParameters> _meshParameters;

        private Processor _meshDataProcessor;

        private Action _processingCompletedEvent;

        public override void Recycle()
        {
            base.Recycle();

            _meshes?.Clear();
            _isSharedMeshFlags?.Clear();

            _meshParameters = default;
        }

        protected override void InitializeSerializedFields(InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            if (initializingContext == InitializationContext.Editor_Duplicate || initializingContext == InitializationContext.Programmatically_Duplicate)
            {
                _meshes?.Clear();
                _isSharedMeshFlags?.Clear();
            }

            InitValue(value => useCollider = value, GetDefaultUseCollider(), initializingContext);
            InitValue(value => convexCollider = value, GetDefaultConvexCollider(), initializingContext);
            InitValue(value => castShadow = value, GetDefaultCastShadow(), initializingContext);
            InitValue(value => receiveShadows = value,GetDefaultReceiveShadows(), initializingContext);
        }

        public override void Initialized(InitializationContext initializingContext)
        {
            base.Initialized(initializingContext);
            
            DetectIfProcessingWasCompromised();
        }

        protected override bool GetDefaultOverrideMaterialFields()
        {
            return true;
        }

#if UNITY_EDITOR
        public override bool AfterAssemblyReload()
        {
            if (base.AfterAssemblyReload())
            {
                DetectIfProcessingWasCompromised();

                return true;
            }
            return false;
        }
#endif

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

        private void DetectIfProcessingWasCompromised()
        {
            if (meshDataProcessor != null && meshDataProcessor.ProcessingWasCompromised())
                meshDataProcessor.Dispose();
        }

        /// <summary>
        /// When enabled the <see cref="DepictionEngine.MeshRendererVisual"/>'s will have collider component.
        /// </summary>
        [Json]
        public bool useCollider
        {
            get => _useCollider;
            set => SetValue(nameof(useCollider), value, ref _useCollider);
        }

        /// <summary>
        /// When enabled the MeshCollider's will have 'MeshCollider.convex' set to true.
        /// </summary>
        [Json]
        public bool convexCollider
        {
            get => _convexCollider;
            set => SetValue(nameof(convexCollider), value, ref _convexCollider);
        }

        /// <summary>
        /// When enabled the meshes 'MeshRenderer' will have 'Renderer.shadowCastingMode' set to 'ShadowCastingMode.On', otherwise it will be 'ShadowCastingMode.Off'.
        /// </summary>
        [Json]
        public bool castShadow
        {
            get => _castShadow;
            set => SetValue(nameof(castShadow), value, ref _castShadow);
        }

        /// <summary>
        /// When enabled the meshes 'MeshRenderer' will have 'Renderer.receiveShadows' set to this value.
        /// </summary>
        [Json]
        public bool receiveShadows
        {
            get => _receiveShadows;
            set => SetValue(nameof(receiveShadows), value, ref _receiveShadows);
        }

        protected override void DontSaveVisualsToSceneChanged(bool newValue, bool oldValue)
        {
            base.DontSaveVisualsToSceneChanged(newValue, oldValue);

            UpdateMeshesDontSaveToScene();
        }

        private void UpdateMeshesDontSaveToScene()
        {
            for (int i = meshes.Count - 1; i >= 0; i--)
            {
                if (!isSharedMeshFlags[i])
                    meshes[i].dontSaveToScene = dontSaveVisualsToScene;
            }
        }

        /// <summary>
        /// Dispatched after the <see cref="DepictionEngine.MeshObjectBase.meshDataProcessor"/> as completed modifying the <see cref="DepictionEngine.Mesh"/> that will be displayed by the child <see cref="DepictionEngine.MeshRendererVisual"/>.
        /// </summary>
        public Action ProcessingCompletedEvent
        {
            get => _processingCompletedEvent;
            set => _processingCompletedEvent = value;
        }

        protected Processor meshDataProcessor
        {
            get => _meshDataProcessor;
            set
            {
                if (Object.ReferenceEquals(_meshDataProcessor, value))
                    return;
                _meshDataProcessor?.Cancel();
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
            get { _meshes ??= new List<Mesh>(); return _meshes; }
        }

        private List<bool> isSharedMeshFlags
        {
            get { _isSharedMeshFlags ??= new List<bool>(); return _isSharedMeshFlags; }
        }

        private List<Mesh> _lastMeshes;
        private List<bool> _lastIsSharedMeshFlags;
        protected override void Saving(Scene scene, string path)
        {
            base.Saving(scene, path);

            if (GetDontSaveVisualsToScene())
            {
                if (_meshes != null)
                {
                    for (int i = _meshes.Count - 1; i >= 0; i--)
                    {
                        if (!isSharedMeshFlags[i])
                            _meshes[i].dontSaveToScene = true;
                    }
                }
                _lastMeshes = _meshes;
                _meshes = null;
                _lastIsSharedMeshFlags = _isSharedMeshFlags;
                _isSharedMeshFlags = null;
            }
        }

        protected override void Saved(Scene scene)
        {
            base.Saved(scene);

            if (GetDontSaveVisualsToScene())
            {
                _meshes = _lastMeshes;
                _isSharedMeshFlags = _lastIsSharedMeshFlags;
                if (_meshes != null)
                {
                    for (int i = _meshes.Count - 1; i >= 0; i--)
                        _meshes[i].dontSaveToScene = false;
                }
            }
        }

        public List<MeshRendererVisualModifier> meshRendererVisualModifiers
        {
            get { _meshRendererVisualModifiers ??= new List<MeshRendererVisualModifier>(); return _meshRendererVisualModifiers; }
            set 
            {
                if (Object.ReferenceEquals(_meshRendererVisualModifiers, value))
                    return;

                if (_meshRendererVisualModifiers != null)
                {
                    foreach (MeshRendererVisualModifier meshRendererVisualModifier in _meshRendererVisualModifiers)
                        DisposeManager.Dispose(meshRendererVisualModifier);
                    _meshRendererVisualModifiers.Clear();
                }

                _meshRendererVisualModifiers = value;
            }
        }

        protected virtual bool EnableRecalculateNormals()
        {
            return true;
        }

        protected Type GetMeshRendererType(MeshRendererVisual.ColliderType colliderType, Type meshRendererVisualNoCollider, Type meshRendererVisualBoxCollider, Type meshRendererVisualMeshCollider)
        {
            return colliderType == MeshRendererVisual.ColliderType.None ? meshRendererVisualNoCollider : colliderType == MeshRendererVisual.ColliderType.Box ? meshRendererVisualBoxCollider : meshRendererVisualMeshCollider;
        }

        protected List<Mesh> GetMeshFromCache(VisualObjectVisualDirtyFlags meshRendererVisualDirtyFlags)
        {
            return renderingManager.GetMeshesFromCache(GetCacheHash(meshRendererVisualDirtyFlags));
        }

        private bool AddMeshToCache(VisualObjectVisualDirtyFlags meshRendererVisualDirtyFlags, List<Mesh> meshes)
        {
            return renderingManager.AddMeshesToCache(GetCacheHash(meshRendererVisualDirtyFlags), meshes);
        }

        protected virtual int GetCacheHash(VisualObjectVisualDirtyFlags meshRendererVisualDirtyFlags)
        {
            return RenderingManager.DEFAULT_MISSING_CACHE_HASH;
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
                if (meshRendererVisualModifier.mesh != Disposable.NULL)
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

                    MeshRendererVisual meshRendererVisual = GetVisual(i) as MeshRendererVisual;
                    if (meshRendererVisual == Disposable.NULL)
                        meshRendererVisual = CreateMeshRendererVisual(meshRendererVisualDirtyFlags.colliderType, meshRendererVisualModifier);
                    else
                        meshRendererVisualModifier.ModifyProperties(meshRendererVisual);

                    meshRendererVisualModifier.mesh = null;
                }

                meshRendererVisualModifiers = null;
            }

            transform.IterateOverChildren<MeshRendererVisual>((meshRendererVisual) =>
            {
                Mesh mesh = GetMeshFromUnityMesh(meshRendererVisual.sharedMesh, false);
                if (mesh != Disposable.NULL && mesh.NormalsDirty() && EnableRecalculateNormals())
                    mesh.RecalculateNormals();
                return true;
            });

            if (meshDataProcessor == null || meshDataProcessor.processingState != Processor.ProcessingState.Processing)
            {
                Mesh.PhysicsBakeMeshType physicsBakeMesh = useCollider ? convexCollider ? Mesh.PhysicsBakeMeshType.Convex : Mesh.PhysicsBakeMeshType.Concave : Mesh.PhysicsBakeMeshType.None;
                transform.IterateOverChildren<MeshRendererVisual>((meshRendererVisual) =>
                {
                    Mesh mesh = GetMeshFromUnityMesh(meshRendererVisual.sharedMesh, false);
                    if (mesh != Disposable.NULL && mesh.PhysicsBakedMeshDirty(physicsBakeMesh) && mesh.vertexCount > 0)
                    {
                        _meshParameters ??= new List<MeshParameters>();
                        _meshParameters.Add(new MeshParameters(mesh, physicsBakeMesh));
                    }
                    return true;
                });

                if (_meshParameters != null)
                {
                    InstanceManager instanceManager = InstanceManager.Instance(false);
                    if (instanceManager != null)
                        meshDataProcessor ??= instanceManager.CreateInstance<Processor>();

                    meshDataProcessor.StartProcessing(MeshProcessingFunctions.ApplyPhysicsBakeMesh, typeof(MeshesProcessorOutput), typeof(MeshesParameters), 
                        (parameters) => 
                        { 
                            (parameters as MeshesParameters).Init(_meshParameters); 
                        },
                        (data, errorMsg) => 
                        {
                            MeshesProcessorOutput meshesProcessorOutput = data as MeshesProcessorOutput;
                            transform.IterateOverChildren<MeshRendererVisual>((meshRendererVisual) =>
                            {
                                if (meshesProcessorOutput.Contains(meshRendererVisual))
                                    meshRendererVisual.UpdateCollider();

                                return true;
                            });

                            if (string.IsNullOrEmpty(errorMsg))
                                ProcessingCompletedEvent?.Invoke();

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
            Type typeNoCollider = meshRendererVisualModifier.typeNoCollider ?? typeof(MeshRendererVisualNoCollider);
            Type typeBoxCollider = meshRendererVisualModifier.typeBoxCollider ?? typeof(MeshRendererVisualBoxCollider);
            Type typeMeshCollider = meshRendererVisualModifier.typeMeshCollider ?? typeof(MeshRendererVisualMeshCollider);
            Type type = GetMeshRendererType(colliderType, typeNoCollider, typeBoxCollider, typeMeshCollider);
  
            return CreateVisual(type, meshRendererVisualModifier.name, null, new List<PropertyModifier>() { meshRendererVisualModifier }) as MeshRendererVisual;
        }

        public Processor meshRendererVisualModifiersProcessor
        {
            get => _meshRendererVisualModifiersProcessor;
            private set
            {
                if (Object.ReferenceEquals(_meshRendererVisualModifiersProcessor, value))
                    return;

                _meshRendererVisualModifiersProcessor?.Cancel();

                _meshRendererVisualModifiersProcessor = value;
            }
        }

        private Processor _meshRendererVisualModifiersProcessor;
        protected virtual void UpdateMeshRendererVisualModifiers(Action<List<MeshRendererVisualModifier>> completedCallback, VisualObjectVisualDirtyFlags meshRendererVisualDirtyFlags)
        {
            if (meshRendererVisualDirtyFlags != null)
            {
                List<Mesh> meshes = GetMeshFromCache(meshRendererVisualDirtyFlags);

                if (meshes != null)
                {
                    List<MeshRendererVisualModifier> meshRendererVisualModifiers = new List<MeshRendererVisualModifier>();

                    foreach (Mesh mesh in meshes)
                    {
                        MeshRendererVisualModifier meshRendererVisualModifier = MeshRendererVisual.CreateMeshRendererVisualModifier();
                        
                        meshRendererVisualModifier.isSharedMesh = true;
                        meshRendererVisualModifier.mesh = mesh;

                        meshRendererVisualModifiers.Add(meshRendererVisualModifier);
                    }

                    completedCallback?.Invoke(meshRendererVisualModifiers);
                }
                else 
                { 
                    Func<ProcessorOutput, ProcessorParameters, IEnumerator> processorFunction = GetProcessorFunction();

                    if (processorFunction != null)
                    {
                        InstanceManager instanceManager = InstanceManager.Instance(false);
                        if (instanceManager != null)
                            meshRendererVisualModifiersProcessor ??= instanceManager.CreateInstance<Processor>();

                        meshRendererVisualDirtyFlags.SetProcessing(true, meshRendererVisualModifiersProcessor);

                        meshRendererVisualModifiersProcessor.StartProcessing(processorFunction, typeof(MeshObjectProcessorOutput), GetProcessorParametersType(), InitializeProcessorParameters,
                            (data, errorMsg) =>
                            {
                                meshRendererVisualDirtyFlags.SetProcessing(false);

                                MeshObjectProcessorOutput meshObjectProcessorOutput = data as MeshObjectProcessorOutput;
                                if (meshObjectProcessorOutput != Disposable.NULL)
                                {
                                    List<MeshRendererVisualModifier> meshRendererVisualModifiers = meshObjectProcessorOutput.meshRendererVisualModifiers;

                                    for (int index = 0; index < meshRendererVisualModifiers.Count; index++)
                                    {
                                        Mesh mesh = index < this.meshes.Count && !isSharedMeshFlags[index] ? this.meshes[index] : null;

                                        MeshRendererVisualModifier meshRendererVisualModifier = meshRendererVisualModifiers[index];

                                        Type meshType = meshRendererVisualModifier.GetMeshType();
                                        if (mesh == Disposable.NULL || mesh.GetType() != meshType)
                                            mesh = Mesh.CreateMesh(meshType);

                                        meshRendererVisualModifier.mesh = ModifyMesh(meshRendererVisualModifier, mesh);

                                        if (meshes == null)
                                            meshes = new();
                                        meshes.Add(meshRendererVisualModifier.mesh);
                                    }

                                    bool sharedMeshes = AddMeshToCache(meshRendererVisualDirtyFlags, meshes);
                                    foreach (MeshRendererVisualModifier meshRendererVisualModifier in meshRendererVisualModifiers)
                                        meshRendererVisualModifier.isSharedMesh = sharedMeshes;

                                    meshObjectProcessorOutput.Clear();

                                    completedCallback?.Invoke(meshRendererVisualModifiers);
                                }

                            }, GetProcessingType(meshRendererVisualDirtyFlags));
                    }
                }
            }
        }

        protected virtual Func<ProcessorOutput, ProcessorParameters, IEnumerator> GetProcessorFunction()
        {
            return null;
        }

        protected virtual Type GetProcessorParametersType()
        {
            return null;
        }

        protected virtual void InitializeProcessorParameters(ProcessorParameters parameters)
        {

        }

        private void UpdateMeshRendererVisualModifiersMesh(List<MeshRendererVisualModifier> meshRendererVisualModifiers)
        {
            if (IsDisposing())
                return;

            for (int i = 0; i < meshRendererVisualModifiers.Count; i++)
                UpdateMeshAtIndex(i, meshRendererVisualModifiers[i].mesh, meshRendererVisualModifiers[i].isSharedMesh);

            this.meshRendererVisualModifiers = meshRendererVisualModifiers;
        }

        protected virtual Mesh ModifyMesh(MeshRendererVisualModifier meshRendererVisualModifier, Mesh mesh, bool disposeMeshModifier = true)
        {
            if (meshRendererVisualModifier.ApplyMeshModifierToMesh(mesh))
            {
                if (disposeMeshModifier)
                    meshRendererVisualModifier.DisposeMeshModifier();
            }

            return mesh;
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
                if (visualsChanged || PropertyDirty(nameof(castShadow)) || PropertyDirty(nameof(color)))
                    ApplyCastShadowToMeshRendererVisual(meshRendererVisual, castShadow ? ShadowCastingMode.On : ShadowCastingMode.Off);
                
                if (visualsChanged || PropertyDirty(nameof(receiveShadows)))
                    meshRendererVisual.receiveShadows = receiveShadows;

                UpdateMeshRendererVisualCollider(meshRendererVisual, meshRendererVisualDirtyFlags, convexCollider, visualsChanged, PropertyDirty(nameof(convexCollider)));

                return true;
            });
        }

        private void DisposeMesh(int index, DisposeContext disposeContext = DisposeContext.Programmatically_Pool)
        {
            if (_isSharedMeshFlags != null && !_isSharedMeshFlags[index] && _meshes != null)
                DisposeManager.Dispose(_meshes[index], disposeContext);
        }

        public override bool OnDispose(DisposeContext disposeContext)
        {
            if (base.OnDispose(disposeContext))
            {
                DisposeDataProcessor(_meshRendererVisualModifiersProcessor);
                DisposeDataProcessor(_meshDataProcessor);

                meshRendererVisualModifiers = null;

                ProcessingCompletedEvent = null;

                if (_meshes != null)
                {
                    for (int i = _meshes.Count - 1; i >= 0; i--)
                        DisposeMesh(i, disposeContext);
                    _meshes.Clear();
                }

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

            _meshesParameters?.Clear();
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
            get => _meshesParameters;
        }

        public IEnumerable ApplyPhysicsBakeMeshToMesh(MeshesProcessorOutput meshesProcessorOutput)
        {
            foreach (MeshParameters meshParameters in meshesParameters)
            {
                if (meshParameters.mesh.PhysicsBakeMesh(meshParameters.physicsBakeMesh))
                    meshesProcessorOutput.Add(meshParameters.mesh.unityMesh);
            }

            yield break;
        }
    }

    /// <summary>
    /// A list of Mesh used by <see cref="DepictionEngine.MeshProcessingFunctions"/>.
    /// </summary>
    public class MeshesProcessorOutput : ProcessorOutput
    {
        private List<UnityEngine.Mesh> _meshes;

        public override void Recycle()
        {
            base.Recycle();

            _meshes?.Clear();
        }

        public void Add(UnityEngine.Mesh mesh)
        {
            _meshes ??= new();
            _meshes.Add(mesh);
        }

        public bool Contains(MeshRendererVisual meshRendererVisual)
        {
            return _meshes != null && _meshes.Contains(meshRendererVisual.sharedMesh);
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
        public static IEnumerator ApplyPhysicsBakeMesh(object data, ProcessorParameters parameters)
        {
            foreach (object enumeration in ApplyPhysicsBakeMesh(data, parameters as MeshesParameters))
                yield return enumeration;
        }

        protected static IEnumerable ApplyPhysicsBakeMesh(object data, MeshesParameters parameters)
        {
            foreach (object enumeration in parameters.ApplyPhysicsBakeMeshToMesh(data as MeshesProcessorOutput))
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
            get => meshRendererVisualModifiers.Count == 0 ? null : meshRendererVisualModifiers[^1];
        }

        public int meshRendererVisualModifiersCount
        {
            get => meshRendererVisualModifiers.Count;
        }

        public List<MeshRendererVisualModifier> meshRendererVisualModifiers
        {
            get { _meshRendererVisualModifiers ??= new List<MeshRendererVisualModifier>(); return _meshRendererVisualModifiers; }
            private set => _meshRendererVisualModifiers = value;
        }

        public void AddMeshRendererVisualModifier(MeshRendererVisualModifier meshRendererVisualModifier)
        {
            if (meshRendererVisualModifiers.Count > 0)
            {
                FeatureMeshModifier featureMeshModifier = meshRendererVisualModifiers[^1].meshModifier as FeatureMeshModifier;
                if (featureMeshModifier != Disposable.NULL)
                    featureMeshModifier.FeatureComplete();
            }
            meshRendererVisualModifiers.Add(meshRendererVisualModifier);
        }

        public void Clear()
        {
            _meshRendererVisualModifiers = default;
        }

        public override bool OnDispose(DisposeContext disposeContext)
        {
            if (base.OnDispose(disposeContext))
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
