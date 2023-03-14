// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace DepictionEngine
{
    /// <summary>
    /// Wrapper class for 'UnityEngine.Mesh' introducing better integrated functionality.
    /// </summary>
    public class Mesh : AssetBase, IUnityMeshAsset
    {
        /// <summary>
        /// Different physics mesh baking type to be used by the MeshCollider. <br/><br/>
		/// <b><see cref="None"/>:</b> <br/>
        /// Do not bake the geometry. <br/><br/>
        /// <b><see cref="Concave"/>:</b> <br/>
        /// Bake a concave geometry. <br/><br/>
        /// <b><see cref="Convex"/>:</b> <br/>
        /// Bake a convex geometry.
        /// </summary>
        public enum PhysicsBakeMeshType
        {
            None,
            Concave,
            Convex
        }

        private enum NormalsType
        {
            None,
            Manual,
            Auto
        };

        private static readonly List<int> EMPTY_TRIANGLES = new();
        private static readonly List<Vector3> EMPTY_VERTICES = new();

        [BeginFoldout("Mesh")]
        [SerializeField, EndFoldout]
#if UNITY_EDITOR
        [ConditionalShow(nameof(GetShowDataField)), ConditionalEnable(nameof(GetEnableDataField))]
#endif
        private UnityEngine.Mesh _unityMesh;

        [SerializeField, HideInInspector]
        private PhysicsBakeMeshType _physicsBakeMesh;
        [SerializeField, HideInInspector]
        private bool _calculatedBounds;
        [SerializeField, HideInInspector]
        private NormalsType _normalsType;

        [SerializeField, HideInInspector]
        private int _unityMeshInstanceId;

        private List<MeshObjectBase> _meshObjectModifying;
        private bool _modificationPending;

        /// <summary>
        /// Dispatched to signal a <see cref="DepictionEngine.MeshRendererVisualModifier"/> in a <see cref="DepictionEngine.MeshGridMeshObject"/> is about to start modification or right after it finished modifying. 
        /// </summary>
        private Action ModificationPendingChangedEvent;

        public override void Recycle()
        {
            base.Recycle();

            unityMesh?.Clear();

            _physicsBakeMesh = default;
            _calculatedBounds = default;
            _normalsType = default;
        }

        protected override void InitializeSerializedFields(InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            if (initializingContext == InitializationContext.Editor_Duplicate || initializingContext == InitializationContext.Programmatically_Duplicate)
                unityMesh = InstanceManager.Duplicate(unityMesh, initializingContext);
            
            //InstanceId can change between execution
            UpdateUnityMeshInstanceId();
        }

        protected override bool UpdateAllDelegates()
        {
            if (base.UpdateAllDelegates())
            {
                foreach (MeshObjectBase meshObject in meshObjectModifying)
                {
                    RemoveMeshObjectDelegate(meshObject);
                    if (!IsDisposing())
                        AddMeshObjectDelegate(meshObject);
                }

                return true;
            }
            return false;
        }

        private void AddMeshObjectDelegate(MeshObjectBase meshObject)
        {
            meshObject.DisposedEvent += MeshObjectDisposedHandler;
        }

        private void RemoveMeshObjectDelegate(MeshObjectBase meshObject)
        {
            meshObject.DisposedEvent -= MeshObjectDisposedHandler;
        }

        private void MeshObjectDisposedHandler(IDisposable disposable, DisposeContext disposeContext)
        {
            RemoveModifying(disposable as MeshObjectBase);
        }

        private List<MeshObjectBase> meshObjectModifying
        {
            get
            {
                _meshObjectModifying ??= new List<MeshObjectBase>();
                return _meshObjectModifying;
            }
        }

        public bool modificationPending
        {
            get { return _modificationPending; }
            private set
            {
                if (_modificationPending == value)
                    return;
                _modificationPending = value;
                if (ModificationPendingChangedEvent != null)
                {
                    Action modificationPendingChangedEvent = ModificationPendingChangedEvent;
                    ModificationPendingChangedEvent = null;
                    modificationPendingChangedEvent();
                }
            }
        }

        public Bounds bounds
        {
            get { return unityMesh != null ? unityMesh.bounds : new Bounds(); }
            set
            {
                if (unityMesh != null)
                {
                    if (unityMesh.bounds != value)
                    {
                        unityMesh.bounds = value;
                        _calculatedBounds = true;
                    }
                }
            }
        }

        public bool IsEmpty()
        {
            return vertexCount == 0;
        }

        public int vertexCount
        {
            get { return unityMesh != null ? unityMesh.vertexCount : 0; }
        }

        public int[] triangles
        {
            get { return unityMesh != null ? unityMesh.triangles : new int[0]; }
        }

        public bool PhysicsBakedMeshDirty(PhysicsBakeMeshType physicsBakeMesh)
        {
            return physicsBakeMesh != PhysicsBakeMeshType.None && _physicsBakeMesh != physicsBakeMesh;
        }

        public bool BoundsDirty(bool useCollider, bool calculateBounds = true)
        {
            return calculateBounds && _calculatedBounds != calculateBounds && (!useCollider || _physicsBakeMesh != PhysicsBakeMeshType.None);
        }

        public bool NormalsDirty()
        {
            return _normalsType == NormalsType.None;
        }

        public void PhysicsBakeMesh(PhysicsBakeMeshType physicsBakeMesh)
        {
            if (PhysicsBakedMeshDirty(physicsBakeMesh))
            {
                Physics.BakeMesh(_unityMeshInstanceId, physicsBakeMesh == PhysicsBakeMeshType.Convex);
                _physicsBakeMesh = physicsBakeMesh;
            }
        }

        public bool RecalculateBounds()
        {
            if (unityMesh != null)
            {
                unityMesh.RecalculateBounds();

                _calculatedBounds = true;

                return true;
            }
            return false;
        }

        public void RecalculateNormals()
        {
            if (unityMesh != null && NormalsDirty())
            {
                unityMesh.RecalculateNormals();
                _normalsType = NormalsType.Auto;

                DataPropertyAssigned();
            }
        }

        public void IterateOverUnityMesh(Action<UnityEngine.Mesh> callback)
        {
            if (unityMesh != null)
                callback(unityMesh);
        }

        protected override byte[] GetDataBytes(LoaderBase.DataType dataType)
        {
            string bytes = "";

            if (unityMesh != null)
                JsonUtility.FromJson(out bytes, JsonUtility.ToJson(unityMesh));

            return Encoding.ASCII.GetBytes(bytes);
        }

        public override void SetData(object value, LoaderBase.DataType dataType, InitializationContext initializingContext = InitializationContext.Programmatically)
        {
            if (JsonUtility.FromJson(out UnityEngine.Mesh newUnityMesh, value as JSONNode))
            {
                SetData(newUnityMesh, initializingContext);

                DataPropertyAssigned();
            }
        }

        public void SetData(List<int> triangles, List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, List<Color32> colors, Bounds? bounds = null, InitializationContext _ = InitializationContext.Programmatically)
        {
            SetData(triangles, vertices, normals, uvs, colors, !bounds.HasValue);
            if (bounds.HasValue)
                this.bounds = bounds.Value;
        }

        public bool SetData(int[] triangles = null, Vector3[] vertices = null, Vector3[] normals = null, List<Vector2> uvs = null, Color32[] colors = null, bool calculateBounds = true, InitializationContext initializingContext = InitializationContext.Programmatically)
        {
            CreateMeshIfRequired(initializingContext);

            bool changed = false;

            if (SetTrianglesVertices(triangles, vertices, calculateBounds))
                changed = true;
            if (SetNormals(normals))
                changed = true;
            if (SetColors(colors))
                changed = true;
            if (SetUVs(uvs))
                changed = true;

            if (changed)
            {
                DataPropertyAssigned();

                return true;
            }

            return false;
        }

        public bool SetData(List<int> triangles = null, List<Vector3> vertices = null, List<Vector3> normals = null, List<Vector2> uvs = null, List<Color32> colors = null, bool calculateBounds = true, InitializationContext initializingContext = InitializationContext.Programmatically)
        {
            CreateMeshIfRequired(initializingContext);

            bool changed = false;
            if (SetTrianglesVertices(triangles, vertices, calculateBounds))
                changed = true;
            if (SetNormals(normals))
                changed = true;
            if (SetColors(colors))
                changed = true;
            if (SetUVs(uvs))
                changed = true;

            if (changed)
            {
                DataPropertyAssigned();

                return true;
            }

            return false;
        }

        private void CreateMeshIfRequired(InitializationContext initializingContext = InitializationContext.Programmatically)
        {
            bool requiresNewMesh = unityMesh == null;

#if UNITY_EDITOR
            if (initializingContext == InitializationContext.Editor)
                requiresNewMesh = true;
#endif

            if (requiresNewMesh)
                SetData(new UnityEngine.Mesh(), initializingContext);
        }

        public void SetData(UnityEngine.Mesh mesh)
        {
            SetData(mesh, InitializationContext.Programmatically);

            DataPropertyAssigned();
        }

        private bool SetTrianglesVertices(int[] triangles, Vector3[] vertices, bool calculateBounds = true)
        {
            if (vertices != null && vertices.Length > 0)
            {
                if (unityMesh != null && triangles != null && triangles.Length > 0)
                {
                    unityMesh.SetTriangles(EMPTY_TRIANGLES, 0, calculateBounds);
                    unityMesh.SetVertices(EMPTY_VERTICES);

                    SetVertices(vertices);
                    SetTriangles(triangles, calculateBounds);
                }
                else
                    SetVertices(vertices);

                _physicsBakeMesh = PhysicsBakeMeshType.None;
                _calculatedBounds = calculateBounds;

                return true;
            }
            return false;
        }

        private bool SetTriangles(int[] triangles, bool calculateBounds = true)
        {
            if (unityMesh != null && triangles != null)
            {
                unityMesh.SetTriangles(triangles, 0, calculateBounds);
                return true;
            }
            return false;
        }

        private bool SetVertices(Vector3[] vertices)
        {
            if (unityMesh != null && vertices != null)
            {
                unityMesh.SetVertices(vertices);
                return true;
            }
            return false;
        }

        private bool SetTrianglesVertices(List<int> triangles, List<Vector3> vertices, bool calculateBounds = true)
        {
            if (vertices != null && vertices.Count > 0)
            {
                if (unityMesh != null && triangles != null && triangles.Count > 0)
                {
                    if (unityMesh.triangles.Length > 0)
                        unityMesh.SetTriangles(EMPTY_TRIANGLES, 0, calculateBounds);
                    if (unityMesh.vertices.Length > 0)
                        unityMesh.SetVertices(EMPTY_VERTICES);

                    SetVertices(vertices);
                    SetTriangles(triangles, calculateBounds);
                }
                else
                    SetVertices(vertices);

                _physicsBakeMesh = PhysicsBakeMeshType.None;
                _calculatedBounds = calculateBounds;

                return true;
            }
            return false;
        }

        private bool SetTriangles(List<int> triangles, bool calculateBounds = true)
        {
            if (unityMesh != null && triangles != null)
            {
                unityMesh.SetTriangles(triangles, 0, calculateBounds);
                return true;
            }
            return false;
        }

        private bool SetVertices(List<Vector3> vertices)
        {
            if (unityMesh != null && vertices != null)
            {
                unityMesh.SetVertices(vertices);
                return true;
            }
            return false;
        }

        private bool SetNormals(Vector3[] normals)
        {
            if (unityMesh != null && normals != null)
            {
                unityMesh.SetNormals(normals);

                UpdateNormalsCount();

                return true;
            }
            return false;
        }

        private bool SetNormals(List<Vector3> normals)
        {
            if (unityMesh != null && normals != null)
            {
                unityMesh.SetNormals(normals);

                UpdateNormalsCount();

                return true;
            }
            return false;
        }

        private void UpdateNormalsCount()
        {
            _normalsType = unityMesh.normals.Length != 0 ? NormalsType.Manual : NormalsType.None;
        }

        public bool SetColor(Color color)
        {
            if (unityMesh != null)
            {
                Color32[] colors = new Color32[unityMesh.vertexCount];
                for (int i = 0; i < unityMesh.vertexCount; i++)
                    colors[i] = color;
                unityMesh.SetColors(colors);

                DataPropertyAssigned();
                return true;
            }
            return false;
        }

        private bool SetColors(Color32[] colors)
        {
            if (unityMesh != null && colors != null)
            {
                unityMesh.SetColors(colors);
                return true;
            }
            return false;
        }

        private bool SetColors(List<Color32> colors)
        {
            if (unityMesh != null && colors != null)
            {
                unityMesh.SetColors(colors);
                return true;
            }
            return false;
        }

        public bool SetUVs(List<Vector4> uvs, int channel = 0)
        {
            if (unityMesh != null && uvs != null)
            {
                unityMesh.SetUVs(channel, uvs);

                DataPropertyAssigned();
                return true;
            }
            return false;
        }

        private bool SetUVs(List<Vector2> uvs, int channel = 0)
        {
            if (unityMesh != null && uvs != null)
            {
                unityMesh.SetUVs(channel, uvs);
                return true;
            }
            return false;
        }


        private void SetData(UnityEngine.Mesh mesh, InitializationContext initializingContext = InitializationContext.Programmatically)
        {
            UnityEngine.Mesh oldUnityMesh = unityMesh;

            unityMesh = mesh;

            DisposeOldDataAndRegisterNewData(oldUnityMesh, unityMesh, initializingContext);
        }

        public UnityEngine.Mesh unityMesh
        {
            get { return _unityMesh; }
            private set
            {
                if (_unityMesh == value)
                    return;

                _unityMesh = value;

                UpdateUnityMeshInstanceId();
            }
        }

        private void UpdateUnityMeshInstanceId()
        {
            _unityMeshInstanceId = _unityMesh != null ? _unityMesh.GetInstanceID() : 0;
        }

        protected override string GetFileExtension()
        {
            return "mesh";
        }

        public bool AddModifying(MeshObjectBase meshObject)
        {
            if (!meshObjectModifying.Contains(meshObject))
            {
                AddMeshObjectDelegate(meshObject);
                meshObjectModifying.Add(meshObject);
                UpdateModificationPending();
                return true;
            }
            return false;
        }

        public bool RemoveModifying(MeshObjectBase meshObject)
        {
            if (meshObjectModifying.Remove(meshObject))
            {
                RemoveMeshObjectDelegate(meshObject);
                UpdateModificationPending();
                return true;
            }
            return false;
        }

        private void UpdateModificationPending()
        {
            modificationPending = meshObjectModifying.Count != 0;
        }

        public static T CreateMesh<T>(InitializationContext initializingContext = InitializationContext.Programmatically) where T : Mesh
        {
            return CreateMesh(typeof(T), initializingContext) as T;
        }

        public static Mesh CreateMesh(InitializationContext initializingContext = InitializationContext.Programmatically)
        {
            return CreateMesh(typeof(Mesh), initializingContext);
        }

        public static Mesh CreateMesh(Type type, InitializationContext initializingContext = InitializationContext.Programmatically)
        {
            Mesh mesh = InstanceManager.Instance().CreateInstance(type, initializingContext: initializingContext) as Mesh;
            mesh.name = typeof(Mesh).Name;
            return mesh;
        }

        public static MeshModifier CreateMeshModifier(Type type)
        {
            return InstanceManager.Instance(false).CreateInstance(type) as MeshModifier;
        }

        public static MeshModifier CreateMeshModifier()
        {
            return CreateMeshModifier<MeshModifier>();
        }

        public static T CreateMeshModifier<T>() where T : MeshModifier
        {
            return InstanceManager.Instance(false).CreateInstance<T>();
        }

        protected override bool OnDisposedLocked()
        {
            if (base.OnDisposedLocked())
            {
                for (int i = meshObjectModifying.Count - 1; i >= 0; i--)
                    RemoveModifying(meshObjectModifying[i]);

                return true;
            }
            return false;
        }

        public override bool OnDispose(DisposeContext disposeContext)
        {
            if (base.OnDispose(disposeContext))
            {
                if (disposeContext != DisposeContext.Programmatically_Pool)
                    Dispose(_unityMesh, disposeContext);

                ModificationPendingChangedEvent = null;

                return true;
            }
            return false;
        }
    }

    [Serializable]
    public class MeshModifier : AssetModifier
    {
        public List<int> triangles;
        public List<Vector3> vertices;
        public List<Vector3> normals;
        public List<Color32> colors;
        public List<Vector2> uvs;
        public Vector3[] customData;
        public Bounds? bounds;

        [SerializeField]
        private Vector3 _minBounds;
        [SerializeField]
        private Vector3 _maxBounds;
        [SerializeField]
        private bool _verticesChanged;
        [SerializeField]
        private bool _normalsChanged;
        [SerializeField]
        private bool _trianglesChanged;
        [SerializeField]
        private bool _uvsChanged;
        [SerializeField]
        private bool _colorsChanged;

        public override void Recycle()
        {
            base.Recycle();

            triangles?.Clear();
            vertices?.Clear();
            normals?.Clear();
            colors?.Clear();
            uvs?.Clear();
            customData = default;

            _verticesChanged = default;
            _normalsChanged = default;
            _trianglesChanged = default;
            _uvsChanged = default;
            _colorsChanged = default;
        }

        public virtual MeshModifier Init(int verticesCount = -1, int normalsCount = -1, int trianglesCount = -1, int uvsCount = -1, int colorsCount = -1)
        {
            _minBounds = Vector3.zero;
            _maxBounds = Vector3.zero;
            bounds = null;

            if (verticesCount != -1)
            {
                if (vertices == null || vertices.Count != verticesCount)
                    vertices = new List<Vector3>(new Vector3[verticesCount]);
                _verticesChanged = true;
            }

            if (normalsCount != -1)
            {
                if (normals == null || normals.Count != normalsCount)
                    normals = new List<Vector3>(new Vector3[normalsCount]);
                _normalsChanged = true;
            }

            if (trianglesCount != -1)
            {
                if (triangles == null || triangles.Count != trianglesCount)
                    triangles = new List<int>(new int[trianglesCount]);
                _trianglesChanged = true;
            }

            if (uvsCount != -1)
            {
                if (uvs == null || uvs.Count != uvsCount)
                    uvs = new List<Vector2>(new Vector2[uvsCount]);
                _uvsChanged = true;
            }

            if (colorsCount != -1)
            {
                if (colors == null || colors.Count != colorsCount)
                    colors = new List<Color32>(new Color32[colorsCount]);
                _colorsChanged = true;
            }

            return this;
        }

        public void UpdateMinMaxBounds(Vector3 vertex)
        {
            _minBounds = Vector3.Min(_minBounds, vertex);
            _maxBounds = Vector3.Max(_maxBounds, vertex);
        }

        public void CalculateBoundsFromMinMax()
        {
            Vector3 size = _maxBounds - _minBounds;
            bounds = new Bounds(_minBounds + size / 2, size);
        }

        public static void PopulateBuffers(MeshModifier meshModifier, string jsonStr)
        {
            JsonUtility.FromJsonOverwrite(jsonStr, meshModifier);
            meshModifier.BuffersAllChanged();
        }

        private void BuffersAllChanged()
        {
            _verticesChanged = true;
            _normalsChanged = true;
            _trianglesChanged = true;
            _uvsChanged = true;
            _colorsChanged = true;
        }

        public override void ModifyProperties(IScriptableBehaviour scriptableBehaviour)
        {
            base.ModifyProperties(scriptableBehaviour);

            Mesh mesh = scriptableBehaviour as Mesh;

            mesh.SetData(
                    _trianglesChanged ? triangles : null,
                    _verticesChanged ? vertices : null,
                    _normalsChanged ? normals : null,
                    _uvsChanged ? uvs : null,
                    _colorsChanged ? colors : null, 
                    bounds);
        }

        public void FlipTriangles()
        {
            for (int i = 0; i < triangles.Count; i += 3)
            {
                (triangles[i + 1], triangles[i]) = (triangles[i], triangles[i + 1]);
            }
        }

        public virtual Type GetMeshType()
        {
            return typeof(Mesh);
        }
    }
}
