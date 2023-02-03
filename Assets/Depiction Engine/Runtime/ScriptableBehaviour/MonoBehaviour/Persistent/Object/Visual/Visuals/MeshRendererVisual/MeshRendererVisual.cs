// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace DepictionEngine
{
    [AddComponentMenu(SceneManager.NAMESPACE + "/Object/Visual/" + nameof(MeshRendererVisual))]
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
           
            sharedMaterial = null;
            sharedMesh = null;
            colliderType = ColliderType.None;
        }

        protected override void InitializeFields(InstanceManager.InitializationContext initializingState)
        {
            base.InitializeFields(initializingState);
            
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
                _meshFilter = gameObject.GetComponent<MeshFilter>();
                if (_meshFilter == null)
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
                _meshRenderer = gameObject.GetComponent<MeshRenderer>();
                if (_meshRenderer == null)
                    _meshRenderer = gameObject.AddComponent<MeshRenderer>();
                return true;
            }
            return false;
        }

        private void InitCollider()
        {
            if (_colliderInternal == null)
                _colliderInternal = gameObject.GetComponent<Collider>();

            if (_colliderInternal != null)
            {
                if (_colliderInternal is BoxCollider)
                    _colliderType = ColliderType.Box;
                if (_colliderInternal is MeshCollider)
                    _colliderType = ColliderType.Mesh;
            }
            else
                _colliderType = ColliderType.None;
        }

        protected override void InitializeSerializedFields(InstanceManager.InitializationContext initializingState)
        {
            base.InitializeSerializedFields(initializingState);

            InitValue(value => shadowCastingMode = value, GetDefaultShadowCastingMode(), initializingState);
            InitValue(value => receiveShadows = value, GetDefaultReceiveShadows(), initializingState);
        }

        protected override bool SetVisualObject(VisualObject oldValue, VisualObject newValue)
        {
            if (base.SetVisualObject(oldValue, newValue))
            {
                if (!Object.ReferenceEquals(oldValue, null))
                    oldValue.RemoveMeshRenderer(meshRenderer);
                if (newValue != null)
                    newValue.AddMeshRenderer(meshRenderer);
                return true;
            }
            return false;
        }

        public Bounds bounds
        {
            get { return meshFilter.sharedMesh != null ? meshFilter.sharedMesh.bounds : new Bounds(); }
        }

        public int vertexCount
        {
            get { return meshFilter.sharedMesh != null ? meshFilter.sharedMesh.vertexCount : 0; }
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
            get { return _meshRenderer.shadowCastingMode; }
            set { _meshRenderer.shadowCastingMode = value; }
        }

        public bool receiveShadows
        {
            get { return _meshRenderer.receiveShadows; }
            set { _meshRenderer.receiveShadows = value; }
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
            get { return meshRenderer.material; }
            set { meshRenderer.material = value; }
        }

        public Material sharedMaterial
        {
            get { return meshRenderer.sharedMaterial; }
            set { meshRenderer.sharedMaterial = value; }
        }

        public UnityEngine.Mesh mesh
        {
            get { return meshFilter.mesh; }
            set { meshFilter.mesh = value; }
        }

        public UnityEngine.Mesh sharedMesh
        {
            get { return meshFilter.sharedMesh; }
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
            get { return _colliderType; }
            private set { SetColliderType(value); }
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

                Dispose(_colliderInternal);

                _colliderInternal = value;

                UpdateColliderProperties();
            }
        }

        public static MeshRendererVisualModifier CreateMeshRendererVisualModifier(string name = null)
        {
            MeshRendererVisualModifier meshRendererVisualModifier = InstanceManager.Instance(false).CreateInstance<MeshRendererVisualModifier>();
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
    }

    public class MeshRendererVisualProcessorOutput : ProcessorOutput
    {
        public MeshModifier _meshModifier;

        public override bool Initialize()
        {
            if (base.Initialize())
            {
                _meshModifier = Mesh.CreateMeshModifier();

                return true;
            }
            return false;
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

        protected override bool OnDisposed(DisposeManager.DestroyContext destroyState)
        {
            if (base.OnDisposed(destroyState))
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

            _name = null;

            _typeNoCollider = null;
            _typeBoxCollider = null;
            _typeMeshCollider = null;
        }

        public Type typeNoCollider
        { 
            get { return _typeNoCollider; }
        }

        public Type typeBoxCollider
        {
            get { return _typeBoxCollider; }
        }

        public Type typeMeshCollider
        {
            get { return _typeMeshCollider; }
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
            get { return _name; }
            set { _name = value; }
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

                if (_meshModifierProcessor != null)
                    _meshModifierProcessor.Cancel();

                _meshModifierProcessor = value;
            }
        }

        private Processor _meshModifierProcessor;
        public void StartProcessing(Func<ProcessorOutput, ProcessorParameters, IEnumerator> processingFunction, Type parametersType, Action<ProcessorParameters> parametersCallback = null, Processor.ProcessingType processingType = Processor.ProcessingType.AsyncTask, Action<MeshRendererVisualProcessorOutput> processingCompleted = null)
        {
            if (processingFunction != null)
            {
                if (meshModifierProcessor == null)
                    meshModifierProcessor = InstanceManager.Instance(false).CreateInstance<Processor>();
                
                meshModifierProcessor.StartProcessing(processingFunction, typeof(MeshRendererVisualProcessorOutput), parametersType, parametersCallback,
                    (data, errorMsg) =>
                    {
                        if (processingCompleted != null)
                            processingCompleted(data as MeshRendererVisualProcessorOutput);
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

        public override bool OnDispose()
        {
            if (base.OnDispose())
            {
                if (_meshModifierProcessor != null)
                    _meshModifierProcessor.Dispose();
             
                return true;
            }
            return false;
        }

        protected override bool OnDisposed(DisposeManager.DestroyContext destroyState)
        {
            if (base.OnDisposed(destroyState))
            {
                sharedMesh = null;

                DisposeMeshModifier();

                return true;
            }
            return false;
        }
    }
}