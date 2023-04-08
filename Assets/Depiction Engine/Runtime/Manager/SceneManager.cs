// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using UnityEngine.Rendering;
using UnityEngine;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Reflection;
using UnityEngine.UIElements;
using System.Text.RegularExpressions;

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
        /// <b><see cref="PreHierarchicalUpdate"/>:</b> <br/>
        /// The hierarchy is traversed and values are prepared for the update. <br/><br/>
        /// <b><see cref="HierarchicalUpdate"/>:</b> <br/>
        /// The hierarchy is traversed and the update code is executed. <br/><br/>
        /// <b><see cref="PostHierarchicalUpdate"/>:</b> <br/>
        /// The hierarchy is traversed and code that required updated values is executed. <br/><br/>
        /// <b><see cref="HierarchicalClearDirtyFlags"/>:</b> <br/>
        /// The hierarchy is traversed and dirty flags are cleared. <br/><br/>
        /// <b><see cref="HierarchicalActivate"/>:</b> <br/>
        /// The hierarchy is traversed and gameObjects that have never been active are temporarly activated and deactivated to allow for their Awake to be called. <br/><br/>
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
            PreHierarchicalUpdate,
            HierarchicalUpdate,
            PostHierarchicalUpdate,
            HierarchicalClearDirtyFlags,
            HierarchicalActivate,
            PastingComponentValues,
            LateUpdate,
            DelayedOnDestroy
        };

        public const string NAMESPACE = "DepictionEngine";
        public const string SCENE_MANAGER_NAME = "Managers (Required)";

        [BeginFoldout("Editor")]
        [SerializeField, Tooltip("When enabled some hidden properties and objects will be exposed to help with debugging.")]
        private bool _debug;
        [SerializeField, Tooltip("When enabled some log entries will be disable such as 'Child GameObject ... became dangling during undo'."), EndFoldout]
        private bool _logConsoleFiltering;

        [BeginFoldout("Performance")]
        [SerializeField, Tooltip("Should the player be running when the application is in the background?")]
        private bool _runInBackground;
        [SerializeField, Tooltip("When enabled some "+nameof(Processor)+"'s will perform their work on seperate threads."), EndFoldout]
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

        private bool _updated;

        private static bool _isUserChange;

        private static bool _activateAll;

        private static ExecutionState _sceneExecutionState;

        private static bool _sceneClosing;

        /// <summary>
        /// Dispatched at the end of the <see cref="DepictionEngine.SceneManager.LateUpdate"/> just before the DelayedOnDestroy.
        /// </summary>
        public static Action LateInitializeEvent;
        /// <summary>
        /// Dispatched at the end of the <see cref="DepictionEngine.SceneManager.LateUpdate"/> just before the DelayedOnDestroy.
        /// </summary>
        public static Action PostLateInitializeEvent;

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
        /// Dispatched at the end of the <see cref="DepictionEngine.SceneManager.LateUpdate"/> just before the DelayedOnDestroy.
        /// </summary>
        public static Action ResetRegisterCompleteUndoEvent;

        /// <summary>
        /// Dispatched when the parent Scene of the <see cref="DepictionEngine.SceneManager"/> gameObject is closing.
        /// </summary>
        public static Action SceneClosingEvent;
        /// <summary>
        /// Dispatched at the same time as the <see cref="UnityEditor.AssemblyReloadEvents.beforeAssemblyReload"/>.
        /// </summary>
        public static Action BeforeAssemblyReloadEvent;
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

        protected override void InitializeFields(InitializationContext initializingContext)
        {
            base.InitializeFields(initializingContext);

#if UNITY_EDITOR            
            UnityEditor.PlayerSettings.allowUnsafeCode = true;

            InitSceneCameraTransforms();

            Editor.AssetManager.InitListeners();

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

#if UNITY_EDITOR
        private void InitSceneCameraTransforms()
        {
            _sceneCameraTransforms ??= new List<TransformBase>();
        }
#endif

        protected override void InitializeSerializedFields(InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            InitValue(value => debug = value, false, initializingContext);
            InitValue(value => logConsoleFiltering = value, true, initializingContext);
            InitValue(value => runInBackground = value, true, initializingContext);
            InitValue(value => enableMultithreading = value, true, initializingContext);
#if UNITY_EDITOR
            InitValue(value => buildOutputPath = value, "Assets/DepictionEngine/Resources/AssetBundle", initializingContext);
            InitValue(value => buildOptions = value, UnityEditor.BuildAssetBundleOptions.None, initializingContext);
            InitValue(value => buildTarget = value, UnityEditor.BuildTarget.StandaloneWindows64, initializingContext);
#endif
        }

#if UNITY_EDITOR
        protected override void UpdateFields()
        {
            base.UpdateFields();

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
                    Application.logMessageReceived += LogMessageReceivedHandler;
                    UnityEditor.EditorApplication.pauseStateChanged += EditorPauseStateChangedHandler;
                    UnityEditor.EditorApplication.playModeStateChanged += PlayModeStateChangedHandler;
                    UnityEditor.SceneManagement.EditorSceneManager.sceneClosing += SceneClosingHandler;
                    UnityEditor.SceneManagement.EditorSceneManager.sceneSaving += SceneSavingHandler;
                    UnityEditor.SceneManagement.EditorSceneManager.sceneSaved += SceneSavedHandler;
                    UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += BeforeAssemblyReloadHandler;
                    UnityEditor.Selection.selectionChanged += SelectionChangedHandler;
                }

                Editor.AssetManager.InitListeners();

                Editor.UndoManager.UpdateAllDelegates(IsDisposing());

                UpdateUpdateDelegate(IsDisposing());

                UpdateHarmonyPatches();
#endif
                return true;
            }
            return false;
        }

#if UNITY_EDITOR
        private bool _lastDebug;
        private bool _lastRunInBackground;
        protected override void UndoRedoPerformed()
        {
            base.UndoRedoPerformed();

            SerializationUtility.PerformUndoRedoPropertyChange((value) => { debug = value; }, ref _debug, ref _lastDebug);
            SerializationUtility.PerformUndoRedoPropertyChange((value) => { runInBackground = value; }, ref _runInBackground, ref _lastRunInBackground);
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
            set
            {
                if (_playModeState == value)
                    return;

                _playModeState = value;
            }
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
            set
            {
                if (_saving == value)
                    return;
                _saving = value;
            }
        }

        private static bool _assembling;
        private void BeforeAssemblyReloadHandler()
        {
            UpdateHarmonyPatches(true);

            tweenManager.DisposeAllTweens();
            poolManager.DestroyAllDisposable();

            BeforeAssemblyReloadEvent?.Invoke();
        }

#if UNITY_EDITOR
        private static List<MonoBehaviourDisposable> _monoBehaviourDisposables;
        [UnityEditor.InitializeOnLoadMethod]
        private static void AfterAssemblyReloadHandler()
        {
            //Editor objects are not listed in FindObjectsOfType, so we find them manually.
            Instance(false)?.sceneCameraTransforms.ForEach((sceneCameraTransform) => 
            {
                if (sceneCameraTransform != Disposable.NULL)
                {
                    _monoBehaviourDisposables ??= new();

                    _monoBehaviourDisposables.Clear();
                    sceneCameraTransform.GetComponents(_monoBehaviourDisposables);
                    foreach (MonoBehaviourDisposable monoBehaviourDisposable in _monoBehaviourDisposables)
                        monoBehaviourDisposable.AfterAssemblyReload();

                    _monoBehaviourDisposables.Clear();
                    sceneCameraTransform.GetComponent<Editor.SceneCamera>()?.targetController?.target?.GetComponents(_monoBehaviourDisposables);
                    foreach (MonoBehaviourDisposable monoBehaviourDisposable in _monoBehaviourDisposables)
                        monoBehaviourDisposable.AfterAssemblyReload();
                }
            });

            MonoBehaviourDisposable[] monoBehaviourDisposables = FindObjectsOfType<MonoBehaviourDisposable>(true);
            foreach (MonoBehaviourDisposable monoBehaviourDisposable in monoBehaviourDisposables)
                monoBehaviourDisposable.AfterAssemblyReload();
            ScriptableObjectDisposable[] scriptableObjectDisposables = FindObjectsOfType<ScriptableObjectDisposable>(true);
            foreach(ScriptableObjectDisposable scriptableObjectDisposable in scriptableObjectDisposables)
                scriptableObjectDisposable.AfterAssemblyReload();

            //Necessary?
            Resources.UnloadUnusedAssets();
        }
#endif

        private void SelectionChangedHandler()
        {
            Editor.Selection.SelectionChanged();
        }

        private void SceneClosingHandler(UnityEngine.SceneManagement.Scene scene, bool removingScene)
        {
            if (gameObject.scene == scene)
            {
                UnityEditor.SceneManagement.EditorSceneManager.sceneClosed += SceneClosed;
                _sceneClosing = true;

                SceneClosingEvent?.Invoke();
            }
        }

        private void SceneClosed(UnityEngine.SceneManagement.Scene scene)
        {
            UnityEditor.SceneManagement.EditorSceneManager.sceneClosed -= SceneClosed;
            _sceneClosing = false;
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

                    MethodInfo unityGetInspectorTitle = typeof(UnityEditor.ObjectNames).GetMethod("GetInspectorTitle", BindingFlags.Static | BindingFlags.Public);
                    MethodInfo postGetInspectorTitle = typeof(Editor.ObjectNames).GetMethod("PatchedPostGetInspectorTitle", BindingFlags.Static | BindingFlags.NonPublic);
                    _harmony.Patch(unityGetInspectorTitle, null, new HarmonyLib.HarmonyMethod(postGetInspectorTitle));

                    //Fix for a Unity Bug where a null item is sent to the DoItemGUI method in TreeViewController causing it to spam NullReferenceException.
                    //The bug usually occurs when a GameObject foldout is opened in the SceneHierarchy while new child objects are being added/created quickly by a loader or anything else. 
                    //The spaming, once triggered, will usually stop after the GameObject is deselected 
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
#endif

        /// <summary>
        /// Makes available to the code executed in the callback, the user context under which it was triggered. If it was triggered by a user action, such as altering properties through the editor inspector or moving object using the manipulator, the value passed to isUserChange should be true. User context inside this callback can always be accessed by calling <see cref="DepictionEngine.IScriptableBehaviour.IsUserChangeContext"/>.
        /// </summary>
        /// <param name="callback">The code to execute.</param>
        /// <param name="isUserChange">Whether the current code execution was triggered by a user action.</param>
        public static void UserContext(Action callback, bool isUserChange = true)
        {
            if (callback is null)
                return;
            bool lastIsUserChange = _isUserChange;
            _isUserChange = isUserChange;
            callback();
            _isUserChange = lastIsUserChange;
        }

        public static bool IsUserChangeContext() => _isUserChange;

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

        public void IterateOverChildren<T>(Func<T, bool> callback) where T : PropertyMonoBehaviour
        {
            foreach (PropertyMonoBehaviour propertyMonoBehaviour in sceneChildren)
            {
                if (propertyMonoBehaviour is T && propertyMonoBehaviour != Disposable.NULL && !callback(propertyMonoBehaviour as T))
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
                List<GameObject> rootGameObjects = GetRootGameObjects();
                if (rootGameObjects != null)
                {
                    //Subtract one for the SceneManager which is not present in the SceneChildren list
                    int unityVisibleRootGameObjectsCount = rootGameObjects.Count;
                    foreach (GameObject go in rootGameObjects)
                    {
                        if (go.name == SCENE_MANAGER_NAME || IsGameObjectHiddenOrDontSave(go))
                            unityVisibleRootGameObjectsCount--;
                    }

                    int visibleRootGameObjectsCount = childCount;
                    //Subtract the gameObject which are not present in the GetRootGameObjects()
                    IterateOverChildren<TransformBase>((propertyMonoBehaviour) =>
                    {
                        if (IsGameObjectHiddenOrDontSave(propertyMonoBehaviour.gameObject))
                            visibleRootGameObjectsCount--;
                        return true;
                    });

                    if (visibleRootGameObjectsCount != unityVisibleRootGameObjectsCount)
                        childrenChanged = true;
                }
            }

            return childrenChanged;
        }

        private bool IsGameObjectHiddenOrDontSave(GameObject go)
        {
            HideFlags hideFlags = go.hideFlags;
            return hideFlags.HasFlag(HideFlags.HideAndDontSave) || hideFlags.HasFlag(HideFlags.DontSave) || hideFlags.HasFlag(HideFlags.HideInHierarchy);
        }

        protected override void UpdateChildren()
        {
#if UNITY_EDITOR
            //SceneCameras are not included in the GetRootGameObjects()
            foreach (UnityEditor.SceneView sceneView in UnityEditor.SceneView.sceneViews)
                SetGameObjectParent(sceneView.camera.gameObject);
#endif

            List<GameObject> rootGameObjects = GetRootGameObjects();
            if (rootGameObjects != null)
            {
                foreach (GameObject rootGameObject in rootGameObjects)
                    SetGameObjectParent(rootGameObject);
            }
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
            UnityEngine.SceneManagement.Scene scene = gameObject.scene;
            if (scene != null && scene.isLoaded)
            {
                _rootGameObjects ??= new List<GameObject>();
                //GetRootGameObjects does not include HideAndDontSave gameObjects
                scene.GetRootGameObjects(_rootGameObjects);

                return _rootGameObjects;
            }
            return null;
        }

#if UNITY_EDITOR
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
#endif

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
            set { _sceneExecutionState = value; }
        }

        private List<TransformBase> sceneChildren
        {
            get => _sceneChildren ??= new List<TransformBase>();
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
#endif
                });
            }
        }

        /// <summary>
        /// When enabled some log entries will be disable such as 'Child GameObject ... became dangling during undo'.
        /// </summary>
        [Json]
        public bool logConsoleFiltering
        {
            get => _logConsoleFiltering;
            set { SetValue(nameof(logConsoleFiltering), value, ref _logConsoleFiltering); }
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
        /// When enabled some <see cref="DepictionEngine.Processor"/>'s will perform their work on seperate threads.
        /// </summary>
        [Json]
        public bool enableMultithreading
        {
            get => _enableMultithreading;
            set { SetValue(nameof(enableMultithreading), value, ref _enableMultithreading); }
        }

#if UNITY_EDITOR
        public string buildOutputPath
        {
            get => _buildOutputPath;
            set { SetValue(nameof(buildOutputPath), value, ref _buildOutputPath); }
        }

        public UnityEditor.BuildAssetBundleOptions buildOptions
        {
            get => _buildOptions;
            set { SetValue(nameof(buildOptions), value, ref _buildOptions); }
        }

        public UnityEditor.BuildTarget buildTarget
        {
            get => _buildTarget;
            set { SetValue(nameof(buildTarget), value, ref _buildTarget); }
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

        public static void ActivateAll()
        {
            _activateAll = true;
        }

        public override void ExplicitOnEnable()
        {
            base.ExplicitOnEnable();

            _updated = false;
        }

#if UNITY_EDITOR
        //Inspector Methods
        private static List<Tuple<UnityEngine.Object[], string>> _queuedRecordObjects;
        public static void RecordObjects(UnityEngine.Object[] targetObjects, string undoGroupName = null)
        {
            _queuedRecordObjects ??= new List<Tuple<UnityEngine.Object[], string>>();

            _queuedRecordObjects.Add(Tuple.Create(targetObjects, undoGroupName));
        }

        private static List<Tuple<UnityEngine.Object, string>> _queuedRecordObject;
        public static void RecordObject(UnityEngine.Object targetObject, string undoGroupName = null)
        {
            _queuedRecordObject ??= new List<Tuple<UnityEngine.Object, string>>();

            _queuedRecordObject.Add(Tuple.Create(targetObject, undoGroupName));
        }

        private static List<Tuple<IScriptableBehaviour, PropertyInfo, object>> _queuedPropertyValueChanges;
        public static void QueuePropertyValueChange(IScriptableBehaviour scriptableBehaviour, PropertyInfo propertyInfo, object value)
        {
            _queuedPropertyValueChanges ??= new List<Tuple<IScriptableBehaviour, PropertyInfo, object>>();

            _queuedPropertyValueChanges.Add(Tuple.Create(scriptableBehaviour, propertyInfo, value));
        }

        public static void SetInspectorPropertyValue(IScriptableBehaviour targetObject, PropertyInfo propertyInfo, object value)
        {
            propertyInfo.SetValue(targetObject, value);
        }
#endif

        public void Update()
        {
            if (this != Disposable.NULL)
            {
                if (!_updated)
                {
                    _updated = true;

                    if (!wasFirstUpdated)
                    {
                        List<GameObject> rootGameObjects = GetRootGameObjects();
                        if (rootGameObjects != null)
                        {
                            foreach (GameObject go in rootGameObjects)
                            {
                                TransformBase childTransform = go.GetComponent<TransformBase>();
                                if (childTransform != Disposable.NULL)
                                    InitializeComponent(childTransform);
                            }
                        }
                    }

                    InvokeAction(ref LateInitializeEvent, "LateInitialize", ExecutionState.LateInitialize);
                    InvokeAction(ref PostLateInitializeEvent, "PostLateInitialize", ExecutionState.PostLateInitialize);
#if UNITY_EDITOR
                    Editor.UndoManager.Update();

                    if (_queuedRecordObjects != null)
                    {
                        foreach (Tuple<UnityEngine.Object[], string> queuedRecordObjects in _queuedRecordObjects)
                        {
                            UnityEngine.Object[] targetObjects = queuedRecordObjects.Item1;
                            string undoGroupName = queuedRecordObjects.Item2;
                            Editor.UndoManager.SetCurrentGroupName(string.IsNullOrEmpty(undoGroupName) ? "Modified Property in " + (targetObjects.Length == 1 ? targetObjects[0].name : "Multiple Objects") : undoGroupName);
                            Editor.UndoManager.RecordObjects(targetObjects);
                        }

                        _queuedRecordObjects.Clear();
                    }

                    if (_queuedRecordObject != null)
                    {
                        foreach (Tuple<UnityEngine.Object, string> queuedRecordObject in _queuedRecordObject)
                        {
                            UnityEngine.Object targetObject = queuedRecordObject.Item1;
                            string undoGroupName = queuedRecordObject.Item2;
                            Editor.UndoManager.SetCurrentGroupName(string.IsNullOrEmpty(undoGroupName) ? "Modified Property in " + targetObject.name : undoGroupName);
                            Editor.UndoManager.RecordObject(targetObject);
                        }

                        _queuedRecordObject.Clear();
                    }

                    if (_queuedPropertyValueChanges != null)
                    {
                        foreach (var queuedPropertyValue in _queuedPropertyValueChanges)
                        {
                            IScriptableBehaviour targetObject = queuedPropertyValue.Item1;
                            if (!Disposable.IsDisposed(targetObject))
                            {
                                try 
                                {
#pragma warning disable UNT0018 // System.Reflection features in performance critical messages
                                    Editor.UndoManager.RecordObject(targetObject as UnityEngine.Object);
                                    UserContext(() => { SetInspectorPropertyValue(targetObject, queuedPropertyValue.Item2, queuedPropertyValue.Item3); });
                                    Editor.UndoManager.FlushUndoRecordObjects();
#pragma warning restore UNT0018 // System.Reflection features in performance critical messages
                                }
                                catch(Exception)
                                {

                                }
                            }
                        }

                        _queuedPropertyValueChanges.Clear();
                    }
#endif

                    sceneExecutionState = ExecutionState.PreHierarchicalUpdate;
                    PreHierarchicalUpdate();
                    sceneExecutionState = ExecutionState.HierarchicalUpdate;
                    HierarchicalUpdate();
                    sceneExecutionState = ExecutionState.PostHierarchicalUpdate;
                    PostHierarchicalUpdate();
                    sceneExecutionState = ExecutionState.HierarchicalClearDirtyFlags;
                    HierarchicalClearDirtyFlags();

                    if (_activateAll)
                    {
                        sceneExecutionState = ExecutionState.HierarchicalActivate;
                        HierarchicalActivate();
                        _activateAll = false;
                    }
                }
#if UNITY_EDITOR
                // Ensure continuous Update calls.
                if (!Application.isPlaying || UnityEditor.EditorApplication.isPaused)
                    UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
#endif
            }
        }

        public void FixedUpdate()
        {
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

            HierarchicalFixedUpdate();

            HierarchicalDetectChanges();
        }

#if UNITY_EDITOR
        private static List<UnityEngine.Object> _resetingObjects = new();
        public static void Reseting(UnityEngine.Object scriptableBehaviour)
        {
            _resetingObjects.Add(scriptableBehaviour);
        }

        private static List<(IJson, JSONObject)> _pastingComponentValuesToObjects = new();
        public static void PastingComponentValues(IJson iJson, JSONObject json)
        {
            if (json[nameof(IProperty.id)] != null)
                json.Remove(nameof(IProperty.id));
            
            _pastingComponentValuesToObjects.Add((iJson, json));
        }
#endif

        protected override void LateUpdate()
        {
            base.LateUpdate();

#if UNITY_EDITOR
            if (_resetingObjects != null && _resetingObjects.Count > 0)
            {
                //We assume that all the objects are of the same type
                IScriptableBehaviour firstUnityObject = _resetingObjects[0] as IScriptableBehaviour;
                string groupName = "Reset " + (_resetingObjects.Count == 1 ? firstUnityObject.name : "Object") + " " + firstUnityObject.GetType().Name;

                Editor.UndoManager.SetCurrentGroupName(groupName);
                
                Editor.UndoManager.RecordObjects(_resetingObjects.ToArray());

                foreach (UnityEngine.Object unityObject in _resetingObjects)
                {
                    if (unityObject is IProperty)
                        (unityObject as IProperty).InspectorReset();
                }

                _resetingObjects.Clear();
            }

            sceneExecutionState = ExecutionState.PastingComponentValues;
            if (_pastingComponentValuesToObjects != null && _pastingComponentValuesToObjects.Count > 0)
            {
                //We assume that all the objects are of the same type
                IScriptableBehaviour firstUnityObject = _pastingComponentValuesToObjects[0].Item1;
                //'Pasted' is not a typo it is used to distinguish Unity 'Paste Component Values' action from the one we create. If there is no distinction then if the undo/redo actions are played again this code will be executed again and a new action will be recorded in the history erasing any subsequent actions.
                string groupName = "Pasted " + firstUnityObject.GetType().FullName + " Values";

                Editor.UndoManager.SetCurrentGroupName(groupName);
                
                UnityEngine.Object[] recordObjects = new UnityEngine.Object[_pastingComponentValuesToObjects.Count];
                for (int i = 0; i < recordObjects.Length; i++)
                    recordObjects[i] = _pastingComponentValuesToObjects[i].Item1 as UnityEngine.Object;
                Editor.UndoManager.RecordObjects(recordObjects);
                
                foreach ((IJson, JSONObject) pastingComponentValuesToObject in _pastingComponentValuesToObjects)
                {
                    IJson iJson = pastingComponentValuesToObject.Item1;
                    JSONObject json = pastingComponentValuesToObject.Item2;
                    UserContext(() => 
                    {
                        iJson.SetJson(json);
                    });
                }
                _pastingComponentValuesToObjects.Clear();
            }
            sceneExecutionState = ExecutionState.None;
#endif

            InvokeAction(ref LateUpdateEvent, "LateUpdate", ExecutionState.LateUpdate);

            DisposeManager.DisposingContext(() => 
            {
                InvokeAction(ref DelayedOnDestroyEvent, "DelayedOnDestroy", ExecutionState.DelayedOnDestroy);
            }, DisposeContext.Editor_Destroy);

#if UNITY_EDITOR
            ResetRegisterCompleteUndoEvent?.Invoke();

            //Unsubscribe and Subscribe again to stay at the bottom of the invocation list. 
            //Could be replaced with [DefaultExecutionOrder(-3)] potentialy?
            UpdateUpdateDelegate();
#endif

            _updated = false;
        }

        private void InvokeAction(ref Action action, string actionName, ExecutionState sceneExecutionState, bool clear = true)
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

                BeginCameraRendering(camera, context);

#if UNITY_EDITOR
                if (camera is Editor.SceneCamera)
                    (camera as Editor.SceneCamera).RenderDistancePass(context);
#endif
            }
            else
            {
                Camera parentCamera = unityCamera.GetComponentInParent<Camera>();
                if ((parentCamera is not RTTCamera) && parentCamera != Disposable.NULL)
                    renderingManager.BeginCameraDistancePassRendering(parentCamera, unityCamera);
            }
        }

        public void BeginCameraRendering(Camera camera, ScriptableRenderContext? context = null)
        {
            HierarchicalDetectChanges();

            //Preemptively apply origin shifting for proper camera relative raycasting
            TransformDouble.ApplyOriginShifting(camera.GetOrigin());

            //Apply Ignore Render layerMask so that viusal objects are property excluded from raycasting
            instanceManager.IterateOverInstances<TransformBase>(
                (transform) =>
                {
                    if (transform.objectBase is VisualObject)
                        (transform.objectBase as VisualObject).ApplyCameraMaskLayerToVisuals(camera);

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

            //Update the reflection probe Transform
            renderingManager.UpdateReflectionProbeTransform(camera);

            //Apply all the latest Transform changes
            TransformDouble.ApplyOriginShifting(camera.GetOrigin());
         
            //Update Shaders and UnityTransform
            HierarchicalBeginCameraRendering(camera);

            //Update the Star before the Reflection/ReflectionProbe renders are generated
            //Note: The Star LensFlare might require a Raycast so we make sure final Orgin shifting as been performed before we get it ready for render
            Star star = instanceManager.GetStar();
            if (star != Disposable.NULL)
                star.UpdateStar(camera);

            //Generate Reflection/ReflectionProbe renders and assign the resulting textures to the Shaders/RenderSettings
            if (context.HasValue)
            {
                renderingManager.ApplyEnvironmentAndReflectionToRenderSettings(camera);
                HierarchicalUpdateEnvironmentAndReflection(camera, context);
            }
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
            else
            {
                if (camera.additionalData != null && camera.additionalData.cameraStack.Count != 0)
                    camera = null;
            }

            if (camera != Disposable.NULL)
                EndCameraRendering(camera);
        }

        public void EndCameraRendering(Camera camera)
        {
            HierarchicalEndCameraRendering(camera);
        }

        protected override bool ApplyBeforeChildren(Action<PropertyMonoBehaviour> callback)
        {
            bool containsDisposed = base.ApplyBeforeChildren(callback);

            if (_tweenManager is not null && TriggerCallback(_tweenManager, callback))
                containsDisposed = true;
            if (_poolManager is not null && TriggerCallback(_poolManager, callback))
                containsDisposed = true;
            if (_cameraManager is not null && TriggerCallback(_cameraManager, callback))
                containsDisposed = true;
            if (_inputManager is not null && TriggerCallback(_inputManager, callback))
                containsDisposed = true;
            if (_instanceManager is not null && TriggerCallback(_instanceManager, callback))
                containsDisposed = true;
            if (_jsonInterface is not null && TriggerCallback(_jsonInterface, callback))
                containsDisposed = true;

            return containsDisposed;
        }

        protected override bool ApplyAfterChildren(Action<PropertyMonoBehaviour> callback)
        {
            bool containsDisposed = base.ApplyAfterChildren(callback);

            if (_datasourceManager is not null && TriggerCallback(_datasourceManager, callback))
                containsDisposed = true;
            if (_renderingManager is not null && TriggerCallback(_renderingManager, callback))
                containsDisposed = true;

            return containsDisposed;
        }
    }
}
