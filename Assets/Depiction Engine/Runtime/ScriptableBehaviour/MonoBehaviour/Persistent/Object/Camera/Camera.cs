// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using DepictionEngine.Editor;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DepictionEngine
{
    /// <summary>
    /// Wrapper class for 'UnityEngine.Camera' introducing better integrated functionality.
    /// </summary>
    [AddComponentMenu(SceneManager.NAMESPACE + "/Object/" + nameof(Camera))]
    [CreateComponent(typeof(Skybox), typeof(UniversalAdditionalCameraData))]
    public class Camera : Object
    {
        public const int DEFAULT_ENVIRONMENT_TEXTURE_SIZE = 512;
        public const int DEFAULT_DISTANCE_PASS = 2;

        public const string CLEAR_FLAGS_TOOLTIP = "How the camera clears the background.";
        public const string BACKGROUND_COLOR_TOOLTIP = "The color with which the screen will be cleared.";
        public const string SKYBOX_MATERIAL_PATH_TOOLTIP = "The path of the material within the Resources directory, such as 'Star-Skybox' or Atmopshere-Skybox.";
        public const string ENVIRONMENT_TEXTURE_SIZE_TOOLTIP = "A power of two value used to establish the width/height of the environment cubemap texture.";

        [BeginFoldout("Projection")]
        [SerializeField, Tooltip("Is the camera orthographic (true) or perspective (false)?")]
        private bool _orthographic;
        [SerializeField, Tooltip("Camera's half-szie when in orthographic mode.")]
        private float _orthographicSize;
        [SerializeField, Tooltip("The vertical field of view of the camera, in degrees.")]
        private float _fieldOfView;
        [SerializeField, Tooltip("The distance of the near clipping plane from the Camera, in world units.")]
        private float _nearClipPlane;
        [SerializeField, Tooltip("The distance of the far clipping plane from the Camera, in world units."), EndFoldout]
        private float _farClipPlane;

        [BeginFoldout("Rendering")]
        [SerializeField, Tooltip("Should the Camera render post processing effects.")]
        private bool _postProcessing;
        [SerializeField, Tooltip("How and if camera generates a depth texture.")]
        private DepthTextureMode _depthTextureMode;
        [SerializeField, Tooltip("A mask used to render parts of the Scene selectively."), Mask]
        private int _cullingMask;
        [SerializeField, Tooltip("Whether or not the Camera will use occlusion culling during redering."), EndFoldout]
        private bool _useOcclusionCulling;

        [BeginFoldout("Environment")]
        [SerializeField, Tooltip(CLEAR_FLAGS_TOOLTIP)]
        private CameraClearFlags _clearFlags;
        [SerializeField, Tooltip(BACKGROUND_COLOR_TOOLTIP)]
        private Color _backgroundColor;
        [SerializeField, Tooltip(SKYBOX_MATERIAL_PATH_TOOLTIP)]
        private string _skyboxMaterialPath;
        [SerializeField, Tooltip(ENVIRONMENT_TEXTURE_SIZE_TOOLTIP), EndFoldout]
        private int _environmentTextureSize;

        [BeginFoldout("Output")]
        [SerializeField, Tooltip("High dynamic range rendering."), EndFoldout]
        private bool _allowHDR;

        private RenderTexture _environmentCubemap;

        private Skybox _skybox;

        private UnityEngine.Camera _unityCamera;
        private UniversalAdditionalCameraData _additionalData;

        private List<Stack> _stacks;

        protected override void InitializeSerializedFields(InstanceManager.InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            InitValue(value => orthographic = value, false, initializingContext);
            InitValue(value => orthographicSize = value, 5.0f, initializingContext);
            InitValue(value => fieldOfView = value, 60.0f, initializingContext);
            InitValue(value => nearClipPlane = value, 0.3f, initializingContext);
            InitValue(value => farClipPlane = value, 5000.0f, initializingContext);

            InitValue(value => postProcessing = value, GetDefaultPostProcessing(), initializingContext);
            InitValue(value => depthTextureMode = value, DepthTextureMode.None, initializingContext);
            InitValue(value => cullingMask = value, -65537, initializingContext);
            InitValue(value => useOcclusionCulling = value, true, initializingContext);

            InitValue(value => clearFlags = value, CameraClearFlags.Skybox, initializingContext);
            InitValue(value => backgroundColor = value, new Color(0.1921569f, 0.3019608f, 0.4745098f, 0.0f), initializingContext);
            InitValue(value => skyboxMaterialPath = value, cameraManager.skyboxMaterialPath, initializingContext);
            InitValue(value => environmentTextureSize = value, DEFAULT_ENVIRONMENT_TEXTURE_SIZE, initializingContext);

            InitValue(value => allowHDR = value, true, initializingContext);
        }

#if UNITY_EDITOR
        //Ugly hack to force a Refresh in the 'UniversalRenderPipelineSerializedCamera' and prevent it from spamming 'Index out of bound exception' when a new camera is created and selected in the Editor
        //This creates a new Undo operation so we need to execute it after the RegisterCreatedObjectUndo
        //TODO: A ticket(Case 1425719) as been opened with Unity to fix this bug, remove this hack once the bug is fixed
        public void PreventCameraStackBug()
        {
            if (stackCount == 0 && Editor.Selection.activeTransform == transform)
            {
                //Breaks Undo/redo by adding an additional "Move Component(s)" action.
                //UnityEditorInternal.ComponentUtility.MoveComponentUp(GetUniversalAdditionalCameraData());
                moveComponentDown = true;
            }
        }
#endif

        protected override void CreateComponents(InstanceManager.InitializationContext initializingContext)
        {
            base.CreateComponents(initializingContext);

            if (!isFallbackValues)
            {
                if (skybox == null)
                    skybox = GetComponent<Skybox>();
            }

            InitializeCamera(initializingContext);

            InitializeStack(initializingContext);
        }

        protected virtual void InitializeCamera(InstanceManager.InitializationContext initializingContext)
        {
            if (!isFallbackValues)
            {
                if (unityCamera == null)
                    unityCamera = GetComponent<UnityEngine.Camera>();
            }

            if (initializingContext == InstanceManager.InitializationContext.Editor || initializingContext == InstanceManager.InitializationContext.Programmatically)
                RemoveIgnoreRenderFromUnityCameraCullingMask(unityCamera);

            InitializeAdditionalData();
        }

        protected virtual void InitializeAdditionalData()
        {
            if (!isFallbackValues)
            {
                if (additionalData == null)
                {
                    additionalData = GetUniversalAdditionalCameraData();
                    additionalData.SetRenderer(GetDefaultRenderer());
                }
            }
        }

        protected virtual bool InitializeStack(InstanceManager.InitializationContext initializingContext)
        {
            string stackName = "Stack";
            if (gameObject.transform.Find(stackName) == null)
            {
                GameObject stackGO = new GameObject(stackName);
                UndoManager.RegisterCreatedObjectUndo(stackGO, initializingContext);
                UndoManager.SetTransformParent(stackGO.transform, gameObject.transform, false, initializingContext);

                Stack stack = stackGO.AddSafeComponent<Stack>(initializingContext);
                stack.main = true;
                stack.synchRenderProperties = true;
                stack.synchOpticalProperties = true;
                stack.synchAspectProperty = true;
                stack.synchBackgroundProperties = true;
                stack.synchClipPlaneProperties = true;
                stack.synchCullingMaskProperty = true;

                UniversalAdditionalCameraData universalAdditionalCameraData = GetUniversalAdditionalCameraData();

                int distancePass = GetDefaultDistancePass();
                for (int i = distancePass - 1; i >= 0; i--)
                {
                    GameObject cameraGO = new GameObject("DistancePass_" + (i + 1));
                    UndoManager.RegisterCreatedObjectUndo(cameraGO, initializingContext);
                    UndoManager.SetTransformParent(cameraGO.transform, stackGO.transform, false, initializingContext);

                    UnityEngine.Camera unityCamera = cameraGO.AddSafeComponent<UnityEngine.Camera>(initializingContext);
                    RemoveIgnoreRenderFromUnityCameraCullingMask(unityCamera);

                    UniversalAdditionalCameraData distancePassCameraUniversalAdditionalCameraData = unityCamera.GetUniversalAdditionalCameraData();
                    UndoManager.RegisterCreatedObjectUndo(distancePassCameraUniversalAdditionalCameraData, initializingContext);
                    if (distancePassCameraUniversalAdditionalCameraData != null)
                    {
                        distancePassCameraUniversalAdditionalCameraData.renderType = CameraRenderType.Overlay;
                        distancePassCameraUniversalAdditionalCameraData.SetRenderer(GetDefaultRenderer());
                    }

                    if (universalAdditionalCameraData != null)
                        universalAdditionalCameraData.cameraStack.Add(unityCamera);
                }

                return true;
            }
            return false;
        }

        protected void RemoveIgnoreRenderFromUnityCameraCullingMask(UnityEngine.Camera unityCamera)
        {
            unityCamera.cullingMask &= ~(1 << LayerUtility.GetLayer(CameraManager.IGNORE_RENDER_LAYER_NAME));
        }

        private List<Stack> _stacksTmp;
        public override int GetAdditionalChildCount()
        {
            return UpdateStacks(ref _stacksTmp);
        }

        public override void UpdateAdditionalChildren()
        {
            base.UpdateAdditionalChildren();

            UpdateStacks(ref _stacks);
        }

        private int UpdateStacks(ref List<Stack> stacks)
        {
            if (stacks == null)
                stacks = new List<Stack>();
            stacks.Clear();
            gameObject.transform.GetComponentsInChildren(stacks);
            return stacks.Count;
        }

        private bool moveComponentDown;
        public override bool LateInitialize()
        {
            if (base.LateInitialize())
            {
#if UNITY_EDITOR
                if (moveComponentDown)
                {
                    moveComponentDown = false;
                    //UnityEditorInternal.ComponentUtility.MoveComponentDown(GetUniversalAdditionalCameraData());
                }
#endif

                return true;
            }
            return false;
        }

        protected override bool UpdateHideFlags()
        {
            if (base.UpdateHideFlags())
            {
                bool debug = false;

                if (!SceneManager.IsSceneBeingDestroyed())
                    debug = sceneManager.debug;

                if (!debug)
                {
                    if (unityCamera != null)
                        unityCamera.hideFlags = HideFlags.HideInInspector;
                    if (skybox != null)
                        skybox.hideFlags = HideFlags.HideInInspector;
                }

                return true;
            }
            return false;
        }

        public override bool RequiresPositioning()
        {
            return true;
        }

        private int stackCount
        {
            get { return _stacks == null ? 0 : _stacks.Count; }
        }

        public virtual int GetDefaultDistancePass()
        {
            return DEFAULT_DISTANCE_PASS;
        }

        protected virtual int GetDefaultRenderer()
        {
            return -1;
        }

        protected virtual bool GetDefaultPostProcessing()
        {
            return true;
        }

        protected virtual bool GetUnityCameraReadOnly()
        {
            return false;
        }

        public virtual UnityEngine.Camera unityCamera
        {
            get { return _unityCamera; }
            protected set
            {
                if (_unityCamera == value)
                    return;

                _unityCamera = value;
            }
        }

        public UniversalAdditionalCameraData additionalData
        {
            get { return _additionalData; }
            private set
            {
                if (_additionalData == value)
                    return;

                _additionalData = value;

                UpdateAdditionalData();
            }
        }

        private void UpdateAdditionalData()
        {
            if (additionalData != null)
            {
                if (additionalData.renderPostProcessing != postProcessing)
                    additionalData.renderPostProcessing = postProcessing;
            }
        }

        public Skybox skybox
        {
            get { return _skybox; }
            private set
            {
                if (_skybox == value)
                    return;

                _skybox = value;

                UpdateSkybox();
            }
        }

        private void UpdateSkybox()
        {
            if (skybox != null)
                skybox.material = RenderingManager.LoadMaterial(skyboxMaterialPath);
        }

        public RenderTexture environmentCubemap
        {
            get { return _environmentCubemap; }
            private set
            {
                if (Object.ReferenceEquals(_environmentCubemap, value))
                    return;

                Dispose(_environmentCubemap);

                _environmentCubemap = value;
            }
        }

        public RenderTexture GetEnvironmentCubeMap()
        {
            int textureSize = Mathf.ClosestPowerOfTwo(environmentTextureSize);
            if (environmentCubemap == null || _environmentCubemap.width != textureSize || _environmentCubemap.height != textureSize)
            {
                environmentCubemap = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.ARGB32, 0);
                environmentCubemap.dimension = TextureDimension.Cube;
                environmentCubemap.name = name + "_Dynamic_Skybox_Cubemap";
            }
            return environmentCubemap;
        }

        public static Camera main
        {
            get { return UnityEngine.Camera.main != null ? UnityEngine.Camera.main.GetComponent<Camera>() : null; }
        }

        public Matrix4x4 GetViewToWorldMatrix()
        {
            return (GL.GetGPUProjectionMatrix(unityCamera.projectionMatrix, false) * unityCamera.worldToCameraMatrix).inverse;
        }

        public virtual int GetMainStackCount()
        {
            int count = 0;

            if (_stacks != null)
            {
                foreach (Stack stack in _stacks)
                {
                    if (stack.main)
                    {
                        foreach (Transform transform in stack.transform)
                        {
                            UnityEngine.Camera unityCamera = transform.GetComponent<UnityEngine.Camera>();
                            if (unityCamera != null && unityCamera.isActiveAndEnabled)
                                count++;
                        }
                    }
                }
            }

            return count;
        }

        public float GetFarDistanceClipPlane()
        {
            return GetFarClipPlane(GetMainStackCount(), farClipPlane);
        }

#if UNITY_EDITOR
        private UnityEngine.Object[] GetUnityCameraAdditionalRecordObjects()
        {
            if (unityCamera != null)
                return new UnityEngine.Object[] { unityCamera };
            return null;
        }
#endif

        /// <summarty>
        /// Is the camera orthographic (true) or perspective (false)?
        /// </summarty>
        [Json]
        public bool orthographic
        {
            get { return _orthographic; }
            set { SetValue(nameof(orthographic), value, ref _orthographic); }
        }

        /// <summarty>
        /// Camera's half-szie when in orthographic mode.
        /// </summarty>
        [Json]
        public float orthographicSize
        {
            get { return _orthographicSize; }
            set { SetValue(nameof(orthographicSize), value, ref _orthographicSize); }
        }

        /// <summary>
        /// The vertical field of view of the camera, in degrees.
        /// </summary>
        [Json]
        public float fieldOfView
        {
            get { return _fieldOfView; }
            set { SetValue(nameof(fieldOfView), value, ref _fieldOfView); }
        }

        /// <summary>
        /// The distance of the near clipping plane from the Camera, in world units.
        /// </summary>
        [Json]
        public float nearClipPlane
        {
            get { return _nearClipPlane; }
            set { SetValue(nameof(nearClipPlane), value, ref _nearClipPlane); }
        }

        /// <summary>
        /// The distance of the far clipping plane from the Camera, in world units.
        /// </summary>
        [Json]
#if UNITY_EDITOR
        [RecordAdditionalObjects(nameof(GetUnityCameraAdditionalRecordObjects))]
#endif
        public float farClipPlane
        {
            get { return _farClipPlane; }
            set 
            {
                if (value > 10000000000.0f)
                    value = 10000000000.0f;
                SetValue(nameof(farClipPlane), value, ref _farClipPlane, (newValue, oldValue) =>
                {
                    if (unityCamera != null && unityCamera.farClipPlane != newValue)
                        unityCamera.farClipPlane = newValue;
                });
            }
        }

#if UNITY_EDITOR
        private UnityEngine.Object[] GetAdditionalDataAdditionalRecordObjects()
        {
            if (additionalData != null)
                return new UnityEngine.Object[] { additionalData };
            return null;
        }
#endif

        /// <summary>
        /// Should the Camera render post processing effects.
        /// </summary>
        [Json]
#if UNITY_EDITOR
        [RecordAdditionalObjects(nameof(GetAdditionalDataAdditionalRecordObjects))]
#endif
        public bool postProcessing
        {
            get { return _postProcessing; }
            set
            {
                SetValue(nameof(postProcessing), value, ref _postProcessing, (newValue, oldValue) =>
                {
                    PostProcessingChanged();
                });
            }
        }

        protected virtual void PostProcessingChanged()
        {
            UpdateAdditionalData();
        }

        /// <summary>
        /// How and if camera generates a depth texture.
        /// </summary>
        [Json]
        public DepthTextureMode depthTextureMode
        {
            get { return _depthTextureMode; }
            set { SetValue(nameof(depthTextureMode), value, ref _depthTextureMode); }
        }

        /// <summary>
        /// A mask used to render parts of the Scene selectively.
        /// </summary>
        [Json]
        public int cullingMask
        {
            get { return _cullingMask; }
            set { SetValue(nameof(cullingMask), value, ref _cullingMask); }
        }

        /// <summary>
        /// Whether or not the Camera will use occlusion culling during redering.
        /// </summary>
        [Json]
        public bool useOcclusionCulling
        {
            get { return _useOcclusionCulling; }
            set { SetValue(nameof(useOcclusionCulling), value, ref _useOcclusionCulling); }
        }

        /// <summary>
        /// How the camera clears the background.
        /// </summary>
        [Json]
        public CameraClearFlags clearFlags
        {
            get { return _clearFlags; }
            set { SetValue(nameof(clearFlags), value, ref _clearFlags); }
        }

        /// <summary>
        /// The color with which the screen will be cleared.
        /// </summary>
        [Json]
        public Color backgroundColor
        {
            get { return _backgroundColor; }
            set { SetValue(nameof(backgroundColor), value, ref _backgroundColor); }
        }

#if UNITY_EDITOR
        private UnityEngine.Object[] GetSkyboxAdditionalRecordObjects()
        {
            if (skybox != null)
                return new UnityEngine.Object[] { skybox };
            return null;
        }
#endif

        /// <summary>
        /// The path of the material within the Resources directory, such as 'Star-Skybox' or Atmopshere-Skybox.
        /// </summary>
        [Json]
#if UNITY_EDITOR
        [RecordAdditionalObjects(nameof(GetSkyboxAdditionalRecordObjects))]
#endif
        public string skyboxMaterialPath
        {
            get { return _skyboxMaterialPath; }
            set 
            {
                SetValue(nameof(skyboxMaterialPath), value, ref _skyboxMaterialPath, (newValue, oldValue) =>
                {
                    UpdateSkybox();
                });
            }
        }

        /// <summary>
        /// A power of two value used to establish the width/height of the environment cubemap texture.
        /// </summary>
        [Json]
        public int environmentTextureSize
        {
            get { return _environmentTextureSize; }
            set 
            {
                if (value < 2)
                    value = 2;
                SetValue(nameof(environmentTextureSize), value, ref _environmentTextureSize); 
            }
        }

        /// <summary>
        /// High dynamic range rendering.
        /// </summary>
        [Json]
        public bool allowHDR
        {
            get { return _allowHDR; }
            set { SetValue(nameof(allowHDR), value, ref _allowHDR); }
        }

        public float aspect
        {
            get { return unityCamera.aspect; }
        }

        public int pixelHeight
        {
            get { return unityCamera.pixelHeight; }
        }

        public int pixelWidth
        {
            get { return unityCamera.pixelWidth; }
        }

        public static Camera current
        {
            get 
            {
                CameraManager cameraManager = CameraManager.Instance(false);
                if (cameraManager != Disposable.NULL)
                    return cameraManager.GetCameraFromUnityCamera(UnityEngine.Camera.current);
                return null;
            }
        }

        public virtual int GetCameraInstanceId()
        {
            return GetInstanceID(); ;
        }

        private Vector3Double _origin;
        public Vector3Double GetOrigin()
        {
            if (Vector3Double.Distance(_origin, transform.position) > 1000.0d)
                _origin = transform.position;
            return _origin;
        }

        public virtual UniversalAdditionalCameraData GetUniversalAdditionalCameraData()
        {
            return unityCamera.GetUniversalAdditionalCameraData();
        }

        private readonly Rect VIEWPORT_UPPER_RIGHT = new Rect(0.0f, 0.0f, 1.0f, 1.0f);
        public void CalculateFrustumCorners(Vector3[] outCorners)
        {
            CalculateFrustumCorners(VIEWPORT_UPPER_RIGHT, 500.0f, UnityEngine.Camera.MonoOrStereoscopicEye.Mono, outCorners);
        }

        public void CalculateFrustumCorners(Rect viewport, float z, UnityEngine.Camera.MonoOrStereoscopicEye eye, Vector3[] outCorners)
        {
            unityCamera.CalculateFrustumCornersSafe(viewport, z, eye, outCorners);
        }

        public RayDouble[] ViewportPointToRays(Vector2[] pos)
        {
            RayDouble[] cornerRays = new RayDouble[pos.Length];
            for (int i = 0; i < cornerRays.Length; i++)
                cornerRays[i] = ViewportPointToRay(pos[i]);
            return cornerRays;
        }

        public RayDouble ViewportPointToRay(Vector3 pos)
        {
            pos.x *= pixelWidth;
            pos.y *= pixelHeight;
            return ScreenPointToRay(pos);
        }

        public RayDouble ScreenPointToRay(Vector3 pos)
        {
            Ray ray = unityCamera.ScreenPointToRaySafe(pos);
            return new RayDouble(transform.position + ray.direction * pos.z, ray.direction);
        }

        public static RayDouble ScreenPointToRay(Vector2 pos, Vector3[] frustumCorners, Vector3Double position, QuaternionDouble rotation, float pixelWidth, float pixelHeight, bool orthographic, float orthographicSize, float aspect)
        {
            Vector3Double origin;
            QuaternionDouble direction;

            pos.x /= pixelWidth;
            pos.y /= pixelHeight;

            if (orthographic)
            {
                origin = position + rotation * ((pos - new Vector2(0.5f, 0.5f)) * new Vector2(orthographicSize * aspect, orthographicSize) * 2.0f);
                direction = rotation;
            }
            else
            {
                origin = position;
                direction = rotation * QuaternionDouble.LookRotation(new Vector3Double(Mathf.Lerp(frustumCorners[0].x, frustumCorners[2].x, pos.x), Mathf.Lerp(frustumCorners[0].y, frustumCorners[2].y, pos.y), frustumCorners[0].z).normalized);
            }
            
            return new RayDouble(origin, direction * Vector3Double.forward);
        }

        public double GetDistanceScaleForCamera(Vector3 position)
        {
            return Math.Abs(GetFrustumSizeAtDistance(unityCamera.WorldToViewportPoint(position).z).y / pixelHeight);
        }

        public Vector3Double GetFrustumSizeAtDistance(double distance)
        {
            double height = 2.0d * (orthographic ? orthographicSize : distance * Math.Tan(fieldOfView * 0.5d * MathPlus.DEG2RAD));
            return new Vector3Double(height * aspect, height, distance);
        }

        public Vector3 WorldToViewportPoint(Vector3Double position)
        {
            return _unityCamera.WorldToViewportPoint(TransformDouble.SubtractOrigin(position));
        }

        public Vector3 WorldToViewportPoint(Vector3Double position, UnityEngine.Camera.MonoOrStereoscopicEye eye)
        {
            return _unityCamera.WorldToViewportPoint(TransformDouble.SubtractOrigin(position), eye);
        }

        public bool ProjectPoint(out Vector3Double projectedPoint, Vector3Double point, Vector3Double planeDirection, Vector3Double planeCenter)
        {
            Vector3 viewPoint = WorldToViewportPoint(point);
            
            if (viewPoint.z > 0.0f)
            {
                viewPoint.z = 0.0f;
                RayDouble ray = ViewportPointToRay(viewPoint);
                if (new PlaneDouble(planeDirection, planeCenter).Raycast(ray, out double enter))
                {
                    projectedPoint = ray.GetPoint(enter);
                    return true;
                }

            }

            projectedPoint = Vector3Double.negativeInfinity;
            return false;
        }

        public override bool HierarchicalBeginCameraRendering(Camera camera)
        {
            if (base.HierarchicalBeginCameraRendering(camera))
            {
                ApplyPropertiesToAllUnityCamera();

                return true;
            }
            return false;
        }

        public override bool HierarchicalEndCameraRendering(Camera camera)
        {
            if (base.HierarchicalEndCameraRendering(camera))
            {
                ApplyPropertiesToUnityCamera();

                return true;
            }
            return false;
        }

        private void ApplyPropertiesToAllUnityCamera()
        {
            int mainStackCount = 0;

            IterateOverCameraStack((unityCamera, i, stack) =>
            {
                bool synchRenderProperties = false;
                bool synchOpticalProperties = false;
                bool synchAspectProperty = false;
                bool synchBackgroundProperties = false;
                bool synchClipPlaneProperties = false;
                bool synchCullingMaskProperty = false;

                if (stack != null)
                {
                    synchRenderProperties = stack.synchRenderProperties;
                    synchOpticalProperties = stack.synchOpticalProperties;
                    synchAspectProperty = stack.synchAspectProperty;
                    synchBackgroundProperties = stack.synchBackgroundProperties;
                    synchClipPlaneProperties = stack.synchClipPlaneProperties;
                    synchCullingMaskProperty = stack.synchCullingMaskProperty;
                    if (stack.main)
                        mainStackCount++;
                }

                if (synchRenderProperties)
                {
                    ApplyRenderPropertiesToUnityCamera(unityCamera);
                    ApplyPostProcessingPropertyToUnityCamera(unityCamera, i);
                }
                if (synchOpticalProperties)
                    ApplyOpticalPropertiesToUnityCamera(unityCamera);
                if (synchAspectProperty)
                    ApplyAspectPropertyToUnityCamera(unityCamera);
                if (synchBackgroundProperties)
                    ApplyBackgroundPropertiesToUnityCamera(unityCamera);
                if (synchClipPlaneProperties)
                    ApplyClipPlanePropertiesToUnityCamera(unityCamera, i);
                if (synchCullingMaskProperty)
                    ApplyCullingMaskPropertyToUnityCamera(unityCamera);
            });

            ApplyPropertiesToUnityCamera(mainStackCount);
        }

        private void ApplyPropertiesToUnityCamera(int i = 0)
        {
            ApplyRenderPropertiesToUnityCamera(unityCamera);
            ApplyPostProcessingPropertyToUnityCamera(unityCamera, i);
            ApplyOpticalPropertiesToUnityCamera(unityCamera);
            ApplyBackgroundPropertiesToUnityCamera(unityCamera);
            ApplyClipPlanePropertiesToUnityCamera(unityCamera, i);
            ApplyCullingMaskPropertyToUnityCamera(unityCamera);
        }

        private void ApplyRenderPropertiesToUnityCamera(UnityEngine.Camera unityCamera)
        {
            unityCamera.useOcclusionCulling = useOcclusionCulling;
            unityCamera.allowHDR = allowHDR;

            if (unityCamera != this.unityCamera)
            {
                UniversalAdditionalCameraData unityCameraUniversalAdditionalCameraData = unityCamera.GetUniversalAdditionalCameraData();
                if (unityCameraUniversalAdditionalCameraData != null)
                {
                    UniversalAdditionalCameraData universalAdditionalCameraData = GetUniversalAdditionalCameraData();
                    if (universalAdditionalCameraData != null)
                    {
                        unityCameraUniversalAdditionalCameraData.renderShadows = universalAdditionalCameraData.renderShadows;
                        unityCameraUniversalAdditionalCameraData.volumeLayerMask = universalAdditionalCameraData.volumeLayerMask;
                        unityCameraUniversalAdditionalCameraData.volumeTrigger = universalAdditionalCameraData.volumeTrigger;
                    }
                }
            }
        }

        private void ApplyOpticalPropertiesToUnityCamera(UnityEngine.Camera unityCamera)
        {
            unityCamera.fieldOfView = fieldOfView;
            unityCamera.orthographic = orthographic;
            unityCamera.orthographicSize = orthographicSize;
        }

        protected virtual void ApplyPostProcessingPropertyToUnityCamera(UnityEngine.Camera unityCamera, int i)
        {
            UniversalAdditionalCameraData unityCameraUniversalAdditionalCameraData = unityCamera.GetUniversalAdditionalCameraData();
            if (unityCameraUniversalAdditionalCameraData != null)
                unityCameraUniversalAdditionalCameraData.renderPostProcessing = i == 0 ? postProcessing : false;
        }

        private void ApplyAspectPropertyToUnityCamera(UnityEngine.Camera unityCamera)
        {
            unityCamera.aspect = aspect;
        }

        private void ApplyBackgroundPropertiesToUnityCamera(UnityEngine.Camera unityCamera)
        {
            unityCamera.clearFlags = clearFlags;
            unityCamera.backgroundColor = backgroundColor;
        }

        protected virtual void ApplyCullingMaskPropertyToUnityCamera(UnityEngine.Camera unityCamera)
        {
            unityCamera.cullingMask = cullingMask;
        }

        protected void ApplyClipPlanePropertiesToUnityCamera(UnityEngine.Camera unityCamera, int i)
        {
            ApplyClipPlanePropertiesToUnityCamera(unityCamera, i, nearClipPlane, farClipPlane);
        }

        public static void ApplyClipPlanePropertiesToUnityCamera(UnityEngine.Camera unityCamera, int i, float nearClipPlane, float farClipPlane)
        {
            unityCamera.farClipPlane = GetFarClipPlane(i, farClipPlane);
            unityCamera.nearClipPlane = i == 0 ? nearClipPlane : Mathf.Pow(10000.0f, i - 1.0f) * farClipPlane;
        }

        public static float GetFarClipPlane(int i, float farClipPlane)
        {
            return Mathf.Pow(10000.0f, i) * farClipPlane;
        }

        public void IterateOverCameraStack(Action<UnityEngine.Camera, int, Stack> callback)
        {
            if (callback != null && additionalData != null && additionalData.scriptableRenderer != null && additionalData.cameraStack != null)
            {
                if (_stacks != null)
                {
                    foreach (Stack stack in _stacks)
                        stack.index = 0;

                    for (int i = additionalData.cameraStack.Count - 1; i >= 0; i--)
                    {
                        UnityEngine.Camera stackedUnityCamera = additionalData.cameraStack[i];

                        if (stackedUnityCamera != null && stackedUnityCamera.isActiveAndEnabled)
                        {
                            Stack stack = stackedUnityCamera.GetComponentInParent<Stack>();
                            if (stack != null)
                            {
                                callback(stackedUnityCamera, stack.index, stack);
                                stack.index++;
                            }
                            else
                                callback(stackedUnityCamera, 0, null);
                        }
                }
                }
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            Dispose(_environmentCubemap);
        }
    }
}
