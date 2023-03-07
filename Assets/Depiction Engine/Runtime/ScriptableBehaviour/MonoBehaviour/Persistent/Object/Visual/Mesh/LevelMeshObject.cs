// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using UnityEngine;

namespace DepictionEngine
{
    public class LevelMeshObject : MeshObjectBase
    {
        public const string FLOOR_MESH_RENDERER_VISUAL_NAME = "FloorMeshRendererVisual";
        public const string WALLS_MESH_RENDERER_VISUAL_NAME = "WallsMeshRendererVisual";
        public const string CEILING_MESH_RENDERER_VISUAL_NAME = "CeilingMeshRendererVisual";

        [BeginFoldout("Material")]
        [SerializeField, Tooltip("The path of the floor mesh material's shader from within the Resources directory.")]
        private string _floorShaderPath;
        [SerializeField, Tooltip("The path of the walls mesh material's shader from within the Resources directory.")]
        private string _wallsShaderPath;
        [SerializeField, Tooltip("The path of the ceiling mesh material's shader from within the Resources directory."), EndFoldout]
        private string _ceilingShaderPath;

        [SerializeField, HideInInspector]
        private Material _floorMaterial;
        [SerializeField, HideInInspector]
        private Material _wallsMaterial;
        [SerializeField, HideInInspector]
        private Material _ceilingMaterial;

        protected override void InitializeSerializedFields(InstanceManager.InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            if (initializingContext == InstanceManager.InitializationContext.Editor_Duplicate || initializingContext == InstanceManager.InitializationContext.Programmatically_Duplicate)
            {
                _floorMaterial = null;
                _wallsMaterial = null;
                _ceilingMaterial = null;
            }

            InitValue(value => floorShaderPath = value, RenderingManager.SHADER_BASE_PATH + "Level", initializingContext);
            InitValue(value => wallsShaderPath = value, RenderingManager.SHADER_BASE_PATH + "Level", initializingContext);
            InitValue(value => ceilingShaderPath = value, RenderingManager.SHADER_BASE_PATH + "Level", initializingContext);
        }

        public float GetHeight()
        {
            MeshRendererVisual mesh = GetVisual(WALLS_MESH_RENDERER_VISUAL_NAME) as MeshRendererVisual;
            if (mesh != Disposable.NULL)
                return mesh.bounds.size.y;
            return 0.0f;
        }
     
        public bool IsOpened()
        {
            MeshRendererVisual mesh = GetVisual(CEILING_MESH_RENDERER_VISUAL_NAME) as MeshRendererVisual;
            if (mesh != Disposable.NULL)
                return !mesh.gameObject.activeSelf;
            return false;
        }

        public void Opened()
        {
            MeshRendererVisual mesh = GetVisual(CEILING_MESH_RENDERER_VISUAL_NAME) as MeshRendererVisual;
            if (mesh != Disposable.NULL)
                mesh.gameObject.SetActive(false);
        }

        public void Closed()
        {
            MeshRendererVisual mesh = GetVisual(CEILING_MESH_RENDERER_VISUAL_NAME) as MeshRendererVisual;
            if (mesh != Disposable.NULL)
                mesh.gameObject.SetActive(true);
        }

        /// <summary>
        /// The path of the floor mesh material's shader from within the Resources directory.
        /// </summary>
        [Json]
        public string floorShaderPath
        {
            get { return _floorShaderPath; }
            set { SetValue(nameof(floorShaderPath), value, ref _floorShaderPath); }
        }

        /// <summary>
        /// The path of the walls mesh material's shader from within the Resources directory.
        /// </summary>
        [Json]
        public string wallsShaderPath
        {
            get { return _wallsShaderPath; }
            set { SetValue(nameof(wallsShaderPath), value, ref _wallsShaderPath); }
        }

        /// <summary>
        /// The path of the ceiling mesh material's shader from within the Resources directory.
        /// </summary>
        [Json]
        public string ceilingShaderPath
        {
            get { return _ceilingShaderPath; }
            set { SetValue(nameof(ceilingShaderPath), value, ref _ceilingShaderPath); }
        }

        protected override void UpdateMeshRendererVisualModifiers(Action<VisualObjectVisualDirtyFlags> completedCallback, VisualObjectVisualDirtyFlags meshRendererVisualDirtyFlags)
        {
            base.UpdateMeshRendererVisualModifiers(completedCallback, meshRendererVisualDirtyFlags);

            completedCallback?.Invoke(meshRendererVisualDirtyFlags);
        }

        protected override void ModifyMesh(MeshRendererVisualModifier meshRendererVisualModifier, Mesh mesh, Action meshModified, VisualObjectVisualDirtyFlags meshRendererVisualDirtyFlags, bool disposeMeshModifier = true)
        {
            base.ModifyMesh(meshRendererVisualModifier, mesh, meshModified, meshRendererVisualDirtyFlags, false);
        }

        protected override void InitializeMaterial(MeshRendererVisual meshRendererVisual, Material material)
        {
            if (meshRendererVisual == transform.children[0])
                material = UpdateMaterial(ref _floorMaterial, floorShaderPath);
            else if (meshRendererVisual == transform.children[1])
                material = UpdateMaterial(ref _wallsMaterial, wallsShaderPath);
            else if (meshRendererVisual == transform.children[2])
                material = UpdateMaterial(ref _ceilingMaterial, ceilingShaderPath);

            base.InitializeMaterial(meshRendererVisual, material);
        }

        protected override bool OnDisposed(DisposeManager.DisposeContext disposeContext, bool pooled)
        {
            if (base.OnDisposed(disposeContext, pooled))
            {
                if (!pooled)
                {
                    Dispose(_floorMaterial, disposeContext);
                    Dispose(_wallsMaterial, disposeContext);
                    Dispose(_ceilingMaterial, disposeContext);
                }

                return true;
            }
            return false;
        }
    }

    public class LevelProcessingFunctions : ProcessingFunctions
    {
        public static void ParseJSON(JSONNode json, LevelModifier levelModifier, CancellationTokenSource cancellationTokenSource)
        {
            if (cancellationTokenSource is null)
            {
                throw new ArgumentNullException(nameof(cancellationTokenSource));
            }

            AddMeshModifierToData(levelModifier.meshObjectProcessorOutput, json["floorBuffers"], LevelMeshObject.FLOOR_MESH_RENDERER_VISUAL_NAME);
            AddMeshModifierToData(levelModifier.meshObjectProcessorOutput, json["wallsBuffers"], LevelMeshObject.WALLS_MESH_RENDERER_VISUAL_NAME);
            AddMeshModifierToData(levelModifier.meshObjectProcessorOutput, json["ceilingBuffers"], LevelMeshObject.CEILING_MESH_RENDERER_VISUAL_NAME, true);
        }

        private static void AddMeshModifierToData(MeshObjectProcessorOutput meshObjectProcessorOutput, JSONNode json, string name, bool flipTriangles = false)
        {
            //TODO: REMOVE AFTER SERVER REFACTOR
            if (json["indices"] != null && json["colorColor32"] != null && json["verticesVector3"] != null)
            {
                json["triangles"] = json["indices"];
                json.Remove("indices");
                json["colors"] = json["colorColor32"];
                json.Remove("colorColor32");
                json["vertices"] = json["verticesVector3"];
                json.Remove("verticesVector3");
                json["normals"] = new JSONArray();
                json["uvs"] = new JSONArray();
                json["customData"] = new JSONArray();

                MeshRendererVisualModifier meshRendererVisualModifier = MeshRendererVisual.CreateMeshRendererVisualModifier(name);
                if (JsonUtility.FromJson(out string jsonStr, json))
                    MeshModifier.PopulateBuffers(meshRendererVisualModifier.CreateMeshModifier(), jsonStr);
                if (flipTriangles)
                    meshRendererVisualModifier.meshModifier.FlipTriangles();
                meshObjectProcessorOutput.AddMeshRendererVisualModifier(meshRendererVisualModifier);
            }
        }
    }

    public class LevelModifier : PropertyModifier
    {
        public MeshObjectProcessorOutput meshObjectProcessorOutput;

        public override void Recycle()
        {
            base.Recycle();

            meshObjectProcessorOutput = null;
        }

        public LevelModifier Init(MeshObjectProcessorOutput meshObjectData)
        {
            this.meshObjectProcessorOutput = meshObjectData;

            return this;
        }

        public override void ModifyProperties(IScriptableBehaviour scriptableBehaviour)
        {
            base.ModifyProperties(scriptableBehaviour);

            if (scriptableBehaviour is LevelMeshObject)
            {
                (scriptableBehaviour as LevelMeshObject).meshRendererVisualModifiers = meshObjectProcessorOutput.meshRendererVisualModifiers;
                meshObjectProcessorOutput.Clear();
            }
        }

        public override bool OnDisposing(DisposeManager.DisposeContext disposeContext)
        {
            if (base.OnDisposing(disposeContext))
            {
                DisposeManager.Dispose(meshObjectProcessorOutput);

                return true;
            }
            return false;
        }
    }
}