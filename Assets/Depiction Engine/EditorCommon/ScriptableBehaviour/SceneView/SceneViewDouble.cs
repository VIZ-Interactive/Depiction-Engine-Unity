// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using UnityEditor.AnimatedValues;
using UnityEngine.Events;
using System.Collections.Generic;

namespace DepictionEngine.Editor
{
    /// <summary>
    /// A 64 bit double version of the SceneView.
    /// </summary>
    public class SceneViewDouble : PersistentScriptableObject
    {
        private const string AUTO_SNAP_VIEW_TO_TERRAIN_EDITOR_PREFS_NAME = "_AutoSnapViewToTerrain";
        private const string ALIGN_VIEW_TO_GEOASTROOBJECT_EDITOR_PREFS_NAME = "_AlignViewToGeoAstroObject";
        private const string PIVOT_GEOCOORDINATE_EDITOR_PREFS_NAME = "_PivotGeoCoordinate";
        private const string PIVOT_EDITOR_PREFS_NAME = "_Pivot";
        private const string ROTATION_EDITOR_PREFS_NAME = "_Rotation";
        private const string CAMERA_DISTANCE_EDITOR_PREFS_NAME = "_CameraDistance";

        private const double MAX_TARGET_POSITION_DISTANCE = 1E+300;

        private const double k_KHandleSize = 80.0d;
        private const float k_MaxSceneViewSize = 3.2e34f;
        private const float kOrthoThresholdAngle = 3f;

        private enum DraggingLockedState
        {
            NotDragging, // Default state. Scene view camera is snapped to selected object instantly
            Dragging, // User is dragging from handles. Scene view camera holds still.
            LookAt // Temporary state after dragging or selection change, where we return scene view camera smoothly to selected object
        }

        [SerializeField]
        private int _sceneViewInstanceId;

        [SerializeField]
        private bool _autoSnapViewToTerrain;
        [SerializeField]
        private GeoAstroObject _alignViewToGeoAstroObject;

        [SerializeField]
        private GeoCoordinate3Double _pivotGeoCoordinate;
        [SerializeField]
        private Vector3Double _pivot;
        [SerializeField]
        private QuaternionDouble _rotation;
        [SerializeField]
        private double _cameraDistance;

        private Vector3 _lastSceneViewPivot;
        private Quaternion _lastSceneViewRotation;
        private float _lastSceneViewCameraDistance;

        private Vector3 _lastAnimSceneViewPivot;
        private Quaternion _lastAnimSceneViewRotation;
        private float _lastAnimSceneViewCameraDistance;

        private float _lastHandleDistanceFactor;
        private Vector3 _lastHandlePosition;

        private bool _showMockHandles;
        private int _handleCount;

        private bool _forceHandleVisibility;

        private bool _deleted;
        private InstanceManager.InitializationContext _sceneViewinitializingContext;

        private TargetControllerComponents _sceneViewDoubleComponents;
        private SceneViewDoubleComponentsDelta _sceneViewDoubleComponentsDelta;

        private static SceneViewDouble _currentSceneViewDouble;

        private EventInfo _onGUIStartedEventInfo;
        private Delegate _onGUIStartedDelegate;
        private MethodInfo _onGUIStartedRemoveDelegate;
        private MethodInfo _onGUIStartedAddDelegate;

        private EventInfo _onGUIEndedEventInfo;
        private Delegate _onGUIEndedDelegate;
        private MethodInfo _onGUIEndedRemoveDelegate;
        private MethodInfo _onGUIEndedAddDelegate;

        protected override void InitializeFields(InstanceManager.InitializationContext initializingContext)
        {
            base.InitializeFields(initializingContext);

            _sceneViewinitializingContext = initializingContext;

            if (initializingContext == InstanceManager.InitializationContext.Existing)
            {
                if (SceneManager.playModeState == PlayModeStateChange.ExitingPlayMode)
                {
                    _autoSnapViewToTerrain = EditorPrefs.GetBool(id + AUTO_SNAP_VIEW_TO_TERRAIN_EDITOR_PREFS_NAME);
                    EditorPrefs.DeleteKey(id + AUTO_SNAP_VIEW_TO_TERRAIN_EDITOR_PREFS_NAME);

                    _alignViewToGeoAstroObject = EditorUtility.InstanceIDToObject(EditorPrefs.GetInt(id + ALIGN_VIEW_TO_GEOASTROOBJECT_EDITOR_PREFS_NAME)) as GeoAstroObject;
                    EditorPrefs.DeleteKey(id + ALIGN_VIEW_TO_GEOASTROOBJECT_EDITOR_PREFS_NAME);

                    if (JsonUtility.FromJson(out GeoCoordinate3Double pivotGeoCoordinate, JSONObject.Parse(EditorPrefs.GetString(id + PIVOT_GEOCOORDINATE_EDITOR_PREFS_NAME))))
                        _pivotGeoCoordinate = pivotGeoCoordinate;
                    EditorPrefs.DeleteKey(id + PIVOT_GEOCOORDINATE_EDITOR_PREFS_NAME);

                    if (JsonUtility.FromJson(out Vector3Double pivot, JSONObject.Parse(EditorPrefs.GetString(id + PIVOT_EDITOR_PREFS_NAME))))
                        _pivot = pivot;
                    EditorPrefs.DeleteKey(id + PIVOT_EDITOR_PREFS_NAME);

                    if (JsonUtility.FromJson(out QuaternionDouble rotation, JSONObject.Parse(EditorPrefs.GetString(id + ROTATION_EDITOR_PREFS_NAME))))
                        _rotation = rotation;
                    EditorPrefs.DeleteKey(id + ROTATION_EDITOR_PREFS_NAME);

                    _cameraDistance = Convert.ToDouble(EditorPrefs.GetString(id + CAMERA_DISTANCE_EDITOR_PREFS_NAME));
                    EditorPrefs.DeleteKey(id + CAMERA_DISTANCE_EDITOR_PREFS_NAME);
                }
            }
        }

        public SceneViewDouble InitSceneView(SceneView sceneView)
        {
            _sceneViewInstanceId = sceneView.GetInstanceID();

            if (_sceneViewinitializingContext == InstanceManager.InitializationContext.Programmatically)
            {
                pivot = sceneView.pivot;
                rotation = sceneView.rotation;
                cameraDistance = GetCameraDistanceFromSize(sceneView.size);
            }

            return this;
        }

        public static void InitSceneViewDoubles(ref List<SceneViewDouble> sceneViewDoubles)
        {
            sceneViewDoubles ??= new List<SceneViewDouble>();

            foreach (SceneViewDouble sceneViewDouble in sceneViewDoubles)
            {
                if (sceneViewDouble != Disposable.NULL)
                    sceneViewDouble.deleted = true;
            }

            foreach (SceneView sceneView in SceneView.sceneViews)
            {
                SceneViewDouble sceneViewDouble = GetSceneViewDouble(sceneViewDoubles, sceneView);
                if (sceneViewDouble == Disposable.NULL)
                {
                    sceneViewDouble = InstanceManager.Instance().CreateInstance<SceneViewDouble>().InitSceneView(sceneView);
                    sceneViewDoubles.Add(sceneViewDouble);
                }
                sceneViewDouble.InitCamera();
                sceneViewDouble.deleted = false;
            }

            for (int i = sceneViewDoubles.Count - 1; i >= 0; i--)
            {
                SceneViewDouble sceneViewDouble = sceneViewDoubles[i];
                if (sceneViewDouble == Disposable.NULL || sceneViewDouble.deleted)
                {
                    sceneViewDoubles.RemoveAt(i);
                    DisposeManager.Dispose(sceneViewDouble, DisposeManager.DisposeContext.Programmatically);
                }
            }
        }

        public void InitCamera()
        {
            if (IsDisposing())
                return;

            if (_camera == Disposable.NULL)
            {
                //Dont Initialize Camera here by calling GetSafeComponent<Camera>(), let them initialize in the Update loop
                _camera = sceneView.camera.GetComponent<SceneCamera>();
                if (_camera == Disposable.NULL)
                {
                    _camera = sceneView.camera.gameObject.AddSafeComponent(typeof(SceneCamera), InstanceManager.InitializationContext.Programmatically) as SceneCamera;
                    InitSceneCamera(_camera);
                }
            }
        }

        private void InitSceneCamera(SceneCamera sceneCamera)
        {
            if (sceneCamera != Disposable.NULL && !sceneCamera.sceneCameraController.wasFirstUpdatedBySceneViewDouble)
            {
                SceneView sceneView = GetSceneView(sceneCamera);
                if (sceneView != null)
                {
                    PostSetupCamera(sceneView);

                    InitSceneViewDoubleComponents();

                    UpdateSceneCamera(sceneCamera, _sceneViewDoubleComponents);
                }
            }
        }

        protected override bool UpdateAllDelegates()
        {
            if (base.UpdateAllDelegates())
            {
                SceneManager.BeforeAssemblyReloadEvent -= BeforeAssemblyReloadHandler;
                if (!IsDisposing())
                    SceneManager.BeforeAssemblyReloadEvent += BeforeAssemblyReloadHandler;

                SceneManager.PlayModeStateChangedEvent -= PlayModeStateChangedHandler;
                if (!IsDisposing())
                    SceneManager.PlayModeStateChangedEvent += PlayModeStateChangedHandler;

                UpdateOnGUIDelegates();

                return true;
            }
            return false;
        }

        private void PlayModeStateChangedHandler(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                EditorPrefs.SetBool(id + AUTO_SNAP_VIEW_TO_TERRAIN_EDITOR_PREFS_NAME, _autoSnapViewToTerrain);
                
                EditorPrefs.SetInt(id + ALIGN_VIEW_TO_GEOASTROOBJECT_EDITOR_PREFS_NAME, _alignViewToGeoAstroObject != Disposable.NULL ? _alignViewToGeoAstroObject.GetInstanceID() : 0);
                
                if (JsonUtility.FromJson(out string pivotGeoCoordinateJsonStr, JsonUtility.ToJson(_pivotGeoCoordinate)))
                    EditorPrefs.SetString(id + PIVOT_GEOCOORDINATE_EDITOR_PREFS_NAME, pivotGeoCoordinateJsonStr);
                
                if (JsonUtility.FromJson(out string pivotJsonStr, JsonUtility.ToJson(_pivot)))
                    EditorPrefs.SetString(id + PIVOT_EDITOR_PREFS_NAME, pivotJsonStr);
                
                if (JsonUtility.FromJson(out string rotationJsonStr, JsonUtility.ToJson(_rotation)))
                    EditorPrefs.SetString(id + ROTATION_EDITOR_PREFS_NAME, rotationJsonStr);
                
                if (JsonUtility.FromJson(out string cameraDistanceJsonStr, JsonUtility.ToJson(_cameraDistance)))
                    EditorPrefs.SetString(id + CAMERA_DISTANCE_EDITOR_PREFS_NAME, cameraDistanceJsonStr);
            }
        }

        private static Material _handleArcMaterial;
        private static void PatchedPostSetupArcMaterial(ref Material __result)
        {
            RenderingManager renderingManager = RenderingManager.Instance(false);
            if (renderingManager != Disposable.NULL && renderingManager.originShifting)
            {
                if (Camera.current != null && __result != null)
                {
                    if (_handleArcMaterial == null)
                        _handleArcMaterial = Resources.Load<Material>("Material/Editor/SceneView/CircularArc");

                    Material mat = _handleArcMaterial;

                    int kPropUseGuiClip = Shader.PropertyToID("_UseGUIClip");
                    mat.SetFloat(kPropUseGuiClip, __result.GetFloat(kPropUseGuiClip));
                    int kPropHandleZTest = Shader.PropertyToID("_HandleZTest");
                    mat.SetFloat(kPropHandleZTest, __result.GetFloat(kPropHandleZTest));
                    int kPropColor = Shader.PropertyToID("_Color");
                    mat.SetColor(kPropColor, __result.GetColor(kPropColor));
                    int kPropHandlesMatrix = Shader.PropertyToID("_HandlesMatrix");
                    mat.SetMatrix(kPropHandlesMatrix, __result.GetMatrix(kPropHandlesMatrix));

                    int kPropCameraPixelHeight = Shader.PropertyToID("_CameraPixelHeight");
                    mat.SetFloat(kPropCameraPixelHeight, Camera.current.pixelHeight);

                    int kPropCameraOrthographicSize = Shader.PropertyToID("_CameraOrthographicSize");
                    mat.SetFloat(kPropCameraOrthographicSize, Camera.current.orthographicSize);

                    __result = mat;
                }
            }
        }

        private void UpdateOnGUIDelegates(bool reloadingAssembly = false)
        {
            Type sceneViewType = typeof(SceneView);

            if (_onGUIStartedRemoveDelegate == null)
            {
                string onGUIStartedMethodName = nameof(onGUIStarted);
                _onGUIStartedEventInfo = sceneViewType.GetEvent(onGUIStartedMethodName, BindingFlags.Static | BindingFlags.NonPublic);
                MethodInfo onGUIStartedMethodInfo = GetType().GetMethod(onGUIStartedMethodName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                _onGUIStartedDelegate = Delegate.CreateDelegate(_onGUIStartedEventInfo.EventHandlerType, this, onGUIStartedMethodInfo);
                _onGUIStartedRemoveDelegate = _onGUIStartedEventInfo.GetRemoveMethod(true);
                _onGUIStartedAddDelegate = _onGUIStartedEventInfo.GetAddMethod(true);
            }

            if (_onGUIEndedRemoveDelegate == null)
            {
                string onGUIEndedMethodName = nameof(onGUIEnded);
                _onGUIEndedEventInfo = sceneViewType.GetEvent(onGUIEndedMethodName, BindingFlags.Static | BindingFlags.NonPublic);
                MethodInfo onGUIEndedMethodInfo = GetType().GetMethod(onGUIEndedMethodName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                _onGUIEndedDelegate = Delegate.CreateDelegate(_onGUIEndedEventInfo.EventHandlerType, this, onGUIEndedMethodInfo);
                _onGUIEndedRemoveDelegate = _onGUIEndedEventInfo.GetRemoveMethod(true);
                _onGUIEndedAddDelegate = _onGUIEndedEventInfo.GetAddMethod(true);
            }

            _onGUIStartedRemoveDelegate.Invoke(_onGUIStartedEventInfo, new[] { _onGUIStartedDelegate });
            _onGUIEndedRemoveDelegate.Invoke(_onGUIEndedEventInfo, new[] { _onGUIEndedDelegate });
            if (!reloadingAssembly && !IsDisposing())
            {
                _onGUIStartedAddDelegate.Invoke(_onGUIStartedEventInfo, new[] { _onGUIStartedDelegate });
                _onGUIEndedAddDelegate.Invoke(_onGUIEndedEventInfo, new[] { _onGUIEndedDelegate });
            }
        }

        private void BeforeAssemblyReloadHandler()
        {
            UpdateOnGUIDelegates(true);
        }

        public bool deleted
        {
            get { return _deleted; }
            set { _deleted = value; }
        }

        public int sceneViewInstanceId
        {
            get { return _sceneViewInstanceId; }
            private set
            {
                if (_sceneViewInstanceId == value)
                    return;

                _sceneViewInstanceId = value;
            }
        }

        protected override bool GetDefaultDontSaveToScene()
        {
            return true;
        }

        protected override bool AddInstanceToManager()
        {
            return false;
        }

        public static SceneViewDouble lastActiveSceneViewDouble
        {
            get 
            {
                SceneViewDouble lastActiveSceneViewDouble = GetSceneViewDouble(EditorWindow.mouseOverWindow as SceneView);
                if (lastActiveSceneViewDouble == Disposable.NULL)
                    lastActiveSceneViewDouble = GetSceneViewDouble(SceneView.lastActiveSceneView);
                return lastActiveSceneViewDouble;
            }
        }

        public static List<SceneViewDouble> sceneViewDoubles
        {
            get 
            {
                SceneManager sceneManager = SceneManager.Instance(false);
                return sceneManager != Disposable.NULL ? sceneManager.sceneViewDoubles : null;
            }
        }

        public bool showMockHandles
        {
            get { return _showMockHandles; }
            set
            {
                if (_showMockHandles == value)
                    return;
                _showMockHandles = value;
            }
        }

        public int handleCount
        {
            get { return _handleCount; }
            set { _handleCount = value; }
        }

        private bool _lastAutoSnapViewToTerrain;
        public bool autoSnapViewToTerrain
        {
            get { return _autoSnapViewToTerrain; }
            set
            {
                if (_autoSnapViewToTerrain == value)
                    return;

                _lastAutoSnapViewToTerrain = _autoSnapViewToTerrain = value;
            }
        }

        private GeoAstroObject _lastAlignViewToGeoAstroObject;
        public GeoAstroObject alignViewToGeoAstroObject
        {
            get { return _alignViewToGeoAstroObject; }
            set
            {
                if (Object.ReferenceEquals(_alignViewToGeoAstroObject, value))
                    return;

                _lastAlignViewToGeoAstroObject = _alignViewToGeoAstroObject = value;

                UpdatePivotGeoCoordinate();
            }
        }

        private AnimBool m_Ortho
        {
            get { return typeof(SceneView).GetField("m_Ortho", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(sceneView) as AnimBool; }
        }

        private bool m_WasFocused
        {
            get { return (bool)typeof(SceneView).GetField("m_WasFocused", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(sceneView); }
            set { typeof(SceneView).GetField("m_WasFocused", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(sceneView, value); }
        }

        private SceneView _sceneView;
        public SceneView sceneView
        {
            get
            {
                if (_sceneView == null)
                    _sceneView = GetSceneView(this);

                return _sceneView;
            }
        }

        private SceneCamera _camera;
        public SceneCamera camera
        {
            get 
            {
                InitCamera();
                return _camera;
            }
        }

        private SceneCamera _sceneCamera;
        public SceneCamera GetSceneCamera(SceneView sceneView)
        {
            if (_sceneCamera == Disposable.NULL || _sceneCamera.unityCamera != sceneView.camera)
            {
                SceneCamera sceneCamera = sceneView.camera.GetComponent<SceneCamera>();
                if (sceneCamera != Disposable.NULL && sceneCamera.sceneCameraController != Disposable.NULL)
                    _sceneCamera = sceneCamera;
            }
            return _sceneCamera;
        }

        public static SceneViewDouble GetSceneViewDouble(Camera camera)
        {
            if (sceneViewDoubles != null && camera != Disposable.NULL)
            {
                foreach (SceneViewDouble sceneViewDouble in sceneViewDoubles)
                {
                    if (sceneViewDouble != Disposable.NULL && sceneViewDouble.camera == camera)
                        return sceneViewDouble;
                }
            }
            return null;
        }

        public static SceneViewDouble GetSceneViewDouble(int instanceId)
        {
            if (sceneViewDoubles != null)
            {
                foreach (SceneViewDouble sceneViewDouble in sceneViewDoubles)
                {
                    if (sceneViewDouble != Disposable.NULL && sceneViewDouble.GetInstanceID() == instanceId)
                        return sceneViewDouble;
                }
            }
            return null;
        }

        public static SceneView GetSceneView(Camera camera)
        {
            if (camera != Disposable.NULL)
            {
                foreach (SceneView sceneView in SceneView.sceneViews)
                {
                    if (sceneView.camera == camera.unityCamera)
                        return sceneView;
                }
            }
            return null;
        }

        public static SceneViewDouble GetSceneViewDouble(SceneView sceneView)
        {
            return GetSceneViewDouble(sceneViewDoubles, sceneView);
        }

        private static SceneViewDouble GetSceneViewDouble(List<SceneViewDouble> sceneViewDoubles, SceneView sceneView)
        {
            if (sceneViewDoubles != null && sceneView != null)
            {
                foreach (SceneViewDouble sceneViewDouble in sceneViewDoubles)
                {
                    if (sceneViewDouble != Disposable.NULL && sceneViewDouble.sceneViewInstanceId == sceneView.GetInstanceID())
                        return sceneViewDouble;
                }
            }
            return null;
        }

        public static SceneView GetSceneView(SceneViewDouble sceneViewDouble)
        {
            if (sceneViewDouble != Disposable.NULL)
            {
                foreach (SceneView sceneView in SceneView.sceneViews)
                {
                    if (sceneViewDouble.sceneViewInstanceId == sceneView.GetInstanceID())
                        return sceneView;
                }
            }
            return null;
        }

        private AnimVector3 GetSceneViewPivot(out Vector3 value, out Vector3 target, SceneView sceneView)
        {
            AnimVector3 sceneViewPivot = sceneView.GetType().GetField("m_Position", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(sceneView) as AnimVector3;

            value = ValidateSceneViewPivot(sceneViewPivot.value);

            target = ValidateSceneViewPivot(sceneViewPivot.target);

            return sceneViewPivot;
        }

        private Vector3 ValidateSceneViewPivot(Vector3 pivot)
        {
            if (float.IsInfinity(pivot.x) || float.IsInfinity(pivot.y) || float.IsInfinity(pivot.z))
                return Vector3.zero;
            else
                return pivot;
        }

        public Vector3Double pivot
        {
            get { return _pivot; }
            set
            {
                if (SetPivot(value))
                    StopPivotAnimation();
            }
        }

        private bool SetPivot(Vector3Double value)
        {
            if (_pivot == value)
                return false;

            _pivot = value;

            UpdatePivotGeoCoordinate();

            return true;
        }

        private void UpdatePivotGeoCoordinate()
        {
            pivotGeoCoordinate = alignViewToGeoAstroObject != Disposable.NULL ? alignViewToGeoAstroObject.GetGeoCoordinateFromPoint(pivot) : GeoCoordinate3Double.zero;
        }

        private GeoCoordinate3Double _lastPivotGeoCoordinate;
        private GeoCoordinate3Double pivotGeoCoordinate
        {
            get { return _pivotGeoCoordinate; }
            set
            {
                if (_pivotGeoCoordinate == value)
                    return;

                _lastPivotGeoCoordinate = _pivotGeoCoordinate = value;
            }
        }

        private AnimFloat _animPivot;
        private Vector3Double _fromPivot;
        private Vector3Double _toPivot;
        private GeoCoordinate3Double _fromPivotGeoCoordinate;
        private GeoCoordinate3Double _toPivotGeoCoordinate;
        public void SetPivotTarget(Vector3Double value, float speed = 2.0f)
        {
            StopPivotAnimation();
            _animPivot ??= new AnimFloat(0.0f, AnimPivotChanged);
            if (_animPivot.valueChanged == null)
            {
                _animPivot.valueChanged = new UnityEvent();
                _animPivot.valueChanged.AddListener(AnimPivotChanged);
            }
            _animPivot.speed = speed;
            
            _fromPivot = pivot;
            _fromPivotGeoCoordinate = alignViewToGeoAstroObject != Disposable.NULL ? alignViewToGeoAstroObject.GetGeoCoordinateFromPoint(_fromPivot) : GeoCoordinate3Double.zero;
            
            _toPivot = value;
            _toPivotGeoCoordinate = alignViewToGeoAstroObject != Disposable.NULL ? alignViewToGeoAstroObject.GetGeoCoordinateFromPoint(_toPivot) : GeoCoordinate3Double.zero;
            
            _animPivot.value = 0.0f;
            _animPivot.target = 1.0f;
            if (_animPivot.speed == 0.0f)
                _animPivot.value = 1.0f;
        }

        private Vector3Double GetPivotTarget()
        {
            return _animPivot != null &&  _animPivot.isAnimating ? _toPivot : pivot;
        }

        private void AnimPivotChanged()
        {
            Vector3Double pivot;

            if (alignViewToGeoAstroObject == Disposable.NULL)
                pivot = Vector3Double.Lerp(_fromPivot, _toPivot, _animPivot.value);
            else
                pivot = alignViewToGeoAstroObject.GetPointFromGeoCoordinate(GeoCoordinate3Double.Lerp(_fromPivotGeoCoordinate, _toPivotGeoCoordinate, _animPivot.value));

            SetPivot(pivot);
        }

        public void StopPivotAnimation()
        {
            if (_sceneViewPivot != null)
                _sceneViewPivot.value = _sceneViewPivot.value;
            if (_animPivot != null)
                _animPivot.value = _animPivot.value;
        }

        private void SetSceneViewPivotTarget(Vector3 value, float speed = 0.0f)
        {
            _sceneViewPivot.speed = speed;
            _sceneViewPivot.target = value;
        }

        private Vector3 GetSceneViewPivotValue()
        {
            return _sceneViewPivot.value;
        }

        private Vector3 GetLastSceneViewPivot()
        {
            return _sceneViewPivot.isAnimating ? _lastAnimSceneViewPivot : _lastSceneViewPivot;
        }

        private AnimVector3 _sceneViewPivot;
        private void UpdateSceneViewPivot(SceneView sceneView)
        {
            _sceneViewPivot ??= new AnimVector3(Vector3.zero);

            AnimVector3 sceneViewPivot = GetSceneViewPivot(out Vector3 sceneViewPivotValue, out Vector3 sceneViewPivotTarget, sceneView);
            if (sceneViewPivot.isAnimating)
            {
                if (!_sceneViewPivot.isAnimating || _sceneViewPivot.target != sceneViewPivotTarget)
                    SetSceneViewPivotTarget(sceneViewPivotTarget, sceneViewPivot.speed);
            }
            else if (!_sceneViewPivot.isAnimating)
                _sceneViewPivot.value = sceneViewPivotValue;
        }

        public QuaternionDouble rotation
        {
            get { return _rotation; }
            set
            {
                if (SetRotation(value))
                    StopRotationAnimation();
            }
        }

        private bool SetRotation(QuaternionDouble value)
        {
            if (_rotation == value)
                return false;

            _rotation = value;

            return true;
        }

        private AnimFloat _animRotation;
        private QuaternionDouble _fromRotation;
        private QuaternionDouble _toRotation;
        public void SetRotationTarget(QuaternionDouble value, float speed = 2.0f)
        {
            StopRotationAnimation();
            _animRotation ??= new AnimFloat(0.0f, AnimRotationChanged);
            if (_animRotation.valueChanged == null)
            {
                _animRotation.valueChanged = new UnityEvent();
                _animRotation.valueChanged.AddListener(AnimRotationChanged);
            }
            _animRotation.speed = speed;
            _fromRotation = rotation;
            _toRotation = value;
            _animRotation.value = 0.0f;
            _animRotation.target = 1.0f;
            if (_animRotation.speed == 0.0f)
                _animRotation.value = 1.0f;
        }

        private QuaternionDouble GetRotationTarget()
        {
            return _animRotation != null && _animRotation.isAnimating ? _toRotation : rotation;
        }

        private void AnimRotationChanged()
        {
            SetRotation(QuaternionDouble.Lerp(_fromRotation, _toRotation, _animRotation.value));
        }

        public void StopRotationAnimation()
        {
            if(_sceneViewRotation != null)
                _sceneViewRotation.value = _sceneViewRotation.value;
            if (_animRotation != null)
                _animRotation.value = _animRotation.value;
        }

        private void SetSceneViewRotationTarget(Quaternion value, float speed = 0.0f)
        {
            _sceneViewRotation.speed = speed;
            _sceneViewRotation.target = value;
        }

        private Quaternion GetSceneViewRotationValue()
        {
            return _sceneViewRotation.value;
        }

        private Quaternion GetLastSceneViewRotation()
        {
            return _sceneViewRotation.isAnimating ? _lastAnimSceneViewRotation : _lastSceneViewRotation;
        }

        private AnimQuaternion _sceneViewRotation;
        private void UpdateSceneViewRotation(SceneView sceneView)
        {
            _sceneViewRotation ??= new AnimQuaternion(Quaternion.identity);
            AnimQuaternion sceneViewRotation = sceneView.GetType().GetField("m_Rotation", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(sceneView) as AnimQuaternion;
            if (sceneViewRotation.isAnimating)
            {
                if (!_sceneViewRotation.isAnimating || _sceneViewRotation.target != sceneViewRotation.target)
                    SetSceneViewRotationTarget(sceneViewRotation.target, sceneViewRotation.speed);
            }
            else if (!_sceneViewRotation.isAnimating)
                _sceneViewRotation.value = sceneViewRotation.value;
        }

        public double cameraDistance
        {
            get { return _cameraDistance; }
            set
            {
                if (SetCameraDistance(value))
                    StopSizeAnimation();
            }
        }

        private bool SetCameraDistance(double value)
        {
            if (_cameraDistance == value)
                return false;

            _cameraDistance = value;

            return true;
        }

        public double size
        {
            get { return GetSizeFromCameraDistance(cameraDistance); }
            set { cameraDistance = GetCameraDistanceFromSize(value); }
        }

        private AnimFloat _animSize;
        private double _fromSize;
        private double _toSize;
        public void SetSizeTarget(double value, float speed = 2.0f)
        {
            StopSizeAnimation();
            _animSize ??= new AnimFloat(0.0f, AnimSizeChanged);
            if (_animSize.valueChanged == null)
            {
                _animSize.valueChanged = new UnityEvent();
                _animSize.valueChanged.AddListener(AnimSizeChanged);
            }
            _animSize.speed = speed;
            _fromSize = size;
            _toSize = value;
            _animSize.value = 0.0f;
            _animSize.target = 1.0f;
            if (_animSize.speed == 0.0f)
                _animSize.value = 1.0f;
        }

        private double GetSizeTarget()
        {
            return _animSize != null && _animSize.isAnimating ? _toSize : size;
        }

        private void AnimSizeChanged()
        {
            SetCameraDistance(GetCameraDistanceFromSize(_fromSize + _animSize.value * (_toSize - _fromSize)));
        }

        public void StopSizeAnimation()
        {
            if (_sceneViewSize != null)
                _sceneViewSize.value = _sceneViewSize.value;
            if (_animSize != null)
                _animSize.value = _animSize.value;
        }

        private void SetSceneViewSizeTarget(float value, float speed = 0.0f)
        {
            _sceneViewSize.speed = speed;
            _sceneViewSize.target = value;
        }

        private float GetSceneViewSizeValue()
        {
            return _sceneViewSize.value;
        }

        private float GetLastSceneViewCameraDistance()
        {
            return _sceneViewSize.isAnimating ? _lastAnimSceneViewCameraDistance : _lastSceneViewCameraDistance;
        }

        private AnimFloat _sceneViewSize;
        private void UpdateSceneViewSize(SceneView sceneView)
        {
            _sceneViewSize ??= new AnimFloat(0.0f);
            AnimFloat sceneViewSize = sceneView.GetType().GetField("m_Size", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(sceneView) as AnimFloat;
            if (sceneViewSize.isAnimating)
            {
                if (!_sceneViewSize.isAnimating || _sceneViewSize.target != sceneViewSize.target)
                    SetSceneViewSizeTarget(sceneViewSize.target, sceneViewSize.speed);
            }
            else if (!_sceneViewSize.isAnimating)
                _sceneViewSize.value = sceneViewSize.value;
        }

        private double GetCameraDistanceFromSize(double size)
        {
            double res;

            //sceneView.camera.orthographic is usually updated in the SetupCamera() which is called during onGuiStarted.
            //Since this code is execuded before the SetupCamera we make sure the fov is above threshold otherwise we treat it as if it was orthographic
            float fov = m_Ortho.Fade(sceneView.cameraSettings.fieldOfView, 0);
            if (!sceneView.camera.orthographic && fov > kOrthoThresholdAngle)
                res = GetPerspectiveCameraDistance(size, fov);
            else
                res = size * 2.0d;

            // clamp to allowed range in case scene view size was huge
            return Math.Clamp(res, -k_MaxSceneViewSize, k_MaxSceneViewSize);
        }

        private double GetPerspectiveCameraDistance(double objectSize, float fov)
        {
            //        A
            //        |\        We want to place camera at a
            //        | \       distance that, at the given FOV,
            //        |  \      would enclose a sphere of radius
            //     _..+.._\     "size". Here |BC|=size, and we
            //   .'   |   '\    need to find |AB|. ACB is a right
            //  /     |    _C   angle, andBAC is half the FOV. So
            // |      | _-   |  that gives: sin(BAC)=|BC|/|AB|,
            // |      B      |  and thus |AB|=|BC|/sin(BAC).
            // |             |
            //  \           /
            //   '._     _.'
            //      `````
            return objectSize / Math.Sin(fov * 0.5d * MathPlus.DEG2RAD);
        }

        private double GetSizeFromCameraDistance(double cameraDistance)
        {
            double size;

            //sceneView.camera.orthographic is usually updated in the SetupCamera() which is called during onGuiStarted.
            //Since this code is execuded before the SetupCamera we make sure the fov is above threshold otherwise we treat it as if it was orthographic
            float fov = m_Ortho.Fade(sceneView.cameraSettings.fieldOfView, 0);
            if (!sceneView.camera.orthographic && fov > kOrthoThresholdAngle)
                size = GetSizeFromCameraDistance(cameraDistance, fov);
            else
                size = cameraDistance / 2.0d;

            return size;
        }

        private double GetSizeFromCameraDistance(double cameraDistance, float fov)
        {
            return cameraDistance * Math.Sin(fov * 0.5d * MathPlus.DEG2RAD);
        }

        private double CalcCameraDist()
        {
            float fov = m_Ortho.Fade(sceneView.cameraSettings.fieldOfView, 0);
            if (fov > kOrthoThresholdAngle)
            {
                sceneView.camera.orthographic = false;
                return GetPerspectiveCameraDistance(GetSizeFromCameraDistance(cameraDistance), fov);
            }
            return 0.0d;
        }

        private static double ValidateSceneSize(double value)
        {
            if (value == 0.0d || double.IsNaN(value))
                return double.Epsilon;
            if (value > k_MaxSceneViewSize)
                return k_MaxSceneViewSize;
            if (value < -k_MaxSceneViewSize)
                return -k_MaxSceneViewSize;
            return value;
        }

        private FieldInfo GetDraggingLockedStateField()
        {
            return typeof(SceneView).GetField("m_DraggingLockedState", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        private DraggingLockedState draggingLocked
        {
            get { return (DraggingLockedState)GetDraggingLockedStateField().GetValue(sceneView); }
            set { GetDraggingLockedStateField().SetValue(sceneView, (int)value); }
        }

        private void UpdateGizmoLabel(SceneView sceneView, Vector3Double direction, bool ortho)
        {
            Assembly assembly = Assembly.GetAssembly(typeof(SceneView));
            
            MethodInfo methodInfo = assembly.GetType("SceneOrientationGizmo").GetMethod("UpdateGizmoLabel", BindingFlags.NonPublic | BindingFlags.Instance);
            
            FieldInfo fieldInfo = typeof(SceneView).GetField("m_OrientationGizmo", BindingFlags.NonPublic | BindingFlags.Instance);
            
            methodInfo.Invoke(fieldInfo.GetValue(sceneView), new object[] { sceneView, (Vector3)direction, ortho });
        }

        public void SetComponents(Vector3Double pivot, QuaternionDouble rotation, double cameraDistance)
        {
            this.pivot = pivot;
            this.rotation = rotation;
            this.cameraDistance = cameraDistance;
        }

        public void FixNegativeSize()
        {
            //Negative size is currently not supported
        }

        // Look at a specific point from a given direction.
        public void LookAt(Vector3Double point, QuaternionDouble direction)
        {
            FixNegativeSize();
            SetPivotTarget(point, 2.0f);
            SetRotationTarget(direction, 2.0f);
            UpdateGizmoLabel(sceneView, direction * Vector3Double.forward, m_Ortho.target);
        }

        public void LookAt(Vector3Double point, QuaternionDouble direction, double newSize, bool ortho, bool instant)
        {
            SceneViewMotion.ResetMotion();

            FixNegativeSize();

            if (instant)
            {
                pivot = point;
                rotation = direction;
                size = Math.Abs(newSize);
                m_Ortho.value = ortho;
                draggingLocked = DraggingLockedState.NotDragging;
            }
            else
            {
                SetPivotTarget(point);
                SetRotationTarget(direction);
                SetSizeTarget(ValidateSceneSize(Math.Abs(newSize)));
                m_Ortho.target = ortho;
            }

            UpdateGizmoLabel(sceneView, direction * Vector3Double.forward, m_Ortho.target);
        }

        public bool Frame(Bounds bounds, bool instant = true)
        {
            double newSize;

            if (Selection.activeTransform != Disposable.NULL && Selection.activeTransform.objectBase is GeoAstroObject)
                newSize = MathPlus.GetRadiusFromCircumference((Selection.activeTransform.objectBase as GeoAstroObject).size);
            else
            {
                newSize = bounds.extents.magnitude;

                if (double.IsInfinity(newSize))
                    return false;

                // If we have no size to focus on, bound default 10 units
                if (newSize < Mathf.Epsilon)
                    newSize = 10.0d;
            }

            MovingToTransform(UnityEditor.Selection.activeTransform.transform);
            
            // We snap instantly into target on playmode, because things might be moving fast and lerping lags behind
            LookAt(TransformDouble.AddOrigin(bounds.center), GetRotationTarget(), newSize, m_Ortho.value, instant);

            return true;
        }

        private bool _executingFrameSelect;
        public bool FrameSelected(bool lockView, bool instant)
        {
            if (!_executingFrameSelect)
            {
                if (UnityEditor.Selection.transforms.Length > 0)
                {
                    GeoAstroObject selectionParentAstroObject = GetParentGeoAstrObject(UnityEditor.Selection.transforms[0]);

                    foreach (Transform transform in UnityEditor.Selection.transforms)
                    {
                        if (UnityEditor.Selection.transforms.Length == 1)
                        {
                            UIBase ui = transform.GetComponent<UIBase>();
                            if (ui != Disposable.NULL && ui.screenSpace)
                                m_WasFocused = true;
                        }

                        if (selectionParentAstroObject != GetParentGeoAstrObject(transform))
                        {
                            selectionParentAstroObject = null;
                            break;
                        }
                    }

                    alignViewToGeoAstroObject = selectionParentAstroObject;
                    UpdateSceneCamera(GetSceneCamera(sceneView));
                }

                OriginShiftSnapshot originShiftSnapshot = null;

                bool isValidBounds = false;

                if (Selection.MoveOriginCloserToPoint(Tools.GetHandlePosition, (position) =>
                {
                    originShiftSnapshot ??= TransformDouble.GetOriginShiftSnapshot();

                    return Tools.GetMostPreciseHandle(position);
                }))
                {
                    if (m_WasFocused || Selection.MoveOriginCloserToPoint(() => { return InternalEditorUtility.CalculateSelectionBounds(false, Tools.pivotMode == PivotMode.Pivot, true).center; }))
                    {
                        _executingFrameSelect = true;
                        Tools.handlePositionComputed = false;
                        sceneView.FrameSelected(lockView, instant);
                        _executingFrameSelect = false;

                        isValidBounds = true;
                    }
                }
                
                if (!isValidBounds)
                    Debug.LogWarning("FrameSelected Failed: Object too far.");

                if (originShiftSnapshot != null)
                    TransformDouble.ApplyOriginShifting(originShiftSnapshot);

                return false;
            }
            else
                return true;
        }

        private GeoAstroObject GetParentGeoAstrObject(Transform transform) 
        {
            GeoAstroObject parentGeoAstroObject = transform.GetComponent<GeoAstroObject>();

            if (parentGeoAstroObject == Disposable.NULL)
                parentGeoAstroObject = transform.GetComponentInParent<GeoAstroObject>(true);
            
            return parentGeoAstroObject;
        }

        public void AlignViewToObject(TransformDouble t)
        {
            MovingToTransform(t.transform);

            FixNegativeSize();
            size = 10.0d;
            LookAt(t.position + t.forward * CalcCameraDist(), t.rotation);
        }

        public void AlignViewToObject(Transform t)
        {
            MovingToTransform(t);

            FixNegativeSize();
            size = 10.0d;
            LookAt(t.GetPosition() + (Vector3Double)t.forward * CalcCameraDist(), t.rotation);
        }

        private void MovingToTransform(Transform t)
        {
            if (alignViewToGeoAstroObject != Disposable.NULL)
            {
                autoSnapViewToTerrain = false;

                GeoAstroObject parentGeoAstroObject = t.GetComponentInParent<GeoAstroObject>();
                if (parentGeoAstroObject != Disposable.NULL && parentGeoAstroObject != alignViewToGeoAstroObject)
                    alignViewToGeoAstroObject = parentGeoAstroObject;
            }
        }

        public void AlignWithView()
        {
            if (Tools.GetHandlePosition(out Vector3Double handlePosition))
            {
                FixNegativeSize();

                Vector3Double dif = camera.transform.position - handlePosition;
                
                QuaternionDouble delta = QuaternionDouble.Inverse(UnityEditor.Selection.activeTransform.rotation) * camera.transform.rotation;

                delta.ToAngleAxis(out double angle, out Vector3Double axis);
                axis = UnityEditor.Selection.activeTransform.TransformDirection(axis);

                UndoManager.SetCurrentGroupName("Align with view");
                UndoManager.RecordObjects(Selection.GetTransforms().ToArray());
                UndoManager.RecordObjects(UnityEditor.Selection.transforms);

                foreach (Transform t in UnityEditor.Selection.transforms)
                {
                    TransformDouble transformDouble = t.GetComponent<TransformDouble>();
                    if (transformDouble != Disposable.NULL)
                    {
                        transformDouble.position += dif;
                        transformDouble.RotateAround(camera.transform.position, axis, angle);
                    }
                    else
                    {
                        t.transform.position += (Vector3)dif;
                        t.RotateAround(t.position, axis, (float)angle);
                    }
                }
            }
            else
                Debug.LogWarning("AlignWithView Failed: Object too far.");
        }

        public void MoveToView(TransformDouble target)
        {
            target.position = pivot;
        }

        public void MoveToView(Transform target)
        {
            target.SetPosition(pivot);
        }

        public void MoveToView()
        {
            if (Tools.GetHandlePosition(out Vector3Double handlePosition))
            {
                Vector3Double dif = pivot - handlePosition;

                UndoManager.SetCurrentGroupName("Move to view");
                UndoManager.RecordObjects(Selection.GetTransforms().ToArray());
                UndoManager.RecordObjects(UnityEditor.Selection.transforms);

                foreach (Transform t in UnityEditor.Selection.transforms)
                {
                    TransformDouble transformDouble = t.GetComponent<TransformDouble>();
                    if (transformDouble != Disposable.NULL)
                        transformDouble.position += dif;
                    else
                        t.transform.position += (Vector3)dif;
                }
            }
            else
                Debug.LogWarning("MoveToView Failed: Object too far.");
        }

        private static bool PatchedPreFrame(SceneView __instance, Bounds bounds, bool instant)
        {
            SceneViewDouble sceneViewDouble = GetSceneViewDouble(__instance);
            if (sceneViewDouble != Disposable.NULL)
            {
                sceneViewDouble.Frame(bounds, instant);

                return false;
            }
            return true;
        }

        private static bool PatchedPreFrameSelected(SceneView __instance, bool lockView, bool instant)
        {
            SceneViewDouble sceneViewDouble = GetSceneViewDouble(__instance);
            if (sceneViewDouble != Disposable.NULL)
                return sceneViewDouble.FrameSelected(lockView, instant);
            return true;
        }

        private static bool PatchedPreAlignViewToObject(SceneView __instance, Transform t)
        {
            SceneViewDouble sceneViewDouble = GetSceneViewDouble(__instance);
            if (sceneViewDouble != Disposable.NULL)
            {
                TransformDouble transformDouble = t.GetComponent<TransformDouble>();
                if (transformDouble != Disposable.NULL)
                    sceneViewDouble.AlignViewToObject(transformDouble);
                else
                    sceneViewDouble.AlignViewToObject(t);

                return false;
            }
            return true;
        }

        private static bool PatchedPreAlignWithView(SceneView __instance)
        {
            SceneViewDouble sceneViewDouble = GetSceneViewDouble(__instance);
            if (sceneViewDouble != Disposable.NULL)
            {
                sceneViewDouble.AlignWithView();

                return false;
            }
            return true;
        }

        private static bool PatchedPreMoveToView(SceneView __instance)
        {
            RenderingManager renderingManager = RenderingManager.Instance(false);
            if (renderingManager != Disposable.NULL && renderingManager.originShifting)
            {
                SceneViewDouble sceneViewDouble = GetSceneViewDouble(__instance);
                if (sceneViewDouble != Disposable.NULL)
                {
                    sceneViewDouble.MoveToView();

                    return false;
                }
            }
            return true;
        }

        private static bool PatchedPreMoveToViewTarget(SceneView __instance, Transform target)
        {
            RenderingManager renderingManager = RenderingManager.Instance(false);
            if (renderingManager != Disposable.NULL && renderingManager.originShifting)
            {
                SceneViewDouble sceneViewDouble = GetSceneViewDouble(__instance);
                if (sceneViewDouble != Disposable.NULL)
                {
                    GameObject gameObject = target.gameObject;

                    TransformDouble targetTransformDouble = gameObject.GetComponent<TransformDouble>();

                    if (targetTransformDouble != Disposable.NULL)
                    {
                        sceneViewDouble.MoveToView(targetTransformDouble);
                        return false;
                    }
                }
            }

            return true;
        }

        private static void PatchedPostGetHandleSize(Vector3 position, ref float __result)
        {
            CameraManager cameraManager = CameraManager.Instance(false);
            if (cameraManager != Disposable.NULL)
            {
                Camera camera = cameraManager.GetCameraFromUnityCamera(UnityEngine.Camera.current);
                if (camera != Disposable.NULL)
                    __result = (float)(k_KHandleSize * camera.GetDistanceScaleForCamera(position) * EditorGUIUtility.pixelsPerPoint);
            }
        }

        private static void PatchedPostSetupCamera(SceneView __instance)
        {
            SceneViewDouble sceneViewDouble = GetSceneViewDouble(__instance);
            if (sceneViewDouble != Disposable.NULL && sceneViewDouble == _currentSceneViewDouble)
                sceneViewDouble.PostSetupCamera(__instance);
        }

        private static void PatchedPreHandleSelectionAndOnSceneGUI(SceneView __instance)
        {
            SceneViewDouble sceneViewDouble = GetSceneViewDouble(__instance);
            if (sceneViewDouble != Disposable.NULL && sceneViewDouble == _currentSceneViewDouble)
                sceneViewDouble.PreHandleSelectionAndOnSceneGUI();
        }

        //TransformDoubleEditor.OnSceneGUI()

        private static void PatchedPostHandleSelectionAndOnSceneGUI(SceneView __instance)
        {
            SceneViewDouble sceneViewDouble = GetSceneViewDouble(__instance);
            if (sceneViewDouble != Disposable.NULL && sceneViewDouble == _currentSceneViewDouble)
                sceneViewDouble.PostHandleSelectionAndOnSceneGUI();
        }

        private static void PatchedPreDefaultHandles(SceneView __instance)
        {
            SceneViewDouble sceneViewDouble = GetSceneViewDouble(__instance);
            if (sceneViewDouble != Disposable.NULL && sceneViewDouble == _currentSceneViewDouble)
                sceneViewDouble.PreDefaultHandles(__instance);
        }

        private static void PatchedPostDefaultHandles(SceneView __instance)
        {
            SceneViewDouble sceneViewDouble = GetSceneViewDouble(__instance);
            if (sceneViewDouble != Disposable.NULL && sceneViewDouble == _currentSceneViewDouble)
                sceneViewDouble.PostDefaultHandles();
        }

        private void onGUIStarted(SceneView sceneView)
        {
            if (sceneView.GetInstanceID() != sceneViewInstanceId)
                return;

            if (renderingManager.originShifting)
            {
                Type sceneViewType = typeof(SceneView);
                BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic;
                PropertyInfo sceneViewGridsPropertyInfo = sceneViewType.GetProperty("sceneViewGrids", bindingFlags);
                PropertyInfo gridOpacityPropertyInfo = sceneViewType.Assembly.GetType("UnityEditor.SceneViewGrid").GetProperty("gridOpacity", bindingFlags);
                gridOpacityPropertyInfo.SetValue(sceneViewGridsPropertyInfo.GetValue(sceneView, null), 0.0f);
            }

            _currentSceneViewDouble = this;

            Event evt = Event.current;
            if (evt != null)
            {
                if (evt.type == EventType.Repaint)
                    handleCount = 0;
            }
            
            bool isNotCenterOfOrbitController = false;
            
            if (alignViewToGeoAstroObject != Disposable.NULL && alignViewToGeoAstroObject.controller != Disposable.NULL && alignViewToGeoAstroObject.controller is OrbitController)
            {
                StarSystem starSystem = (alignViewToGeoAstroObject.controller as OrbitController).GetStarSystem();
                if (starSystem != Disposable.NULL && starSystem.orbitAroundAstroObject != Disposable.NULL)
                {
                    GeoAstroObject orbitAroundGeoAstroObject = starSystem.orbitAroundAstroObject as GeoAstroObject;
                    if (orbitAroundGeoAstroObject != Disposable.NULL && orbitAroundGeoAstroObject.IsFlat() && orbitAroundGeoAstroObject != alignViewToGeoAstroObject)
                        isNotCenterOfOrbitController = true;
                }
            }

            if (isNotCenterOfOrbitController)
                alignViewToGeoAstroObject = null;
        }

        private void PostSetupCamera(SceneView sceneView)
        {
            SceneCamera sceneCamera = GetSceneCamera(sceneView);

            UnityEngine.Camera unitySceneCamera = sceneView.camera;

            if (sceneCamera != Disposable.NULL)
            {
                sceneCamera.transform.RevertUnityLocalPosition();
                sceneCamera.transform.RevertUnityLocalRotation();

                CameraManager cameraManager = this.cameraManager;
                sceneCamera.clearFlags = cameraManager.clearFlags;

                sceneCamera.skyboxMaterialPath = cameraManager.skyboxMaterialPath;
                sceneCamera.backgroundColor = cameraManager.backgroundColor;
                sceneCamera.environmentTextureSize = cameraManager.environmentTextureSize;

                sceneCamera.orthographic = unitySceneCamera.orthographic;
                sceneCamera.orthographicSize = unitySceneCamera.orthographicSize;
                sceneCamera.fieldOfView = unitySceneCamera.fieldOfView;
                sceneCamera.nearClipPlane = unitySceneCamera.nearClipPlane;
                sceneCamera.farClipPlane = unitySceneCamera.farClipPlane;

                sceneCamera.depthTextureMode = unitySceneCamera.depthTextureMode;
                sceneCamera.cullingMask = unitySceneCamera.cullingMask;
                sceneCamera.useOcclusionCulling = unitySceneCamera.useOcclusionCulling;

                sceneCamera.allowHDR = unitySceneCamera.allowHDR;
            }
        }

        //SceneManager.BeginCameraRendering()

        private float _lastNearClipPlane;
        private float _lastFarClipPlane;
        private void PreHandleSelectionAndOnSceneGUI()
        {
            if (renderingManager.originShifting)
            {
                if (Camera.current != null)
                {
                    //Ensure the Handles are visible even from very far away
                    _lastNearClipPlane = Camera.current.nearClipPlane;
                    _lastFarClipPlane = Camera.current.farClipPlane;
                    Camera.current.nearClipPlane = 0.001f;
                    Camera.current.farClipPlane = 10000000000.0f;
                }
            }
        }

        //TransformDoubleEditor.OnSceneGUI()

        private void PostHandleSelectionAndOnSceneGUI()
        {
           if (renderingManager.originShifting)
            {
                if (Camera.current != null)
                {
                    Camera.current.nearClipPlane = _lastNearClipPlane;
                    Camera.current.farClipPlane = _lastFarClipPlane;
                }
            }
        }

        private Tool _lastToolCurrent;
        private static List<TransformDouble> _lastActiveSceneCameraTransforms;
        private void PreDefaultHandles(SceneView sceneView)
        {
            sceneManager.UpdateAstroObjects(camera);

            UpdatePivotRotationDistance();

            sceneManager.UpdateAstroObjects(camera);

            if (renderingManager.originShifting)
            {
                SceneViewDouble lastActiveSceneViewDouble = SceneViewDouble.lastActiveSceneViewDouble;

                if (lastActiveSceneViewDouble != Disposable.NULL)
                {
                    Vector3Double origin = lastActiveSceneViewDouble.camera.GetOrigin();
                    if (UnityEditor.Selection.transforms.Length > 0)
                    {
                        Selection.ApplyOriginShifting(origin);

                        _lastActiveSceneCameraTransforms ??= new List<TransformDouble>();
                        _lastActiveSceneCameraTransforms.Clear();
                        if (lastActiveSceneViewDouble != Disposable.NULL)
                        {
                            _lastActiveSceneCameraTransforms.Add(lastActiveSceneViewDouble.camera.transform);
                            _lastActiveSceneCameraTransforms.Add(lastActiveSceneViewDouble.camera.sceneCameraController.targetTransform);
                        }

                        TransformDouble.ApplyOriginShifting(_lastActiveSceneCameraTransforms, origin);
                    }

                    //The Rect Tool throws "Screen position out of view frustum" errors when the selected object is too far away so we disable it beyond a certain distance
                    _lastToolCurrent = Tools.current;
                    if (Tools.current == Tool.Rect && !Tools.GetHandlePosition(out Vector3 _))
                        Tools.current = Tool.None;

                    Tools.hidden = sceneView.camera != lastActiveSceneViewDouble.camera.unityCamera;

                    bool showMockHandles = false;
                    if (UnityEditor.Selection.transforms.Length == Selection.GetTransformDoubleSelectionCount())
                        showMockHandles = Tools.hidden;
                    this.showMockHandles = showMockHandles;
                }
            }
        }

        private bool InitSceneViewDoubleComponents()
        {
            bool initialized = false;

            if (_sceneViewDoubleComponents == null)
            {
                UpdateLastSceneViewComponents();
                _sceneViewDoubleComponents = new TargetControllerComponents();
            }

            if (alignViewToGeoAstroObject != Disposable.NULL)
                SetPivot(alignViewToGeoAstroObject.gameObject.GetSafeComponent<TransformDouble>().TransformPoint(alignViewToGeoAstroObject.GetLocalPointFromGeoCoordinate(pivotGeoCoordinate)));
      
            _sceneViewDoubleComponents.SetComponents(pivot, rotation, cameraDistance);
           
            return initialized;
        }

        public void UpdatePivotRotationDistance()
        {
            UpdateSceneViewPivot(sceneView);
            UpdateSceneViewRotation(sceneView);
            UpdateSceneViewSize(sceneView);

            InitSceneViewDoubleComponents();

            _sceneViewDoubleComponentsDelta ??= new SceneViewDoubleComponentsDelta();
            _sceneViewDoubleComponentsDelta.targetParentGeoAstroObject = alignViewToGeoAstroObject;

            SceneCamera sceneCamera = GetSceneCamera(sceneView);
            if (sceneCamera != Disposable.NULL)
            {
                if (GetSceneViewComponentsUserDelta(sceneView, ref _sceneViewDoubleComponentsDelta, sceneCamera.gameObject.transform))
                {
                    //Stop Animations
                    if (_sceneViewDoubleComponentsDelta.TargetPositionChanged())
                        StopPivotAnimation();
                    if (_sceneViewDoubleComponentsDelta.RotationChanged())
                        StopRotationAnimation();
                    if (_sceneViewDoubleComponentsDelta.CameraDistanceChanged())
                        StopSizeAnimation();

                    AddUserDeltasToSceneViewDoubleComponents(sceneView, sceneCamera, _sceneViewDoubleComponents, _sceneViewDoubleComponentsDelta);
                }
            }

            if (GetSceneViewComponentsEditorDelta(ref _sceneViewDoubleComponentsDelta))
                AddEditorDeltasToSceneViewDoubleComponents(_sceneViewDoubleComponents, _sceneViewDoubleComponentsDelta);
       
            if (sceneView.in2DMode)
                _sceneViewDoubleComponents.rotation = QuaternionDouble.identity;

            if (_sceneViewDoubleComponents.targetPosition.magnitude > MAX_TARGET_POSITION_DISTANCE)
                _sceneViewDoubleComponents.targetPosition = _sceneViewDoubleComponents.targetPosition.normalized * MAX_TARGET_POSITION_DISTANCE;
            
            if (sceneCamera != Disposable.NULL)
            {
                SceneCameraTarget sceneCameraTarget = UpdateSceneCamera(sceneCamera, _sceneViewDoubleComponents);
                if (sceneCameraTarget != Disposable.NULL)
                {
                    if (alignViewToGeoAstroObject != Disposable.NULL)
                    {
                        //Always keep aligned to the target
                        if (!sceneView.in2DMode && !_sceneViewPivot.isAnimating && !_sceneViewRotation.isAnimating)
                        {
                            _sceneViewDoubleComponents.rotation = QuaternionDouble.LookRotation(_sceneViewDoubleComponents.rotation * Vector3Double.forward, sceneCameraTarget.transform.rotation * Vector3Double.up);
                            sceneCamera.sceneCameraController.SetTargetPositionRotationDistance(_sceneViewDoubleComponents.targetPosition, _sceneViewDoubleComponents.rotation, _sceneViewDoubleComponents.cameraDistance);
                        }
                    }

                    _sceneViewDoubleComponents.SetComponents(sceneCameraTarget.transform.position, sceneCamera.transform.rotation, Vector3Double.Distance(sceneCameraTarget.transform.position, sceneCamera.transform.position));

                    Vector3Double sceneCameraPosition = TargetControllerBase.GetTargetPosition(_sceneViewDoubleComponents.targetPosition, _sceneViewDoubleComponents.rotation, _sceneViewDoubleComponents.cameraDistance);
                    Vector3Double sceneCameraTargetPosition = _sceneViewDoubleComponents.targetPosition;
                    sceneView.pivot = TransformDouble.SubtractPointFromCameraOrigin(sceneCameraTargetPosition, lastActiveSceneViewDouble.camera);
                    if (!sceneView.in2DMode)
                        sceneView.rotation = _sceneViewDoubleComponents.rotation;
                    sceneView.size = (float)GetSizeFromCameraDistance((float)Vector3Double.Distance(sceneCameraTargetPosition, sceneCameraPosition));
                }
            }

            UpdateLastSceneViewComponents();

            SetPivot(_sceneViewDoubleComponents.targetPosition);
            SetRotation(_sceneViewDoubleComponents.rotation);
            SetCameraDistance(_sceneViewDoubleComponents.cameraDistance);
        }

        private SceneCameraTarget UpdateSceneCamera(SceneCamera sceneCamera, TargetControllerComponents sceneViewDoubleComponents = null)
        {
            SceneCameraTarget sceneCameraTarget = null;

            SceneCameraController sceneCameraController = sceneCamera.sceneCameraController;

            if (sceneCameraController != Disposable.NULL)
            {
                sceneCameraTarget = sceneCameraController.target as SceneCameraTarget;
                if (sceneCameraTarget != Disposable.NULL)
                {
                    sceneCameraTarget.SetTargetAutoSnapToGround(autoSnapViewToTerrain);
                    sceneCameraTarget.SetTargetParent(alignViewToGeoAstroObject != Disposable.NULL ? alignViewToGeoAstroObject.gameObject.GetSafeComponent<TransformDouble>() : null);
                }

                if (sceneViewDoubleComponents != null)
                    sceneCameraController.SetTargetPositionRotationDistance(sceneViewDoubleComponents.targetPosition, sceneViewDoubleComponents.rotation, sceneViewDoubleComponents.cameraDistance);
            }

            return sceneCameraTarget;
        }

        private void PostDefaultHandles()
        {
            sceneManager.HierarchicalDetectChanges();

            if (renderingManager.originShifting)
                Tools.current = _lastToolCurrent;
        }

        private void onGUIEnded(SceneView sceneView)
        {
            if (sceneView.GetInstanceID() != sceneViewInstanceId)
                return;

            _currentSceneViewDouble = null;
        }

        private MethodInfo _sceneViewMotionDoViewToolMethodInfo;
        private bool GetSceneViewComponentsUserDelta(SceneView sceneView, ref SceneViewDoubleComponentsDelta sceneViewComponentDeltas, Transform transform)
        {
            Event evt = Event.current;
            if (evt != null && evt.type != EventType.MouseDown && evt.type != EventType.MouseUp)
            {
                Vector3 unityTransformPosition = transform.position;
                Quaternion unityTransformRotation = transform.rotation;

                Vector3 sceneViewPivot = sceneView.pivot;
                Quaternion sceneViewRotation = sceneView.rotation;

                sceneView.pivot = Vector3.zero;
                if (!sceneView.in2DMode && sceneViewComponentDeltas.targetParentGeoAstroObject != Disposable.NULL)
                    sceneView.rotation = Quaternion.identity;

                transform.position = sceneView.pivot;
                transform.rotation = sceneView.rotation;

                float sceneViewCameraDistance = (float)GetCameraDistanceFromSize(sceneView.size);
                sceneViewComponentDeltas.SetComponents(sceneView.pivot, TargetControllerBase.GetTargetPosition(sceneView.pivot, sceneView.rotation, -sceneViewCameraDistance), sceneView.rotation, sceneViewCameraDistance);
                SceneViewMotion.DoViewTool(sceneView);
                sceneViewCameraDistance = (float)GetCameraDistanceFromSize(sceneView.size);
                sceneViewComponentDeltas.CalculateComponentDeltas(sceneView.pivot, TargetControllerBase.GetTargetPosition(sceneView.pivot, sceneView.rotation, -sceneViewCameraDistance), sceneView.rotation, sceneViewCameraDistance);

                transform.position = unityTransformPosition;
                transform.rotation = unityTransformRotation;

                sceneView.pivot = sceneViewPivot;
                if (!sceneView.in2DMode)
                    sceneView.rotation = sceneViewRotation;
            }
            else
                sceneViewComponentDeltas.Reset();

            return sceneViewComponentDeltas.Changed();
        }

        private void AddUserDeltasToSceneViewDoubleComponents(SceneView sceneView, SceneCamera sceneCamera, TargetControllerComponents sceneViewDoubleComponents, SceneViewDoubleComponentsDelta sceneViewDoubleComponentsDeltas)
        {
            Vector3Double targetPosition = sceneViewDoubleComponents.targetPosition;

            Vector3Double cameraPosition = TargetControllerBase.GetTargetPosition(targetPosition, sceneViewDoubleComponents.rotation, -sceneViewDoubleComponents.cameraDistance);
            QuaternionDouble cameraRotation = sceneViewDoubleComponents.rotation;

            Vector3Double cameraPositionDelta = sceneViewDoubleComponentsDeltas.cameraPosition;
            double cameraDistanceDelta = sceneViewDoubleComponentsDeltas.cameraDistance;

            bool pan = sceneViewDoubleComponentsDeltas.TargetPositionChanged() && sceneViewDoubleComponentsDeltas.CameraPositionChanged();
            bool fps = sceneViewDoubleComponentsDeltas.TargetPositionChanged() && !sceneViewDoubleComponentsDeltas.CameraPositionChanged() && sceneViewDoubleComponentsDeltas.RotationChanged();
            bool orbit = !sceneViewDoubleComponentsDeltas.TargetPositionChanged() && sceneViewDoubleComponentsDeltas.CameraPositionChanged() && sceneViewDoubleComponentsDeltas.RotationChanged();
            bool scroll = sceneViewDoubleComponentsDeltas.CameraDistanceChanged();

            if (scroll)
                sceneViewDoubleComponents.cameraDistance += cameraDistanceDelta;

            if (pan || fps)
            {
                QuaternionDouble fromCameraSurfaceRotation = GetUpVectorFromPoint(sceneCamera, cameraPosition);
                QuaternionDouble toCameraSurfaceRotation = fromCameraSurfaceRotation;
                
                if (pan)
                {
                    cameraPosition += (sceneViewDoubleComponentsDeltas.targetParentGeoAstroObject != Disposable.NULL ? cameraRotation : QuaternionDouble.identity) * cameraPositionDelta;
                    toCameraSurfaceRotation = GetUpVectorFromPoint(sceneCamera, cameraPosition);
                }

                if (!sceneView.in2DMode)
                    sceneViewDoubleComponents.rotation = AddDeltaToRotation(cameraRotation, sceneViewDoubleComponentsDeltas, fromCameraSurfaceRotation, toCameraSurfaceRotation);

                sceneViewDoubleComponents.targetPosition = TargetControllerBase.GetTargetPosition(cameraPosition, sceneViewDoubleComponents.rotation, sceneViewDoubleComponents.cameraDistance);
            }
            else if (orbit)
            {
                QuaternionDouble targetSurfaceRotation = GetUpVectorFromPoint(sceneCamera, targetPosition);
                sceneViewDoubleComponents.rotation = AddDeltaToRotation(cameraRotation, sceneViewDoubleComponentsDeltas, targetSurfaceRotation, targetSurfaceRotation);
            }
        }

        private QuaternionDouble GetUpVectorFromPoint(SceneCamera sceneCamera, Vector3Double position)
        {
            GeoAstroObject targetParentGeoAstroObject = sceneCamera != Disposable.NULL ? sceneCamera.sceneCameraController.GetTargetParentGeoAstroObject() : null;
            if (targetParentGeoAstroObject != Disposable.NULL && targetParentGeoAstroObject.IsValidSphericalRatio())
                return targetParentGeoAstroObject.GetUpVectorFromPoint(position);
            else
                return QuaternionDouble.identity;
        }

        private QuaternionDouble AddDeltaToRotation(QuaternionDouble rotation, SceneViewDoubleComponentsDelta sceneViewDoubleComponentsDeltas, QuaternionDouble fromCameraSurfaceRotation, QuaternionDouble toCameraSurfaceRotation)
        {
            QuaternionDouble rotationDelta = sceneViewDoubleComponentsDeltas.rotation;

            QuaternionDouble cameraSurfaceRelativeRotation = QuaternionDouble.Inverse(fromCameraSurfaceRotation) * rotation;

            if (sceneViewDoubleComponentsDeltas.targetParentGeoAstroObject != Disposable.NULL)
            {
                Vector3Double cameraSurfaceRelativeRotationEuler = cameraSurfaceRelativeRotation.eulerAngles;
                cameraSurfaceRelativeRotationEuler += rotationDelta.eulerAngles;
                cameraSurfaceRelativeRotationEuler.z = 0.0d;
                cameraSurfaceRelativeRotation = QuaternionDouble.Euler(ValidateRelativeRotation(cameraSurfaceRelativeRotationEuler));
            }
            else
                cameraSurfaceRelativeRotation = (cameraSurfaceRelativeRotation * rotationDelta).normalized;

            return toCameraSurfaceRotation * cameraSurfaceRelativeRotation;
        }

        private bool GetSceneViewComponentsEditorDelta(ref SceneViewDoubleComponentsDelta sceneViewDoubleComponentsDelta)
        {
            sceneViewDoubleComponentsDelta.SetComponents(GetLastSceneViewPivot(), Vector3Double.zero, GetLastSceneViewRotation(), GetLastSceneViewCameraDistance());
            sceneViewDoubleComponentsDelta.CalculateComponentDeltas(GetSceneViewPivotValue(), Vector3Double.zero, GetSceneViewRotationValue(), (float)GetCameraDistanceFromSize(GetSceneViewSizeValue()));

            return sceneViewDoubleComponentsDelta.Changed();
        }

        private void AddEditorDeltasToSceneViewDoubleComponents(TargetControllerComponents sceneViewDoubleComponents, SceneViewDoubleComponentsDelta sceneViewDoubleComponentsDeltas)
        {
            sceneViewDoubleComponents.targetPosition += sceneViewDoubleComponentsDeltas.targetPosition;
            sceneViewDoubleComponents.rotation = (sceneViewDoubleComponents.rotation * sceneViewDoubleComponentsDeltas.rotation).normalized;
            sceneViewDoubleComponents.cameraDistance += sceneViewDoubleComponentsDeltas.cameraDistance;
        }

        private void UpdateLastSceneViewComponents()
        {
            UpdateSceneViewPivot(sceneView);
            UpdateSceneViewRotation(sceneView);
            UpdateSceneViewSize(sceneView);

            GetSceneViewPivot(out Vector3 sceneViewPivotValue, out Vector3 _, sceneView);
            _lastSceneViewPivot = sceneViewPivotValue;
            _lastAnimSceneViewPivot = _sceneViewPivot.value;

            AnimQuaternion sceneViewRotation = sceneView.GetType().GetField("m_Rotation", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(sceneView) as AnimQuaternion;
            _lastSceneViewRotation = sceneViewRotation.value;
            _lastAnimSceneViewRotation = _sceneViewRotation.value;

            AnimFloat sceneViewSize = sceneView.GetType().GetField("m_Size", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(sceneView) as AnimFloat;
            _lastSceneViewCameraDistance = (float)GetCameraDistanceFromSize(sceneViewSize.value);
            _lastAnimSceneViewCameraDistance = (float)GetCameraDistanceFromSize(_sceneViewSize.value);
        }

        private Vector3Double ValidateRelativeRotation(Vector3Double eulerRotation)
        {
            //Prevent singularity
            double x = eulerRotation.x % 360.0d;
            if (x > 180.0d)
                x -= 360.0d;
            if (x > 89.0d)
                eulerRotation.x = 89.0d;
            if (x < -89.0d)
                eulerRotation.x = -89.0d;

            return eulerRotation;
        }

        protected override bool OnDisposed(DisposeManager.DisposeContext disposeContext, bool pooled = false)
        {
            if (base.OnDisposed(disposeContext, pooled))
            {
                SceneManager sceneManager = SceneManager.Instance(false);
                if (sceneManager != Disposable.NULL)
                    sceneManager.SceneViewDoubleDisposed(this);

                foreach (SceneView sceneView in SceneView.sceneViews)
                {
                    if (sceneView.GetInstanceID() == sceneViewInstanceId)
                    {
                        sceneView.pivot = pivot;
                        if (!sceneView.in2DMode)
                            sceneView.rotation = rotation;
                        sceneView.size = (float)size;

                        break;
                    }
                }

                return true;
            }
            return false;
        }

        private class TargetControllerComponents
        {
            private Vector3Double _targetPosition;
            private QuaternionDouble _rotation;
            private double _cameraDistance;

            public TargetControllerComponents()
            {
            }

            public TargetControllerComponents(Vector3Double targetPosition, QuaternionDouble rotation, double cameraDistance)
            {
                SetComponents(targetPosition, rotation, cameraDistance);
            }

            public void SetComponents(Vector3Double targetPosition, QuaternionDouble rotation, double cameraDistance)
            {
                this.targetPosition = targetPosition;
                this.rotation = rotation;
                this.cameraDistance = cameraDistance;
            }

            public Vector3Double targetPosition
            {
                get { return _targetPosition; }
                set
                {
                    if (_targetPosition == value)
                        return;
                    _targetPosition = value;
                }
            }

            public QuaternionDouble rotation
            {
                get { return _rotation; }
                set
                {
                    if (_rotation == value)
                        return;
                    _rotation = value;
                }
            }

            public double cameraDistance
            {
                get { return _cameraDistance; }
                set
                {
                    if (_cameraDistance == value)
                        return;

                    _cameraDistance = value;
                }
            }

            public void Reset()
            {
                SetComponents(Vector3Double.zero, QuaternionDouble.identity, 0.0d);
            }

            public override string ToString()
            {
                return targetPosition + ", " + rotation + ", " + cameraDistance;
            }
        }

        private class SceneViewDoubleComponentsDelta
        {
            public GeoAstroObject targetParentGeoAstroObject;

            public Vector3 targetPosition;
            public Vector3 cameraPosition;
            public Quaternion rotation;
            public float cameraDistance;

            public SceneViewDoubleComponentsDelta()
            {
            }

            public SceneViewDoubleComponentsDelta(Vector3 targetPosition, Vector3 cameraPosition, Quaternion rotation, float cameraDistance)
            {
                SetComponents(targetPosition, cameraPosition, rotation, cameraDistance);
            }

            public void SetComponents(Vector3 targetPosition, Vector3 cameraPosition, Quaternion rotation, float cameraDistance)
            {
                this.targetPosition = targetPosition;
                this.cameraPosition = cameraPosition;
                this.rotation = rotation;
                this.cameraDistance = cameraDistance;
            }

            public void Reset()
            {
                SetComponents(Vector3.zero, Vector3.zero, Quaternion.identity, 0.0f);
            }

            public void CalculateComponentDeltas(Vector3 targetPosition, Vector3 cameraPosition, Quaternion rotation, float cameraDistance)
            {
                this.targetPosition = targetPosition - this.targetPosition;
                this.cameraPosition = cameraPosition - this.cameraPosition;
                this.rotation = (Quaternion.Inverse(this.rotation) * rotation).normalized;
                this.cameraDistance = cameraDistance - this.cameraDistance;
            }

            public bool TargetPositionChanged()
            {
                return targetPosition != Vector3.zero;
            }

            public bool CameraPositionChanged()
            {
                return cameraPosition != Vector3.zero;
            }

            public bool RotationChanged()
            {
                return rotation != Quaternion.identity;
            }

            public bool CameraDistanceChanged()
            {
                return cameraDistance != 0.0f;
            }

            public bool Changed()
            {
                return TargetPositionChanged() || CameraPositionChanged() || RotationChanged() || CameraDistanceChanged();
            }

            public override string ToString()
            {
                return "TargetPosition: "+targetPosition + ", CameraPosition:" + cameraPosition + ", Rotation:" + rotation + ", CameraDistance:" + cameraDistance;
            }
        }
    }
}
#endif
