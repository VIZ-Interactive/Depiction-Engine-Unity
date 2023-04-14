// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace DepictionEngine
{
    /// <summary>
    /// A <see cref="DepictionEngine.Visual"/> containing a MeshRenderer.
    /// </summary>
    [AddComponentMenu(SceneManager.NAMESPACE + "/Object/MeshRendererVisual/" + nameof(MeshRendererVisual))]
    public class MeshRendererVisual : Visual
    {
        /// <summary>
        /// The different types of colliders. <br/><br/>
        /// <b><see cref="None"/>:</b> <br/>
        /// No Collider. <br/><br/>
        /// <b><see cref="Box"/>:</b> <br/>
        /// A box-shaped primitive collider. <br/><br/>
        /// <b><see cref="Mesh"/>:</b> <br/>
        /// A collider that conforms to the shape of the mesh.
        /// </summary> 
        public enum ColliderType
        {
            None,
            Box,
            Mesh
        }

        private MeshRenderer _meshRenderer;
        private MeshFilter _meshFilter;

        private ColliderType _colliderType;
        private Collider _colliderInternal;

        public override void Recycle()
        {
            base.Recycle();

            sharedMesh = default;
            sharedMaterial = default;

            colliderType = default;
        }

        protected override void InitializeFields(InitializationContext initializingContext)
        {
            base.InitializeFields(initializingContext);
            
            InitMeshFilter();
            InitMeshRenderer();
            InitCollider();
        }

        private bool InitMeshFilter()
        {
            if (IsDisposing())
                return false;

            if (_meshFilter == null)
            {
                if (!gameObject.TryGetComponent<MeshFilter>(out _meshFilter))
                    _meshFilter = gameObject.AddComponent<MeshFilter>();
                return true;
            }
            return false;
        }

        private bool InitMeshRenderer()
        {
            if (IsDisposing())
                return false;

            if (_meshRenderer == null)
            {
                if (!gameObject.TryGetComponent<MeshRenderer>(out _meshRenderer))
                    _meshRenderer = gameObject.AddComponent<MeshRenderer>();
                return true;
            }
            return false;
        }

        private void InitCollider()
        {
            if (gameObject.TryGetComponent<Collider>(out _colliderInternal))
            {
                if (_colliderInternal is BoxCollider)
                    _colliderType = ColliderType.Box;
                if (_colliderInternal is MeshCollider)
                    _colliderType = ColliderType.Mesh;
            }
            else
                _colliderType = ColliderType.None;
        }

        protected override void InitializeSerializedFields(InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            InitValue(value => shadowCastingMode = value, GetDefaultShadowCastingMode(), initializingContext);
            InitValue(value => receiveShadows = value, GetDefaultReceiveShadows(), initializingContext);
        }

        protected override bool SetVisualObject(VisualObject oldValue, VisualObject newValue)
        {
            if (base.SetVisualObject(oldValue, newValue))
            {
                RemoveVisualObjectMeshRenderer(oldValue);
                AddVisualObjectMeshRenderer(newValue);
                return true;
            }
            return false;
        }

        private void RemoveVisualObjectMeshRenderer(VisualObject visualObject)
        {
            if (visualObject is not null)
                visualObject.RemoveMeshRenderer(meshRenderer);
        }

        private void AddVisualObjectMeshRenderer(VisualObject visualObject)
        {
            if (visualObject != Disposable.NULL)
                visualObject.AddMeshRenderer(meshRenderer);
        }

        public Bounds bounds
        {
            get => meshFilter.sharedMesh != null ? meshFilter.sharedMesh.bounds : new Bounds();
        }

        public int vertexCount
        {
            get => meshFilter.sharedMesh != null ? meshFilter.sharedMesh.vertexCount : 0;
        }

        protected virtual ShadowCastingMode GetDefaultShadowCastingMode()
        {
            return ShadowCastingMode.On;
        }

        protected virtual bool GetDefaultReceiveShadows()
        {
            return true;
        }

        public ShadowCastingMode shadowCastingMode
        {
            get => _meshRenderer.shadowCastingMode;
            set => _meshRenderer.shadowCastingMode = value;
        }

        public bool receiveShadows
        {
            get => _meshRenderer.receiveShadows;
            set => _meshRenderer.receiveShadows = value;
        }

        public MeshRenderer meshRenderer
        {
            get 
            {
                InitMeshRenderer();
                return _meshRenderer; 
            }
            private set
            {
                if (Object.ReferenceEquals(_meshRenderer, value))
                    return;
                _meshRenderer = value;
            }
        }

        public MeshFilter meshFilter
        {
            get 
            {
                InitMeshFilter();
                return _meshFilter; 
            }
            private set
            {
                if (Object.ReferenceEquals(_meshFilter, value))
                    return;
                _meshFilter = value;
            }
        }

        public Material material
        {
            get => meshRenderer.material; 
            set => meshRenderer.material = value; 
        }

        public Material sharedMaterial
        {
            get => meshRenderer.sharedMaterial; 
            set => meshRenderer.sharedMaterial = value; 
        }

        public UnityEngine.Mesh mesh
        {
            get => meshFilter.mesh; 
            set => meshFilter.mesh = value; 
        }

        public UnityEngine.Mesh sharedMesh
        {
            get => meshFilter.sharedMesh;
            set 
            {
                UnityEngine.Mesh oldValue = sharedMesh;
                UnityEngine.Mesh newValue = value;

                if (newValue != null && newValue.vertices.Length == 0)
                    newValue = null;

                if (HasChanged(newValue, oldValue, false))
                {
                    meshFilter.sharedMesh = newValue;

                    Collider collider = colliderInternal;
                    if (collider != null)
                    {
                        if (collider is MeshCollider)
                            (collider as MeshCollider).sharedMesh = newValue;
                        UpdateColliderProperties();
                    }
                }
            }
        }

        private void UpdateColliderProperties()
        {
            if (IsDisposing())
                return;

            if (_colliderInternal != null)
            {
                if (_colliderInternal is BoxCollider)
                {
                    BoxCollider boxCollider = _colliderInternal as BoxCollider;
                    if (sharedMesh != null)
                    {
                        boxCollider.center = sharedMesh.bounds.center;
                        boxCollider.size = sharedMesh.bounds.size;
                    }
                    else
                    {
                        boxCollider.center = Vector3.zero;
                        boxCollider.size = Vector3.zero;
                    }
                }
                if (_colliderInternal is MeshCollider)
                {
                    MeshCollider meshCollider = _colliderInternal as MeshCollider;
                    if (meshCollider.sharedMesh != null)
                    {
                        lock (meshCollider) 
                        { 
                            //TODO: This crashes Unity sometimes and should be fixed somehow
                            //Hack to force MeshCollider Update
                            bool lastColliderEnabled = meshCollider.enabled;
                            meshCollider.enabled = false;
                            meshCollider.enabled = true;
                            meshCollider.enabled = lastColliderEnabled;
                        }
                    }
                }
            }
        }

        public ColliderType colliderType
        {
            get => _colliderType;
            private set => SetColliderType(value);
        }

        public virtual bool SetColliderType(ColliderType value)
        {
            if (_colliderType == value)
                return false;
           
            _colliderType = value;
            
            colliderInternal = _colliderType != ColliderType.None ? gameObject.AddComponent(_colliderType == ColliderType.Box ? typeof(BoxCollider) : typeof(MeshCollider)) as Collider : null;
            
            return true;
        }

        public Collider GetCollider()
        {
            return _colliderInternal;
        }

        private Collider colliderInternal
        {
            get 
            {
                InitCollider();
                return _colliderInternal; 
            }
            set 
            {
                if (Object.ReferenceEquals(_colliderInternal, value))
                    return;

                DisposeManager.Dispose(_colliderInternal);

                _colliderInternal = value;

                UpdateColliderProperties();
            }
        }

        public static MeshRendererVisualModifier CreateMeshRendererVisualModifier(string name = null)
        {
            MeshRendererVisualModifier meshRendererVisualModifier = InstanceManager.Instance(false)?.CreateInstance<MeshRendererVisualModifier>();
            meshRendererVisualModifier.name = name;

            return meshRendererVisualModifier;
        }

        public void OnMouseMoveHit(RaycastHitDouble hit)
        {
            if (visualObject != Disposable.NULL)
                visualObject.OnMouseMoveHit(hit);
        }

        public void OnMouseClickedHit(RaycastHitDouble hit)
        {
            if (visualObject != Disposable.NULL)
                visualObject.OnMouseClickedHit(hit);
        }

        public void OnMouseUpHit(RaycastHitDouble hit)
        {
            if (visualObject != Disposable.NULL)
                visualObject.OnMouseUpHit(hit);
        }

        public void OnMouseDownHit(RaycastHitDouble hit)
        {
            if (visualObject != Disposable.NULL)
                visualObject.OnMouseDownHit(hit);
        }

        public void OnMouseEnterHit(RaycastHitDouble hit)
        {
            if (visualObject != Disposable.NULL)
                visualObject.OnMouseEnterHit(hit);
        }

        public void OnMouseExitHit(RaycastHitDouble hit)
        {
            if (visualObject != Disposable.NULL)
                visualObject.OnMouseExitHit(hit);
        }

        public override bool OnDispose(DisposeContext disposeContext)
        {
            if (base.OnDispose(disposeContext))
            {
                RemoveVisualObjectMeshRenderer(visualObject);

#if UNITY_EDITOR
                //When undoing an Add Component(such as Add GeoCoordinateController) on a VisualObject(tested on Markers) the children whose creation was not registered with the UndoManager are disposed automatically for some reason.
                //If we detect that the visuals were disposed as a result of an Undo Redo we ask the AutoGenerateVisualObject to recreate them. If it was the AutoGenerateVisualObject that was destroyed, and not just its child visuals, then the visualObject will be null and nothing will happen.
                if (disposeContext == DisposeContext.Editor_UndoRedo)
                    (visualObject as AutoGenerateVisualObject)?.SetMeshRendererVisualsDirty();
#endif
                return true;
            }
            return false;
        }
    }

    public class MeshRendererVisualProcessorOutput : ProcessorOutput
    {
        public MeshModifier _meshModifier;

        public override void Initializing()
        {
            base.Initializing();

            _meshModifier = Mesh.CreateMeshModifier();
        }

        public MeshModifier meshModifier
        {
            get { return _meshModifier; }
            set
            {
                if (Object.ReferenceEquals(_meshModifier, value))
                    return;

                _meshModifier = value;
            }
        }

        public override bool OnDispose(DisposeContext disposeContext)
        {
            if (base.OnDispose(disposeContext))
            {
                DisposeManager.Dispose(_meshModifier);

                return true;
            }
            return false;
        }
    }

    [Serializable]
    public class MeshRendererVisualModifier : PropertyModifier
    {
        [SerializeField]
        private string _name;

        private Mesh _sharedMesh;

        [SerializeField]
        private MeshModifier _meshModifier;

        private Type _typeNoCollider;
        private Type _typeBoxCollider;
        private Type _typeMeshCollider;

        public override void Recycle()
        {
            base.Recycle();

            _name = default;

            _typeNoCollider = default;
            _typeBoxCollider = default;
            _typeMeshCollider = default;
        }

        public Type typeNoCollider
        { 
            get => _typeNoCollider;
        }

        public Type typeBoxCollider
        {
            get => _typeBoxCollider;
        }

        public Type typeMeshCollider
        {
            get => _typeMeshCollider;
        }

        public Type GetMeshType()
        {
            return _meshModifier != Disposable.NULL ? _meshModifier.GetMeshType() : typeof(Mesh);
        }

        public void SetTypes(Type noCollider, Type boxCollider, Type meshCollider)
        {
            _typeNoCollider = noCollider;
            _typeBoxCollider = boxCollider;
            _typeMeshCollider = meshCollider;
        }

        public string name
        {
            get => _name;
            set => _name = value;
        }

        public MeshModifier CreateMeshModifier()
        {
            return CreateMeshModifier<MeshModifier>();
        }

        public MeshModifier CreateMeshModifier<T>() where T : MeshModifier
        {
            meshModifier = Mesh.CreateMeshModifier<T>();
            return meshModifier;
        }

        public MeshModifier meshModifier
        {
            get { return _meshModifier; }
            set
            {
                if (Object.ReferenceEquals(_meshModifier, value))
                    return;

                DisposeManager.Dispose(_meshModifier);

                _meshModifier = value;
            }
        }

        public Mesh sharedMesh
        {
            get { return _sharedMesh; }
            set 
            {
                if (_sharedMesh == value)
                    return;

                _sharedMesh = value;
            }
        }

        public override void ModifyProperties(IScriptableBehaviour scriptableBehaviour)
        {
            base.ModifyProperties(scriptableBehaviour);

            if (scriptableBehaviour is MeshRendererVisual)
            {
                MeshRendererVisual meshRendererVisual = scriptableBehaviour as MeshRendererVisual;

                meshRendererVisual.name = !string.IsNullOrEmpty(name) ? name : meshRendererVisual.GetType().Name;

                meshRendererVisual.sharedMesh = sharedMesh.unityMesh;
            }
        }

        public bool ApplyMeshModifierToMesh(Mesh mesh)
        {
            if (meshModifier != Disposable.NULL)
            {
                meshModifier.ModifyProperties(mesh);
                return true;
            }
            return false;
        }

        public Processor meshModifierProcessor
        {
            get { return _meshModifierProcessor; }
            private set
            {
                if (Object.ReferenceEquals(_meshModifierProcessor, value))
                    return;

                _meshModifierProcessor?.Cancel();

                _meshModifierProcessor = value;
            }
        }

        private Processor _meshModifierProcessor;
        public void StartProcessing(Func<ProcessorOutput, ProcessorParameters, IEnumerator> processingFunction, Type parametersType, Action<ProcessorParameters> parametersCallback = null, Processor.ProcessingType processingType = Processor.ProcessingType.AsyncTask, Action<MeshRendererVisualProcessorOutput> processingCompleted = null)
        {
            if (processingFunction != null)
            {
                meshModifierProcessor ??= InstanceManager.Instance(false)?.CreateInstance<Processor>();
                
                meshModifierProcessor.StartProcessing(processingFunction, typeof(MeshRendererVisualProcessorOutput), parametersType, parametersCallback,
                    (data, errorMsg) =>
                    {
                        processingCompleted?.Invoke(data as MeshRendererVisualProcessorOutput);
                    }, processingType);
            }
        }

        public void DisposeMeshModifier()
        {
            meshModifier = null;
        }

        public void DisposeMeshModifierProcessorIfProcessingWasCompromised()
        {
            if (meshModifier != Disposable.NULL && meshModifierProcessor != null && meshModifierProcessor.ProcessingWasCompromised())
                meshModifierProcessor.Dispose();
        }

        public override bool OnDispose(DisposeContext disposeContext)
        {
            if (base.OnDispose(disposeContext))
            {
                _meshModifierProcessor?.Dispose();

                DisposeMeshModifier();

                return true;
            }
            return false;
        }
    }
}