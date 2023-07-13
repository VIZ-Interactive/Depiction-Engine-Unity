// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using UnityEngine.Rendering;
using UnityEngine;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Reflection;
using UnityEngine.UIElements;
using System.Text.RegularExpressions;
using System.Linq;
using UnityEngine.SceneManagement;

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Design", "IDE1006",
    Justification = "Unity uses camelCase instead of PascalCase for public properties.",
    Scope = "module")]

namespace DepictionEngine
{
    /// <summary>
    /// Singleton managing the scene.
    /// </summary>
    [DefaultExecutionOrder(-3)]
    [AddComponentMenu(SceneManager.NAMESPACE + "/Manager/" + nameof(SceneManager))]
    [DisallowMultipleComponent]
    public class SceneManager : ManagerBase, IHasChildren
    {
        /// <summary>
        /// The different steps of an update in order of execution. <br/><br/>
        /// <b><see cref="None"/>:</b> <br/>
        /// Update code is not currently being executed. <br/><br/>
        /// <b><see cref="LateInitialize"/>:</b> <br/>
        /// All other object have been created and initialized at this point, even during a duplicate operation involving multiple objects. <br/><br/>
        /// <b><see cref="PostLateInitialize"/>:</b> <br/>
        /// All other object have been created and initialized at this point, even during a duplicate operation involving multiple objects. <br/><br/>
        /// <b><see cref="Update"/>:</b> <br/>
        /// The hierarchy is traversed and values are updated. <br/><br/>
        /// <b><see cref="PastingComponentValues"/>:</b> <br/>
        /// Pasting component values from the inspector(Editor only). <br/><br/>
        /// <b><see cref="LateUpdate"/>:</b> <br/>
        /// A Late Update. <br/><br/>
        /// <b><see cref="DelayedOnDestroy"/>:</b> <br/>
        /// Objects that were waiting to be destroyed are destroyed. <br/><br/>
        /// </summary> 
        public enum ExecutionState
        {
            None,
            LateInitialize,
            PostLateInitialize,
            Update,
            PastingComponentValues,
            LateUpdate,
            DelayedOnDestroy
        };

        public const string NAMESPACE = "Depiction Engine";
        public const string SCENE_MANAGER_NAME = "Managers (Required)";

        [BeginFoldout("Editor")]
        [SerializeField, Tooltip("When enabled some hidden properties and objects will be exposed to help with debugging.")]
        private bool _debug;
        [SerializeField, Tooltip("When enabled CameraGrid2DLoader's will display their loading/loaded count in the inspector next to the GameObject name.")]
        private bool _showLoadCountInInspector;
        [SerializeField, Tooltip("When enabled an approximate Editor framerate will be shown in the scene view windows.")]
        private bool _showFrameRateInSceneViews;
        [SerializeField, Tooltip("When enabled some log entries will be disable such as 'Child GameObject ... became dangling during undo'."), EndFoldout]
        private bool _logConsoleFiltering;

        [BeginFoldout("Performance")]
        [SerializeField, Tooltip("Should the player be running when the application is in the background?")]
        private bool _runInBackground;
        [SerializeField, Tooltip("When enabled some "+nameof(Processor)+"'s will perform their work on separate threads."), EndFoldout]
        private bool _enableMultithreading;

#if UNITY_EDITOR
        [BeginFoldout("Asset Bundle")]
        [SerializeField, Tooltip("The directory where the AssetBundle should be built.")]
        private string _buildOutputPath;
        [SerializeField, Tooltip("AssetBundle building options.")]
        private UnityEditor.BuildAssetBundleOptions _buildOptions;
        [SerializeField, Tooltip("Target build platform.")]
        private UnityEditor.BuildTarget _buildTarget;
        [SerializeField, Button(nameof(BuildAssetBundlesBtn)), ConditionalShow(nameof(IsNotFallbackValues)), Tooltip("Build all AssetBundle.")]
        private bool _buildAssetBundles;
        [Space()]
        [SerializeField, Button(nameof(UnloadAllAssetBundlesBtn)), ConditionalShow(nameof(IsNotFallbackValues)), Tooltip("Unloads all currently loaded AssetBundles."), EndFoldout]
        private bool _unloadAllBuildAssetBundles;

        private void BuildAssetBundlesBtn()
        {
            UnityEditor.BuildPipeline.BuildAssetBundles(buildOutputPath, buildOptions, buildTarget);
        }

        private void UnloadAllAssetBundlesBtn()
        {
            UnityEngine.AssetBundle.UnloadAllAssetBundles(true);
        }
#endif

        private TweenManager _tweenManager;
        private PoolManager _poolManager;
        private InputManager _inputManager;
        private CameraManager _cameraManager;
        private RenderingManager _renderingManager;
        private DatasourceManager _datasourceManager;
        private JsonInterface _jsonInterface;
        private InstanceManager _instanceManager;

#if UNITY_EDITOR
        private List<TransformBase> _sceneCameraTransforms;
#endif
        private List<TransformBase> _sceneChildren;

        [NonSerialized]
        private bool _updated;

        private static ExecutionState _sceneExecutionState;

        private static bool _sceneClosing;

        /// <summary>
        /// Dispatched at the end of the <see cref="DepictionEngine.SceneManager.LateUpdate"/> just before the DelayedOnDestroy.
        /// </summary>
        public static Action LateUpdateEvent;
        /// <summary>
        /// Dispatched at the end of the <see cref="DepictionEngine.SceneManager.LateUpdate"/>.
        /// </summary>
        public static Action DelayedOnDestroyEvent;

#if UNITY_EDITOR
        /// <summary>
        /// Dispatched when the parent Scene of the <see cref="DepictionEngine.SceneManager"/> gameObject is closing.
        /// </summary>
        public static Action SceneClosingEvent;
        /// <summary>
        /// Dispatched when the parent Scene of the <see cref="DepictionEngine.SceneManager"/> gameObject as closed.
        /// </summary>
        public static Action SceneClosedEvent;
        /// <summary>
        /// Dispatched at the same time as the <see cref="UnityEditor.AssemblyReloadEvents.beforeAssemblyReload"/>.
        /// </summary>
        public static Action BeforeAssemblyReloadEvent;
        /// <summary>
        /// Dispatched when a left mouse up event happens in the Scene or Inspector window. 
        /// </summary>
        public static Action LeftMouseUpInSceneOrInspectorEvent;
        /// <summary>
        /// Dispatched when the debug field is changed. 
        /// </summary>
        public static Action<bool> DebugChangedEvent;
#endif

        private static SceneManager _instance;
        /// <summary>
        /// Get a singleton of the manager.
        /// </summary>
        /// <param name="createIfMissing">If true a new instance will be created if one doesn't already exist. </param>
        /// <returns>An instance of the manager if the context allows.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SceneManager Instance(bool createIfMissing = true)
        {
            if (_instance == Disposable.NULL)
            {
                _instance = GetManagerComponent<SceneManager>(createIfMissing);
                if (_instance != Disposable.NULL)
                    _instance.transform.SetAsFirstSibling();
            }
            return _instance;
        }

        protected override bool InitializeLastFields()
        {
            if (base.InitializeLastFields())
            {
#if UNITY_EDITOR
                _lastDebug = debug;
                _lastRunInBackground = runInBackground;
#endif
                return true;
            }
            return false;
        }

        protected override void CreateAndInitializeDependencies(InitializationContext initializingContext)
        {
            base.CreateAndInitializeDependencies(initializingContext);

            ScriptableObjectDisposable[] scriptableObjectDisposables = FindObjectsByType<ScriptableObjectDisposable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (ScriptableObjectDisposable scriptableObjectDisposable in scriptableObjectDisposables)
                InstanceManager.Initialize(scriptableObjectDisposable);
        }

        protected override void InitializeFields(InitializationContext initializingContext)
        {
            base.InitializeFields(initializingContext);

            UnityEngine.Texture.allowThreadedTextureCreation = true;

#if UNITY_EDITOR
            //Experimental
            //UnityEditor.PlayerSettings.WebGL.threadsSupport = false;

            UnityEditor.PlayerSettings.gcIncremental = true;
            UnityEditor.PlayerSettings.allowUnsafeCode = true;

            InitSceneCameraTransforms();

            Editor.SceneViewDouble.InitSceneViewDoubles(ref _sceneViewDoubles);
#endif

            PoolManager.Instance();
            InputManager.Instance();
            DatasourceManager.Instance();
            CameraManager.Instance();
            RenderingManager.Instance();

            Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Ignore Render"), 0);

            UpdateRunInBackground();
        }

        protected override void InitializeSerializedFields(InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            InitValue(value => debug = value, false, initializingContext);
            InitValue(value => showLoadCountInInspector = value, true, initializingContext);
            InitValue(value => showFrameRateInSceneViews = value, false, initializingContext);
            InitValue(value => logConsoleFiltering = value, true, initializingContext);
            InitValue(value => runInBackground = value, true, initializingContext);
            InitValue(value => enableMultithreading = value, true, initializingContext);
#if UNITY_EDITOR
            InitValue(value => buildOutputPath = value, "Assets/Depiction Engine/Resources/AssetBundle", initializingContext);
            InitValue(value => buildOptions = value, UnityEditor.BuildAssetBundleOptions.None, initializingContext);
            InitValue(value => buildTarget = value, UnityEditor.BuildTarget.StandaloneWindows64, initializingContext);
#endif
        }

#if UNITY_EDITOR
        private void InitSceneCameraTransforms()
        {
            _sceneCameraTransforms ??= new List<TransformBase>();
        }

        public override void Initialized(InitializationContext initializingContext)
        {
            base.Initialized(initializingContext);

            instanceManager.IterateOverInstances<CameraGrid2DLoader>((cameraGrid2DLoader) => 
            {
                AddCameraGrid2DLoader(cameraGrid2DLoader);
                return true;
            });
        }

        public override void UpdateDependencies()
        {
            base.UpdateDependencies();

            Editor.SceneViewDouble.InitSceneViewDoubles(ref _sceneViewDoubles);
        }
#endif

        protected override bool UpdateAllDelegates()
        {
            if (base.UpdateAllDelegates())
            {
                RenderPipelineManager.beginCameraRendering -= BeginCameraRendering;
                RenderPipelineManager.endCameraRendering -= EndCameraRendering;
                if (!IsDisposing())
                {
                    RenderPipelineManager.beginCameraRendering += BeginCameraRendering;
                    RenderPipelineManager.endCameraRendering += EndCameraRendering;
                }

#if UNITY_EDITOR
                UnityEditor.EditorApplication.hierarchyWindowItemOnGUI -= HierarchyWindowItemOnGUIHandler;
                UnityEditor.SceneView.duringSceneGui -= CustomOnSceneGUI;
                UnityEditor.ClipboardUtility.copyingGameObjects -= CopyingGameObjectsHandler;
                UnityEditor.ClipboardUtility.pastedGameObjects -= PastedGameObjectsHandler;
                Application.logMessageReceived -= LogMessageReceivedHandler;
                UnityEditor.EditorApplication.pauseStateChanged -= EditorPauseStateChangedHandler;
                UnityEditor.EditorApplication.playModeStateChanged -= PlayModeStateChangedHandler;
                UnityEditor.SceneManagement.EditorSceneManager.sceneClosing -= SceneClosingHandler;
                UnityEditor.SceneManagement.EditorSceneManager.sceneSaving -= SceneSavingHandler;
                UnityEditor.SceneManagement.EditorSceneManager.sceneSaved -= SceneSavedHandler;
                UnityEditor.AssemblyReloadEvents.beforeAssemblyReload -= BeforeAssemblyReloadHandler;
                UnityEditor.Selection.selectionChanged -= SelectionChangedHandler;
                if (!IsDisposing())
                {
                    UnityEditor.EditorApplication.hierarchyWindowItemOnGUI += HierarchyWindowItemOnGUIHandler;
                    UnityEditor.SceneView.duringSceneGui += CustomOnSceneGUI;
                    UnityEditor.ClipboardUtility.copyingGameObjects += CopyingGameObjectsHandler;
                    UnityEditor.ClipboardUtility.pastedGameObjects += PastedGameObjectsHandler;
                    Application.logMessageReceived += LogMessageReceivedHandler;
                    UnityEditor.EditorApplication.pauseStateChanged += EditorPauseStateChangedHandler;
                    UnityEditor.EditorApplication.playModeStateChanged += PlayModeStateChangedHandler;
                    UnityEditor.SceneManagement.EditorSceneManager.sceneClosing += SceneClosingHandler;
                    UnityEditor.SceneManagement.EditorSceneManager.sceneSaving += SceneSavingHandler;
                    UnityEditor.SceneManagement.EditorSceneManager.sceneSaved += SceneSavedHandler;
                    UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += BeforeAssemblyReloadHandler;
                    UnityEditor.Selection.selectionChanged += SelectionChangedHandler;
                }

                Editor.UndoManager.UpdateAllDelegates(IsDisposing());

                UpdateUpdateDelegate(IsDisposing());

                UpdateHarmonyPatches();
#endif
                return true;
            }
            return false;
        }

#if UNITY_EDITOR
        protected override void InstanceAddedHandler(IProperty property)
        {
            base.InstanceAddedHandler(property);

            if (property is CameraGrid2DLoader cameraGrid2DLoader)
                AddCameraGrid2DLoader(cameraGrid2DLoader);
        }

        protected override void InstanceRemovedHandler(IProperty property)
        {
            base.InstanceRemovedHandler(property);

            if (property is CameraGrid2DLoader cameraGrid2DLoader)
            {
                if (_loaderGOs != null)
                    _loaderGOs.Remove(cameraGrid2DLoader.GetGameObjectInstanceID());
            }
        }

        private void AddCameraGrid2DLoader(CameraGrid2DLoader cameraGrid2DLoader)
        {
            _loaderGOs ??= new();
            _loaderGOs.TryAdd(cameraGrid2DLoader.gameObject.GetInstanceID(), cameraGrid2DLoader.gameObject);
        }

        [Serializable]
        private class GameObjectDictionary : SerializableDictionary<int, GameObject> { };
        private GameObjectDictionary _loaderGOs;
        private void HierarchyWindowItemOnGUIHandler(int instanceID, Rect selectionRect)
        {
            if (showLoadCountInInspector && _loaderGOs != null && _loaderGOs.TryGetValue(instanceID, out GameObject value) && value != null)
            {
                CameraGrid2DLoader cameraGrid2DLoader = value.GetComponent<CameraGrid2DLoader>();
                string label = "(Loading: " + cameraGrid2DLoader.loadingCount + "/" + (cameraGrid2DLoader.loadingCount + cameraGrid2DLoader.loadedCount) + ")";
                float width = label.Length * 6.0f + 1.0f;
                GUI.Label(new Rect(selectionRect.xMax - width, selectionRect.y, width, selectionRect.height), label);
            }
        }
        
        private void CustomOnSceneGUI(UnityEditor.SceneView sceneview)
        {
            if (showFrameRateInSceneViews)
            {
                Editor.SceneViewDouble sceneViewDouble = Editor.SceneViewDouble.GetSceneViewDouble(sceneview);
                if (sceneViewDouble != null)
                {
                    UnityEditor.Handles.BeginGUI();

                    GUILayout.BeginArea(new Rect(43, 2, 65, 50));

                    var rect = UnityEditor.EditorGUILayout.BeginVertical();
                    UnityEditor.EditorGUI.DrawRect(rect, UnityEditor.EditorGUIUtility.isProSkin ? new Color32(56, 56, 56, 200) : new Color32(194, 194, 194, 200));

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("FPS: " + sceneViewDouble.GetFrameRate().ToString("F1"));
                    GUILayout.EndHorizontal();

                    UnityEditor.EditorGUILayout.EndVertical();

                    GUILayout.EndArea();

                    UnityEditor.Handles.EndGUI();
                }
            }
        }

        private bool _lastDebug;
        private bool _lastRunInBackground;
        protected override bool UpdateUndoRedoSerializedFields()
        {
            if (base.UpdateUndoRedoSerializedFields())
            {
                SerializationUtility.PerformUndoRedoPropertyAssign((value) => { debug = value; }, ref _debug, ref _lastDebug);
                SerializationUtility.PerformUndoRedoPropertyAssign((value) => { runInBackground = value; }, ref _runInBackground, ref _lastRunInBackground);

                return true;
            }
            return false;
        }

        /// <summary>
        /// Dispatched at the same time as the <see cref="UnityEditor.EditorApplication.playModeStateChanged"/>.
        /// </summary>
        public static Action<UnityEditor.PlayModeStateChange> PlayModeStateChangedEvent;

        private static UnityEditor.PlayModeStateChange _playModeState;
        private void PlayModeStateChangedHandler(UnityEditor.PlayModeStateChange state)
        {
            playModeState = state;

            PlayModeStateChangedEvent?.Invoke(state);
        }

        public static UnityEditor.PlayModeStateChange playModeState
        {
            get => _playModeState;
            set => _playModeState = value;
        }

        private static bool _saving;
        private void SceneSavingHandler(UnityEngine.SceneManagement.Scene scene, string path)
        {
            saving = true;
        }

        private void SceneSavedHandler(UnityEngine.SceneManagement.Scene scene)
        {
            saving = false;
        }

        private static bool saving
        {
            get => _saving;
            set =>_saving = value;
        }

        private static bool _assembling;
        private void BeforeAssemblyReloadHandler()
        {
            UpdateHarmonyPatches(true);

            tweenManager.DisposeAllTweens();

            BeforeAssemblyReloadEvent?.Invoke();
        }

        private static List<MonoBehaviourDisposable> _monoBehaviourDisposables;
        [UnityEditor.InitializeOnLoadMethod]
        private static void AfterAssemblyReloadHandler()
        {
            if (!SceneManager.IsSceneBeingDestroyed())
            {
                //Editor objects are not listed in FindObjectsOfType, so we find them manually.
                SceneManager sceneManager = Instance(false);
                if (sceneManager != Disposable.NULL)
                {
                    sceneManager.sceneCameraTransforms.ForEach((sceneCameraTransform) =>
                    {
                        if (sceneCameraTransform != Disposable.NULL)
                        {
                            _monoBehaviourDisposables ??= new();

                            _monoBehaviourDisposables.Clear();
                            sceneCameraTransform.GetComponents(_monoBehaviourDisposables);
                            foreach (MonoBehaviourDisposable monoBehaviourDisposable in _monoBehaviourDisposables)
                                monoBehaviourDisposable.AfterAssemblyReload();

                            _monoBehaviourDisposables.Clear();
                            Editor.SceneCamera sceneCamera = sceneCameraTransform.GetComponent<Editor.SceneCamera>();
                            if (sceneCamera != null & sceneCamera.targetController != null && sceneCamera.targetController.target != null)
                                sceneCamera.targetController.target.GetComponents(_monoBehaviourDisposables);
                            foreach (MonoBehaviourDisposable monoBehaviourDisposable in _monoBehaviourDisposables)
                                monoBehaviourDisposable.AfterAssemblyReload();
                        }
                    });
                }

                MonoBehaviourDisposable[] monoBehaviourDisposables = Resources.FindObjectsOfTypeAll<MonoBehaviourDisposable>();
                foreach (MonoBehaviourDisposable monoBehaviourDisposable in monoBehaviourDisposables)
                    monoBehaviourDisposable.AfterAssemblyReload();
                ScriptableObjectDisposable[] scriptableObjectDisposables = Resources.FindObjectsOfTypeAll<ScriptableObjectDisposable>();
                foreach (ScriptableObjectDisposable scriptableObjectDisposable in scriptableObjectDisposables)
                    scriptableObjectDisposable.AfterAssemblyReload();
            }
        }

        public override bool AfterAssemblyReload()
        {
            if (base.AfterAssemblyReload())
            {
                //Necessary?
                Resources.UnloadUnusedAssets();

                return true;
            }
            return false;
        }

        private void SelectionChangedHandler()
        {
            Editor.Selection.SelectionChanged();
        }

        private void SceneClosingHandler(Scene scene, bool removingScene)
        {
            try
            {
                if (gameObject.scene == scene)
                {
                    UnityEditor.SceneManagement.EditorSceneManager.sceneClosed += SceneClosed;
                    _sceneClosing = true;

                    SceneClosingEvent?.Invoke();
                }
            }catch(MissingReferenceException)
            { }
        }

        private void SceneClosed(Scene scene)
        {
            UnityEditor.SceneManagement.EditorSceneManager.sceneClosed -= SceneClosed;
            _sceneClosing = false;

            SceneClosedEvent?.Invoke();
        }

        private HarmonyLib.Harmony _harmony;
        private void UpdateHarmonyPatches(bool reloadingAssembly = false)
        {
            string harmonyId = id.ToString();

            if (!reloadingAssembly && !IsDisposing())
            {
                if (_harmony is null)
                {
                    _harmony = new HarmonyLib.Harmony(harmonyId);

                    MethodInfo unityMoveToView = typeof(UnityEditor.SceneView).GetMethod("MoveToView", new Type[] { });
                    MethodInfo preMoveToView = typeof(Editor.SceneViewDouble).GetMethod("PatchedPreMoveToView", BindingFlags.Static | BindingFlags.NonPublic);
                    _harmony.Patch(unityMoveToView, new HarmonyLib.HarmonyMethod(preMoveToView));

                    MethodInfo unityMoveToViewTarget = typeof(UnityEditor.SceneView).GetMethod("MoveToView", new Type[] { typeof(Transform) });
                    MethodInfo preMoveToViewTarget = typeof(Editor.SceneViewDouble).GetMethod("PatchedPreMoveToViewTarget", BindingFlags.Static | BindingFlags.NonPublic);
                    _harmony.Patch(unityMoveToViewTarget, new HarmonyLib.HarmonyMethod(preMoveToViewTarget));

                    MethodInfo unityAlignWithView = typeof(UnityEditor.SceneView).GetMethod("AlignWithView", new Type[] { });
                    MethodInfo preAlignWithView = typeof(Editor.SceneViewDouble).GetMethod("PatchedPreAlignWithView", BindingFlags.Static | BindingFlags.NonPublic);
                    _harmony.Patch(unityAlignWithView, new HarmonyLib.HarmonyMethod(preAlignWithView));

                    MethodInfo unityAlignViewToObject = typeof(UnityEditor.SceneView).GetMethod("AlignViewToObject", new Type[] { typeof(Transform) });
                    MethodInfo preAlignViewToObject = typeof(Editor.SceneViewDouble).GetMethod("PatchedPreAlignViewToObject", BindingFlags.Static | BindingFlags.NonPublic);
                    _harmony.Patch(unityAlignViewToObject, new HarmonyLib.HarmonyMethod(preAlignViewToObject));

                    MethodInfo unityFrameSelected = typeof(UnityEditor.SceneView).GetMethod("FrameSelected", new Type[] { typeof(bool), typeof(bool) });
                    MethodInfo preFrameSelected = typeof(Editor.SceneViewDouble).GetMethod("PatchedPreFrameSelected", BindingFlags.Static | BindingFlags.NonPublic);
                    _harmony.Patch(unityFrameSelected, new HarmonyLib.HarmonyMethod(preFrameSelected));

                    MethodInfo unityFrame = typeof(UnityEditor.SceneView).GetMethod("Frame", BindingFlags.Instance | BindingFlags.Public);
                    MethodInfo preFrame = typeof(Editor.SceneViewDouble).GetMethod("PatchedPreFrame", BindingFlags.Static | BindingFlags.NonPublic);
                    _harmony.Patch(unityFrame, new HarmonyLib.HarmonyMethod(preFrame));

                    MethodInfo unitySetupCamera = typeof(UnityEditor.SceneView).GetMethod("SetupCamera", BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                    MethodInfo postSetupCamera = typeof(Editor.SceneViewDouble).GetMethod("PatchedPostSetupCamera", BindingFlags.Static | BindingFlags.NonPublic);
                    _harmony.Patch(unitySetupCamera, null, new HarmonyLib.HarmonyMethod(postSetupCamera));

                    MethodInfo unityDefaultHandles = typeof(UnityEditor.SceneView).GetMethod("DefaultHandles", BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                    MethodInfo preDefaultHandles = typeof(Editor.SceneViewDouble).GetMethod("PatchedPreDefaultHandles", BindingFlags.Static | BindingFlags.NonPublic);
                    MethodInfo postDefaultHandles = typeof(Editor.SceneViewDouble).GetMethod("PatchedPostDefaultHandles", BindingFlags.Static | BindingFlags.NonPublic);
                    _harmony.Patch(unityDefaultHandles, new HarmonyLib.HarmonyMethod(preDefaultHandles), new HarmonyLib.HarmonyMethod(postDefaultHandles));

                    MethodInfo unityHandleSelectionAndOnSceneGUI = typeof(UnityEditor.SceneView).GetMethod("HandleSelectionAndOnSceneGUI", BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                    MethodInfo preHandleSelectionAndOnSceneGUI = typeof(Editor.SceneViewDouble).GetMethod("PatchedPreHandleSelectionAndOnSceneGUI", BindingFlags.Static | BindingFlags.NonPublic);
                    MethodInfo postHandleSelectionAndOnSceneGUI = typeof(Editor.SceneViewDouble).GetMethod("PatchedPostHandleSelectionAndOnSceneGUI", BindingFlags.Static | BindingFlags.NonPublic);
                    _harmony.Patch(unityHandleSelectionAndOnSceneGUI, new HarmonyLib.HarmonyMethod(preHandleSelectionAndOnSceneGUI), new HarmonyLib.HarmonyMethod(postHandleSelectionAndOnSceneGUI));

                    MethodInfo unityGetHandleSize = typeof(UnityEditor.HandleUtility).GetMethod("GetHandleSize", BindingFlags.Static | BindingFlags.Public);
                    MethodInfo postGetHandleSize = typeof(Editor.SceneViewDouble).GetMethod("PatchedPostGetHandleSize", BindingFlags.Static | BindingFlags.NonPublic);
                    _harmony.Patch(unityGetHandleSize, null, new HarmonyLib.HarmonyMethod(postGetHandleSize));

                    MethodInfo unitySetupArcMaterial = typeof(UnityEditor.Handles).GetMethod("SetupArcMaterial", BindingFlags.Static | BindingFlags.NonPublic);
                    MethodInfo postSetupArcMaterial = typeof(Editor.SceneViewDouble).GetMethod("PatchedPostSetupArcMaterial", BindingFlags.Static | BindingFlags.NonPublic);
                    _harmony.Patch(unitySetupArcMaterial, null, new HarmonyLib.HarmonyMethod(postSetupArcMaterial));

                    MethodInfo unityHandleMouseUp = Editor.SceneViewMotion.GetUnitySceneViewMotionType().GetMethod("HandleMouseUp", BindingFlags.Static | BindingFlags.NonPublic);
                    MethodInfo preHandleMouseUp = typeof(Editor.SceneViewMotion).GetMethod("PatchedPreHandleMouseUp", BindingFlags.Static | BindingFlags.NonPublic);
                    _harmony.Patch(unityHandleMouseUp, new HarmonyLib.HarmonyMethod(preHandleMouseUp));

                    MethodInfo unityHandleMouseDrag = Editor.SceneViewMotion.GetUnitySceneViewMotionType().GetMethod("HandleMouseDrag", BindingFlags.Static | BindingFlags.NonPublic);
                    MethodInfo preHandleMouseDrag = typeof(Editor.SceneViewMotion).GetMethod("PatchedPreHandleMouseDrag", BindingFlags.Static | BindingFlags.NonPublic);
                    _harmony.Patch(unityHandleMouseDrag, new HarmonyLib.HarmonyMethod(preHandleMouseDrag));

                    Type consoleWindowType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.ConsoleWindow");
                    MethodInfo unityConsoleWindowOnGUI = consoleWindowType.GetMethod("OnGUI", BindingFlags.NonPublic | BindingFlags.Instance);
                    MethodInfo preConsoleWindowOnGUI = typeof(SceneManager).GetMethod("PatchedPreConsoleWindowOnGUI", BindingFlags.NonPublic | BindingFlags.Static);
                    _harmony.Patch(unityConsoleWindowOnGUI, new HarmonyLib.HarmonyMethod(preConsoleWindowOnGUI));

                    Type appStatusBarType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.AppStatusBar");
                    MethodInfo unityAppStatusBarOnGUI = appStatusBarType.GetMethod("OldOnGUI", BindingFlags.NonPublic | BindingFlags.Instance);
                    MethodInfo preAppStatusBarOnGUI = typeof(SceneManager).GetMethod("PatchedPreAppStatusBarOnGUI", BindingFlags.NonPublic | BindingFlags.Static);
                    _harmony.Patch(unityAppStatusBarOnGUI, new HarmonyLib.HarmonyMethod(preAppStatusBarOnGUI));

                    MethodInfo unityGetInspectorTitle = null;
                    foreach (MethodInfo methodInfo in typeof(UnityEditor.ObjectNames).GetMethods())
                    {
                        if (methodInfo.Name == "GetInspectorTitle" && (unityGetInspectorTitle == null || unityGetInspectorTitle.GetParameters().Count() < methodInfo.GetParameters().Count()))
                            unityGetInspectorTitle = methodInfo;
                    }

                    MethodInfo postGetInspectorTitle = typeof(Editor.ObjectNames).GetMethod("PatchedPostGetInspectorTitle", BindingFlags.Static | BindingFlags.NonPublic);
                    _harmony.Patch(unityGetInspectorTitle, null, new HarmonyLib.HarmonyMethod(postGetInspectorTitle));

                    //Fix for a Unity Bug where a null item is sent to the DoItemGUI method in TreeViewController causing it to spam NullReferenceException.
                    //The bug usually occurs when a GameObject foldout is opened in the SceneHierarchy while new child objects are being added/created quickly by a loader or anything else. 
                    //The spamming, once triggered, will usually stop after the GameObject is deselected 
                    //TODO: File a bug report with Unity to fix the problem in the source code directly
                    MethodInfo unityDoItemGUI = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.IMGUI.Controls.TreeViewController").GetMethod("DoItemGUI", BindingFlags.Instance | BindingFlags.NonPublic);
                    MethodInfo preDoItemGUI = GetType().GetMethod(nameof(PatchedPreDoItemGUI), BindingFlags.Static | BindingFlags.NonPublic);
                    _harmony.Patch(unityDoItemGUI, new HarmonyLib.HarmonyMethod(preDoItemGUI));
                }
            }
            else
            {
                if (_harmony is not null)
                {
                    _harmony.UnpatchAll(harmonyId);
                    _harmony = null;
                }
            }
        }

        private void EditorPauseStateChangedHandler(UnityEditor.PauseState state)
        {
            UpdateUpdateDelegate();
        }

        private void UpdateUpdateDelegate(bool disposed = false)
        {
            UnityEditor.EditorApplication.update -= Update;
            if (!disposed)
                UnityEditor.EditorApplication.update += Update;
        }

        private static bool PatchedPreDoItemGUI(UnityEditor.IMGUI.Controls.TreeViewItem item)
        {
            return item != null;
        }

        private static bool PatchedPreConsoleWindowOnGUI()
        {
            RemoveFilteredLogEntries();
            return true;
        }

        private static bool PatchedPreAppStatusBarOnGUI()
        {
            RemoveFilteredLogEntries();
            return true;
        }

        private GameObject[] _copyingGameObjects;
        private bool[] _copyingGameObjectsActive;
        private void CopyingGameObjectsHandler(GameObject[] gameObjects)
        {
            _copyingGameObjects = gameObjects;
            _copyingGameObjectsActive = new bool[_copyingGameObjects.Length];

            for (int i = gameObjects.Length - 1; i >= 0; i--)
            {
                GameObject go = gameObjects[i];
                _copyingGameObjects[i] = go;
                _copyingGameObjectsActive[i] = go.activeSelf;
                go.SetActive(true);
            }
        }

        private void PastedGameObjectsHandler(GameObject[] gameObjects)
        {
            for (int i = gameObjects.Length - 1; i >= 0; i--)
                gameObjects[i].SetActive(_copyingGameObjects[i].activeSelf);
        }

        private void ResetCopyingGameObjectsActiveState()
        {
            if (_copyingGameObjectsActive != null)
            {
                for (int i = _copyingGameObjectsActive.Length - 1; i >= 0; i--)
                    _copyingGameObjects[i].SetActive(_copyingGameObjectsActive[i]);
                _copyingGameObjectsActive = null;
            }
        }

        private static HashSet<int> _removeEntryRows = new();
        private static Regex _logConsoleFilterRegEx = new("dangling during");
        private void LogMessageReceivedHandler(string logString, string stackTrace, LogType type)
        {
            if (logConsoleFiltering && _logConsoleFilterRegEx.IsMatch(logString))
            {
                string filteringText = null; bool collapse = false; bool log = false; bool warning = false; bool error = false; bool showTimestamp = false;
                Editor.LogEntries.GetConsoleFlags(ref filteringText, ref collapse, ref log, ref warning, ref error, ref showTimestamp);

                Editor.LogEntries.SetFilteringText("");
                Editor.LogEntries.SetConsoleFlag(Editor.LogEntries.COLLAPSE_FLAG, false);
                Editor.LogEntries.SetConsoleFlag(Editor.LogEntries.LOG_FLAG, true);
                Editor.LogEntries.SetConsoleFlag(Editor.LogEntries.WARNING_FLAG, true);
                Editor.LogEntries.SetConsoleFlag(Editor.LogEntries.ERROR_FLAG, true);

                _removeEntryRows.Add(Editor.LogEntries.StartGettingEntries());
                Editor.LogEntries.EndGettingEntries();

                Editor.LogEntries.SetFilteringText(filteringText);
                Editor.LogEntries.SetConsoleFlag(Editor.LogEntries.COLLAPSE_FLAG, collapse);
                Editor.LogEntries.SetConsoleFlag(Editor.LogEntries.LOG_FLAG, log);
                Editor.LogEntries.SetConsoleFlag(Editor.LogEntries.WARNING_FLAG, warning);
                Editor.LogEntries.SetConsoleFlag(Editor.LogEntries.ERROR_FLAG, error);
            }
        }

        private static void RemoveFilteredLogEntries()
        {
            if (Editor.LogEntries.RemoveEntries(_removeEntryRows) != 0)
                _removeEntryRows.Clear();
        }

        public static bool IsEditorNamespace(Type type)
        {
            return type.Namespace == typeof(Editor.SceneCamera).Namespace;
        }

        private List<TransformBase> sceneCameraTransforms
        {
            get => _sceneCameraTransforms;
        }
        [SerializeField, HideInInspector]
        private List<Editor.SceneViewDouble> _sceneViewDoubles;
        public List<Editor.SceneViewDouble> sceneViewDoubles
        {
            get => _sceneViewDoubles;
        }

        public void SceneViewDoubleDisposed(Editor.SceneViewDouble sceneViewDouble)
        {
            _sceneViewDoubles.Remove(sceneViewDouble);
        }

        public string buildOutputPath
        {
            get => _buildOutputPath;
            set => SetValue(nameof(buildOutputPath), value, ref _buildOutputPath);
        }

        public UnityEditor.BuildAssetBundleOptions buildOptions
        {
            get => _buildOptions;
            set => SetValue(nameof(buildOptions), value, ref _buildOptions);
        }

        public UnityEditor.BuildTarget buildTarget
        {
            get => _buildTarget;
            set => SetValue(nameof(buildTarget), value, ref _buildTarget);
        }

        public static void MarkSceneDirty()
        {
            if (!Application.isPlaying)
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }

        private bool AddSceneCameraTransform(TransformBase sceneCameraTransform)
        {
            InitSceneCameraTransforms();
            if (!_sceneCameraTransforms.Contains(sceneCameraTransform))
            {
                _sceneCameraTransforms.Add(sceneCameraTransform);
                return true;
            }
            return false;
        }

        private bool RemoveSceneCameraTransform(TransformBase sceneCameraTransform)
        {
            if (_sceneCameraTransforms.Remove(sceneCameraTransform))
                return true;
            return false;
        }
#endif

        private static int _isUserChange;
        public static bool GetIsUserChangeContext() => _isUserChange > 0;
        /// <summary>
        /// Indicates that the following code will be executed as a result of an Editor User action.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining), HideInCallstack]
        public static void StartUserContext()
        {
            _isUserChange++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining), HideInCallstack]
        public static void EndUserContext()
        {
            _isUserChange--;
        }

        public static bool IsValidActiveStateChange()
        {
            bool isValid = true;

#if UNITY_EDITOR
            isValid = (UnityEditor.EditorApplication.isPlaying && UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) || (!UnityEditor.EditorApplication.isPlaying && !UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode);
#endif

            return isValid;
        }

        protected override PropertyMonoBehaviour GetParent()
        {
            return null;
        }

        protected override PropertyMonoBehaviour GetRootParent()
        {
            return null;
        }

        protected override Type GetSiblingType()
        {
            return typeof(ManagerBase);
        }

        private List<ManagerBase> _managerList;
        protected override bool SiblingsHasChanged()
        {
            bool siblingChanged = base.SiblingsHasChanged();

            if (!siblingChanged)
            {
                _managerList ??= new List<ManagerBase>();
                GetComponents(_managerList);
                int unityManagerCount = _managerList.Count;
                foreach (ManagerBase manager in _managerList) 
                {
                    if (manager is SceneManager)
                        unityManagerCount--;
                }

                int managerCount = GetManagerCount();
                if (managerCount != unityManagerCount)
                    siblingChanged = true;
            }

            return siblingChanged;
        }

        protected int GetManagerCount()
        {
            int scriptCount = 0;

            if (_tweenManager != Disposable.NULL)
                scriptCount++;
            if (_poolManager != Disposable.NULL)
                scriptCount++;
            if (_inputManager != Disposable.NULL)
                scriptCount++;
            if (_cameraManager != Disposable.NULL)
                scriptCount++;
            if (_renderingManager != Disposable.NULL)
                scriptCount++;
            if (_datasourceManager != Disposable.NULL)
                scriptCount++;
            if (_jsonInterface != Disposable.NULL)
                scriptCount++;
            if (_instanceManager != Disposable.NULL)
                scriptCount++;

            return scriptCount;
        }

        public int childCount
        {
            get => sceneChildren.Count;
        }

        public void IterateOverChildren<T>(Func<T, bool> callback, bool includeDontSave = true) where T : PropertyMonoBehaviour
        {
            foreach (TransformBase transform in sceneChildren)
            {
                if (transform is T && transform != Disposable.NULL && (includeDontSave || !transform.gameObject.hideFlags.HasFlag(HideFlags.DontSave)) && !callback(transform as T))
                    return;
            }
        }

        protected override Type GetChildType()
        {
            return typeof(PropertyMonoBehaviour);
        }

        protected override bool ChildrenHasChanged()
        {
            bool childrenChanged = base.ChildrenHasChanged();

            if (!childrenChanged)
            {
                Scene scene = gameObject.scene;
                if (scene != null && scene.isLoaded)
                {
                    int visibleRootGameObjectsCount = 0;

                    //Count only the gameObjects which do not have DontSave hideFlags as they are not included in the scene.rootCount
                    IterateOverChildren<TransformBase>((propertyMonoBehaviour) =>
                    {
                        visibleRootGameObjectsCount++;
                        return true;
                    }, false);

                    //Subtract one for the SceneManager which is not present in the SceneChildren list
                    if (visibleRootGameObjectsCount != scene.rootCount - 1)
                        childrenChanged = true;
                }
            }

            return childrenChanged;
        }

        protected override void UpdateChildren()
        {
#if UNITY_EDITOR
            //SceneCameras are not included in the GetRootGameObjects()
            foreach (UnityEditor.SceneView sceneView in UnityEditor.SceneView.sceneViews)
                SetGameObjectParent(sceneView.camera.gameObject);
#endif
            foreach (GameObject rootGameObject in GetRootGameObjects())
                SetGameObjectParent(rootGameObject);
        }

        private void SetGameObjectParent(GameObject go)
        {
            if (go != null && go != gameObject)
            {
                TransformBase childComponent = go.GetComponent<TransformBase>();
                if (childComponent != Disposable.NULL)
                {
                    if (InitializeComponent(childComponent))
                        childComponent.UpdateParent(this);
                }
            }
        }

        private List<GameObject> _rootGameObjects;
        public List<GameObject> GetRootGameObjects()
        {
            _rootGameObjects ??= new List<GameObject>();
            _rootGameObjects.Clear();

            Scene scene = gameObject.scene;
            if (scene != null && scene.isLoaded)
                scene.GetRootGameObjects(_rootGameObjects);
            else
            {
                foreach (GameObject go in FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (go.transform.parent == null && !go.hideFlags.HasFlag(HideFlags.DontSave))
                        _rootGameObjects.Add(go);
                }
            }

            return _rootGameObjects;
        }

        public static bool IsSceneBeingDestroyed()
        {
            return sceneClosing || !IsValidActiveStateChange();
        }

        public static bool sceneClosing
        {
            get => _sceneClosing;
        }

        public static ExecutionState sceneExecutionState
        {
            get => _sceneExecutionState; 
            set => _sceneExecutionState = value;
        }

        private List<TransformBase> sceneChildren
        {
            get { _sceneChildren ??= new List<TransformBase>(); return _sceneChildren; }
        }

        /// <summary>
        /// When enabled some hidden properties and objects will be exposed to help with debugging.
        /// </summary>
        [Json]
        public bool debug
        {
            get 
            {
                bool debug = _debug;
#if !UNITY_EDITOR
                debug = false;
#endif
                return debug; 
            }
            set
            {
                SetValue(nameof(debug), value, ref _debug, (newValue, oldValue) =>
                {
#if UNITY_EDITOR
                    _lastDebug = newValue;
                    DebugChangedEvent?.Invoke(newValue);
#endif
                });
            }
        }

        /// <summary>
        /// When enabled <see cref="DepictionEngine.CameraGrid2DLoader"/>'s will display their loading/loaded count in the inspector next to the GameObject name.
        /// </summary>
        [Json]
        public bool showLoadCountInInspector
        {
            get => _showLoadCountInInspector;
            set => SetValue(nameof(_showLoadCountInInspector), value, ref _showLoadCountInInspector, (newValue, oldValue) => 
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.RepaintHierarchyWindow();
#endif
            });
        }

        /// <summary>
        /// When enabled the framerate will be shown in the scene view windows.
        /// </summary>
        [Json]
        public bool showFrameRateInSceneViews
        {
            get => _showFrameRateInSceneViews;
            set => SetValue(nameof(_showFrameRateInSceneViews), value, ref _showFrameRateInSceneViews);
        }

        /// <summary>
        /// When enabled some log entries will be disable such as 'Child GameObject ... became dangling during undo'.
        /// </summary>
        [Json]
        public bool logConsoleFiltering
        {
            get => _logConsoleFiltering;
            set => SetValue(nameof(logConsoleFiltering), value, ref _logConsoleFiltering);
        }

        /// <summary>
        /// Should the player be running when the application is in the background?
        /// </summary>
        [Json]
        public bool runInBackground
        {
            get => _runInBackground;
            set 
            { 
                SetValue(nameof(runInBackground), value, ref _runInBackground, (newValue, oldValue) =>
                {
#if UNITY_EDITOR
                    _lastRunInBackground = newValue;
#endif
                    UpdateRunInBackground();
                }); 
            }
        }

        private void UpdateRunInBackground()
        {
            Application.runInBackground = runInBackground;
        }

        /// <summary>
        /// When enabled some <see cref="DepictionEngine.Processor"/>'s will perform their work on separate threads.
        /// </summary>
        [Json]
        public bool enableMultithreading
        {
            get => _enableMultithreading;
            set => SetValue(nameof(enableMultithreading), value, ref _enableMultithreading);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Debugging()
        {
            bool debug = false;

            if (!IsSceneBeingDestroyed())
            {
                SceneManager sceneManager = SceneManager.Instance(false);
                if (sceneManager != Disposable.NULL)
                    debug = sceneManager.debug;
            }

            return debug;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override bool AddChild(PropertyMonoBehaviour child)
        {
            if (child is TweenManager)
                _tweenManager = child as TweenManager;
            else if (child is PoolManager)
                _poolManager = child as PoolManager;
            else if (child is InputManager)
                _inputManager = child as InputManager;
            else if (child is DatasourceManager)
                _datasourceManager = child as DatasourceManager;
            else if (child is CameraManager)
                _cameraManager = child as CameraManager;
            else if (child is RenderingManager)
                _renderingManager = child as RenderingManager;
            else if (child is JsonInterface)
                _jsonInterface = child as JsonInterface;
            else if (child is InstanceManager)
                _instanceManager = child as InstanceManager;
            else
            {
                if (base.AddChild(child))
                {
                    if (child is TransformBase && child.transform.parent == null)
                    {
                        TransformBase transform = child as TransformBase;
#if UNITY_EDITOR
                        if (transform.name == CameraManager.SCENECAMERA_NAME)
                            AddSceneCameraTransform(transform);
                        else
#endif
                            sceneChildren.Add(transform);
                    }
                    return true;
                }
                else
                    return false;
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override bool RemoveChild(PropertyMonoBehaviour child)
        {
            if (this != Disposable.NULL)
            {
                if (child is TweenManager)
                    _tweenManager = null;
                else if (child is PoolManager)
                    _poolManager = null;
                else if (child is InputManager)
                    _inputManager = null;
                else if (child is DatasourceManager)
                    _datasourceManager = null;
                else if (child is CameraManager)
                    _cameraManager = null;
                else if (child is RenderingManager)
                    _renderingManager = null;
                else if (child is JsonInterface)
                    _jsonInterface = null;
                else if (child is InstanceManager)
                    _instanceManager = null;
                else
                {
                    if (base.RemoveChild(child))
                    {
                        if (child is TransformBase)
                        {
                            TransformBase transform = child as TransformBase;
#if UNITY_EDITOR
                            //Use the objectBase name to avoid the "You are trying to access a destroyed object..." error on Destroyed object during Undo/Redo
                            if (transform.objectBase != Disposable.NULL && transform.objectBase.name == CameraManager.SCENECAMERA_NAME)
                                RemoveSceneCameraTransform(transform);
                            else
#endif
                                sceneChildren.Remove(transform);
                        }
                        return true;
                    }
                    else
                        return false;
                }
                return true;
            }
            return false;
        }

        private static List<PropertyMonoBehaviour> _selectedPropertyMonoBehaviours = new();
        public void Update()
        {
            if (this != Disposable.NULL)
            {
#if UNITY_EDITOR
                //Make sure GameObjects are always active when copy/pasted in the Editor so that Awake is always called.
                ResetCopyingGameObjectsActiveState();

                //Detect inspector actions such as Reset or Copy Component Values.
                Editor.InspectorManager.DetectInspectorActions();

                //Detect GameObjects/Components created in the Editor.
                HierarchicalInitializeEditorCreatedObjects();
                Editor.UndoManager.ProcessQueuedOperations();

                //Capture changes happening in Unity(Inspector, Transform etc...) such as: Name, Index, Layer, Tag, MeshRenderer Material, Enabled, GameObjectActive, Transform in the sceneview(localPosition, localRotation, localScale)
                foreach (GameObject go in UnityEditor.Selection.gameObjects)
                {
                    go.GetComponents(_selectedPropertyMonoBehaviours);
                    foreach (PropertyMonoBehaviour propertyMonoBehaviour in _selectedPropertyMonoBehaviours)
                    {
                        if (propertyMonoBehaviour != Disposable.NULL && !propertyMonoBehaviour.isFallbackValues)
                        {
                            if (propertyMonoBehaviour is TransformBase transform)
                                transform.DetectGameObjectChanges();

                            StartUserContext();

                            if (propertyMonoBehaviour is JsonMonoBehaviour jsonMonoBehaviour)
                                jsonMonoBehaviour.DetectUserGameObjectChanges();

                            propertyMonoBehaviour.UpdateActiveAndEnabled();

                            EndUserContext();
                        }
                    }
                }
#endif

                //Capture physics driven transform change
                DetectTransformChange(instanceManager.physicTransforms);

                if (!_updated)
                {
                    _updated = true;
                    sceneExecutionState = ExecutionState.Update;
                    PreHierarchicalUpdate();
                    HierarchicalUpdate();
                    PostHierarchicalUpdate();
                    sceneExecutionState = ExecutionState.None;
                }
            }
        }

#if UNITY_EDITOR
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void HierarchicalInitializeEditorCreatedObjects()
        {
            if (Editor.UndoManager.DetectEditorUndoRedoRegistered())
            {
                InstanceManager.InitializingContext(() =>
                {
                    HierarchicalInitialize();
                }, InitializationContext.Editor);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool HierarchicalInitialize()
        {
            if (base.HierarchicalInitialize())
            {
                InstanceManager.LateInitializeObjects();
                return true;
            }
            return false;
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool PreHierarchicalUpdate()
        {
            if (base.PreHierarchicalUpdate())
            {
                FinishInitializingObjects();
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool HierarchicalUpdate()
        {
            if (base.HierarchicalUpdate())
            {
                FinishInitializingObjects();
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool PostHierarchicalUpdate()
        {
            if (base.PostHierarchicalUpdate())
            {
                FinishInitializingObjects();
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FinishInitializingObjects()
        {
            InstanceManager.LateInitializeObjects();
#if UNITY_EDITOR
            Editor.UndoManager.ProcessQueuedOperations();
#endif
        }

        private void FixedUpdate()
        {
            InstanceManager instanceManager = InstanceManager.Instance(false);
            if (instanceManager != Disposable.NULL && instanceManager.physicTransforms != null && instanceManager.physicTransforms.Count() > 0)
            {
                //Capture physics driven transform change
                DetectTransformChange(instanceManager.physicTransforms);

                RenderingManager renderingManager = RenderingManager.Instance(false);
                if (renderingManager != Disposable.NULL && renderingManager.originShifting)
                {
                    //The physics camera is the camera relative to which we render physics when origin shifting is enabled.
                    //The physics camera will be the main camera, if there is one, otherwise it will be any other camera.
                    Camera physicsCamera = Camera.main;

                    if (physicsCamera == Disposable.NULL)
                    {
                        instanceManager.IterateOverInstances<Camera>(
                            (camera) =>
                            {
#if UNITY_EDITOR
                                if (camera is Editor.SceneCamera)
                                    camera = null;
#endif
                                if (camera != Disposable.NULL)
                                {
                                    physicsCamera = camera;
                                    return false;
                                }

                                return true;
                            });
                    }

#if UNITY_EDITOR
                    //If we do not have a physicsCamera and we are in the Editor we use the first SceneCamera.
                    if (physicsCamera == Disposable.NULL)
                    {
                        instanceManager.IterateOverInstances<Camera>(
                            (camera) =>
                            {
                                if (camera is Editor.SceneCamera && camera != Disposable.NULL)
                                {
                                    physicsCamera = camera;
                                    return false;
                                }

                                return true;
                            });
                    }
#endif

                    if (physicsCamera != Disposable.NULL && physicsCamera.transform != Disposable.NULL)
                        TransformDouble.ApplyOriginShifting(physicsCamera.GetOrigin());
                }
            }
        }

        public void DetectTransformChange(IList<TransformDouble> transforms)
        {
            IterateThroughList(transforms, (transform) => { transform.DetectTransformChanges(); });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LateUpdate()
        {
            InvokeAction(ref LateUpdateEvent, "LateUpdate", ExecutionState.LateUpdate);

            DisposeManager.DisposingContext(() => { InvokeAction(ref DelayedOnDestroyEvent, "DelayedOnDestroy", ExecutionState.DelayedOnDestroy); }, DisposeContext.Editor_Destroy);

#if UNITY_EDITOR
            //Unsubscribe and Subscribe again to stay at the bottom of the invocation list. 
            //Could be replaced with [DefaultExecutionOrder(-3)] potentially?
            UpdateUpdateDelegate();
#endif
            Datasource.ResetAllowAutoDispose();
            _isUserChange = 0;
            _updated = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InvokeAction(ref Action action, string actionName, ExecutionState sceneExecutionState, bool clear = true)
        {
            if (action != null)
            {
#if UNITY_EDITOR
                int delegatesCount = action.GetInvocationList().Length;
#endif

                SceneManager.sceneExecutionState = sceneExecutionState;
                action();
                SceneManager.sceneExecutionState = ExecutionState.None;

#if UNITY_EDITOR
                if (delegatesCount != action.GetInvocationList().Length)
                    Debug.LogError(actionName + " delegate list Changed!");
#endif
                if (clear)
                    action = null;
            }
        }

        private int _stackedCameraCount;
        private int test;
        private void BeginCameraRendering(ScriptableRenderContext context, UnityEngine.Camera unityCamera)
        {
            if (IsDisposing())
                return;

            Camera camera = instanceManager.GetCameraFromUnityCamera(unityCamera);

            if (camera != Disposable.NULL)
            {
                _stackedCameraCount = 0;
                if (camera.additionalData != null)
                {
                    foreach (UnityEngine.Camera stackedUnityCamera in camera.additionalData.cameraStack)
                    {
                        if (stackedUnityCamera.isActiveAndEnabled)
                            _stackedCameraCount++;
                    }
                }

                test = _stackedCameraCount;

                BeginCameraRendering(camera, context);

#if UNITY_EDITOR
                if (camera is Editor.SceneCamera sceneCamera)
                    sceneCamera.RenderDistancePass(context);
#endif
            }
            else
            {
                Camera parentCamera = unityCamera.GetComponentInParent<Camera>();
                if ((parentCamera is not RTTCamera) && parentCamera != Disposable.NULL)
                {
                    renderingManager.BeginCameraDistancePassRendering(parentCamera, unityCamera, test);
                    test--;
                }
            }
        }

        public void BeginCameraRendering(Camera camera, ScriptableRenderContext? context = null)
        {
            //Preemptively apply origin shifting for proper camera relative raycasting
            TransformDouble.ApplyOriginShifting(camera.GetOrigin());
     
            //Apply Ignore Render layerMask so that visual objects are property excluded from raycasting
            instanceManager.IterateOverInstances<TransformBase>(
                (transform) =>
                {
                    if (transform.objectBase is VisualObject visualObject)
                        visualObject.ApplyCameraMaskLayerToVisuals(camera);

                    return true;
                });

            //Update Mouse properties
            inputManager.UpdateCameraMouse(camera);
          
            //Update Camera Controllers
            instanceManager.IterateOverInstances<Camera>(
                (updateCamera) =>
                {
                    if (updateCamera.controller != Disposable.NULL)
                        updateCamera.controller.UpdateControllerTransform(camera);

                    return true;
                });

            //Update AstroObject Controllers
            UpdateAstroObjects(camera);

            //Apply all the latest Transform changes
            TransformDouble.ApplyOriginShifting(camera.GetOrigin());

            //Update Shaders and UnityTransform
            HierarchicalBeginCameraRendering(camera);
       
            //Update the Star before the Reflection/ReflectionProbe renders are generated
            //Note: The Star LensFlare might require a raycast so we make sure final Origin shifting as been performed before we get it ready for render
            Star star = instanceManager.GetStar();
            if (star != Disposable.NULL)
                star.UpdateStar(camera);

            //Generate Reflection/ReflectionProbe renders and assign the resulting textures to the Shaders/RenderSettings
            renderingManager.BeginCameraRendering(camera, context);
        }

        public void UpdateAstroObjects(Camera camera)
        {
            instanceManager.IterateOverInstances<AstroObject>(
                (astroObject) =>
                {
                    if (astroObject.controller != Disposable.NULL)
                        astroObject.controller.UpdateControllerTransform(camera);

                    return true;
                });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EndCameraRendering(ScriptableRenderContext context, UnityEngine.Camera unityCamera)
        {
            if (IsDisposing())
                return;

            Camera camera = instanceManager.GetCameraFromUnityCamera(unityCamera);

            if (camera == Disposable.NULL)
            {
                Stack stack = unityCamera.GetComponentInParent<Stack>();
                if (stack != null)
                {
                    _stackedCameraCount--;

                    Camera parentCamera = unityCamera.GetComponentInParent<Camera>();
                    if ((parentCamera is not RTTCamera) && parentCamera != Disposable.NULL)
                        renderingManager.EndCameraDistancePassRendering(parentCamera, unityCamera);

                    if (_stackedCameraCount == 0)
                        camera = parentCamera;
                }
            }
            else if (camera.additionalData != null && camera.additionalData.cameraStack.Count != 0)
                camera = null;

            if (camera != Disposable.NULL)
                EndCameraRendering(camera);
        }

        public void EndCameraRendering(Camera camera)
        {
            HierarchicalEndCameraRendering(camera);

            renderingManager.EndCameraRendering(camera);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override bool ApplyBeforeChildren(Action<PropertyMonoBehaviour> callback)
        {
            bool containsDisposed = base.ApplyBeforeChildren(callback);

            if (_tweenManager is not null && !TriggerCallback(_tweenManager, callback))
                containsDisposed = true;
            if (_poolManager is not null && !TriggerCallback(_poolManager, callback))
                containsDisposed = true;
            if (_cameraManager is not null && !TriggerCallback(_cameraManager, callback))
                containsDisposed = true;
            if (_inputManager is not null && !TriggerCallback(_inputManager, callback))
                containsDisposed = true;
            if (_instanceManager is not null && !TriggerCallback(_instanceManager, callback))
                containsDisposed = true;
            if (_jsonInterface is not null && !TriggerCallback(_jsonInterface, callback))
                containsDisposed = true;

            return containsDisposed;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override bool ApplyAfterChildren(Action<PropertyMonoBehaviour> callback)
        {
            bool containsDisposed = base.ApplyAfterChildren(callback);

            if (_datasourceManager is not null && !TriggerCallback(_datasourceManager, callback))
                containsDisposed = true;

            if (_renderingManager is not null && !TriggerCallback(_renderingManager, callback))
                containsDisposed = true;

            return containsDisposed;
        }

        public override bool OnDispose(DisposeContext disposeContext)
        {
            if (base.OnDispose(disposeContext))
            {
                LateUpdateEvent = null;
                DelayedOnDestroyEvent = null;

#if UNITY_EDITOR
                SceneClosingEvent = null;
                BeforeAssemblyReloadEvent = null;
                LeftMouseUpInSceneOrInspectorEvent = null;
#endif
                return true;
            }
            return false;
        }
    }
}
