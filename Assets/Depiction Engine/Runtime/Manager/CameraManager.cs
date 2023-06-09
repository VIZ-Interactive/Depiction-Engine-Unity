// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace DepictionEngine
{
    /// <summary>
    /// Singleton managing <see cref="DepictionEngine.Camera"/>'s.
    /// </summary>
    [AddComponentMenu(SceneManager.NAMESPACE + "/Manager/" + nameof(CameraManager))]
    [RequireComponent(typeof(SceneManager))]
    [DisallowMultipleComponent]
    public class CameraManager : ManagerBase
    {
        public static readonly string IGNORE_RENDER_LAYER_NAME = "Ignore Render";

        private const int MAX_DISTANCE_PASS = 8;

        public const double MAX_CAMERA_DISTANCE = 1900000000000.0d;
                                                 
        public const string SCENECAMERA_NAME = "SceneCamera";

        [BeginFoldout(SCENECAMERA_NAME)]
        [SerializeField, Tooltip(Camera.CLEAR_FLAGS_TOOLTIP)]
        private CameraClearFlags _clearFlags;
        [SerializeField, Tooltip(Camera.BACKGROUND_COLOR_TOOLTIP)]
#if UNITY_EDITOR
        [ConditionalShow(nameof(GetShowBackgroundColor))]
#endif
        private Color _backgroundColor;
        [SerializeField, Tooltip(Camera.SKYBOX_MATERIAL_PATH_TOOLTIP)]
        private string _skyboxMaterialPath;
        [SerializeField, Range(0.0f, MAX_DISTANCE_PASS), Tooltip("The number of distance pass the renderer should do in order to draw distant objects.")]
        private int _distancePass;
        [SerializeField, Tooltip(Camera.ENVIRONMENT_TEXTURE_SIZE_TOOLTIP), EndFoldout]
        private int _environmentTextureSize;

        [BeginFoldout(SCENECAMERA_NAME + "Target")]
        [SerializeField, Tooltip("A min and max clamping values for the camera distance."), EndFoldout]
        private Vector2Double _minMaxCameraDistance;

#if UNITY_EDITOR
        private bool GetShowBackgroundColor()
        {
            return clearFlags == CameraClearFlags.Color;
        }
#endif

        private static CameraManager _instance;
        /// <summary>
        /// Get a singleton of the manager.
        /// </summary>
        /// <param name="createIfMissing">If true a new instance will be created if one doesn't already exist. </param>
        /// <returns>An instance of the manager if the context allows.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CameraManager Instance(bool createIfMissing = true)
        {
            if (_instance == Disposable.NULL)
                _instance = GetManagerComponent<CameraManager>(createIfMissing);
            return _instance;
        }

        protected override void InitializeSerializedFields(InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            InitValue(value => clearFlags = value, CameraClearFlags.Skybox, initializingContext);
            InitValue(value => backgroundColor = value, Color.black, initializingContext);
            InitValue(value => skyboxMaterialPath = value, RenderingManager.MATERIAL_BASE_PATH + "Skybox/Star-Skybox", initializingContext);
            InitValue(value => distancePass = value, Camera.DEFAULT_DISTANCE_PASS, initializingContext);
            InitValue(value => environmentTextureSize = value, 512, initializingContext);
            InitValue(value => minMaxCameraDistance = value, new Vector2Double(0.01d, MAX_CAMERA_DISTANCE), initializingContext);
        }

        /// <summary>
        /// How the camera clears the background.
        /// </summary>
        public CameraClearFlags clearFlags
        {
            get => _clearFlags;
            set => SetValue(nameof(clearFlags), value, ref _clearFlags);
        }

        /// <summary>
        /// The color with which the screen will be cleared.
        /// </summary>
        public Color backgroundColor
        {
            get => _backgroundColor;
            set => SetValue(nameof(backgroundColor), value, ref _backgroundColor);
        }

        /// <summary>
        /// The path of the material within the Resources directory, such as 'Star-Skybox' or Atmosphere-Skybox.
        /// </summary>
        public string skyboxMaterialPath
        {
            get => _skyboxMaterialPath;
            set => SetValue(nameof(skyboxMaterialPath), value, ref _skyboxMaterialPath);
        }

        /// <summary>
        /// The number of distance pass the renderer should do in order to draw distant objects.
        /// </summary>
        public int distancePass
        {
            get => _distancePass;
            set => SetValue(nameof(distancePass), (int)Mathf.Clamp(value, 0.0f, MAX_DISTANCE_PASS), ref _distancePass);
        }

        /// <summary>
        /// A power of two value used to establish the width/height of the environment cubemap texture.
        /// </summary>
        public int environmentTextureSize
        {
            get => _environmentTextureSize;
            set 
            {
                if (value < 2)
                    value = 2;
                SetValue(nameof(environmentTextureSize), value, ref _environmentTextureSize); 
            }
        }

        /// <summary>
        /// A min and max clamping values for the camera distance.
        /// </summary>
        public Vector2Double minMaxCameraDistance
        {
            get => _minMaxCameraDistance;
            set 
            {
                if (value.y > MAX_CAMERA_DISTANCE)
                    value.y = MAX_CAMERA_DISTANCE;
                SetValue(nameof(minMaxCameraDistance), value, ref _minMaxCameraDistance); 
            }
        }

        private PropertyInfo _targetDisplayProperty;
        private Type _playModeViewType;
        public Camera GetMouseOverWindowCamera()
        {
            Camera mouseOverWindowCamera = null;

#if UNITY_EDITOR
            if (_targetDisplayProperty == null || _playModeViewType == null)
            {
                Assembly assembly = typeof(UnityEditor.EditorWindow).Assembly;
                _playModeViewType = assembly.GetType("UnityEditor.PlayModeView");
                _targetDisplayProperty = _playModeViewType.GetProperty("targetDisplay", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            }

            UnityEditor.EditorWindow mouseOverWindow = UnityEditor.EditorWindow.mouseOverWindow;
            if (_playModeViewType.IsInstanceOfType(mouseOverWindow))
            {
                int targetDisplayID = (int)_targetDisplayProperty.GetValue(mouseOverWindow);
                instanceManager.IterateOverInstances<Camera>(
                    (camera) =>
                    {
                        if (!(camera is Editor.SceneCamera) && camera.unityCamera.targetDisplay == targetDisplayID)
                        {
                            mouseOverWindowCamera = camera;
                            return false;
                        }
                        return true;
                    });
            }
#else
            mouseOverWindowCamera = Camera.main;
#endif

            return mouseOverWindowCamera;
        }

        public Camera GetCameraFromUnityCamera(UnityEngine.Camera unityCamera)
        {
            Camera result = null;

            instanceManager.IterateOverInstances<Camera>(
                (camera) =>
                {
                    if (camera.unityCamera == unityCamera)
                    {
                        result = camera;
                        return false;
                    }
                    return true;
                });

            return result;
        }
    }
}
